using Apex.Serialization;
using UnityEngine;

namespace Rust.Ai
{
	public class ProximityToPeers : WeightedScorerBase<Vector3>
	{
		[ApexSerialization(defaultValue = 14f)]
		public float desiredRange = 14f;

		public override float GetScore(BaseContext c, Vector3 position)
		{
			float num = float.MaxValue;
			Vector3 vector = Vector3.zero;
			for (int i = 0; i < c.Memory.All.Count; i++)
			{
				Memory.SeenInfo memory = c.Memory.All[i];
				if (memory.Entity == null)
				{
					continue;
				}
				float num2 = Test(memory, c);
				if (!(num2 <= 0f))
				{
					float num3 = (position - memory.Position).sqrMagnitude * num2;
					if (num3 < num)
					{
						num = num3;
						vector = memory.Position;
					}
				}
			}
			if (vector == Vector3.zero)
			{
				return 0f;
			}
			num = Vector3.Distance(vector, position);
			return 1f - num / desiredRange;
		}

		protected virtual float Test(Memory.SeenInfo memory, BaseContext c)
		{
			if (memory.Entity == null)
			{
				return 0f;
			}
			if (memory.Entity as BaseNpc == null)
			{
				return 0f;
			}
			return 1f;
		}
	}
}
