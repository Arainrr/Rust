#define UNITY_ASSERTIONS
using ConVar;
using Facepunch;
using Facepunch.Rust;
using Network;
using Oxide.Core;
using ProtoBuf;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class ExcavatorArm : BaseEntity
{
	public float yaw1;

	public float yaw2;

	public Transform wheel;

	public float wheelSpeed = 2f;

	public float turnSpeed = 0.1f;

	public Transform miningOffset;

	public GameObjectRef bounceEffect;

	public LightGroupAtTime lights;

	public Material conveyorMaterial;

	public float beltSpeedMax = 0.1f;

	public const Flags Flag_HasPower = Flags.Reserved8;

	public List<ExcavatorOutputPile> outputPiles;

	public SoundDefinition miningStartButtonSoundDef;

	[Header("Production")]
	public ItemAmount[] resourcesToMine;

	public float resourceProductionTickRate = 3f;

	public float timeForFullResources = 120f;

	public ItemAmount[] pendingResources;

	public float movedAmount;

	public float currentTurnThrottle;

	public float lastMoveYaw;

	private float excavatorStartTime;

	public int resourceMiningIndex;

	protected override float PositionTickRate => 0.05f;

	public override bool OnRpcMessage(BasePlayer player, uint rpc, Message msg)
	{
		using (TimeWarning.New("ExcavatorArm.OnRpcMessage"))
		{
			RPCMessage rPCMessage;
			if (rpc == 2059417170 && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - RPC_SetResourceTarget ");
				}
				using (TimeWarning.New("RPC_SetResourceTarget"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.MaxDistance.Test(2059417170u, "RPC_SetResourceTarget", this, player, 3f))
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
							RPC_SetResourceTarget(msg2);
						}
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
						player.Kick("RPC Error in RPC_SetResourceTarget");
					}
				}
				return true;
			}
			if (rpc == 2882020740u && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - RPC_StopMining ");
				}
				using (TimeWarning.New("RPC_StopMining"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.MaxDistance.Test(2882020740u, "RPC_StopMining", this, player, 3f))
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
							RPC_StopMining(msg3);
						}
					}
					catch (Exception exception2)
					{
						Debug.LogException(exception2);
						player.Kick("RPC Error in RPC_StopMining");
					}
				}
				return true;
			}
		}
		return base.OnRpcMessage(player, rpc, msg);
	}

	public bool IsPowered()
	{
		return HasFlag(Flags.Reserved8);
	}

	public bool IsMining()
	{
		return IsOn();
	}

	public override float GetNetworkTime()
	{
		return UnityEngine.Time.fixedTime;
	}

	public void FixedUpdate()
	{
		if (!base.isClient)
		{
			bool flag = IsMining() && IsPowered();
			float num = flag ? 1f : 0f;
			currentTurnThrottle = Mathf.Lerp(currentTurnThrottle, num, UnityEngine.Time.fixedDeltaTime * (flag ? 0.333f : 1f));
			if (Mathf.Abs(num - currentTurnThrottle) < 0.025f)
			{
				currentTurnThrottle = num;
			}
			movedAmount += UnityEngine.Time.fixedDeltaTime * turnSpeed * currentTurnThrottle;
			float t = (Mathf.Sin(movedAmount) + 1f) / 2f;
			float num2 = Mathf.Lerp(yaw1, yaw2, t);
			if (num2 != lastMoveYaw)
			{
				lastMoveYaw = num2;
				base.transform.rotation = Quaternion.Euler(0f, num2, 0f);
				base.transform.hasChanged = true;
			}
		}
	}

	public void BeginMining()
	{
		if (IsPowered())
		{
			SetFlag(Flags.On, true);
			InvokeRepeating(ProduceResources, resourceProductionTickRate, resourceProductionTickRate);
			ExcavatorServerEffects.SetMining(true);
			Facepunch.Rust.Analytics.ExcavatorStarted();
			excavatorStartTime = GetNetworkTime();
			Interface.CallHook("OnExcavatorMiningToggled", this);
		}
	}

	public void StopMining()
	{
		ExcavatorServerEffects.SetMining(false);
		CancelInvoke(ProduceResources);
		if (HasFlag(Flags.On))
		{
			Facepunch.Rust.Analytics.ExcavatorStopped(GetNetworkTime() - excavatorStartTime);
		}
		SetFlag(Flags.On, false);
		Interface.CallHook("OnExcavatorMiningToggled", this);
	}

	public void ProduceResources()
	{
		float num = resourceProductionTickRate / timeForFullResources;
		float num2 = resourcesToMine[resourceMiningIndex].amount * num;
		pendingResources[resourceMiningIndex].amount += num2;
		ItemAmount[] array = pendingResources;
		foreach (ItemAmount itemAmount in array)
		{
			if (itemAmount.amount >= (float)outputPiles.Count)
			{
				int num3 = Mathf.FloorToInt(itemAmount.amount / (float)outputPiles.Count);
				itemAmount.amount -= num3 * 2;
				foreach (ExcavatorOutputPile outputPile in outputPiles)
				{
					Item item = ItemManager.Create(resourcesToMine[resourceMiningIndex].itemDef, num3, 0uL);
					if (Interface.CallHook("OnExcavatorGather", this, item) != null)
					{
						return;
					}
					if (!item.MoveToContainer(outputPile.inventory))
					{
						item.Drop(outputPile.GetDropPosition(), outputPile.GetDropVelocity());
					}
				}
			}
		}
	}

	public override void OnEntityMessage(BaseEntity from, string msg)
	{
		base.OnEntityMessage(from, msg);
		if (msg == "DieselEngineOn")
		{
			SetFlag(Flags.Reserved8, true);
		}
		else if (msg == "DieselEngineOff")
		{
			SetFlag(Flags.Reserved8, false);
			StopMining();
		}
	}

	[RPC_Server.MaxDistance(3f)]
	[RPC_Server]
	public void RPC_SetResourceTarget(RPCMessage msg)
	{
		string text = msg.read.String();
		if (Interface.CallHook("OnExcavatorResourceSet", this, text, msg.player) == null)
		{
			if (text == "HQM")
			{
				resourceMiningIndex = 0;
			}
			else if (text == "Sulfur")
			{
				resourceMiningIndex = 1;
			}
			else if (text == "Stone")
			{
				resourceMiningIndex = 2;
			}
			else if (text == "Metal")
			{
				resourceMiningIndex = 3;
			}
			if (!IsOn())
			{
				BeginMining();
			}
		}
	}

	[RPC_Server.MaxDistance(3f)]
	[RPC_Server]
	public void RPC_StopMining(RPCMessage msg)
	{
	}

	public override void Spawn()
	{
		base.Spawn();
	}

	public override void PostServerLoad()
	{
		base.PostServerLoad();
		Init();
		if (IsOn() && IsPowered())
		{
			BeginMining();
		}
		else
		{
			StopMining();
		}
	}

	public override void Save(SaveInfo info)
	{
		base.Save(info);
		info.msg.ioEntity = Facepunch.Pool.Get<ProtoBuf.IOEntity>();
		info.msg.ioEntity.genericFloat1 = movedAmount;
		info.msg.ioEntity.genericInt1 = resourceMiningIndex;
	}

	public override void Load(LoadInfo info)
	{
		base.Load(info);
		if (info.msg.ioEntity != null)
		{
			movedAmount = info.msg.ioEntity.genericFloat1;
			resourceMiningIndex = info.msg.ioEntity.genericInt1;
		}
	}

	public override void PostMapEntitySpawn()
	{
		base.PostMapEntitySpawn();
		Init();
	}

	public void Init()
	{
		pendingResources = new ItemAmount[resourcesToMine.Length];
		for (int i = 0; i < resourcesToMine.Length; i++)
		{
			pendingResources[i] = new ItemAmount(resourcesToMine[i].itemDef);
		}
		List<ExcavatorOutputPile> obj = Facepunch.Pool.GetList<ExcavatorOutputPile>();
		Vis.Entities(base.transform.position, 200f, obj, 512);
		outputPiles = new List<ExcavatorOutputPile>();
		foreach (ExcavatorOutputPile item in obj)
		{
			if (!item.isClient)
			{
				outputPiles.Add(item);
			}
		}
		Facepunch.Pool.FreeList(ref obj);
	}
}
