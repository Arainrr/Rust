using Apex.Serialization;

namespace Rust.Ai
{
	public class IsMountedToType : BaseScorer
	{
		[ApexSerialization]
		public PlayerModel.MountPoses MountableType;

		public override float GetScore(BaseContext context)
		{
			return Test(context as NPCHumanContext, MountableType);
		}

		public static float Test(NPCHumanContext c, PlayerModel.MountPoses mountableType)
		{
			BaseMountable mounted = c.Human.GetMounted();
			if (mounted == null)
			{
				return 0f;
			}
			if (mounted.mountPose != mountableType)
			{
				return 0f;
			}
			return 1f;
		}
	}
}
