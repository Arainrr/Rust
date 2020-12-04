﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord.DiscordEvents;
using Oxide.Ext.Discord.DiscordObjects;

namespace Oxide.Plugins
{
    [Info("Discord Core Roles", "MJSU", "1.2.9")]
    [Description("Syncs players oxide group with discord roles")]
    partial class DiscordCoreRoles : CovalencePlugin
    {
        #region Class Fields

        [PluginReference] private Plugin DiscordCore;

        private PluginConfig _pluginConfig; //Plugin Config

        private readonly List<PlayerSync> _processIds = new List<PlayerSync>();
        private readonly Hash<string, string> _discordRoleLookup = new Hash<string, string>();
        
        private enum DebugEnum { Message, None, Error, Warning, Info}

        private Timer _playerChecker;

        private enum Source
        {
            Umod,
            Discord
        }

        #endregion

        #region Setup & Loading
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Loading Default Config");
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            Config.Settings.DefaultValueHandling = DefaultValueHandling.Populate;
            _pluginConfig = AdditionalConfig(Config.ReadObject<PluginConfig>());
            Config.WriteObject(_pluginConfig);
        }

        private PluginConfig AdditionalConfig(PluginConfig config)
        {
            config.SyncData = config.SyncData ?? new List<SyncData>
            {
                new SyncData
                {
                    Oxide = "Default",
                    Discord = "DiscordRoleNameOrId",
                    Source = Source.Umod,
                },
                new SyncData
                {
                    Oxide = "VIP",
                    Discord = "VIP",
                    Source = Source.Discord,
                }
            };
            return config;
        }

        private void OnServerInitialized()
        {
            if (DiscordCore == null)
            {
                PrintError("Missing plugin dependency DiscordCore: https://umod.org/plugins/discord-core");
                return;
            }

            OnDiscordCoreReady();
        }

        private void OnDiscordCoreReady()
        {
            if (!(DiscordCore?.Call<bool>("IsReady") ?? false))
            {
                return;
            }

            DiscordCore.Call("RegisterPluginForExtensionHooks", this);

            foreach (var data in _pluginConfig.SyncData.ToList())
            {
                bool remove = false;
                if (!permission.GroupExists(data.Oxide))
                {
                    PrintWarning($"Oxide group does not exist: '{data.Oxide}'. Please create the group or correct the name");
                    remove = true;
                }

                Role role = GetRole(data.Discord);
                if (role == null)
                {
                    PrintWarning($"Discord role name or id does not exist: '{data.Discord}'.\n" +
                                 "Please add the discord role or fix the role name/id.");
                    remove = true;
                }

                if (remove)
                {
                    _pluginConfig.SyncData.Remove(data);
                    continue;
                }

                _discordRoleLookup[role.id] = role.name;
                _discordRoleLookup[role.name] = role.id;
            }

            timer.In(5f, CheckAllPlayers);
        }

        private void CheckAllPlayers()
        {
            Debug(DebugEnum.Message, "Start checking all players");
            List<string> users = DiscordCore.Call<List<string>>("GetAllUsers");
            foreach (string user in users)
            {
                _processIds.Add(new PlayerSync(user));
            }
            
            if (_playerChecker == null)
            {
                _playerChecker = timer.Every(_pluginConfig.UpdateRate, ProcessNextStartupId);;
            }
        }

        private void ProcessNextStartupId()
        {
            if (_processIds.Count == 0)
            {
                _playerChecker?.Destroy();
                _playerChecker = null;
                return;
            }

            PlayerSync id = _processIds[0];
            _processIds.RemoveAt(0);

            Debug(DebugEnum.Info, $"Start processing: Player Id: {id.PlayerId} Discord Id: {id.DiscordId} Is Leaving: {id.IsLeaving}");
            
            IPlayer player = players.FindPlayerById(id.PlayerId);
            if (player != null)
            {
                HandleUserConnected(id.PlayerId, id.DiscordId, id.IsLeaving);
            }
        }
        #endregion

        #region Commands

        [Command("dcr.forcecheck")]
        private void HandleCommand(IPlayer player, string cmd, string[] args)
        {
            Debug(DebugEnum.Message, "Begin checking all players");
            CheckAllPlayers();
        }

        #endregion

        #region Oxide Hooks

        private void OnUserConnected(IPlayer player)
        {
            ProcessChange(player.Id);
        }

