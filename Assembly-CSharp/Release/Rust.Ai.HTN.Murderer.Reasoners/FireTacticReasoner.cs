using Rust.Ai.HTN.Reasoning;

namespace Rust.Ai.HTN.Murderer.Reasoners
{
	public class FireTacticReasoner : INpcReasoner
	{
		public float TickFrequency
		{
			get;
			set;
		}

		public float LastTickTime
		{
			get;
			set;
		}

		public void Tick(IHTNAgent npc, float deltaTime, float time)
		{
			MurdererContext murdererContext = npc.AiDomain.NpcContext as MurdererContext;
			if (murdererContext == null)
			{
				return;
			}
			HTNPlayer hTNPlayer = npc as HTNPlayer;
			if (hTNPlayer == null)
			{
				return;
			}
			FireTactic value = FireTactic.Single;
			AttackEntity attackEntity = hTNPlayer.GetHeldEntity() as AttackEntity;
			if ((bool)attackEntity)
			{
				BaseProjectile ent = attackEntity as BaseProjectile;
				float num = float.MaxValue;
				if (murdererContext.PrimaryEnemyPlayerInLineOfSight.Player != null)
				{
					num = murdererContext.PrimaryEnemyPlayerInLineOfSight.SqrDistance;
				}
				value = ((attackEntity.attackLengthMin >= 0f && num <= murdererContext.Body.AiDefinition.Engagement.SqrCloseRangeFirearm(ent)) ? FireTactic.FullAuto : ((!(attackEntity.attackLengthMin >= 0f) || !(num <= murdererContext.Body.AiDefinition.Engagement.SqrMediumRangeFirearm(ent))) ? FireTactic.Single : FireTactic.Burst));
			}
			murdererContext.SetFact(Facts.FireTactic, value);
		}
	}
}
