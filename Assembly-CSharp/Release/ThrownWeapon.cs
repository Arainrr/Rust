#define UNITY_ASSERTIONS
using ConVar;
using Network;
using Oxide.Core;
using Rust;
using Rust.Ai;
using System;
using UnityEngine;
using UnityEngine.Assertions;

public class ThrownWeapon : AttackEntity
{
	[Header("Throw Weapon")]
	public GameObjectRef prefabToThrow;

	public float maxThrowVelocity = 10f;

	public float tumbleVelocity;

	public Vector3 overrideAngle = Vector3.zero;

	public override bool OnRpcMessage(BasePlayer player, uint rpc, Message msg)
	{
		using (TimeWarning.New("ThrownWeapon.OnRpcMessage"))
		{
			RPCMessage rPCMessage;
			if (rpc == 1513023343 && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (ConVar.Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - DoDrop ");
				}
				using (TimeWarning.New("DoDrop"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.IsActiveItem.Test(1513023343u, "DoDrop", this, player))
						{
							return true;
						}
					}
					try
					{
						using (TimeWarning.New("Call"))
						{
							rPCMessage = default(RPCMessage);
							rPCMessage.connection = msg.connection;
							rPCMessage.player = player;
							rPCMessage.read = msg.read;
							RPCMessage msg2 = rPCMessage;
							DoDrop(msg2);
						}
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
						player.Kick("RPC Error in DoDrop");
					}
				}
				return true;
			}
			if (rpc == 1974840882 && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (ConVar.Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - DoThrow ");
				}
				using (TimeWarning.New("DoThrow"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.IsActiveItem.Test(1974840882u, "DoThrow", this, player))
						{
							return true;
						}
					}
					try
					{
						using (TimeWarning.New("Call"))
						{
							rPCMessage = default(RPCMessage);
							rPCMessage.connection = msg.connection;
							rPCMessage.player = player;
							rPCMessage.read = msg.read;
							RPCMessage msg3 = rPCMessage;
							DoThrow(msg3);
						}
					}
					catch (Exception exception2)
					{
						Debug.LogException(exception2);
						player.Kick("RPC Error in DoThrow");
					}
				}
				return true;
			}
		}
		return base.OnRpcMessage(player, rpc, msg);
	}

	public override Vector3 GetInheritedVelocity(BasePlayer player)
	{
		return player.GetInheritedThrowVelocity();
	}

	public void ServerThrow(Vector3 targetPosition)
	{
		if (base.isClient || !HasItemAmount() || HasAttackCooldown())
		{
			return;
		}
		BasePlayer ownerPlayer = GetOwnerPlayer();
		if (ownerPlayer == null)
		{
			return;
		}
		Vector3 position = ownerPlayer.eyes.position;
		Vector3 a = ownerPlayer.eyes.BodyForward();
		float d = 1f;
		SignalBroadcast(Signal.Throw, string.Empty);
		BaseEntity baseEntity = GameManager.server.CreateEntity(prefabToThrow.resourcePath, position, Quaternion.LookRotation((overrideAngle == Vector3.zero) ? (-a) : overrideAngle));
		if (baseEntity == null)
		{
			return;
		}
		baseEntity.creatorEntity = ownerPlayer;
		Vector3 vector = a + Quaternion.AngleAxis(10f, Vector3.right) * Vector3.up;
		float num = GetThrowVelocity(position, targetPosition, vector);
		if (float.IsNaN(num))
		{
			vector = a + Quaternion.AngleAxis(20f, Vector3.right) * Vector3.up;
			num = GetThrowVelocity(position, targetPosition, vector);
			if (float.IsNaN(num))
			{
				num = 5f;
			}
		}
		baseEntity.SetVelocity(vector * num * d);
		if (tumbleVelocity > 0f)
		{
			baseEntity.SetAngularVelocity(new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)) * tumbleVelocity);
		}
		baseEntity.Spawn();
		StartAttackCooldown(repeatDelay);
		UseItemAmount(1);
		TimedExplosive timedExplosive = baseEntity as TimedExplosive;
		Sensation sensation;
		if (timedExplosive != null)
		{
			float num2 = 0f;
			foreach (DamageTypeEntry damageType in timedExplosive.damageTypes)
			{
				num2 += damageType.amount;
			}
			sensation = default(Sensation);
			sensation.Type = SensationType.ThrownWeapon;
			sensation.Position = ownerPlayer.transform.position;
			sensation.Radius = 50f;
			sensation.DamagePotential = num2;
			sensation.InitiatorPlayer = ownerPlayer;
			sensation.Initiator = ownerPlayer;
			sensation.UsedEntity = timedExplosive;
			Sense.Stimulate(sensation);
		}
		else
		{
			sensation = default(Sensation);
			sensation.Type = SensationType.ThrownWeapon;
			sensation.Position = ownerPlayer.transform.position;
			sensation.Radius = 50f;
			sensation.DamagePotential = 0f;
			sensation.InitiatorPlayer = ownerPlayer;
			sensation.Initiator = ownerPlayer;
			sensation.UsedEntity = this;
			Sense.Stimulate(sensation);
		}
	}

	private float GetThrowVelocity(Vector3 throwPos, Vector3 targetPos, Vector3 aimDir)
	{
		Vector3 vector = targetPos - throwPos;
		float magnitude = new Vector2(vector.x, vector.z).magnitude;
		float y = vector.y;
		float magnitude2 = new Vector2(aimDir.x, aimDir.z).magnitude;
		float y2 = aimDir.y;
		float y3 = UnityEngine.Physics.gravity.y;
		return Mathf.Sqrt(0.5f * y3 * magnitude * magnitude / (magnitude2 * (magnitude2 * y - y2 * magnitude)));
	}

	[RPC_Server.IsActiveItem]
	[RPC_Server]
	private void DoThrow(RPCMessage msg)
	{
		if (!HasItemAmount() || HasAttackCooldown())
		{
			return;
		}
		Vector3 vector = msg.read.Vector3();
		Vector3 normalized = msg.read.Vector3().normalized;
		float d = Mathf.Clamp01(msg.read.Float());
		if (msg.player.isMounted || msg.player.HasParent())
		{
			vector = msg.player.eyes.position;
		}
		else if (!ValidateEyePos(msg.player, vector))
		{
			return;
		}
		BaseEntity baseEntity = GameManager.server.CreateEntity(prefabToThrow.resourcePath, vector, Quaternion.LookRotation((overrideAngle == Vector3.zero) ? (-normalized) : overrideAngle));
		if (baseEntity == null)
		{
			return;
		}
		baseEntity.creatorEntity = msg.player;
		baseEntity.skinID = skinID;
		baseEntity.SetVelocity(GetInheritedVelocity(msg.player) + normalized * maxThrowVelocity * d + msg.player.estimatedVelocity * 0.5f);
		if (tumbleVelocity > 0f)
		{
			baseEntity.SetAngularVelocity(new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)) * tumbleVelocity);
		}
		baseEntity.Spawn();
		SetUpThrownWeapon(baseEntity);
		StartAttackCooldown(repeatDelay);
		Interface.CallHook("OnExplosiveThrown", msg.player, baseEntity, this);
		UseItemAmount(1);
		BasePlayer player = msg.player;
		if (player != null)
		{
			TimedExplosive timedExplosive = baseEntity as TimedExplosive;
			Sensation sensation;
			if (timedExplosive != null)
			{
				float num = 0f;
				foreach (DamageTypeEntry damageType in timedExplosive.damageTypes)
				{
					num += damageType.amount;
				}
				sensation = default(Sensation);
				sensation.Type = SensationType.ThrownWeapon;
				sensation.Position = player.transform.position;
				sensation.Radius = 50f;
				sensation.DamagePotential = num;
				sensation.InitiatorPlayer = player;
				sensation.Initiator = player;
				sensation.UsedEntity = timedExplosive;
				Sense.Stimulate(sensation);
			}
			else
			{
				sensation = default(Sensation);
				sensation.Type = SensationType.ThrownWeapon;
				sensation.Position = player.transform.position;
				sensation.Radius = 50f;
				sensation.DamagePotential = 0f;
				sensation.InitiatorPlayer = player;
				sensation.Initiator = player;
				sensation.UsedEntity = this;
				Sense.Stimulate(sensation);
			}
		}
	}

	[RPC_Server.IsActiveItem]
	[RPC_Server]
	private void DoDrop(RPCMessage msg)
	{
		if (!HasItemAmount() || HasAttackCooldown())
		{
			return;
		}
		Vector3 vector = msg.read.Vector3();
		Vector3 normalized = msg.read.Vector3().normalized;
		if (msg.player.isMounted || msg.player.HasParent())
		{
			vector = msg.player.eyes.position;
		}
		else if (!ValidateEyePos(msg.player, vector))
		{
			return;
		}
		BaseEntity baseEntity = GameManager.server.CreateEntity(prefabToThrow.resourcePath, vector, Quaternion.LookRotation(Vector3.up));
		if (baseEntity == null)
		{
			return;
		}
		RaycastHit hitInfo;
		if (UnityEngine.Physics.SphereCast(new Ray(vector, normalized), 0.05f, out hitInfo, 1.5f, 1236478737))
		{
			Vector3 point = hitInfo.point;
			Vector3 normal = hitInfo.normal;
			BaseEntity entity = RaycastHitEx.GetEntity(hitInfo);
			if ((bool)entity && entity is StabilityEntity && baseEntity is TimedExplosive)
			{
				entity = entity.ToServer<BaseEntity>();
				TimedExplosive obj = baseEntity as TimedExplosive;
				obj.onlyDamageParent = true;
				obj.DoStick(point, normal, entity);
			}
			else
			{
				baseEntity.SetVelocity(normalized);
			}
		}
		else
		{
			baseEntity.SetVelocity(normalized);
		}
		baseEntity.creatorEntity = msg.player;
		baseEntity.skinID = skinID;
		baseEntity.Spawn();
		SetUpThrownWeapon(baseEntity);
		StartAttackCooldown(repeatDelay);
		Interface.CallHook("OnExplosiveDropped", msg.player, baseEntity, this);
		UseItemAmount(1);
	}

	protected virtual void SetUpThrownWeapon(BaseEntity ent)
	{
	}
}
