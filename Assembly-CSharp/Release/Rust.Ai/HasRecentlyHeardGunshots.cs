using Apex.Serialization;

namespace Rust.Ai
{
	public sealed class HasRecentlyHeardGunshots : BaseScorer
	{
		[ApexSerialization]
		public float WithinSeconds = 10f;

		public override float GetScore(BaseContext c)
		{
			BaseNpc baseNpc = c.AIAgent as BaseNpc;
			if (baseNpc == null)
			{
				return 0f;
			}
			if (float.IsInfinity(baseNpc.SecondsSinceLastHeardGunshot) || float.IsNaN(baseNpc.SecondsSinceLastHeardGunshot))
			{
				return 0f;
			}
			return (WithinSeconds - baseNpc.SecondsSinceLastHeardGunshot) / WithinSeconds;
		}
	}
}
