using Rust.Ai.HTN.Reasoning;

namespace Rust.Ai.HTN.Bear.Reasoners
{
	public class EnemyPlayerLineOfSightReasoner : INpcReasoner
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
			BearContext bearContext = npc.AiDomain.NpcContext as BearContext;
			if (bearContext == null)
			{
				return;
			}
			BearDomain bearDomain = npc.AiDomain as BearDomain;
			if (!(bearDomain == null))
			{
				bearContext.SetFact(Facts.CanSeeEnemy, bearContext.EnemyPlayersInLineOfSight.Count > 0);
				bool isStanding = bearDomain.BearContext.IsFact(Facts.IsStandingUp);
				float num = 0f;
				NpcPlayerInfo primaryEnemyPlayerInLineOfSight = default(NpcPlayerInfo);
				foreach (NpcPlayerInfo item in npc.AiDomain.NpcContext.EnemyPlayersInLineOfSight)
				{
					float num2 = 1f - item.SqrDistance / bearDomain.BearDefinition.SqrAggroRange(isStanding);
					float num3 = (item.ForwardDotDir + 1f) * 0.5f;
					float num4 = num2 * 2f + num3;
					if (num4 > num)
					{
						num = num4;
						primaryEnemyPlayerInLineOfSight = item;
					}
					NpcPlayerInfo info = item;
					info.VisibilityScore = num4;
					bearContext.Memory.RememberEnemyPlayer(npc, ref info, time, 0f, "SEE!");
				}
				bearContext.PrimaryEnemyPlayerInLineOfSight = primaryEnemyPlayerInLineOfSight;
				if (primaryEnemyPlayerInLineOfSight.Player != null && (bearContext.Memory.PrimaryKnownEnemyPlayer.PlayerInfo.Player == null || bearContext.Memory.PrimaryKnownEnemyPlayer.PlayerInfo.AudibleScore < num))
				{
					bearContext.Memory.RememberPrimaryEnemyPlayer(primaryEnemyPlayerInLineOfSight.Player);
					bearContext.IncrementFact(Facts.Alertness, 2);
				}
			}
		}
	}
}
