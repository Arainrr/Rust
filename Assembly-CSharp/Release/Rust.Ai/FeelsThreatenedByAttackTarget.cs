namespace Rust.Ai
{
	public sealed class FeelsThreatenedByAttackTarget : BaseScorer
	{
		public override float GetScore(BaseContext c)
		{
			if (c.AIAgent.AttackTarget == null)
			{
				return 0f;
			}
			return 1f;
		}
	}
}
