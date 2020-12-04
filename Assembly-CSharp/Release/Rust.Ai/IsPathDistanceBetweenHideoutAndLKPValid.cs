using Apex.AI;

namespace Rust.Ai
{
	public class IsPathDistanceBetweenHideoutAndLKPValid : OptionScorerBase<CoverPoint>
	{
		public override float Score(IAIContext context, CoverPoint option)
		{
			if (!Evaluate(context as CoverContext, option))
			{
				return 0f;
			}
			return 1f;
		}

		public static bool Evaluate(CoverContext c, CoverPoint option)
		{
			if (c == null || c.Self.AttackTarget == null)
			{
				return false;
			}
			NPCPlayerApex nPCPlayerApex = c.Self.Entity as NPCPlayerApex;
			if (nPCPlayerApex == null)
			{
				return false;
			}
			return nPCPlayerApex.PathDistanceIsValid(option.Position, c.DangerPoint, true);
		}
	}
}
