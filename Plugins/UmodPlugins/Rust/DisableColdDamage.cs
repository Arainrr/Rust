using System.Reflection;
using Rust;

namespace Oxide.Plugins
{
    [Info("Disable Cold Damage", "Talha", "1.0.3")]
    [Description("Prevents cold damage for players, with permission.")]
    public class DisableColdDamage : RustPlugin
    {
        private const string permDisable = "disablecolddamage.use";
		
        private void Init()
        {
            permission.RegisterPermission(permDisable, this);
        }

        void OnRunPlayerMetabolism(PlayerMetabolism metabolism, BaseCombatEntity entity)
        {
            var player = entity as BasePlayer;
            if (!(entity is BasePlayer)) return;
            if (!permission.UserHasPermission(player.UserIDString, permDisable)) return;
            if (player.metabolism.temperature.value < 20)
            {
                player.metabolism.temperature.value = 21;
            }
        }
    }
}