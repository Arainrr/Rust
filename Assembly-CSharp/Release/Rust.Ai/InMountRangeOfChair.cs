using UnityEngine;

namespace Rust.Ai
{
	public class InMountRangeOfChair : BaseScorer
	{
		public override float GetScore(BaseContext context)
		{
			return Test(context as NPCHumanContext);
		}

		public static float Test(NPCHumanContext c)
		{
			if (!(c.ChairTarget != null))
			{
				return 0f;
			}
			return IsInRange(c, c.ChairTarget);
		}

		private static float IsInRange(NPCHumanContext c, BaseMountable mountable)
		{
			Vector3 vector = mountable.transform.position - c.Position;
			if (vector.y > mountable.maxMountDistance)
			{
				vector.y -= mountable.maxMountDistance;
			}
			if (vector.sqrMagnitude <= mountable.maxMountDistance * mountable.maxMountDistance)
			{
				return 1f;
			}
			return 0f;
		}
	}
}
