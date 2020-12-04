using Apex.Serialization;
using UnityEngine;

namespace Rust.Ai
{
	public class HumanAttackOperator : BaseAction
	{
		[ApexSerialization]
		public AttackOperator.AttackType Type;

		[ApexSerialization]
		public AttackOperator.AttackTargetType Target;

		public override void DoExecute(BaseContext c)
		{
			if (Target == AttackOperator.AttackTargetType.Enemy)
			{
				AttackEnemy(c as NPCHumanContext, Type);
			}
		}

		public static void AttackEnemy(NPCHumanContext c, AttackOperator.AttackType type)
		{
			if (c.GetFact(NPCPlayerApex.Facts.IsWeaponAttackReady) == 0)
			{
				return;
			}
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
				c.SetFact(NPCPlayerApex.Facts.IsWeaponAttackReady, 0);
				if (Random.value < 0.1f && c.Human.OnAggro != null)
				{
					c.Human.OnAggro();
				}
			}
		}
	}
}
