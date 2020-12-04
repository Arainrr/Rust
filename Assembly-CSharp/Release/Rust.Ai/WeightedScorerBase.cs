using Apex.AI;
using Apex.Serialization;
using UnityEngine;

namespace Rust.Ai
{
	public abstract class WeightedScorerBase<T> : OptionScorerBase<T>
	{
		[ApexSerialization(defaultValue = false)]
		public bool InvertScore;

		[ApexSerialization(defaultValue = 50f)]
		public float ScoreScale = 50f;

		private string DebugName;

		public WeightedScorerBase()
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
			return s * ScoreScale;
		}

		public override float Score(IAIContext context, T option)
		{
			return ProcessScore(GetScore((BaseContext)context, option));
		}

		public abstract float GetScore(BaseContext context, T option);
	}
}
