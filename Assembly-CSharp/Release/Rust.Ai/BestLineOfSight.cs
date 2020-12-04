using Apex.AI;
using Apex.Serialization;

namespace Rust.Ai
{
	public class BestLineOfSight : OptionScorerBase<BasePlayer>
	{
		[ApexSerialization]
		private float score = 10f;

		public override float Score(IAIContext context, BasePlayer option)
		{
			PlayerTargetContext playerTargetContext = context as PlayerTargetContext;
			if (playerTargetContext != null)
			{
				NPCPlayerApex nPCPlayerApex = playerTargetContext.Self as NPCPlayerApex;
				if ((bool)nPCPlayerApex)
				{
					int standing;
					int crouched;
					playerTargetContext.LineOfSight[playerTargetContext.CurrentOptionsIndex] = Evaluate(nPCPlayerApex, option, out standing, out crouched);
					return (float)(standing + crouched) * 0.5f * score;
				}
			}
			playerTargetContext.LineOfSight[playerTargetContext.CurrentOptionsIndex] = 0;
			return 0f;
		}

		public static byte Evaluate(NPCPlayerApex self, BasePlayer option, out int standing, out int crouched)
		{
			standing = (self.IsVisibleStanding(option) ? 1 : 0);
			crouched = (self.IsVisibleCrouched(option) ? 1 : 0);
			return (byte)(standing + crouched * 2);
		}
	}
}
