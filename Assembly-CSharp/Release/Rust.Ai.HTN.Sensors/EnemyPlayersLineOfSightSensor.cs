using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rust.Ai.HTN.Sensors
{
	[Serializable]
	public class EnemyPlayersLineOfSightSensor : INpcSensor
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

		public int MaxVisible
		{
			get;
			set;
		}

		public void Tick(IHTNAgent npc, float deltaTime, float time)
		{
			TickStatic(npc, (MaxVisible > 0) ? MaxVisible : 3);
		}

		public static void TickStatic(IHTNAgent npc, int maxVisible = 3)
		{
			npc.AiDomain.NpcContext.EnemyPlayersInLineOfSight.Clear();
			int num = 0;
			List<NpcPlayerInfo> enemyPlayersInRange = npc.AiDomain.NpcContext.EnemyPlayersInRange;
			for (int i = 0; i < enemyPlayersInRange.Count; i++)
			{
				NpcPlayerInfo info = enemyPlayersInRange[i];
				if (info.Player == null || info.Player.transform == null || info.Player.IsDestroyed || info.Player.IsDead() || info.Player.IsWounded())
				{
					enemyPlayersInRange.RemoveAt(i);
					i--;
				}
				else if (TickLineOfSightTest(npc, ref info))
				{
					num++;
					if (num >= maxVisible)
					{
						break;
					}
				}
			}
		}

		public static bool TickLineOfSightTest(IHTNAgent npc, ref NpcPlayerInfo info)
		{
			info.HeadVisible = false;
			info.BodyVisible = false;
			Vector3 vector = info.Player.transform.position - npc.transform.position;
			Vector3 lhs = npc.EyeRotation * Vector3.forward;
			float sqrMagnitude = vector.sqrMagnitude;
			float num = Vector3.Dot(lhs, vector.normalized);
			if (sqrMagnitude < npc.AiDefinition.Engagement.SqrAggroRange && num > npc.AiDefinition.Sensory.FieldOfViewRadians)
			{
				if (info.Player.IsVisible(npc.EyePosition, info.Player.CenterPoint(), npc.AiDefinition.Engagement.AggroRange))
				{
					info.BodyVisible = true;
					npc.AiDomain.NpcContext.EnemyPlayersInLineOfSight.Add(info);
				}
				else if (info.Player.IsVisible(npc.EyePosition, info.Player.eyes.position, npc.AiDefinition.Engagement.AggroRange))
				{
					info.HeadVisible = true;
					npc.AiDomain.NpcContext.EnemyPlayersInLineOfSight.Add(info);
				}
			}
			if (!info.HeadVisible)
			{
				return info.BodyVisible;
			}
			return true;
		}
	}
}
