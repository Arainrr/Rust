using Apex.AI;
using Apex.Serialization;
using UnityEngine;

namespace Rust.Ai
{
	public class BestPlayerDistance : OptionScorerBase<BasePlayer>
	{
		[ApexSerialization]
		private float score = 10f;

		public override float Score(IAIContext context, BasePlayer option)
		{
			PlayerTargetContext playerTargetContext = context as PlayerTargetContext;
			if (playerTargetContext != null)
			{
				float distanceSqr;
				float aggroRangeSqr;
				Evaluate(playerTargetContext.Self, option.ServerPosition, out distanceSqr, out aggroRangeSqr);
				playerTargetContext.DistanceSqr[playerTargetContext.CurrentOptionsIndex] = distanceSqr;
				return (1f - distanceSqr / aggroRangeSqr) * score;
			}
			playerTargetContext.DistanceSqr[playerTargetContext.CurrentOptionsIndex] = -1f;
			return 0f;
		}

		public static void Evaluate(IAIAgent self, Vector3 optionPosition, out float distanceSqr, out float aggroRangeSqr)
		{
			Vector3 vector = optionPosition - self.Entity.ServerPosition;
			aggroRangeSqr = self.GetActiveAggressionRangeSqr();
			distanceSqr = Mathf.Min(vector.sqrMagnitude, aggroRangeSqr);
		}
	}
}
