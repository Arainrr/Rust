using Apex.Serialization;
using UnityEngine;

namespace Rust.Ai
{
	public sealed class LineOfSightToTargetEntity : BaseScorer
	{
		[ApexSerialization]
		private CoverPoint.CoverType Cover;

		public override float GetScore(BaseContext c)
		{
			if (c.AIAgent.AttackTarget == null)
			{
				return 0f;
			}
			BasePlayer basePlayer = c.AIAgent.AttackTarget as BasePlayer;
			if ((bool)basePlayer)
			{
				Vector3 attackPosition = c.AIAgent.AttackPosition;
				if (!basePlayer.IsVisible(attackPosition, basePlayer.CenterPoint()) && !basePlayer.IsVisible(attackPosition, basePlayer.eyes.position) && !basePlayer.IsVisible(attackPosition, basePlayer.transform.position))
				{
					return 0f;
				}
				return 1f;
			}
			if (Cover == CoverPoint.CoverType.Full)
			{
				if (!c.AIAgent.AttackTarget.IsVisible(c.AIAgent.AttackPosition))
				{
					return 0f;
				}
				return 1f;
			}
			if (!c.AIAgent.AttackTarget.IsVisible(c.AIAgent.CrouchedAttackPosition))
			{
				return 0f;
			}
			return 1f;
		}
	}
}
