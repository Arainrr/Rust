namespace Rust.Ai
{
	public sealed class IsCurrentAttackEntity : WeightedScorerBase<BaseEntity>
	{
		public override float GetScore(BaseContext c, BaseEntity target)
		{
			return (c.AIAgent.AttackTarget == target) ? 1 : 0;
		}
	}
}
