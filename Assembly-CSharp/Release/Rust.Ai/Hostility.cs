namespace Rust.Ai
{
	public sealed class Hostility : BaseScorer
	{
		public override float GetScore(BaseContext c)
		{
			return c.AIAgent.GetStats.Hostility;
		}
	}
}
