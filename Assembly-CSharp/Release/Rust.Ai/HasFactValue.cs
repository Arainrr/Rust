using Apex.Serialization;

namespace Rust.Ai
{
	public class HasFactValue : BaseScorer
	{
		[ApexSerialization]
		public BaseNpc.Facts fact;

		[ApexSerialization(defaultValue = 0f)]
		public byte value;

		public override float GetScore(BaseContext c)
		{
			if (c.GetFact(fact) != value)
			{
				return 0f;
			}
			return 1f;
		}
	}
}
