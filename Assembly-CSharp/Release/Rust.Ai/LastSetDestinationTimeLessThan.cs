using Apex.Serialization;
using UnityEngine;

namespace Rust.Ai
{
	public class LastSetDestinationTimeLessThan : BaseScorer
	{
		[ApexSerialization]
		private float Timeout = 5f;

		public override float GetScore(BaseContext c)
		{
			BaseNpc baseNpc = c.AIAgent as BaseNpc;
			if (baseNpc != null && (Mathf.Approximately(baseNpc.LastSetDestinationTime, 0f) || baseNpc.SecondsSinceLastSetDestination < Timeout))
			{
				return 1f;
			}
			return 0f;
		}
	}
}
