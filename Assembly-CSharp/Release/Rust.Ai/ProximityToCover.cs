using Apex.Serialization;
using UnityEngine;

namespace Rust.Ai
{
	public class ProximityToCover : WeightedScorerBase<Vector3>
	{
		public enum CoverType
		{
			All,
			Full,
			Partial
		}

		[ApexSerialization]
		public float MaxDistance = 20f;

		[ApexSerialization]
		public CoverType _coverType;

		[ApexSerialization]
		public AnimationCurve Response = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

		public override float GetScore(BaseContext ctx, Vector3 option)
		{
			NPCHumanContext nPCHumanContext = ctx as NPCHumanContext;
			if (nPCHumanContext != null)
			{
				float bestDistance;
				CoverPoint closestCover = GetClosestCover(nPCHumanContext, option, MaxDistance, _coverType, out bestDistance);
				if (closestCover != null)
				{
					return Response.Evaluate(bestDistance / MaxDistance) * closestCover.Score;
				}
			}
			return 0f;
		}

		internal static CoverPoint GetClosestCover(NPCHumanContext c, Vector3 point, float MaxDistance, CoverType _coverType, out float bestDistance)
		{
			bestDistance = MaxDistance;
			CoverPoint result = null;
			for (int i = 0; i < c.sampledCoverPoints.Count; i++)
			{
				CoverPoint coverPoint = c.sampledCoverPoints[i];
				CoverPoint.CoverType coverType = c.sampledCoverPointTypes[i];
				if ((_coverType != CoverType.Full || coverType == CoverPoint.CoverType.Full) && (_coverType != CoverType.Partial || coverType == CoverPoint.CoverType.Partial))
				{
					float num = Vector3.Distance(coverPoint.Position, point);
					if (num < bestDistance)
					{
						bestDistance = num;
						result = coverPoint;
					}
				}
			}
			return result;
		}
	}
}
