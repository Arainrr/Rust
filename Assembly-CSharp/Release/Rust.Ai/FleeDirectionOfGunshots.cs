using Apex.Serialization;
using UnityEngine;

namespace Rust.Ai
{
	public sealed class FleeDirectionOfGunshots : WeightedScorerBase<Vector3>
	{
		[ApexSerialization]
		public float WithinSeconds = 10f;

		[ApexSerialization]
		public float Arc = -0.2f;

		public override float GetScore(BaseContext c, Vector3 option)
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
			if ((WithinSeconds - baseNpc.SecondsSinceLastHeardGunshot) / WithinSeconds <= 0f)
			{
				return 0f;
			}
			Vector3 rhs = option - baseNpc.transform.localPosition;
			float num = Vector3.Dot(baseNpc.LastHeardGunshotDirection, rhs);
			if (!(Arc > num))
			{
				return 0f;
			}
			return 1f;
		}
	}
}
