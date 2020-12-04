﻿//Reference: UnityEngine.UI
using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("StackSize", "Mr. Blue", "2.1.2")]
    class StackSize : HurtworldPlugin
    {
        #region Variables
        private GlobalItemManager GIM = Singleton<GlobalItemManager>.Instance;
        private PluginConfig config = new PluginConfig();
        #endregion Variables

        #region Classes
        public class PluginConfig
        {
            public int GameVersion;
            public List<StackItem> StackSizes = new List<StackItem>();
        }

        public class StackItem
        {
            public string Name;
            public int StackSize;
            public string Guid;

            public StackItem(string name, int stackSize, string guid)
            {
                Name = name;
                StackSize = stackSize;
                Guid = guid;
            }
        }
        #endregion Classes

        #region Config
        protected override void SaveConfig()
        {
            Config.WriteObject(config, true);
        }

        private void CheckConfig()
        {
            if (config.GameVersion == GameManager.PROTOCOL_VERSION) return;
            UpdateStackSizes();
            Puts("Incorrect config version ({0}/{1}), generating new config", config.GameVersion, GameManager.PROTOCOL_VERSION);
            Config.WriteObject(config, false, $"{Config.Filename}.old");
            LoadDefaultConfig();
        }

        protected override void LoadDefaultConfig()
        {
            if (GIM?.ItemGenerators?.Values == null || GIM?.ItemGenerators?.Values?.Count < 1)
            {
                timer.Once(10f, LoadDefaultConfig);
                return;
            }

            Puts("Generating Default Config");
            List<StackItem> stackSizes = new List<StackItem>();
            foreach (ItemGeneratorAsset i in GIM.ItemGenerators.Values)
            {
                if (i == null || i?.name == "" || i?.DataProvider == null || i?.DataProvider?.MaxStackSize == null)
                    continue;

                stackSizes.Add(new StackItem(i.name, i.DataProvider.MaxStackSize, RuntimeHurtDB.Instance.GetGuid(i)));
            }
            Config.Clear();
            config = new PluginConfig
            {
                GameVersion = GameManager.PROTOCOL_VERSION,
                StackSizes = stackSizes
            };
            SaveConfig();
        }
        #endregion Config

        void OnServerInitialized()
        {
            if (!Config.Exists())
                LoadDefaultConfig();
            else
            {
                config = Config.ReadObject<PluginConfig>();
                CheckConfig();
                UpdateStackSizes();
            }
        }

        void UpdateStackSizes()
        {
            foreach (StackItem item in config.StackSizes)
            {
                Dictionary<int, ItemGeneratorAsset>.ValueCollection i = GIM.ItemGenerators.Values;
                if (i == null || i.Count() < 1) continue;

                IEnumerable<ItemGeneratorAsset> itemGeneratorAssets = i.Where(x => (RuntimeHurtDB.Instance.GetGuid(x) ?? null) == item.Guid);
                if (itemGeneratorAssets == null || itemGeneratorAssets.Count() < 1) continue;

                ItemGeneratorAsset itemGeneratorAsset = itemGeneratorAssets.First();
                if (itemGeneratorAsset?.DataProvider?.MaxStackSize == null) continue;

                itemGeneratorAsset.DataProvider.MaxStackSize = Convert.ToUInt16(item.StackSize);
            }
            Puts("Loaded Stacksizes!");
        }
    }
}