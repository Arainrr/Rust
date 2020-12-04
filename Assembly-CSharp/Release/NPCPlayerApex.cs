using Apex.AI;
using Apex.AI.Components;
using Apex.AI.Serialization;
using Apex.LoadBalancing;
using ConVar;
using Oxide.Core;
using Rust.Ai;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPCPlayerApex : NPCPlayer, IContextProvider, IAIAgent, ILoadBalanced
{
	public class CoverPointComparer : IComparer<CoverPoint>
	{
		private readonly BaseEntity compareTo;

		public CoverPointComparer(BaseEntity compareTo)
		{
			this.compareTo = compareTo;
		}

		public int Compare(CoverPoint a, CoverPoint b)
		{
			if (compareTo == null || a == null || b == null)
			{
				return 0;
			}
			float sqrMagnitude = (compareTo.ServerPosition - a.Position).sqrMagnitude;
			if (sqrMagnitude < 0.01f)
			{
				return -1;
			}
			float sqrMagnitude2 = (compareTo.ServerPosition - b.Position).sqrMagnitude;
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

	public delegate void ActionCallback();

	public enum WeaponTypeEnum : byte
	{
		None,
		CloseRange,
		MediumRange,
		LongRange
	}

	public enum EnemyRangeEnum : byte
	{
		CloseAttackRange,
		MediumAttackRange,
		LongAttackRange,
		OutOfRange
	}

	public enum EnemyEngagementRangeEnum : byte
	{
		AggroRange,
		DeaggroRange,
		NeutralRange
	}

	public enum ToolTypeEnum : byte
	{
		None,
		Research,
		Lightsource
	}

	public enum Facts
	{
		HasEnemy,
		HasSecondaryEnemies,
		EnemyRange,
		CanTargetEnemies,
		Health,
		Speed,
		IsWeaponAttackReady,
		CanReload,
		IsRoamReady,
		IsAggro,
		WantsToFlee,
		AttackedLately,
		LoudNoiseNearby,
		IsMoving,
		IsFleeing,
		IsAfraid,
		AfraidRange,
		IsUnderHealthThreshold,
		CanNotMove,
		SeekingCover,
		IsInCover,
		IsCrouched,
		CurrentAmmoState,
		CurrentWeaponType,
		BodyState,
		HasLineOfSight,
		CanSwitchWeapon,
		CoverInRange,
		IsMovingToCover,
		ExplosiveInRange,
		HasLineOfSightCrouched,
		HasLineOfSightStanding,
		PathToTargetStatus,
		AimsAtTarget,
		RetreatCoverInRange,
		FlankCoverInRange,
		AdvanceCoverInRange,
		IsRetreatingToCover,
		SidesteppedOutOfCover,
		IsCoverCompromised,
		AttackedVeryRecently,
		RangeToSpawnLocation,
		AttackedRecently,
		CurrentToolType,
		CanSwitchTool,
		AllyAttackedRecently,
		IsMounted,
		WantsToDismount,
		CanNotWieldWeapon,
		IsMobile,
		HasWaypoints,
		IsPeacekeeper,
		IsSearchingForEnemy,
		EnemyEngagementRange,
		IsMovingTowardWaypoint,
		IsMilitaryTunnelLab,
		IncompletePathToTarget,
		IsBandit
	}

	public enum AfraidRangeEnum : byte
	{
		InAfraidRange,
		OutOfRange
	}

	public enum HealthEnum : byte
	{
		Fine,
		Medium,
		Low
	}

	public enum SpeedEnum : byte
	{
		StandStill,
		CrouchWalk,
		Walk,
		Run,
		CrouchRun,
		Sprint
	}

	public enum AmmoStateEnum : byte
	{
		Full,
		High,
		Medium,
		Low,
		Empty
	}

	public enum BodyState : byte
	{
		StandingTall,
		Crouched
	}

	public BaseNpc.AiStatistics Stats;

	[SerializeField]
	public UtilityAIComponent utilityAiComponent;

	public bool NewAI;

	public bool NeverMove;

	public bool IsMountableAgent;

	public float WeaponSwitchFrequency = 5f;

	public float ToolSwitchFrequency = 5f;

	public WaypointSet WaypointSet;

	[NonSerialized]
	public Transform[] LookAtInterestPointsStationary;

	private NPCHumanContext _aiContext;

	[NonSerialized]
	public StateTimer BusyTimer;

	private float maxFleeTime;

	private float fleeHealthThresholdPercentage = 1f;

	private float aggroTimeout = float.NegativeInfinity;

	private float lastAggroChanceResult;

	private float lastAggroChanceCalcTime;

	private const float aggroChanceRecalcTimeout = 5f;

	private BaseEntity blockTargetingThisEnemy;

	[NonSerialized]
	[ReadOnly]
	public float NextWeaponSwitchTime;

	[NonSerialized]
	[ReadOnly]
	public float NextToolSwitchTime;

	[NonSerialized]
	[ReadOnly]
	public float NextDetectionCheck;

	private bool wasAggro;

	[NonSerialized]
	public float TimeLastMoved;

	[NonSerialized]
	public float TimeLastMovedToCover;

	[NonSerialized]
	public float AllyAttackedRecentlyTimeout;

	[NonSerialized]
	public float LastHasEnemyTime;

	[NonSerialized]
	public bool LastDetectionCheckResult;

	[NonSerialized]
	public BaseNpc.Behaviour _currentBehavior;

	protected float lastInRangeOfSpawnPositionTime = float.NegativeInfinity;

	private static Vector3[] pathCornerCache = new Vector3[128];

	private static NavMeshPath _pathCache = null;

	private float nextLookAtPointTime;

	[NonSerialized]
	public Transform LookAtPoint;

	[NonSerialized]
	public PlayerEyes LookAtEyes;

	[Header("Npc Communication")]
	public float CommunicationRadius = -1f;

	[NonSerialized]
	public byte[] CurrentFacts = new byte[Enum.GetValues(typeof(Facts)).Length];

	[Header("NPC Player Senses")]
	public int ForgetUnseenEntityTime = 10;

	public float SensesTickRate = 0.5f;

	public float MaxDistanceToCover = 15f;

	public float MinDistanceToRetreatCover = 6f;

	[Header("NPC Player Senses Target Scoring")]
	public float VisionRangeScore = 1f;

	public float AggroRangeScore = 5f;

	public float LongRangeScore = 1f;

	public float MediumRangeScore = 5f;

	public float CloseRangeScore = 10f;

	[NonSerialized]
	public BaseEntity[] SensesResults = new BaseEntity[128];

	private List<NavPointSample> navPointSamples = new List<NavPointSample>(8);

	private CoverPointComparer coverPointComparer;

	private new float lastTickTime;

	private const int sensesTicksPerCoverSweep = 5;

	private int sensesTicksSinceLastCoverSweep = 5;

	private float alertness;

	protected float lastSeenPlayerTime = float.NegativeInfinity;

	private bool isAlreadyCheckingPathPending;

	private int numPathPendingAttempts;

	private float accumPathPendingDelay;

	[Tooltip("Only care about sensations from our active enemy target, and nobody else.")]
	[Header("Sensory")]
	public bool OnlyTargetSensations;

	private const int MaxPlayers = 128;

	public static BasePlayer[] PlayerQueryResults = new BasePlayer[128];

	public static int PlayerQueryResultCount = 0;

	private static NavMeshPath PathToPlayerTarget;

	private static PlayerTargetContext _playerTargetContext;

	public static BaseEntity[] EntityQueryResults = new BaseEntity[128];

	public static int EntityQueryResultCount = 0;

	private static EntityTargetContext _entityTargetContext;

	private static CoverContext _coverContext;

	private static BaseAiUtilityClient _selectPlayerTargetAI;

	private static BaseAiUtilityClient _selectPlayerTargetMountedAI;

	private static BaseAiUtilityClient _selectEntityTargetAI;

	private static BaseAiUtilityClient _selectCoverTargetsAI;

	private static BaseAiUtilityClient _selectEnemyHideoutAI;

	[Header("Sensory System")]
	public AIStorage SelectPlayerTargetUtility;

	public AIStorage SelectPlayerTargetMountedUtility;

	public AIStorage SelectEntityTargetsUtility;

	public AIStorage SelectCoverTargetsUtility;

	public AIStorage SelectEnemyHideoutUtility;

	private float playerTargetDecisionStartTime;

	private float animalTargetDecisionStartTime;

	private float nextCoverInfoTick;

	private float nextCoverPosInfoTick;

	private float _lastHeardGunshotTime = float.NegativeInfinity;

	public Vector2 RadioEffectRepeatRange = new Vector2(10f, 15f);

	public GameObjectRef RadioEffect;

	public GameObjectRef DeathEffect;

	public int agentTypeIndex;

	public float stuckDuration;

	public float lastStuckTime;

	public float timeAtDestination;

	public bool IsInvinsible;

	public float lastInvinsibleStartTime;

	public float InvinsibleTime = 2f;

	public string deathStatName = "kill_scientist";

	private Vector3 lastStuckPos;

	public const float TickRate = 0.1f;

	public static readonly HashSet<NPCPlayerApex> AllJunkpileNPCs = new HashSet<NPCPlayerApex>();

	public static readonly HashSet<NPCPlayerApex> AllBanditCampNPCs = new HashSet<NPCPlayerApex>();

	private float nextSensorySystemTick;

	private float nextReasoningSystemTick;

	private float attackTargetVisibleFor;

	private BaseEntity lastAttackTarget;

	public NPCHumanContext AiContext
	{
		get
		{
			if (_aiContext == null)
			{
				SetupAiContext();
			}
			return _aiContext;
		}
	}

	public float TimeAtDestination => timeAtDestination;

	public int WaypointDirection
	{
		get;
		set;
	}

	public bool IsWaitingAtWaypoint
	{
		get;
		set;
	}

	public int CurrentWaypointIndex
	{
		get;
		set;
	}

	public float WaypointDelayTime
	{
		get;
		set;
	}

	public Vector3 Destination
	{
		get
		{
			if (IsNavRunning())
			{
				return GetNavAgent.destination;
			}
			return Entity.ServerPosition;
		}
		set
		{
			if (IsNavRunning())
			{
				IsStopped = false;
				GetNavAgent.destination = value;
			}
		}
	}

	public float StoppingDistance
	{
		get
		{
			if (IsNavRunning())
			{
				return GetNavAgent.stoppingDistance;
			}
			return 0f;
		}
		set
		{
			if (IsNavRunning())
			{
				GetNavAgent.stoppingDistance = value;
			}
		}
	}

	public float SqrStoppingDistance
	{
		get
		{
			if (IsNavRunning())
			{
				return GetNavAgent.stoppingDistance * GetNavAgent.stoppingDistance;
			}
			return 0f;
		}
	}

	public bool IsStopped
	{
		get
		{
			if (IsNavRunning())
			{
				return GetNavAgent.isStopped;
			}
			return true;
		}
		set
		{
			if (IsNavRunning())
			{
				if (value)
				{
					GetNavAgent.destination = ServerPosition;
				}
				GetNavAgent.isStopped = value;
			}
		}
	}

	public bool AutoBraking
	{
		get
		{
			if (IsNavRunning())
			{
				return GetNavAgent.autoBraking;
			}
			return false;
		}
		set
		{
			if (IsNavRunning())
			{
				GetNavAgent.autoBraking = value;
			}
		}
	}

	public Vector3 SpawnPosition
	{
		get;
		set;
	}

	public float AttackTargetVisibleFor => attackTargetVisibleFor;

	public BaseEntity AttackTarget
	{
		get;
		set;
	}

	public Memory.SeenInfo AttackTargetMemory
	{
		get;
		set;
	}

	public BaseCombatEntity CombatTarget => AttackTarget as BaseCombatEntity;

	public Vector3 AttackPosition => eyes.position;

	public Vector3 CrouchedAttackPosition
	{
		get
		{
			if (IsDucked())
			{
				return AttackPosition;
			}
			return AttackPosition - Vector3.down * 1f;
		}
	}

	public BaseNpc.Behaviour CurrentBehaviour
	{
		get
		{
			return _currentBehavior;
		}
		set
		{
			_currentBehavior = value;
			BehaviourChanged();
		}
	}

	public float currentBehaviorDuration
	{
		get;
		set;
	}

	public BaseCombatEntity Entity => this;

	public NavMeshAgent GetNavAgent
	{
		get
		{
			if (NavAgent == null)
			{
				NavAgent = GetComponent<NavMeshAgent>();
				if (NavAgent == null)
				{
					Debug.LogErrorFormat("{0} has no nav agent!", base.name);
				}
			}
			return NavAgent;
		}
	}

	public BaseNpc.AiStatistics GetStats => Stats;

	public float GetAttackRate => 0f;

	public float GetAttackRange => WeaponAttackRange();

	public Vector3 GetAttackOffset => new Vector3(0f, 1.8f, 0f);

	public Vector3 CurrentAimAngles => eyes.BodyForward();

	public float GetStamina => 1f;

	public float GetEnergy => 1f;

	public float GetAttackCost => 0f;

	public float GetSleep => 1f;

	public float GetStuckDuration => 0f;

	public float GetLastStuckTime => 0f;

	public BaseEntity FoodTarget
	{
		get;
		set;
	}

	public float TargetSpeed
	{
		get;
		set;
	}

	public float SecondsSinceLastInRangeOfSpawnPosition => UnityEngine.Time.time - lastInRangeOfSpawnPositionTime;

	public ActionCallback OnFleeExplosive
	{
		get;
		set;
	}

	public ActionCallback OnTakeCover
	{
		get;
		set;
	}

	public ActionCallback OnAggro
	{
		get;
		set;
	}

	public ActionCallback OnChatter
	{
		get;
		set;
	}

	public ActionCallback OnDeath
	{
		get;
		set;
	}

	public ActionCallback OnReload
	{
		get;
		set;
	}

	public float SecondsSinceSeenPlayer => UnityEngine.Time.time - lastSeenPlayerTime;

	private static PlayerTargetContext PlayerTargetContext
	{
		get
		{
			if (_playerTargetContext == null)
			{
				_playerTargetContext = new PlayerTargetContext
				{
					Direction = new Vector3[128],
					Dot = new float[128],
					DistanceSqr = new float[128],
					LineOfSight = new byte[128]
				};
			}
			return _playerTargetContext;
		}
	}

	private static EntityTargetContext EntityTargetContext
	{
		get
		{
			if (_entityTargetContext == null)
			{
				_entityTargetContext = new EntityTargetContext();
			}
			return _entityTargetContext;
		}
	}

	private static CoverContext CoverContext
	{
		get
		{
			if (_coverContext == null)
			{
				_coverContext = new CoverContext();
			}
			return _coverContext;
		}
	}

	private BaseAiUtilityClient SelectPlayerTargetAI
	{
		get
		{
			if (_selectPlayerTargetAI == null && SelectPlayerTargetUtility != null)
			{
				_selectPlayerTargetAI = new BaseAiUtilityClient(AIManager.GetAI(new Guid(SelectPlayerTargetUtility.aiId)), this);
				_selectPlayerTargetAI.Initialize();
			}
			return _selectPlayerTargetAI;
		}
	}

	private BaseAiUtilityClient SelectPlayerTargetMountedAI
	{
		get
		{
			if (_selectPlayerTargetMountedAI == null && SelectPlayerTargetMountedUtility != null)
			{
				_selectPlayerTargetMountedAI = new BaseAiUtilityClient(AIManager.GetAI(new Guid(SelectPlayerTargetMountedUtility.aiId)), this);
				_selectPlayerTargetMountedAI.Initialize();
			}
			return _selectPlayerTargetMountedAI;
		}
	}

	private BaseAiUtilityClient SelectEntityTargetAI
	{
		get
		{
			if (_selectEntityTargetAI == null && SelectEntityTargetsUtility != null)
			{
				_selectEntityTargetAI = new BaseAiUtilityClient(AIManager.GetAI(new Guid(SelectEntityTargetsUtility.aiId)), this);
				_selectEntityTargetAI.Initialize();
			}
			return _selectEntityTargetAI;
		}
	}

	private BaseAiUtilityClient SelectCoverTargetsAI
	{
		get
		{
			if (_selectCoverTargetsAI == null && SelectCoverTargetsUtility != null)
			{
				_selectCoverTargetsAI = new BaseAiUtilityClient(AIManager.GetAI(new Guid(SelectCoverTargetsUtility.aiId)), this);
				_selectCoverTargetsAI.Initialize();
			}
			return _selectCoverTargetsAI;
		}
	}

	private BaseAiUtilityClient SelectEnemyHideoutAI
	{
		get
		{
			if (_selectEnemyHideoutAI == null && SelectEnemyHideoutUtility != null)
			{
				_selectEnemyHideoutAI = new BaseAiUtilityClient(AIManager.GetAI(new Guid(SelectEnemyHideoutUtility.aiId)), this);
				_selectEnemyHideoutAI.Initialize();
			}
			return _selectEnemyHideoutAI;
		}
	}

	public float SecondsSinceLastHeardGunshot => UnityEngine.Time.time - _lastHeardGunshotTime;

	public Vector3 LastHeardGunshotDirection
	{
		get;
		set;
	}

	public override BaseNpc.AiStatistics.FamilyEnum Family => BaseNpc.AiStatistics.FamilyEnum.Scientist;

	public int AgentTypeIndex
	{
		get
		{
			return agentTypeIndex;
		}
		set
		{
			agentTypeIndex = value;
		}
	}

	public bool IsStuck
	{
		get;
		set;
	}

	public override bool IsDormant
	{
		get
		{
			return base.IsDormant;
		}
		set
		{
			base.IsDormant = value;
			if (value)
			{
				StopMoving();
				Pause();
			}
			else if (GetNavAgent == null || AiManager.nav_disable)
			{
				IsDormant = true;
			}
			else
			{
				Resume();
			}
		}
	}

	bool ILoadBalanced.repeat => true;

	protected virtual void SetupAiContext()
	{
		_aiContext = new NPCHumanContext(this);
	}

	public IAIContext GetContext(Guid aiId)
	{
		if ((SelectPlayerTargetAI != null && aiId == SelectPlayerTargetAI.ai.id) || (SelectPlayerTargetMountedAI != null && aiId == SelectPlayerTargetMountedAI.ai.id))
		{
			return PlayerTargetContext;
		}
		if (SelectEntityTargetAI != null && aiId == SelectEntityTargetAI.ai.id)
		{
			return EntityTargetContext;
		}
		if ((SelectCoverTargetsAI != null && aiId == SelectCoverTargetsAI.ai.id) || (SelectEnemyHideoutAI != null && aiId == SelectEnemyHideoutAI.ai.id))
		{
			return CoverContext;
		}
		return AiContext;
	}

	public override bool IsNavRunning()
	{
		if (base.isServer && !AiManager.nav_disable && !base.isMounted && GetNavAgent != null && GetNavAgent.enabled)
		{
			return GetNavAgent.isOnNavMesh;
		}
		return false;
	}

	public void Pause()
	{
		if (GetNavAgent != null && GetNavAgent.enabled)
		{
			GetNavAgent.enabled = false;
		}
		if (utilityAiComponent == null)
		{
			utilityAiComponent = Entity.GetComponent<UtilityAIComponent>();
		}
		if (utilityAiComponent != null)
		{
			utilityAiComponent.Pause();
			utilityAiComponent.enabled = false;
		}
		CancelInvoke(RadioChatter);
	}

	public override void Resume()
	{
		if (base.isMounted)
		{
			if (utilityAiComponent == null)
			{
				utilityAiComponent = Entity.GetComponent<UtilityAIComponent>();
			}
			if (utilityAiComponent != null)
			{
				utilityAiComponent.enabled = true;
				utilityAiComponent.Resume();
			}
			SendNetworkUpdateImmediate();
			return;
		}
		if (!GetNavAgent.isOnNavMesh)
		{
			if (Interface.CallHook("OnNpcResume", this) == null)
			{
				StartCoroutine(TryForceToNavmesh());
			}
			return;
		}
		GetNavAgent.enabled = true;
		StoppingDistance = 1f;
		if (utilityAiComponent == null)
		{
			utilityAiComponent = Entity.GetComponent<UtilityAIComponent>();
		}
		if (utilityAiComponent != null)
		{
			utilityAiComponent.enabled = true;
			utilityAiComponent.Resume();
		}
		InvokeRandomized(RadioChatter, RadioEffectRepeatRange.x, RadioEffectRepeatRange.x, RadioEffectRepeatRange.y - RadioEffectRepeatRange.x);
	}

	public void Mount(BaseMountable mountable)
	{
		if (!(mountable.GetMounted() == null))
		{
			return;
		}
		mountable.AttemptMount(this);
		mountable = GetMounted();
		if ((bool)mountable)
		{
			NavAgent.enabled = false;
			SetFact(Facts.IsMounted, 1);
			if (!mountable.canWieldItems)
			{
				SetFact(Facts.CanNotWieldWeapon, 1);
			}
			CancelInvoke(RadioChatter);
		}
	}

	public void Dismount()
	{
		BaseMountable mounted = GetMounted();
		if (mounted != null && mounted.AttemptDismount(this))
		{
			SetFact(Facts.IsMounted, 0);
			SetFact(Facts.WantsToDismount, 0);
			SetFact(Facts.CanNotWieldWeapon, 0);
			Resume();
		}
	}

	public override void DismountObject()
	{
		base.DismountObject();
		SetFact(Facts.WantsToDismount, 1);
	}

	private IEnumerator TryForceToNavmesh()
	{
		yield return null;
		int numTries = 0;
		float waitForRetryTime2 = 1f;
		float maxDistanceMultiplier = 2f;
		if (SingletonComponent<DynamicNavMesh>.Instance != null)
		{
			while (SingletonComponent<DynamicNavMesh>.Instance.IsBuilding)
			{
				yield return CoroutineEx.waitForSecondsRealtime(waitForRetryTime2);
				waitForRetryTime2 += 0.5f;
			}
		}
		waitForRetryTime2 = 1f;
		for (; numTries < 3; numTries++)
		{
			if (!GetNavAgent.isOnNavMesh)
			{
				NavMeshHit hit;
				if (NavMesh.SamplePosition(ServerPosition, out hit, GetNavAgent.height * maxDistanceMultiplier, GetNavAgent.areaMask))
				{
					ServerPosition = hit.position;
					GetNavAgent.Warp(ServerPosition);
					GetNavAgent.enabled = true;
					float num = SpawnPosition.y - ServerPosition.y;
					if (num < 0f)
					{
						num = Mathf.Max(num, -0.25f);
						GetNavAgent.baseOffset = num;
					}
					StoppingDistance = 1f;
					if (utilityAiComponent == null)
					{
						utilityAiComponent = Entity.GetComponent<UtilityAIComponent>();
					}
					if (utilityAiComponent != null)
					{
						utilityAiComponent.enabled = true;
						utilityAiComponent.Resume();
					}
					InvokeRandomized(RadioChatter, RadioEffectRepeatRange.x, RadioEffectRepeatRange.x, RadioEffectRepeatRange.y - RadioEffectRepeatRange.x);
					yield break;
				}
				yield return CoroutineEx.waitForSecondsRealtime(waitForRetryTime2);
				maxDistanceMultiplier *= 1.5f;
				continue;
			}
			GetNavAgent.enabled = true;
			StoppingDistance = 1f;
			if (utilityAiComponent == null)
			{
				utilityAiComponent = Entity.GetComponent<UtilityAIComponent>();
			}
			if (utilityAiComponent != null)
			{
				utilityAiComponent.enabled = true;
				utilityAiComponent.Resume();
			}
			InvokeRandomized(RadioChatter, RadioEffectRepeatRange.x, RadioEffectRepeatRange.x, RadioEffectRepeatRange.y - RadioEffectRepeatRange.x);
			yield break;
		}
		int areaFromName = NavMesh.GetAreaFromName("Walkable");
		if ((GetNavAgent.areaMask & (1 << areaFromName)) == 0)
		{
			NavMeshBuildSettings settingsByIndex = NavMesh.GetSettingsByIndex(1);
			GetNavAgent.agentTypeID = settingsByIndex.agentTypeID;
			GetNavAgent.areaMask = 1 << areaFromName;
			yield return TryForceToNavmesh();
		}
		else if (base.transform != null && !base.IsDestroyed)
		{
			Debug.LogWarningFormat("Failed to spawn {0} on a valid navmesh.", base.name);
			Kill();
		}
	}

	public float FearLevel(BaseEntity ent)
	{
		return 0f;
	}

	public float GetWantsToAttack(BaseEntity target)
	{
		if (target == null)
		{
			return 0f;
		}
		object obj = Interface.CallHook("IOnNpcTarget", this, target);
		if (obj is float)
		{
			return (float)obj;
		}
		if (!target.HasAnyTrait(TraitFlag.Animal | TraitFlag.Human))
		{
			return 0f;
		}
		if (target.GetType() == GetType())
		{
			return 0f;
		}
		if (target.Health() <= 0f)
		{
			return 0f;
		}
		return 1f;
	}

	public bool BusyTimerActive()
	{
		return BusyTimer.IsActive;
	}

	public void SetBusyFor(float dur)
	{
		BusyTimer.Activate(dur);
	}

	public bool WantsToEat(BaseEntity ent)
	{
		return false;
	}

	public void Eat()
	{
	}

	public byte GetFact(BaseNpc.Facts fact)
	{
		return 0;
	}

	public void SetFact(BaseNpc.Facts fact, byte value, bool triggerCallback = true, bool onlyTriggerCallbackOnDiffValue = true)
	{
	}

	public float ToSpeed(BaseNpc.SpeedEnum speed)
	{
		return 0f;
	}

	public List<NavPointSample> RequestNavPointSamplesInCircle(NavPointSampler.SampleCount sampleCount, float radius, NavPointSampler.SampleFeatures features = NavPointSampler.SampleFeatures.None)
	{
		navPointSamples.Clear();
		NavPointSampler.SampleCircle(sampleCount, ServerPosition, radius, new NavPointSampler.SampleScoreParams
		{
			WaterMaxDepth = Stats.MaxWaterDepth,
			Agent = this,
			Features = features
		}, ref navPointSamples);
		return navPointSamples;
	}

	public List<NavPointSample> RequestNavPointSamplesInCircleWaterDepthOnly(NavPointSampler.SampleCount sampleCount, float radius, float waterDepth)
	{
		navPointSamples.Clear();
		NavPointSampler.SampleCircleWaterDepthOnly(sampleCount, ServerPosition, radius, new NavPointSampler.SampleScoreParams
		{
			WaterMaxDepth = waterDepth,
			Agent = this
		}, ref navPointSamples);
		return navPointSamples;
	}

	private void OnFactChanged(Facts fact, byte oldValue, byte newValue)
	{
		switch (fact)
		{
		case Facts.WantsToFlee:
		case Facts.AttackedLately:
		case Facts.LoudNoiseNearby:
		case Facts.IsFleeing:
		case Facts.IsAfraid:
		case Facts.AfraidRange:
		case Facts.IsUnderHealthThreshold:
		case Facts.CanNotMove:
		case Facts.SeekingCover:
		case Facts.IsInCover:
		case Facts.IsCrouched:
		case Facts.CurrentAmmoState:
		case Facts.CurrentWeaponType:
		case Facts.BodyState:
		{
			int num = 24;
			break;
		}
		case Facts.IsAggro:
			if (newValue > 0 && GetFact(Facts.IsRetreatingToCover) == 0)
			{
				CurrentBehaviour = BaseNpc.Behaviour.Attack;
				if (newValue != oldValue)
				{
					SetPlayerFlag(PlayerFlags.Relaxed, false);
				}
			}
			else
			{
				SetPlayerFlag(PlayerFlags.Relaxed, true);
				SetFact(Facts.Speed, 0);
			}
			break;
		case Facts.IsSearchingForEnemy:
			if (newValue > 0)
			{
				CurrentBehaviour = BaseNpc.Behaviour.Attack;
			}
			break;
		case Facts.Speed:
			switch (newValue)
			{
			case 0:
				StopMoving();
				if (GetFact(Facts.IsAggro) == 0 && GetFact(Facts.IsRetreatingToCover) == 0)
				{
					CurrentBehaviour = BaseNpc.Behaviour.Idle;
					if (newValue != oldValue)
					{
						SetPlayerFlag(PlayerFlags.Relaxed, true);
					}
				}
				break;
			case 2:
				IsStopped = false;
				if (GetFact(Facts.IsAggro) == 0 && GetFact(Facts.IsRetreatingToCover) == 0)
				{
					CurrentBehaviour = BaseNpc.Behaviour.Wander;
					if (newValue != oldValue)
					{
						SetPlayerFlag(PlayerFlags.Relaxed, true);
					}
				}
				break;
			default:
				IsStopped = false;
				if (GetFact(Facts.IsAggro) > 0)
				{
					if (newValue != oldValue)
					{
						SetPlayerFlag(PlayerFlags.Relaxed, false);
					}
				}
				else if (newValue != oldValue)
				{
					SetPlayerFlag(PlayerFlags.Relaxed, true);
				}
				break;
			}
			break;
		case Facts.CanTargetEnemies:
			if (newValue == 1)
			{
				blockTargetingThisEnemy = null;
			}
			break;
		case Facts.IsMoving:
			if (newValue == 1)
			{
				TimeLastMoved = UnityEngine.Time.realtimeSinceStartup;
			}
			break;
		case Facts.IsRetreatingToCover:
			if (newValue == 1)
			{
				CurrentBehaviour = BaseNpc.Behaviour.RetreatingToCover;
				if (newValue != oldValue)
				{
					SetPlayerFlag(PlayerFlags.Relaxed, true);
				}
			}
			else if (GetFact(Facts.IsAggro) > 0)
			{
				CurrentBehaviour = BaseNpc.Behaviour.Attack;
				if (newValue != oldValue)
				{
					SetPlayerFlag(PlayerFlags.Relaxed, false);
				}
			}
			else
			{
				CurrentBehaviour = BaseNpc.Behaviour.Idle;
				if (newValue != oldValue)
				{
					SetPlayerFlag(PlayerFlags.Relaxed, true);
				}
			}
			break;
		case Facts.HasEnemy:
			if (newValue == 1)
			{
				LastHasEnemyTime = UnityEngine.Time.time;
				if (GetFact(Facts.HasLineOfSight) > 0)
				{
					CurrentBehaviour = BaseNpc.Behaviour.Attack;
				}
			}
			break;
		}
	}

	private void TickBehaviourState()
	{
		if (GetFact(Facts.WantsToFlee) == 1 && ToPathStatus(GetPathStatus()) == NavMeshPathStatus.PathComplete && UnityEngine.Time.realtimeSinceStartup - (maxFleeTime - Stats.MaxFleeTime) > 0.5f)
		{
			TickFlee();
		}
		if (GetFact(Facts.IsAggro) == 1)
		{
			TickAggro();
		}
		if (GetFact(Facts.AllyAttackedRecently) == 1 && UnityEngine.Time.realtimeSinceStartup >= AllyAttackedRecentlyTimeout)
		{
			SetFact(Facts.AllyAttackedRecently, 0);
		}
	}

	public bool TryAggro(float sqrRange)
	{
		if (!HostilityConsideration(AiContext.EnemyPlayer))
		{
			wasAggro = false;
			return false;
		}
		bool flag = IsWithinAggroRange(sqrRange);
		if (GetFact(Facts.IsAggro) == 0 && flag)
		{
			return StartAggro(Stats.DeaggroChaseTime);
		}
		wasAggro = flag;
		return false;
	}

	public bool TryAggro(EnemyRangeEnum range)
	{
		if (!HostilityConsideration(AiContext.EnemyPlayer))
		{
			wasAggro = false;
			return false;
		}
		if (GetFact(Facts.IsAggro) == 0 && IsWithinAggroRange(range))
		{
			float a = ((int)range <= 1) ? 1f : Stats.Defensiveness;
			a = Mathf.Max(a, Stats.Hostility);
			if (UnityEngine.Time.realtimeSinceStartup > lastAggroChanceCalcTime + 5f)
			{
				lastAggroChanceResult = UnityEngine.Random.value;
				lastAggroChanceCalcTime = UnityEngine.Time.realtimeSinceStartup;
			}
			if (lastAggroChanceResult < a)
			{
				return StartAggro(Stats.DeaggroChaseTime);
			}
		}
		wasAggro = IsWithinAggroRange(range);
		return false;
	}

	public bool StartAggro(float timeout, bool broadcastEvent = true)
	{
		if (GetFact(Facts.IsAggro) == 1)
		{
			wasAggro = true;
			return false;
		}
		SetFact(Facts.IsAggro, 1);
		aggroTimeout = UnityEngine.Time.realtimeSinceStartup + timeout;
		if (!wasAggro && broadcastEvent && OnAggro != null && GetFact(Facts.HasLineOfSight) > 0)
		{
			OnAggro();
		}
		wasAggro = true;
		return true;
	}

	private void TickAggro()
	{
		bool flag = false;
		bool triggerCallback = true;
		if (float.IsInfinity(base.SecondsSinceDealtDamage) || float.IsNegativeInfinity(base.SecondsSinceDealtDamage) || float.IsNaN(base.SecondsSinceDealtDamage))
		{
			flag = (UnityEngine.Time.realtimeSinceStartup > aggroTimeout);
		}
		else
		{
			BaseCombatEntity baseCombatEntity = AttackTarget as BaseCombatEntity;
			flag = ((!(baseCombatEntity != null) || !(baseCombatEntity.lastAttacker != null) || net == null || baseCombatEntity.lastAttacker.net == null || base.isMounted) ? (UnityEngine.Time.realtimeSinceStartup > aggroTimeout) : (baseCombatEntity.lastAttacker.net.ID == net.ID && base.SecondsSinceDealtDamage > Stats.DeaggroChaseTime));
		}
		if (!flag)
		{
			if (AiContext.EnemyNpc != null && (AiContext.EnemyNpc.IsDead() || AiContext.EnemyNpc.IsDestroyed))
			{
				flag = true;
				triggerCallback = false;
			}
			else if (AiContext.EnemyPlayer != null && (AiContext.EnemyPlayer.IsDead() || AiContext.EnemyPlayer.IsDestroyed))
			{
				flag = true;
				triggerCallback = false;
			}
		}
		if (flag)
		{
			SetFact(Facts.IsAggro, 0, triggerCallback);
		}
	}

	private bool CheckHealthThresholdToFlee()
	{
		if (base.healthFraction > Stats.HealthThresholdForFleeing)
		{
			if (Stats.HealthThresholdForFleeing < 1f)
			{
				SetFact(Facts.IsUnderHealthThreshold, 0);
				return false;
			}
			if (GetFact(Facts.HasEnemy) == 1)
			{
				SetFact(Facts.IsUnderHealthThreshold, 0);
				return false;
			}
		}
		bool flag = UnityEngine.Random.value < Stats.HealthThresholdFleeChance;
		SetFact(Facts.IsUnderHealthThreshold, (byte)(flag ? 1 : 0));
		return flag;
	}

	private void WantsToFlee()
	{
		if (GetFact(Facts.WantsToFlee) != 1 && IsNavRunning())
		{
			SetFact(Facts.WantsToFlee, 1);
			maxFleeTime = UnityEngine.Time.realtimeSinceStartup + Stats.MaxFleeTime;
		}
	}

	private void TickFlee()
	{
		if (UnityEngine.Time.realtimeSinceStartup > maxFleeTime || (IsNavRunning() && NavAgent.remainingDistance <= NavAgent.stoppingDistance + 1f))
		{
			SetFact(Facts.WantsToFlee, 0);
			SetFact(Facts.IsFleeing, 0);
			Stats.HealthThresholdForFleeing = base.healthFraction * fleeHealthThresholdPercentage;
		}
	}

	private void FindCoverFromEnemy()
	{
		AiContext.CoverSet.Reset();
		if (AttackTarget != null)
		{
			FindCoverFromPosition(AiContext.EnemyPosition);
		}
	}

	private void FindCoverFromPosition(Vector3 position)
	{
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		CoverPoint retreat = null;
		CoverPoint flank = null;
		CoverPoint advance = null;
		AiContext.CoverSet.Reset();
		foreach (CoverPoint sampledCoverPoint in AiContext.sampledCoverPoints)
		{
			if (!sampledCoverPoint.IsReserved && !sampledCoverPoint.IsCompromised && sampledCoverPoint.ProvidesCoverFromPoint(position, -0.8f))
			{
				Vector3 vector = sampledCoverPoint.Position - ServerPosition;
				Vector3 vector2 = position - ServerPosition;
				float num4 = Vector3.Dot(vector.normalized, vector2.normalized);
				if (!(num4 > 0.5f) || !(vector.sqrMagnitude > vector2.sqrMagnitude))
				{
					if (num4 <= -0.5f)
					{
						if (vector.sqrMagnitude < MinDistanceToRetreatCover * MinDistanceToRetreatCover)
						{
							num4 = 0.1f;
						}
						else
						{
							float num5 = num4 * -1f;
							if (num5 > num)
							{
								num = num5;
								retreat = sampledCoverPoint;
							}
						}
					}
					if (num4 >= 0.5f)
					{
						float sqrMagnitude = vector.sqrMagnitude;
						if (sqrMagnitude > vector2.sqrMagnitude)
						{
							continue;
						}
						float num6 = num4;
						if (num6 > num3)
						{
							if (AI.npc_cover_use_path_distance && IsNavRunning() && AttackTarget != null && !PathDistanceIsValid(AttackTarget.ServerPosition, sampledCoverPoint.Position))
							{
								continue;
							}
							if ((sampledCoverPoint.Position - position).sqrMagnitude < sqrMagnitude)
							{
								num6 *= 0.9f;
							}
							num3 = num6;
							advance = sampledCoverPoint;
						}
					}
					if (num4 >= -0.1f && num4 <= 0.1f)
					{
						float num7 = 1f - Mathf.Abs(num4);
						if (num7 > num2 && (!AI.npc_cover_use_path_distance || !IsNavRunning() || !(AttackTarget != null) || PathDistanceIsValid(AttackTarget.ServerPosition, sampledCoverPoint.Position)))
						{
							num2 = 0.1f - Mathf.Abs(num7);
							flank = sampledCoverPoint;
						}
					}
				}
			}
		}
		AiContext.CoverSet.Update(retreat, flank, advance);
	}

	public bool PathDistanceIsValid(Vector3 from, Vector3 to, bool allowCloseRange = false)
	{
		float sqrMagnitude = (from - to).sqrMagnitude;
		if (sqrMagnitude > Stats.MediumRange * Stats.MediumRange || (!allowCloseRange && sqrMagnitude < Stats.CloseRange * Stats.CloseRange))
		{
			return true;
		}
		float num = Mathf.Sqrt(sqrMagnitude);
		if (_pathCache == null)
		{
			_pathCache = new NavMeshPath();
		}
		if (NavMesh.CalculatePath(from, to, GetNavAgent.areaMask, _pathCache))
		{
			int cornersNonAlloc = _pathCache.GetCornersNonAlloc(pathCornerCache);
			if (_pathCache.status == NavMeshPathStatus.PathComplete && cornersNonAlloc > 1)
			{
				float num2 = PathDistance(cornersNonAlloc, ref pathCornerCache, num + AI.npc_cover_path_vs_straight_dist_max_diff);
				if (Mathf.Abs(num - num2) > AI.npc_cover_path_vs_straight_dist_max_diff)
				{
					return false;
				}
			}
		}
		return true;
	}

	private float PathDistance(int count, ref Vector3[] path, float maxDistance)
	{
		if (count < 2)
		{
			return 0f;
		}
		Vector3 a = path[0];
		float num = 0f;
		for (int i = 0; i < count; i++)
		{
			Vector3 vector = path[i];
			num += Vector3.Distance(a, vector);
			a = vector;
			if (num > maxDistance)
			{
				return num;
			}
		}
		return num;
	}

	private void FindClosestCoverToUs()
	{
		float num = float.MaxValue;
		CoverPoint coverPoint = null;
		AiContext.CoverSet.Reset();
		foreach (CoverPoint sampledCoverPoint in AiContext.sampledCoverPoints)
		{
			if (!sampledCoverPoint.IsReserved && !sampledCoverPoint.IsCompromised)
			{
				float sqrMagnitude = (sampledCoverPoint.Position - ServerPosition).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					coverPoint = sampledCoverPoint;
				}
			}
		}
		if (coverPoint != null)
		{
			AiContext.CoverSet.Closest.ReservedCoverPoint = coverPoint;
		}
	}

	public int PeekNextWaypointIndex()
	{
		if (WaypointSet == null || WaypointSet.Points.Count == 0)
		{
			return CurrentWaypointIndex;
		}
		int currentWaypointIndex = CurrentWaypointIndex;
		switch (WaypointSet.NavMode)
		{
		case WaypointSet.NavModes.Loop:
			currentWaypointIndex++;
			if (currentWaypointIndex >= WaypointSet.Points.Count)
			{
				currentWaypointIndex = 0;
			}
			else if (currentWaypointIndex < 0)
			{
				currentWaypointIndex = WaypointSet.Points.Count - 1;
			}
			break;
		case WaypointSet.NavModes.PingPong:
			currentWaypointIndex += WaypointDirection;
			if (currentWaypointIndex >= WaypointSet.Points.Count)
			{
				currentWaypointIndex = CurrentWaypointIndex - 1;
			}
			else if (currentWaypointIndex < 0)
			{
				currentWaypointIndex = 0;
			}
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		return currentWaypointIndex;
	}

	public int GetNextWaypointIndex()
	{
		if (WaypointSet == null || WaypointSet.Points.Count == 0 || WaypointSet.Points[PeekNextWaypointIndex()].IsOccupied)
		{
			return CurrentWaypointIndex;
		}
		int currentWaypointIndex = CurrentWaypointIndex;
		if (currentWaypointIndex >= 0 && currentWaypointIndex < WaypointSet.Points.Count)
		{
			WaypointSet.Waypoint value = WaypointSet.Points[currentWaypointIndex];
			value.IsOccupied = false;
			WaypointSet.Points[currentWaypointIndex] = value;
		}
		switch (WaypointSet.NavMode)
		{
		case WaypointSet.NavModes.Loop:
			currentWaypointIndex++;
			if (currentWaypointIndex >= WaypointSet.Points.Count)
			{
				currentWaypointIndex = 0;
			}
			else if (currentWaypointIndex < 0)
			{
				currentWaypointIndex = WaypointSet.Points.Count - 1;
			}
			break;
		case WaypointSet.NavModes.PingPong:
			currentWaypointIndex += WaypointDirection;
			if (currentWaypointIndex >= WaypointSet.Points.Count)
			{
				currentWaypointIndex = CurrentWaypointIndex - 1;
				WaypointDirection = -1;
			}
			else if (currentWaypointIndex < 0)
			{
				currentWaypointIndex = 0;
				WaypointDirection = 1;
			}
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		if (currentWaypointIndex >= 0 && currentWaypointIndex < WaypointSet.Points.Count)
		{
			WaypointSet.Waypoint value2 = WaypointSet.Points[currentWaypointIndex];
			value2.IsOccupied = true;
			WaypointSet.Points[currentWaypointIndex] = value2;
		}
		return currentWaypointIndex;
	}

	public Transform GetLookatPointFromWaypoints()
	{
		LookAtEyes = null;
		if (WaypointSet == null || WaypointSet.Points.Count == 0)
		{
			return null;
		}
		WaypointSet.Waypoint waypoint = WaypointSet.Points[CurrentWaypointIndex];
		if (waypoint.LookatPoints != null && waypoint.LookatPoints.Length != 0)
		{
			return waypoint.LookatPoints[UnityEngine.Random.Range(0, waypoint.LookatPoints.Length)];
		}
		return null;
	}

	private Transform GetLookatPoint(ref Transform[] points)
	{
		LookAtEyes = null;
		if (points != null && points.Length != 0)
		{
			return points[UnityEngine.Random.Range(0, points.Length)];
		}
		return null;
	}

	public void LookAtRandomPoint(float nextTimeAddition = 5f)
	{
		if (UnityEngine.Time.realtimeSinceStartup > nextLookAtPointTime)
		{
			LookAtEyes = null;
			nextLookAtPointTime = UnityEngine.Time.realtimeSinceStartup + nextTimeAddition;
			LookAtPoint = GetLookatPointFromWaypoints();
			if (LookAtPoint == null && LookAtInterestPointsStationary != null)
			{
				LookAtPoint = GetLookatPoint(ref LookAtInterestPointsStationary);
			}
		}
	}

	public int TopologyPreference()
	{
		return -1;
	}

	public bool IsInCommunicationRange(NPCPlayerApex npc)
	{
		if (npc != null && !npc.IsDestroyed && npc.transform != null && npc.Health() > 0f)
		{
			return (npc.ServerPosition - ServerPosition).sqrMagnitude <= CommunicationRadius * CommunicationRadius;
		}
		return false;
	}

	public virtual int GetAlliesInRange(out List<Scientist> allies)
	{
		allies = null;
		return 0;
	}

	public virtual void SendStatement(AiStatement_EnemyEngaged statement)
	{
	}

	public virtual void SendStatement(AiStatement_EnemySeen statement)
	{
	}

	public virtual void OnAiStatement(NPCPlayerApex source, AiStatement_EnemyEngaged statement)
	{
	}

	public virtual void OnAiStatement(NPCPlayerApex source, AiStatement_EnemySeen statement)
	{
	}

	public virtual int AskQuestion(AiQuestion_ShareEnemyTarget question, out List<AiAnswer_ShareEnemyTarget> answers)
	{
		answers = null;
		return 0;
	}

	public AiAnswer_ShareEnemyTarget OnAiQuestion(NPCPlayerApex source, AiQuestion_ShareEnemyTarget question)
	{
		AiAnswer_ShareEnemyTarget aiAnswer_ShareEnemyTarget = default(AiAnswer_ShareEnemyTarget);
		aiAnswer_ShareEnemyTarget.Source = this;
		aiAnswer_ShareEnemyTarget.PlayerTarget = AiContext?.EnemyPlayer;
		AiAnswer_ShareEnemyTarget result = aiAnswer_ShareEnemyTarget;
		if (AiContext?.EnemyPlayer != null)
		{
			Memory.SeenInfo info = AiContext.Memory.GetInfo(AiContext.EnemyPlayer);
			if (info.Entity != null && !info.Entity.IsDestroyed && !AiContext.EnemyPlayer.IsDead())
			{
				result.LastKnownPosition = info.Position;
				if ((object)source != null && source.AiContext?.AiLocationManager?.LocationType == AiLocationSpawner.SquadSpawnerLocation.BanditTown)
				{
					source.AiContext.LastAttacker = result.PlayerTarget;
					source.lastAttackedTime = lastAttackedTime;
				}
			}
			else
			{
				result.PlayerTarget = null;
			}
		}
		return result;
	}

	public void InitFacts()
	{
		SetFact(Facts.CanTargetEnemies, 1);
	}

	public byte GetFact(Facts fact)
	{
		return CurrentFacts[(int)fact];
	}

	public void SetFact(Facts fact, byte value, bool triggerCallback = true, bool onlyTriggerCallbackOnDiffValue = true)
	{
		byte b = CurrentFacts[(int)fact];
		CurrentFacts[(int)fact] = value;
		if (triggerCallback && (!onlyTriggerCallbackOnDiffValue || value != b))
		{
			OnFactChanged(fact, b, value);
		}
	}

	public EnemyRangeEnum ToEnemyRangeEnum(float sqrRange)
	{
		if (sqrRange <= ToSqrRange(EnemyRangeEnum.CloseAttackRange))
		{
			return EnemyRangeEnum.CloseAttackRange;
		}
		if (sqrRange <= ToSqrRange(EnemyRangeEnum.MediumAttackRange))
		{
			return EnemyRangeEnum.MediumAttackRange;
		}
		if (sqrRange <= ToSqrRange(EnemyRangeEnum.LongAttackRange))
		{
			return EnemyRangeEnum.LongAttackRange;
		}
		return EnemyRangeEnum.OutOfRange;
	}

	public EnemyEngagementRangeEnum ToEnemyEngagementRangeEnum(float sqrRange)
	{
		if (sqrRange <= ToSqrRange(EnemyEngagementRangeEnum.AggroRange))
		{
			return EnemyEngagementRangeEnum.AggroRange;
		}
		if (sqrRange > ToSqrRange(EnemyEngagementRangeEnum.DeaggroRange))
		{
			return EnemyEngagementRangeEnum.DeaggroRange;
		}
		return EnemyEngagementRangeEnum.NeutralRange;
	}

	public float ToSqrRange(EnemyRangeEnum range)
	{
		switch (range)
		{
		case EnemyRangeEnum.CloseAttackRange:
			return Stats.CloseRange * Stats.CloseRange;
		case EnemyRangeEnum.MediumAttackRange:
			return Stats.MediumRange * Stats.MediumRange;
		case EnemyRangeEnum.LongAttackRange:
			return Stats.LongRange * Stats.LongRange;
		default:
			return float.PositiveInfinity;
		}
	}

	public float ToSqrRange(EnemyEngagementRangeEnum range)
	{
		switch (range)
		{
		case EnemyEngagementRangeEnum.AggroRange:
			return Stats.AggressionRange * Stats.AggressionRange;
		case EnemyEngagementRangeEnum.DeaggroRange:
			return Stats.DeaggroRange * Stats.DeaggroRange;
		default:
			return float.PositiveInfinity;
		}
	}

	public float GetActiveAggressionRangeSqr()
	{
		if (GetFact(Facts.IsAggro) == 1)
		{
			return Stats.DeaggroRange * Stats.DeaggroRange;
		}
		return Stats.AggressionRange * Stats.AggressionRange;
	}

	public bool IsWithinAggroRange(EnemyRangeEnum range)
	{
		float sqrRange = ToSqrRange(range);
		EnemyEngagementRangeEnum enemyEngagementRangeEnum = ToEnemyEngagementRangeEnum(sqrRange);
		if (enemyEngagementRangeEnum != 0)
		{
			if (GetFact(Facts.IsAggro) == 1)
			{
				return enemyEngagementRangeEnum == EnemyEngagementRangeEnum.NeutralRange;
			}
			return false;
		}
		return true;
	}

	public bool IsWithinAggroRange(float sqrRange)
	{
		EnemyEngagementRangeEnum enemyEngagementRangeEnum = ToEnemyEngagementRangeEnum(sqrRange);
		if (enemyEngagementRangeEnum != 0)
		{
			if (GetFact(Facts.IsAggro) == 1)
			{
				return enemyEngagementRangeEnum == EnemyEngagementRangeEnum.NeutralRange;
			}
			return false;
		}
		return true;
	}

	public bool IsBeyondDeaggroRange(EnemyRangeEnum range)
	{
		float sqrRange = ToSqrRange(range);
		return ToEnemyEngagementRangeEnum(sqrRange) == EnemyEngagementRangeEnum.DeaggroRange;
	}

	public AfraidRangeEnum ToAfraidRangeEnum(float sqrRange)
	{
		if (sqrRange <= Stats.AfraidRange * Stats.AfraidRange)
		{
			return AfraidRangeEnum.InAfraidRange;
		}
		return AfraidRangeEnum.OutOfRange;
	}

	public HealthEnum ToHealthEnum(float healthNormalized)
	{
		if (healthNormalized >= 0.75f)
		{
			return HealthEnum.Fine;
		}
		if (healthNormalized >= 0.25f)
		{
			return HealthEnum.Medium;
		}
		return HealthEnum.Low;
	}

	public SpeedEnum ToSpeedEnum(float speed)
	{
		if (speed <= 0.01f)
		{
			return SpeedEnum.StandStill;
		}
		if (speed <= AI.npc_speed_crouch_walk)
		{
			return SpeedEnum.CrouchWalk;
		}
		if (speed <= AI.npc_speed_walk)
		{
			return SpeedEnum.Walk;
		}
		if (speed <= AI.npc_speed_crouch_run)
		{
			return SpeedEnum.CrouchRun;
		}
		if (speed <= AI.npc_speed_run)
		{
			return SpeedEnum.Run;
		}
		return SpeedEnum.Sprint;
	}

	public float ToSpeed(SpeedEnum speed)
	{
		switch (speed)
		{
		case SpeedEnum.StandStill:
			return 0f;
		case SpeedEnum.CrouchWalk:
			return AI.npc_speed_crouch_walk * Stats.Speed;
		case SpeedEnum.Walk:
			return AI.npc_speed_walk * Stats.Speed;
		case SpeedEnum.CrouchRun:
			return AI.npc_speed_crouch_run * Stats.Speed;
		case SpeedEnum.Run:
			return AI.npc_speed_run * Stats.Speed;
		default:
			return AI.npc_speed_sprint * Stats.Speed;
		}
	}

	public AmmoStateEnum GetCurrentAmmoStateEnum()
	{
		AttackEntity attackEntity = GetHeldEntity() as AttackEntity;
		if (attackEntity == null)
		{
			return AmmoStateEnum.Empty;
		}
		BaseProjectile baseProjectile = attackEntity as BaseProjectile;
		if ((bool)baseProjectile)
		{
			if (baseProjectile.primaryMagazine.contents == 0)
			{
				return AmmoStateEnum.Empty;
			}
			float num = (float)baseProjectile.primaryMagazine.contents / (float)baseProjectile.primaryMagazine.capacity;
			if (num < 0.3f)
			{
				return AmmoStateEnum.Low;
			}
			if (num < 0.65f)
			{
				return AmmoStateEnum.Medium;
			}
			if (num < 1f)
			{
				return AmmoStateEnum.High;
			}
			return AmmoStateEnum.Full;
		}
		return AmmoStateEnum.Full;
	}

	public WeaponTypeEnum GetCurrentWeaponTypeEnum()
	{
		HeldEntity heldEntity = GetHeldEntity();
		if (heldEntity == null)
		{
			return WeaponTypeEnum.None;
		}
		AttackEntity attackEntity = heldEntity as AttackEntity;
		if (attackEntity == null)
		{
			return WeaponTypeEnum.None;
		}
		return attackEntity.effectiveRangeType;
	}

	public WeaponTypeEnum GetWeaponTypeEnum(BaseProjectile proj)
	{
		if ((bool)proj)
		{
			return proj.effectiveRangeType;
		}
		return WeaponTypeEnum.None;
	}

	public EnemyRangeEnum WeaponToEnemyRange(WeaponTypeEnum weapon)
	{
		switch (weapon)
		{
		case WeaponTypeEnum.None:
		case WeaponTypeEnum.CloseRange:
			return EnemyRangeEnum.CloseAttackRange;
		case WeaponTypeEnum.MediumRange:
			return EnemyRangeEnum.MediumAttackRange;
		case WeaponTypeEnum.LongRange:
			return EnemyRangeEnum.LongAttackRange;
		default:
			return EnemyRangeEnum.OutOfRange;
		}
	}

	public EnemyRangeEnum CurrentWeaponToEnemyRange()
	{
		WeaponTypeEnum currentWeaponTypeEnum = GetCurrentWeaponTypeEnum();
		return WeaponToEnemyRange(currentWeaponTypeEnum);
	}

	public byte GetPathStatus()
	{
		if (!IsNavRunning())
		{
			return 2;
		}
		return (byte)NavAgent.pathStatus;
	}

	public NavMeshPathStatus ToPathStatus(byte value)
	{
		return (NavMeshPathStatus)value;
	}

	public ToolTypeEnum GetCurrentToolTypeEnum()
	{
		HeldEntity heldEntity = GetHeldEntity();
		if (heldEntity == null)
		{
			return ToolTypeEnum.None;
		}
		return heldEntity.toolType;
	}

	public void TickReasoningSystem()
	{
		SetFact(Facts.HasEnemy, (byte)((AttackTarget != null) ? 1 : 0));
		_GatherPlayerTargetFacts();
		if (base.isMounted)
		{
			_UpdateMountedSelfFacts();
		}
		else
		{
			_UpdateGroundedSelfFacts();
			_UpdateCoverFacts();
		}
		if (AttackTarget != null)
		{
			Memory.ExtendedInfo extendedInfo = AiContext.Memory.GetExtendedInfo(AttackTarget);
			if (extendedInfo.Entity != null)
			{
				TryAggro(extendedInfo.DistanceSqr);
			}
		}
	}

	private void _GatherPlayerTargetFacts()
	{
		if (AttackTarget != null)
		{
			float num = float.MaxValue;
			byte b = 0;
			if (PlayerTargetContext.Index >= 0)
			{
				int index = PlayerTargetContext.Index;
				num = PlayerTargetContext.DistanceSqr[index];
				b = PlayerTargetContext.LineOfSight[index];
				lastSeenPlayerTime = UnityEngine.Time.time;
			}
			else
			{
				Memory.ExtendedInfo extendedInfo = AiContext.Memory.GetExtendedInfo(AttackTarget);
				if (!(extendedInfo.Entity != null))
				{
					_NoEnemyFacts();
					return;
				}
				num = extendedInfo.DistanceSqr;
				b = extendedInfo.LineOfSight;
			}
			SetFact(Facts.EnemyRange, (byte)ToEnemyRangeEnum(num));
			SetFact(Facts.EnemyEngagementRange, (byte)ToEnemyEngagementRangeEnum(num));
			SetFact(Facts.AfraidRange, (byte)ToAfraidRangeEnum(num));
			SetFact(Facts.HasLineOfSight, (byte)((b > 0) ? 1 : 0));
			SetFact(Facts.HasLineOfSightStanding, (byte)((b == 1 || b == 3) ? 1 : 0));
			SetFact(Facts.HasLineOfSightCrouched, (byte)((b == 2 || b == 3) ? 1 : 0));
		}
		else
		{
			_NoEnemyFacts();
		}
	}

	private void _NoEnemyFacts()
	{
		SetFact(Facts.EnemyRange, 3);
		SetFact(Facts.EnemyEngagementRange, 1);
		SetFact(Facts.AfraidRange, 1);
		SetFact(Facts.HasLineOfSight, 0);
		SetFact(Facts.HasLineOfSightCrouched, 0);
		SetFact(Facts.HasLineOfSightStanding, 0);
	}

	private void _UpdateMountedSelfFacts()
	{
		SetFact(Facts.Health, (byte)ToHealthEnum(base.healthFraction));
		SetFact(Facts.IsWeaponAttackReady, (byte)((UnityEngine.Time.realtimeSinceStartup >= NextAttackTime()) ? 1 : 0));
		SetFact(Facts.AttackedLately, (byte)((base.SecondsSinceAttacked < Stats.AttackedMemoryTime) ? 1 : 0));
		SetFact(Facts.AttackedVeryRecently, (byte)((base.SecondsSinceAttacked < 2f) ? 1 : 0));
		SetFact(Facts.AttackedRecently, (byte)((base.SecondsSinceAttacked < 7f) ? 1 : 0));
		SetFact(Facts.CanSwitchWeapon, (byte)((UnityEngine.Time.realtimeSinceStartup > NextWeaponSwitchTime) ? 1 : 0));
		SetFact(Facts.CurrentAmmoState, (byte)GetCurrentAmmoStateEnum());
		SetFact(Facts.CurrentWeaponType, (byte)GetCurrentWeaponTypeEnum());
	}

	private void _UpdateGroundedSelfFacts()
	{
		SetFact(Facts.Health, (byte)ToHealthEnum(base.healthFraction));
		SetFact(Facts.IsWeaponAttackReady, (byte)((UnityEngine.Time.realtimeSinceStartup >= NextAttackTime()) ? 1 : 0));
		SetFact(Facts.IsRoamReady, (byte)((UnityEngine.Time.realtimeSinceStartup >= AiContext.NextRoamTime && IsNavRunning()) ? 1 : 0));
		SetFact(Facts.Speed, (byte)ToSpeedEnum(TargetSpeed / Stats.Speed));
		SetFact(Facts.AttackedLately, (byte)((base.SecondsSinceAttacked < Stats.AttackedMemoryTime) ? 1 : 0));
		SetFact(Facts.AttackedVeryRecently, (byte)((base.SecondsSinceAttacked < 2f) ? 1 : 0));
		SetFact(Facts.AttackedRecently, (byte)((base.SecondsSinceAttacked < 7f) ? 1 : 0));
		SetFact(Facts.IsMoving, IsMoving(), true, false);
		SetFact(Facts.CanSwitchWeapon, (byte)((UnityEngine.Time.realtimeSinceStartup > NextWeaponSwitchTime) ? 1 : 0));
		SetFact(Facts.CanSwitchTool, (byte)((UnityEngine.Time.realtimeSinceStartup > NextToolSwitchTime) ? 1 : 0));
		SetFact(Facts.CurrentAmmoState, (byte)GetCurrentAmmoStateEnum());
		SetFact(Facts.CurrentWeaponType, (byte)GetCurrentWeaponTypeEnum());
		SetFact(Facts.CurrentToolType, (byte)GetCurrentToolTypeEnum());
		SetFact(Facts.ExplosiveInRange, (byte)((AiContext.DeployedExplosives.Count > 0) ? 1 : 0));
		SetFact(Facts.IsMobile, (byte)(Stats.IsMobile ? 1 : 0));
		SetFact(Facts.HasWaypoints, (byte)((WaypointSet != null && WaypointSet.Points.Count > 0) ? 1 : 0));
		EnemyRangeEnum rangeToSpawnPoint = GetRangeToSpawnPoint();
		SetFact(Facts.RangeToSpawnLocation, (byte)rangeToSpawnPoint);
		if ((int)rangeToSpawnPoint < (int)Stats.MaxRangeToSpawnLoc || (Stats.MaxRangeToSpawnLoc == EnemyRangeEnum.CloseAttackRange && rangeToSpawnPoint == EnemyRangeEnum.CloseAttackRange))
		{
			lastInRangeOfSpawnPositionTime = UnityEngine.Time.time;
		}
		if (CheckHealthThresholdToFlee())
		{
			WantsToFlee();
		}
	}

	private void _UpdateCoverFacts()
	{
		if (GetFact(Facts.HasEnemy) == 1)
		{
			SetFact(Facts.RetreatCoverInRange, (byte)((AiContext.CoverSet.Retreat.ReservedCoverPoint != null) ? 1 : 0));
			SetFact(Facts.FlankCoverInRange, (byte)((AiContext.CoverSet.Flank.ReservedCoverPoint != null) ? 1 : 0));
			SetFact(Facts.AdvanceCoverInRange, (byte)((AiContext.CoverSet.Advance.ReservedCoverPoint != null) ? 1 : 0));
			SetFact(Facts.CoverInRange, (byte)((AiContext.CoverSet.Closest.ReservedCoverPoint != null) ? 1 : 0));
			if (GetFact(Facts.IsMovingToCover) == 1)
			{
				SetFact(Facts.IsMovingToCover, IsMoving());
			}
			Memory.ExtendedInfo extendedInfo = AiContext.Memory.GetExtendedInfo(AttackTarget);
			if (extendedInfo.Entity != null)
			{
				if (base.isMounted)
				{
					SetFact(Facts.AimsAtTarget, (byte)((extendedInfo.Dot > AI.npc_valid_mounted_aim_cone) ? 1 : 0));
				}
				else
				{
					SetFact(Facts.AimsAtTarget, (byte)((extendedInfo.Dot > AI.npc_valid_aim_cone) ? 1 : 0));
				}
			}
		}
		else
		{
			SetFact(Facts.RetreatCoverInRange, 0);
			SetFact(Facts.FlankCoverInRange, 0);
			SetFact(Facts.AdvanceCoverInRange, 0);
			SetFact(Facts.CoverInRange, (byte)((AiContext.CoverSet.Closest.ReservedCoverPoint != null) ? 1 : 0));
			SetFact(Facts.IsMovingToCover, 0);
			SetFact(Facts.AimsAtTarget, 0);
		}
		if (AiContext.CoverSet.Closest.ReservedCoverPoint != null)
		{
			byte b = (byte)(((AiContext.CoverSet.Closest.ReservedCoverPoint.Position - ServerPosition).sqrMagnitude < 0.5625f) ? 1 : 0);
			SetFact(Facts.IsInCover, b);
			if (b == 1)
			{
				SetFact(Facts.IsCoverCompromised, (byte)(AiContext.CoverSet.Closest.ReservedCoverPoint.IsCompromised ? 1 : 0));
			}
		}
		if (GetFact(Facts.IsRetreatingToCover) == 1)
		{
			SetFact(Facts.IsRetreatingToCover, IsMoving());
		}
	}

	private void TickSenses()
	{
		if (Query.Server != null && AiContext != null && !IsDormant)
		{
			if (UnityEngine.Time.realtimeSinceStartup > lastTickTime + SensesTickRate)
			{
				TickVision();
				TickHearing();
				TickSmell();
				AiContext.Memory.Forget(ForgetUnseenEntityTime);
				lastTickTime = UnityEngine.Time.realtimeSinceStartup;
			}
			TickEnemyAwareness();
			if (base.isMounted)
			{
				UpdateMountedSelfFacts();
			}
			else
			{
				UpdateSelfFacts();
			}
		}
	}

	public static float Distance2DSqr(Vector3 a, Vector3 b)
	{
		return (new Vector2(a.x, a.z) - new Vector2(b.x, b.z)).sqrMagnitude;
	}

	private void TickVision()
	{
		AiContext.Players.Clear();
		AiContext.Npcs.Clear();
		AiContext.DeployedExplosives.Clear();
		if (IsMountableAgent)
		{
			AiContext.Chairs.Clear();
		}
		if (base.isMounted)
		{
			for (int i = 0; i < BasePlayer.activePlayerList.Count; i++)
			{
				BasePlayer basePlayer = BasePlayer.activePlayerList[i];
				if (!(basePlayer == null) && basePlayer.isServer && !AI.ignoreplayers && !basePlayer.IsSleeping() && !basePlayer.IsDead() && Distance2DSqr(basePlayer.ServerPosition, ServerPosition) <= Stats.VisionRange * Stats.VisionRange)
				{
					AiContext.Players.Add(basePlayer);
				}
			}
		}
		else
		{
			if (Query.Server == null)
			{
				return;
			}
			int num = 0;
			num = ((!AI.npc_ignore_chairs) ? Query.Server.GetInSphere(base.transform.position, Stats.VisionRange, SensesResults, AiCaresAbout) : Query.Server.GetInSphere(base.transform.position, Stats.VisionRange, SensesResults, AiCaresAboutIgnoreChairs));
			if (num == 0)
			{
				return;
			}
			for (int j = 0; j < num; j++)
			{
				BaseEntity baseEntity = SensesResults[j];
				if (baseEntity == null || baseEntity == this || !baseEntity.isServer)
				{
					continue;
				}
				BasePlayer basePlayer2 = baseEntity as BasePlayer;
				if (basePlayer2 != null)
				{
					if (!AI.ignoreplayers && !(basePlayer2 is HTNPlayer) && !(basePlayer2 is NPCPlayer) && !basePlayer2.IsSleeping() && !basePlayer2.IsDead())
					{
						AiContext.Players.Add(baseEntity as BasePlayer);
					}
				}
				else if (baseEntity is BaseNpc)
				{
					AiContext.Npcs.Add(baseEntity as BaseNpc);
				}
				else if (baseEntity is TimedExplosive)
				{
					TimedExplosive timedExplosive = baseEntity as TimedExplosive;
					if ((ServerPosition - timedExplosive.ServerPosition).sqrMagnitude < (timedExplosive.explosionRadius + 2f) * (timedExplosive.explosionRadius + 2f))
					{
						AiContext.DeployedExplosives.Add(timedExplosive);
					}
				}
				else if (IsMountableAgent && !AI.npc_ignore_chairs && baseEntity is BaseChair)
				{
					AiContext.Chairs.Add(baseEntity as BaseChair);
				}
			}
			float num2 = float.MaxValue;
			foreach (BasePlayer player in AiContext.Players)
			{
				float sqrMagnitude = (player.ServerPosition - ServerPosition).sqrMagnitude;
				if (sqrMagnitude < num2 && !player.IsDead() && !player.IsDestroyed)
				{
					num2 = sqrMagnitude;
					AiContext.ClosestPlayer = player;
				}
			}
			sensesTicksSinceLastCoverSweep++;
			if (sensesTicksSinceLastCoverSweep > 5)
			{
				FindCoverPoints();
				sensesTicksSinceLastCoverSweep = 0;
			}
		}
	}

	public bool IsVisibleMounted(BasePlayer player)
	{
		Vector3 worldMountedPosition = eyes.worldMountedPosition;
		if (!player.IsVisible(worldMountedPosition, player.CenterPoint()) && !player.IsVisible(worldMountedPosition, player.transform.position) && !player.IsVisible(worldMountedPosition, player.eyes.position))
		{
			return false;
		}
		if (!IsVisible(player.CenterPoint(), worldMountedPosition) && !IsVisible(player.transform.position, worldMountedPosition) && !IsVisible(player.eyes.position, worldMountedPosition))
		{
			return false;
		}
		return true;
	}

	public bool IsVisibleStanding(BasePlayer player)
	{
		Vector3 worldStandingPosition = eyes.worldStandingPosition;
		if (!player.IsVisible(worldStandingPosition, player.CenterPoint()) && !player.IsVisible(worldStandingPosition, player.transform.position) && !player.IsVisible(worldStandingPosition, player.eyes.position))
		{
			return false;
		}
		if (!IsVisible(player.CenterPoint(), worldStandingPosition) && !IsVisible(player.transform.position, worldStandingPosition) && !IsVisible(player.eyes.position, worldStandingPosition))
		{
			return false;
		}
		return true;
	}

	public bool IsVisibleCrouched(BasePlayer player)
	{
		Vector3 worldCrouchedPosition = eyes.worldCrouchedPosition;
		if (!player.IsVisible(worldCrouchedPosition, player.CenterPoint()) && !player.IsVisible(worldCrouchedPosition, player.transform.position) && !player.IsVisible(worldCrouchedPosition, player.eyes.position))
		{
			return false;
		}
		if (!IsVisible(player.CenterPoint(), worldCrouchedPosition) && !IsVisible(player.transform.position, worldCrouchedPosition) && !IsVisible(player.eyes.position, worldCrouchedPosition))
		{
			return false;
		}
		return true;
	}

	public bool IsVisibleStanding(BaseNpc npc)
	{
		Vector3 vector = eyes.transform.position + eyes.transform.up * PlayerEyes.EyeOffset.y;
		if (!npc.IsVisible(vector, npc.CenterPoint()))
		{
			return false;
		}
		if (!IsVisible(npc.CenterPoint(), vector))
		{
			return false;
		}
		return true;
	}

	public bool IsVisibleCrouched(BaseNpc npc)
	{
		Vector3 vector = eyes.transform.position + eyes.transform.up * (PlayerEyes.EyeOffset.y + PlayerEyes.DuckOffset.y);
		if (!npc.IsVisible(vector, npc.CenterPoint()))
		{
			return false;
		}
		if (!IsVisible(npc.CenterPoint(), vector))
		{
			return false;
		}
		return true;
	}

	private void FindCoverPoints()
	{
		if (SingletonComponent<AiManager>.Instance == null || !SingletonComponent<AiManager>.Instance.enabled || !SingletonComponent<AiManager>.Instance.UseCover)
		{
			return;
		}
		if (AiContext.sampledCoverPoints.Count > 0)
		{
			AiContext.sampledCoverPoints.Clear();
		}
		if (AiContext.CurrentCoverVolume == null || !AiContext.CurrentCoverVolume.Contains(AiContext.Position))
		{
			AiContext.CurrentCoverVolume = SingletonComponent<AiManager>.Instance.GetCoverVolumeContaining(AiContext.Position);
			bool flag = AiContext.CurrentCoverVolume == null;
		}
		if (AiContext.CurrentCoverVolume != null)
		{
			foreach (CoverPoint coverPoint in AiContext.CurrentCoverVolume.CoverPoints)
			{
				if (!coverPoint.IsReserved)
				{
					Vector3 position = coverPoint.Position;
					if (!((AiContext.Position - position).sqrMagnitude > MaxDistanceToCover * MaxDistanceToCover))
					{
						AiContext.sampledCoverPoints.Add(coverPoint);
					}
				}
			}
			if (AiContext.sampledCoverPoints.Count > 0)
			{
				AiContext.sampledCoverPoints.Sort(coverPointComparer);
			}
		}
	}

	private void TickHearing()
	{
		SetFact(Facts.LoudNoiseNearby, 0);
	}

	private void TickSmell()
	{
	}

	private void TickMountableAwareness()
	{
		SelectMountable();
	}

	private void SelectMountable()
	{
		if (AiContext.Chairs.Count == 0 && !base.isMounted)
		{
			AiContext.ChairTarget = null;
			SetFact(Facts.IsMounted, 0);
		}
		else
		{
			TargetClosestChair();
		}
	}

	private void TargetClosestChair()
	{
		float num = float.MaxValue;
		foreach (BaseChair chair in AiContext.Chairs)
		{
			if (!chair.IsMounted())
			{
				float sqrMagnitude = (chair.ServerPosition - ServerPosition).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					AiContext.ChairTarget = chair;
				}
			}
		}
	}

	private void TickEnemyAwareness()
	{
		if (GetFact(Facts.CanTargetEnemies) == 0 && blockTargetingThisEnemy == null)
		{
			AiContext.EnemyNpc = null;
			AiContext.EnemyPlayer = null;
			SetFact(Facts.HasEnemy, 0);
			SetFact(Facts.EnemyRange, 3);
			SetFact(Facts.IsAggro, 0, false);
		}
		else
		{
			SelectEnemy();
		}
	}

	private void SelectEnemy()
	{
		if (AiContext.Players.Count == 0 && AiContext.Npcs.Count == 0)
		{
			AiContext.EnemyNpc = null;
			AiContext.EnemyPlayer = null;
			SetFact(Facts.HasEnemy, 0);
			SetFact(Facts.EnemyRange, 3);
			SetFact(Facts.IsAggro, 0, false);
		}
		else if (base.isMounted)
		{
			AggroClosestPlayerMounted();
		}
		else
		{
			AggroBestScorePlayerOrClosestAnimal();
		}
	}

	private void AggroClosestPlayerMounted()
	{
		float num = float.MaxValue;
		bool lineOfSightStanding = false;
		bool lineOfSightCrouched = false;
		BasePlayer player = null;
		foreach (BasePlayer player2 in AiContext.Players)
		{
			if (!player2.IsDead() && !player2.IsDestroyed && (!Stats.OnlyAggroMarkedTargets || HostilityConsideration(player2)))
			{
				bool flag = IsVisibleMounted(player2);
				if (flag)
				{
					AiContext.Memory.Update(player2);
				}
				Vector3 vector = player2.ServerPosition - ServerPosition;
				BaseMountable mounted = GetMounted();
				if (!(Vector3.Dot(vector.normalized, mounted.transform.forward) < -0.1f))
				{
					float sqrMagnitude = vector.sqrMagnitude;
					if (sqrMagnitude < num)
					{
						num = sqrMagnitude;
						player = player2;
						lineOfSightStanding = flag;
						lineOfSightCrouched = flag;
					}
				}
			}
		}
		SetAttackTarget(player, 1f, num, lineOfSightStanding, lineOfSightCrouched);
	}

	private void AggroBestScorePlayerOrClosestAnimal()
	{
		float num = float.MaxValue;
		float num2 = 0f;
		bool flag = false;
		bool flag2 = false;
		BasePlayer basePlayer = null;
		BaseNpc baseNpc = null;
		AiContext.AIAgent.AttackTarget = null;
		Vector3 zero = Vector3.zero;
		float sqrRange = float.MaxValue;
		foreach (BasePlayer player in AiContext.Players)
		{
			if (!player.IsDead() && !player.IsDestroyed && (!(blockTargetingThisEnemy != null) || player.net == null || blockTargetingThisEnemy.net == null || player.net.ID != blockTargetingThisEnemy.net.ID) && (!Stats.OnlyAggroMarkedTargets || HostilityConsideration(player)))
			{
				NPCPlayerApex nPCPlayerApex = player as NPCPlayerApex;
				if (!(nPCPlayerApex != null) || Stats.Family != nPCPlayerApex.Stats.Family)
				{
					float num3 = 0f;
					Vector3 vector = player.ServerPosition - ServerPosition;
					float sqrMagnitude = vector.sqrMagnitude;
					if (sqrMagnitude < num)
					{
						num = sqrMagnitude;
					}
					if (sqrMagnitude < Stats.VisionRange * Stats.VisionRange)
					{
						num3 += VisionRangeScore;
					}
					if (sqrMagnitude < Stats.AggressionRange * Stats.AggressionRange)
					{
						num3 += AggroRangeScore;
					}
					switch (ToEnemyRangeEnum(sqrMagnitude))
					{
					case EnemyRangeEnum.LongAttackRange:
						num3 += LongRangeScore;
						break;
					case EnemyRangeEnum.MediumAttackRange:
						num3 += MediumRangeScore;
						break;
					case EnemyRangeEnum.CloseAttackRange:
						num3 += CloseRangeScore;
						break;
					}
					bool flag3 = IsVisibleStanding(player);
					bool flag4 = false;
					if (!flag3)
					{
						flag4 = IsVisibleCrouched(player);
					}
					if (!flag3 && !flag4)
					{
						if (AiContext.Memory.GetInfo(player).Entity == null || !IsWithinAggroRange(sqrMagnitude))
						{
							continue;
						}
						num3 *= 0.75f;
					}
					else
					{
						AiContext.Memory.Update(player);
					}
					float dist = Mathf.Sqrt(sqrMagnitude);
					num3 *= VisibilityScoreModifier(player, vector, dist, flag3, flag4);
					if (num3 > num2)
					{
						basePlayer = player;
						baseNpc = null;
						zero = vector;
						sqrRange = sqrMagnitude;
						num2 = num3;
						flag = flag3;
						flag2 = flag4;
					}
				}
			}
		}
		List<AiAnswer_ShareEnemyTarget> answers;
		if (!base.isMounted && basePlayer == null && AskQuestion(default(AiQuestion_ShareEnemyTarget), out answers) > 0)
		{
			foreach (AiAnswer_ShareEnemyTarget item in answers)
			{
				if (item.PlayerTarget != null && item.LastKnownPosition.HasValue && HostilityConsideration(item.PlayerTarget))
				{
					basePlayer = item.PlayerTarget;
					baseNpc = null;
					zero = item.LastKnownPosition.Value - ServerPosition;
					sqrRange = zero.sqrMagnitude;
					num2 = 100f;
					num = zero.sqrMagnitude;
					flag = IsVisibleStanding(basePlayer);
					flag2 = false;
					if (!flag)
					{
						flag2 = IsVisibleCrouched(basePlayer);
					}
					AiContext.Memory.Update(basePlayer, item.LastKnownPosition.Value);
					break;
				}
			}
		}
		if (num > 0.1f && num2 < 10f)
		{
			bool flag5 = basePlayer != null && num <= Stats.AggressionRange;
			foreach (BaseNpc npc in AiContext.Npcs)
			{
				if (!npc.IsDead() && !npc.IsDestroyed && Stats.Family != npc.Stats.Family)
				{
					Vector3 vector2 = npc.ServerPosition - ServerPosition;
					float sqrMagnitude2 = vector2.sqrMagnitude;
					if (sqrMagnitude2 < num)
					{
						EnemyRangeEnum enemyRangeEnum = ToEnemyRangeEnum(sqrMagnitude2);
						if ((!flag5 || (int)enemyRangeEnum <= 0) && (int)enemyRangeEnum <= 1)
						{
							num = sqrMagnitude2;
							baseNpc = npc;
							basePlayer = null;
							zero = vector2;
							sqrRange = sqrMagnitude2;
							flag2 = false;
							flag = IsVisibleStanding(npc);
							if (!flag)
							{
								flag2 = IsVisibleCrouched(npc);
							}
							if (flag | flag2)
							{
								AiContext.Memory.Update(npc);
							}
							if (num < 0.1f)
							{
								break;
							}
						}
					}
				}
			}
		}
		AiContext.EnemyPlayer = basePlayer;
		AiContext.EnemyNpc = baseNpc;
		AiContext.LastTargetScore = num2;
		if ((basePlayer != null && !basePlayer.IsDestroyed && !basePlayer.IsDead()) || baseNpc != null)
		{
			SetFact(Facts.HasEnemy, 1, true, false);
			if (basePlayer != null)
			{
				AiContext.AIAgent.AttackTarget = basePlayer;
			}
			else
			{
				AiContext.AIAgent.AttackTarget = baseNpc;
			}
			EnemyRangeEnum enemyRangeEnum2 = ToEnemyRangeEnum(sqrRange);
			AfraidRangeEnum value = ToAfraidRangeEnum(sqrRange);
			SetFact(Facts.EnemyRange, (byte)enemyRangeEnum2);
			SetFact(Facts.AfraidRange, (byte)value);
			bool flag6 = flag | flag2;
			SetFact(Facts.HasLineOfSight, (byte)(flag6 ? 1 : 0));
			SetFact(Facts.HasLineOfSightCrouched, (byte)(flag2 ? 1 : 0));
			SetFact(Facts.HasLineOfSightStanding, (byte)(flag ? 1 : 0));
			if (basePlayer != null && flag6)
			{
				lastSeenPlayerTime = UnityEngine.Time.time;
			}
			TryAggro(enemyRangeEnum2);
		}
		else
		{
			SetFact(Facts.HasEnemy, 0);
			SetFact(Facts.EnemyRange, 3);
			SetFact(Facts.AfraidRange, 1);
			SetFact(Facts.HasLineOfSight, 0);
			SetFact(Facts.HasLineOfSightCrouched, 0);
			SetFact(Facts.HasLineOfSightStanding, 0);
		}
	}

	protected void SetAttackTarget(BasePlayer player, float score, float sqrDistance, bool lineOfSightStanding, bool lineOfSightCrouched, bool tryAggro = true)
	{
		if (player != null && !player.IsDestroyed && !player.IsDead())
		{
			AiContext.EnemyPlayer = player;
			AiContext.EnemyNpc = null;
			AiContext.LastTargetScore = score;
			SetFact(Facts.HasEnemy, 1, true, false);
			AiContext.AIAgent.AttackTarget = player;
			EnemyRangeEnum enemyRangeEnum = ToEnemyRangeEnum(sqrDistance);
			AfraidRangeEnum value = ToAfraidRangeEnum(sqrDistance);
			SetFact(Facts.EnemyRange, (byte)enemyRangeEnum);
			SetFact(Facts.AfraidRange, (byte)value);
			bool flag = lineOfSightStanding | lineOfSightCrouched;
			SetFact(Facts.HasLineOfSight, (byte)(flag ? 1 : 0));
			SetFact(Facts.HasLineOfSightCrouched, (byte)(lineOfSightCrouched ? 1 : 0));
			SetFact(Facts.HasLineOfSightStanding, (byte)(lineOfSightStanding ? 1 : 0));
			if (flag)
			{
				lastSeenPlayerTime = UnityEngine.Time.time;
			}
			if (tryAggro)
			{
				TryAggro(enemyRangeEnum);
			}
		}
		else
		{
			SetFact(Facts.HasEnemy, 0);
			SetFact(Facts.EnemyRange, 3);
			SetFact(Facts.AfraidRange, 1);
			SetFact(Facts.HasLineOfSight, 0);
			SetFact(Facts.HasLineOfSightCrouched, 0);
			SetFact(Facts.HasLineOfSightStanding, 0);
		}
	}

	private float VisibilityScoreModifier(BasePlayer target, Vector3 dir, float dist, bool losStand, bool losCrouch)
	{
		if (base.isMounted)
		{
			BaseMountable mounted = GetMounted();
			if (Vector3.Dot(dir.normalized, mounted.transform.forward) > -0.1f)
			{
				return 1f;
			}
			SetFact(Facts.HasEnemy, 0);
			SetFact(Facts.IsAggro, 0);
			AiContext.EnemyNpc = null;
			AiContext.EnemyPlayer = null;
			AttackTarget = null;
			return 0f;
		}
		float num = target.IsDucked() ? 0.5f : 1f;
		num *= (target.IsRunning() ? 1.5f : 1f);
		num *= ((target.estimatedSpeed <= 0.01f) ? 0.5f : 1f);
		float value = 1f;
		bool flag = false;
		Item activeItem = target.GetActiveItem();
		if (activeItem != null)
		{
			HeldEntity heldEntity = activeItem.GetHeldEntity() as HeldEntity;
			if (heldEntity != null)
			{
				flag = heldEntity.LightsOn();
			}
		}
		if (!flag)
		{
			value = Stats.DistanceVisibility.Evaluate(Mathf.Clamp01(dist / Stats.VisionRange));
			if (!losStand && losCrouch)
			{
				value *= 0.75f;
			}
			else if (losStand && !losCrouch)
			{
				value *= 0.9f;
			}
			if (num < 1f)
			{
				float num2 = Vector3.Dot(dir.normalized, eyes.HeadForward().normalized);
				value = ((num2 > Mathf.Abs(Stats.VisionCone)) ? (value * 1.5f) : ((!(num2 > 0f)) ? (value * (0.25f * num)) : (value * Mathf.Clamp01(num2 + num))));
			}
			else
			{
				value *= num;
			}
		}
		value = Mathf.Clamp01(value);
		float num3 = 0f;
		if (alertness > 0.5f)
		{
			return (UnityEngine.Random.value < value) ? value : 0f;
		}
		if (alertness > 0.01f)
		{
			return (UnityEngine.Random.value < value * alertness) ? value : 0f;
		}
		return (value > AI.npc_alertness_zero_detection_mod) ? value : 0f;
	}

	public bool HostilityConsideration(BasePlayer target)
	{
		if (target == null || target.transform == null || target.IsDestroyed || target.IsDead())
		{
			return true;
		}
		if (Stats.OnlyAggroMarkedTargets && target.HasPlayerFlag(PlayerFlags.SafeZone))
		{
			if (target.IsSleeping() && target.secondsSleeping >= NPCAutoTurret.sleeperhostiledelay)
			{
				return true;
			}
			return target.IsHostile();
		}
		if (Stats.Hostility > 0f)
		{
			return true;
		}
		if (Stats.Defensiveness > 0f && AiContext.LastAttacker == target && Stats.AttackedMemoryTime > base.SecondsSinceAttacked)
		{
			return true;
		}
		if (AiContext.AiLocationManager != null && AiContext.AiLocationManager.LocationType == AiLocationSpawner.SquadSpawnerLocation.BanditTown)
		{
			if (target.IsHostile())
			{
				return true;
			}
			if (target.IsSleeping() && target.secondsSleeping >= NPCAutoTurret.sleeperhostiledelay)
			{
				return true;
			}
		}
		return false;
	}

	private void UpdateMountedSelfFacts()
	{
		SetFact(Facts.Health, (byte)ToHealthEnum(base.healthFraction));
		SetFact(Facts.IsWeaponAttackReady, (byte)((UnityEngine.Time.realtimeSinceStartup >= NextAttackTime()) ? 1 : 0));
		SetFact(Facts.AttackedLately, (byte)((base.SecondsSinceAttacked < Stats.AttackedMemoryTime) ? 1 : 0));
		SetFact(Facts.AttackedVeryRecently, (byte)((base.SecondsSinceAttacked < 2f) ? 1 : 0));
		SetFact(Facts.AttackedRecently, (byte)((base.SecondsSinceAttacked < 7f) ? 1 : 0));
		SetFact(Facts.CanSwitchWeapon, (byte)((UnityEngine.Time.realtimeSinceStartup > NextWeaponSwitchTime) ? 1 : 0));
		SetFact(Facts.CurrentAmmoState, (byte)GetCurrentAmmoStateEnum());
		SetFact(Facts.CurrentWeaponType, (byte)GetCurrentWeaponTypeEnum());
	}

	private void UpdateSelfFacts()
	{
		if ((!float.IsNegativeInfinity(base.SecondsSinceAttacked) && base.SecondsSinceAttacked < Stats.AttackedMemoryTime) || (!float.IsNegativeInfinity(SecondsSinceSeenPlayer) && SecondsSinceSeenPlayer < Stats.AttackedMemoryTime))
		{
			alertness = 1f;
		}
		else if (alertness > 0f)
		{
			alertness = Mathf.Clamp01(alertness - AI.npc_alertness_drain_rate);
		}
		SetFact(Facts.Health, (byte)ToHealthEnum(base.healthFraction));
		SetFact(Facts.IsWeaponAttackReady, (byte)((UnityEngine.Time.realtimeSinceStartup >= NextAttackTime()) ? 1 : 0));
		SetFact(Facts.IsRoamReady, (byte)((UnityEngine.Time.realtimeSinceStartup >= AiContext.NextRoamTime && IsNavRunning()) ? 1 : 0));
		SetFact(Facts.Speed, (byte)ToSpeedEnum(TargetSpeed / Stats.Speed));
		SetFact(Facts.AttackedLately, (byte)((base.SecondsSinceAttacked < Stats.AttackedMemoryTime) ? 1 : 0));
		SetFact(Facts.AttackedVeryRecently, (byte)((base.SecondsSinceAttacked < 2f) ? 1 : 0));
		SetFact(Facts.AttackedRecently, (byte)((base.SecondsSinceAttacked < 7f) ? 1 : 0));
		SetFact(Facts.IsMoving, IsMoving(), true, false);
		SetFact(Facts.CanSwitchWeapon, (byte)((UnityEngine.Time.realtimeSinceStartup > NextWeaponSwitchTime) ? 1 : 0));
		SetFact(Facts.CanSwitchTool, (byte)((UnityEngine.Time.realtimeSinceStartup > NextToolSwitchTime) ? 1 : 0));
		SetFact(Facts.CurrentAmmoState, (byte)GetCurrentAmmoStateEnum());
		SetFact(Facts.CurrentWeaponType, (byte)GetCurrentWeaponTypeEnum());
		SetFact(Facts.CurrentToolType, (byte)GetCurrentToolTypeEnum());
		SetFact(Facts.ExplosiveInRange, (byte)((AiContext.DeployedExplosives.Count > 0) ? 1 : 0));
		SetFact(Facts.IsMobile, (byte)(Stats.IsMobile ? 1 : 0));
		SetFact(Facts.HasWaypoints, (byte)((WaypointSet != null && WaypointSet.Points.Count > 0) ? 1 : 0));
		EnemyRangeEnum rangeToSpawnPoint = GetRangeToSpawnPoint();
		SetFact(Facts.RangeToSpawnLocation, (byte)rangeToSpawnPoint);
		if ((int)rangeToSpawnPoint < (int)Stats.MaxRangeToSpawnLoc)
		{
			lastInRangeOfSpawnPositionTime = UnityEngine.Time.time;
		}
		if (CheckHealthThresholdToFlee())
		{
			WantsToFlee();
		}
		if (GetFact(Facts.HasEnemy) == 1)
		{
			FindCoverFromEnemy();
			SetFact(Facts.RetreatCoverInRange, (byte)((AiContext.CoverSet.Retreat.ReservedCoverPoint != null) ? 1 : 0));
			SetFact(Facts.FlankCoverInRange, (byte)((AiContext.CoverSet.Flank.ReservedCoverPoint != null) ? 1 : 0));
			SetFact(Facts.AdvanceCoverInRange, (byte)((AiContext.CoverSet.Advance.ReservedCoverPoint != null) ? 1 : 0));
			SetFact(Facts.CoverInRange, (byte)((AiContext.CoverSet.Closest.ReservedCoverPoint != null) ? 1 : 0));
			if (GetFact(Facts.IsMovingToCover) == 1)
			{
				SetFact(Facts.IsMovingToCover, IsMoving());
			}
			Vector3 normalized = (AttackTarget.ServerPosition - ServerPosition).normalized;
			float num = Vector3.Dot(eyes.BodyForward(), normalized);
			if (base.isMounted)
			{
				SetFact(Facts.AimsAtTarget, (byte)((num > AI.npc_valid_mounted_aim_cone) ? 1 : 0));
			}
			else
			{
				SetFact(Facts.AimsAtTarget, (byte)((num > AI.npc_valid_aim_cone) ? 1 : 0));
			}
		}
		else
		{
			FindClosestCoverToUs();
			SetFact(Facts.RetreatCoverInRange, 0);
			SetFact(Facts.FlankCoverInRange, 0);
			SetFact(Facts.AdvanceCoverInRange, 0);
			SetFact(Facts.CoverInRange, (byte)((AiContext.CoverSet.Closest.ReservedCoverPoint != null) ? 1 : 0));
			SetFact(Facts.IsMovingToCover, 0);
			SetFact(Facts.AimsAtTarget, 0);
		}
		if (AiContext.CoverSet.Closest.ReservedCoverPoint != null)
		{
			byte b = (byte)(((AiContext.CoverSet.Closest.ReservedCoverPoint.Position - ServerPosition).sqrMagnitude < 0.5625f) ? 1 : 0);
			SetFact(Facts.IsInCover, b);
			if (b == 1)
			{
				SetFact(Facts.IsCoverCompromised, (byte)(AiContext.CoverSet.Closest.ReservedCoverPoint.IsCompromised ? 1 : 0));
			}
		}
		if (GetFact(Facts.IsRetreatingToCover) == 1)
		{
			SetFact(Facts.IsRetreatingToCover, IsMoving());
		}
	}

	private EnemyRangeEnum GetRangeToSpawnPoint()
	{
		float num = ToSqrRange(Stats.MaxRangeToSpawnLoc) * 2f;
		float sqrMagnitude = (ServerPosition - SpawnPosition).sqrMagnitude;
		if (sqrMagnitude > num)
		{
			return EnemyRangeEnum.OutOfRange;
		}
		return ToEnemyRangeEnum(sqrMagnitude);
	}

	private byte IsMoving()
	{
		return (byte)((IsNavRunning() && NavAgent.hasPath && NavAgent.remainingDistance > NavAgent.stoppingDistance && !IsStuck && !IsStopped) ? 1 : 0);
	}

	private float NextAttackTime()
	{
		AttackEntity attackEntity = GetHeldEntity() as AttackEntity;
		if (attackEntity == null)
		{
			return float.PositiveInfinity;
		}
		return attackEntity.NextAttackTime;
	}

	public void SetTargetPathStatus(float pendingDelay = 0.05f)
	{
		if (!isAlreadyCheckingPathPending)
		{
			if (NavAgent.pathPending && numPathPendingAttempts < 10)
			{
				isAlreadyCheckingPathPending = true;
				Invoke(DelayedTargetPathStatus, pendingDelay);
			}
			else
			{
				numPathPendingAttempts = 0;
				accumPathPendingDelay = 0f;
				SetFact(Facts.PathToTargetStatus, GetPathStatus());
			}
		}
	}

	private void DelayedTargetPathStatus()
	{
		numPathPendingAttempts++;
		accumPathPendingDelay += 0.1f;
		isAlreadyCheckingPathPending = false;
		SetTargetPathStatus(accumPathPendingDelay);
	}

	private static bool AiCaresAbout(BaseEntity ent)
	{
		if (ent is BasePlayer)
		{
			return true;
		}
		if (ent is BaseNpc)
		{
			return true;
		}
		if (ent is WorldItem)
		{
			return true;
		}
		if (ent is BaseCorpse)
		{
			return true;
		}
		if (ent is TimedExplosive)
		{
			return true;
		}
		if (ent is BaseChair)
		{
			return true;
		}
		return false;
	}

	private static bool AiCaresAboutIgnoreChairs(BaseEntity ent)
	{
		if (ent is BasePlayer)
		{
			return true;
		}
		if (ent is BaseNpc)
		{
			return true;
		}
		if (ent is WorldItem)
		{
			return true;
		}
		if (ent is BaseCorpse)
		{
			return true;
		}
		if (ent is TimedExplosive)
		{
			return true;
		}
		return false;
	}

	private static bool WithinVisionCone(NPCPlayerApex npc, BaseEntity other)
	{
		if (Mathf.Approximately(npc.Stats.VisionCone, -1f))
		{
			return true;
		}
		Vector3 normalized = (other.ServerPosition - npc.ServerPosition).normalized;
		if (Vector3.Dot(npc.ServerRotation * Vector3.forward, normalized) < npc.Stats.VisionCone)
		{
			return false;
		}
		return true;
	}

	private void ShutdownSensorySystem()
	{
		_selectPlayerTargetAI.Kill();
		_selectPlayerTargetMountedAI.Kill();
		_selectEntityTargetAI.Kill();
		_selectCoverTargetsAI.Kill();
		_selectEnemyHideoutAI.Kill();
	}

	public void TickSensorySystem()
	{
		if (Query.Server == null || AiContext == null || IsDormant)
		{
			return;
		}
		AiContext.Players.Clear();
		AiContext.Npcs.Clear();
		AiContext.DeployedExplosives.Clear();
		_FindPlayersInVisionRange();
		PlayerTargetContext.Refresh(this, PlayerQueryResults, PlayerQueryResultCount);
		if (base.isMounted)
		{
			SelectPlayerTargetMountedAI?.Execute();
			AiContext.EnemyPlayer = PlayerTargetContext.Target;
			AiContext.EnemyNpc = null;
			AttackTarget = PlayerTargetContext.Target;
		}
		else
		{
			SelectPlayerTargetAI?.Execute();
			_FindEntitiesInCloseRange();
			EntityTargetContext.Refresh(this, EntityQueryResults, EntityQueryResultCount);
			SelectEntityTargetAI?.Execute();
			byte b = 0;
			if (AiContext.EnemyPlayer == null || AiContext.EnemyPlayer.IsDestroyed || AiContext.EnemyPlayer.IsDead() || PlayerTargetContext.Score > AiContext.LastEnemyPlayerScore + DecisionMomentumPlayerTarget())
			{
				AiContext.EnemyPlayer = PlayerTargetContext.Target;
				AiContext.LastEnemyPlayerScore = PlayerTargetContext.Score;
				playerTargetDecisionStartTime = UnityEngine.Time.time;
				if (PlayerTargetContext.Index >= 0 && PlayerTargetContext.Index < PlayerTargetContext.LineOfSight.Length)
				{
					b = PlayerTargetContext.LineOfSight[PlayerTargetContext.Index];
				}
				else
				{
					Memory.ExtendedInfo extendedInfo = AiContext.Memory.GetExtendedInfo(AiContext.EnemyPlayer);
					if ((bool)extendedInfo.Entity)
					{
						b = extendedInfo.LineOfSight;
					}
				}
			}
			else if (PlayerTargetContext.Target == null && DecisionMomentumPlayerTarget() < 0.01f)
			{
				AiContext.EnemyPlayer = PlayerTargetContext.Target;
				AiContext.LastEnemyPlayerScore = 0f;
				playerTargetDecisionStartTime = 0f;
			}
			else
			{
				Memory.ExtendedInfo extendedInfo2 = AiContext.Memory.GetExtendedInfo(AiContext.EnemyPlayer);
				if ((bool)extendedInfo2.Entity)
				{
					b = extendedInfo2.LineOfSight;
				}
			}
			AiContext.ClosestPlayer = PlayerTargetContext.Target;
			if (AiContext.ClosestPlayer == null)
			{
				AiContext.ClosestPlayer = AiContext.EnemyPlayer;
			}
			if (AiContext.EnemyNpc == null || AiContext.EnemyNpc.IsDestroyed || AiContext.EnemyNpc.IsDead() || EntityTargetContext.AnimalScore > AiContext.LastEnemyNpcScore + DecisionMomentumAnimalTarget())
			{
				AiContext.EnemyNpc = EntityTargetContext.AnimalTarget;
				AiContext.LastEnemyNpcScore = EntityTargetContext.AnimalScore;
				animalTargetDecisionStartTime = UnityEngine.Time.time;
			}
			else if (EntityTargetContext.AnimalTarget == null && DecisionMomentumAnimalTarget() < 0.01f)
			{
				AiContext.EnemyNpc = EntityTargetContext.AnimalTarget;
				AiContext.LastEnemyNpcScore = 0f;
				animalTargetDecisionStartTime = 0f;
			}
			AiContext.DeployedExplosives.Clear();
			if (EntityTargetContext.ExplosiveTarget != null)
			{
				AiContext.DeployedExplosives.Add(EntityTargetContext.ExplosiveTarget);
			}
			AttackTarget = AiContext.EnemyPlayer;
			if (AttackTarget == null)
			{
				AttackTarget = AiContext.EnemyNpc;
			}
			if (AiContext.EnemyPlayer != null)
			{
				Memory.SeenInfo info = AiContext.Memory.GetInfo(AiContext.EnemyPlayer);
				bool flag = false;
				if (GetFact(Facts.IsMilitaryTunnelLab) > 0)
				{
					if (PathToPlayerTarget == null)
					{
						PathToPlayerTarget = new NavMeshPath();
					}
					flag = ((NavAgent != null && NavAgent.isOnNavMesh && !NavAgent.CalculatePath(AiContext.EnemyPlayer.ServerPosition, PathToPlayerTarget)) || PathToPlayerTarget.status != NavMeshPathStatus.PathComplete);
					SetFact(Facts.IncompletePathToTarget, (byte)(flag ? 1 : 0));
				}
				if (!flag)
				{
					_FindCoverPointsInVolume();
					CoverContext.Refresh(this, info.Position, AiContext.sampledCoverPoints);
					SelectCoverTargetsAI?.Execute();
					AiContext.CoverSet.Reset();
					AiContext.CoverSet.Update(CoverContext.BestRetreatCP, CoverContext.BestFlankCP, CoverContext.BestAdvanceCP);
					if (b == 0)
					{
						if (_FindCoverPointsInVolume(info.Position))
						{
							CoverContext.Refresh(this, info.Position, AiContext.EnemyCoverPoints);
							SelectEnemyHideoutAI?.Execute();
							AiContext.EnemyHideoutGuess = CoverContext.HideoutCP;
						}
					}
					else
					{
						AiContext.EnemyHideoutGuess = null;
					}
				}
			}
			else
			{
				AiContext.EnemyHideoutGuess = null;
			}
		}
		AiContext.Memory.Forget(ForgetUnseenEntityTime);
		AiContext.ForgetCheckedHideouts((float)ForgetUnseenEntityTime * 0.5f);
	}

	private float DecisionMomentumPlayerTarget()
	{
		float num = UnityEngine.Time.time - playerTargetDecisionStartTime;
		if (num > 1f)
		{
			return 0f;
		}
		return num;
	}

	private float DecisionMomentumAnimalTarget()
	{
		float num = UnityEngine.Time.time - animalTargetDecisionStartTime;
		if (num > 1f)
		{
			return 0f;
		}
		return num;
	}

	private void _FindPlayersInVisionRange()
	{
		if (!AI.ignoreplayers && !(base.transform == null) && Interface.CallHook("IOnNpcSenseVision", this) == null)
		{
			PlayerQueryResultCount = Query.Server.GetPlayersInSphere(base.transform.position, Stats.VisionRange, PlayerQueryResults, delegate(BasePlayer player)
			{
				if (player == null || !player.isServer || player.IsDead())
				{
					return false;
				}
				if (player.IsSleeping() && player.secondsSleeping < NPCAutoTurret.sleeperhostiledelay)
				{
					return false;
				}
				float num = Stats.VisionRange * Stats.VisionRange;
				return (!((player.ServerPosition - ServerPosition).sqrMagnitude > num)) ? true : false;
			});
		}
	}

	private void _FindEntitiesInCloseRange()
	{
		if (Interface.CallHook("IOnNpcSenseClose", this) == null)
		{
			EntityQueryResultCount = Query.Server.GetInSphere(base.transform.position, Stats.CloseRange, EntityQueryResults, delegate(BaseEntity entity)
			{
				if (entity == null || !entity.isServer || entity.IsDestroyed)
				{
					return false;
				}
				return (entity is BaseNpc || entity is TimedExplosive) ? true : false;
			});
		}
	}

	private bool _FindCoverPointsInVolume()
	{
		CoverPointVolume volume = AiContext.CurrentCoverVolume;
		return _FindCoverPointsInVolume(AiContext.Position, AiContext.sampledCoverPoints, ref volume, ref nextCoverInfoTick);
	}

	private bool _FindCoverPointsInVolume(Vector3 position)
	{
		CoverPointVolume volume = null;
		return _FindCoverPointsInVolume(position, AiContext.EnemyCoverPoints, ref volume, ref nextCoverPosInfoTick);
	}

	private bool _FindCoverPointsInVolume(Vector3 position, List<CoverPoint> coverPoints, ref CoverPointVolume volume, ref float timer)
	{
		if (SingletonComponent<AiManager>.Instance == null || !SingletonComponent<AiManager>.Instance.enabled || !SingletonComponent<AiManager>.Instance.UseCover)
		{
			return false;
		}
		if (UnityEngine.Time.time > timer)
		{
			timer = UnityEngine.Time.time + 0.1f * AI.npc_cover_info_tick_rate_multiplier;
			if (volume == null || !volume.Contains(position))
			{
				volume = SingletonComponent<AiManager>.Instance.GetCoverVolumeContaining(position);
				if (volume == null)
				{
					volume = AiManager.CreateNewCoverVolume(position, (AiContext.AiLocationManager != null) ? AiContext.AiLocationManager.CoverPointGroup : null);
				}
			}
		}
		if (volume != null)
		{
			if (coverPoints.Count > 0)
			{
				coverPoints.Clear();
			}
			float num = MaxDistanceToCover * MaxDistanceToCover;
			foreach (CoverPoint coverPoint in volume.CoverPoints)
			{
				if (!coverPoint.IsReserved && !coverPoint.IsCompromised)
				{
					Vector3 position2 = coverPoint.Position;
					if (!((position - position2).sqrMagnitude > num))
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

	public override void OnSensation(Sensation sensation)
	{
		if (AiContext == null || this is NPCMurderer)
		{
			return;
		}
		BasePlayer initiatorPlayer = sensation.InitiatorPlayer;
		if (OnlyTargetSensations && (initiatorPlayer == null || initiatorPlayer != AiContext.EnemyPlayer))
		{
			return;
		}
		switch (sensation.Type)
		{
		case SensationType.Gunshot:
			if (sensation.DamagePotential > 0f)
			{
				OnSenseGunshot(sensation, initiatorPlayer);
			}
			else
			{
				OnSenseItemOfInterest(sensation);
			}
			break;
		case SensationType.ThrownWeapon:
			if (sensation.DamagePotential > 0f)
			{
				OnSenseThrownThreat(sensation, initiatorPlayer);
			}
			else
			{
				OnSenseItemOfInterest(sensation);
			}
			break;
		}
	}

	protected virtual void OnSenseItemOfInterest(Sensation sensation)
	{
		bool flag = AttackTarget == null;
		if (sensation.InitiatorPlayer != null && AiContext.AiLocationManager != null && AiContext.AiLocationManager.LocationType == AiLocationSpawner.SquadSpawnerLocation.BanditTown && Family != sensation.InitiatorPlayer.Family && InSafeZone())
		{
			sensation.InitiatorPlayer.MarkHostileFor(30f);
		}
	}

	protected virtual void OnSenseThrownThreat(Sensation sensation, BasePlayer invoker)
	{
		if (AiContext.Memory.GetInfo(sensation.Position).Entity == null)
		{
			if (invoker != null)
			{
				Memory.ExtendedInfo extendedInfo;
				UpdateTargetMemory(invoker, 1f, sensation.Position, out extendedInfo);
			}
			else
			{
				AiContext.Memory.AddDanger(sensation.Position, 1f);
			}
		}
		else
		{
			Memory.ExtendedInfo extendedInfo2;
			UpdateTargetMemory(invoker, 1f, sensation.Position, out extendedInfo2);
		}
		_lastHeardGunshotTime = UnityEngine.Time.time;
		LastHeardGunshotDirection = (sensation.Position - base.transform.localPosition).normalized;
		if (invoker != null && AiContext.AiLocationManager != null && AiContext.AiLocationManager.LocationType == AiLocationSpawner.SquadSpawnerLocation.BanditTown && Family != invoker.Family && InSafeZone())
		{
			invoker.MarkHostileFor(30f);
		}
	}

	protected virtual void OnSenseGunshot(Sensation sensation, BasePlayer invoker)
	{
		if (AiContext.Memory.GetInfo(sensation.Position).Entity == null)
		{
			if (invoker != null)
			{
				Memory.ExtendedInfo extendedInfo;
				UpdateTargetMemory(invoker, 1f, sensation.Position, out extendedInfo);
			}
			else
			{
				AiContext.Memory.AddDanger(sensation.Position, 1f);
			}
		}
		else
		{
			Memory.ExtendedInfo extendedInfo2;
			UpdateTargetMemory(invoker, 1f, sensation.Position, out extendedInfo2);
		}
		_lastHeardGunshotTime = UnityEngine.Time.time;
		LastHeardGunshotDirection = (sensation.Position - base.transform.localPosition).normalized;
	}

	private void DelayedSpawnPosition()
	{
		SpawnPosition = GetPosition();
	}

	public override void ServerInit()
	{
		if (base.isClient)
		{
			return;
		}
		base.ServerInit();
		SpawnPosition = GetPosition();
		if (SpawnPosition.sqrMagnitude < 0.01f)
		{
			Invoke(DelayedSpawnPosition, 1f);
		}
		IsStuck = false;
		if (!NewAI)
		{
			return;
		}
		InitFacts();
		CurrentWaypointIndex = 0;
		IsWaitingAtWaypoint = false;
		WaypointDirection = 1;
		fleeHealthThresholdPercentage = Stats.HealthThresholdForFleeing;
		coverPointComparer = new CoverPointComparer(this);
		SwitchWeaponOperator.TrySwitchWeaponTo(AiContext, WeaponTypeEnum.MediumRange);
		DelayedReloadOnInit();
		NPCSensesLoadBalancer.NpcSensesLoadBalancer.Add(this);
		lastInvinsibleStartTime = UnityEngine.Time.time;
		if (AiContext.AiLocationManager == null)
		{
			float num = float.PositiveInfinity;
			AiLocationManager aiLocationManager = null;
			if (AiLocationManager.Managers != null && AiLocationManager.Managers.Count > 0)
			{
				foreach (AiLocationManager manager in AiLocationManager.Managers)
				{
					float sqrMagnitude = (manager.transform.position - ServerPosition).sqrMagnitude;
					if (sqrMagnitude < num)
					{
						num = sqrMagnitude;
						aiLocationManager = manager;
					}
				}
			}
			if (aiLocationManager != null && num <= Stats.DeaggroRange * Stats.DeaggroRange)
			{
				AiContext.AiLocationManager = aiLocationManager;
				if (AiContext.AiLocationManager.LocationType == AiLocationSpawner.SquadSpawnerLocation.JunkpileA || AiContext.AiLocationManager.LocationType == AiLocationSpawner.SquadSpawnerLocation.JunkpileG)
				{
					AllJunkpileNPCs.Add(this);
				}
				else if (AiContext.AiLocationManager.LocationType == AiLocationSpawner.SquadSpawnerLocation.BanditTown)
				{
					AllBanditCampNPCs.Add(this);
				}
			}
		}
		else if (AiContext.AiLocationManager.LocationType == AiLocationSpawner.SquadSpawnerLocation.JunkpileA || AiContext.AiLocationManager.LocationType == AiLocationSpawner.SquadSpawnerLocation.JunkpileG)
		{
			AllJunkpileNPCs.Add(this);
		}
		else if (AiContext.AiLocationManager.LocationType == AiLocationSpawner.SquadSpawnerLocation.BanditTown)
		{
			AllBanditCampNPCs.Add(this);
		}
	}

	private void DelayedReloadOnInit()
	{
		ReloadOperator.Reload(AiContext);
	}

	internal override void DoServerDestroy()
	{
		base.DoServerDestroy();
		if (NewAI)
		{
			if (AiContext != null && AiContext.AiLocationManager != null)
			{
				if (AiContext.AiLocationManager.LocationType == AiLocationSpawner.SquadSpawnerLocation.JunkpileA || AiContext.AiLocationManager.LocationType == AiLocationSpawner.SquadSpawnerLocation.JunkpileG)
				{
					AllJunkpileNPCs.Remove(this);
				}
				else if (AiContext.AiLocationManager.LocationType == AiLocationSpawner.SquadSpawnerLocation.BanditTown)
				{
					AllBanditCampNPCs.Remove(this);
				}
			}
			NPCSensesLoadBalancer.NpcSensesLoadBalancer.Remove(this);
		}
		CancelInvoke(RadioChatter);
	}

	float? ILoadBalanced.ExecuteUpdate(float deltaTime, float nextInterval)
	{
		float time = UnityEngine.Time.time;
		IsInvinsible = (time - lastInvinsibleStartTime < InvinsibleTime);
		if (time > nextSensorySystemTick)
		{
			using (TimeWarning.New("NPC.TickSensorySystem"))
			{
				TickSensorySystem();
			}
			nextSensorySystemTick = time + 0.1f * AI.npc_sensory_system_tick_rate_multiplier + UnityEngine.Random.value * 0.1f;
		}
		if (time > nextReasoningSystemTick)
		{
			using (TimeWarning.New("NPC.TickReasoningSystem"))
			{
				TickReasoningSystem();
			}
			nextReasoningSystemTick = time + 0.1f * AI.npc_reasoning_system_tick_rate_multiplier + UnityEngine.Random.value * 0.1f;
		}
		using (TimeWarning.New("NPC.TickBehaviourState"))
		{
			TickBehaviourState();
		}
		return UnityEngine.Random.value * 0.1f + 0.1f;
	}

	public void RadioChatter()
	{
		if (base.IsDestroyed || base.transform == null)
		{
			CancelInvoke(RadioChatter);
		}
		else if (RadioEffect.isValid)
		{
			Effect.server.Run(RadioEffect.resourcePath, this, StringPool.Get("head"), Vector3.zero, Vector3.zero);
		}
	}

	public override void OnKilled(HitInfo info)
	{
		base.OnKilled(info);
		OnDeath?.Invoke();
		RemoveFromJunkpiles();
		if (NewAI)
		{
			NPCSensesLoadBalancer.NpcSensesLoadBalancer.Remove(this);
			ShutdownSensorySystem();
		}
		CancelInvoke(RadioChatter);
		if (DeathEffect != null && DeathEffect.isValid)
		{
			Effect.server.Run(DeathEffect.resourcePath, this, 0u, Vector3.zero, Vector3.zero);
		}
		AddKilledStat(info);
	}

	private void AddKilledStat(HitInfo info)
	{
		if (info != null && !(info.InitiatorPlayer == null) && !info.InitiatorPlayer.IsNpc && info.InitiatorPlayer.stats != null && !string.IsNullOrEmpty(deathStatName))
		{
			info.InitiatorPlayer.stats.Add(deathStatName, 1);
		}
	}

	private void RemoveFromJunkpiles()
	{
		if (NewAI && AiContext != null && !(AiContext.AiLocationManager == null))
		{
			if (AiContext.AiLocationManager.LocationType == AiLocationSpawner.SquadSpawnerLocation.JunkpileA || AiContext.AiLocationManager.LocationType == AiLocationSpawner.SquadSpawnerLocation.JunkpileG)
			{
				AllJunkpileNPCs.Remove(this);
			}
			else if (AiContext.AiLocationManager.LocationType == AiLocationSpawner.SquadSpawnerLocation.BanditTown)
			{
				AllBanditCampNPCs.Remove(this);
			}
		}
	}

	public override void Hurt(HitInfo info)
	{
		if (IsInvinsible)
		{
			return;
		}
		if (AI.npc_families_no_hurt)
		{
			NPCPlayerApex nPCPlayerApex = info.Initiator as NPCPlayerApex;
			if (nPCPlayerApex != null && nPCPlayerApex.Family == Family)
			{
				return;
			}
		}
		base.Hurt(info);
		if (info.Initiator != null && AiContext != null)
		{
			float dmg = info.damageTypes.Total();
			if (info.InitiatorPlayer != null && AiContext.EnemyPlayer == null)
			{
				AiContext.EnemyPlayer = info.InitiatorPlayer;
			}
			else if (info.Initiator is BaseNpc)
			{
				AiContext.EnemyNpc = (BaseNpc)info.Initiator;
			}
			Memory.ExtendedInfo extendedInfo;
			UpdateTargetMemory(info.Initiator, dmg, out extendedInfo);
			AiContext.LastAttacker = info.Initiator;
			if (AiContext.CoverSet.Closest.ReservedCoverPoint != null && GetFact(Facts.IsInCover) > 0)
			{
				AiContext.CoverSet.Closest.ReservedCoverPoint.CoverIsCompromised(AI.npc_cover_compromised_cooldown);
			}
			if (TryAggro(extendedInfo.DistanceSqr) && AiContext.EnemyPlayer != null)
			{
				SetAttackTarget(AiContext.EnemyPlayer, 1f, extendedInfo.DistanceSqr, (extendedInfo.LineOfSight & 1) != 0, (extendedInfo.LineOfSight & 2) != 0, false);
			}
		}
	}

	public override void TickAi(float delta)
	{
		MovementUpdate(delta);
	}

	public override void MovementUpdate(float delta)
	{
		BaseMountable mounted = GetMounted();
		modelState.mounted = (mounted != null);
		modelState.poseType = (int)(modelState.mounted ? mounted.mountPose : PlayerModel.MountPoses.Chair);
		if (!AI.move || (!base.isMounted && !IsNavRunning()))
		{
			return;
		}
		if (IsDormant || !syncPosition)
		{
			StopMoving();
			return;
		}
		base.MovementUpdate(delta);
		if (base.isMounted)
		{
			timeAtDestination += delta;
		}
		else if ((IsNavRunning() && !NavAgent.hasPath) || Vector3Ex.Distance2D(NavAgent.destination, GetPosition()) < 1f)
		{
			timeAtDestination += delta;
		}
		else
		{
			timeAtDestination = 0f;
		}
		modelState.aiming = (timeAtDestination > 0.25f && AttackTarget != null && GetFact(Facts.HasLineOfSight) > 0 && GetFact(Facts.IsRetreatingToCover) == 0);
		TickStuck(delta);
	}

	protected override void UpdatePositionAndRotation(Vector3 moveToPosition)
	{
		if (TerrainMeta.HeightMap != null && AiContext.AiLocationManager != null && (AiContext.AiLocationManager.LocationType == AiLocationSpawner.SquadSpawnerLocation.JunkpileA || AiContext.AiLocationManager.LocationType == AiLocationSpawner.SquadSpawnerLocation.JunkpileG))
		{
			float height = TerrainMeta.HeightMap.GetHeight(moveToPosition);
			float num = moveToPosition.y - height;
			if (num > 0f)
			{
				moveToPosition.y = height;
			}
			else if (num < 0.5f)
			{
				moveToPosition.y = height;
			}
		}
		base.UpdatePositionAndRotation(moveToPosition);
	}

	public void TickStuck(float delta)
	{
		if (IsNavRunning() && !NavAgent.isStopped && (lastStuckPos - ServerPosition).sqrMagnitude < 0.0625f && AttackReady())
		{
			stuckDuration += delta;
			if (stuckDuration >= 5f && Mathf.Approximately(lastStuckTime, 0f))
			{
				lastStuckTime = UnityEngine.Time.time;
				OnBecomeStuck();
			}
			return;
		}
		stuckDuration = 0f;
		lastStuckPos = ServerPosition;
		if (UnityEngine.Time.time - lastStuckTime > 5f)
		{
			lastStuckTime = 0f;
			OnBecomeUnStuck();
		}
	}

	public void OnBecomeStuck()
	{
		IsStuck = true;
	}

	public void OnBecomeUnStuck()
	{
		IsStuck = false;
	}

	public void BehaviourChanged()
	{
		currentBehaviorDuration = 0f;
	}

	public override void ServerThink(float delta)
	{
		base.ServerThink(delta);
		currentBehaviorDuration += delta;
		UpdateAttackTargetVisibility(delta);
		SetFlag(Flags.Reserved3, AttackTarget != null && IsAlive());
	}

	public void UpdateAttackTargetVisibility(float delta)
	{
		if (AttackTarget == null || (lastAttackTarget != null && lastAttackTarget != AttackTarget) || GetFact(Facts.HasLineOfSight) == 0)
		{
			attackTargetVisibleFor = 0f;
		}
		else
		{
			attackTargetVisibleFor += delta;
		}
		lastAttackTarget = AttackTarget;
	}

	public void UpdateDestination(Vector3 newDest)
	{
		SetDestination(newDest);
	}

	public void UpdateDestination(Transform tx)
	{
		SetDestination(tx.position);
	}

	public override void SetDestination(Vector3 newDestination)
	{
		if (Interface.CallHook("OnNpcDestinationSet", this, newDestination) == null)
		{
			base.SetDestination(newDestination);
			Destination = newDestination;
		}
	}

	public float WeaponAttackRange()
	{
		AttackEntity attackEntity = GetHeldEntity() as AttackEntity;
		if (attackEntity == null)
		{
			return 0f;
		}
		return attackEntity.effectiveRange;
	}

	public void StopMoving()
	{
		if (Interface.CallHook("OnNpcStopMoving", this) == null)
		{
			IsStopped = true;
			finalDestination = GetPosition();
		}
	}

	public override float DesiredMoveSpeed()
	{
		float running = 0f;
		float ducking = modelState.ducked ? 1f : 0f;
		float num = 1f;
		if (CurrentBehaviour == BaseNpc.Behaviour.Wander)
		{
			num = AI.npc_speed_walk * 3f;
		}
		else
		{
			num = 1f;
			float num2 = Vector3.Dot(NavAgent.desiredVelocity.normalized, eyes.BodyForward());
			num2 = ((!(num2 > 0.75f)) ? 0f : Mathf.Clamp01((num2 - 0.75f) / 0.25f));
			running = num2;
		}
		return GetSpeed(running, ducking) * num;
	}

	public override Vector3 GetAimDirection()
	{
		if (base.isMounted)
		{
			BaseMountable mounted = GetMounted();
			if (CurrentBehaviour == BaseNpc.Behaviour.Attack && AttackTarget != null)
			{
				Vector3 b = Vector3.zero;
				BasePlayer basePlayer = AttackTarget as BasePlayer;
				if (basePlayer != null)
				{
					if (basePlayer.IsDucked())
					{
						b = PlayerEyes.DuckOffset;
					}
					else if (basePlayer.IsSleeping())
					{
						b = new Vector3(0f, -1f, 0f);
					}
				}
				else if (AttackTarget as BaseNpc != null)
				{
					b = new Vector3(0f, -0.5f, 0f);
				}
				Vector3 b2 = CenterPoint() + new Vector3(0f, 0f, 0f);
				Vector3 a = AttackTarget.CenterPoint();
				if (!AttackTarget.IsVisible(eyes.position, AttackTarget.CenterPoint()))
				{
					Memory.SeenInfo info = AiContext.Memory.GetInfo(AttackTarget);
					if (!(info.Entity != null) || !((info.Position - ServerPosition).sqrMagnitude > 4f))
					{
						return mounted.transform.forward;
					}
					a = info.Position;
				}
				return (a + b - b2).normalized;
			}
			return mounted.transform.forward;
		}
		if (LookAtEyes != null && LookAtEyes.transform != null && (CurrentBehaviour == BaseNpc.Behaviour.Wander || CurrentBehaviour == BaseNpc.Behaviour.Idle))
		{
			Vector3 b3 = CenterPoint();
			return (LookAtEyes.position + PlayerEyes.DuckOffset - b3).normalized;
		}
		if (LookAtPoint != null && (CurrentBehaviour == BaseNpc.Behaviour.Wander || CurrentBehaviour == BaseNpc.Behaviour.Idle))
		{
			Vector3 b4 = CenterPoint();
			return (LookAtPoint.position - b4).normalized;
		}
		if (_traversingNavMeshLink)
		{
			Vector3 vector = (!(AttackTarget != null)) ? (NavAgent.destination - ServerPosition) : (AttackTarget.ServerPosition - ServerPosition);
			if (vector.sqrMagnitude > 1f)
			{
				vector = _currentNavMeshLinkEndPos - ServerPosition;
			}
			if (vector.sqrMagnitude > 0.001f)
			{
				return _currentNavMeshLinkOrientation * Vector3.forward;
			}
		}
		if (CurrentBehaviour == BaseNpc.Behaviour.Wander || CurrentBehaviour == BaseNpc.Behaviour.RetreatingToCover)
		{
			if (IsNavRunning() && NavAgent.desiredVelocity.sqrMagnitude > 0.01f)
			{
				return NavAgent.desiredVelocity.normalized;
			}
			return base.transform.rotation * Vector3.forward;
		}
		if (CurrentBehaviour == BaseNpc.Behaviour.Attack && AttackTarget != null)
		{
			Vector3 b5 = Vector3.zero;
			BasePlayer basePlayer2 = AttackTarget as BasePlayer;
			if (basePlayer2 != null)
			{
				if (basePlayer2.IsDucked())
				{
					b5 = PlayerEyes.DuckOffset;
				}
				else if (basePlayer2.IsSleeping())
				{
					b5 = new Vector3(0f, -1f, 0f);
				}
			}
			else if (AttackTarget as BaseNpc != null)
			{
				b5 = new Vector3(0f, -0.5f, 0f);
			}
			Vector3 b6 = CenterPoint() + new Vector3(0f, 0f, 0f);
			Vector3 a2 = AttackTarget.CenterPoint();
			Memory.ExtendedInfo extendedInfo = AiContext.Memory.GetExtendedInfo(AttackTarget);
			if (extendedInfo.Entity == null || extendedInfo.LineOfSight == 0)
			{
				if (IsNavRunning() && NavAgent.desiredVelocity.sqrMagnitude > 0.01f && IsMoving() > 0)
				{
					return NavAgent.desiredVelocity.normalized;
				}
				return base.transform.rotation * Vector3.forward;
			}
			return (a2 + b5 - b6).normalized;
		}
		if (IsNavRunning() && NavAgent.desiredVelocity.sqrMagnitude > 0.01f)
		{
			return NavAgent.desiredVelocity.normalized;
		}
		return base.transform.rotation * Vector3.forward;
	}

	public override void SetAimDirection(Vector3 newAim)
	{
		if (!(newAim == Vector3.zero))
		{
			AttackEntity attackEntity = GetAttackEntity();
			if ((bool)attackEntity && (bool)AttackTarget && GetFact(Facts.HasLineOfSight) > 0 && CurrentBehaviour == BaseNpc.Behaviour.Attack)
			{
				float swayModifier = 1f;
				newAim = attackEntity.ModifyAIAim(newAim, swayModifier);
			}
			if (base.isMounted)
			{
				BaseMountable mounted = GetMounted();
				Vector3 eulerAngles = mounted.transform.eulerAngles;
				Quaternion rotation = Quaternion.Euler(Quaternion.LookRotation(newAim, mounted.transform.up).eulerAngles);
				Vector3 eulerAngles2 = Quaternion.LookRotation(base.transform.InverseTransformDirection(rotation * Vector3.forward), base.transform.up).eulerAngles;
				eulerAngles2 = BaseMountable.ConvertVector(eulerAngles2);
				Quaternion rotation2 = Quaternion.Euler(Mathf.Clamp(eulerAngles2.x, mounted.pitchClamp.x, mounted.pitchClamp.y), Mathf.Clamp(eulerAngles2.y, mounted.yawClamp.x, mounted.yawClamp.y), eulerAngles.z);
				newAim = BaseMountable.ConvertVector(Quaternion.LookRotation(base.transform.TransformDirection(rotation2 * Vector3.forward), base.transform.up).eulerAngles);
			}
			eyes.rotation = (base.isMounted ? Quaternion.Slerp(eyes.rotation, Quaternion.Euler(newAim), UnityEngine.Time.smoothDeltaTime * 70f) : Quaternion.LookRotation(newAim, base.transform.up));
			viewAngles = eyes.rotation.eulerAngles;
			ServerRotation = eyes.rotation;
		}
	}

	public void StartAttack()
	{
		if (IsAlive())
		{
			ShotTest();
			MeleeAttack();
		}
	}

	public Memory.SeenInfo UpdateTargetMemory(BaseEntity target, float dmg, out Memory.ExtendedInfo extendedInfo)
	{
		return UpdateTargetMemory(target, dmg, target.ServerPosition, out extendedInfo);
	}

	public Memory.SeenInfo UpdateTargetMemory(BaseEntity target, float dmg, Vector3 lastKnownPosition, out Memory.ExtendedInfo extendedInfo)
	{
		if (target == null || Interface.CallHook("OnNpcTarget", this, target) != null)
		{
			extendedInfo = default(Memory.ExtendedInfo);
			return default(Memory.SeenInfo);
		}
		Vector3 dir;
		float dot;
		if (base.isMounted)
		{
			BestMountedPlayerDirection.Evaluate(this, lastKnownPosition, out dir, out dot);
		}
		else
		{
			BestPlayerDirection.Evaluate(this, lastKnownPosition, out dir, out dot);
		}
		float distanceSqr;
		float aggroRangeSqr;
		BestPlayerDistance.Evaluate(this, lastKnownPosition, out distanceSqr, out aggroRangeSqr);
		BasePlayer basePlayer = target.ToPlayer();
		int standing;
		int crouched;
		byte b = (byte)((!basePlayer) ? 1 : ((!base.isMounted) ? BestLineOfSight.Evaluate(this, basePlayer, out standing, out crouched) : BestMountedLineOfSight.Evaluate(this, basePlayer)));
		SetFact(Facts.HasLineOfSight, b);
		return AiContext.Memory.Update(target, lastKnownPosition, dmg, dir, dot, distanceSqr, b, lastAttacker == target, lastAttackedTime, out extendedInfo);
	}

	public void StartAttack(AttackOperator.AttackType type, BaseCombatEntity target)
	{
		if (!IsAlive())
		{
			return;
		}
		AttackTarget = target;
		Memory.ExtendedInfo extendedInfo;
		UpdateTargetMemory(AttackTarget, 0.1f, out extendedInfo);
		if (type == AttackOperator.AttackType.CloseRange)
		{
			if (!MeleeAttack())
			{
				ShotTest();
			}
		}
		else
		{
			ShotTest();
		}
	}

	public override bool ShotTest()
	{
		if (base.ShotTest())
		{
			lastInvinsibleStartTime = 0f;
			return true;
		}
		return false;
	}

	public override void TriggerDown()
	{
		if (AttackTarget == null || (int)SwitchToolOperator.ReactiveAimsAtTarget.Test(AiContext) == 0)
		{
			CancelInvoke(TriggerDown);
			AttackEntity attackEntity = GetHeldEntity() as AttackEntity;
			nextTriggerTime = UnityEngine.Time.time + ((attackEntity != null) ? attackEntity.attackSpacing : 1f);
		}
		else
		{
			base.TriggerDown();
		}
	}

	public bool AttackReady()
	{
		return true;
	}

	public override string Categorize()
	{
		return "scientist";
	}
}
