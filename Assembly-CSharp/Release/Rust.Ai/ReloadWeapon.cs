namespace Rust.Ai
{
	public sealed class ReloadWeapon : BaseAction
	{
		public override void DoExecute(BaseContext c)
		{
			BasePlayer basePlayer = c.AIAgent as BasePlayer;
			if (!(basePlayer != null))
			{
				return;
			}
			AttackEntity attackEntity = basePlayer.GetHeldEntity() as AttackEntity;
			if (attackEntity != null)
			{
				BaseProjectile baseProjectile = attackEntity as BaseProjectile;
				if ((bool)baseProjectile)
				{
					baseProjectile.ServerReload();
				}
			}
		}
	}
}
