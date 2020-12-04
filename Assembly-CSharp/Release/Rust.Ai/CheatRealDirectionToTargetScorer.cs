using Apex.AI;
using UnityEngine;

namespace Rust.Ai
{
	public class CheatRealDirectionToTargetScorer : OptionScorerBase<CoverPoint>
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
				Vector3 normalized = (c.DangerPoint - serverPosition).normalized;
				float num = Vector3.Dot((option.Position - serverPosition).normalized, normalized);
				if (num > 0.5f)
				{
					return num;
				}
			}
			return 0f;
		}
	}
}
