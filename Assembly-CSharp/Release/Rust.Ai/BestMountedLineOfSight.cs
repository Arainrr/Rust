using Apex.AI;
using Apex.Serialization;

namespace Rust.Ai
{
	public class BestMountedLineOfSight : OptionScorerBase<BasePlayer>
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
					byte b = Evaluate(nPCPlayerApex, option);
					playerTargetContext.LineOfSight[playerTargetContext.CurrentOptionsIndex] = b;
					return (float)(int)b * score;
				}
			}
			playerTargetContext.LineOfSight[playerTargetContext.CurrentOptionsIndex] = 0;
			return 0f;
		}

		public static byte Evaluate(NPCPlayerApex self, BasePlayer option)
		{
			return (byte)(self.IsVisibleMounted(option) ? 1 : 0);
		}
	}
}
