using Apex.AI;
using System.Collections.Generic;
using UnityEngine;

namespace Rust.Ai
{
	public class CoverContext : IAIContext
	{
		public IAIAgent Self;

		public Vector3 DangerPoint;

		public List<CoverPoint> SampledCoverPoints;

		public float BestRetreatValue;

		public float BestFlankValue;

		public float BestAdvanceValue;

		public CoverPoint BestRetreatCP;

		public CoverPoint BestFlankCP;

		public CoverPoint BestAdvanceCP;

		public float HideoutValue;

		public CoverPoint HideoutCP;

		public void Refresh(IAIAgent self, Vector3 dangerPoint, List<CoverPoint> sampledCoverPoints)
		{
			Self = self;
			DangerPoint = dangerPoint;
			SampledCoverPoints = sampledCoverPoints;
			BestRetreatValue = 0f;
			BestFlankValue = 0f;
			BestAdvanceValue = 0f;
			BestRetreatCP = null;
			BestFlankCP = null;
			BestAdvanceCP = null;
			HideoutValue = 0f;
			HideoutCP = null;
		}
	}
}
