using Apex.LoadBalancing;
using Rust.Ai.HTN;
using Rust.Ai.HTN.ScientistJunkpile;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Rust.Ai
{
	[DefaultExecutionOrder(-103)]
	public class AiManager : SingletonComponent<AiManager>, IServerComponent, ILoadBalanced
	{
		public class AgencyHTN
		{
			private readonly HashSet<IHTNAgent> activeAgents = new HashSet<IHTNAgent>();

			private readonly List<IHTNAgent> dormantAgents = new List<IHTNAgent>();

			private readonly HashSet<IHTNAgent> pendingAddToActive = new HashSet<IHTNAgent>();

			private readonly HashSet<IHTNAgent> pendingAddToDormant = new HashSet<IHTNAgent>();

			private readonly HashSet<IHTNAgent> pendingRemoveFromActive = new HashSet<IHTNAgent>();

			private readonly HashSet<IHTNAgent> pendingRemoveFromDormant = new HashSet<IHTNAgent>();

			private readonly List<HTNPlayer> tickingPlayers = new List<HTNPlayer>();

			private readonly List<HTNPlayer> tickingJunkpilePlayers = new List<HTNPlayer>();

			private readonly List<HTNAnimal> tickingAnimals = new List<HTNAnimal>();

			private int playerTickIndex;

			private int junkpilePlayerTickIndex;

			private int animalTickIndex;

			private Stopwatch watch = new Stopwatch();

			private int lastWakeUpDormantIndex;

			private readonly BasePlayer[] playerVicinityQuery = new BasePlayer[1];

			private readonly Func<BasePlayer, bool> filter = InterestedInPlayersOnly;

			internal void OnEnableAgency()
			{
			}

			internal void OnDisableAgency()
			{
			}

			public void InvokedTick()
			{
				watch.Reset();
				watch.Start();
				int num = playerTickIndex;
				while (tickingPlayers.Count > 0)
				{
					if (playerTickIndex >= tickingPlayers.Count)
					{
						playerTickIndex = 0;
					}
					HTNPlayer hTNPlayer = tickingPlayers[playerTickIndex];
					if (hTNPlayer != null && hTNPlayer.transform != null && !hTNPlayer.IsDestroyed)
					{
						hTNPlayer.Tick();
					}
					playerTickIndex++;
					if (playerTickIndex >= tickingPlayers.Count)
					{
						playerTickIndex = 0;
					}
					if (playerTickIndex == num || watch.Elapsed.TotalMilliseconds > (double)ai_htn_player_tick_budget)
					{
						break;
					}
				}
				watch.Reset();
				watch.Start();
				num = junkpilePlayerTickIndex;
				while (tickingJunkpilePlayers.Count > 0)
				{
					if (junkpilePlayerTickIndex >= tickingJunkpilePlayers.Count)
					{
						junkpilePlayerTickIndex = 0;
					}
					HTNPlayer hTNPlayer2 = tickingJunkpilePlayers[junkpilePlayerTickIndex];
					if (hTNPlayer2 != null && hTNPlayer2.transform != null && !hTNPlayer2.IsDestroyed)
					{
						hTNPlayer2.Tick();
					}
					junkpilePlayerTickIndex++;
					if (junkpilePlayerTickIndex >= tickingJunkpilePlayers.Count)
					{
						junkpilePlayerTickIndex = 0;
					}
					if (junkpilePlayerTickIndex == num || watch.Elapsed.TotalMilliseconds > (double)ai_htn_player_junkpile_tick_budget)
					{
						break;
					}
				}
				watch.Reset();
				watch.Start();
				num = animalTickIndex;
				while (tickingAnimals.Count > 0)
				{
					if (animalTickIndex >= tickingAnimals.Count)
					{
						animalTickIndex = 0;
					}
					HTNAnimal hTNAnimal = tickingAnimals[animalTickIndex];
					if (hTNAnimal != null && hTNAnimal.transform != null && !hTNAnimal.IsDestroyed)
					{
						hTNAnimal.Tick();
					}
					animalTickIndex++;
					if (animalTickIndex >= tickingAnimals.Count)
					{
						animalTickIndex = 0;
					}
					if (animalTickIndex == num || watch.Elapsed.TotalMilliseconds > (double)ai_htn_animal_tick_budget)
					{
						break;
					}
				}
			}

			public void Add(IHTNAgent agent)
			{
				if (ai_dormant)
				{
					if (IsAgentCloseToPlayers(agent))
					{
						AddActiveAgency(agent);
					}
					else
					{
						AddDormantAgency(agent);
					}
				}
				else
				{
					AddActiveAgency(agent);
				}
			}

			public void Remove(IHTNAgent agent)
			{
				RemoveActiveAgency(agent);
				if (ai_dormant)
				{
					RemoveDormantAgency(agent);
				}
			}

			internal void AddActiveAgency(IHTNAgent agent)
			{
				if (!pendingAddToActive.Contains(agent))
				{
					pendingAddToActive.Add(agent);
				}
			}

			internal void AddDormantAgency(IHTNAgent agent)
			{
				if (!pendingAddToDormant.Contains(agent))
				{
					pendingAddToDormant.Add(agent);
				}
			}

			internal void RemoveActiveAgency(IHTNAgent agent)
			{
				if (!pendingRemoveFromActive.Contains(agent))
				{
					pendingRemoveFromActive.Add(agent);
				}
			}

			internal void RemoveDormantAgency(IHTNAgent agent)
			{
				if (!pendingRemoveFromDormant.Contains(agent))
				{
					pendingRemoveFromDormant.Add(agent);
				}
			}

			internal void UpdateAgency()
			{
				AgencyCleanup();
				AgencyAddPending();
				if (ai_dormant)
				{
					TryWakeUpDormantAgents();
					TryMakeAgentsDormant();
				}
			}

			private void AgencyCleanup()
			{
				if (ai_dormant)
				{
					foreach (IHTNAgent item in pendingRemoveFromDormant)
					{
						if (item != null)
						{
							dormantAgents.Remove(item);
						}
					}
					pendingRemoveFromDormant.Clear();
				}
				foreach (IHTNAgent item2 in pendingRemoveFromActive)
				{
					if (item2 != null)
					{
						activeAgents.Remove(item2);
						HTNPlayer hTNPlayer = item2 as HTNPlayer;
						if ((bool)hTNPlayer)
						{
							if (hTNPlayer.AiDomain is ScientistJunkpileDomain)
							{
								tickingJunkpilePlayers.Remove(hTNPlayer);
							}
							else
							{
								tickingPlayers.Remove(hTNPlayer);
							}
						}
						else
						{
							HTNAnimal hTNAnimal = item2 as HTNAnimal;
							if ((bool)hTNAnimal)
							{
								tickingAnimals.Remove(hTNAnimal);
							}
						}
					}
				}
				pendingRemoveFromActive.Clear();
			}

			private void AgencyAddPending()
			{
				if (ai_dormant)
				{
					foreach (IHTNAgent item in pendingAddToDormant)
					{
						if (item != null && !item.IsDestroyed)
						{
							dormantAgents.Add(item);
							item.IsDormant = true;
						}
					}
					pendingAddToDormant.Clear();
				}
				foreach (IHTNAgent item2 in pendingAddToActive)
				{
					if (item2 != null && !item2.IsDestroyed && activeAgents.Add(item2))
					{
						item2.IsDormant = false;
						HTNPlayer hTNPlayer = item2 as HTNPlayer;
						if ((bool)hTNPlayer)
						{
							if (hTNPlayer.AiDomain is ScientistJunkpileDomain)
							{
								tickingJunkpilePlayers.Add(hTNPlayer);
							}
							else
							{
								tickingPlayers.Add(hTNPlayer);
							}
						}
						else
						{
							HTNAnimal hTNAnimal = item2 as HTNAnimal;
							if ((bool)hTNAnimal)
							{
								tickingAnimals.Add(hTNAnimal);
							}
						}
					}
				}
				pendingAddToActive.Clear();
			}

			private void TryWakeUpDormantAgents()
			{
				if (!ai_dormant || dormantAgents.Count == 0)
				{
					return;
				}
				if (lastWakeUpDormantIndex >= dormantAgents.Count)
				{
					lastWakeUpDormantIndex = 0;
				}
				int num = lastWakeUpDormantIndex;
				int num2 = 0;
				while (num2 < ai_dormant_max_wakeup_per_tick)
				{
					if (lastWakeUpDormantIndex >= dormantAgents.Count)
					{
						lastWakeUpDormantIndex = 0;
					}
					if (lastWakeUpDormantIndex != num || num2 <= 0)
					{
						IHTNAgent iHTNAgent = dormantAgents[lastWakeUpDormantIndex];
						lastWakeUpDormantIndex++;
						num2++;
						if (iHTNAgent.IsDestroyed)
						{
							RemoveDormantAgency(iHTNAgent);
						}
						else if (IsAgentCloseToPlayers(iHTNAgent))
						{
							AddActiveAgency(iHTNAgent);
							RemoveDormantAgency(iHTNAgent);
						}
						continue;
					}
					break;
				}
			}

			private void TryMakeAgentsDormant()
			{
				if (ai_dormant)
				{
					foreach (IHTNAgent activeAgent in activeAgents)
					{
						if (activeAgent.IsDestroyed)
						{
							RemoveActiveAgency(activeAgent);
						}
						else if (!IsAgentCloseToPlayers(activeAgent))
						{
							AddDormantAgency(activeAgent);
							RemoveActiveAgency(activeAgent);
						}
					}
				}
			}

			private bool IsAgentCloseToPlayers(IHTNAgent agent)
			{
				return BaseEntity.Query.Server.GetPlayersInSphere(agent.transform.position, ai_to_player_distance_wakeup_range, playerVicinityQuery, filter) > 0;
			}
		}

		private readonly HashSet<IAIAgent> activeAgents = new HashSet<IAIAgent>();

		private readonly List<IAIAgent> dormantAgents = new List<IAIAgent>();

		private readonly HashSet<IAIAgent> pendingAddToActive = new HashSet<IAIAgent>();

		private readonly HashSet<IAIAgent> pendingAddToDormant = new HashSet<IAIAgent>();

		private readonly HashSet<IAIAgent> pendingRemoveFromActive = new HashSet<IAIAgent>();

		private readonly HashSet<IAIAgent> pendingRemoveFromDormant = new HashSet<IAIAgent>();

		private int lastWakeUpDormantIndex;

		[Header("Cover System")]
		[SerializeField]
		public bool UseCover = true;

		public float CoverPointVolumeCellSize = 20f;

		public float CoverPointVolumeCellHeight = 8f;

		public float CoverPointRayLength = 1f;

		public CoverPointVolume cpvPrefab;

		[SerializeField]
		public LayerMask DynamicCoverPointVolumeLayerMask;

		private WorldSpaceGrid<CoverPointVolume> coverPointVolumeGrid;

		[ServerVar(Help = "If true we'll wait for the navmesh to generate before completely starting the server. This might cause your server to hitch and lag as it generates in the background.")]
		public static bool nav_wait = true;

		[ServerVar(Help = "If set to true the navmesh won't generate.. which means Ai that uses the navmesh won't be able to move")]
		public static bool nav_disable = false;

		[ServerVar(Help = "If ai_dormant is true, any npc outside the range of players will render itself dormant and take up less resources, but wildlife won't simulate as well.")]
		public static bool ai_dormant = true;

		[ServerVar(Help = "The maximum amount of nodes processed each frame in the asynchronous pathfinding process. Increasing this value will cause the paths to be processed faster, but can cause some hiccups in frame rate. Default value is 100, a good range for tuning is between 50 and 500.")]
		public static int pathfindingIterationsPerFrame = 100;

		[ServerVar(Help = "If an agent is beyond this distance to a player, it's flagged for becoming dormant.")]
		public static float ai_to_player_distance_wakeup_range = 160f;

		[ServerVar(Help = "nav_obstacles_carve_state defines which obstacles can carve the terrain. 0 - No carving, 1 - Only player construction carves, 2 - All obstacles carve.")]
		public static int nav_obstacles_carve_state = 2;

		[ServerVar(Help = "ai_dormant_max_wakeup_per_tick defines the maximum number of dormant agents we will wake up in a single tick. (default: 30)")]
		public static int ai_dormant_max_wakeup_per_tick = 30;

		[ServerVar(Help = "ai_htn_player_tick_budget defines the maximum amount of milliseconds ticking htn player agents are allowed to consume. (default: 4 ms)")]
		public static float ai_htn_player_tick_budget = 4f;

		[ServerVar(Help = "ai_htn_player_junkpile_tick_budget defines the maximum amount of milliseconds ticking htn player junkpile agents are allowed to consume. (default: 4 ms)")]
		public static float ai_htn_player_junkpile_tick_budget = 4f;

		[ServerVar(Help = "ai_htn_animal_tick_budget defines the maximum amount of milliseconds ticking htn animal agents are allowed to consume. (default: 4 ms)")]
		public static float ai_htn_animal_tick_budget = 4f;

		[ServerVar(Help = "If ai_htn_use_agency_tick is true, the ai manager's agency system will tick htn agents at the ms budgets defined in ai_htn_player_tick_budget and ai_htn_animal_tick_budget. If it's false, each agent registers with the invoke system individually, with no frame-budget restrictions. (default: true)")]
		public static bool ai_htn_use_agency_tick = true;

		private readonly BasePlayer[] playerVicinityQuery = new BasePlayer[1];

		private readonly Func<BasePlayer, bool> filter = InterestedInPlayersOnly;

		public AgencyHTN HTNAgency
		{
			get;
		} = new AgencyHTN();


		public bool repeat => true;

		internal void OnEnableAgency()
		{
		}

		internal void OnDisableAgency()
		{
		}

		public void Add(IAIAgent agent)
		{
			if (ai_dormant)
			{
				if (IsAgentCloseToPlayers(agent))
				{
					AddActiveAgency(agent);
				}
				else
				{
					AddDormantAgency(agent);
				}
			}
			else
			{
				AddActiveAgency(agent);
			}
		}

		public void Remove(IAIAgent agent)
		{
			RemoveActiveAgency(agent);
			if (ai_dormant)
			{
				RemoveDormantAgency(agent);
			}
		}

		internal void AddActiveAgency(IAIAgent agent)
		{
			if (!pendingAddToActive.Contains(agent))
			{
				pendingAddToActive.Add(agent);
			}
		}

		internal void AddDormantAgency(IAIAgent agent)
		{
			if (!pendingAddToDormant.Contains(agent))
			{
				pendingAddToDormant.Add(agent);
			}
		}

		internal void RemoveActiveAgency(IAIAgent agent)
		{
			if (!pendingRemoveFromActive.Contains(agent))
			{
				pendingRemoveFromActive.Add(agent);
			}
		}

		internal void RemoveDormantAgency(IAIAgent agent)
		{
			if (!pendingRemoveFromDormant.Contains(agent))
			{
				pendingRemoveFromDormant.Add(agent);
			}
		}

		internal void UpdateAgency()
		{
			AgencyCleanup();
			AgencyAddPending();
			if (ai_dormant)
			{
				TryWakeUpDormantAgents();
				TryMakeAgentsDormant();
			}
		}

		private void AgencyCleanup()
		{
			if (ai_dormant)
			{
				foreach (IAIAgent item in pendingRemoveFromDormant)
				{
					if (item != null)
					{
						dormantAgents.Remove(item);
					}
				}
				pendingRemoveFromDormant.Clear();
			}
			foreach (IAIAgent item2 in pendingRemoveFromActive)
			{
				if (item2 != null)
				{
					activeAgents.Remove(item2);
				}
			}
			pendingRemoveFromActive.Clear();
		}

		private void AgencyAddPending()
		{
			if (ai_dormant)
			{
				foreach (IAIAgent item in pendingAddToDormant)
				{
					if (item != null && !(item.Entity == null) && !item.Entity.IsDestroyed)
					{
						dormantAgents.Add(item);
						item.IsDormant = true;
					}
				}
				pendingAddToDormant.Clear();
			}
			foreach (IAIAgent item2 in pendingAddToActive)
			{
				if (item2 != null && !(item2.Entity == null) && !item2.Entity.IsDestroyed && activeAgents.Add(item2))
				{
					item2.IsDormant = false;
				}
			}
			pendingAddToActive.Clear();
		}

		private void TryWakeUpDormantAgents()
		{
			if (!ai_dormant || dormantAgents.Count == 0)
			{
				return;
			}
			if (lastWakeUpDormantIndex >= dormantAgents.Count)
			{
				lastWakeUpDormantIndex = 0;
			}
			int num = lastWakeUpDormantIndex;
			int num2 = 0;
			while (num2 < ai_dormant_max_wakeup_per_tick)
			{
				if (lastWakeUpDormantIndex >= dormantAgents.Count)
				{
					lastWakeUpDormantIndex = 0;
				}
				if (lastWakeUpDormantIndex != num || num2 <= 0)
				{
					IAIAgent iAIAgent = dormantAgents[lastWakeUpDormantIndex];
					lastWakeUpDormantIndex++;
					num2++;
					if (iAIAgent.Entity.IsDestroyed)
					{
						RemoveDormantAgency(iAIAgent);
					}
					else if (IsAgentCloseToPlayers(iAIAgent))
					{
						AddActiveAgency(iAIAgent);
						RemoveDormantAgency(iAIAgent);
					}
					continue;
				}
				break;
			}
		}

		private void TryMakeAgentsDormant()
		{
			if (ai_dormant)
			{
				foreach (IAIAgent activeAgent in activeAgents)
				{
					if (activeAgent.Entity.IsDestroyed)
					{
						RemoveActiveAgency(activeAgent);
					}
					else if (!IsAgentCloseToPlayers(activeAgent))
					{
						AddDormantAgency(activeAgent);
						RemoveActiveAgency(activeAgent);
					}
				}
			}
		}

		internal void OnEnableCover()
		{
			if (coverPointVolumeGrid == null)
			{
				Vector3 size = TerrainMeta.Size;
				coverPointVolumeGrid = new WorldSpaceGrid<CoverPointVolume>(size.x, CoverPointVolumeCellSize);
			}
		}

		internal void OnDisableCover()
		{
			if (coverPointVolumeGrid != null && coverPointVolumeGrid.Cells != null)
			{
				for (int i = 0; i < coverPointVolumeGrid.Cells.Length; i++)
				{
					UnityEngine.Object.Destroy(coverPointVolumeGrid.Cells[i]);
				}
			}
		}

		public static CoverPointVolume CreateNewCoverVolume(Vector3 point, Transform coverPointGroup)
		{
			if (SingletonComponent<AiManager>.Instance != null && SingletonComponent<AiManager>.Instance.enabled && SingletonComponent<AiManager>.Instance.UseCover)
			{
				CoverPointVolume coverPointVolume = SingletonComponent<AiManager>.Instance.GetCoverVolumeContaining(point);
				if (coverPointVolume == null)
				{
					Vector2i vector2i = SingletonComponent<AiManager>.Instance.coverPointVolumeGrid.WorldToGridCoords(point);
					coverPointVolume = ((!(SingletonComponent<AiManager>.Instance.cpvPrefab != null)) ? new GameObject("CoverPointVolume").AddComponent<CoverPointVolume>() : UnityEngine.Object.Instantiate(SingletonComponent<AiManager>.Instance.cpvPrefab));
					coverPointVolume.transform.localPosition = default(Vector3);
					coverPointVolume.transform.position = SingletonComponent<AiManager>.Instance.coverPointVolumeGrid.GridToWorldCoords(vector2i) + Vector3.up * point.y;
					coverPointVolume.transform.localScale = new Vector3(SingletonComponent<AiManager>.Instance.CoverPointVolumeCellSize, SingletonComponent<AiManager>.Instance.CoverPointVolumeCellHeight, SingletonComponent<AiManager>.Instance.CoverPointVolumeCellSize);
					coverPointVolume.CoverLayerMask = SingletonComponent<AiManager>.Instance.DynamicCoverPointVolumeLayerMask;
					coverPointVolume.CoverPointRayLength = SingletonComponent<AiManager>.Instance.CoverPointRayLength;
					SingletonComponent<AiManager>.Instance.coverPointVolumeGrid[vector2i] = coverPointVolume;
					coverPointVolume.GenerateCoverPoints(coverPointGroup);
				}
				return coverPointVolume;
			}
			return null;
		}

		public CoverPointVolume GetCoverVolumeContaining(Vector3 point)
		{
			if (coverPointVolumeGrid == null)
			{
				return null;
			}
			Vector2i cellCoords = coverPointVolumeGrid.WorldToGridCoords(point);
			return coverPointVolumeGrid[cellCoords];
		}

		public void Initialize()
		{
			OnEnableAgency();
			if (UseCover)
			{
				OnEnableCover();
			}
			AiManagerLoadBalancer.aiManagerLoadBalancer.Add(this);
			if (HTNAgency != null)
			{
				HTNAgency.OnEnableAgency();
				if (ai_htn_use_agency_tick)
				{
					InvokeHandler.InvokeRepeating(this, HTNAgency.InvokedTick, 0f, 0.033f);
				}
			}
		}

		private void OnDisable()
		{
			if (Application.isQuitting)
			{
				return;
			}
			OnDisableAgency();
			if (UseCover)
			{
				OnDisableCover();
			}
			AiManagerLoadBalancer.aiManagerLoadBalancer.Remove(this);
			if (HTNAgency != null)
			{
				HTNAgency.OnDisableAgency();
				if (ai_htn_use_agency_tick)
				{
					InvokeHandler.CancelInvoke(this, HTNAgency.InvokedTick);
				}
			}
		}

		public float? ExecuteUpdate(float deltaTime, float nextInterval)
		{
			if (nav_disable)
			{
				return nextInterval;
			}
			UpdateAgency();
			HTNAgency?.UpdateAgency();
			return UnityEngine.Random.value + 1f;
		}

		private bool IsAgentCloseToPlayers(IAIAgent agent)
		{
			return BaseEntity.Query.Server.GetPlayersInSphere(agent.Entity.transform.position, ai_to_player_distance_wakeup_range, playerVicinityQuery, filter) > 0;
		}

		private static bool InterestedInPlayersOnly(BaseEntity entity)
		{
			BasePlayer basePlayer = entity as BasePlayer;
			if (basePlayer == null)
			{
				return false;
			}
			if (basePlayer is IAIAgent)
			{
				return false;
			}
			if (basePlayer.IsSleeping() || !basePlayer.IsConnected)
			{
				return false;
			}
			return true;
		}
	}
}
