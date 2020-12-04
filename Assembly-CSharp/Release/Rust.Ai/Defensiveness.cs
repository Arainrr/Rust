namespace Rust.Ai
{
	public sealed class Defensiveness : BaseScorer
	{
		public override float GetScore(BaseContext c)
		{
			return c.AIAgent.GetStats.Defensiveness;
		}
	}
}
