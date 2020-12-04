namespace Rust.Ai
{
	public sealed class IsCurrentFoodTarget : WeightedScorerBase<BaseEntity>
	{
		public override float GetScore(BaseContext c, BaseEntity target)
		{
			return (c.AIAgent.FoodTarget == target) ? 1 : 0;
		}
	}
}
