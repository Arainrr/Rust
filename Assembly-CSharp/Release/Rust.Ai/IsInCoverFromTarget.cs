using Apex.Serialization;

namespace Rust.Ai
{
	public class IsInCoverFromTarget : BaseScorer
	{
		[ApexSerialization]
		public float CoverArcThreshold = -0.75f;

		public override float GetScore(BaseContext ctx)
		{
			if (SingletonComponent<AiManager>.Instance == null || !SingletonComponent<AiManager>.Instance.enabled || !SingletonComponent<AiManager>.Instance.UseCover || ctx.AIAgent.AttackTarget == null)
			{
				return 0f;
			}
			bool flag = ctx is NPCHumanContext;
			return 0f;
		}
	}
}
