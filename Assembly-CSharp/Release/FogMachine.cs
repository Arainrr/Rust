#define UNITY_ASSERTIONS
using ConVar;
using Network;
using System;
using UnityEngine;
using UnityEngine.Assertions;

public class FogMachine : StorageContainer
{
	public const Flags FogFieldOn = Flags.Reserved8;

	public const Flags MotionMode = Flags.Reserved7;

	public const Flags Emitting = Flags.Reserved6;

	public const Flags Flag_HasJuice = Flags.Reserved5;

	public float fogLength = 60f;

	public float nozzleBlastDuration = 5f;

	public float fuelPerSec = 1f;

	private float pendingFuel;

	public bool IsEmitting()
	{
		return HasFlag(Flags.Reserved6);
	}

	public bool HasJuice()
	{
		return HasFlag(Flags.Reserved5);
	}

	[RPC_Server]
	[RPC_Server.IsVisible(3f)]
	public void SetFogOn(RPCMessage msg)
	{
		if (!IsEmitting() && !IsOn() && HasFuel() && msg.player.CanBuild())
		{
			SetFlag(Flags.On, true);
			InvokeRepeating(StartFogging, 0f, fogLength - 1f);
		}
	}

	[RPC_Server.IsVisible(3f)]
	[RPC_Server]
	public void SetFogOff(RPCMessage msg)
	{
		if (IsOn() && msg.player.CanBuild())
		{
			CancelInvoke(StartFogging);
			SetFlag(Flags.On, false);
		}
	}

	[RPC_Server]
	[RPC_Server.IsVisible(3f)]
	public void SetMotionDetection(RPCMessage msg)
	{
		bool flag = msg.read.Bit();
		if (msg.player.CanBuild())
		{
			SetFlag(Flags.Reserved7, flag);
			if (flag)
			{
				SetFlag(Flags.On, false);
			}
			UpdateMotionMode();
		}
	}

	public void UpdateMotionMode()
	{
		if (HasFlag(Flags.Reserved7))
		{
			InvokeRandomized(CheckTrigger, UnityEngine.Random.Range(0f, 0.5f), 0.5f, 0.1f);
		}
		else
		{
			CancelInvoke(CheckTrigger);
		}
	}

	public void CheckTrigger()
	{
		if (!IsEmitting() && BasePlayer.AnyPlayersVisibleToEntity(base.transform.position + base.transform.forward * 3f, 3f, this, base.transform.position + Vector3.up * 0.1f, true))
		{
			StartFogging();
		}
	}

	public void StartFogging()
	{
		if (!UseFuel(1f))
		{
			CancelInvoke(StartFogging);
			SetFlag(Flags.On, false);
			return;
		}
		SetFlag(Flags.Reserved6, true);
		Invoke(EnableFogField, 1f);
		Invoke(DisableNozzle, nozzleBlastDuration);
		Invoke(FinishFogging, fogLength);
	}

	public virtual void EnableFogField()
	{
		SetFlag(Flags.Reserved8, true);
	}

	public void DisableNozzle()
	{
		SetFlag(Flags.Reserved6, false);
	}

	public virtual void FinishFogging()
	{
		SetFlag(Flags.Reserved8, false);
	}

	public override void PostServerLoad()
	{
		base.PostServerLoad();
		SetFlag(Flags.Reserved8, false);
		SetFlag(Flags.Reserved6, false);
		SetFlag(Flags.Reserved5, HasFuel());
		if (IsOn())
		{
			InvokeRepeating(StartFogging, 0f, fogLength - 1f);
		}
		UpdateMotionMode();
	}

	public override void PlayerStoppedLooting(BasePlayer player)
	{
		SetFlag(Flags.Reserved5, HasFuel());
		base.PlayerStoppedLooting(player);
	}

	public int GetFuelAmount()
	{
		Item slot = base.inventory.GetSlot(0);
		if (slot == null || slot.amount < 1)
		{
			return 0;
		}
		return slot.amount;
	}

	public bool HasFuel()
	{
		return GetFuelAmount() >= 1;
	}

	public bool UseFuel(float seconds)
	{
		Item slot = base.inventory.GetSlot(0);
		if (slot == null || slot.amount < 1)
		{
			return false;
		}
		pendingFuel += seconds * fuelPerSec;
		if (pendingFuel >= 1f)
		{
			int num = Mathf.FloorToInt(pendingFuel);
			slot.UseItem(num);
			pendingFuel -= num;
		}
		return true;
	}

	public virtual bool MotionModeEnabled()
	{
		return true;
	}

	public override bool OnRpcMessage(BasePlayer player, uint rpc, Message msg)
	{
		using (TimeWarning.New("FogMachine.OnRpcMessage"))
		{
			RPCMessage rPCMessage;
			if (rpc == 2788115565u && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - SetFogOff ");
				}
				using (TimeWarning.New("SetFogOff"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.IsVisible.Test(2788115565u, "SetFogOff", this, player, 3f))
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
							RPCMessage fogOff = rPCMessage;
							SetFogOff(fogOff);
						}
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
						player.Kick("RPC Error in SetFogOff");
					}
				}
				return true;
			}
			if (rpc == 3905831928u && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - SetFogOn ");
				}
				using (TimeWarning.New("SetFogOn"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.IsVisible.Test(3905831928u, "SetFogOn", this, player, 3f))
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
							RPCMessage fogOn = rPCMessage;
							SetFogOn(fogOn);
						}
					}
					catch (Exception exception2)
					{
						Debug.LogException(exception2);
						player.Kick("RPC Error in SetFogOn");
					}
				}
				return true;
			}
			if (rpc == 1773639087 && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - SetMotionDetection ");
				}
				using (TimeWarning.New("SetMotionDetection"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.IsVisible.Test(1773639087u, "SetMotionDetection", this, player, 3f))
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
							RPCMessage motionDetection = rPCMessage;
							SetMotionDetection(motionDetection);
						}
					}
					catch (Exception exception3)
					{
						Debug.LogException(exception3);
						player.Kick("RPC Error in SetMotionDetection");
					}
				}
				return true;
			}
		}
		return base.OnRpcMessage(player, rpc, msg);
	}
}
