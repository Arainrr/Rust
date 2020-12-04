using ConVar;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rust.Ai.HTN.ScientistJunkpile
{
	public class ScientistJunkpileSpawner : MonoBehaviour, IServerComponent, ISpawnGroup
	{
		public enum JunkpileType
		{
			A,
			B,
			C,
			D,
			E,
			F,
			G
		}

		public GameObjectRef ScientistPrefab;

		[NonSerialized]
		public List<ScientistJunkpileDomain> Spawned = new List<ScientistJunkpileDomain>();

		[NonSerialized]
		public BaseSpawnPoint[] SpawnPoints;

		public int MaxPopulation = 1;

		public bool InitialSpawn;

		public float MinRespawnTimeMinutes = 120f;

		public float MaxRespawnTimeMinutes = 120f;

		public HTNDomain.MovementRule Movement = HTNDomain.MovementRule.FreeMove;

		public float MovementRadius = -1f;

		public bool ReducedLongRangeAccuracy;

		public JunkpileType SpawnType;

		[Range(0f, 1f)]
		public float SpawnBaseChance = 1f;

		private float nextRespawnTime;

		private bool pendingRespawn;

		public int currentPopulation => Spawned.Count;

		private void Awake()
		{
			SpawnPoints = GetComponentsInChildren<BaseSpawnPoint>();
			if ((bool)SingletonComponent<SpawnHandler>.Instance)
			{
				SingletonComponent<SpawnHandler>.Instance.SpawnGroups.Add(this);
			}
		}

		public void Fill()
		{
			DoRespawn();
		}

		public void Clear()
		{
			if (Spawned != null)
			{
				foreach (ScientistJunkpileDomain item in Spawned)
				{
					if (!(item == null) && !(item.gameObject == null) && !(item.transform == null))
					{
						BaseEntity baseEntity = GameObjectEx.ToBaseEntity(item.gameObject);
						if ((bool)baseEntity)
						{
							baseEntity.Kill();
						}
					}
				}
				Spawned.Clear();
			}
		}

		public void SpawnInitial()
		{
			nextRespawnTime = UnityEngine.Time.time + UnityEngine.Random.Range(3f, 4f);
			pendingRespawn = true;
		}

		public void SpawnRepeating()
		{
			CheckIfRespawnNeeded();
		}

		public void CheckIfRespawnNeeded()
		{
			if (!IsUnderGlobalSpawnThreshold())
			{
				return;
			}
			if (!pendingRespawn)
			{
				if (Spawned == null || Spawned.Count == 0 || IsAllSpawnedDead())
				{
					ScheduleRespawn();
				}
			}
			else if ((Spawned == null || Spawned.Count == 0 || IsAllSpawnedDead()) && UnityEngine.Time.time >= nextRespawnTime)
			{
				DoRespawn();
			}
		}

		private bool IsUnderGlobalSpawnThreshold()
		{
			if (ScientistJunkpileDomain.AllJunkpileNPCs != null && ScientistJunkpileDomain.AllJunkpileNPCs.Count >= ConVar.AI.npc_max_junkpile_count)
			{
				return false;
			}
			return true;
		}

		private bool IsAllSpawnedDead()
		{
			int num = 0;
			while (num < Spawned.Count)
			{
				ScientistJunkpileDomain scientistJunkpileDomain = Spawned[num];
				if (scientistJunkpileDomain == null || scientistJunkpileDomain.transform == null || scientistJunkpileDomain.ScientistContext == null || scientistJunkpileDomain.ScientistContext.Body == null || scientistJunkpileDomain.ScientistContext.Body.IsDestroyed || scientistJunkpileDomain.ScientistContext.Body.IsDead())
				{
					Spawned.RemoveAt(num);
					num--;
					num++;
					continue;
				}
				return false;
			}
			return true;
		}

		public void ScheduleRespawn()
		{
			nextRespawnTime = UnityEngine.Time.time + UnityEngine.Random.Range(MinRespawnTimeMinutes, MaxRespawnTimeMinutes) * 60f;
			pendingRespawn = true;
		}

		public void DoRespawn()
		{
			if (!Application.isLoading && !Application.isLoadingSave)
			{
				SpawnScientist();
			}
			pendingRespawn = false;
		}

		public void SpawnScientist()
		{
			if (!ConVar.AI.npc_enable || Spawned == null || Spawned.Count >= MaxPopulation || !IsUnderGlobalSpawnThreshold())
			{
				return;
			}
			float num = SpawnBaseChance;
			switch (SpawnType)
			{
			case JunkpileType.A:
				num = ConVar.AI.npc_junkpile_a_spawn_chance;
				break;
			case JunkpileType.G:
				num = ConVar.AI.npc_junkpile_g_spawn_chance;
				break;
			}
			if (UnityEngine.Random.value > num)
			{
				return;
			}
			int num2 = MaxPopulation - Spawned.Count;
			int num3 = 0;
			BaseEntity baseEntity;
			while (true)
			{
				if (num3 >= num2)
				{
					return;
				}
				Vector3 pos;
				Quaternion rot;
				if (!(GetSpawnPoint(out pos, out rot) == null))
				{
					baseEntity = GameManager.server.CreateEntity(ScientistPrefab.resourcePath, pos, rot, false);
					ScientistJunkpileDomain component = baseEntity.GetComponent<ScientistJunkpileDomain>();
					if (!component)
					{
						break;
					}
					baseEntity.enableSaving = false;
					PoolableEx.AwakeFromInstantiate(baseEntity.gameObject);
					baseEntity.Spawn();
					component.Movement = Movement;
					component.MovementRadius = MovementRadius;
					component.ReducedLongRangeAccuracy = ReducedLongRangeAccuracy;
					Spawned.Add(component);
				}
				num3++;
			}
			baseEntity.Kill();
		}

		private BaseSpawnPoint GetSpawnPoint(out Vector3 pos, out Quaternion rot)
		{
			BaseSpawnPoint baseSpawnPoint = null;
			pos = Vector3.zero;
			rot = Quaternion.identity;
			int num = UnityEngine.Random.Range(0, SpawnPoints.Length);
			for (int i = 0; i < SpawnPoints.Length; i++)
			{
				baseSpawnPoint = SpawnPoints[(num + i) % SpawnPoints.Length];
				if ((bool)baseSpawnPoint && baseSpawnPoint.gameObject.activeSelf)
				{
					break;
				}
			}
			if ((bool)baseSpawnPoint)
			{
				baseSpawnPoint.GetLocation(out pos, out rot);
			}
			return baseSpawnPoint;
		}
	}
}
