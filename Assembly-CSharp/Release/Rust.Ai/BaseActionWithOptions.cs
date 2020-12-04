using Apex.AI;
using System.Collections.Generic;

namespace Rust.Ai
{
	public abstract class BaseActionWithOptions<T> : ActionWithOptions<T>
	{
		private string DebugName;

		public BaseActionWithOptions()
		{
			DebugName = GetType().Name;
		}

		public override void Execute(IAIContext context)
		{
			BaseContext baseContext = context as BaseContext;
			if (baseContext != null)
			{
				DoExecute(baseContext);
			}
		}

		public abstract void DoExecute(BaseContext context);

		public bool TryGetBest(BaseContext context, IList<T> options, bool allScorersMustScoreAboveZero, out T best, out float bestScore)
		{
			bestScore = float.MinValue;
			best = default(T);
			for (int i = 0; i < options.Count; i++)
			{
				float num = 0f;
				bool flag = true;
				for (int j = 0; j < base.scorers.Count; j++)
				{
					if (!base.scorers[j].isDisabled)
					{
						float num2 = base.scorers[j].Score(context, options[i]);
						if (allScorersMustScoreAboveZero && num2 <= 0f)
						{
							flag = false;
							break;
						}
						num += num2;
					}
				}
				if (flag && num > bestScore)
				{
					bestScore = num;
					best = options[i];
				}
			}
			return best != null;
		}
	}
}
