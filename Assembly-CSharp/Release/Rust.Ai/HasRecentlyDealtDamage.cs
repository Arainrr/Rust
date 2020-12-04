using Apex.Serialization;

namespace Rust.Ai
{
	public sealed class HasRecentlyDealtDamage : BaseScorer
	{
		[ApexSerialization]
		public float WithinSeconds = 10f;

		public override float GetScore(BaseContext c)
		{
			if (float.IsInfinity(c.Entity.SecondsSinceDealtDamage) || float.IsNaN(c.Entity.SecondsSinceDealtDamage))
			{
				return 0f;
			}
			return (WithinSeconds - c.Entity.SecondsSinceDealtDamage) / WithinSeconds;
		}
	}
}
