using ConVar;
using Rust.Ai.HTN.Sensors;
using System;

namespace Rust.Ai.HTN.Bear.Sensors
{
	[Serializable]
	public class BearEnemyPlayersInRangeSensor : INpcSensor
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
			if (ConVar.AI.ignoreplayers)
			{
				return;
			}
			BaseNpcContext npcContext = npc.AiDomain.NpcContext;
			npcContext.EnemyPlayersInRange.Clear();
			for (int i = 0; i < npcContext.PlayersInRange.Count; i++)
			{
				NpcPlayerInfo player = npcContext.PlayersInRange[i];
				if (npcContext.BaseMemory.ShouldRemoveOnPlayerForgetTimeout(time, player))
				{
					npcContext.PlayersInRange.RemoveAt(i);
					i--;
				}
				else
				{
					EvaluatePlayer(npcContext, npc, player, time);
				}
			}
		}

		protected virtual bool EvaluatePlayer(BaseNpcContext context, IHTNAgent npc, NpcPlayerInfo player, float time)
		{
			if (player.Player.Family == npc.Family)
			{
				return false;
			}
			context.EnemyPlayersInRange.Add(player);
			return true;
		}
	}
}
