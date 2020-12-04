﻿using Newtonsoft.Json;
using Rust.Modular;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Engine Parts Durability", "WhiteThunder", "1.1.0")]
    [Description("Alters engine part durability loss when modular cars are damaged.")]
    internal class EnginePartsDurability : CovalencePlugin
    {
        private DurabilityConfig PluginConfig;

        private void Init()
        {
            PluginConfig = Config.ReadObject<DurabilityConfig>();
        }

        private void OnServerInitialized(bool serverInitialized)
        {
            // The OnEntitySpawned hook already covers server init so this is just for late loading
            if (!serverInitialized)
            {
                foreach (var engineModule in BaseNetworkable.serverEntities.OfType<VehicleModuleEngine>())
                    AdjustEnginePartsDurability(engineModule);
            }
        }

        private void OnEntitySpawned(VehicleModuleEngine engineModule) =>
            AdjustEnginePartsDurability(engineModule);

        private void AdjustEnginePartsDurability(VehicleModuleEngine engineModule)
        {
            NextTick(() =>
            {
                if (engineModule == null) return;
                var engineStorage = engineModule.GetContainer() as EngineStorage;
                if (engineStorage == null) return;
                engineStorage.internalDamageMultiplier = PluginConfig.DurabilityLossMultiplier;
            });
        }

        protected override void LoadDefaultConfig() => Config.WriteObject(new DurabilityConfig(), true);

        internal class DurabilityConfig
        {
            [JsonProperty("DurabilityLossMultiplier")]
            public float DurabilityLossMultiplier = 0;
        }
    }
}
