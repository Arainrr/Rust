﻿using Newtonsoft.Json;
using Oxide.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Admin Auto Teams", "Dana", "0.1.6")]
    [Description("Puts admins in teams.")]
    public class AdminAutoTeams : RustPlugin
    {
        private PluginConfig _pluginConfig;
        public const string PermissionUse = "adminautoteams.use";

        #region Hooks

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Loading Default Config");
        }
        protected override void LoadConfig()
        {
            var configPath = $"{Manager.ConfigPath}/{Name}.json";
            var newConfig = new DynamicConfigFile(configPath);
            if (!newConfig.Exists())
            {
                LoadDefaultConfig();
                newConfig.Save();
            }
            try
            {
                newConfig.Load();
            }
            catch (Exception ex)
            {
                RaiseError("Failed to load config file (is the config file corrupt?) (" + ex.Message + ")");
                return;
            }

            newConfig.Settings.DefaultValueHandling = DefaultValueHandling.Populate;
            _pluginConfig = newConfig.ReadObject<PluginConfig>();
            if (_pluginConfig.Config == null)
            {
                _pluginConfig.Config = new AdminAutoTeamsConfig
                {
                    Teams = new Dictionary<string, AdminTeam>
                    {
                        {"Admins", new AdminTeam { ForceJoin = true, UserIds = new List<string>()}},
                        {"Moderators", new AdminTeam { ForceJoin = true, UserIds = new List<string>()}},
                        {"Chat Moderators", new AdminTeam { ForceJoin = true, UserIds = new List<string>()}},
                    }
                };
            }
            newConfig.WriteObject(_pluginConfig);
            PrintWarning("Config Loaded");
        }

        private void OnServerInitialized()
        {
            permission.RegisterPermission(PermissionUse, this);
            Refresh();
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { MessageManager.NoPermission, "You don't have permissions to use this command!" }
            }, this);
        }

        #endregion Hooks

        #region Commands

        [ConsoleCommand("at.refresh")]
        private void RefreshCommand(ConsoleSystem.Arg arg)
        {
            var player = arg?.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, PermissionUse))
            {
                player.ChatMessage(Lang(MessageManager.NoPermission, player.UserIDString));
                return;
            }

            Refresh();
        }
        #endregion Commands

        #region Helpers & Methods

        private void Refresh()
        {
            foreach (var adminTeam in _pluginConfig.Config.Teams)
            {
                if (adminTeam.Value.UserIds == null || adminTeam.Value.UserIds.Count == 0)
                    continue;
                var targets = new List<BasePlayer>();
                var leader = 0UL;
                foreach (var playerId in adminTeam.Value.UserIds)
                {
                    if (!playerId.IsSteamId())
                        continue;

                    var target = BasePlayer.FindAwakeOrSleeping(playerId);
                    if (target == null)
                        continue;

                    if (!adminTeam.Value.ForceJoin && target.currentTeam > 0)
                        continue;

                    if (!leader.IsSteamId())
                        leader = target.userID;

                    targets.Add(target);
                }

                var oldTeam = RelationshipManager.Instance.teams.FirstOrDefault(x => x.Value.teamName == $"_AT_{adminTeam.Key}_TA_");
                oldTeam.Value?.Disband();
                var team = RelationshipManager.Instance.CreateTeam();
                team.teamName = $"_AT_{adminTeam.Key}_TA_";
                foreach (var member in targets)
                {
                    member.ClearTeam();
                    RelationshipManager.Instance.playerToTeam.Remove(member.userID);
                    team.AddPlayer(member);
                }
                team.SetTeamLeader(leader);
            }
        }
        public string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        #endregion Helpers & Methods

        #region Classes

        public class MessageManager
        {
            public const string NoPermission = "NoPermission";
        }

        private class PluginConfig
        {
            public AdminAutoTeamsConfig Config { get; set; }
        }
        private class AdminAutoTeamsConfig
        {
            [JsonProperty(PropertyName = "Teams")]
            public Dictionary<string, AdminTeam> Teams { get; set; }
        }
        public class AdminTeam
        {
            [JsonProperty(PropertyName = "Force Join - Enabled")]
            public bool ForceJoin { get; set; }

            [JsonProperty(PropertyName = "Players Steam IDs")]
            public List<string> UserIds { get; set; }
        }
        #endregion Classes
    }
}