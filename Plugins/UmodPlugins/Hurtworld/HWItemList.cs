﻿using System.IO;
using Oxide.Core;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("HW Item List", "klauz24", "1.4.1"), Description("Allows to get all the item properties")]
    internal class HWItemList : HurtworldPlugin
    {
        private readonly int _protocol = GameManager.PROTOCOL_VERSION;

        private struct Items
        {
            public int itemId;
            public string itemName, itemFullNameKey, itemGuid;
        }

        private Configuration _config;

        private class Configuration
        {
            [JsonProperty(PropertyName = "Protocol version")]
            public int ProtocolVersion { get; set; } = GameManager.PROTOCOL_VERSION;

            [JsonProperty(PropertyName = "Reset on load (Enable it if you are using any Steam Workshop mods)")]
            public bool ResetOnLoad { get; set; } = false;

            [JsonProperty(PropertyName = "List of items")]
            public List<Items> ItemList { get; set; } = new List<Items>();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();

                if (_config == null)
                {
                    throw new JsonException();
                }
            }
            catch
            {
                string configPath = $"{Interface.Oxide.ConfigDirectory}{Path.DirectorySeparatorChar}{Name}";
                PrintWarning($"Could not load a valid configuration file, creating a new configuration file at {configPath}.json");
                Config.WriteObject(_config, false, $"{configPath}_invalid.json");
                LoadDefaultConfig();
                SaveConfig();
            }
        }

        protected override void LoadDefaultConfig() => _config = new Configuration();

        protected override void SaveConfig() => Config.WriteObject(_config);

        private void OnServerInitialized() => ValidateConfig();

        private void ValidateConfig()
        {
            if (_config.ItemList.Count == 0)
            {
                GenerateItemList();
                PrintWarning("Generating new list of items (Empty list).");
                return;
            }
            if (_config.ResetOnLoad)
            {
                GenerateItemList();
                PrintWarning("Generating new list of items (_config.ResetOnLoad enabled).");
                return;
            }
            if (_config.ProtocolVersion < _protocol)
            {
                _config.ProtocolVersion = _protocol;
                GenerateItemList();
                PrintWarning("Protocol has changed, generating new list of items.");
            }
        }

        private void GenerateItemList()
        {
            if (_config.ItemList.Count > 0) _config.ItemList.Clear();
            foreach (ItemGeneratorAsset item in GlobalItemManager.Instance.GetGenerators().Values)
            {
                if (item == null) continue;
                Items newList = new Items
                {
                    itemId = item.GeneratorId,
                    itemName = item.ToString(),
                    itemFullNameKey = item.GetNameKey(),
                    itemGuid = RuntimeHurtDB.Instance.GetGuid(item)
                };
                _config.ItemList.Add(newList);
            }
            SaveConfig();
        }
    }
}