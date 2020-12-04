using System.Linq;

namespace Oxide.Plugins
{
    [Info("Disable Temperature Functions", "Orange", "1.0.0")]
    [Description("Prevents cold/heat damage/overlay for players")]
    public class DisableTemperatureFunctions : RustPlugin
    {
		private const string permDisable = "disabletemperaturefunctions.use";
		
		private void Init()
        {
            permission.RegisterPermission(permDisable, this);
        }

        private void OnServerInitialized()
        {
            foreach (var player in BasePlayer.activePlayerList.ToList())
            {
                OnPlayerSleepEnded(player);
            }
            
            foreach (var player in BasePlayer.sleepingPlayerList.ToList())
            {
                OnPlayerSleep(player);
            }
        }
        
        private void OnPlayerSleep(BasePlayer player)
        {
            Check(player);
        }
        
        private void OnPlayerSleepEnded(BasePlayer player)
        {
            Check(player);
        }

        private void Check(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, permDisable))
            {
                player.metabolism.temperature.max = 30;
                player.metabolism.temperature.min = 30;
                player.metabolism.temperature.value = 30;
                player.SendNetworkUpdate();
            }
        }
    }
}