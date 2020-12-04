namespace Rust.Ai
{
	public class TimeAtDestination : BaseScorer
	{
		public override float GetScore(BaseContext c)
		{
			return c.AIAgent.TimeAtDestination;
		}
	}
}
