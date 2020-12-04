using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rust.Ai.HTN.Sensors
{
	[Serializable]
	public class PlayersViewAngleSensor : INpcSensor
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
					Vector3 normalized = (npc.transform.position - value.Player.transform.position).normalized;
					value.ForwardDotDir = Vector3.Dot(-npc.transform.forward, normalized);
					playersInRange[i] = value;
				}
			}
		}
	}
}
