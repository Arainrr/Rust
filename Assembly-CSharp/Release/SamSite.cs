using Facepunch;
using Oxide.Core;
using ProtoBuf;
using System.Collections.Generic;
using UnityEngine;

public class SamSite : ContainerIOEntity
{
	public Animator pitchAnimator;

	public GameObject yaw;

	public GameObject pitch;

	public GameObject gear;

	public Transform eyePoint;

	public float gearEpislonDegrees = 20f;

	public float turnSpeed = 1f;

	public float clientLerpSpeed = 1f;

	public Vector3 currentAimDir = Vector3.forward;

	public Vector3 targetAimDir = Vector3.forward;

	public BaseCombatEntity currentTarget;

	public float scanRadius = 350f;

	public GameObjectRef projectileTest;

	public GameObjectRef muzzleFlashTest;

	public bool staticRespawn;

	public ItemDefinition ammoType;

	public Transform[] tubes;

	[ServerVar(Help = "targetmode, 1 = all air vehicles, 0 = only hot air ballons")]
	public static bool alltarget = false;

	[ServerVar(Help = "how long until static sam sites auto repair")]
	public static float staticrepairseconds = 1200f;

	public SoundDefinition yawMovementLoopDef;

	public float yawGainLerp = 8f;

	public float yawGainMovementSpeedMult = 0.1f;

	public SoundDefinition pitchMovementLoopDef;

	public float pitchGainLerp = 10f;

	public float pitchGainMovementSpeedMult = 0.5f;

	public Item ammoItem;

	public float lockOnTime;

	public float lastTargetVisibleTime;

	public int currentTubeIndex;

	private int firedCount;

	public float nextBurstTime;

	public override bool IsPowered()
	{
		if (!staticRespawn)
		{
			return HasFlag(Flags.Reserved8);
		}
		return true;
	}

	public override int ConsumptionAmount()
	{
		return 25;
	}

	public override void Load(LoadInfo info)
	{
		base.Load(info);
	}

	public override void PostServerLoad()
	{
		base.PostServerLoad();
		if (staticRespawn && HasFlag(Flags.Reserved1))
		{
			Invoke(SelfHeal, staticrepairseconds);
		}
	}

	public void SelfHeal()
	{
		lifestate = LifeState.Alive;
		base.health = startHealth;
		SetFlag(Flags.Reserved1, false);
	}

	public override void Die(HitInfo info = null)
	{
		if (staticRespawn)
		{
			currentTarget = null;
			Quaternion rotation = Quaternion.Euler(0f, Quaternion.LookRotation(currentAimDir, Vector3.up).eulerAngles.y, 0f);
			currentAimDir = rotation * Vector3.forward;
			Invoke(SelfHeal, staticrepairseconds);
			lifestate = LifeState.Dead;
			base.health = 0f;
			SetFlag(Flags.Reserved1, true);
		}
		else
		{
			base.Die(info);
		}
	}

	public Vector3 EntityCenterPoint(BaseEntity ent)
	{
		return ent.transform.TransformPoint(ent.bounds.center);
	}

	public void FixedUpdate()
	{
		Vector3 rhs = currentAimDir;
		if (currentTarget != null && IsPowered())
		{
			float speed = projectileTest.Get().GetComponent<ServerProjectile>().speed;
			Vector3 a = EntityCenterPoint(currentTarget);
			float num = Vector3.Distance(a, eyePoint.transform.position);
			float d = num / speed;
			Vector3 a2 = a + currentTarget.GetWorldVelocity() * d;
			d = Vector3.Distance(a2, eyePoint.transform.position) / speed;
			a2 = a + currentTarget.GetWorldVelocity() * d;
			if (currentTarget.GetWorldVelocity().magnitude > 0.1f)
			{
				float d2 = Mathf.Sin(Time.time * 3f) * (1f + d * 0.5f);
				a2 += currentTarget.GetWorldVelocity().normalized * d2;
			}
			currentAimDir = (a2 - eyePoint.transform.position).normalized;
			if (num > scanRadius)
			{
				currentTarget = null;
			}
		}
		Vector3 eulerAngles = Quaternion.LookRotation(currentAimDir, base.transform.up).eulerAngles;
		eulerAngles = BaseMountable.ConvertVector(eulerAngles);
		float t = Mathf.InverseLerp(0f, 90f, 0f - eulerAngles.x);
		float z = Mathf.Lerp(15f, -75f, t);
		Quaternion localRotation = Quaternion.Euler(0f, eulerAngles.y, 0f);
		yaw.transform.localRotation = localRotation;
		Quaternion localRotation2 = Quaternion.Euler(pitch.transform.localRotation.eulerAngles.x, pitch.transform.localRotation.eulerAngles.y, z);
		pitch.transform.localRotation = localRotation2;
		if (currentAimDir != rhs)
		{
			SendNetworkUpdate();
		}
	}

	public Vector3 GetAimDir()
	{
		return currentAimDir;
	}

