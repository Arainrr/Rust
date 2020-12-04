using Apex.Serialization;
using UnityEngine;

namespace Rust.Ai
{
	public class TimeSinceLastMoveToCoverThreshold : BaseScorer
	{
		[ApexSerialization]
		public float minThreshold;

		[ApexSerialization]
		public float maxThreshold = 1f;

		public override float GetScore(BaseContext c)
		{
			return Evaluate(c as NPCHumanContext, minThreshold, maxThreshold) ? 1 : 0;
		}

		public static bool Evaluate(NPCHumanContext c, float minThreshold, float maxThreshold)
		{
			if (Mathf.Approximately(c.Human.TimeLastMovedToCover, 0f))
			{
				return true;
			}
			float num = Time.realtimeSinceStartup - c.Human.TimeLastMovedToCover;
			if (c.GetFact(NPCPlayerApex.Facts.IsMovingToCover) > 0 || num < minThreshold)
			{
				return false;
			}
			if (num >= maxThreshold)
			{
				return true;
			}
			float num2 = maxThreshold - minThreshold;
			float num3 = maxThreshold - num;
			return Random.value < num3 / num2;
		}
	}
}
