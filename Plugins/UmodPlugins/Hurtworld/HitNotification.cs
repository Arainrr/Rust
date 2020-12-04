using System.Linq;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Hit Notification", "klauz24", "1.0.1"), Description("Shows dealt damage as notification.")]
    internal class HitNotification : HurtworldPlugin
    {
        protected override void LoadDefaultMessages() => lang.RegisterMessages(new Dictionary<string, string>() { { "Notification", "{0} hp" } }, this);

        private void OnEntityTakeDamage(AIEntity entity, EntityEffectSourceData source) => HandleNotification(GetPlayerSession(source), source.Value);

        private void OnPlayerTakeDamage(PlayerSession session, EntityEffectSourceData source) => HandleNotification(GetPlayerSession(source), source.Value);

        private void HandleNotification(PlayerSession attacker, float value)
        {
            if (attacker != null)
            {
                var dmg = value.ToString().Split('.').First();
                if (dmg != "-0" || dmg != "0")
                {
                    AlertManager.Instance.GenericTextNotificationServer(string.Format(lang.GetMessage("Notification", this, attacker.SteamId.ToString()), dmg), attacker.Player);
                }
            }
        }

        private PlayerSession GetPlayerSession(EntityEffectSourceData dataSource)
        {
            if (dataSource?.EntitySource?.GetComponent<EntityStats>()?.networkView == null) return null;
            HNetworkView networkView = dataSource.EntitySource.GetComponent<EntityStats>().networkView;
            return GameManager.Instance.GetSession(networkView.owner);
        }
    }
}