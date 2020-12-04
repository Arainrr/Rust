using Apex.Serialization;
using UnityEngine;

namespace Rust.Ai
{
	public class ProximityToDanger : WeightedScorerBase<Vector3>
	{
		[ApexSerialization]
		public float Range = 20f;

		public override float GetScore(BaseContext c, Vector3 position)
		{
			float num = 0f;
			for (int i = 0; i < c.Memory.All.Count; i++)
			{
				float num2 = Vector3.Distance(position, c.Memory.All[i].Position) / Range;
				num2 = 1f - num2;
				if (!(num2 < 0f))
				{
					num += c.Memory.All[i].Danger * num2;
				}
			}
			return num;
		}
	}
}
