using Apex.AI;
using Apex.Serialization;
using System;
using System.Collections.Generic;

namespace Rust.Ai
{
	public class TargetSelectorAnimal : ActionWithOptions<BaseEntity>
	{
		[ApexSerialization]
		private bool allScorersMustScoreAboveZero = true;

		public override void Execute(IAIContext context)
		{
			EntityTargetContext entityTargetContext = context as EntityTargetContext;
			if (entityTargetContext != null)
			{
				Evaluate(entityTargetContext, base.scorers, entityTargetContext.Entities, entityTargetContext.EntityCount, allScorersMustScoreAboveZero, out entityTargetContext.AnimalTarget, out entityTargetContext.AnimalScore);
			}
		}

		public static bool Evaluate(EntityTargetContext context, IList<IOptionScorer<BaseEntity>> scorers, BaseEntity[] options, int numOptions, bool allScorersMustScoreAboveZero, out BaseNpc best, out float bestScore)
		{
			bestScore = float.MinValue;
			best = null;
			BaseEntity baseEntity = null;
			for (int i = 0; i < numOptions; i++)
			{
				float num = 0f;
				bool flag = true;
				for (int j = 0; j < scorers.Count; j++)
				{
					if (!scorers[j].isDisabled)
					{
						float num2 = scorers[j].Score(context, options[i]);
						if (allScorersMustScoreAboveZero && num2 <= 0f)
						{
							flag = false;
							break;
						}
						num += num2;
					}
				}
				if (flag)
				{
					(context.Self.GetContext(Guid.Empty) as BaseContext)?.Memory.Update(options[i], num);
					if (num > bestScore)
					{
						bestScore = num;
						baseEntity = options[i];
					}
				}
			}
			if (baseEntity != null)
			{
				best = (baseEntity as BaseNpc);
			}
			return best != null;
		}
	}
}
