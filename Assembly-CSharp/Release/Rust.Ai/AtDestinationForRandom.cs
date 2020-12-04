using Apex.Serialization;
using UnityEngine;

namespace Rust.Ai
{
	public class AtDestinationForRandom : BaseScorer
	{
		[ApexSerialization]
		public float MinDuration = 2.5f;

		[ApexSerialization]
		public float MaxDuration = 5f;

		public override float GetScore(BaseContext c)
		{
			if (!(c.AIAgent.TimeAtDestination >= Random.Range(MinDuration, MaxDuration)))
			{
				return 0f;
			}
			return 1f;
		}
	}
}