        private void OnUserGroupAdded(string id, string groupName)
        {
            ProcessChange(id);
        }

        private void OnUserGroupRemoved(string id, string groupName)
        {
            ProcessChange(id);
        }
        
        private void OnDiscordCoreJoin(IPlayer player)
        {
            ProcessChange(player.Id);
        }
        
        private void OnDiscordCoreLeave(IPlayer player, string discordId)
        {
            ProcessChange(player.Id, discordId, true);
        }

        private void Discord_MemberAdded(GuildMember member)
        {
            HandleDiscordChange(member.user.id, false);
        }

        private void Discord_MemberRemoved(GuildMember member)
        {
            HandleDiscordChange(member.user.id, true);
        }
        
        private void Discord_GuildMemberUpdate(GuildMemberUpdate update, GuildMember oldMember)
        {
            HandleDiscordChange(oldMember.user.id, false);
        }
        
        public void HandleDiscordChange(string discordId, bool isLeaving)
        {
            string playerId = GetPlayerId(discordId);
            if (string.IsNullOrEmpty(playerId))
            {
                return;
            }
            
            ProcessChange(playerId, discordId, isLeaving);
        }
        
        private void ProcessChange(string playerId, string discordId = null, bool isLeaving = false)
        {
            PlayerSync sync = _processIds.FirstOrDefault(p => p.PlayerId == playerId);
            if (sync != null)
            {
                _processIds.Remove(sync);
                _processIds.Insert(0, sync);
            }
            else
            {
                _processIds.Insert(0, new PlayerSync(playerId, discordId, isLeaving));
            }

            if (_playerChecker == null)
            {
                _playerChecker = timer.Every(_pluginConfig.UpdateRate, ProcessNextStartupId);
            }
        }
        #endregion

        #region Role Handling
        public void HandleUserConnected(string playerId, string discordId, bool isLeaving)
        {
            try
            {
                UnsubscribeAll();
                if (string.IsNullOrEmpty(discordId))
                {
                    discordId = GetDiscordId(playerId);
                    if (string.IsNullOrEmpty(discordId))
                    {
                        return;
                    }
                }

                IPlayer player = covalence.Players.FindPlayerById(playerId);
                
                Debug(DebugEnum.Info, $"Checking player {player?.Name} ({playerId}) Discord: {discordId}");
                HandleOxideGroups(playerId, discordId, isLeaving);
                HandleDiscordRoles(playerId, isLeaving);
                HandleUserNick(playerId, isLeaving);
            }
            finally
            {
                SubscribeAll();
            }
        }

        public void HandleOxideGroups(string playerId, string discordId, bool isLeaving)
        {
            if (DiscordCore == null)
            {
                PrintError("Missing plugin dependency DiscordCore: https://umod.org/plugins/discord-core");
                return;
            }
            
            Debug(DebugEnum.Info,$"Processing Oxide for {players.FindPlayerById(playerId)?.Name}({playerId}) Discord ID: {discordId} Is Leaving {isLeaving}");

            IPlayer player = covalence.Players.FindPlayerById(playerId);
            foreach (IGrouping<string, SyncData> data in _pluginConfig.SyncData.Where(s => s.Source == Source.Umod).GroupBy(s => s.Discord))
            {
                bool isInGroup = !isLeaving && data.Any(d => permission.UserHasGroup(playerId, d.Oxide)) ;
                bool isInDiscord = DiscordCore.Call<bool>("UserHasRole", playerId, data.Key);
                if (isInDiscord == isInGroup)
                {
                    Debug(DebugEnum.Info,$"{player?.Name} skipping Umod Sync: [{string.Join(", ", data.Select(d => d.Oxide).ToArray())}] -> {GetRoleDisplayName(data.Key)} {(isInGroup ? "Already Synced" : "Not in group")}");
                    continue;
                }

                string hook = isInGroup ? "AddRoleToUser" : "RemoveRoleFromUser";
                DiscordCore.Call(hook, discordId, data.Key);
                
                if (isInGroup)
                {
                    Debug(DebugEnum.Message,$"Adding player {player?.Name}({playerId}) to discord role {GetRoleDisplayName(data.Key)}");
                }
                else
                {
                    Debug(DebugEnum.Message,$"Removing player {player?.Name}({playerId}) from discord role {GetRoleDisplayName(data.Key)}");
                }
            }
        }
        
