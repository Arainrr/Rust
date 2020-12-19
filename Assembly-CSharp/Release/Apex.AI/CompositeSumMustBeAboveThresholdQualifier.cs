using Apex.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace Apex.AI
{
	[FriendlyName("Sum must be above threshold", "Scores 0 if sum is below threshold.")]
	[AICategory("Composite Qualifiers")]
	public class CompositeSumMustBeAboveThresholdQualifier : CompositeQualifier
	{
		[ApexSerialization(defaultValue = 0f)]
		public float threshold;

		public sealed override float Score(IAIContext context, IList<IContextualScorer> scorers)
		{
			float num = 0f;
			int count = scorers.Count;
			for (int i = 0; i < count; i++)
			{
				IContextualScorer contextualScorer = scorers[i];
				if (contextualScorer.isDisabled)
				{
					continue;
				}
				float num2 = contextualScorer.Score(context);
				if (num2 < 0f)
				{
					Debug.LogWarning("SumMustBeAboveThreshold scorer does not support scores below 0!");
					continue;
				}
				num += num2;
				if (num > threshold)
				{
					break;
				}
			}
			if (num <= threshold)
			{
				return 0f;
			}
			return num;
		}
	}
}
