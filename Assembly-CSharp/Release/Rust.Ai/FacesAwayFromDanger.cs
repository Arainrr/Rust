using UnityEngine;

namespace Rust.Ai
{
	public class FacesAwayFromDanger : WeightedScorerBase<Vector3>
	{
		public override float GetScore(BaseContext c, Vector3 position)
		{
			float num = 0f;
			Vector3 lhs = position - c.Entity.transform.position.normalized;
			for (int i = 0; i < c.Memory.All.Count; i++)
			{
				Vector3 normalized = (c.Memory.All[i].Position - c.Entity.transform.position).normalized;
				float num2 = Vector3.Dot(lhs, normalized);
				num += 0f - num2;
			}
			return num;
		}
	}
}
