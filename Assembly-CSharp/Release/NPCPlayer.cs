using Oxide.Core;
using System;
using UnityEngine;
using UnityEngine.AI;

public class NPCPlayer : BasePlayer
{
	protected bool _traversingNavMeshLink;

	protected OffMeshLinkData _currentNavMeshLink;

	protected string _currentNavMeshLinkName;

	protected Quaternion _currentNavMeshLinkOrientation;

	protected Vector3 _currentNavMeshLinkEndPos;

	public Vector3 finalDestination;

	[NonSerialized]
	private float randomOffset;

	[NonSerialized]
	public Vector3 spawnPos;

	public PlayerInventoryProperties[] loadouts;

	public LayerMask movementMask = 429990145;

	public NavMeshAgent NavAgent;

	public float damageScale = 1f;

	private bool _isDormant;

	public float lastGunShotTime;

	public float triggerEndTime;

	public float nextTriggerTime;

	private float lastThinkTime;

	public Vector3 lastPos;

	public bool AgencyUpdateRequired
	{
		get;
		set;
	}

	public bool IsOnOffmeshLinkAndReachedNewCoord
	{
		get;
		set;
	}

	public override bool IsNpc => true;

	public virtual bool IsDormant
	{
		get
		{
			return _isDormant;
		}
		set
		{
			_isDormant = value;
			bool isDormant = _isDormant;
		}
	}

	protected override float PositionTickRate => 0.1f;

	public virtual bool IsOnNavMeshLink
	{
		get
		{
			if (IsNavRunning())
			{
				return NavAgent.isOnOffMeshLink;
			}
			return false;
		}
	}

	public virtual bool HasPath
	{
		get
		{
			if (IsNavRunning())
			{
				return NavAgent.hasPath;
			}
			return false;
		}
	}

	private void HandleNavMeshLinkTraversal(float delta, ref Vector3 moveToPosition)
	{
		if (!_traversingNavMeshLink)
		{
			HandleNavMeshLinkTraversalStart(delta);
		}
		HandleNavMeshLinkTraversalTick(delta, ref moveToPosition);
		if (IsNavMeshLinkTraversalComplete(delta, ref moveToPosition))
		{
			CompleteNavMeshLink();
		}
	}

	private bool HandleNavMeshLinkTraversalStart(float delta)
	{
		OffMeshLinkData currentOffMeshLinkData = NavAgent.currentOffMeshLinkData;
		if (!currentOffMeshLinkData.valid || !currentOffMeshLinkData.activated)
		{
			return false;
		}
		Vector3 normalized = (currentOffMeshLinkData.endPos - currentOffMeshLinkData.startPos).normalized;
		normalized.y = 0f;
		Vector3 desiredVelocity = NavAgent.desiredVelocity;
		desiredVelocity.y = 0f;
		if (Vector3.Dot(desiredVelocity, normalized) < 0.1f)
		{
			CompleteNavMeshLink();
			return false;
		}
		_currentNavMeshLink = currentOffMeshLinkData;
		_currentNavMeshLinkName = currentOffMeshLinkData.linkType.ToString();
		if ((ServerPosition - currentOffMeshLinkData.startPos).sqrMagnitude > (ServerPosition - currentOffMeshLinkData.endPos).sqrMagnitude)
		{
			_currentNavMeshLinkEndPos = currentOffMeshLinkData.startPos;
			_currentNavMeshLinkOrientation = Quaternion.LookRotation(currentOffMeshLinkData.startPos + Vector3.up * (currentOffMeshLinkData.endPos.y - currentOffMeshLinkData.startPos.y) - currentOffMeshLinkData.endPos);
		}
		else
		{
			_currentNavMeshLinkEndPos = currentOffMeshLinkData.endPos;
			_currentNavMeshLinkOrientation = Quaternion.LookRotation(currentOffMeshLinkData.endPos + Vector3.up * (currentOffMeshLinkData.startPos.y - currentOffMeshLinkData.endPos.y) - currentOffMeshLinkData.startPos);
		}
		_traversingNavMeshLink = true;
		NavAgent.ActivateCurrentOffMeshLink(false);
		NavAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
		if (!(_currentNavMeshLinkName == "OpenDoorLink") && !(_currentNavMeshLinkName == "JumpRockLink"))
		{
			bool flag = _currentNavMeshLinkName == "JumpFoundationLink";
		}
		return true;
	}

	private void HandleNavMeshLinkTraversalTick(float delta, ref Vector3 moveToPosition)
	{
		if (_currentNavMeshLinkName == "OpenDoorLink")
		{
			moveToPosition = Vector3.MoveTowards(moveToPosition, _currentNavMeshLinkEndPos, NavAgent.speed * delta);
		}
		else if (_currentNavMeshLinkName == "JumpRockLink")
		{
			moveToPosition = Vector3.MoveTowards(moveToPosition, _currentNavMeshLinkEndPos, NavAgent.speed * delta);
		}
		else if (_currentNavMeshLinkName == "JumpFoundationLink")
		{
			moveToPosition = Vector3.MoveTowards(moveToPosition, _currentNavMeshLinkEndPos, NavAgent.speed * delta);
		}
		else
		{
			moveToPosition = Vector3.MoveTowards(moveToPosition, _currentNavMeshLinkEndPos, NavAgent.speed * delta);
		}
	}

