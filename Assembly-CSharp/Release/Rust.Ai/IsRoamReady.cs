namespace Rust.Ai
{
	public class IsRoamReady : BaseScorer
	{
		public override float GetScore(BaseContext c)
		{
			return Evaluate(c) ? 1 : 0;
		}

		public static bool Evaluate(BaseContext c)
		{
			if (c is NPCHumanContext)
			{
				return c.GetFact(NPCPlayerApex.Facts.IsRoamReady) > 0;
			}
			return c.GetFact(BaseNpc.Facts.IsRoamReady) > 0;
		}
	}
}
