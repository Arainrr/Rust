using System.IO;
using Oxide.Core;
using UnityEngine;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("HW Constructions Decay", "klauz24", 1.0), Description("Easy management of Hurtworld decay system")]
    internal class HWConstructionsDecay : HurtworldPlugin
    {
        private void OnServerInitialized() => UpdateDecayDefaultValues();

        private Configuration _config;

        private class Configuration
        {
            [JsonProperty(PropertyName = "Enable decay system (false = disabled/true = enabled)")]
            public bool EnableDecay { get; set; } = true;

            [JsonProperty(PropertyName = "Decay damage (How much damage will be done when a structure decays)")]
            public Vector2 DecayDamage { get; set; } = new Vector2(5, 10);

            [JsonProperty(PropertyName = "Decay frequency (How often a structure will be damaged by decay in seconds)")]
            public float DecayFrequency { get; set; } = 600f;

            [JsonProperty(PropertyName = "Decay modify/claim timeout (How long a structure must be both unclaimed and unmodified, before it begins to decay in seconds)")]
            public float DecayModifyClaimTimeout { get; set; } = 86400f;
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

        private void UpdateDecayDefaultValues()
        {
            ConstructionManager.StructureDecayEnabled = (bool)_config.EnableDecay;
            ConstructionManager.StructureDecayDamage = (Vector2)_config.DecayDamage;
            ConstructionManager.StructureDecayFrequency = (float)_config.DecayFrequency;
            ConstructionManager.StructureDecayModifyTime = (float)_config.DecayModifyClaimTimeout;
        }
    }
}