	private bool IsNavMeshLinkTraversalComplete(float delta, ref Vector3 moveToPosition)
	{
		if ((moveToPosition - _currentNavMeshLinkEndPos).sqrMagnitude < 0.01f)
		{
			moveToPosition = _currentNavMeshLinkEndPos;
			_traversingNavMeshLink = false;
			_currentNavMeshLink = default(OffMeshLinkData);
			_currentNavMeshLinkName = string.Empty;
			_currentNavMeshLinkOrientation = Quaternion.identity;
			CompleteNavMeshLink();
			return true;
		}
		return false;
	}

	private void CompleteNavMeshLink()
	{
		NavAgent.ActivateCurrentOffMeshLink(true);
		NavAgent.CompleteOffMeshLink();
		NavAgent.isStopped = false;
		NavAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
	}

	public virtual bool IsLoadBalanced()
	{
		return false;
	}

	public override void ServerInit()
	{
		if (!base.isClient)
		{
			spawnPos = GetPosition();
			randomOffset = UnityEngine.Random.Range(0f, 1f);
			base.ServerInit();
			UpdateNetworkGroup();
			if (loadouts != null && loadouts.Length != 0)
			{
				loadouts[UnityEngine.Random.Range(0, loadouts.Length)].GiveToPlayer(this);
			}
			else
			{
				Debug.LogWarningFormat("Loadout for NPC {0} was empty.", base.name);
			}
			if (!IsLoadBalanced())
			{
				InvokeRepeating(ServerThink_Internal, 0f, 0.1f);
				lastThinkTime = Time.time;
			}
			Invoke(EquipTest, 0.25f);
			finalDestination = base.transform.position;
			AgencyUpdateRequired = false;
			IsOnOffmeshLinkAndReachedNewCoord = false;
			if (NavAgent == null)
			{
				NavAgent = GetComponent<NavMeshAgent>();
			}
			if ((bool)NavAgent)
			{
				NavAgent.updateRotation = false;
				NavAgent.updatePosition = false;
			}
		}
	}

	public override void ApplyInheritedVelocity(Vector3 velocity)
	{
		ServerPosition = BaseNpc.GetNewNavPosWithVelocity(this, velocity);
	}

	public void RandomMove()
	{
		float d = 8f;
		Vector2 vector = UnityEngine.Random.insideUnitCircle * d;
		SetDestination(spawnPos + new Vector3(vector.x, 0f, vector.y));
	}

	public virtual void SetDestination(Vector3 newDestination)
	{
		finalDestination = newDestination;
	}

	public AttackEntity GetAttackEntity()
	{
		return GetHeldEntity() as AttackEntity;
	}

	public BaseProjectile GetGun()
	{
		AttackEntity attackEntity = GetHeldEntity() as AttackEntity;
		if (attackEntity == null)
		{
			return null;
		}
		BaseProjectile baseProjectile = attackEntity as BaseProjectile;
		if ((bool)baseProjectile)
		{
			return baseProjectile;
		}
		return null;
	}

	public virtual float AmmoFractionRemaining()
	{
		AttackEntity attackEntity = GetAttackEntity();
		if ((bool)attackEntity)
		{
			return attackEntity.AmmoFraction();
		}
		return 0f;
	}

	public virtual bool IsReloading()
	{
		AttackEntity attackEntity = GetAttackEntity();
		if (!attackEntity)
		{
			return false;
		}
		return attackEntity.ServerIsReloading();
	}

	public virtual void AttemptReload()
	{
		AttackEntity attackEntity = GetAttackEntity();
		if (!(attackEntity == null) && attackEntity.CanReload())
		{
			attackEntity.ServerReload();
		}
	}

	public virtual bool ShotTest()
	{
		AttackEntity attackEntity = GetHeldEntity() as AttackEntity;
		if (attackEntity == null)
		{
			return false;
		}
		BaseProjectile baseProjectile = attackEntity as BaseProjectile;
		if ((bool)baseProjectile)
		{
			if (baseProjectile.primaryMagazine.contents <= 0)
			{
				baseProjectile.ServerReload();
				NPCPlayerApex nPCPlayerApex = this as NPCPlayerApex;
				if ((bool)nPCPlayerApex && nPCPlayerApex.OnReload != null)
				{
					nPCPlayerApex.OnReload();
				}
				return false;
			}
			if (baseProjectile.NextAttackTime > Time.time)
			{
				return false;
			}
		}
		if (!Mathf.Approximately(attackEntity.attackLengthMin, -1f))
		{
			if (IsInvoking(TriggerDown))
			{
				return true;
			}
			if (Time.time < nextTriggerTime)
			{
				return true;
			}
			InvokeRepeating(TriggerDown, 0f, 0.01f);
			triggerEndTime = Time.time + UnityEngine.Random.Range(attackEntity.attackLengthMin, attackEntity.attackLengthMax);
			TriggerDown();
			return true;
		}
		attackEntity.ServerUse(damageScale);
		lastGunShotTime = Time.time;
		return true;
	}

