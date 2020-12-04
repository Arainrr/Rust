using ConVar;
using UnityEngine;

namespace Rust.Ai
{
	public class AiLocationSpawner : SpawnGroup
	{
		public enum SquadSpawnerLocation
		{
			MilitaryTunnels,
			JunkpileA,
			JunkpileG,
			CH47,
			None,
			Compound,
			BanditTown,
			CargoShip
		}

		public SquadSpawnerLocation Location;

		public AiLocationManager Manager;

		public JunkPile Junkpile;

		public bool IsMainSpawner = true;

		public float chance = 1f;

		private int defaultMaxPopulation;

		private int defaultNumToSpawnPerTickMax;

		private int defaultNumToSpawnPerTickMin;

		public override void SpawnInitial()
		{
			if (IsMainSpawner)
			{
				if (Location == SquadSpawnerLocation.MilitaryTunnels)
				{
					maxPopulation = ConVar.AI.npc_max_population_military_tunnels;
					numToSpawnPerTickMax = ConVar.AI.npc_spawn_per_tick_max_military_tunnels;
					numToSpawnPerTickMin = ConVar.AI.npc_spawn_per_tick_min_military_tunnels;
					respawnDelayMax = ConVar.AI.npc_respawn_delay_max_military_tunnels;
					respawnDelayMin = ConVar.AI.npc_respawn_delay_min_military_tunnels;
				}
				else
				{
					defaultMaxPopulation = maxPopulation;
					defaultNumToSpawnPerTickMax = numToSpawnPerTickMax;
					defaultNumToSpawnPerTickMin = numToSpawnPerTickMin;
				}
			}
			else
			{
				defaultMaxPopulation = maxPopulation;
				defaultNumToSpawnPerTickMax = numToSpawnPerTickMax;
				defaultNumToSpawnPerTickMin = numToSpawnPerTickMin;
			}
			base.SpawnInitial();
		}

		protected override void Spawn(int numToSpawn)
		{
			if (!ConVar.AI.npc_enable)
			{
				maxPopulation = 0;
				numToSpawnPerTickMax = 0;
				numToSpawnPerTickMin = 0;
				return;
			}
			if (numToSpawn == 0)
			{
				if (IsMainSpawner)
				{
					if (Location == SquadSpawnerLocation.MilitaryTunnels)
					{
						maxPopulation = ConVar.AI.npc_max_population_military_tunnels;
						numToSpawnPerTickMax = ConVar.AI.npc_spawn_per_tick_max_military_tunnels;
						numToSpawnPerTickMin = ConVar.AI.npc_spawn_per_tick_min_military_tunnels;
						numToSpawn = Random.Range(numToSpawnPerTickMin, numToSpawnPerTickMax + 1);
					}
					else
					{
						maxPopulation = defaultMaxPopulation;
						numToSpawnPerTickMax = defaultNumToSpawnPerTickMax;
						numToSpawnPerTickMin = defaultNumToSpawnPerTickMin;
						numToSpawn = Random.Range(numToSpawnPerTickMin, numToSpawnPerTickMax + 1);
					}
				}
				else
				{
					maxPopulation = defaultMaxPopulation;
					numToSpawnPerTickMax = defaultNumToSpawnPerTickMax;
					numToSpawnPerTickMin = defaultNumToSpawnPerTickMin;
					numToSpawn = Random.Range(numToSpawnPerTickMin, numToSpawnPerTickMax + 1);
				}
			}
			float num = chance;
			switch (Location)
			{
			case SquadSpawnerLocation.JunkpileA:
				num = ConVar.AI.npc_junkpile_a_spawn_chance;
				break;
			case SquadSpawnerLocation.JunkpileG:
				num = ConVar.AI.npc_junkpile_g_spawn_chance;
				break;
			}
			if (numToSpawn == 0 || Random.value > num || ((Location == SquadSpawnerLocation.JunkpileA || Location == SquadSpawnerLocation.JunkpileG) && NPCPlayerApex.AllJunkpileNPCs.Count >= ConVar.AI.npc_max_junkpile_count))
			{
				return;
			}
			numToSpawn = Mathf.Min(numToSpawn, maxPopulation - base.currentPopulation);
			for (int i = 0; i < numToSpawn; i++)
			{
				Vector3 pos;
				Quaternion rot;
				BaseSpawnPoint spawnPoint = GetSpawnPoint(out pos, out rot);
				if (!spawnPoint)
				{
					continue;
				}
				BaseEntity baseEntity = GameManager.server.CreateEntity(GetPrefab(), pos, rot);
				if (!baseEntity)
				{
					continue;
				}
				if (Manager != null)
				{
					NPCPlayerApex nPCPlayerApex = baseEntity as NPCPlayerApex;
					if (nPCPlayerApex != null)
					{
						nPCPlayerApex.AiContext.AiLocationManager = Manager;
						if (Junkpile != null)
						{
							Junkpile.AddNpc(nPCPlayerApex);
						}
					}
				}
				baseEntity.Spawn();
				SpawnPointInstance spawnPointInstance = baseEntity.gameObject.AddComponent<SpawnPointInstance>();
				spawnPointInstance.parentSpawnPointUser = this;
				spawnPointInstance.parentSpawnPoint = spawnPoint;
				spawnPointInstance.Notify();
			}
		}

		protected override BaseSpawnPoint GetSpawnPoint(out Vector3 pos, out Quaternion rot)
		{
			return base.GetSpawnPoint(out pos, out rot);
		}
	}
}
