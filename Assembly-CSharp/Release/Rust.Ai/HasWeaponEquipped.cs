namespace Rust.Ai
{
	public sealed class HasWeaponEquipped : BaseScorer
	{
		public override float GetScore(BaseContext c)
		{
			BasePlayer basePlayer = c.AIAgent as BasePlayer;
			if (basePlayer != null && basePlayer.GetHeldEntity() as AttackEntity != null)
			{
				return 1f;
			}
			return 0f;
		}
	}
}
