namespace Rust.Ai
{
	public sealed class HealthFraction : BaseScorer
	{
		public override float GetScore(BaseContext c)
		{
			return c.Entity.healthFraction;
		}
	}
}