	public override void Save(SaveInfo info)
	{
		base.Save(info);
		info.msg.samSite = Pool.Get<SAMSite>();
		info.msg.samSite.aimDir = GetAimDir();
	}

	public override void ServerInit()
	{
		base.ServerInit();
		InvokeRandomized(TargetScan, 1f, 3f, 1f);
		currentAimDir = base.transform.forward;
	}

	public bool HasValidTarget()
	{
		return currentTarget != null;
	}

	public override bool CanPickup(BasePlayer player)
	{
		if (base.isServer && pickup.requireEmptyInv && base.inventory != null && base.inventory.itemList.Count > 0)
		{
			return false;
		}
		return !HasAmmo();
	}

	public void TargetScan()
	{
		if (!IsPowered())
		{
			lastTargetVisibleTime = 0f;
			return;
		}
		if (Time.time > lastTargetVisibleTime + 3f)
		{
			currentTarget = null;
		}
		if (!HasValidTarget() && !IsDead())
		{
			List<BaseCombatEntity> obj = Pool.GetList<BaseCombatEntity>();
			Vis.Entities(eyePoint.transform.position, scanRadius, obj, 32768, QueryTriggerInteraction.Ignore);
			BaseCombatEntity baseCombatEntity = null;
			foreach (BaseCombatEntity item in obj)
			{
				if (!item.isClient && !(EntityCenterPoint(item).y < eyePoint.transform.position.y) && item.IsVisible(eyePoint.transform.position, scanRadius * 2f) && Interface.CallHook("OnSamSiteTarget", this, item) == null)
				{
					BaseVehicle component = item.GetComponent<BaseVehicle>();
					if ((staticRespawn || !(component != null) || !component.InSafeZone()) && (alltarget || (bool)item.GetComponent<HotAirBalloon>() || (bool)item.GetComponent<MiniCopter>()))
					{
						baseCombatEntity = item;
					}
				}
			}
			if (baseCombatEntity != null && currentTarget != baseCombatEntity)
			{
				lockOnTime = Time.time + 0.5f;
			}
			currentTarget = baseCombatEntity;
			if (currentTarget != null)
			{
				lastTargetVisibleTime = Time.time;
			}
			Pool.FreeList(ref obj);
			if (currentTarget == null)
			{
				CancelInvoke(WeaponTick);
			}
			else
			{
				InvokeRandomized(WeaponTick, 0f, 0.5f, 0.2f);
			}
		}
	}

	public virtual bool HasAmmo()
	{
		if (!staticRespawn)
		{
			if (ammoItem != null && ammoItem.amount > 0)
			{
				return ammoItem.parent == base.inventory;
			}
			return false;
		}
		return true;
	}

	public void Reload()
	{
		if (staticRespawn)
		{
			return;
		}
		for (int i = 0; i < base.inventory.itemList.Count; i++)
		{
			Item item = base.inventory.itemList[i];
			if (item != null && item.info.itemid == ammoType.itemid && item.amount > 0)
			{
				ammoItem = item;
				return;
			}
		}
		ammoItem = null;
	}

	public void EnsureReloaded()
	{
		if (!HasAmmo())
		{
			Reload();
		}
	}

	public bool IsReloading()
	{
		return IsInvoking(Reload);
	}

	public void WeaponTick()
	{
		if (IsDead() || Time.time < lockOnTime || Time.time < nextBurstTime)
		{
			return;
		}
		if (!IsPowered())
		{
			firedCount = 0;
			return;
		}
		if (firedCount >= 6)
		{
			nextBurstTime = Time.time + 5f;
			firedCount = 0;
			return;
		}
		EnsureReloaded();
		if (HasAmmo() && Interface.CallHook("CanSamSiteShoot", this) == null)
		{
			if (!staticRespawn && ammoItem != null)
			{
				ammoItem.UseItem();
			}
			firedCount++;
			FireProjectile(tubes[currentTubeIndex].position, currentAimDir, currentTarget);
			Effect.server.Run(muzzleFlashTest.resourcePath, this, StringPool.Get("Tube " + (currentTubeIndex + 1).ToString()), Vector3.zero, Vector3.up);
			currentTubeIndex++;
			if (currentTubeIndex >= tubes.Length)
			{
				currentTubeIndex = 0;
			}
		}
	}

	public void FireProjectile(Vector3 origin, Vector3 direction, BaseCombatEntity target)
	{
		BaseEntity baseEntity = GameManager.server.CreateEntity(projectileTest.resourcePath, origin, Quaternion.LookRotation(direction, Vector3.up));
		if (!(baseEntity == null))
		{
			baseEntity.creatorEntity = this;
			ServerProjectile component = baseEntity.GetComponent<ServerProjectile>();
			if ((bool)component)
			{
				component.InitializeVelocity(GetInheritedProjectileVelocity() + direction * component.speed);
			}
			baseEntity.Spawn();
		}
	}
}
