using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Air Fuel", "WhiteDragon", "0.1.1")]
    [Description("Sets the initial amount of fuel for vendor purchased air vehicles.")]
    public class AirFuel : RustPlugin
    {
        #region _fields_

        private AirFuelConfig PluginConfig;

        #endregion _fields_

        #region _hooks_

        private void Init()
        {
            PluginConfig = Config.ReadObject<AirFuelConfig>();
        }

        private void OnEntitySpawned(MiniCopter vehicle)
        {
            if (Rust.Application.isLoadingSave) return;

            NextTick(() =>
            {
				// creatorEntity is only set temporarily when spawned by a VehicleSpawner (Air Wolf vendor)
				// This ignores helis spawned other ways
				if (vehicle.creatorEntity == null) return;
				
                var fuelsystem = vehicle?.GetFuelSystem();
                if(fuelsystem != null)
                {
                    var initialFuelAmount = vehicle is ScrapTransportHelicopter ?
                        PluginConfig.ScrapHelicopterInitialFuelAmount :
                        PluginConfig.InitialFuelAmount;

                    if (fuelsystem.GetFuelAmount() <= initialFuelAmount)
                    {
                        return;
                    }

                    var fuel = fuelsystem.GetFuelItem();
                    if(fuel != null)
                    {
                        fuel.amount = initialFuelAmount;
                        fuel.MarkDirty();
                    }
                }
            });
        }

        #endregion _hooks_

        #region _config_

        protected override void LoadDefaultConfig() => Config.WriteObject(GetDefaultConfig(), true);

        private AirFuelConfig GetDefaultConfig()
        {
            return new AirFuelConfig
            {
                InitialFuelAmount = 100,
                ScrapHelicopterInitialFuelAmount = 100
            };
        }

        internal class AirFuelConfig
        {
            [JsonIgnore]
            private int _scrapHeliAmount = -1;

            [JsonProperty("InitialFuelAmount")]
            public int InitialFuelAmount = 100;

            [JsonProperty("ScrapHelicopterInitialFuelAmount")]
            public int ScrapHelicopterInitialFuelAmount
            {
                get { return _scrapHeliAmount == -1 ? InitialFuelAmount : _scrapHeliAmount; }
                set { _scrapHeliAmount = value; }
            }
        }

        #endregion _config_
    }
}