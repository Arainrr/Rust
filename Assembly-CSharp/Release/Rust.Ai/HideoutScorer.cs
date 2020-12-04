using Apex.AI;
using Apex.Serialization;
using UnityEngine;

namespace Rust.Ai
{
	public class HideoutScorer : OptionScorerBase<CoverPoint>
	{
		[ApexSerialization]
		[Range(-1f, 1f)]
		public float coverFromPointArcThreshold = -0.8f;

		[ApexSerialization]
		public float maxRange = 5f;

		public override float Score(IAIContext context, CoverPoint option)
		{
			return Evaluate(context as CoverContext, option, coverFromPointArcThreshold, maxRange);
		}

		public static float Evaluate(CoverContext c, CoverPoint option, float arcThreshold, float maxRange)
		{
			if (c != null)
			{
				Vector3 serverPosition = c.Self.Entity.ServerPosition;
				if (option.ProvidesCoverFromPoint(serverPosition, arcThreshold))
				{
					float sqrMagnitude = (option.Position - c.DangerPoint).sqrMagnitude;
					float num = maxRange * maxRange;
					return 1f - Mathf.Min(sqrMagnitude, num) / num;
				}
			}
			return 0f;
		}
	}
}
