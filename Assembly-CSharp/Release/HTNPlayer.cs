using Apex.Ai.HTN;
using ConVar;
using Rust.Ai;
using Rust.Ai.HTN;
using UnityEngine;

public class HTNPlayer : BasePlayer, IHTNAgent
{
	[Header("Hierarchical Task Network")]
	public HTNDomain _aiDomain;

	[Header("Ai Definition")]
	public BaseNpcDefinition _aiDefinition;

	public string deathStatName = "kill_scientist";

	public string LootPanelName;

	private bool isDormant;

	private float lastInvokedTickTime;

	private int serverMaxProjectileID;

	public override bool IsNpc => true;

	public BaseEntity Body => this;

	public Vector3 BodyPosition => base.transform.position;

	public Vector3 EyePosition
	{
		get
		{
			BaseEntity parentEntity = GetParentEntity();
			if (parentEntity != null)
			{
				return BodyPosition + parentEntity.transform.up * PlayerEyes.EyeOffset.y;
			}
			return eyes.position;
		}
	}

	public Quaternion EyeRotation => eyes.rotation;

	protected override float PositionTickRate => 0.05f;

	public override BaseNpc.AiStatistics.FamilyEnum Family => AiDefinition.Info.ToFamily(AiDefinition.Info.Family);

	public BaseNpcDefinition AiDefinition => _aiDefinition;

	public bool OnlyRotateAroundYAxis
	{
		get;
		set;
	}

	public HTNDomain AiDomain => _aiDomain;

	public bool IsDormant
	{
		get
		{
			return isDormant;
		}
		set
		{
			if (isDormant != value)
			{
				isDormant = value;
				if (isDormant)
				{
					Pause();
				}
				else
				{
					Resume();
				}
			}
		}
	}

	public BaseEntity MainTarget
	{
		get
		{
			if (AiDomain.NpcContext.OrientationType != NpcOrientation.LookAtAnimal)
			{
				return AiDomain.NpcContext.PrimaryEnemyPlayerInLineOfSight.Player;
			}
			return AiDomain.NpcContext.BaseMemory.PrimaryKnownAnimal.Animal;
		}
	}

	public override float StartHealth()
	{
		return AiDefinition?.Vitals.HP ?? 0f;
	}

	public override float StartMaxHealth()
	{
		return AiDefinition?.Vitals.HP ?? 0f;
	}

	public override float MaxHealth()
	{
		return AiDefinition?.Vitals.HP ?? 0f;
	}

	public override float MaxVelocity()
	{
		return AiDefinition?.Movement.RunSpeed ?? 0f;
	}

	public override Quaternion GetNetworkRotation()
	{
		if (base.isServer)
		{
			return eyes.bodyRotation;
		}
		return Quaternion.identity;
	}

	public override string Categorize()
	{
		return "npc";
	}

	public override bool ShouldDropActiveItem()
	{
		return false;
	}

	public override void ServerInit()
	{
		if (base.isClient)
		{
			return;
		}
		base.ServerInit();
		UpdateNetworkGroup();
		if (AiDomain == null)
		{
			Debug.LogError(base.name + " requires an AI domain to be set.");
			DieInstantly();
			return;
		}
		AiDomain.Initialize(this);
		if (!AiManager.ai_htn_use_agency_tick)
		{
			InvokeRepeating(InvokedTick, 0f, 0.1f);
		}
		AiDefinition?.Loadout(this);
	}

	public override void ResetState()
	{
		base.ResetState();
		if (AiDomain != null)
		{
			AiDomain.ResetState();
		}
	}

	internal override void DoServerDestroy()
	{
		base.DoServerDestroy();
		AiDomain.Dispose();
	}

	public override void Hurt(HitInfo info)
	{
		if (!(info.InitiatorPlayer != null) || info.InitiatorPlayer.Family != Family)
		{
			if (AiDomain != null && IsAlive())
			{
				AiDomain.OnPreHurt(info);
			}
			base.Hurt(info);
			if (AiDomain != null && IsAlive())
			{
				AiDomain.OnHurt(info);
			}
		}
	}

	public override bool EligibleForWounding(HitInfo info)
	{
		return false;
	}

	public override void OnKilled(HitInfo info)
	{
		base.OnKilled(info);
		AiDefinition?.StopVoices(this);
		AddKilledStat(info);
	}

	private void AddKilledStat(HitInfo info)
	{
		if (info != null && !(info.InitiatorPlayer == null) && !info.InitiatorPlayer.IsNpc && info.InitiatorPlayer.stats != null && !string.IsNullOrEmpty(deathStatName))
		{
			info.InitiatorPlayer.stats.Add(deathStatName, 1);
		}
	}

	public override BaseCorpse CreateCorpse()
	{
		BaseCorpse baseCorpse = AiDefinition.OnCreateCorpse(this);
		if ((bool)baseCorpse)
		{
			PlayerCorpse playerCorpse;
			if ((object)(playerCorpse = (baseCorpse as PlayerCorpse)) != null)
			{
				playerCorpse.playerName = LootPanelName;
			}
			return baseCorpse;
		}
		return base.CreateCorpse();
	}

	public override void OnSensation(Sensation sensation)
	{
		base.OnSensation(sensation);
		if (AiDomain != null && IsAlive())
		{
			AiDomain.OnSensation(sensation);
		}
	}

