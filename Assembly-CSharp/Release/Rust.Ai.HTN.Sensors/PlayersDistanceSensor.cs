using System;
using System.Collections.Generic;

namespace Rust.Ai.HTN.Sensors
{
	[Serializable]
	public class PlayersDistanceSensor : INpcSensor
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
			List<NpcPlayerInfo> playersInRange = npc.AiDomain.NpcContext.PlayersInRange;
			for (int i = 0; i < playersInRange.Count; i++)
			{
				NpcPlayerInfo value = playersInRange[i];
				if (value.Player == null || value.Player.transform == null || value.Player.IsDestroyed || value.Player.IsDead() || value.Player.IsWounded())
				{
					playersInRange.RemoveAt(i);
					i--;
				}
				else
				{
					value.SqrDistance = (npc.transform.position - value.Player.transform.position).sqrMagnitude;
					playersInRange[i] = value;
				}
			}
		}
	}
}
