using Apex.Serialization;

namespace Rust.Ai
{
	public class AtDestinationFor : BaseScorer
	{
		[ApexSerialization]
		public float Duration = 5f;

		public override float GetScore(BaseContext c)
		{
			if (!(c.AIAgent.TimeAtDestination >= Duration))
			{
				return 0f;
			}
			return 1f;
		}
	}
}
