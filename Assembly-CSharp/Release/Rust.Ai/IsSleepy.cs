namespace Rust.Ai
{
	public sealed class IsSleepy : BaseScorer
	{
		public override float GetScore(BaseContext c)
		{
			return 1f - c.AIAgent.GetSleep;
		}
	}
}
