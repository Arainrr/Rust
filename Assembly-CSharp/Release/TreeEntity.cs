using ConVar;
using Network;
using System;
using UnityEngine;

public class TreeEntity : ResourceEntity, IPrefabPreProcess
{
	[Header("Falling")]
	public bool fallOnKilled = true;

	public float fallDuration = 1.5f;

	public GameObjectRef fallStartSound;

	public GameObjectRef fallImpactSound;

	public GameObjectRef fallImpactParticles;

	public SoundDefinition fallLeavesLoopDef;

	[NonSerialized]
	public bool[] usedHeights = new bool[20];

	public bool impactSoundPlayed;

	private float treeDistanceUponFalling;

	public static ListHashSet<TreeEntity> activeTreeList = new ListHashSet<TreeEntity>();

	public GameObjectRef prefab;

	public bool hasBonusGame = true;

	public GameObjectRef bonusHitEffect;

	public GameObjectRef bonusHitSound;

	public Collider serverCollider;

	public Collider clientCollider;

	public TreeMarkerData MarkerData;

	public SoundDefinition smallCrackSoundDef;

	public SoundDefinition medCrackSoundDef;

	private float lastAttackDamage;

	[NonSerialized]
	public BaseEntity xMarker;

	private int currentBonusLevel;

	private float lastDirection = -1f;

	private float lastHitTime;

	private int lastHitMarkerIndex = -1;

	public override bool OnRpcMessage(BasePlayer player, uint rpc, Message msg)
	{
		using (TimeWarning.New("TreeEntity.OnRpcMessage"))
		{
		}
		return base.OnRpcMessage(player, rpc, msg);
	}

	public override void ResetState()
	{
		base.ResetState();
	}

	public override float BoundsPadding()
	{
		return 1f;
	}

	public override void ServerInit()
	{
		base.ServerInit();
		lastDirection = ((UnityEngine.Random.Range(0, 2) != 0) ? 1 : (-1));
		activeTreeList.Add(this);
	}

	internal override void DoServerDestroy()
	{
		base.DoServerDestroy();
		activeTreeList.Remove(this);
		CleanupMarker();
		TreeManager.OnTreeDestroyed(this);
	}

	public bool DidHitMarker(HitInfo info)
	{
		if (xMarker == null)
		{
			return false;
		}
		if (MarkerData != null)
		{
			if (new Bounds(xMarker.transform.position, Vector3.one * 0.2f).Contains(info.HitPositionWorld))
			{
				return true;
			}
		}
		else
		{
			Vector3 lhs = Vector3Ex.Direction2D(base.transform.position, xMarker.transform.position);
			if (MarkerData != null)
			{
				lhs = xMarker.transform.forward;
			}
			Vector3 attackNormal = info.attackNormal;
			float num = Vector3.Dot(lhs, attackNormal);
			float num2 = Vector3.Distance(xMarker.transform.position, info.HitPositionWorld);
			if (num >= 0.3f && num2 <= 0.2f)
			{
				return true;
			}
		}
		return false;
	}

	public void StartBonusGame()
	{
		if (IsInvoking(StopBonusGame))
		{
			CancelInvoke(StopBonusGame);
		}
		Invoke(StopBonusGame, 60f);
	}

	public void StopBonusGame()
	{
		CleanupMarker();
		lastHitTime = 0f;
		currentBonusLevel = 0;
	}

	public bool BonusActive()
	{
		return xMarker != null;
	}

