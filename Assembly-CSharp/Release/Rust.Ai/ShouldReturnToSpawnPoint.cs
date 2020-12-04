namespace Rust.Ai
{
	public class ShouldReturnToSpawnPoint : BaseScorer
	{
		public override float GetScore(BaseContext ctx)
		{
			NPCHumanContext nPCHumanContext = ctx as NPCHumanContext;
			if (nPCHumanContext != null && (int)nPCHumanContext.GetFact(NPCPlayerApex.Facts.RangeToSpawnLocation) >= (int)nPCHumanContext.Human.GetStats.MaxRangeToSpawnLoc)
			{
				if (float.IsNaN(nPCHumanContext.Human.SecondsSinceLastInRangeOfSpawnPosition) || float.IsNegativeInfinity(nPCHumanContext.Human.SecondsSinceLastInRangeOfSpawnPosition) || float.IsInfinity(nPCHumanContext.Human.SecondsSinceLastInRangeOfSpawnPosition))
				{
					return 0f;
				}
				return (nPCHumanContext.Human.SecondsSinceLastInRangeOfSpawnPosition >= nPCHumanContext.Human.GetStats.OutOfRangeOfSpawnPointTimeout) ? 1 : 0;
			}
			return 0f;
		}
	}
}
