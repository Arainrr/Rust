﻿using CodeHatch.Blocks.Networking.Events;
using CodeHatch.Common;
using CodeHatch.Engine.Modules.SocialSystem;
using CodeHatch.Engine.Networking;
using CodeHatch.Thrones.SocialSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;

namespace Oxide.Plugins
{
    [Info("Detect Cheats", "D-Kay & Troll Knight", "2.0.1")]
    [Description("Monitors players attempting to use admin permissions or cheats")]
    public class DetectCheats : ReignOfKingsPlugin
    {
        #region Variables

        #region Fields

        private Configuration _config;

        #endregion

        #region Properties

        private new Configuration Config
        {
            get { return _config ?? (_config = new Configuration(base.Config)); }
        }

        private readonly List<string> _permissions = new List<string>
        {
            "Admin",
        };

        #endregion

        #region Classes

        public class Configuration
        {
            public static Configuration Instance { get; private set; }

            private DynamicConfigFile Config { get; }
            private bool ConfigHasChanged { get; set; }

            public bool AutoKick { get; set; }
            public bool AutoBan { get; set; }
            public bool BroadcastKick { get; set; }
            public bool BroadcastBan { get; set; }

            public int IntervalScan { get; set; }

            public Configuration(DynamicConfigFile config)
            {
                this.Config = config;
                this.ConfigHasChanged = false;
                Instance = this;
            }

            public void Load()
            {
                AutoKick = GetConfig(true, "Automation", "Kick", "Enabled");
                AutoBan = GetConfig(false, "Automation", "Bans", "Enabled");
                BroadcastKick = GetConfig(true, "Automation", "Kicks", "Broadcast");
                BroadcastBan = GetConfig(true, "Automation", "Bans", "Broadcast");

                IntervalScan = GetConfig(60, "Interval", "Seconds");

                if (!ConfigHasChanged) return;
                this.Save();
                ConfigHasChanged = false;
            }

            public void Save()
            {
                Config.Set("Automation", "Kicks", "Enabled", AutoKick);
                Config.Set("Automation", "Bans", "Enabled", AutoBan);
                Config.Set("Automation", "Kicks", "Broadcast", BroadcastKick);
                Config.Set("Automation", "Bans", "Broadcast", BroadcastBan);
                Config.Set("Interval", "Seconds", IntervalScan);

                Config.Save();
            }

            public void LoadDefault()
            {
                this.Load();
                this.Save();
            }

            private T GetConfig<T>(T defaultValue, params string[] path)
            {
                try
                {
                    var value = Config.Get(path);
                    if (value == null) throw new Exception("There is no return value with provided arguments.");
                    return Config.ConvertValue<T>(value);
                }
                catch (Exception)
                {
                    ConfigHasChanged = true;
                    return defaultValue;
                }
            }
        }

        #endregion

        #endregion

        #region Save and Load Data

        private void Init()
        {
            Config.Load();
            RegisterPermissions();

            if (Config.IntervalScan > 0) timer.In(Config.IntervalScan, Scan);
        }

        protected override void LoadDefaultConfig()
        {
            Config.LoadDefault();
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "No permission", "You do not have permission to use this command." },
                { "Invalid args", "It looks like you made a mistake somewhere. Use /cheats help for a list of available commands" },
                { "Kicked", "You were detected using cheats. Cheats ruin the game!" },
                { "Banned", "You were detected using cheats. Cheats ruin the game!" },

