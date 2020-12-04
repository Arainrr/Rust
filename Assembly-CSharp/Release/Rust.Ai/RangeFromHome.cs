using Apex.Serialization;
using UnityEngine;

namespace Rust.Ai
{
	public class RangeFromHome : WeightedScorerBase<Vector3>
	{
		[ApexSerialization]
		public float Range = 50f;

		[ApexSerialization]
		public AnimationCurve ResponseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

		[ApexSerialization]
		public bool UseResponseCurve = true;

		public override float GetScore(BaseContext c, Vector3 position)
		{
			float num = Mathf.Min(Vector3.Distance(position, c.AIAgent.SpawnPosition), Range) / Range;
			if (!UseResponseCurve)
			{
				return num;
			}
			return ResponseCurve.Evaluate(num);
		}
	}
}
