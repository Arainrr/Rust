using ConVar;
using Rust.Ai.HTN.Reasoning;
using UnityEngine;

namespace Rust.Ai.HTN.ScientistJunkpile.Reasoners
{
	public class EnemyPlayerMarkTooCloseReasoner : INpcReasoner
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
			if (scientistJunkpileContext == null)
			{
				return;
			}
			float num = ConVar.AI.npc_junkpile_dist_aggro_gate * ConVar.AI.npc_junkpile_dist_aggro_gate;
			for (int i = 0; i < npc.AiDomain.NpcContext.EnemyPlayersInRange.Count; i++)
			{
				NpcPlayerInfo value = npc.AiDomain.NpcContext.EnemyPlayersInRange[i];
				if (!(value.Player == null) && !(value.Player.transform == null))
				{
					if (Mathf.Approximately(value.SqrDistance, 0f))
					{
						value.SqrDistance = (value.Player.transform.position - npc.BodyPosition).sqrMagnitude;
						npc.AiDomain.NpcContext.EnemyPlayersInRange[i] = value;
					}
					if (value.SqrDistance < num)
					{
						scientistJunkpileContext.Memory.MarkEnemy(value.Player);
					}
				}
			}
		}
	}
}
