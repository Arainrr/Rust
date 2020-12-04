using ConVar;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rust.Ai.HTN.Scientist
{
	public class ScientistSpawner : MonoBehaviour, IServerComponent, ISpawnGroup
	{
		public GameObjectRef ScientistPrefab;

		[NonSerialized]
		public List<ScientistDomain> Spawned = new List<ScientistDomain>();

		[NonSerialized]
		public BaseSpawnPoint[] SpawnPoints;

		public int MaxPopulation = 1;

		public bool InitialSpawn;

		public float MinRespawnTimeMinutes = 20f;

		public float MaxRespawnTimeMinutes = 20f;

		public HTNDomain.MovementRule Movement = HTNDomain.MovementRule.FreeMove;

		public float MovementRadius = -1f;

		public bool ReducedLongRangeAccuracy;

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
			foreach (ScientistDomain item in Spawned)
			{
				BaseEntity baseEntity = GameObjectEx.ToBaseEntity(item.gameObject);
				if ((bool)baseEntity)
				{
					baseEntity.Kill();
				}
			}
			Spawned.Clear();
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

		private bool IsAllSpawnedDead()
		{
			int num = 0;
			while (num < Spawned.Count)
			{
				ScientistDomain scientistDomain = Spawned[num];
				if (scientistDomain == null || scientistDomain.transform == null || scientistDomain.ScientistContext == null || scientistDomain.ScientistContext.Body == null || scientistDomain.ScientistContext.Body.IsDestroyed || scientistDomain.ScientistContext.Body.IsDead())
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
			if (Spawned == null || Spawned.Count >= MaxPopulation)
			{
				Debug.LogWarning("Attempted to spawn a Scientist, but the spawner was full!");
			}
			else
			{
				if (!ConVar.AI.npc_enable)
				{
					return;
				}
				int num = MaxPopulation - Spawned.Count;
				int num2 = 0;
				BaseEntity baseEntity;
				while (true)
				{
					if (num2 >= num)
					{
						return;
					}
					Vector3 pos;
					Quaternion rot;
					if (!(GetSpawnPoint(out pos, out rot) == null))
					{
						baseEntity = GameManager.server.CreateEntity(ScientistPrefab.resourcePath, pos, rot, false);
						ScientistDomain component = baseEntity.GetComponent<ScientistDomain>();
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
					num2++;
				}
				baseEntity.Kill();
			}
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
