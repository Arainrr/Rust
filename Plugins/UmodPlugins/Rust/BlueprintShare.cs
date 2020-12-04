﻿using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Blueprint Share", "c_creep", "1.2.0")]
    [Description("Allows players to share researched blueprints with their friends, clan or team")]

    class BlueprintShare : RustPlugin
    {
        #region Fields

        [PluginReference] private Plugin Clans, ClansReborn, Friends;

        private StoredData storedData;

        private bool clansEnabled = true, friendsEnabled = true, teamsEnabled = true, recycleEnabled = false;

        #endregion

        #region Oxide Hooks

        private void Init()
        {
            LoadData();

            LoadDefaultConfig();

            permission.RegisterPermission("blueprintshare.toggle", this);
            permission.RegisterPermission("blueprintshare.share", this);
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            var playerUID = player.UserIDString;

            if (!storedData.playerData.ContainsKey(playerUID))
            {
                storedData.playerData.Add(playerUID, new StoredData.PlayerInfo
                {
                    enabled = true,
                    blueprints = new List<string>()
                });

                SaveData();
            }
        }

        private void OnItemAction(Item item, string action, BasePlayer player)
        {
            if (player != null && action == "study" && item.IsBlueprint() && (InClan(player.userID) || HasFriends(player.userID) || InTeam(player.userID)))
            {
                var itemShortName = item.blueprintTargetDef.shortname;

                if (string.IsNullOrEmpty(itemShortName)) return;

                if (CanShareBlueprint(player, itemShortName))
                {
                    // Hard coded patch for how new triangle blueprints work

                    if (itemShortName == "floor.grill" || itemShortName == "floor.triangle.grill" || itemShortName == "floor.ladder.hatch" || itemShortName == "floor.triangle.ladder.hatch")
                    {
                        HandleAlternativeBlueprint(player, itemShortName);
                    }

                    item.Remove();

                    InsertBlueprint(player, itemShortName);

                    if (GetSharingEnabled(player.UserIDString))
                    {
                        ShareWithMultiplePlayers(player, itemShortName);
                    }
                }
                else
                {
                    if (recycleEnabled)
                    {
                        var playerUID = player.UserIDString;

                        player.ChatMessage(GetLangValue("Prefix", playerUID) + GetLangValue("Recycle", playerUID));
                    }
                    else
                    {
                        item.Remove();
                    }

                    return;
                }
            }
        }

        #endregion

        #region Data

        private class StoredData
        {
            public Dictionary<string, PlayerInfo> playerData = new Dictionary<string, PlayerInfo>();

            public class PlayerInfo
            {
                public bool enabled;

                public List<string> blueprints;
            }
        }

        private void LoadData()
        {
            try
            {
                storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch
            {
                ClearData();
            }
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, storedData);

        private void ClearData()
        {
            storedData = new StoredData();

            SaveData();
        }

        #endregion

        #region Config

        protected override void LoadDefaultConfig()
        {
            Config["ClansEnabled"] = clansEnabled = GetConfigValue("ClansEnabled", true);
            Config["FriendsEnabled"] = friendsEnabled = GetConfigValue("FriendsEnabled", true);
            Config["TeamsEnabled"] = teamsEnabled = GetConfigValue("TeamsEnabled", true);
            Config["RecycleBlueprints"] = recycleEnabled = GetConfigValue("RecycleBlueprints", false);

            SaveConfig();
        }

        private T GetConfigValue<T>(string name, T defaultValue)
        {
            return Config[name] == null ? defaultValue : (T)Convert.ChangeType(Config[name], typeof(T));
        }

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Prefix"] = "<color=#D85540>[Blueprint Share] </color>",
                ["ArgumentsError"] = "Error, incorrect arguments. Try /bs help.",
                ["Help"] = "<color=#D85540>Blueprint Share Help:</color>\n\n<color=#D85540>/bs toggle</color> - Toggles the sharing of blueprints.\n<color=#D85540>/bs share <player></color> - Shares your blueprints with other player.",
                ["ToggleOn"] = "You have enabled sharing blueprints.",
                ["ToggleOff"] = "You have disabled sharing blueprints.",
                ["NoPermission"] = "You don't have permission to use this command!",
                ["CannotShare"] = "You cannot share blueprints with this player because they aren't a friend or in the same clan or team!",
                ["NoTarget"] = "You didn't specifiy a player to share with!",
                ["TargetEqualsPlayer"] = "You cannot share blueprints with your self!",
                ["PlayerNotFound"] = "Couldn't find a player with that name!",
                ["MultiplePlayersFound"] = "Found multiple players with a similar name: {0}",
                ["ShareSuccess"] = "You shared {0} blueprints with {1}.",
                ["ShareFailure"] = "You don't have any new blueprints to share with {0}",
                ["ShareReceieve"] = "{0} has shared {1} blueprints with you.",
                ["Recycle"] = "You have kept the blueprint because no one learnt the blueprint."
            }, this);
        }

        private string GetLangValue(string key, string id = null, params object[] args)
        {
            var msg = lang.GetMessage(key, this, id);

            return args.Length > 0 ? string.Format(msg, args) : msg;
        }

        #endregion

        #region General Methods

        private bool UnlockBlueprint(BasePlayer player, string itemShortName)
        {
            if (player == null) return false;
            if (string.IsNullOrEmpty(itemShortName)) return false;

            var blueprintComponent = player.blueprints;

            if (blueprintComponent == null) return false;

            var itemDefinition = GetItemDefinition(itemShortName);

            if (itemDefinition == null) return false;

            if (blueprintComponent.HasUnlocked(itemDefinition)) return false;

            var soundEffect = new Effect("assets/prefabs/deployable/research table/effects/research-success.prefab", player.transform.position, Vector3.zero);

            if (soundEffect == null) return false;
            
            EffectNetwork.Send(soundEffect, player.net.connection);

            blueprintComponent.Unlock(itemDefinition);

            return true;
        }

        private void ShareWithMultiplePlayers(BasePlayer player, string itemShortName)
        {
            if (player == null || string.IsNullOrEmpty(itemShortName)) return;

            var playersToShareWith = SelectSharePlayers(player);

            foreach (var sharePlayer in playersToShareWith)
            {
                if (sharePlayer != null)
                {
                    if (UnlockBlueprint(sharePlayer, itemShortName))
                    {
                        InsertBlueprint(sharePlayer, itemShortName);
                    }
                }
            }
        }

        private void ShareWithSinglePlayer(BasePlayer sharer, BasePlayer player)
        {
            var sharerUID = sharer.UserIDString;

            var playerUID = player.UserIDString;

            List<string> itemShortNames = new List<string>();

            if (SameTeam(sharer, player) || SameClan(sharerUID, playerUID) || AreFriends(sharerUID, playerUID))
            {
                if (LoadBlueprints(sharerUID) != null)
                {
                    itemShortNames = LoadBlueprints(sharerUID);
                }

                if (itemShortNames.Count > 0)
                {
                    var learnedBlueprints = 0;

                    foreach (var itemShortName in itemShortNames)
                    {
                        if (player == null || string.IsNullOrEmpty(itemShortName)) return;

                        if (UnlockBlueprint(player, itemShortName))
                        {
                            learnedBlueprints++;
                        }
                    }

                    if (learnedBlueprints > 0)
                    {
                        sharer.ChatMessage(GetLangValue("Prefix", sharerUID) + GetLangValue("ShareSuccess", sharerUID, learnedBlueprints, player.displayName));

                        player.ChatMessage(GetLangValue("Prefix", playerUID) + GetLangValue("ShareReceieve", playerUID, sharer.displayName, learnedBlueprints));
                    }
                    else
                    {
                        sharer.ChatMessage(GetLangValue("Prefix", sharerUID) + GetLangValue("ShareFailure", sharerUID, player.displayName));
                    }
                }
                else
                {
                    sharer.ChatMessage(GetLangValue("Prefix", sharerUID) + GetLangValue("ShareFailure", sharerUID, player.displayName));
                }
            }
            else
            {
                sharer.ChatMessage(GetLangValue("Prefix", playerUID) + GetLangValue("CannotShare", playerUID));
            }
        }

        private List<BasePlayer> SelectSharePlayers(BasePlayer player)
        {
            var playersToShareWith = new List<BasePlayer>();

            var playerUID = player.userID;

            if (clansEnabled && (Clans != null || ClansReborn != null) && InClan(playerUID))
            {
                playersToShareWith.AddRange(GetClanMembers(playerUID));
            }

            if (friendsEnabled && Friends != null && HasFriends(playerUID))
            {
                playersToShareWith.AddRange(GetFriends(playerUID));
            }

            if (teamsEnabled && InTeam(playerUID))
            {
                playersToShareWith.AddRange(GetTeamMembers(playerUID));
            }

            return playersToShareWith;
        }

        private List<string> LoadBlueprints(string playerUID) => storedData.playerData.ContainsKey(playerUID) ? storedData.playerData[playerUID].blueprints : null;

        private void InsertBlueprint(BasePlayer player, string itemShortName)
        {
            if (player == null || string.IsNullOrEmpty(itemShortName)) return;

            var playerUID = player.UserIDString;

            if (storedData.playerData.ContainsKey(playerUID))
            {
                if (!storedData.playerData[playerUID].blueprints.Contains(itemShortName))
                {
                    storedData.playerData[playerUID].blueprints.Add(itemShortName);

                    SaveData();
                }
            }
        }

        private void HandleAlternativeBlueprint(BasePlayer sharer, string itemShortName)
        {
            if (string.IsNullOrEmpty(itemShortName)) return;

            string alternativeItemShortName = "";

            switch (itemShortName)
            {
                case "floor.grill": // Square floor grill so return triangle floor grill
                {
                    alternativeItemShortName = "floor.triangle.grill";
                    break;
                }
                case "floor.triangle.grill": // Triangle floor grill so return square floor grill
                {
                    alternativeItemShortName = "floor.grill";
                    break;
                }
                case "floor.ladder.hatch": // Square ladder hatch so return triangle ladder hatch
                {
                    alternativeItemShortName = "floor.triangle.ladder.hatch";
                    break;
                }
                case "floor.triangle.ladder.hatch": // Triangle ladder hatch so return square ladder hatch
                {
                    alternativeItemShortName = "floor.ladder.hatch";
                    break;
                }
            }

            if (string.IsNullOrEmpty(alternativeItemShortName)) return;

            InsertBlueprint(sharer, alternativeItemShortName);

            ShareWithMultiplePlayers(sharer, alternativeItemShortName);
        }

        private bool CanShareBlueprint(BasePlayer sharer, string itemShortName)
        {
            var players = SelectSharePlayers(sharer);

            var blueprintItem = GetItemDefinition(itemShortName);

            var counter = 0;

            foreach (var player in players)
            {
                if (player != null)
                {
                    if (!player.blueprints.HasUnlocked(blueprintItem))
                    {
                        counter++;
                    }
                }
            }

            return counter > 0;
        }

        #endregion

        #region Clan Methods

        private bool InClan(ulong playerUID)
        {
            if (ClansReborn == null && Clans == null) return false;

            var clanName = Clans?.Call<string>("GetClanOf", playerUID);

            return clanName != null;
        }

        private List<BasePlayer> GetClanMembers(ulong playerUID)
        {
            var membersList = new List<BasePlayer>();

            var clanName = Clans?.Call<string>("GetClanOf", playerUID);

            if (!string.IsNullOrEmpty(clanName))
            {
                var clan = Clans?.Call<JObject>("GetClan", clanName);

                if (clan != null && clan is JObject)
                {
                    var members = clan.GetValue("members");

                    if (members != null)
                    {
                        foreach (JToken member in members)
                        {
                            ulong clanMemberUID;

                            if (!ulong.TryParse(member.ToString(), out clanMemberUID)) continue;

                            BasePlayer clanMember = RustCore.FindPlayerById(clanMemberUID);

                            membersList.Add(clanMember);
                        }
                    }
                }
            }
            return membersList;
        }

        private bool SameClan(string sharerUID, string playerUID) => ClansReborn == null && Clans == null ? false : (bool)Clans?.Call<bool>("IsClanMember", sharerUID, playerUID);

        #endregion

        #region Friends Methods

        private bool HasFriends(ulong playerUID)
        {
            if (Friends == null) return false;

            var friendsList = Friends.Call<ulong[]>("GetFriends", playerUID);

            return friendsList != null && friendsList.Length != 0;
        }

        private List<BasePlayer> GetFriends(ulong playerUID)
        {
            var friendsList = new List<BasePlayer>();

            var friends = Friends.Call<ulong[]>("GetFriends", playerUID);

            foreach (var friendUID in friends)
            {
                var friend = RustCore.FindPlayerById(friendUID);

                friendsList.Add(friend);
            }

            return friendsList;
        }

        private bool AreFriends(string sharerUID, string playerUID) => Friends == null ? false : Friends.Call<bool>("AreFriends", sharerUID, playerUID);

        #endregion

        #region Team Methods

        private bool InTeam(ulong playerUID)
        {
            var player = RustCore.FindPlayerById(playerUID);

            return player.currentTeam != 0;
        }

        private List<BasePlayer> GetTeamMembers(ulong playerUID)
        {
            var membersList = new List<BasePlayer>();

            var player = RustCore.FindPlayerById(playerUID);

            var playersCurrentTeam = RelationshipManager.Instance.FindTeam(player.currentTeam);

            var teamMembers = playersCurrentTeam.members;

            foreach (var teamMemberUID in teamMembers)
            {
                var teamMember = RustCore.FindPlayerById(teamMemberUID);

                membersList.Add(teamMember);
            }

            return membersList;
        }

        private bool SameTeam(BasePlayer sharer, BasePlayer player) => sharer.currentTeam == player.currentTeam;

        #endregion

        #region Chat Commands

        [ChatCommand("bs")]
        private void ToggleCommand(BasePlayer player, string command, string[] args)
        {
            var playerUID = player.UserIDString;

            if (args.Length < 1)
            {
                player.ChatMessage(GetLangValue("Prefix", playerUID) + GetLangValue("Help", playerUID));

                return;
            }

            switch (args[0].ToLower())
            {
                case "help":
                {
                    player.ChatMessage(GetLangValue("Help", playerUID));

                    break;
                }
                case "toggle":
                {
                    if (permission.UserHasPermission(playerUID, "blueprintshare.toggle"))
                    {
                        player.ChatMessage(GetLangValue("Prefix", playerUID) + GetLangValue(GetSharingEnabled(playerUID) ? "ToggleOff" : "ToggleOn", playerUID));

                        if (storedData.playerData.ContainsKey(playerUID))
                        {
                            storedData.playerData[playerUID].enabled = !storedData.playerData[playerUID].enabled;

                            SaveData();
                        }
                    }
                    else
                    {
                        player.ChatMessage(GetLangValue("Prefix", playerUID) + GetLangValue("NoPermission", playerUID));
                    }

                    break;
                }
                case "share":
                {
                    if (permission.UserHasPermission(playerUID, "blueprintshare.share"))
                    {
                        if (args.Length == 2)
                        {
                            var target = FindPlayer(args[1], player, playerUID);

                            if (target == null) return;

                            if (target == player)
                            {
                                player.ChatMessage(GetLangValue("Prefix", playerUID) + GetLangValue("TargetEqualsPlayer", playerUID));

                                return;
                            }

                            ShareWithSinglePlayer(player, target);
                        }
                        else
                        {
                            player.ChatMessage(GetLangValue("Prefix", playerUID) + GetLangValue("NoTarget", playerUID));

                            return;
                        }
                    }
                    else
                    {
                        player.ChatMessage(GetLangValue("Prefix", playerUID) + GetLangValue("NoPermission", playerUID));
                    }

                    break;
                }
                default:
                {
                    player.ChatMessage(GetLangValue("Prefix", playerUID) + GetLangValue("ArgumentsError", playerUID));

                    break;
                }
            }
        }

        #endregion

        #region Utility Methods

        private BasePlayer FindPlayer(string playerName, BasePlayer player, string playerUID)
        {
            var targets = FindPlayers(playerName);

            if (targets.Count <= 0)
            {
                player.ChatMessage(GetLangValue("Prefix", playerUID) + GetLangValue("PlayerNotFound", playerUID));

                return null;
            }

            if (targets.Count > 1)
            {
                player.ChatMessage(GetLangValue("Prefix", playerUID) + GetLangValue("MultiplePlayersFound", playerUID));

                return null;
            }

            return targets.First();
        }

        private List<BasePlayer> FindPlayers(string playerName)
        {
            if (string.IsNullOrEmpty(playerName)) return null;

            return BasePlayer.allPlayerList.Where(p => p && p.UserIDString == playerName || p.displayName.Contains(playerName, CompareOptions.OrdinalIgnoreCase)).ToList();
        }

        private ItemDefinition GetItemDefinition(string itemShortName)
        {
            if (string.IsNullOrEmpty(itemShortName)) return null;

            var itemDefinition = ItemManager.FindItemDefinition(itemShortName.ToLower());

            return itemDefinition;
        }

        #endregion

        #region API

        private bool GetSharingEnabled(string playerUID) => storedData.playerData.ContainsKey(playerUID) ? storedData.playerData[playerUID].enabled : true;

        #endregion
    }
}