	public override void OnAttacked(HitInfo info)
	{
		bool canGather = info.CanGather;
		float num = UnityEngine.Time.time - lastHitTime;
		lastHitTime = UnityEngine.Time.time;
		if (!hasBonusGame || !canGather || info.Initiator == null || (BonusActive() && !DidHitMarker(info)))
		{
			base.OnAttacked(info);
			return;
		}
		if (xMarker != null && !info.DidGather && info.gatherScale > 0f)
		{
			xMarker.ClientRPC(null, "MarkerHit", currentBonusLevel);
			currentBonusLevel++;
			info.gatherScale = 1f + Mathf.Clamp((float)currentBonusLevel * 0.125f, 0f, 1f);
		}
		Vector3 vector = (xMarker != null) ? xMarker.transform.position : info.HitPositionWorld;
		CleanupMarker();
		if (MarkerData != null)
		{
			Vector3 normal;
			Vector3 nearbyPoint = MarkerData.GetNearbyPoint(base.transform.InverseTransformPoint(vector), ref lastHitMarkerIndex, out normal);
			nearbyPoint = base.transform.TransformPoint(nearbyPoint);
			Quaternion rot = QuaternionEx.LookRotationNormal(base.transform.TransformDirection(normal));
			xMarker = GameManager.server.CreateEntity("assets/content/nature/treesprefabs/trees/effects/tree_marking_nospherecast.prefab", nearbyPoint, rot);
		}
		else
		{
			Vector3 vector2 = Vector3Ex.Direction2D(base.transform.position, vector);
			Vector3 a = Vector3.Cross(vector2, Vector3.up);
			float d = lastDirection;
			Vector3 vector3 = Vector3.Lerp(t: UnityEngine.Random.Range(0.5f, 0.5f), a: -vector2, b: a * d);
			Vector3 vector4 = base.transform.InverseTransformDirection(vector3.normalized) * 2.5f;
			vector4 = base.transform.InverseTransformPoint(GetCollider().ClosestPoint(base.transform.TransformPoint(vector4)));
			Vector3 aimFrom = base.transform.TransformPoint(vector4);
			Vector3 vector5 = base.transform.InverseTransformPoint(info.HitPositionWorld);
			vector4.y = vector5.y;
			Vector3 vector6 = base.transform.InverseTransformPoint(info.Initiator.CenterPoint());
			float min = Mathf.Max(0.75f, vector6.y);
			float max = vector6.y + 0.5f;
			vector4.y = Mathf.Clamp(vector4.y + UnityEngine.Random.Range(0.1f, 0.2f) * ((UnityEngine.Random.Range(0, 2) == 0) ? (-1f) : 1f), min, max);
			Vector3 vector7 = Vector3Ex.Direction2D(base.transform.position, aimFrom);
			Vector3 a2 = vector7;
			vector7 = base.transform.InverseTransformDirection(vector7);
			Quaternion quaternion = QuaternionEx.LookRotationNormal(-vector7, Vector3.zero);
			vector4 = base.transform.TransformPoint(vector4);
			quaternion = QuaternionEx.LookRotationNormal(-a2, Vector3.zero);
			vector4 = GetCollider().ClosestPoint(vector4);
			quaternion = QuaternionEx.LookRotationNormal(-Vector3Ex.Direction(new Line(GetCollider().transform.TransformPoint(new Vector3(0f, 10f, 0f)), GetCollider().transform.TransformPoint(new Vector3(0f, -10f, 0f))).ClosestPoint(vector4), vector4));
			xMarker = GameManager.server.CreateEntity("assets/content/nature/treesprefabs/trees/effects/tree_marking.prefab", vector4, quaternion);
		}
		xMarker.Spawn();
		if (num > 5f)
		{
			StartBonusGame();
		}
		base.OnAttacked(info);
		if (health > 0f)
		{
			lastAttackDamage = info.damageTypes.Total();
			int num2 = Mathf.CeilToInt(health / lastAttackDamage);
			if (num2 < 2)
			{
				ClientRPC(null, "CrackSound", 1);
			}
			else if (num2 < 5)
			{
				ClientRPC(null, "CrackSound", 0);
			}
		}
	}

	public void CleanupMarker()
	{
		if ((bool)xMarker)
		{
			xMarker.Kill();
		}
		xMarker = null;
	}

	public Collider GetCollider()
	{
		if (base.isServer)
		{
			if (!(serverCollider == null))
			{
				return serverCollider;
			}
			return GetComponentInChildren<CapsuleCollider>();
		}
		if (!(clientCollider == null))
		{
			return clientCollider;
		}
		return GetComponent<Collider>();
	}

	public override void OnKilled(HitInfo info)
	{
		if (isKilled)
		{
			return;
		}
		isKilled = true;
		CleanupMarker();
		if (fallOnKilled)
		{
			Collider collider = GetCollider();
			if ((bool)collider)
			{
				collider.enabled = false;
			}
			ClientRPC(null, "TreeFall", info.attackNormal);
			Invoke(DelayedKill, fallDuration + 1f);
		}
		else
		{
			DelayedKill();
		}
	}

	public void DelayedKill()
	{
		Kill();
	}

	public override void PreProcess(IPrefabProcessor preProcess, GameObject rootObj, string name, bool serverside, bool clientside, bool bundling)
	{
		base.PreProcess(preProcess, rootObj, name, serverside, clientside, bundling);
		if (serverside)
		{
			globalBroadcast = ConVar.Tree.global_broadcast;
		}
	}
}
