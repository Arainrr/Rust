using Apex.AI;
using Apex.Serialization;
using UnityEngine;

namespace Rust.Ai
{
	public abstract class BaseScorer : ContextualScorerBase
	{
		[ApexSerialization(defaultValue = false)]
		public bool InvertScore;

		private string DebugName;

		public BaseScorer()
		{
			DebugName = GetType().Name;
		}

		protected float ProcessScore(float s)
		{
			s = Mathf.Clamp01(s);
			if (InvertScore)
			{
				s = 1f - s;
			}
			return s * score;
		}

		public override float Score(IAIContext context)
		{
			return ProcessScore(GetScore((BaseContext)context));
		}

		public abstract float GetScore(BaseContext context);
	}
}
