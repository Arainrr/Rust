using Apex.AI;
using Apex.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace Rust.Ai
{
	public class AverageSumOfChildren : CompositeQualifier
	{
		[ApexSerialization]
		private bool normalize = true;

		[ApexSerialization]
		private float postNormalizeMultiplier = 1f;

		[ApexSerialization]
		private float MaxAverageScore = 100f;

		[ApexSerialization]
		private bool FailIfAnyScoreZero = true;

		public override float Score(IAIContext context, IList<IContextualScorer> scorers)
		{
			if (scorers.Count == 0)
			{
				return 0f;
			}
			float num = 0f;
			for (int i = 0; i < scorers.Count; i++)
			{
				float num2 = scorers[i].Score(context);
				if (FailIfAnyScoreZero && (num2 < 0f || Mathf.Approximately(num2, 0f)))
				{
					return 0f;
				}
				num += num2;
			}
			num /= (float)scorers.Count;
			if (normalize)
			{
				num /= MaxAverageScore;
				return num * postNormalizeMultiplier;
			}
			return num;
		}
	}
}
