using System;
using System.Collections.Generic;

namespace Rust.Ai.HTN.Sensors
{
	[Serializable]
	public class AnimalDistanceSensor : INpcSensor
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
			List<AnimalInfo> animalsInRange = npc.AiDomain.NpcContext.AnimalsInRange;
			for (int i = 0; i < animalsInRange.Count; i++)
			{
				AnimalInfo value = animalsInRange[i];
				if (value.Animal == null || value.Animal.transform == null || value.Animal.IsDestroyed || value.Animal.IsDead())
				{
					animalsInRange.RemoveAt(i);
					i--;
				}
				else
				{
					value.SqrDistance = (npc.transform.position - value.Animal.transform.position).sqrMagnitude;
					animalsInRange[i] = value;
				}
			}
		}
	}
}
