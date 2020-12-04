namespace Oxide.Plugins
{
    [Info("Quarry Repair", "Orange", "1.0.21")] 
    [Description("Allows players to repair quarries")]
    public class QuarryRepair : RustPlugin
    {
        #region Oxide Hooks

        private void OnHammerHit(BasePlayer player, HitInfo info)
        {
            var entity = info.HitEntity?.GetComponent<BaseCombatEntity>();
            if (entity == null || entity.SecondsSinceAttacked < 30)
            {
                return;
            }

            if (entity is BaseResourceExtractor)
            {
                var missing = entity.MaxHealth() - entity.Health();
                entity.Heal(missing);
                entity.OnRepair();
            }
        }

        #endregion
    }
}