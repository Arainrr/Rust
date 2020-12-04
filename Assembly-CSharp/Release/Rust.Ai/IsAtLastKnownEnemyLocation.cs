namespace Rust.Ai
{
	public class IsAtLastKnownEnemyLocation : BaseScorer
	{
		public override float GetScore(BaseContext c)
		{
			return Evaluate(c as NPCHumanContext) ? 1 : 0;
		}

		public static bool Evaluate(NPCHumanContext c)
		{
			if (c.AIAgent.AttackTarget != null && c.AIAgent.IsNavRunning())
			{
				Memory.SeenInfo info = c.Memory.GetInfo(c.AIAgent.AttackTarget);
				if (info.Entity != null)
				{
					return (info.Position - c.Position).sqrMagnitude < 4f;
				}
			}
			return false;
		}
	}
}
