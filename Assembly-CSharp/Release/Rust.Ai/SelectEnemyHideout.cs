using Apex.AI;
using Apex.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace Rust.Ai
{
	public class SelectEnemyHideout : ActionWithOptions<CoverPoint>
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
				if (num > context.HideoutValue)
				{
					context.HideoutCP = options[i];
				}
			}
			if (context.HideoutCP != null)
			{
				NPCPlayerApex nPCPlayerApex = context.Self.Entity as NPCPlayerApex;
				if (nPCPlayerApex != null)
				{
					nPCPlayerApex.AiContext.CheckedHideoutPoints.Add(new NPCHumanContext.HideoutPoint
					{
						Hideout = context.HideoutCP,
						Time = Time.time
					});
				}
				return true;
			}
			return false;
		}
	}
}
