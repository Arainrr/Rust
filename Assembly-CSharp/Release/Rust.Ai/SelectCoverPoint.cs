using Apex.AI;
using Apex.Serialization;
using System.Collections.Generic;

namespace Rust.Ai
{
	public class SelectCoverPoint : ActionWithOptions<CoverPoint>
	{
		[ApexSerialization]
		private bool allScorersMustScoreAboveZero = true;

		public override void Execute(IAIContext context)
		{
			CoverContext coverContext = context as CoverContext;
			if (coverContext != null)
			{
				Evaluate(coverContext, base.scorers, coverContext.SampledCoverPoints, coverContext.SampledCoverPoints.Count, allScorersMustScoreAboveZero);
			}
		}

		public static bool Evaluate(CoverContext context, IList<IOptionScorer<CoverPoint>> scorers, List<CoverPoint> options, int numOptions, bool allScorersMustScoreAboveZero)
		{
			for (int i = 0; i < numOptions; i++)
			{
				float num = 0f;
				for (int j = 0; j < scorers.Count; j++)
				{
					if (!scorers[j].isDisabled)
					{
						float num2 = scorers[j].Score(context, options[i]);
						if (allScorersMustScoreAboveZero && num2 <= 0f)
						{
							break;
						}
						num += num2;
					}
				}
			}
			if (context.BestAdvanceCP == null && context.BestFlankCP == null)
			{
				return context.BestRetreatCP != null;
			}
			return true;
		}
	}
}
