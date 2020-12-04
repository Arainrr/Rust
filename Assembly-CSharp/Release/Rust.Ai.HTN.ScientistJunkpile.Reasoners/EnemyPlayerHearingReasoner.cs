using Rust.Ai.HTN.Reasoning;
using UnityEngine;

namespace Rust.Ai.HTN.ScientistJunkpile.Reasoners
{
	public class EnemyPlayerHearingReasoner : INpcReasoner
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
			ScientistJunkpileContext scientistJunkpileContext = npc.AiDomain.NpcContext as ScientistJunkpileContext;
			if (scientistJunkpileContext != null)
			{
				scientistJunkpileContext.SetFact(Facts.CanHearEnemy, scientistJunkpileContext.EnemyPlayersAudible.Count > 0);
				float num = 0f;
				NpcPlayerInfo primaryEnemyPlayerAudible = default(NpcPlayerInfo);
				foreach (NpcPlayerInfo item in npc.AiDomain.NpcContext.EnemyPlayersAudible)
				{
					if (scientistJunkpileContext.Memory.MarkedEnemies.Contains(item.Player) && !(item.SqrDistance > npc.AiDefinition.Sensory.SqrHearingRange))
					{
						float num2 = 1f - Mathf.Min(1f, item.SqrDistance / npc.AiDefinition.Sensory.SqrHearingRange);
						float num3 = num2 * 2f;
						if (num3 > num)
						{
							num = num3;
							primaryEnemyPlayerAudible = item;
						}
						NpcPlayerInfo info = item;
						info.AudibleScore = num3;
						scientistJunkpileContext.Memory.RememberEnemyPlayer(npc, ref info, time, (1f - num2) * 20f, "SOUND!");
					}
				}
				scientistJunkpileContext.PrimaryEnemyPlayerAudible = primaryEnemyPlayerAudible;
				if (primaryEnemyPlayerAudible.Player != null && (scientistJunkpileContext.Memory.PrimaryKnownEnemyPlayer.PlayerInfo.Player == null || scientistJunkpileContext.Memory.PrimaryKnownEnemyPlayer.PlayerInfo.VisibilityScore < num))
				{
					scientistJunkpileContext.Memory.RememberPrimaryEnemyPlayer(primaryEnemyPlayerAudible.Player);
					scientistJunkpileContext.IncrementFact(Facts.Alertness, 1);
				}
			}
		}
	}
}
