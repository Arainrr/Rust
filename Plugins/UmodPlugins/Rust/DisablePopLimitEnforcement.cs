using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
	[Info("Disable Population Limit Enforcement", "WhiteThunder", "1.0.1")]
	[Description("Disables limit enforcement for the configured entity populations.")]

	internal class DisablePopLimitEnforcement : CovalencePlugin
    {
        private void OnSaveLoad()
        {
			var pluginConfig = Config.ReadObject<PluginConfig>();
            DisableSpawnLimitEnforcement(pluginConfig);
        }

		private void DisableSpawnLimitEnforcement(PluginConfig pluginConfig)
        {
            var spawnHandler = SingletonComponent<SpawnHandler>.Instance;
            foreach (var population in spawnHandler.AllSpawnPopulations)
            {
                if (pluginConfig.DisableLimitEnforcementForPopulations.Contains(population.name))
                {
                    if (population.EnforcePopulationLimits)
                    {
                        population.EnforcePopulationLimits = false;
                        Puts("Disabled spawn limit enforcement for: {0}", population.name);
                    }
                    else
                        Puts("Spawn limit enforcement already disabled for: {0}", population.name);
                }
            }
        }

        [Command("populations.limitenforcement.list")]
		private void ListPopulationLimitEnforcementCommand(IPlayer player, string cmd, string[] args)
        {
			if (!player.IsServer) return;
			var spawnHandler = SingletonComponent<SpawnHandler>.Instance;

			Puts("Listing all populations and whether limit enforcement is enabled...");
            foreach (var population in spawnHandler.AllSpawnPopulations)
				Puts("{0}: {1}", population.name, population.EnforcePopulationLimits ? "Enabled" : "Disabled");
		}

		protected override void LoadDefaultConfig() => Config.WriteObject(GetDefaultConfig(), true);

        private PluginConfig GetDefaultConfig() => new PluginConfig();

        internal class PluginConfig
        {
            [JsonProperty("DisableLimitEnforcementForPopulations")]
            public string[] DisableLimitEnforcementForPopulations = new string[0];
        }
	}
}
