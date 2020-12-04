using Apex.Serialization;

namespace Rust.Ai
{
	public class HasHumanFactEnemyRange : BaseScorer
	{
		[ApexSerialization(defaultValue = NPCPlayerApex.EnemyRangeEnum.CloseAttackRange)]
		public NPCPlayerApex.EnemyRangeEnum value;

		public override float GetScore(BaseContext c)
		{
			if ((uint)c.GetFact(NPCPlayerApex.Facts.EnemyRange) != (uint)value)
			{
				return 0f;
			}
			return 1f;
		}
	}
}
