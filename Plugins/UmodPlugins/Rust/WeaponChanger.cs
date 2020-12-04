using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Weapon Changer", "Orange", "1.0.1")]
    [Description("Change weapon stats on your server")]
    public class WeaponChanger : RustPlugin
    {
        #region Vars

        private Dictionary<string, WeaponEntry> weapons = new Dictionary<string, WeaponEntry>();

        #endregion
        
        #region Oxide Hooks

        private void OnServerInitialized()
        {
            if (config.weapons.Count == 0)
            {
                GetWeapons();
            }

            foreach (var value in config.weapons)
            {
                weapons.Add(value.shortname, value);
                permission.RegisterPermission(value.config.clip.permission, this);
            }
        }

        private void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            CheckItem(item, task.owner?.UserIDString ?? "0");
        }
        
        private void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            CheckItem(item, container.playerOwner?.UserIDString ?? "0");
        }
        
        private void OnReloadWeapon(BasePlayer player, BaseProjectile projectile)
        {
            CheckItem(projectile.GetItem(), player.UserIDString);
        }

        #endregion

        #region Core

        private void GetWeapons()
        {
            foreach (var item in ItemManager.itemList)
            {
                if (item.category != ItemCategory.Weapon)
                {
                    continue;
                }

                var weapon = item.GetComponent<ItemModEntity>()?.entityPrefab?.Get()?.GetComponent<BaseProjectile>();
                if (weapon == null)
                {
                    continue;
                }

                var size = weapon.primaryMagazine.definition.builtInSize;

                config.weapons.Add(new WeaponEntry
                {
                    shortname = item.shortname,
                    enabled = false,
                    config = new WeaponConfig
                    {
                        clip = new ClipSettings
                        {
                            size = size,
                            permission = $"weaponchanger.clip.{item.shortname}",
                            permSize = size + 10
                        }
                    }
                });
            }
                
            SaveConfig();
        }

        private void CheckItem(Item item, string playerID)
        {
            var weapon = item?.GetHeldEntity()?.GetComponent<BaseProjectile>();
            if (weapon == null) {return;}
            var name = item.info.shortname;
            var data = (WeaponEntry) null;
            
            if (weapons.TryGetValue(name, out data) == false)
            {
                return;
            }

            if (data.enabled == false)
            {
                return;
            }
            
            ChangeClip(weapon, data.config.clip, playerID);
            weapon.SendNetworkUpdate();
        }

        private void ChangeClip(BaseProjectile weapon, ClipSettings settings, string playerID)
        {
            var size = permission.UserHasPermission(playerID, settings.permission) ? settings.permSize : settings.size;
            weapon.primaryMagazine.capacity = size;
        }

        #endregion
        
        #region Configuration 1.1.0
        
        private static ConfigData config;
        
        private class ConfigData
        {
            [JsonProperty(PropertyName = "List")]
            public List<WeaponEntry> weapons = new List<WeaponEntry>();
        }
        
        private ConfigData GetDefaultConfig()
        {
            return new ConfigData 
            {
                weapons = new List<WeaponEntry>()
            };
        }
        
        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigData>();

                if (config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                PrintError("Configuration file is corrupt! Unloading plugin...");
                return;
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
        
        #endregion

        #region Classes

        private class WeaponEntry
        {
            [JsonProperty(PropertyName = "Shortname")]
            public string shortname;
            
            [JsonProperty(PropertyName = "Enabled")]
            public bool enabled;
            
            [JsonProperty(PropertyName = "Settings")]
            public WeaponConfig config;
        }
        
        private class WeaponConfig
        {
            [JsonProperty(PropertyName = "Magazine")]
            public ClipSettings clip;
        }

        private class ClipSettings
        {
            [JsonProperty(PropertyName = "Size")]
            public int size;

            [JsonProperty(PropertyName = "Permission")]
            public string permission;

            [JsonProperty(PropertyName = "Size with permission")]
            public int permSize;
        }

        #endregion
    }
}