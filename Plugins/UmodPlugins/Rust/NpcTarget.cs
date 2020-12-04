namespace Oxide.Plugins
{
    [Info("NPC Target", "Iv Misticos", "1.0.3")]
	[Description("Deny NPCs target other NPCs")]
    class NpcTarget : RustPlugin
    {
        private object OnNpcTarget(BaseEntity attacker, BaseEntity entity)
        {
            if (entity != null && (entity.IsNpc || entity is BaseNpc) &&
                attacker != null && (attacker.IsNpc || attacker is BaseNpc))
                return true;
            
            return null;
        }
    }
}