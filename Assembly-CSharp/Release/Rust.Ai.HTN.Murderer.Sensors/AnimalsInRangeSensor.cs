using Rust.Ai.HTN.Scientist;
using Rust.Ai.HTN.Sensors;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rust.Ai.HTN.Murderer.Sensors
{
	[Serializable]
	public class AnimalsInRangeSensor : INpcSensor
	{
		public const int MaxAnimals = 128;

		public static BaseNpc[] QueryResults = new BaseNpc[128];

		public static int QueryResultCount = 0;

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
			ScientistDomain scientistDomain = npc.AiDomain as ScientistDomain;
			if (scientistDomain == null || scientistDomain.ScientistContext == null)
			{
				return;
			}
			AttackEntity firearm = scientistDomain.GetFirearm();
			BaseEntity.Query.EntityTree server = BaseEntity.Query.Server;
			Vector3 position = npc.transform.position;
			float distance = npc.AiDefinition.Engagement.CloseRangeFirearm(firearm) + npc.AiDefinition.Engagement.CloseRange;
			BaseEntity[] queryResults = QueryResults;
			QueryResultCount = server.GetInSphere(position, distance, queryResults, delegate(BaseEntity entity)
			{
				BaseNpc baseNpc2 = entity as BaseNpc;
				return (!(baseNpc2 == null) && baseNpc2.isServer && !baseNpc2.IsDestroyed && !(baseNpc2.transform == null) && !baseNpc2.IsDead()) ? true : false;
			});
			List<AnimalInfo> animalsInRange = npc.AiDomain.NpcContext.AnimalsInRange;
			if (QueryResultCount > 0)
			{
				for (int i = 0; i < QueryResultCount; i++)
				{
					BaseNpc baseNpc = QueryResults[i];
					float sqrMagnitude = (baseNpc.transform.position - npc.transform.position).sqrMagnitude;
					if (sqrMagnitude > npc.AiDefinition.Engagement.SqrCloseRangeFirearm(firearm) + npc.AiDefinition.Engagement.SqrCloseRange)
					{
						continue;
					}
					bool flag = false;
					for (int j = 0; j < animalsInRange.Count; j++)
					{
						AnimalInfo value = animalsInRange[j];
						if (value.Animal == baseNpc)
						{
							value.Time = time;
							value.SqrDistance = sqrMagnitude;
							animalsInRange[j] = value;
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						animalsInRange.Add(new AnimalInfo
						{
							Animal = baseNpc,
							Time = time,
							SqrDistance = sqrMagnitude
						});
					}
				}
			}
			for (int k = 0; k < animalsInRange.Count; k++)
			{
				AnimalInfo animalInfo = animalsInRange[k];
				if (time - animalInfo.Time > npc.AiDefinition.Memory.ForgetAnimalInRangeTime)
				{
					animalsInRange.RemoveAt(k);
					k--;
				}
			}
		}
	}
}
