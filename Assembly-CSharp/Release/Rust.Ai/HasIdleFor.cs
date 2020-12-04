using Apex.Serialization;

namespace Rust.Ai
{
	public class HasIdleFor : BaseScorer
	{
		[ApexSerialization]
		public float StuckSeconds = 5f;

		public override float GetScore(BaseContext c)
		{
			if (!(c.AIAgent.GetStuckDuration >= StuckSeconds))
			{
				return 0f;
			}
			return 1f;
		}
	}
}
