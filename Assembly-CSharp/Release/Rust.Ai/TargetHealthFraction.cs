namespace Rust.Ai
{
	public sealed class TargetHealthFraction : BaseScorer
	{
		public override float GetScore(BaseContext c)
		{
			BaseCombatEntity combatTarget = c.AIAgent.CombatTarget;
			if (!(combatTarget == null))
			{
				return combatTarget.healthFraction;
			}
			return 0f;
		}
	}
}
