﻿using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Item History", "birthdates", "1.1.0")]
    [Description("Keep history of an item")]
    public class ItemHistory : RustPlugin
    {
        #region Variables
        private const string Permission = "ItemHistory.use";
        #endregion

        #region Hooks
        private void Init() => permission.RegisterPermission(Permission, this);

        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (item == null) return;
            if (_config.blacklist?.Contains(item.info.shortname) == true) return;
            var player = item.GetOwnerPlayer() ?? container.GetOwnerPlayer();
            if (player == null) return;
            if (!permission.UserHasPermission(player.UserIDString, Permission) && !player.IsAdmin)
            {
                return;
            }

            if (!string.IsNullOrEmpty(item.name)) return;
            if (!container.Equals(player.inventory.containerMain) &&
                !container.Equals(player.inventory.containerBelt) &&
                !container.Equals(player.inventory.containerWear)) return;
            item.name = player.displayName + "'s " + item.info.displayName.english;

        }

        private ConfigFile _config;

        public class ConfigFile
        {
            [JsonProperty("Blacklisted Items (Won't get any history)")]
            public List<string> blacklist;
            public static ConfigFile DefaultConfig()
            {
                return new ConfigFile()
                {
                    blacklist = new List<string>
                    {
                        "shotgun.spas12"
                    }
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<ConfigFile>();
            if (_config == null)
            {
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig()
        {
            _config = ConfigFile.DefaultConfig();
            PrintWarning("Default configuration has been loaded.");
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        #endregion
    }
}