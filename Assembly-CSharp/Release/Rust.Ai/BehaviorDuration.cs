using Apex.Serialization;

namespace Rust.Ai
{
	public sealed class BehaviorDuration : BaseScorer
	{
		[ApexSerialization]
		public BaseNpc.Behaviour Behaviour;

		[ApexSerialization]
		public float duration;

		public override float GetScore(BaseContext c)
		{
			return (c.AIAgent.CurrentBehaviour == Behaviour && c.AIAgent.currentBehaviorDuration >= duration) ? 1 : 0;
		}
	}
}
