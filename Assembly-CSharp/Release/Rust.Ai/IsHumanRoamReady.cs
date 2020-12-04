namespace Rust.Ai
{
	public class IsHumanRoamReady : BaseScorer
	{
		public override float GetScore(BaseContext c)
		{
			return Evaluate(c as NPCHumanContext) ? 1 : 0;
		}

		public static bool Evaluate(NPCHumanContext c)
		{
			return c.GetFact(NPCPlayerApex.Facts.IsRoamReady) > 0;
		}
	}
}
