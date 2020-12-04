using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Loot Multiplier", "Orange", "1.2.2")]
    [Description("Multiply items in all loot containers in the game")]
    public class LootMultiplier : RustPlugin
    {
        #region Configuration

        private ConfigData configData;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Global settings")]
            public GlobalSettings globalS = new GlobalSettings();

            [JsonProperty(PropertyName = "Items and containers settings")]
            public ItemSettings itemS = new ItemSettings();

            public class GlobalSettings
            {
                [JsonProperty(PropertyName = "Default Multiplier for new containers")]
                public int defaultContainerMultiplier = 1;

                [JsonProperty(PropertyName = "Default Multiplier for new Categories")]
                public int defaultCategoryMultiplier = 1;

                [JsonProperty(PropertyName = "Multiply items with condition")]
                public bool multiplyItemsWithCondition = false;

                [JsonProperty(PropertyName = "Multiply blueprints")]
                public bool multiplyBlueprints = false;

                [JsonProperty(PropertyName = "Delay after spawning container to multiply it")]
                public float delay = 1f;
            }

            public class ItemSettings
            {
                [JsonProperty(PropertyName = "Containers list (shortPrefabName: multiplier)")]
                public Dictionary<string, int> containers = new Dictionary<string, int>()
                {
                    {"bradley_crate", 1},
                    {"codelockedhackablecrate", 1},
                    {"codelockedhackablecrate_oilrig", 1},
                    {"crate_basic", 1},
                    {"crate_elite", 1},
                    {"crate_mine", 1},
                    {"crate_normal", 1},
                    {"crate_normal_2", 1},
                    {"crate_normal_2_food", 1},
                    {"crate_normal_2_medical", 1},
                    {"crate_tools", 1},
                    {"crate_underwater_advanced", 1},
                    {"crate_underwater_basic", 1},
                    {"foodbox", 1},
                    {"heli_crate", 1},
                    {"loot-barrel-1", 1},
                    {"loot-barrel-2", 1},
                    {"loot_barrel_1", 1},
                    {"loot_barrel_2", 1},
                    {"minecart", 1},
                    {"oil_barrel", 1},
                    {"supply_drop", 1},
                    {"trash-pile-1", 1},
                    {"vehicle_parts", 1}
                };

                [JsonProperty(PropertyName = "Categories list (Category: multiplier)")]
                public Dictionary<string, int> categories = new Dictionary<string, int>()
                {
                    {"Ammunition", 1},
                    {"Attire", 1},
                    {"Component", 1},
                    {"Construction", 1},
                    {"Electrical", 1},
                    {"Food", 1},
                    {"Fun", 1},
                    {"Items", 1},
                    {"Medical", 1},
                    {"Misc", 1},
                    {"Resources", 1},
                    {"Tool", 1},
                    {"Traps", 1},
                    {"Weapon", 1}
                };

                [JsonProperty(PropertyName = "Items list (shortname: multiplier)")]
                public Dictionary<string, int> items = new Dictionary<string, int>()
                {
                    {"metalpipe", 1},
                    {"scrap", 1},
                    {"tarp", 1}
                };

                [JsonProperty(PropertyName = "Item | Category Blacklist", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<string> blacklist = new List<string>()
                {
                    {"ammo.rocket.smoke"},
                    {"Attire"},
                    {"Weapon"}
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                configData = Config.ReadObject<ConfigData>();

                if (configData == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file");
            configData = new ConfigData();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(configData);
        }

        #endregion

        #region Oxide Hooks

        private void OnLootSpawn(StorageContainer container)
        {
            timer.Once(configData.globalS.delay, () =>
            {
                Multiply(container);
            });
        }

        #endregion

        #region Core

        private void Multiply(StorageContainer container)
        {
            if (container == null) return;

            int containerMultiplier = GetContainerMultiplier(container.ShortPrefabName);

            foreach (var item in container.inventory.itemList)
            {
                var shortname = item.info.shortname;
                var category = item.info.category.ToString();

                if (configData.itemS.blacklist.Contains(shortname) || configData.itemS.blacklist.Contains(category))
                    continue;

                if (!configData.globalS.multiplyItemsWithCondition && item.hasCondition)
                    continue;

                if (!configData.globalS.multiplyBlueprints && item.IsBlueprint())
                    continue;

                int categoryMultiplier = GetCategoryMultiplier(category);
                int itemMultiplier;
                if (!configData.itemS.items.TryGetValue(shortname, out itemMultiplier))
                {
                    itemMultiplier = 1;
                }

                if (containerMultiplier * categoryMultiplier * itemMultiplier <= 1)
                {
                    continue;
                }

                item.amount *= containerMultiplier * categoryMultiplier * itemMultiplier;
            }
        }

        #endregion

        #region Helpers
        private int GetContainerMultiplier(string containerName)
        {
            int multiplier;
            if (configData.itemS.containers.TryGetValue(containerName, out multiplier))
            {
                return multiplier;
            }

            configData.itemS.containers[containerName] = configData.globalS.defaultContainerMultiplier;
            configData.itemS.containers = SortDictionary(configData.itemS.containers);
            SaveConfig();
            return configData.globalS.defaultContainerMultiplier;
        }
        
        private int GetCategoryMultiplier(string category)
        {
            int multiplier;
            if (configData.itemS.categories.TryGetValue(category, out multiplier))
            {
                return multiplier;
            }

            configData.itemS.categories[category] = configData.globalS.defaultCategoryMultiplier;
            configData.itemS.categories = SortDictionary(configData.itemS.categories);
            SaveConfig();
            return configData.globalS.defaultContainerMultiplier;
        }

        private Dictionary<string, int> SortDictionary(Dictionary<string, int> dic)
        {
            return dic.OrderBy(key => key.Key)
                .ToDictionary(key => key.Key, value => value.Value);
        }

        #endregion Helpers
    }
}