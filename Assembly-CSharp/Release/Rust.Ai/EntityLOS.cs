namespace Rust.Ai
{
	public sealed class EntityLOS : WeightedScorerBase<BaseEntity>
	{
		public override float GetScore(BaseContext c, BaseEntity target)
		{
			if (!c.Entity.IsVisible(target.CenterPoint()))
			{
				return 0f;
			}
			return 1f;
		}
	}
}
