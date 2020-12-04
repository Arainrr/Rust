using ConVar;
using Rust.Ai.HTN.Sensors;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rust.Ai.HTN.ScientistJunkpile.Sensors
{
	[Serializable]
	public class CoverPointsInRangeSensor : INpcSensor
	{
		public class CoverPointComparer : IComparer<CoverPoint>
		{
			private readonly IHTNAgent compareTo;

			public CoverPointComparer(IHTNAgent compareTo)
			{
				this.compareTo = compareTo;
			}

			public int Compare(CoverPoint a, CoverPoint b)
			{
				if (compareTo == null || a == null || b == null)
				{
					return 0;
				}
				float sqrMagnitude = (compareTo.transform.position - a.Position).sqrMagnitude;
				if (sqrMagnitude < 0.01f)
				{
					return -1;
				}
				float sqrMagnitude2 = (compareTo.transform.position - b.Position).sqrMagnitude;
				if (sqrMagnitude < sqrMagnitude2)
				{
					return -1;
				}
				if (sqrMagnitude > sqrMagnitude2)
				{
					return 1;
				}
				return 0;
			}
		}

		private CoverPointComparer coverPointComparer;

		private float nextCoverPosInfoTick;

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
			ScientistJunkpileDomain scientistJunkpileDomain = npc.AiDomain as ScientistJunkpileDomain;
			if (!(scientistJunkpileDomain == null) && scientistJunkpileDomain.ScientistContext != null)
			{
				if (coverPointComparer == null)
				{
					coverPointComparer = new CoverPointComparer(npc);
				}
				float allowedCoverRangeSqr = scientistJunkpileDomain.GetAllowedCoverRangeSqr();
				_FindCoverPointsInVolume(npc.transform.position, scientistJunkpileDomain.ScientistContext.CoverPoints, ref scientistJunkpileDomain.ScientistContext.CoverVolume, ref nextCoverPosInfoTick, time, scientistJunkpileDomain.ScientistContext.Location, allowedCoverRangeSqr);
			}
		}

		private bool _FindCoverPointsInVolume(Vector3 position, List<CoverPoint> coverPoints, ref CoverPointVolume volume, ref float nextTime, float time, AiLocationManager location, float maxDistanceToCoverSqr)
		{
			if (SingletonComponent<AiManager>.Instance == null || !SingletonComponent<AiManager>.Instance.enabled || !SingletonComponent<AiManager>.Instance.UseCover)
			{
				return false;
			}
			if (time > nextTime)
			{
				nextTime = time + TickFrequency * ConVar.AI.npc_cover_info_tick_rate_multiplier;
				if (location.DynamicCoverPointVolume != null)
				{
					volume = location.DynamicCoverPointVolume;
				}
				else if ((volume == null || !volume.Contains(position)) && SingletonComponent<AiManager>.Instance != null && SingletonComponent<AiManager>.Instance.enabled && SingletonComponent<AiManager>.Instance.UseCover)
				{
					volume = SingletonComponent<AiManager>.Instance.GetCoverVolumeContaining(position);
					if (volume == null)
					{
						volume = AiManager.CreateNewCoverVolume(position, (location != null) ? location.CoverPointGroup : null);
					}
				}
			}
			if (volume != null)
			{
				if (coverPoints.Count > 0)
				{
					coverPoints.Clear();
				}
				foreach (CoverPoint coverPoint in volume.CoverPoints)
				{
					if (!coverPoint.IsReserved && !coverPoint.IsCompromised)
					{
						Vector3 position2 = coverPoint.Position;
						if (!((position - position2).sqrMagnitude > maxDistanceToCoverSqr))
						{
							coverPoints.Add(coverPoint);
						}
					}
				}
				if (coverPoints.Count > 1)
				{
					coverPoints.Sort(coverPointComparer);
				}
				return true;
			}
			return false;
		}
	}
}
