namespace Rust.Ai
{
	public class IsPathDistanceToTargetValid : BaseScorer
	{
		public override float GetScore(BaseContext c)
		{
			return Evaluate(c as NPCHumanContext) ? 1 : 0;
		}

		public static bool Evaluate(NPCHumanContext c)
		{
			if (c == null || c.Human.AttackTarget == null)
			{
				return false;
			}
			return c.Human.PathDistanceIsValid(c.Human.ServerPosition, c.Human.AttackTarget.ServerPosition);
		}
	}
}
