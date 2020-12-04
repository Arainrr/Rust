using Apex.Serialization;

namespace Rust.Ai
{
	public class LastSetDestinationTimeGreaterThan : BaseScorer
	{
		[ApexSerialization]
		private float Timeout = 5f;

		public override float GetScore(BaseContext c)
		{
			BaseNpc baseNpc = c.AIAgent as BaseNpc;
			if (baseNpc != null && baseNpc.SecondsSinceLastSetDestination > Timeout)
			{
				return 1f;
			}
			return 0f;
		}
	}
}
