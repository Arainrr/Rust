using Apex.Serialization;

namespace Rust.Ai
{
	public class HasFactEnemyRange : BaseScorer
	{
		[ApexSerialization(defaultValue = BaseNpc.EnemyRangeEnum.AttackRange)]
		public BaseNpc.EnemyRangeEnum value;

		public override float GetScore(BaseContext c)
		{
			if ((uint)c.GetFact(BaseNpc.Facts.EnemyRange) != (uint)value)
			{
				return 0f;
			}
			return 1f;
		}
	}
}
