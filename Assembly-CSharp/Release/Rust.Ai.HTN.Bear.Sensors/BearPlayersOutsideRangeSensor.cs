using ConVar;
using Rust.Ai.HTN.Sensors;
using System;
using System.Collections.Generic;

namespace Rust.Ai.HTN.Bear.Sensors
{
	[Serializable]
	public class BearPlayersOutsideRangeSensor : INpcSensor
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
			for (int i = 0; i < npcContext.PlayersOutsideDetectionRange.Count; i++)
			{
				NpcPlayerInfo player = npcContext.PlayersOutsideDetectionRange[i];
				if (npcContext.BaseMemory.ShouldRemoveOnPlayerForgetTimeout(time, player))
				{
					npcContext.PlayersOutsideDetectionRange.RemoveAt(i);
					i--;
				}
				else
				{
					EvaluatePlayer(npcContext, npc, ref player, time);
				}
			}
		}

		protected virtual bool EvaluatePlayer(BaseNpcContext context, IHTNAgent npc, ref NpcPlayerInfo player, float time)
		{
			if (player.Player.Family == npc.Family)
			{
				return false;
			}
			List<NpcPlayerInfo> playersInRange = npc.AiDomain.NpcContext.PlayersInRange;
			bool flag = false;
			for (int i = 0; i < playersInRange.Count; i++)
			{
				if (playersInRange[i].Player == player.Player)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				playersInRange.Add(player);
			}
			return true;
		}
	}
}
