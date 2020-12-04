namespace Rust.Ai
{
	public sealed class IsBusy : BaseScorer
	{
		public override float GetScore(BaseContext c)
		{
			return c.AIAgent.BusyTimerActive() ? 1 : 0;
		}
	}
}
