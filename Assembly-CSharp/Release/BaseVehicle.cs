#define UNITY_ASSERTIONS
using ConVar;
using Facepunch;
using Network;
using Oxide.Core;
using Rust;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class BaseVehicle : BaseMountable
{
	[Serializable]
	public class MountPointInfo
	{
		public bool isDriver;

		public Vector3 pos;

		public Vector3 rot;

		public string bone = "";

		public GameObjectRef prefab;

		public BaseMountable mountable;
	}

	[Tooltip("Allow players to mount other mountables/ladders from this vehicle")]
	public bool mountChaining = true;

	public bool shouldShowHudHealth;

	[Header("Rigidbody (Optional)")]
	public Rigidbody rigidBody;

	[Header("Mount Points")]
	public List<MountPointInfo> mountPoints;

	[Header("Damage")]
	public DamageRenderer damageRenderer;

	public const Flags Flag_OnlyOwnerEntry = Flags.Locked;

	public const Flags Flag_Headlights = Flags.Reserved5;

	public const Flags Flag_Stationary = Flags.Reserved7;

	private const float MIN_TIME_BETWEEN_PUSHES = 1f;

	public TimeSince timeSinceLastPush;

	public bool seatClipCheck;

	private Queue<BasePlayer> recentDrivers = new Queue<BasePlayer>();

	private Action clearRecentDriverAction;

	public float safeAreaRadius;

	public Vector3 safeAreaOrigin;

	public float spawnTime = -1f;

	protected override bool PositionTickFixedTime => true;

	protected bool RecentlyPushed => (float)timeSinceLastPush < 1f;

	public override bool OnRpcMessage(BasePlayer player, uint rpc, Message msg)
	{
		using (TimeWarning.New("BaseVehicle.OnRpcMessage"))
		{
			if (rpc == 2115395408 && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (ConVar.Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - RPC_WantsPush ");
				}
				using (TimeWarning.New("RPC_WantsPush"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.MaxDistance.Test(2115395408u, "RPC_WantsPush", this, player, 5f))
						{
							return true;
						}
					}
					try
					{
						using (TimeWarning.New("Call"))
						{
							RPCMessage rPCMessage = default(RPCMessage);
							rPCMessage.connection = msg.connection;
							rPCMessage.player = player;
							rPCMessage.read = msg.read;
							RPCMessage msg2 = rPCMessage;
							RPC_WantsPush(msg2);
						}
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
						player.Kick("RPC Error in RPC_WantsPush");
					}
				}
				return true;
			}
		}
		return base.OnRpcMessage(player, rpc, msg);
	}

	public bool IsStationary()
	{
		return HasFlag(Flags.Reserved7);
	}

	public bool IsMoving()
	{
		return !HasFlag(Flags.Reserved7);
	}

	protected virtual bool CanPushNow(BasePlayer pusher)
	{
		return !IsOn();
	}

	public override void OnAttacked(HitInfo info)
	{
		if (IsSafe() && !info.damageTypes.Has(DamageType.Decay))
		{
			info.damageTypes.ScaleAll(0f);
		}
		base.OnAttacked(info);
	}

	public override void PostServerLoad()
	{
		base.PostServerLoad();
		ClearOwnerEntry();
	}

	public override float GetNetworkTime()
	{
		return UnityEngine.Time.fixedTime;
	}

	public bool HasAnyPassengers()
	{
		foreach (MountPointInfo mountPoint in mountPoints)
		{
			if (mountPoint.mountable != null && (bool)mountPoint.mountable.GetMounted())
			{
				return true;
			}
		}
		return false;
	}

	protected override void VehicleFixedUpdate()
	{
		base.VehicleFixedUpdate();
		if (seatClipCheck && HasAnyPassengers())
		{
			Vector3 center = base.transform.TransformPoint(bounds.center);
			int layerMask = 1210122497;
			if (UnityEngine.Physics.OverlapBox(center, bounds.extents, base.transform.rotation, layerMask).Length != 0)
			{
				CheckSeatsForClipping();
			}
		}
		if ((bool)rigidBody)
		{
			SetFlag(Flags.Reserved7, rigidBody.IsSleeping() && !HasAnyPassengers());
		}
		if (OnlyOwnerAccessible() && safeAreaRadius != -1f && Vector3.Distance(base.transform.position, safeAreaOrigin) > safeAreaRadius)
		{
			ClearOwnerEntry();
		}
	}

	public virtual int StartingFuelUnits()
	{
		return -1;
	}

	public bool InSafeZone()
	{
		float num = 0f;
		if (triggers != null)
		{
			for (int i = 0; i < triggers.Count; i++)
			{
				TriggerSafeZone triggerSafeZone = triggers[i] as TriggerSafeZone;
				if (!(triggerSafeZone == null))
				{
					float safeLevel = triggerSafeZone.GetSafeLevel(base.transform.position);
					if (safeLevel > num)
					{
						num = safeLevel;
					}
				}
			}
		}
		return num > 0f;
	}

	public virtual bool IsSeatVisible(BaseMountable mountable, Vector3 eyePos, int mask = 1218511105)
	{
		if (mountable == null)
		{
			return false;
		}
		Vector3 p = mountable.transform.position + base.transform.up * 0.15f;
		return GamePhysics.LineOfSight(eyePos, p, mask);
	}

	public virtual bool IsSeatClipping(BaseMountable mountable, int mask = 1218511105)
	{
		if (mountable == null)
		{
			return false;
		}
		Vector3 position = mountable.transform.position;
		Vector3 position2 = mountable.eyeOverride.transform.position;
		Vector3 end = position + base.transform.up * 0.15f;
		return GamePhysics.CheckCapsule(position2, end, 0.1f, mask);
	}

	public virtual void CheckSeatsForClipping()
	{
		foreach (MountPointInfo mountPoint in mountPoints)
		{
			BaseMountable mountable = mountPoint.mountable;
			if (!(mountable == null) && mountable.IsMounted() && IsSeatClipping(mountable, 1210122497))
			{
				SeatClippedWorld(mountable);
			}
		}
	}

	public virtual void SeatClippedWorld(BaseMountable mountable)
	{
		mountable.DismountPlayer(mountable.GetMounted());
	}

	public override void MounteeTookDamage(BasePlayer mountee, HitInfo info)
	{
	}

	public override void DismountAllPlayers()
	{
		foreach (MountPointInfo mountPoint in mountPoints)
		{
			if (mountPoint.mountable != null)
			{
				mountPoint.mountable.DismountAllPlayers();
			}
		}
	}

	public override void ServerInit()
	{
		base.ServerInit();
		clearRecentDriverAction = ClearRecentDriver;
	}

	public virtual void SpawnSubEntities()
	{
		for (int i = 0; i < mountPoints.Count; i++)
		{
			SpawnMountPoint(mountPoints[i], model);
		}
	}

	public override void Spawn()
	{
		base.Spawn();
		SpawnSubEntities();
	}

	public bool AnyMounted()
	{
		return NumMounted() > 0;
	}

	public int NumMounted()
	{
		if (!HasMountPoints())
		{
			if (!IsMounted())
			{
				return 0;
			}
			return 1;
		}
		int num = 0;
		for (int i = 0; i < mountPoints.Count; i++)
		{
			MountPointInfo mountPointInfo = mountPoints[i];
			if (mountPointInfo.mountable != null && mountPointInfo.mountable.GetMounted() != null)
			{
				num++;
			}
		}
		return num;
	}

	public bool HasDriver()
	{
		if (HasMountPoints())
		{
			foreach (MountPointInfo mountPoint in mountPoints)
			{
				if (mountPoint != null && mountPoint.mountable != null && mountPoint.isDriver && mountPoint.mountable.IsMounted())
				{
					return true;
				}
			}
			return false;
		}
		return base.IsMounted();
	}

	public bool IsDriver(BasePlayer player)
	{
		if (HasMountPoints())
		{
			foreach (MountPointInfo mountPoint in mountPoints)
			{
				if (mountPoint != null && mountPoint.mountable != null && mountPoint.isDriver)
				{
					BasePlayer mounted = mountPoint.mountable.GetMounted();
					if (mounted != null && mounted == player)
					{
						return true;
					}
				}
			}
		}
		else if (_mounted != null)
		{
			return _mounted == player;
		}
		return false;
	}

	public BasePlayer GetDriver()
	{
		if (HasMountPoints())
		{
			foreach (MountPointInfo mountPoint in mountPoints)
			{
				if (mountPoint != null && mountPoint.mountable != null && mountPoint.isDriver)
				{
					BasePlayer mounted = mountPoint.mountable.GetMounted();
					if (mounted != null)
					{
						return mounted;
					}
				}
			}
		}
		else if (_mounted != null)
		{
			return _mounted;
		}
		return null;
	}

	public void GetDrivers(List<BasePlayer> drivers)
	{
		if (HasMountPoints())
		{
			foreach (MountPointInfo mountPoint in mountPoints)
			{
				if (mountPoint != null && mountPoint.mountable != null && mountPoint.isDriver)
				{
					BasePlayer mounted = mountPoint.mountable.GetMounted();
					if (mounted != null)
					{
						drivers.Add(mounted);
					}
				}
			}
		}
		else if (_mounted != null)
		{
			drivers.Add(_mounted);
		}
	}

	public BasePlayer GetPlayerDamageInitiator()
	{
		if (HasDriver())
		{
			return GetDriver();
		}
		if (recentDrivers.Count <= 0)
		{
			return null;
		}
		return recentDrivers.Peek();
	}

	public int GetPlayerSeat(BasePlayer player)
	{
		if (!HasMountPoints() && GetMounted() == player)
		{
			return 0;
		}
		for (int i = 0; i < mountPoints.Count; i++)
		{
			MountPointInfo mountPointInfo = mountPoints[i];
			if (mountPointInfo.mountable != null && mountPointInfo.mountable.GetMounted() == player)
			{
				return i;
			}
		}
		return -1;
	}

	public MountPointInfo GetPlayerSeatInfo(BasePlayer player)
	{
		if (!HasMountPoints())
		{
			return null;
		}
		for (int i = 0; i < mountPoints.Count; i++)
		{
			MountPointInfo mountPointInfo = mountPoints[i];
			if (mountPointInfo.mountable != null && mountPointInfo.mountable.GetMounted() == player)
			{
				return mountPointInfo;
			}
		}
		return null;
	}

	public void SwapSeats(BasePlayer player, int targetSeat = 0)
	{
		if (!HasMountPoints())
		{
			return;
		}
		int playerSeat = GetPlayerSeat(player);
		if (playerSeat == -1)
		{
			return;
		}
		BaseMountable mountable = mountPoints[playerSeat].mountable;
		int num = playerSeat;
		BaseMountable baseMountable = null;
		if (baseMountable == null)
		{
			for (int i = 0; i < mountPoints.Count; i++)
			{
				num++;
				if (num >= mountPoints.Count)
				{
					num = 0;
				}
				MountPointInfo mountPointInfo = mountPoints[num];
				if (mountPointInfo.mountable != null && !mountPointInfo.mountable.IsMounted() && mountPointInfo.mountable.CanSwapToThis(player) && !IsSeatClipping(mountPointInfo.mountable) && IsSeatVisible(mountPointInfo.mountable, player.eyes.position))
				{
					baseMountable = mountPointInfo.mountable;
					break;
				}
			}
		}
		if (baseMountable != null && baseMountable != mountable)
		{
			mountable.DismountPlayer(player, true);
			baseMountable.MountPlayer(player);
			player.MarkSwapSeat();
		}
	}

	public bool HasMountPoints()
	{
		return mountPoints.Count > 0;
	}

	public bool HasDriverMountPoints()
	{
		foreach (MountPointInfo mountPoint in mountPoints)
		{
			if (mountPoint.isDriver)
			{
				return true;
			}
		}
		return false;
	}

	public bool OnlyOwnerAccessible()
	{
		return HasFlag(Flags.Locked);
	}

	public bool IsDespawnEligable()
	{
		if (spawnTime != -1f)
		{
			return spawnTime + 120f < UnityEngine.Time.realtimeSinceStartup;
		}
		return true;
	}

	public void SetupOwner(BasePlayer owner, Vector3 newSafeAreaOrigin, float newSafeAreaRadius)
	{
		if (owner != null)
		{
			creatorEntity = owner;
			SetFlag(Flags.Locked, true);
			safeAreaRadius = newSafeAreaRadius;
			safeAreaOrigin = newSafeAreaOrigin;
			spawnTime = UnityEngine.Time.realtimeSinceStartup;
		}
	}

	public void ClearOwnerEntry()
	{
		creatorEntity = null;
		SetFlag(Flags.Locked, false);
		safeAreaRadius = -1f;
		safeAreaOrigin = Vector3.zero;
	}

	public virtual EntityFuelSystem GetFuelSystem()
	{
		return null;
	}

	public bool IsSafe()
	{
		if (OnlyOwnerAccessible())
		{
			return Vector3.Distance(safeAreaOrigin, base.transform.position) <= safeAreaRadius;
		}
		return false;
	}

	public override void ScaleDamageForPlayer(BasePlayer player, HitInfo info)
	{
		if (IsSafe())
		{
			info.damageTypes.ScaleAll(0f);
		}
		base.ScaleDamageForPlayer(player, info);
	}

	public BaseMountable GetIdealMountPoint(Vector3 eyePos, Vector3 pos)
	{
		if (!HasMountPoints())
		{
			return this;
		}
		BaseMountable result = null;
		float num = float.PositiveInfinity;
		foreach (MountPointInfo mountPoint in mountPoints)
		{
			if (!mountPoint.mountable.IsMounted())
			{
				float num2 = Vector3.Distance(mountPoint.mountable.mountAnchor.position, pos);
				if (!(num2 > num))
				{
					if (IsSeatClipping(mountPoint.mountable))
					{
						if (UnityEngine.Application.isEditor)
						{
							Debug.Log($"Skipping seat {mountPoint.mountable} - it's clipping");
						}
					}
					else if (!IsSeatVisible(mountPoint.mountable, eyePos))
					{
						if (UnityEngine.Application.isEditor)
						{
							Debug.Log($"Skipping seat {mountPoint.mountable} - it's not visible");
						}
					}
					else if (!OnlyOwnerAccessible() || mountPoint.isDriver)
					{
						result = mountPoint.mountable;
						num = num2;
					}
				}
			}
		}
		return result;
	}

	public override bool IsMounted()
	{
		return HasDriver();
	}

	public virtual bool MountEligable(BasePlayer player)
	{
		if (creatorEntity != null && OnlyOwnerAccessible() && player != creatorEntity)
		{
			return false;
		}
		return true;
	}

	public int GetIndexFromSeat(BaseMountable seat)
	{
		for (int i = 0; i < mountPoints.Count; i++)
		{
			if (mountPoints[i].mountable == seat)
			{
				return i;
			}
		}
		return -1;
	}

	public virtual void PlayerMounted(BasePlayer player, BaseMountable seat)
	{
	}

	public virtual void PlayerDismounted(BasePlayer player, BaseMountable seat)
	{
		recentDrivers.Enqueue(player);
		if (!IsInvoking(clearRecentDriverAction))
		{
			Invoke(clearRecentDriverAction, 3f);
		}
	}

	private void ClearRecentDriver()
	{
		if (recentDrivers.Count > 0)
		{
			recentDrivers.Dequeue();
		}
		if (recentDrivers.Count > 0)
		{
			Invoke(clearRecentDriverAction, 3f);
		}
	}

	public override void AttemptMount(BasePlayer player, bool doMountChecks = true)
	{
		if (_mounted != null || !MountEligable(player))
		{
			return;
		}
		BaseMountable idealMountPointFor = GetIdealMountPointFor(player);
		if (!(idealMountPointFor == null))
		{
			if (idealMountPointFor == this)
			{
				base.AttemptMount(player, doMountChecks);
			}
			else
			{
				idealMountPointFor.AttemptMount(player, doMountChecks);
			}
			if (player.GetMountedVehicle() == this)
			{
				PlayerMounted(player, idealMountPointFor);
			}
		}
	}

	protected BaseMountable GetIdealMountPointFor(BasePlayer player)
	{
		return GetIdealMountPoint(player.eyes.position, player.eyes.position + player.eyes.HeadForward() * 1f);
	}

	public override bool GetDismountPosition(BasePlayer player, out Vector3 res)
	{
		BaseVehicle baseVehicle = VehicleParent();
		if (baseVehicle != null)
		{
			return baseVehicle.GetDismountPosition(player, out res);
		}
		List<Vector3> obj = Facepunch.Pool.GetList<Vector3>();
		Vector3 visualCheckOrigin = player.TriggerPoint();
		Transform[] dismountPositions = base.dismountPositions;
		foreach (Transform transform in dismountPositions)
		{
			if (ValidDismountPosition(transform.transform.position, visualCheckOrigin))
			{
				obj.Add(transform.transform.position);
			}
		}
		if (obj.Count == 0)
		{
			Debug.LogWarning("Failed to find dismount position for player :" + player.displayName + " / " + player.userID + " on obj : " + base.gameObject.name);
			Facepunch.Pool.FreeList(ref obj);
			res = player.transform.position;
			return false;
		}
		Vector3 pos = player.transform.position;
		obj.Sort((Vector3 a, Vector3 b) => Vector3.Distance(a, pos).CompareTo(Vector3.Distance(b, pos)));
		res = obj[0];
		Facepunch.Pool.FreeList(ref obj);
		return true;
	}

	public BaseEntity AddMountPoint(MountPointInfo newMountPoint, Model model = null)
	{
		if (mountPoints.Contains(newMountPoint))
		{
			return newMountPoint.mountable;
		}
		mountPoints.Add(newMountPoint);
		return SpawnMountPoint(newMountPoint, model);
	}

	public void RemoveMountPoint(MountPointInfo mountPoint)
	{
		if (mountPoints.Remove(mountPoint) && mountPoint.mountable != null)
		{
			mountPoint.mountable.Kill();
		}
	}

	private BaseMountable SpawnMountPoint(MountPointInfo mountToSpawn, Model model)
	{
		Vector3 vector = Quaternion.Euler(mountToSpawn.rot) * Vector3.forward;
		Vector3 pos = mountToSpawn.pos;
		Vector3 up = Vector3.up;
		if (mountToSpawn.bone != "")
		{
			pos = model.FindBone(mountToSpawn.bone).transform.position + base.transform.TransformDirection(mountToSpawn.pos);
			vector = base.transform.TransformDirection(vector);
			up = base.transform.up;
		}
		BaseEntity baseEntity = GameManager.server.CreateEntity(mountToSpawn.prefab.resourcePath, pos, Quaternion.LookRotation(vector, up));
		BaseMountable baseMountable = baseEntity as BaseMountable;
		if (baseMountable != null)
		{
			if (mountToSpawn.bone != "")
			{
				baseMountable.SetParent(this, mountToSpawn.bone, true, true);
			}
			else
			{
				baseMountable.SetParent(this);
			}
			baseMountable.Spawn();
			mountToSpawn.mountable = baseMountable;
		}
		else
		{
			Debug.LogError("MountPointInfo prefab is not a BaseMountable. Cannot spawn mount point.");
			if (baseEntity != null)
			{
				baseEntity.Kill();
			}
		}
		return baseMountable;
	}

	[RPC_Server]
	[RPC_Server.MaxDistance(5f)]
	public void RPC_WantsPush(RPCMessage msg)
	{
		BasePlayer player = msg.player;
		if (!player.isMounted && !RecentlyPushed && CanPushNow(player) && (!OnlyOwnerAccessible() || !(player != creatorEntity)) && Interface.CallHook("OnVehiclePush", this, msg.player) == null)
		{
			DoPushAction(player);
			timeSinceLastPush = 0f;
		}
	}

	protected virtual void DoPushAction(BasePlayer player)
	{
	}

	public override bool SupportsChildDeployables()
	{
		return false;
	}

	public bool IsFlipped()
	{
		return Vector3.Dot(Vector3.up, base.transform.up) <= 0f;
	}
}
