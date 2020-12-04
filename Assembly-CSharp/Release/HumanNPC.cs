using ConVar;
using Facepunch;
using Oxide.Core;
using ProtoBuf;
using Rust.Ai;
using Rust.AI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class HumanNPC : NPCPlayer, IThinker
{
	public enum SpeedType
	{
		Crouch = 1,
		SlowWalk,
		Walk,
		Sprint
	}

	[Header("Loot")]
	public LootContainer.LootSpawnSlot[] LootSpawnSlots;

	public SpeedType desiredSpeed = SpeedType.SlowWalk;

	[Header("Detection")]
	public float sightRange = 30f;

	public float sightRangeLarge = 200f;

	public float visionCone = -0.8f;

	[Header("Damage")]
	public float aimConeScale = 2f;

	public List<BaseCombatEntity> _targets = new List<BaseCombatEntity>();

	public BaseCombatEntity currentTarget;

	private HumanBrain _brain;

	public float lastDismountTime;

	[NonSerialized]
	protected bool lightsOn;

	private bool navmeshEnabled;

	private const float TargetUpdateRate = 0.5f;

	private const float TickItemRate = 0.1f;

	public float nextZoneSearchTime;

	private AIInformationZone cachedInfoZone;

	[NonSerialized]
	public bool currentTargetLOS;

	[NonSerialized]
	public BaseEntity[] QueryResults = new BaseEntity[64];

	private SimpleAIMemory myMemory = new SimpleAIMemory();

	[NonSerialized]
	public float memoryDuration = 10f;

	private bool pendingDucked;

	private float timeSinceItemTick = 0.1f;

	private float timeSinceTargetUpdate = 0.5f;

	private float targetAimedDuration;

	private Vector3 aimOverridePosition = Vector3.zero;

	public override float StartHealth()
	{
		return startHealth;
	}

	public override float StartMaxHealth()
	{
		return startHealth;
	}

	public override float MaxHealth()
	{
		return startHealth;
	}

	public override bool IsNavRunning()
	{
		if (!base.isMounted)
		{
			return navmeshEnabled;
		}
		return false;
	}

	public override bool IsLoadBalanced()
	{
		return true;
	}

	public override void ServerInit()
	{
		base.ServerInit();
		_brain = GetComponent<HumanBrain>();
		if (!base.isClient)
		{
			AIThinkManager.Add(this);
			Invoke(EnableNavAgent, 0.25f);
		}
	}

	internal override void DoServerDestroy()
	{
		AIThinkManager.Remove(this);
		base.DoServerDestroy();
	}

	public void LightCheck()
	{
		if ((TOD_Sky.Instance.IsNight && !lightsOn) || (TOD_Sky.Instance.IsDay && lightsOn))
		{
			LightToggle();
			lightsOn = !lightsOn;
		}
	}

	public override float GetAimConeScale()
	{
		return aimConeScale;
	}

	public override void EquipWeapon()
	{
		base.EquipWeapon();
	}

	public override void DismountObject()
	{
		base.DismountObject();
		lastDismountTime = UnityEngine.Time.time;
	}

	public bool RecentlyDismounted()
	{
		return UnityEngine.Time.time < lastDismountTime + 10f;
	}

	public AITraversalArea GetTraversalArea()
	{
		if (triggers == null)
		{
			return null;
		}
		foreach (TriggerBase trigger in triggers)
		{
			AITraversalArea component = trigger.GetComponent<AITraversalArea>();
			if (component != null)
			{
				return component;
			}
		}
		return null;
	}

	public bool IsInTraversalArea()
	{
		if (triggers == null)
		{
			return false;
		}
		foreach (TriggerBase trigger in triggers)
		{
			if ((bool)trigger.GetComponent<AITraversalArea>())
			{
				return true;
			}
		}
		return false;
	}

	public virtual float GetIdealDistanceFromTarget()
	{
		return Mathf.Max(5f, EngagementRange() * 0.75f);
	}

	public void SetDesiredSpeed(SpeedType newSpeed)
	{
		if (newSpeed != desiredSpeed)
		{
			desiredSpeed = newSpeed;
		}
	}

	public float SpeedFromEnum(SpeedType newSpeed)
	{
		switch (newSpeed)
		{
		case SpeedType.Crouch:
			return 0.8f;
		case SpeedType.SlowWalk:
			return 1.5f;
		case SpeedType.Walk:
			return 2.5f;
		case SpeedType.Sprint:
			return 5f;
		default:
			return 0f;
		}
	}

	public List<BaseCombatEntity> GetTargets()
	{
		if (_targets == null)
		{
			_targets = Facepunch.Pool.GetList<BaseCombatEntity>();
		}
		return _targets;
	}

	public AIInformationZone GetInformationZone()
	{
		if (cachedInfoZone == null || UnityEngine.Time.time > nextZoneSearchTime)
		{
			cachedInfoZone = AIInformationZone.GetForPoint(base.transform.position, this);
			nextZoneSearchTime = UnityEngine.Time.time + 5f;
		}
		return cachedInfoZone;
	}

	public Vector3 GetRandomPositionAround(Vector3 position, float minDistFrom = 0f, float maxDistFrom = 2f)
	{
		if (maxDistFrom < 0f)
		{
			maxDistFrom = 0f;
		}
		Vector2 vector = UnityEngine.Random.insideUnitCircle * maxDistFrom;
		float x = Mathf.Clamp(Mathf.Max(Mathf.Abs(vector.x), minDistFrom), minDistFrom, maxDistFrom) * Mathf.Sign(vector.x);
		float z = Mathf.Clamp(Mathf.Max(Mathf.Abs(vector.y), minDistFrom), minDistFrom, maxDistFrom) * Mathf.Sign(vector.y);
		return position + new Vector3(x, 0f, z);
	}

	public Vector3 GetIdealPositionNear(Vector3 position, float maxDistFrom)
	{
		if (position == base.transform.position)
		{
			Vector2 vector = UnityEngine.Random.insideUnitCircle * maxDistFrom;
			return position + new Vector3(vector.x, 0f, vector.y);
		}
		Vector3 normalized = (base.transform.position - position).normalized;
		return position + normalized * maxDistFrom;
	}

	public bool HasAnyTargets()
	{
		return GetTargets().Count > 0;
	}

	public bool HasTarget()
	{
		return currentTarget != null;
	}

	public float EngagementRange()
	{
		AttackEntity attackEntity = GetHeldEntity() as AttackEntity;
		if ((bool)attackEntity)
		{
			return attackEntity.effectiveRange;
		}
		return sightRange;
	}

	public bool TargetInRange()
	{
		if (HasTarget())
		{
			return DistanceToTarget() <= EngagementRange();
		}
		return false;
	}

	public bool CanSeeTarget()
	{
		if (HasTarget())
		{
			return currentTargetLOS;
		}
		return false;
	}

	public void UpdateMemory()
	{
		int inSphere = Query.Server.GetInSphere(base.transform.position, sightRange, QueryResults, AiCaresAbout);
		for (int i = 0; i < inSphere; i++)
		{
			BaseEntity baseEntity = QueryResults[i];
			if (!(baseEntity == null) && !baseEntity.EqualNetID(this) && baseEntity.isServer && WithinVisionCone(baseEntity))
			{
				BasePlayer basePlayer = baseEntity as BasePlayer;
				if (!(basePlayer != null) || baseEntity.IsNpc || (!AI.ignoreplayers && IsVisibleToUs(basePlayer)))
				{
					myMemory.Update(baseEntity);
				}
			}
		}
		myMemory.Forget(memoryDuration);
	}

	public bool WithinVisionCone(BaseEntity other)
	{
		Vector3 rhs = Vector3Ex.Direction(other.transform.position, base.transform.position);
		if (Vector3.Dot(eyes.BodyForward(), rhs) < visionCone)
		{
			return false;
		}
		return true;
	}

	private static bool AiCaresAbout(BaseEntity ent)
	{
		if (ent is BasePlayer)
		{
			return true;
		}
		return false;
	}

	public float DistanceToTarget()
	{
		if (!(currentTarget == null))
		{
			return Vector3.Distance(base.transform.position, currentTarget.transform.position);
		}
		return -1f;
	}

	public void UpdateTargets(float delta)
	{
		UpdateMemory();
		int num = -1;
		float num2 = -1f;
		Vector3 position = base.transform.position;
		for (int i = 0; i < myMemory.All.Count; i++)
		{
			SimpleAIMemory.SeenInfo seenInfo = myMemory.All[i];
			if (seenInfo.Entity == null)
			{
				continue;
			}
			float num3 = 0f;
			float value = Vector3.Distance(seenInfo.Entity.transform.position, position);
			if (!seenInfo.Entity.IsNpc && !(seenInfo.Entity.Health() <= 0f))
			{
				num3 += 1f - Mathf.InverseLerp(10f, sightRange, value);
				float value2 = Vector3.Dot((seenInfo.Entity.transform.position - eyes.position).normalized, eyes.BodyForward());
				num3 += Mathf.InverseLerp(visionCone, 1f, value2);
				float value3 = seenInfo.Timestamp - UnityEngine.Time.realtimeSinceStartup;
				num3 += 1f - Mathf.InverseLerp(0f, 3f, value3);
				if (num3 > num2)
				{
					num = i;
					num2 = num3;
				}
			}
		}
		if (num != -1)
		{
			SimpleAIMemory.SeenInfo seenInfo2 = myMemory.All[num];
			if (seenInfo2.Entity != null && seenInfo2.Entity is BasePlayer)
			{
				BasePlayer component = seenInfo2.Entity.GetComponent<BasePlayer>();
				if (Interface.CallHook("OnNpcTarget", this, component) == null)
				{
					currentTarget = component;
					currentTargetLOS = IsVisibleToUs(component);
				}
			}
		}
		else
		{
			currentTarget = null;
			currentTargetLOS = false;
		}
	}

	public void SetDucked(bool wantsDucked)
	{
		if (Interface.CallHook("OnNpcDuck", this) == null)
		{
			pendingDucked = wantsDucked;
			ApplyPendingDucked();
		}
	}

	public void ApplyPendingDucked()
	{
		if (pendingDucked)
		{
			SetDesiredSpeed(SpeedType.Crouch);
		}
		modelState.ducked = pendingDucked;
		SendNetworkUpdate();
	}

	public virtual void TryThink()
	{
		ServerThink_Internal();
	}

	public override void ServerThink(float delta)
	{
		base.ServerThink(delta);
		if (_brain.ShouldThink())
		{
			_brain.DoThink();
		}
		timeSinceItemTick += delta;
		timeSinceTargetUpdate += delta;
		if (timeSinceItemTick > 0.1f)
		{
			TickItems(timeSinceItemTick);
			timeSinceItemTick = 0f;
		}
		if (timeSinceTargetUpdate > 0.5f)
		{
			UpdateTargets(timeSinceTargetUpdate);
			timeSinceTargetUpdate = 0f;
		}
	}

	public void TickItems(float delta)
	{
		if (desiredSpeed == SpeedType.Sprint || currentTarget == null)
		{
			targetAimedDuration = 0f;
			CancelBurst(0f);
			return;
		}
		if (currentTargetLOS)
		{
			if (Vector3.Dot(eyes.BodyForward(), currentTarget.CenterPoint() - eyes.position) > 0.8f)
			{
				targetAimedDuration += delta;
			}
		}
		else
		{
			targetAimedDuration = 0f;
		}
		if (targetAimedDuration > 0.2f)
		{
			AttackEntity attackEntity = GetAttackEntity();
			if ((bool)attackEntity && DistanceToTarget() < attackEntity.effectiveRange * (attackEntity.aiOnlyInRange ? 1f : 2f))
			{
				ShotTest();
			}
		}
		else
		{
			CancelBurst();
		}
	}

	public void SetNavMeshEnabled(bool on)
	{
		if (NavAgent.enabled == on)
		{
			return;
		}
		if (AiManager.nav_disable)
		{
			NavAgent.enabled = false;
			navmeshEnabled = false;
			return;
		}
		NavAgent.agentTypeID = NavAgent.agentTypeID;
		if (on)
		{
			NavMeshHit hit;
			if (NavMesh.SamplePosition(base.transform.position + Vector3.up * 1f, out hit, 5f, -1))
			{
				NavAgent.Warp(hit.position);
				NavAgent.enabled = true;
				base.transform.position = hit.position;
			}
			else
			{
				Debug.Log("Failed to sample navmesh");
			}
		}
		navmeshEnabled = on;
		if (!on)
		{
			NavAgent.isStopped = true;
			NavAgent.enabled = false;
		}
		else
		{
			NavAgent.enabled = true;
			NavAgent.isStopped = false;
			SetDestination(base.transform.position);
		}
	}

	public void EnableNavAgent()
	{
		if (!base.isMounted)
		{
			SetNavMeshEnabled(true);
		}
	}

	public void LogAttacker(BaseEntity attacker)
	{
	}

	public override void Hurt(HitInfo info)
	{
		if (base.isMounted)
		{
			info.damageTypes.ScaleAll(0.1f);
		}
		base.Hurt(info);
		BaseEntity initiator = info.Initiator;
		if (initiator != null && !initiator.EqualNetID(this))
		{
			myMemory.Update(initiator);
		}
	}

	public void Stop()
	{
		if (IsNavRunning())
		{
			NavAgent.SetDestination(base.transform.position);
		}
	}

	public override void SetDestination(Vector3 newDestination)
	{
		if (IsNavRunning())
		{
			base.SetDestination(newDestination);
			NavAgent.SetDestination(newDestination);
		}
	}

	public override float DesiredMoveSpeed()
	{
		return SpeedFromEnum(desiredSpeed);
	}

	public Vector3 AimOffset(BaseCombatEntity aimat)
	{
		BasePlayer basePlayer = aimat as BasePlayer;
		if (basePlayer != null)
		{
			if (basePlayer.IsSleeping())
			{
				return basePlayer.transform.position + Vector3.up * 0.1f;
			}
			return basePlayer.eyes.position - Vector3.up * 0.05f;
		}
		return aimat.CenterPoint();
	}

	public AIMovePoint GetBestRoamPosition(Vector3 start)
	{
		AIInformationZone informationZone = GetInformationZone();
		if (informationZone == null)
		{
			return null;
		}
		float num = -1f;
		AIMovePoint result = null;
		foreach (AIMovePoint movePoint in informationZone.movePoints)
		{
			if (movePoint.transform.parent.gameObject.activeSelf)
			{
				float num2 = 0f;
				float value = Vector3.Dot(eyes.BodyForward(), Vector3Ex.Direction2D(movePoint.transform.position, eyes.position));
				num2 += Mathf.InverseLerp(-1f, 1f, value) * 100f;
				float num3 = Vector3.Distance(base.transform.position, movePoint.transform.position);
				if (!movePoint.IsUsedForRoaming())
				{
					num2 += 1000f;
				}
				float num4 = Mathf.Abs(base.transform.position.y - movePoint.transform.position.y);
				num2 += (1f - Mathf.InverseLerp(1f, 10f, num4)) * 100f;
				if (!(movePoint.transform.position.y < WaterSystem.OceanLevel) && (!(base.transform.position.y >= WaterSystem.OceanLevel) || !(num4 > 5f)))
				{
					if (num3 > 5f)
					{
						num2 += (1f - Mathf.InverseLerp(5f, 20f, num3)) * 50f;
					}
					if (num2 > num)
					{
						result = movePoint;
						num = num2;
					}
				}
			}
		}
		return result;
	}

	public float GetAimSwayScalar()
	{
		return 1f - Mathf.InverseLerp(1f, 3f, UnityEngine.Time.time - lastGunShotTime);
	}

	public override void SetAimDirection(Vector3 newAim)
	{
		if (!(newAim == Vector3.zero))
		{
			AttackEntity attackEntity = GetAttackEntity();
			if ((bool)attackEntity)
			{
				newAim = attackEntity.ModifyAIAim(newAim, GetAimSwayScalar());
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
			eyes.rotation = (base.isMounted ? Quaternion.Slerp(eyes.rotation, Quaternion.Euler(newAim), UnityEngine.Time.smoothDeltaTime * 70f) : Quaternion.Lerp(eyes.rotation, Quaternion.LookRotation(newAim, base.transform.up), UnityEngine.Time.deltaTime * 25f));
			viewAngles = eyes.rotation.eulerAngles;
			ServerRotation = eyes.rotation;
		}
	}

	public void SetStationaryAimPoint(Vector3 aimAt)
	{
		aimOverridePosition = aimAt;
	}

	public void ClearStationaryAimPoint()
	{
		aimOverridePosition = Vector3.zero;
	}

	public override Vector3 GetAimDirection()
	{
		bool num = currentTarget != null;
		bool flag = num && currentTargetLOS;
		bool flag2 = Vector3Ex.Distance2D(finalDestination, GetPosition()) > 0.5f;
		Vector3 desiredVelocity = NavAgent.desiredVelocity;
		desiredVelocity.y = 0f;
		desiredVelocity.Normalize();
		if (!num)
		{
			if (flag2)
			{
				return desiredVelocity;
			}
			return Vector3.zero;
		}
		if (flag && desiredSpeed != SpeedType.Sprint)
		{
			return (AimOffset(currentTarget) - eyes.position).normalized;
		}
		if (flag2)
		{
			return desiredVelocity;
		}
		if (aimOverridePosition != Vector3.zero)
		{
			return Vector3Ex.Direction2D(aimOverridePosition, base.transform.position);
		}
		return Vector3Ex.Direction2D(base.transform.position + eyes.BodyForward() * 1000f, base.transform.position);
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

	public bool IsVisibleToUs(BasePlayer player)
	{
		if (base.isMounted)
		{
			return IsVisibleMounted(player);
		}
		if (IsDucked())
		{
			return IsVisibleCrouched(player);
		}
		return IsVisibleStanding(player);
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

	public override bool ShouldDropActiveItem()
	{
		return false;
	}

	public override BaseCorpse CreateCorpse()
	{
		using (TimeWarning.New("Create corpse"))
		{
			NPCPlayerCorpse nPCPlayerCorpse = DropCorpse("assets/prefabs/npc/scientist/scientist_corpse.prefab") as NPCPlayerCorpse;
			if ((bool)nPCPlayerCorpse)
			{
				nPCPlayerCorpse.transform.position = nPCPlayerCorpse.transform.position + Vector3.down * NavAgent.baseOffset;
				nPCPlayerCorpse.SetLootableIn(2f);
				nPCPlayerCorpse.SetFlag(Flags.Reserved5, HasPlayerFlag(PlayerFlags.DisplaySash));
				nPCPlayerCorpse.SetFlag(Flags.Reserved2, true);
				nPCPlayerCorpse.TakeFrom(inventory.containerMain, inventory.containerWear, inventory.containerBelt);
				nPCPlayerCorpse.playerName = base.displayName;
				nPCPlayerCorpse.playerSteamID = userID;
				nPCPlayerCorpse.Spawn();
				nPCPlayerCorpse.TakeChildren(this);
				ItemContainer[] containers = nPCPlayerCorpse.containers;
				for (int i = 0; i < containers.Length; i++)
				{
					containers[i].Clear();
				}
				if (LootSpawnSlots.Length != 0)
				{
					object obj = Interface.CallHook("OnCorpsePopulate", this, nPCPlayerCorpse);
					if (obj is BaseCorpse)
					{
						return (BaseCorpse)obj;
					}
					LootContainer.LootSpawnSlot[] lootSpawnSlots = LootSpawnSlots;
					for (int i = 0; i < lootSpawnSlots.Length; i++)
					{
						LootContainer.LootSpawnSlot lootSpawnSlot = lootSpawnSlots[i];
						for (int j = 0; j < lootSpawnSlot.numberToSpawn; j++)
						{
							if (UnityEngine.Random.Range(0f, 1f) <= lootSpawnSlot.probability)
							{
								lootSpawnSlot.definition.SpawnIntoContainer(nPCPlayerCorpse.containers[0]);
							}
						}
					}
				}
			}
			return nPCPlayerCorpse;
		}
	}

	public override void AttackerInfo(PlayerLifeStory.DeathInfo info)
	{
		base.AttackerInfo(info);
		info.inflictorName = inventory.containerBelt.GetSlot(0).info.shortname;
		info.attackerName = "scientist";
	}
}
