namespace Rust.Ai
{
	public class HasAttackTarget : BaseScorer
	{
		public override float GetScore(BaseContext c)
		{
			if (c.AIAgent.AttackTarget == null)
			{
				return 0f;
			}
			return score;
		}
	}
}