	public virtual float GetAimConeScale()
	{
		return 1f;
	}

	public void CancelBurst(float delay = 0.2f)
	{
		if (triggerEndTime > Time.time + delay)
		{
			triggerEndTime = Time.time + delay;
		}
	}

	public bool MeleeAttack()
	{
		AttackEntity attackEntity = GetHeldEntity() as AttackEntity;
		if (attackEntity == null)
		{
			return false;
		}
		BaseMelee baseMelee = attackEntity as BaseMelee;
		if (baseMelee == null)
		{
			return false;
		}
		baseMelee.ServerUse(damageScale);
		return true;
	}

	public virtual void TriggerDown()
	{
		AttackEntity attackEntity = GetHeldEntity() as AttackEntity;
		if (attackEntity != null)
		{
			attackEntity.ServerUse(damageScale);
		}
		lastGunShotTime = Time.time;
		if (Time.time > triggerEndTime)
		{
			CancelInvoke(TriggerDown);
			nextTriggerTime = Time.time + ((attackEntity != null) ? attackEntity.attackSpacing : 1f);
		}
	}

	public virtual void EquipWeapon()
	{
		Item slot = inventory.containerBelt.GetSlot(0);
		if (slot == null || Interface.CallHook("OnNpcEquipWeapon", this, slot) != null)
		{
			return;
		}
		UpdateActiveItem(inventory.containerBelt.GetSlot(0).uid);
		BaseEntity heldEntity = slot.GetHeldEntity();
		if (heldEntity != null)
		{
			AttackEntity component = heldEntity.GetComponent<AttackEntity>();
			if (component != null)
			{
				component.TopUpAmmo();
			}
		}
	}

	public void EquipTest()
	{
		EquipWeapon();
	}

	internal void ServerThink_Internal()
	{
		float delta = Time.time - lastThinkTime;
		ServerThink(delta);
		lastThinkTime = Time.time;
	}

	public virtual void ServerThink(float delta)
	{
		TickAi(delta);
	}

	public virtual void Resume()
	{
	}

	public virtual bool IsNavRunning()
	{
		return false;
	}

	public virtual void TickAi(float delta)
	{
		MovementUpdate(delta);
	}

	public virtual void MovementUpdate(float delta)
	{
		if (base.isClient || !IsAlive() || IsWounded() || (!base.isMounted && !IsNavRunning()))
		{
			return;
		}
		if (IsDormant || !syncPosition)
		{
			if (IsNavRunning())
			{
				NavAgent.destination = ServerPosition;
			}
			return;
		}
		Vector3 moveToPosition = base.transform.position;
		if (IsOnNavMeshLink)
		{
			HandleNavMeshLinkTraversal(delta, ref moveToPosition);
		}
		else if (HasPath)
		{
			moveToPosition = NavAgent.nextPosition;
		}
		if (ValidateNextPosition(ref moveToPosition))
		{
			UpdateSpeed(delta);
			UpdatePositionAndRotation(moveToPosition);
		}
	}

	private bool ValidateNextPosition(ref Vector3 moveToPosition)
	{
		if (!ValidBounds.Test(moveToPosition) && base.transform != null && !base.IsDestroyed)
		{
			Debug.Log("Invalid NavAgent Position: " + this + " " + moveToPosition.ToString() + " (destroying)");
			Kill();
			return false;
		}
		return true;
	}

	private void UpdateSpeed(float delta)
	{
		float b = DesiredMoveSpeed();
		NavAgent.speed = Mathf.Lerp(NavAgent.speed, b, delta * 8f);
	}

	protected virtual void UpdatePositionAndRotation(Vector3 moveToPosition)
	{
		ServerPosition = moveToPosition;
		SetAimDirection(GetAimDirection());
	}

	public Vector3 GetPosition()
	{
		return base.transform.position;
	}

	public virtual float DesiredMoveSpeed()
	{
		float running = Mathf.Sin(Time.time + randomOffset);
		return GetSpeed(running, 0f);
	}

	public override bool EligibleForWounding(HitInfo info)
	{
		return false;
	}

	public virtual Vector3 GetAimDirection()
	{
		if (Vector3Ex.Distance2D(finalDestination, GetPosition()) >= 1f)
		{
			Vector3 normalized = (finalDestination - GetPosition()).normalized;
			return new Vector3(normalized.x, 0f, normalized.z);
		}
		return eyes.BodyForward();
	}

	public virtual void SetAimDirection(Vector3 newAim)
	{
		if (!(newAim == Vector3.zero))
		{
			AttackEntity attackEntity = GetAttackEntity();
			if ((bool)attackEntity)
			{
				newAim = attackEntity.ModifyAIAim(newAim);
			}
			eyes.rotation = Quaternion.LookRotation(newAim, Vector3.up);
			viewAngles = eyes.rotation.eulerAngles;
			ServerRotation = eyes.rotation;
		}
	}
}
