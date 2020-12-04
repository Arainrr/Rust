namespace Rust.Ai
{
	public sealed class IsSleeping : BaseScorer
	{
		public override float GetScore(BaseContext c)
		{
			return (c.AIAgent.CurrentBehaviour == BaseNpc.Behaviour.Sleep) ? 1 : 0;
		}
	}
}
