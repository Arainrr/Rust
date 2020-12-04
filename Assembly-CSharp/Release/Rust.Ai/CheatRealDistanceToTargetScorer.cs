using Apex.AI;
using UnityEngine;

namespace Rust.Ai
{
	public class CheatRealDistanceToTargetScorer : OptionScorerBase<CoverPoint>
	{
		public override float Score(IAIContext context, CoverPoint option)
		{
			return Evaluate(context as CoverContext, option);
		}

		public static float Evaluate(CoverContext c, CoverPoint option)
		{
			if (c != null)
			{
				Vector3 serverPosition = c.Self.Entity.ServerPosition;
				float magnitude = (c.DangerPoint - serverPosition).magnitude;
				float magnitude2 = (option.Position - serverPosition).magnitude;
				if (Mathf.Abs(magnitude - magnitude2) > 8f)
				{
					return 0f;
				}
				return 1f;
			}
			return 0f;
		}
	}
}
