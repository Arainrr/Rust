using Apex.Serialization;
using UnityEngine;

namespace Rust.Ai
{
	public sealed class BeingAimedAt : BaseScorer
	{
		public enum Equality
		{
			Equal,
			LEqual,
			GEqual,
			NEqual,
			Less,
			Greater
		}

		[ApexSerialization]
		public float arc;

		[ApexSerialization]
		public Equality EqualityType;

		public override float GetScore(BaseContext c)
		{
			float num = 0f;
			int num2 = 0;
			foreach (BaseEntity item in c.Memory.Visible)
			{
				BasePlayer basePlayer = item as BasePlayer;
				if (basePlayer != null && !(basePlayer is IAIAgent))
				{
					Vector3 rhs = basePlayer.eyes.BodyForward();
					float num3 = 0f;
					float num4 = Vector3.Dot(c.AIAgent.CurrentAimAngles, rhs);
					switch (EqualityType)
					{
					case Equality.Equal:
						num3 = (Mathf.Approximately(num4, arc) ? 1f : 0f);
						break;
					case Equality.NEqual:
						num3 = (Mathf.Approximately(num4, arc) ? 0f : 1f);
						break;
					case Equality.LEqual:
						num3 = ((num4 <= arc) ? 1f : 0f);
						break;
					case Equality.GEqual:
						num3 = ((num4 >= arc) ? 1f : 0f);
						break;
					case Equality.Less:
						num3 = ((num4 < arc) ? 1f : 0f);
						break;
					case Equality.Greater:
						num3 = ((num4 > arc) ? 1f : 0f);
						break;
					}
					num += num3;
					num2++;
				}
			}
			if (num2 > 0)
			{
				num /= (float)num2;
			}
			return num;
		}
	}
}
