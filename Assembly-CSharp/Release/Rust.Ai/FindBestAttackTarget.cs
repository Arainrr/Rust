using Apex.Serialization;

namespace Rust.Ai
{
	public class FindBestAttackTarget : BaseActionWithOptions<BaseEntity>
	{
		[ApexSerialization]
		public float ScoreThreshold;

		[ApexSerialization]
		public bool AllScorersMustScoreAboveZero;

		public override void DoExecute(BaseContext c)
		{
			BaseEntity best;
			float bestScore;
			if (!TryGetBest(c, c.Memory.Visible, AllScorersMustScoreAboveZero, out best, out bestScore) || bestScore < ScoreThreshold)
			{
				NPCHumanContext nPCHumanContext = c as NPCHumanContext;
				if (nPCHumanContext != null && c.AIAgent.GetWantsToAttack(nPCHumanContext.LastAttacker) > 0f)
				{
					c.AIAgent.AttackTarget = nPCHumanContext.LastAttacker;
				}
				else
				{
					c.AIAgent.AttackTarget = null;
				}
			}
			else
			{
				if (c.AIAgent.GetWantsToAttack(best) < 0.1f)
				{
					best = null;
				}
				c.AIAgent.AttackTarget = best;
			}
			if (c.AIAgent.AttackTarget != null)
			{
				foreach (Memory.SeenInfo item in c.Memory.All)
				{
					if (item.Entity == best)
					{
						c.AIAgent.AttackTargetMemory = item;
						break;
					}
				}
			}
		}
	}
}
