using UnityEngine;

namespace Rust.Ai
{
	public class HasPatrolPointsInRoamRange : BaseScorer
	{
		public override float GetScore(BaseContext c)
		{
			return Evaluate(c as NPCHumanContext) ? 1 : 0;
		}

		public static bool Evaluate(NPCHumanContext c)
		{
			if (c.AiLocationManager != null)
			{
				PathInterestNode firstPatrolPointInRange = c.AiLocationManager.GetFirstPatrolPointInRange(c.Position, c.AIAgent.GetStats.MinRoamRange, c.AIAgent.GetStats.MaxRoamRange);
				if (firstPatrolPointInRange != null)
				{
					return Time.time >= firstPatrolPointInRange.NextVisitTime;
				}
				return false;
			}
			return false;
		}
	}
}
