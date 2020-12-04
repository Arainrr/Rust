namespace Rust.Ai
{
	public sealed class TargetAlive : BaseScorer
	{
		public override float GetScore(BaseContext c)
		{
			BaseCombatEntity combatTarget = c.AIAgent.CombatTarget;
			if (!(combatTarget == null) && combatTarget.IsAlive())
			{
				return 1f;
			}
			return 0f;
		}
	}
}
