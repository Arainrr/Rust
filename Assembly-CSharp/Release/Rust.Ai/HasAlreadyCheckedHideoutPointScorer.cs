using Apex.AI;

namespace Rust.Ai
{
	public class HasAlreadyCheckedHideoutPointScorer : OptionScorerBase<CoverPoint>
	{
		public override float Score(IAIContext context, CoverPoint option)
		{
			return Evaluate(context as CoverContext, option);
		}

		public static float Evaluate(CoverContext c, CoverPoint option)
		{
			if (c != null)
			{
				NPCPlayerApex nPCPlayerApex = c.Self.Entity as NPCPlayerApex;
				if (nPCPlayerApex != null && !nPCPlayerApex.AiContext.HasCheckedHideout(option))
				{
					return 1f;
				}
			}
			return 0f;
		}
	}
}
