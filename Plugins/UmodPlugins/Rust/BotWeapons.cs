using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;

namespace Oxide.Plugins
{
    // Creation date: 24-08-2020
    // Last update date: UPDATE_DATE
    [Info("Bot Weapons", "Orange", "1.0.3")]
    [Description("Allows changing bot weapons")]
    public class BotWeapons : RustPlugin
    {
        #region Oxide Hooks

        private void Init()
        {
            Unsubscribe(nameof(OnEntitySpawned));
        }

        private void Unload()
        {
            config = null;
        }

        private void OnServerInitialized()
        {
            Subscribe(nameof(OnEntitySpawned));
            
            foreach (var entity in UnityEngine.Object.FindObjectsOfType<BasePlayer>())
            {
                OnEntitySpawned(entity);
            }
        }

        private void OnEntitySpawned(BasePlayer player)
        {
            CheckNPC(player);
        }

        #endregion

        #region Core

        private void CheckNPC(BasePlayer player)
        {
            if (player.userID.IsSteamId() == true)
            {
                return;
            }

            var entry = config.values.FirstOrDefault(x => x.prefabs.Contains(player.ShortPrefabName) || x.prefabs.Contains(player.PrefabName));
            if (entry == null)
            {
                return;
            }

            if (entry.removeOldWeapons == true)
            {
                var items = player.inventory.AllItems();
                foreach (var item in items)
                {
                    if (item.info.category == ItemCategory.Weapon)
                    {
                        item.GetHeldEntity().Kill();
                        item.DoRemove();
                    }
                }
            }

            var count = Core.Random.Range(entry.weaponsMin, entry.weaponsMax);
            var belt = player.inventory.containerBelt;
            belt.capacity += count;
            
            for (var i = 0; i < count; i++)
            {
                var shortname = entry.weaponsList.GetRandom();
                var item = ItemManager.CreateByName(shortname);
                if (item != null)
                {
                    var held = item.GetHeldEntity() as BaseProjectile;
                    if (held != null)
                    {
                        held.reloadTime = entry.reloadTime;
                        held.effectiveRange = entry.fireDistance;
                        held.damageScale = entry.damageScale;
                    }

                    if (item.MoveToContainer(belt) == false)
                    {
                        player.GiveItem(item);
                    }
                }
            }
            
            player.SendNetworkUpdate();
        }

        #endregion
         
        #region Configuration | 24.05.2020

        private static ConfigData config = new ConfigData();

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Values")]
            public NPCValue[] values =
            {
                new NPCValue(), 
                new NPCValue(), 
                new NPCValue(), 
            };
        }

        private class NPCValue
        {
            [JsonProperty(PropertyName = "Prefabs")]
            public string[] prefabs =
            {
                "heavyscientist",
                "scientistnpc",
                "other values here"
            };
            
            [JsonProperty(PropertyName = "Remove old weapons")]
            public bool removeOldWeapons = true;

            [JsonProperty(PropertyName = "Fire distance")]
            public float fireDistance = 50f;
            
            [JsonProperty(PropertyName = "Reload time")]
            public float reloadTime = 5f;
            
            [JsonProperty(PropertyName = "Damage scale")]
            public float damageScale = 0.5f;
            
            [JsonProperty(PropertyName = "Weapons minimal")]
            public int weaponsMin = 1;
            
            [JsonProperty(PropertyName = "Weapons maximal")]
            public int weaponsMax = 3;
            
            [JsonProperty(PropertyName = "Weapon items list")]
            public string[] weaponsList =
            {
                "rifle.ak",
                "rifle.bolt",
                "bow.compound",
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
                for (var i = 0; i < 3; i++)
                {
                    PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                }
                
                LoadDefaultConfig();
                return;
            }

            ValidateConfig();
            SaveConfig();
        }

        private void ValidateConfig()
        {
            if (Interface.Oxide.CallHook("Debug_UseDefaultValues") != null)
            {
                PrintWarning("Using default configuration on debug server");
                config = new ConfigData();
            }
        }

        protected override void LoadDefaultConfig()
        {
            config = new ConfigData();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion
    }
}