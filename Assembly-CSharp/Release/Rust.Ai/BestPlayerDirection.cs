using Apex.AI;
using Apex.Serialization;
using UnityEngine;

namespace Rust.Ai
{
	public class BestPlayerDirection : OptionScorerBase<BasePlayer>
	{
		[ApexSerialization]
		private float score = 10f;

		public override float Score(IAIContext context, BasePlayer option)
		{
			PlayerTargetContext playerTargetContext = context as PlayerTargetContext;
			Vector3 dir;
			float dot;
			if (playerTargetContext != null && Evaluate(playerTargetContext.Self, option.ServerPosition, out dir, out dot))
			{
				playerTargetContext.Direction[playerTargetContext.CurrentOptionsIndex] = dir;
				playerTargetContext.Dot[playerTargetContext.CurrentOptionsIndex] = dot;
				return (dot + 1f) * 0.5f * score;
			}
			playerTargetContext.Direction[playerTargetContext.CurrentOptionsIndex] = Vector3.zero;
			playerTargetContext.Dot[playerTargetContext.CurrentOptionsIndex] = -1f;
			return 0f;
		}

		public static bool Evaluate(IAIAgent self, Vector3 optionPosition, out Vector3 dir, out float dot)
		{
			dir = optionPosition - self.Entity.ServerPosition;
			NPCPlayerApex nPCPlayerApex = self as NPCPlayerApex;
			if (nPCPlayerApex != null)
			{
				if (nPCPlayerApex.ToEnemyRangeEnum(dir.sqrMagnitude) == NPCPlayerApex.EnemyRangeEnum.CloseAttackRange)
				{
					dot = 1f;
					dir.Normalize();
					return true;
				}
				dir.Normalize();
				dot = Vector3.Dot(dir, nPCPlayerApex.eyes.BodyForward());
				if (dot < self.GetStats.VisionCone)
				{
					dot = -1f;
					return false;
				}
			}
			else
			{
				dir.Normalize();
				dot = Vector3.Dot(dir, self.Entity.transform.forward);
				if (dot < self.GetStats.VisionCone)
				{
					dot = -1f;
					return false;
				}
			}
			return true;
		}
	}
}
