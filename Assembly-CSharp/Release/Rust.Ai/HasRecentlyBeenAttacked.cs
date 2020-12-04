using Apex.Serialization;

namespace Rust.Ai
{
	public sealed class HasRecentlyBeenAttacked : BaseScorer
	{
		[ApexSerialization]
		public float WithinSeconds = 10f;

		[ApexSerialization]
		public bool BooleanResult;

		public override float GetScore(BaseContext c)
		{
			if (float.IsNegativeInfinity(c.Entity.SecondsSinceAttacked) || float.IsNaN(c.Entity.SecondsSinceAttacked))
			{
				return 0f;
			}
			if (BooleanResult)
			{
				if (!(c.Entity.SecondsSinceAttacked <= WithinSeconds))
				{
					return 0f;
				}
				return 1f;
			}
			return (WithinSeconds - c.Entity.SecondsSinceAttacked) / WithinSeconds;
		}
	}
}
