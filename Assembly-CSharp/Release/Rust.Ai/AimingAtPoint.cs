using UnityEngine;

namespace Rust.Ai
{
	public sealed class AimingAtPoint : WeightedScorerBase<Vector3>
	{
		public override float GetScore(BaseContext context, Vector3 position)
		{
			return Vector3.Dot(context.Entity.transform.forward, (position - context.Position).normalized);
		}
	}
}
