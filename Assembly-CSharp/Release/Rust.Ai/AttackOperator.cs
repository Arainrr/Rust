using Apex.Serialization;

namespace Rust.Ai
{
	public class AttackOperator : BaseAction
	{
		public enum AttackType
		{
			CloseRange,
			MediumRange,
			LongRange
		}

		public enum AttackTargetType
		{
			Enemy
		}

		[ApexSerialization]
		public AttackType Type;

		[ApexSerialization]
		public AttackTargetType Target;

		public override void DoExecute(BaseContext c)
		{
			if (Target == AttackTargetType.Enemy)
			{
				AttackEnemy(c, Type);
			}
		}

		public static void AttackEnemy(BaseContext c, AttackType type)
		{
			if (c.GetFact(BaseNpc.Facts.IsAttackReady) != 0)
			{
				BaseCombatEntity baseCombatEntity = null;
				if (c.EnemyNpc != null)
				{
					baseCombatEntity = c.EnemyNpc;
				}
				if (c.EnemyPlayer != null)
				{
					baseCombatEntity = c.EnemyPlayer;
				}
				if (!(baseCombatEntity == null))
				{
					c.AIAgent.StartAttack(type, baseCombatEntity);
					c.SetFact(BaseNpc.Facts.IsAttackReady, 0);
				}
			}
		}
	}
}
