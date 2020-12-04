namespace Rust.Ai
{
	public class IsAtSpawnLocation : BaseScorer
	{
		public override float GetScore(BaseContext c)
		{
			return Evaluate(c as NPCHumanContext) ? 1 : 0;
		}

		public static bool Evaluate(NPCHumanContext c)
		{
			if (c.AIAgent.IsNavRunning())
			{
				return (c.Human.SpawnPosition - c.Position).sqrMagnitude < 4f;
			}
			return false;
		}
	}
}