                { "Help title", "[0000FF]Detect Cheats Commands[-]" },
                { "Help scan", "[00FF00]/cheats scan[-] - Manually scan all online players for cheats or admin permissions they should not have." },
            }, this);
        }

        #endregion

        #region Commands

        [ChatCommand("Cheats")]
        private void CmdCheats(Player player, string cmd, string[] args)
        {
            if (!CheckPermission(player, "Admin")) return;
            if (!CheckArgs(player, args)) return;

            switch (args.First().ToLower())
            {
                case "scan":
                    Scan();
                    break;
                case "help":
                    SendHelpText(player);
                    break;
                default:
                    SendError(player, "Invalid args");
                    break;
            }
        }

        #endregion

        #region Command Functions

        private void Scan()
        {
            foreach (var player in Server.AllPlayers)
            {
                var isDetected = false;
                var message = string.Empty;

                if (HasPermission(player, "admin")) continue;

                if (player.HasGodMode())
                {
                    message += $"{player.Name} was detected playing in Godmode.";
                    isDetected = true;
                }

                if (player.IsShowingNameTags())
                {
                    if (isDetected) message += "\n";
                    message += $"{player.Name} has been detected playing with ESP.";
                    isDetected = true;
                }

                var permission = "codehatch.command.admin.kick";
                if (player.HasPermission(permission))
                {
                    if (isDetected) message += "\n";
                    message += $"{player.Name} has been detected playing with admin permission " + permission + ".";
                    isDetected = true;
                }

                permission = "codehatch.command.admin.ban";
                if (player.HasPermission(permission))
                {
                    if (isDetected) message += "\n";
                    message += $"{player.Name} has been detected playing with admin permission " + permission + ".";
                    isDetected = true;
                }

                permission = "rok.command.teleport.coord";
                if (player.HasPermission(permission))
                {
                    if (isDetected) message += "\n";
                    message += $"{player.Name} has been detected playing with teleport permissions " + permission + ".";
                    isDetected = true;
                }

                permission = "rok.command.teleport.user";
                if (player.HasPermission(permission))
                {
                    if (isDetected) message += "\n";
                    message += $"{player.Name} has been detected playing with teleport permissions " + permission + ".";
                    isDetected = true;
                }

                permission = "rok.command.admin.fly";
                if (player.HasPermission(permission))
                {
                    if (isDetected) message += "\n";
                    message += $"{player.Name} has been detected playing with fly permissions.";
                    isDetected = true;
                }

                permission = "rok.command.admin.videofly";
                if (player.HasPermission(permission))
                {
                    if (isDetected) message += "\n";
                    message += $"{player.Name} has been detected playing with fly permissions.";
                    isDetected = true;
                }

                permission = "rok.command.items.give";
                if (player.HasPermission(permission))
                {
                    if (isDetected) message += "\n";
                    message = $"{player.Name} has been detected playing with spawn item permissions.";
                    isDetected = true;
                }

                if (isDetected)
                {
                    HandleDetection(player, message);
                }
            }
        }

        #endregion

        #region System Functions

        private void HandleDetection(Player player, string reason)
        {
            if (Config.AutoBan)
            {
                this.BanPlayer(player, reason);
            }
            else if (Config.AutoKick)
            {
                this.KickPlayer(player, reason);
            }
        }

        private void BanPlayer(Player player, string reason)
        {
            Log(reason);
            Server.Ban(player.Id, player.Name, player.Connection.IpAddress, -1, GetMessage("Banned", player), Config.BroadcastBan);
        }

        private void KickPlayer(Player player, string reason)
        {
            Log(reason);
            Server.Kick(player, GetMessage("Kicked", player), Config.BroadcastKick);
        }

        #endregion

        #region Hooks

        private void OnCubeTakeDamage(CubeDamageEvent damageEvent)
        {
            #region Null Checks
            if (damageEvent?.Damage?.Damager == null) return;
            if (damageEvent.Damage.Amount <= 1500) return;
            if (damageEvent.Damage.DamageSource == null) return;
            if (damageEvent.Damage.DamageSource.Owner.IsServer) return;
            #endregion

            var player = damageEvent.Damage.DamageSource.Owner;
            if (HasPermission(player, "Admin")) return;

            var crestScheme = SocialAPI.Get<CrestScheme>();
            var position = damageEvent.Grid.LocalToWorldCoordinate(damageEvent.Position);
            var crest = crestScheme.GetCrestAt(position);
            if (crest == null) return;

            var damageSource = damageEvent.Damage.Damager.name;
            var message = $"{player.Name} attacked a crested block at position [{position}] with a {damageSource} causing {damageEvent.Damage.Amount} damage.";
            HandleDetection(player, message);
        }

        private void OnPlayerCommand(Player player, string command, string[] args)
        {
            if (HasPermission(player, "Admin")) return;

            command = command.ToLower();
            var isDetected = command == "give" ||
                             command == "giveall" ||
                             command == "fly" ||
                             command == "heal" ||
                             command == "tp" ||
                             command == "godmode" ||
                             command == "killall" ||
                             command == "killbyblueprint" ||
                             command == "kick" ||
                             command == "ban" ||
                             command == "pos";

            if (!isDetected) return;
            KickPlayer(player, $"{player.Name} was detected trying to use admin command(s).");
        }

        private void SendHelpText(Player player)
        {
            if (!HasPermission(player, "Admin")) return;
            PrintToChat(player, "[0000FF]DetectCheats Commands[FFFFFF]");
            PrintToChat(player, "[00FF00]/phelp[FFFFFF] - Shows the available commands for this plugin");
        }

        #endregion

        #region Utility

        private void RegisterPermissions()
        {
            _permissions.Foreach(p => permission.RegisterPermission($"{Name}.{p}", this));
        }
        private bool CheckPermission(Player player, string permission)
        {
            if (HasPermission(player, permission)) return true;
            SendError(player, "No permission");
            return false;
        }
        private bool HasPermission(Player player, string permission)
        {
            return player.HasPermission($"{Name}.{permission}");
        }
        private bool CheckArgs(Player player, IEnumerable<string> args, int count = 1)
        {
            if (!args.Any())
            {
                SendError(player, "Invalid args");
                return false;
            }
            if (count <= 1) return true;

            if (args.Count() >= count) return true;
            SendError(player, "Invalid args");
            return false;
        }

        private void SendError(Player player, string key, params object[] obj) => player?.SendError(GetMessage(key, player, obj));

        public string GetMessage(string key, Player player, params object[] obj)
        {
            return GetMessage(key, player, false, obj);
        }
        public string GetMessage(string key, Player player, bool hasTitle, params object[] args)
        {
            var title = hasTitle ? lang.GetMessage("Message prefix", this, player?.Id.ToString()) : "";
            return string.Format(title + lang.GetMessage(key, this, player?.Id.ToString()), args);
        }

        private void Log(string msg)
        {
            using (var stream = new FileStream(Path.Combine(Interface.Oxide.LogDirectory, $"CheatDetection {DateTime.Now:yyyy-MM-dd}.txt"), FileMode.OpenOrCreate))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine($"[{DateTime.Now:h:mm:ss tt}] {msg}");
            }
        }

        #endregion
    }
}
