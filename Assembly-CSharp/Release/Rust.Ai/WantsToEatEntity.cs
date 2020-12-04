namespace Rust.Ai
{
	public sealed class WantsToEatEntity : WeightedScorerBase<BaseEntity>
	{
		public override float GetScore(BaseContext c, BaseEntity target)
		{
			return c.AIAgent.WantsToEat(target) ? 1 : 0;
		}
	}
}