	public override Vector3 GetLocalVelocityServer()
	{
		return base.estimatedVelocity - GetParentVelocity();
	}

	public void Resume()
	{
		if (AiDomain != null)
		{
			AiDomain.Resume();
		}
		if (AiDefinition != null)
		{
			AiDefinition.StartVoices(this);
		}
	}

	public void Pause()
	{
		if (AiDomain != null)
		{
			AiDomain.Pause();
		}
		if (AiDefinition != null)
		{
			AiDefinition.StopVoices(this);
		}
	}

	public void Tick()
	{
		InvokedTick();
	}

	private void InvokedTick()
	{
		if (base.transform == null || base.IsDestroyed || IsDead())
		{
			return;
		}
		float time = UnityEngine.Time.time;
		float delta = time - lastInvokedTickTime;
		lastInvokedTickTime = UnityEngine.Time.time;
		if (IsDormant)
		{
			return;
		}
		if (AiDomain != null)
		{
			AiDomain.TickDestinationTracker();
			if (AiDomain.PlannerContext.IsWorldStateDirty || AiDomain.PlannerContext.PlanState == PlanStateType.NoPlan)
			{
				AiDomain.Think();
			}
			AiDomain.Tick(UnityEngine.Time.time);
		}
		TickMovement(delta);
		TickOrientation(delta, time);
	}

	private void TickMovement(float delta)
	{
		if (!AI.move || AiDomain == null)
		{
			return;
		}
		Vector3 moveToPosition = AiDomain.GetNextPosition(delta);
		if (_ValidateNextPosition(ref moveToPosition))
		{
			BaseEntity parentEntity = GetParentEntity();
			if ((bool)parentEntity)
			{
				base.transform.localPosition = parentEntity.transform.InverseTransformPoint(moveToPosition);
			}
			else
			{
				base.transform.localPosition = moveToPosition;
			}
			base.transform.hasChanged = true;
		}
	}

	private bool _ValidateNextPosition(ref Vector3 moveToPosition)
	{
		if (!ValidBounds.Test(moveToPosition) && base.transform != null && !base.IsDestroyed)
		{
			Debug.Log($"Invalid NavAgent Position: {this} {moveToPosition.ToString()} (destroying)");
			Kill();
			return false;
		}
		return true;
	}

	public void ForceOrientationTick()
	{
		TickOrientation(UnityEngine.Time.deltaTime, UnityEngine.Time.time);
	}

	private void TickOrientation(float delta, float time)
	{
		if (AiDomain == null || AiDomain.NpcContext == null)
		{
			return;
		}
		Vector3 forward = base.transform.forward;
		switch (AiDomain.NpcContext.OrientationType)
		{
		default:
			return;
		case NpcOrientation.Heading:
			forward = AiDomain.GetHeadingDirection();
			break;
		case NpcOrientation.PrimaryTargetBody:
			forward = AiDomain.NpcContext.GetDirectionToPrimaryEnemyPlayerTargetBody();
			break;
		case NpcOrientation.PrimaryTargetHead:
			forward = AiDomain.NpcContext.GetDirectionToPrimaryEnemyPlayerTargetHead();
			break;
		case NpcOrientation.LastKnownPrimaryTargetLocation:
			forward = AiDomain.NpcContext.GetDirectionToMemoryOfPrimaryEnemyPlayerTarget();
			break;
		case NpcOrientation.LastAttackedDirection:
			forward = AiDomain.NpcContext.GetDirectionLastAttackedDir();
			break;
		case NpcOrientation.LookAround:
			forward = AiDomain.NpcContext.GetDirectionLookAround();
			break;
		case NpcOrientation.AudibleTargetDirection:
			forward = AiDomain.NpcContext.GetDirectionAudibleTarget();
			break;
		case NpcOrientation.LookAtAnimal:
			forward = AiDomain.NpcContext.GetDirectionToAnimal();
			break;
		case NpcOrientation.Home:
			forward = AiDomain.GetHomeDirection();
			break;
		}
		if (Mathf.Approximately(forward.sqrMagnitude, 0f))
		{
			return;
		}
		BaseEntity parentEntity = GetParentEntity();
		if ((bool)parentEntity)
		{
			Vector3 vector = parentEntity.transform.InverseTransformDirection(forward);
			Vector3 forward2 = new Vector3(forward.x, vector.y, forward.z);
			eyes.rotation = Quaternion.LookRotation(forward2, parentEntity.transform.up);
			if (OnlyRotateAroundYAxis)
			{
				eyes.rotation = Quaternion.Euler(new Vector3(0f, eyes.rotation.eulerAngles.y, 0f));
			}
			ServerRotation = eyes.bodyRotation;
		}
		else
		{
			eyes.bodyRotation = Quaternion.LookRotation(forward, base.transform.up);
			if (OnlyRotateAroundYAxis)
			{
				eyes.bodyRotation = Quaternion.Euler(new Vector3(0f, eyes.bodyRotation.eulerAngles.y, 0f));
			}
			ServerRotation = eyes.bodyRotation;
		}
	}

	public int NewServerProjectileID()
	{
		return ++serverMaxProjectileID;
	}

	Transform IHTNAgent.get_transform()
	{
		return base.transform;
	}
}
