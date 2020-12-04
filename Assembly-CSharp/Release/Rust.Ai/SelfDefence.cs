using Apex.Serialization;

namespace Rust.Ai
{
	public sealed class SelfDefence : BaseScorer
	{
		[ApexSerialization]
		public float WithinSeconds = 10f;

		public override float GetScore(BaseContext c)
		{
			if (float.IsNegativeInfinity(c.Entity.SecondsSinceAttacked) || float.IsNaN(c.Entity.SecondsSinceAttacked))
			{
				return 0f;
			}
			return (WithinSeconds - c.Entity.SecondsSinceAttacked) / WithinSeconds * c.AIAgent.GetStats.Defensiveness;
		}
	}
}
