using Newtonsoft.Json;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Vending Loot Drop", "Bazz3l", "1.0.5")]
    [Description("Drops vending machine contents when destroyed.")]
    class VendingLootDrop : RustPlugin
    {
        #region Fields
        PluginConfig _config;
        #endregion

        #region Config
        PluginConfig GetDefaultConfig()
        {
            return new PluginConfig {
                DropChance = 0.75f
            };
        }

        class PluginConfig
        {
            [JsonProperty(PropertyName = "Drop chance (percents, 1f = 100%)")]
            public float DropChance;
        }
        #endregion

        #region Oxide
        protected override void LoadDefaultConfig() => Config.WriteObject(GetDefaultConfig(), true);

        private void OnServerInitialized()
        {
            Subscribe(nameof(OnEntitySpawned));

            foreach (VendingMachine machine in BaseNetworkable.serverEntities.OfType<VendingMachine>())
            {
                OnEntitySpawned(machine);
            }
        }

        void Init()
        {
            _config = Config.ReadObject<PluginConfig>();

            Unsubscribe(nameof(OnEntitySpawned));
        }

        void OnEntitySpawned(VendingMachine machine)
        {
            machine.dropChance = _config.DropChance;
        }
        #endregion
    }
}