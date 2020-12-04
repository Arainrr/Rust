namespace Rust.Ai
{
	public class ReloadOperator : BaseAction
	{
		public override void DoExecute(BaseContext c)
		{
			Reload(c as NPCHumanContext);
		}

		public static void Reload(NPCHumanContext c)
		{
			if (c == null)
			{
				return;
			}
			AttackEntity attackEntity = c.Human.GetHeldEntity() as AttackEntity;
			if (attackEntity == null)
			{
				return;
			}
			BaseProjectile baseProjectile = attackEntity as BaseProjectile;
			if ((bool)baseProjectile && baseProjectile.primaryMagazine.CanAiReload(c.Human))
			{
				baseProjectile.ServerReload();
				if (c.Human.OnReload != null)
				{
					c.Human.OnReload();
				}
			}
		}
	}
}
