#define UNITY_ASSERTIONS
using ConVar;
using Network;
using System;
using UnityEngine;
using UnityEngine.Assertions;

public class RHIB : MotorRowboat
{
	public GameObject steeringWheel;

	[ServerVar(Help = "Population active on the server")]
	public static float rhibpopulation;

	private float targetGasPedal;

	public override bool OnRpcMessage(BasePlayer player, uint rpc, Message msg)
	{
		using (TimeWarning.New("RHIB.OnRpcMessage"))
		{
			if (rpc == 1382282393 && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - Server_Release ");
				}
				using (TimeWarning.New("Server_Release"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.IsVisible.Test(1382282393u, "Server_Release", this, player, 6f))
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
							Server_Release(msg2);
						}
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
						player.Kick("RPC Error in Server_Release");
					}
				}
				return true;
			}
		}
		return base.OnRpcMessage(player, rpc, msg);
	}

	[RPC_Server.IsVisible(6f)]
	[RPC_Server]
	public void Server_Release(RPCMessage msg)
	{
		if (!(GetParentEntity() == null))
		{
			SetParent(null, true, true);
			rigidBody.isKinematic = false;
		}
	}

	protected override void VehicleFixedUpdate()
	{
		gasPedal = Mathf.MoveTowards(gasPedal, targetGasPedal, UnityEngine.Time.fixedDeltaTime * 1f);
		base.VehicleFixedUpdate();
	}

	public override bool EngineOn()
	{
		return base.EngineOn();
	}

	public override void DriverInput(InputState inputState, BasePlayer player)
	{
		base.DriverInput(inputState, player);
		if (inputState.IsDown(BUTTON.FORWARD))
		{
			targetGasPedal = 1f;
		}
		else if (inputState.IsDown(BUTTON.BACKWARD))
		{
			targetGasPedal = -0.5f;
		}
		else
		{
			targetGasPedal = 0f;
		}
		if (inputState.IsDown(BUTTON.LEFT))
		{
			steering = 1f;
		}
		else if (inputState.IsDown(BUTTON.RIGHT))
		{
			steering = -1f;
		}
		else
		{
			steering = 0f;
		}
	}

	public void AddFuel(int amount)
	{
		BaseEntity baseEntity = fuelSystem.fuelStorageInstance.Get(true);
		if ((bool)baseEntity)
		{
			baseEntity.GetComponent<StorageContainer>().inventory.AddItem(ItemManager.FindItemDefinition("lowgradefuel"), amount, 0uL);
		}
	}
}
