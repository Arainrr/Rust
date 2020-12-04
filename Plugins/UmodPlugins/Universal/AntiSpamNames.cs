﻿using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System;

namespace Oxide.Plugins
{
    [Info("Anti Spam Names", "MON@H", "1.2.3")]
    [Description("Replaces domain extensions or specific spam words in player's name. Also renames fake admins.")]

    class AntiSpamNames : CovalencePlugin
    {
        #region Initialization
        private const string PERMISSION_IMMUNE = "antispamnames.immune";

        private void OnServerInitialized()
        {
            permission.RegisterPermission(PERMISSION_IMMUNE, this);
            foreach (var player in players.Connected)
            {
                if (!permission.UserHasPermission(player.Id, PERMISSION_IMMUNE))
                {
                    HandleName(player);
                }
            }
        }

        #endregion Initialization

        #region Config

        private ConfigData configData;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Allow check name for spam regex list")]
            public bool UseRegexList = false;

            [JsonProperty(PropertyName = "Print all name changes to log")]
            public bool PrintEnabled = true;

            [JsonProperty(PropertyName = "Check admin names")]
            public bool CheckAdminNames = false;

            [JsonProperty(PropertyName = "Admin name blacklist", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> AdminNameBlacklist = new List<string>()
            {
                "Administrator",
                "Admin"
            };

            [JsonProperty(PropertyName = "Replace for admin")]
            public string ReplaceForAdmin = "Player";

            [JsonProperty(PropertyName = "Check spam names")]
            public bool CheckSpamNames = false;

            [JsonProperty(PropertyName = "Replace for spam")]
            public string ReplaceForSpam = "";

            [JsonProperty(PropertyName = "Replace if empty (whole name filtered)")]
            public string ReplaceForEmpty = "Good name";

            [JsonProperty(PropertyName = "Spam keyword blacklist", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> SpamKeywordBlacklist = new List<string>()
            {
                "#SPAMRUST",
                "#BESTRUST"
            };

            [JsonProperty(PropertyName = "Spam regex list", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> SpamRegexList = new List<string>()
            {
                "(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)",
                "(:\\d{3,5})",
                "([Ааa4][Ддd][Ммm][Ииi1][Ннn])",
                "(https|http|ftp|):\\/\\/",
                "((\\p{L}+|[0-9]+)+\\.)+(com|org|net|int|edu|gov|mil|ch|cn|co|de|eu|fr|in|nz|ru|tk|tr|uk|us)",
                "((\\p{L}+|[0-9]+)+\\.)+(ua|pro|io|dev|me|ml|tk|ml|ga|cf|gq|tf|money|pl|gg|net|info|cz|sk|nl)",
                "((\\p{L}+|[0-9]+)+\\.)+(store|shop)"
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                configData = Config.ReadObject<ConfigData>();
                if (configData == null)
                    LoadDefaultConfig();
            }
            catch
            {
                PrintError("The configuration file is corrupted");
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file");
            configData = new ConfigData();
        }

        protected override void SaveConfig() => Config.WriteObject(configData);

        #endregion Configuration

        #region Hooks

        private void OnUserConnected(IPlayer player)
        {
            if (!permission.UserHasPermission(player.Id, PERMISSION_IMMUNE))
            {
                HandleName(player);
            }
        }

        private void OnUserNameUpdated(string id, string oldName, string newName)
        {
            if (oldName == newName) return;

            IPlayer player = players.FindPlayerById(id);
            if (!permission.UserHasPermission(player.Id, PERMISSION_IMMUNE))
            {
                HandleName(player);
            }
        }

        private string GetClearName(IPlayer player)
        {
            string newName = player.Name;
            if (configData.UseRegexList)
            {
                newName = GetClearText(newName, configData.SpamRegexList, configData.ReplaceForSpam);    
            }

            if (configData.CheckSpamNames)
            {
                foreach (string spamKeyword in configData.SpamKeywordBlacklist)
                {
                    if (player.Name.ToLower().Contains(spamKeyword.ToLower()))
                    {
                        Regex regex = new Regex(spamKeyword, RegexOptions.IgnoreCase);
                        newName = regex.Replace(newName, configData.ReplaceForSpam);
                    }
                }
            }

            if (configData.CheckAdminNames && !player.IsAdmin)
            {
                foreach (string adminName in configData.AdminNameBlacklist)
                {
                    if (player.Name.ToLower().Contains(adminName.ToLower()))
                    {
                        Regex regex = new Regex(adminName, RegexOptions.IgnoreCase);
                        newName = regex.Replace(newName, configData.ReplaceForAdmin);
                    }
                }
            }

            return newName;
        }

        private string GetClearText(string text, List<string> regexList, string replacement = null)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            foreach (var SpamRegex in regexList)
            {
                Regex rgx = new Regex(SpamRegex, RegexOptions.IgnoreCase);
                text = rgx.Replace(text, string.IsNullOrEmpty(replacement) ? string.Empty : replacement);
            }

            return text;
        }

        private void HandleName(IPlayer player)
        {
            if (player == null || !player.IsConnected) return;
            string newName = GetClearName(player);

            if (!player.Name.Equals(newName, StringComparison.InvariantCultureIgnoreCase))
            {
                if (newName.Trim().Count() == 0)
                {
                    newName = string.IsNullOrEmpty(configData.ReplaceForEmpty) ? "Good Name" : configData.ReplaceForEmpty;
                }
                if (configData.PrintEnabled)
                {
                    PrintWarning("Renaming a player (" + player.Id + "): oldname=" + player.Name + ", newname=" + newName);
                }
                player.Rename(newName);
            }
        }

        #endregion
    }
}