        public void HandleDiscordRoles(string playerId, bool isLeaving)
        {
            if (DiscordCore == null)
            {
                PrintError("Missing plugin dependency DiscordCore: https://umod.org/plugins/discord-core");
                return;
            }

            if (string.IsNullOrEmpty(playerId))
            {
                return;
            }
            
            Debug(DebugEnum.Info,$"Processing Discord for {players.FindPlayerById(playerId)?.Name} ({playerId}) Is Leaving {isLeaving}");
            
            IPlayer player = covalence.Players.FindPlayerById(playerId);
            foreach (IGrouping<string, SyncData> data in _pluginConfig.SyncData.Where(s => s.Source == Source.Discord).GroupBy(s => s.Oxide))
            {
                bool isInGroup = permission.UserHasGroup(playerId, data.Key);
                bool isInDiscord = false;
                if (!isLeaving)
                {
                    foreach (SyncData syncData in data)
                    {
                        if (DiscordUserHasRole(playerId, syncData.Discord))
                        {
                            isInDiscord = true;
                            break;
                        }
                    }
                }

                if (isInDiscord == isInGroup)
                {
                    Debug(DebugEnum.Info,$"{player?.Name} skipping Discord Sync: [{string.Join(", ", data.Select(d => GetRoleDisplayName(d.Discord)).ToArray())}] -> {data.Key} {(isInDiscord ? "Already Synced" : "Doesn't have role")}");
                    continue;
                }
                
                if (isInDiscord)
                {
                    Debug(DebugEnum.Message,$"Adding player {player?.Name}({playerId}) to oxide group {data.Key}");
                    permission.AddUserGroup(playerId, data.Key);
                }
                else
                {
                    Debug(DebugEnum.Message,$"Removing player {player?.Name}({playerId}) from oxide group {data.Key}");
                    permission.RemoveUserGroup(playerId, data.Key);
                }
            }
        }
        
        public void HandleUserNick(string playerId, bool isLeaving)
        {
            IPlayer player = covalence.Players.FindPlayerById(playerId);
            if (player == null)
            {
                Debug(DebugEnum.Warning, $"Failed to sync player id '{playerId}' as they don't have an IPlayer");
                return;
            }
            
            if (!_pluginConfig.SyncNicknames || isLeaving)
            {
                return;
            }
            
            Debug(DebugEnum.Info, $"Setting {player.Name} as their discord nickname");
            
            UpdateUserNick(player.Id, player.Name);
        }
        #endregion

        #region Subscription Handling
        public void UnsubscribeAll()
        {
            Unsubscribe(nameof(OnUserGroupAdded));
            Unsubscribe(nameof(OnUserGroupRemoved));
            Unsubscribe(nameof(Discord_GuildMemberUpdate));
        }

        public void SubscribeAll()
        {
            Subscribe(nameof(OnUserGroupAdded));
            Subscribe(nameof(OnUserGroupRemoved));
            Subscribe(nameof(Discord_GuildMemberUpdate));
        }
        #endregion

        #region Helper Methods
        private string GetDiscordId(string playerId)
        {
            return DiscordCore?.Call<string>("GetDiscordIdFromSteamId", playerId);
        }

        private string GetPlayerId(string discordId)
        {
            return DiscordCore?.Call<string>("GetSteamIdFromDiscordId", discordId);
        }

        private Role GetRole(string role)
        {
            return DiscordCore.Call<Role>("GetRole", role);
        }

        public bool DiscordUserHasRole(string playerId, string role)
        {
            return DiscordCore.Call<bool>("UserHasRole", playerId, role);
        }

        private void UpdateUserNick(string id, string newNick)
        {
            DiscordCore.Call("UpdateUserNick", id, newNick);
        }

        private string GetRoleDisplayName(string role)
        {
            ulong val;
            if (ulong.TryParse(role, out val))
            {
                return $"{_discordRoleLookup[role]}({role})";
            }
            
            return $"{role}({_discordRoleLookup[role]})";
        }
        
        private void Debug(DebugEnum level, string message)
        {
            if (level <= _pluginConfig.DebugLevel)
            {
                Puts($"{level}: {message}");
            }
        }
        #endregion

        #region Classes

        private class PluginConfig
        {
            [DefaultValue(false)]
            [JsonProperty(PropertyName = "Sync Nicknames")]
            public bool SyncNicknames { get; set; }
            
            [DefaultValue(2f)]
            [JsonProperty(PropertyName = "Update Rate (Seconds)")]
            public float UpdateRate { get; set; }
            
            [JsonProperty(PropertyName = "Sync Data")]
            public List<SyncData> SyncData { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            [DefaultValue(DebugEnum.None)]
            [JsonProperty(PropertyName = "Debug Level (None, Error, Warning, Info)")]
            public DebugEnum DebugLevel { get; set; }
        }

        private class SyncData
        {
            [JsonProperty(PropertyName = "Oxide Group")]
            public string Oxide { get; set; }

            [JsonProperty(PropertyName = "Discord Role (Name or Id)")]
            public string Discord { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty(PropertyName = "Sync Source (Umod or Discord)")]
            public Source Source { get; set; }
        }

        private class PlayerSync
        {
            public string PlayerId;
            public string DiscordId;
            public bool IsLeaving;

            public PlayerSync(string playerId)
            {
                PlayerId = playerId;
            }
            
            public PlayerSync(string playerId, string discordId, bool isLeaving)
            {
                PlayerId = playerId;
                DiscordId = discordId;
                IsLeaving = isLeaving;
            }
        }
        #endregion
    }

    partial class DiscordCoreRoles
    {
        private TimerRoutine _activeRoutine;
        
        public class TimerRoutine : IDisposable
        {
            private IEnumerator _routine;
            private PluginTimers _timer;
            private Timer _currentTimer;
            private bool _stop;

            public TimerRoutine(PluginTimers timers, IEnumerator routine)
            {
                _routine = routine;
                _timer = timers;
                ProcessNext();
            }

            public void MoveNext()
            {
                if (_stop)
                {
                    return;
                }

                if (!_routine.MoveNext())
                {
                    Stop();
                    return;
                }
                
                ProcessNext();
            }

            private void ProcessNext()
            {
                if (_stop)
                {
                    return;
                }
                
                object current = _routine.Current;
                
                if (!(current is ITimerRoutine))
                {
                    NextTick();
                }
                else
                {
                    if (current is TimerWaitForSeconds)
                    {
                        TimerWaitForSeconds seconds = (TimerWaitForSeconds) current;
                        WaitForSeconds(seconds.Seconds);
                    }
                    else if (current is TimerWaitUntil)
                    {
                        TimerWaitUntil until = (TimerWaitUntil) current;
                        WaitUntil(until.Func);
                    }
                    else if (current is TimeWaitWhile)
                    {
                        TimeWaitWhile until = (TimeWaitWhile) current;
                        WaitWhile(until.Func);
                    }
                }
            }

            private void NextTick()
            {
                Interface.Oxide.NextTick(MoveNext);
            }

            private void WaitForSeconds(float seconds)
            {
                _currentTimer = _timer.In(seconds, MoveNext);
            }

            private void WaitUntil(Func<bool> wait)
            {
                if (_stop)
                {
                    return;
                }
                
                if (wait.Invoke())
                {
                    Interface.Oxide.NextTick(MoveNext);
                    return;
                }
                
                Interface.Oxide.NextTick(() => WaitUntil(wait));
            }
            
            private void WaitWhile(Func<bool> wait)
            {
                if (_stop)
                {
                    return;
                }
                
                if (!wait.Invoke())
                {
                    Interface.Oxide.NextTick(MoveNext);
                    return;
                }
                
                Interface.Oxide.NextTick(() => WaitUntil(wait));
            }

            public void Stop()
            {
                _stop = true;
                _currentTimer?.Destroy();
                Dispose();
            }

            public void Dispose()
            {
                _routine = null;
                _timer = null;
            }
        }

        public interface ITimerRoutine
        {
            
        }

        public class TimerWaitForSeconds : ITimerRoutine
        {
            public readonly float Seconds;

            public TimerWaitForSeconds(float seconds)
            {
                Seconds = seconds;
            }
        }

        public class TimerWaitUntil : ITimerRoutine
        {
            public readonly Func<bool> Func;

            public TimerWaitUntil(Func<bool> func)
            {
                Func = func;
            }
        }
        
        public class TimeWaitWhile : ITimerRoutine
        {
            public readonly Func<bool> Func;

            public TimeWaitWhile(Func<bool> func)
            {
                Func = func;
            }
        }
    }
}