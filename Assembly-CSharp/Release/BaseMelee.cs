#define UNITY_ASSERTIONS
using ConVar;
using Facepunch;
using Network;
using Oxide.Core;
using ProtoBuf;
using Rust;
using Rust.Ai;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class BaseMelee : AttackEntity
{
	[Serializable]
	public class MaterialFX
	{
		public string materialName;

		public GameObjectRef fx;
	}

	[Header("Throwing")]
	public bool canThrowAsProjectile;

	public bool canAiHearIt;

	public bool onlyThrowAsProjectile;

	[Header("Melee")]
	public DamageProperties damageProperties;

	public List<DamageTypeEntry> damageTypes;

	public float maxDistance = 1.5f;

	public float attackRadius = 0.3f;

	public bool isAutomatic = true;

	public bool blockSprintOnAttack = true;

	[Header("Effects")]
	public GameObjectRef strikeFX;

	public bool useStandardHitEffects = true;

	[Header("NPCUsage")]
	public float aiStrikeDelay = 0.2f;

	public GameObjectRef swingEffect;

	public List<MaterialFX> materialStrikeFX = new List<MaterialFX>();

	[Header("Other")]
	[Range(0f, 1f)]
	public float heartStress = 0.5f;

	public ResourceDispenser.GatherProperties gathering;

	public override bool OnRpcMessage(BasePlayer player, uint rpc, Message msg)
	{
		using (TimeWarning.New("BaseMelee.OnRpcMessage"))
		{
			RPCMessage rPCMessage;
			if (rpc == 3168282921u && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (ConVar.Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - CLProject ");
				}
				using (TimeWarning.New("CLProject"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.FromOwner.Test(3168282921u, "CLProject", this, player))
						{
							return true;
						}
						if (!RPC_Server.IsActiveItem.Test(3168282921u, "CLProject", this, player))
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
							CLProject(msg2);
						}
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
						player.Kick("RPC Error in CLProject");
					}
				}
				return true;
			}
			if (rpc == 4088326849u && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (ConVar.Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - PlayerAttack ");
				}
				using (TimeWarning.New("PlayerAttack"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.IsActiveItem.Test(4088326849u, "PlayerAttack", this, player))
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
							PlayerAttack(msg3);
						}
					}
					catch (Exception exception2)
					{
						Debug.LogException(exception2);
						player.Kick("RPC Error in PlayerAttack");
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

	[RPC_Server.FromOwner]
	[RPC_Server.IsActiveItem]
	[RPC_Server]
	private void CLProject(RPCMessage msg)
	{
		BasePlayer player = msg.player;
		if (!VerifyClientAttack(player))
		{
			SendNetworkUpdate();
		}
		else
		{
			if (player == null || player.IsHeadUnderwater())
			{
				return;
			}
			if (!canThrowAsProjectile)
			{
				AntiHack.Log(player, AntiHackType.ProjectileHack, "Not throwable (" + base.ShortPrefabName + ")");
				player.stats.combat.Log(this, "not_throwable");
				return;
			}
			Item item = GetItem();
			if (item == null)
			{
				AntiHack.Log(player, AntiHackType.ProjectileHack, "Item not found (" + base.ShortPrefabName + ")");
				player.stats.combat.Log(this, "item_missing");
				return;
			}
			ItemModProjectile component = item.info.GetComponent<ItemModProjectile>();
			if (component == null)
			{
				AntiHack.Log(player, AntiHackType.ProjectileHack, "Item mod not found (" + base.ShortPrefabName + ")");
				player.stats.combat.Log(this, "mod_missing");
				return;
			}
			ProjectileShoot projectileShoot = ProjectileShoot.Deserialize(msg.read);
			if (projectileShoot.projectiles.Count != 1)
			{
				AntiHack.Log(player, AntiHackType.ProjectileHack, "Projectile count mismatch (" + base.ShortPrefabName + ")");
				player.stats.combat.Log(this, "count_mismatch");
				return;
			}
			player.CleanupExpiredProjectiles();
			foreach (ProjectileShoot.Projectile projectile in projectileShoot.projectiles)
			{
				if (player.HasFiredProjectile(projectile.projectileID))
				{
					AntiHack.Log(player, AntiHackType.ProjectileHack, "Duplicate ID (" + projectile.projectileID + ")");
					player.stats.combat.Log(this, "duplicate_id");
				}
				else if (ValidateEyePos(player, projectile.startPos))
				{
					player.NoteFiredProjectile(projectile.projectileID, projectile.startPos, projectile.startVel, this, item.info, item);
					Effect effect = new Effect();
					effect.Init(Effect.Type.Projectile, projectile.startPos, projectile.startVel, msg.connection);
					effect.scale = 1f;
					effect.pooledString = component.projectileObject.resourcePath;
					effect.number = projectile.seed;
					EffectNetwork.Send(effect);
				}
			}
			projectileShoot?.Dispose();
			item.SetParent(null);
			Interface.CallHook("OnMeleeThrown", player, item);
			if (!canAiHearIt)
			{
				return;
			}
			float num = 0f;
			if (component.projectileObject != null)
			{
				GameObject gameObject = component.projectileObject.Get();
				if (gameObject != null)
				{
					Projectile component2 = gameObject.GetComponent<Projectile>();
					if (component2 != null)
					{
						foreach (DamageTypeEntry damageType in component2.damageTypes)
						{
							num += damageType.amount;
						}
					}
				}
			}
			if (player != null)
			{
				Sensation sensation = default(Sensation);
				sensation.Type = SensationType.ThrownWeapon;
				sensation.Position = player.transform.position;
				sensation.Radius = 50f;
				sensation.DamagePotential = num;
				sensation.InitiatorPlayer = player;
				sensation.Initiator = player;
				Sense.Stimulate(sensation);
			}
		}
	}

	public override void GetAttackStats(HitInfo info)
	{
		info.damageTypes.Add(damageTypes);
		info.CanGather = gathering.Any();
	}

	public virtual void DoAttackShared(HitInfo info)
	{
		if (Interface.CallHook("OnPlayerAttack", GetOwnerPlayer(), info) != null)
		{
			return;
		}
		GetAttackStats(info);
		if (info.HitEntity != null)
		{
			using (TimeWarning.New("OnAttacked", 50))
			{
				info.HitEntity.OnAttacked(info);
			}
		}
		if (info.DoHitEffects)
		{
			if (base.isServer)
			{
				using (TimeWarning.New("ImpactEffect", 20))
				{
					Effect.server.ImpactEffect(info);
				}
			}
			else
			{
				using (TimeWarning.New("ImpactEffect", 20))
				{
					Effect.client.ImpactEffect(info);
				}
			}
		}
		if (base.isServer && !base.IsDestroyed)
		{
			using (TimeWarning.New("UpdateItemCondition", 50))
			{
				UpdateItemCondition(info);
			}
			StartAttackCooldown(repeatDelay);
		}
	}

	public ResourceDispenser.GatherPropertyEntry GetGatherInfoFromIndex(ResourceDispenser.GatherType index)
	{
		return gathering.GetFromIndex(index);
	}

	public virtual bool CanHit(HitTest info)
	{
		return true;
	}

	public float TotalDamage()
	{
		float num = 0f;
		foreach (DamageTypeEntry damageType in damageTypes)
		{
			if (!(damageType.amount <= 0f))
			{
				num += damageType.amount;
			}
		}
		return num;
	}

	public bool IsItemBroken()
	{
		return GetOwnerItem()?.isBroken ?? true;
	}

	public void LoseCondition(float amount)
	{
		GetOwnerItem()?.LoseCondition(amount);
	}

	public virtual float GetConditionLoss()
	{
		return 1f;
	}

	public void UpdateItemCondition(HitInfo info)
	{
		Item ownerItem = GetOwnerItem();
		if (ownerItem != null && ownerItem.hasCondition && info != null && info.DidHit && !info.DidGather)
		{
			float conditionLoss = GetConditionLoss();
			float num = 0f;
			foreach (DamageTypeEntry damageType in damageTypes)
			{
				if (!(damageType.amount <= 0f))
				{
					num += Mathf.Clamp(damageType.amount - info.damageTypes.Get(damageType.type), 0f, damageType.amount);
				}
			}
			conditionLoss += num * 0.2f;
			ownerItem.LoseCondition(conditionLoss);
		}
	}

	[RPC_Server]
	[RPC_Server.IsActiveItem]
	public void PlayerAttack(RPCMessage msg)
	{
		BasePlayer player = msg.player;
		if (!VerifyClientAttack(player))
		{
			SendNetworkUpdate();
		}
		else
		{
			using (TimeWarning.New("PlayerAttack", 50))
			{
				using (PlayerAttack playerAttack = ProtoBuf.PlayerAttack.Deserialize(msg.read))
				{
					HitInfo hitInfo;
					if (playerAttack != null)
					{
						hitInfo = Facepunch.Pool.Get<HitInfo>();
						hitInfo.LoadFromAttack(playerAttack.attack, true);
						hitInfo.Initiator = player;
						hitInfo.Weapon = this;
						hitInfo.WeaponPrefab = this;
						hitInfo.Predicted = msg.connection;
						hitInfo.damageProperties = damageProperties;
						if (Interface.CallHook("OnMeleeAttack", player, hitInfo) == null)
						{
							if (hitInfo.IsNaNOrInfinity())
							{
								string shortPrefabName = base.ShortPrefabName;
								AntiHack.Log(player, AntiHackType.MeleeHack, "Contains NaN (" + shortPrefabName + ")");
								player.stats.combat.Log(hitInfo, "melee_nan");
							}
							else
							{
								if (ConVar.AntiHack.melee_protection <= 0 || !hitInfo.HitEntity)
								{
									goto IL_0688;
								}
								bool flag = true;
								BasePlayer basePlayer = hitInfo.HitEntity as BasePlayer;
								float num = 1f + ConVar.AntiHack.melee_forgiveness;
								float melee_clientframes = ConVar.AntiHack.melee_clientframes;
								float melee_serverframes = ConVar.AntiHack.melee_serverframes;
								float num2 = melee_clientframes / 60f;
								float num3 = melee_serverframes * Mathx.Max(UnityEngine.Time.deltaTime, UnityEngine.Time.smoothDeltaTime, UnityEngine.Time.fixedDeltaTime);
								float num4 = (player.desyncTime + num2 + num3) * num;
								int layerMask = ConVar.AntiHack.melee_terraincheck ? 10551296 : 2162688;
								if ((bool)basePlayer && hitInfo.boneArea == (HitArea)(-1))
								{
									string shortPrefabName2 = base.ShortPrefabName;
									string shortPrefabName3 = hitInfo.HitEntity.ShortPrefabName;
									AntiHack.Log(player, AntiHackType.MeleeHack, "Bone is invalid  (" + shortPrefabName2 + " on " + shortPrefabName3 + " bone " + hitInfo.HitBone + ")");
									player.stats.combat.Log(hitInfo, "melee_bone");
									flag = false;
								}
								if (ConVar.AntiHack.projectile_protection >= 2)
								{
									float num5 = hitInfo.HitEntity.MaxVelocity() + hitInfo.HitEntity.GetParentVelocity().magnitude;
									float num6 = hitInfo.HitEntity.BoundsPadding() + num4 * num5;
									float num7 = hitInfo.HitEntity.Distance(hitInfo.HitPositionWorld);
									if (num7 > num6)
									{
										string shortPrefabName4 = base.ShortPrefabName;
										string shortPrefabName5 = hitInfo.HitEntity.ShortPrefabName;
										AntiHack.Log(player, AntiHackType.MeleeHack, "Entity too far away (" + shortPrefabName4 + " on " + shortPrefabName5 + " with " + num7 + "m > " + num6 + "m in " + num4 + "s)");
										player.stats.combat.Log(hitInfo, "melee_target");
										flag = false;
									}
								}
								if (ConVar.AntiHack.melee_protection >= 1)
								{
									float num8 = hitInfo.Initiator.MaxVelocity() + hitInfo.Initiator.GetParentVelocity().magnitude;
									float num9 = hitInfo.Initiator.BoundsPadding() + num4 * num8 + num * maxDistance;
									float num10 = hitInfo.Initiator.Distance(hitInfo.HitPositionWorld);
									if (num10 > num9)
									{
										string shortPrefabName6 = base.ShortPrefabName;
										string shortPrefabName7 = hitInfo.HitEntity.ShortPrefabName;
										AntiHack.Log(player, AntiHackType.MeleeHack, "Initiator too far away (" + shortPrefabName6 + " on " + shortPrefabName7 + " with " + num10 + "m > " + num9 + "m in " + num4 + "s)");
										player.stats.combat.Log(hitInfo, "melee_initiator");
										flag = false;
									}
								}
								if (ConVar.AntiHack.melee_protection >= 3)
								{
									Vector3 pointStart = hitInfo.PointStart;
									Vector3 vector = hitInfo.HitPositionWorld + hitInfo.HitNormalWorld.normalized * 0.001f;
									Vector3 center = player.eyes.center;
									Vector3 position = player.eyes.position;
									Vector3 vector2 = pointStart;
									Vector3 vector3 = hitInfo.PositionOnRay(vector);
									Vector3 vector4 = vector;
									bool num11 = GamePhysics.LineOfSight(center, position, vector2, vector3, vector4, layerMask);
									if (!num11)
									{
										player.stats.Add("hit_" + hitInfo.HitEntity.Categorize() + "_indirect_los", 1, Stats.Server);
									}
									else
									{
										player.stats.Add("hit_" + hitInfo.HitEntity.Categorize() + "_direct_los", 1, Stats.Server);
									}
									if (!num11)
									{
										string shortPrefabName8 = base.ShortPrefabName;
										string shortPrefabName9 = hitInfo.HitEntity.ShortPrefabName;
										AntiHack.Log(player, AntiHackType.MeleeHack, "Line of sight (" + shortPrefabName8 + " on " + shortPrefabName9 + ") " + center + " " + position + " " + vector2 + " " + vector3 + " " + vector4);
										player.stats.combat.Log(hitInfo, "melee_los");
										flag = false;
									}
									if ((bool)basePlayer)
									{
										Vector3 vector5 = hitInfo.HitPositionWorld + hitInfo.HitNormalWorld.normalized * 0.001f;
										Vector3 position2 = basePlayer.eyes.position;
										Vector3 vector6 = basePlayer.CenterPoint();
										if (!GamePhysics.LineOfSight(vector5, position2, layerMask) && !GamePhysics.LineOfSight(vector5, vector6, layerMask))
										{
											string shortPrefabName10 = base.ShortPrefabName;
											string shortPrefabName11 = hitInfo.HitEntity.ShortPrefabName;
											AntiHack.Log(player, AntiHackType.MeleeHack, "Line of sight (" + shortPrefabName10 + " on " + shortPrefabName11 + ") " + vector5 + " " + position2 + " or " + vector5 + " " + vector6);
											player.stats.combat.Log(hitInfo, "melee_los");
											flag = false;
										}
									}
								}
								if (flag)
								{
									goto IL_0688;
								}
								AntiHack.AddViolation(player, AntiHackType.MeleeHack, ConVar.AntiHack.melee_penalty);
							}
						}
					}
					goto end_IL_0031;
					IL_0688:
					player.metabolism.UseHeart(heartStress * 0.2f);
					using (TimeWarning.New("DoAttackShared", 50))
					{
						DoAttackShared(hitInfo);
					}
					end_IL_0031:;
				}
			}
		}
	}

	public override bool CanBeUsedInWater()
	{
		return true;
	}

	public string GetStrikeEffectPath(string materialName)
	{
		for (int i = 0; i < materialStrikeFX.Count; i++)
		{
			if (materialStrikeFX[i].materialName == materialName && materialStrikeFX[i].fx.isValid)
			{
				return materialStrikeFX[i].fx.resourcePath;
			}
		}
		return strikeFX.resourcePath;
	}

	public override void ServerUse()
	{
		if (base.isClient || HasAttackCooldown())
		{
			return;
		}
		BasePlayer ownerPlayer = GetOwnerPlayer();
		if (!(ownerPlayer == null))
		{
			StartAttackCooldown(repeatDelay * 2f);
			ownerPlayer.SignalBroadcast(Signal.Attack, string.Empty);
			if (swingEffect.isValid)
			{
				Effect.server.Run(swingEffect.resourcePath, base.transform.position, Vector3.forward, ownerPlayer.net.connection);
			}
			if (IsInvoking(ServerUse_Strike))
			{
				CancelInvoke(ServerUse_Strike);
			}
			Invoke(ServerUse_Strike, aiStrikeDelay);
		}
	}

	public virtual void ServerUse_OnHit(HitInfo info)
	{
	}

	public void ServerUse_Strike()
	{
		BasePlayer ownerPlayer = GetOwnerPlayer();
		if (ownerPlayer == null)
		{
			return;
		}
		Vector3 position = ownerPlayer.eyes.position;
		Vector3 vector = ownerPlayer.eyes.BodyForward();
		for (int i = 0; i < 2; i++)
		{
			List<RaycastHit> obj = Facepunch.Pool.GetList<RaycastHit>();
			GamePhysics.TraceAll(new Ray(position - vector * ((i == 0) ? 0f : 0.2f), vector), (i == 0) ? 0f : attackRadius, obj, effectiveRange + 0.2f, 1219701521);
			bool flag = false;
			for (int j = 0; j < obj.Count; j++)
			{
				RaycastHit hit = obj[j];
				BaseEntity entity = RaycastHitEx.GetEntity(hit);
				if (!(entity == null) && (!(entity != null) || (!(entity == ownerPlayer) && !entity.EqualNetID(ownerPlayer))) && (!(entity != null) || !entity.isClient) && !(entity.Categorize() == ownerPlayer.Categorize()))
				{
					float num = 0f;
					foreach (DamageTypeEntry damageType in damageTypes)
					{
						num += damageType.amount;
					}
					entity.OnAttacked(new HitInfo(ownerPlayer, entity, DamageType.Slash, num * npcDamageScale));
					HitInfo obj2 = Facepunch.Pool.Get<HitInfo>();
					obj2.HitEntity = entity;
					obj2.HitPositionWorld = hit.point;
					obj2.HitNormalWorld = -vector;
					if (entity is BaseNpc || entity is BasePlayer)
					{
						obj2.HitMaterial = StringPool.Get("Flesh");
					}
					else
					{
						obj2.HitMaterial = StringPool.Get((RaycastHitEx.GetCollider(hit).sharedMaterial != null) ? AssetNameCache.GetName(RaycastHitEx.GetCollider(hit).sharedMaterial) : "generic");
					}
					ServerUse_OnHit(obj2);
					Effect.server.ImpactEffect(obj2);
					Facepunch.Pool.Free(ref obj2);
					flag = true;
					if (!(entity != null) || entity.ShouldBlockProjectiles())
					{
						break;
					}
				}
			}
			Facepunch.Pool.FreeList(ref obj);
			if (flag)
			{
				break;
			}
		}
	}
}
