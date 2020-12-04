﻿using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Chainsaw Options", "Arainrr", "1.1.0")]
    [Description("Control player's chainsaws")]
    public class ChainsawOptions : RustPlugin
    {
        private void Init()
        {
            foreach (var permissionS in configData.permissionList)
            {
                if (!permission.PermissionExists(permissionS.permission, this))
                    permission.RegisterPermission(permissionS.permission, this);
            }
        }

        private void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (player == null || newItem == null || !player.userID.IsSteamId()) return;
            if (newItem.info.shortname == "chainsaw")
            {
                var chainsaw = newItem.GetHeldEntity() as Chainsaw;
                if (chainsaw == null) return;
                var permissionS = GetPermissionS(player);
                if (permissionS == null)
                {
                    chainsaw.engineStartChance = 0.4f;
                    chainsaw.maxAmmo = 50;
                    chainsaw.fuelPerSec = 1;
                    return;
                }
                chainsaw.engineStartChance = permissionS.chance;
                chainsaw.maxAmmo = permissionS.maxAmmo;
                chainsaw.fuelPerSec = permissionS.fuelPerSec;
            }
        }

        private ConfigData.PermissionS GetPermissionS(BasePlayer player)
        {
            ConfigData.PermissionS permissionS = null;
            int priority = 0;
            foreach (var p in configData.permissionList)
            {
                if (permission.UserHasPermission(player.UserIDString, p.permission) && p.priority >= priority)
                {
                    priority = p.priority;
                    permissionS = p;
                }
            }
            return permissionS;
        }

        #region ConfigurationFile

        private ConfigData configData;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Permission List", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<PermissionS> permissionList = new List<PermissionS>
            {
                new PermissionS
                {
                    permission = "chainsawoptions.use",
                    priority = 0,
                    chance = 0.5f,
                    maxAmmo = 70,
                    fuelPerSec = 1,
                },
                new PermissionS
                {
                    permission = "chainsawoptions.vip",
                    priority = 1,
                    chance = 1f,
                    maxAmmo = 100,
                    fuelPerSec = 0.5f,
                },
            };

            public class PermissionS
            {
                [JsonProperty(PropertyName = "Permission")]
                public string permission;

                [JsonProperty(PropertyName = "Priority")]
                public int priority;

                [JsonProperty(PropertyName = "Engine Start Chance")]
                public float chance;

                [JsonProperty(PropertyName = "Max Ammo")]
                public int maxAmmo;

                [JsonProperty(PropertyName = "Fuel Per Seconds")]
                public float fuelPerSec;
            }
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

        #endregion ConfigurationFile
    }
}