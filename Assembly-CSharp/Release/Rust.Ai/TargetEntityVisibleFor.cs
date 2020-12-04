using Apex.Serialization;

namespace Rust.Ai
{
	public sealed class TargetEntityVisibleFor : BaseScorer
	{
		[ApexSerialization]
		public float duration;

		public override float GetScore(BaseContext c)
		{
			if (!(c.AIAgent.AttackTargetVisibleFor >= duration))
			{
				return 0f;
			}
			return 1f;
		}
	}
}
