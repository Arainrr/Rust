using Apex.Serialization;
using UnityEngine;

namespace Rust.Ai
{
	public class TimeSinceLastMoveThreshold : BaseScorer
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
			if (Mathf.Approximately(c.Human.TimeLastMoved, 0f))
			{
				return true;
			}
			float num = Time.realtimeSinceStartup - c.Human.TimeLastMoved;
			if (c.GetFact(NPCPlayerApex.Facts.IsMoving) > 0 || num < minThreshold)
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
