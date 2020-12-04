using Apex.AI;
using Apex.Serialization;

namespace Rust.Ai
{
	public class IsAtLocationPlayerTargeting : ContextualScorerBase<PlayerTargetContext>
	{
		[ApexSerialization]
		public AiLocationSpawner.SquadSpawnerLocation Location;

		public override float Score(PlayerTargetContext c)
		{
			if (!Test(c, Location))
			{
				return 0f;
			}
			return score;
		}

		public static bool Test(PlayerTargetContext c, AiLocationSpawner.SquadSpawnerLocation location)
		{
			NPCPlayerApex nPCPlayerApex = c.Self as NPCPlayerApex;
			if (nPCPlayerApex != null)
			{
				if (nPCPlayerApex.AiContext.AiLocationManager != null)
				{
					return nPCPlayerApex.AiContext.AiLocationManager.LocationType == location;
				}
				return false;
			}
			return false;
		}
	}
}
