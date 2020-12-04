using ConVar;
using System;
using System.Collections.Generic;

namespace Rust.Ai.HTN.Sensors
{
	[Serializable]
	public class EnemyPlayersInRangeSensor : INpcSensor
	{
		public class EnemyPlayerInRangeComparer : IComparer<NpcPlayerInfo>
		{
			public int Compare(NpcPlayerInfo a, NpcPlayerInfo b)
			{
				if (a.Player == null || b.Player == null)
				{
					return 0;
				}
				if (a.SqrDistance < 0.01f)
				{
					return -1;
				}
				if (a.SqrDistance < b.SqrDistance)
				{
					return -1;
				}
				if (a.SqrDistance > b.SqrDistance)
				{
					return 1;
				}
				return 0;
			}
		}

		private static EnemyPlayerInRangeComparer _comparer = new EnemyPlayerInRangeComparer();

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
			npcContext.EnemyPlayersInRange.Sort(_comparer);
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
