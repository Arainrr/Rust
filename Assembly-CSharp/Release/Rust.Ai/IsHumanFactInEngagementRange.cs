using Apex.Serialization;

namespace Rust.Ai
{
	public class IsHumanFactInEngagementRange : BaseScorer
	{
		[ApexSerialization(defaultValue = NPCPlayerApex.EnemyEngagementRangeEnum.AggroRange)]
		public NPCPlayerApex.EnemyEngagementRangeEnum value;

		public override float GetScore(BaseContext c)
		{
			if ((uint)c.GetFact(NPCPlayerApex.Facts.EnemyEngagementRange) != (uint)value)
			{
				return 0f;
			}
			return 1f;
		}
	}
}
