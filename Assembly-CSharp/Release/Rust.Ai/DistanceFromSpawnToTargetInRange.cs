using Apex.Serialization;

namespace Rust.Ai
{
	public class DistanceFromSpawnToTargetInRange : BaseScorer
	{
		[ApexSerialization]
		private NPCPlayerApex.EnemyRangeEnum range;

		public override float GetScore(BaseContext c)
		{
			return Evaluate(c as NPCHumanContext, range) ? 1 : 0;
		}

		public static bool Evaluate(NPCHumanContext c, NPCPlayerApex.EnemyRangeEnum range)
		{
			if (c == null || c.Human.AttackTarget == null)
			{
				return false;
			}
			Memory.SeenInfo info = c.Memory.GetInfo(c.Human.AttackTarget);
			if (info.Entity == null)
			{
				return false;
			}
			float sqrMagnitude = (info.Position - c.Human.SpawnPosition).sqrMagnitude;
			NPCPlayerApex.EnemyRangeEnum enemyRangeEnum = c.Human.ToEnemyRangeEnum(sqrMagnitude);
			if (enemyRangeEnum == range)
			{
				return true;
			}
			if ((int)enemyRangeEnum < (int)range)
			{
				return true;
			}
			return false;
		}
	}
}
