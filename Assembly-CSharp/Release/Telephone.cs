#define UNITY_ASSERTIONS
using ConVar;
using Facepunch;
using Network;
using ProtoBuf;
using System;
using UnityEngine;
using UnityEngine.Assertions;

public class Telephone : ContainerIOEntity
{
	public enum CallState
	{
		Idle,
		Dialing,
		Ringing,
		InProcess
	}

	public enum DialFailReason
	{
		TimedOut,
		Engaged,
		WrongNumber,
		CallSelf,
		HangUp,
		NetworkBusy,
		TimeOutDuringCall
	}

	public const int MaxPhoneNameLength = 20;

	public const int MaxSavedNumbers = 10;

	public Transform PhoneHotspot;

	public Transform AnsweringMachineHotspot;

	public Transform[] HandsetRoots;

	public ItemDefinition[] ValidCassettes;

	public Transform ParentedHandsetTransform;

	public LineRenderer CableLineRenderer;

	public Transform CableStartPoint;

	public Transform CableEndPoint;

	public float LineDroopAmount = 0.25f;

	public PhoneController Controller;

	public Cassette cachedCassette
	{
		get;
		private set;
	}

	public BaseEntity ToBaseEntity => this;

	public override bool OnRpcMessage(BasePlayer player, uint rpc, Message msg)
	{
		using (TimeWarning.New("Telephone.OnRpcMessage"))
		{
			RPCMessage rPCMessage;
			if (rpc == 1529322558 && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - AnswerPhone ");
				}
				using (TimeWarning.New("AnswerPhone"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.MaxDistance.Test(1529322558u, "AnswerPhone", this, player, 3f))
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
							AnswerPhone(msg2);
						}
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
						player.Kick("RPC Error in AnswerPhone");
					}
				}
				return true;
			}
			if (rpc == 2754362156u && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - ClearCurrentUser ");
				}
				using (TimeWarning.New("ClearCurrentUser"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.MaxDistance.Test(2754362156u, "ClearCurrentUser", this, player, 9f))
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
							ClearCurrentUser(msg3);
						}
					}
					catch (Exception exception2)
					{
						Debug.LogException(exception2);
						player.Kick("RPC Error in ClearCurrentUser");
					}
				}
				return true;
			}
			if (rpc == 1095090232 && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - InitiateCall ");
				}
				using (TimeWarning.New("InitiateCall"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.MaxDistance.Test(1095090232u, "InitiateCall", this, player, 3f))
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
							RPCMessage msg4 = rPCMessage;
							InitiateCall(msg4);
						}
					}
					catch (Exception exception3)
					{
						Debug.LogException(exception3);
						player.Kick("RPC Error in InitiateCall");
					}
				}
				return true;
			}
			if (rpc == 2606442785u && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - Server_AddSavedNumber ");
				}
				using (TimeWarning.New("Server_AddSavedNumber"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.CallsPerSecond.Test(2606442785u, "Server_AddSavedNumber", this, player, 5uL))
						{
							return true;
						}
						if (!RPC_Server.MaxDistance.Test(2606442785u, "Server_AddSavedNumber", this, player, 3f))
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
							RPCMessage msg5 = rPCMessage;
							Server_AddSavedNumber(msg5);
						}
					}
					catch (Exception exception4)
					{
						Debug.LogException(exception4);
						player.Kick("RPC Error in Server_AddSavedNumber");
					}
				}
				return true;
			}
			if (rpc == 1402406333 && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - Server_RemoveSavedNumber ");
				}
				using (TimeWarning.New("Server_RemoveSavedNumber"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.CallsPerSecond.Test(1402406333u, "Server_RemoveSavedNumber", this, player, 5uL))
						{
							return true;
						}
						if (!RPC_Server.MaxDistance.Test(1402406333u, "Server_RemoveSavedNumber", this, player, 3f))
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
							RPCMessage msg6 = rPCMessage;
							Server_RemoveSavedNumber(msg6);
						}
					}
					catch (Exception exception5)
					{
						Debug.LogException(exception5);
						player.Kick("RPC Error in Server_RemoveSavedNumber");
					}
				}
				return true;
			}
			if (rpc == 942544266 && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - Server_RequestPhoneDirectory ");
				}
				using (TimeWarning.New("Server_RequestPhoneDirectory"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.CallsPerSecond.Test(942544266u, "Server_RequestPhoneDirectory", this, player, 5uL))
						{
							return true;
						}
						if (!RPC_Server.MaxDistance.Test(942544266u, "Server_RequestPhoneDirectory", this, player, 3f))
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
							RPCMessage msg7 = rPCMessage;
							Server_RequestPhoneDirectory(msg7);
						}
					}
					catch (Exception exception6)
					{
						Debug.LogException(exception6);
						player.Kick("RPC Error in Server_RequestPhoneDirectory");
					}
				}
				return true;
			}
			if (rpc == 1221129498 && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - ServerHangUp ");
				}
				using (TimeWarning.New("ServerHangUp"))
				{
					try
					{
						using (TimeWarning.New("Call"))
						{
							rPCMessage = default(RPCMessage);
							rPCMessage.connection = msg.connection;
							rPCMessage.player = player;
							rPCMessage.read = msg.read;
							RPCMessage msg8 = rPCMessage;
							ServerHangUp(msg8);
						}
					}
					catch (Exception exception7)
					{
						Debug.LogException(exception7);
						player.Kick("RPC Error in ServerHangUp");
					}
				}
				return true;
			}
			if (rpc == 3900772076u && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - SetCurrentUser ");
				}
				using (TimeWarning.New("SetCurrentUser"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.MaxDistance.Test(3900772076u, "SetCurrentUser", this, player, 3f))
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
							RPCMessage currentUser = rPCMessage;
							SetCurrentUser(currentUser);
						}
					}
					catch (Exception exception8)
					{
						Debug.LogException(exception8);
						player.Kick("RPC Error in SetCurrentUser");
					}
				}
				return true;
			}
			if (rpc == 2760249627u && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - UpdatePhoneName ");
				}
				using (TimeWarning.New("UpdatePhoneName"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.CallsPerSecond.Test(2760249627u, "UpdatePhoneName", this, player, 5uL))
						{
							return true;
						}
						if (!RPC_Server.MaxDistance.Test(2760249627u, "UpdatePhoneName", this, player, 3f))
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
							RPCMessage msg9 = rPCMessage;
							UpdatePhoneName(msg9);
						}
					}
					catch (Exception exception9)
					{
						Debug.LogException(exception9);
						player.Kick("RPC Error in UpdatePhoneName");
					}
				}
				return true;
			}
		}
		return base.OnRpcMessage(player, rpc, msg);
	}

	public override void Save(SaveInfo info)
	{
		base.Save(info);
		if (info.msg.telephone == null)
		{
			info.msg.telephone = Facepunch.Pool.Get<ProtoBuf.Telephone>();
		}
		info.msg.telephone.phoneNumber = Controller.PhoneNumber;
		info.msg.telephone.phoneName = Controller.PhoneName;
		info.msg.telephone.lastNumber = Controller.lastDialedNumber;
		info.msg.telephone.savedNumbers = Controller.savedNumbers;
		if (!info.forDisk)
		{
			info.msg.telephone.usingPlayer = Controller.currentPlayerRef.uid;
		}
	}

	public override void ServerInit()
	{
		base.ServerInit();
		Controller.ServerInit();
		ItemContainer inventory = base.inventory;
		inventory.canAcceptItem = (Func<Item, int, bool>)Delegate.Combine(inventory.canAcceptItem, new Func<Item, int, bool>(CanAcceptItem));
	}

	public override void PostServerLoad()
	{
		base.PostServerLoad();
		Controller.PostServerLoad();
	}

	internal override void DoServerDestroy()
	{
		base.DoServerDestroy();
		Controller.DoServerDestroy();
	}

	[RPC_Server]
	[RPC_Server.MaxDistance(9f)]
	public void ClearCurrentUser(RPCMessage msg)
	{
		Controller.ClearCurrentUser(msg);
	}

	[RPC_Server]
	[RPC_Server.MaxDistance(3f)]
	public void SetCurrentUser(RPCMessage msg)
	{
		Controller.SetCurrentUser(msg);
	}

	[RPC_Server.MaxDistance(3f)]
	[RPC_Server]
	public void InitiateCall(RPCMessage msg)
	{
		Controller.InitiateCall(msg);
	}

	[RPC_Server]
	[RPC_Server.MaxDistance(3f)]
	public void AnswerPhone(RPCMessage msg)
	{
		Controller.AnswerPhone(msg);
	}

	[RPC_Server]
	private void ServerHangUp(RPCMessage msg)
	{
		Controller.ServerHangUp(msg);
	}

	private bool CanAcceptItem(Item item, int targetSlot)
	{
		ItemDefinition[] validCassettes = ValidCassettes;
		for (int i = 0; i < validCassettes.Length; i++)
		{
			if (validCassettes[i] == item.info)
			{
				return true;
			}
		}
		return false;
	}

	public override void DestroyShared()
	{
		base.DestroyShared();
		Controller.DestroyShared();
	}

	[RPC_Server]
	[RPC_Server.MaxDistance(3f)]
	[RPC_Server.CallsPerSecond(5uL)]
	public void UpdatePhoneName(RPCMessage msg)
	{
		Controller.UpdatePhoneName(msg);
	}

	[RPC_Server]
	[RPC_Server.MaxDistance(3f)]
	[RPC_Server.CallsPerSecond(5uL)]
	public void Server_RequestPhoneDirectory(RPCMessage msg)
	{
		Controller.Server_RequestPhoneDirectory(msg);
	}

	[RPC_Server]
	[RPC_Server.CallsPerSecond(5uL)]
	[RPC_Server.MaxDistance(3f)]
	public void Server_AddSavedNumber(RPCMessage msg)
	{
		Controller.Server_AddSavedNumber(msg);
	}

	[RPC_Server]
	[RPC_Server.MaxDistance(3f)]
	[RPC_Server.CallsPerSecond(5uL)]
	public void Server_RemoveSavedNumber(RPCMessage msg)
	{
		Controller.Server_RemoveSavedNumber(msg);
	}

	private void WatchForDisconnects()
	{
		bool flag = false;
		if (Controller.currentPlayer != null)
		{
			if (Controller.currentPlayer.IsSleeping())
			{
				flag = true;
			}
			if (Controller.currentPlayer.IsDead())
			{
				flag = true;
			}
			if (Vector3.Distance(base.transform.position, Controller.currentPlayer.transform.position) > 5f)
			{
				flag = true;
			}
		}
		else
		{
			flag = true;
		}
		if (flag)
		{
			Controller.ServerHangUp();
			Controller.ClearCurrentUser();
		}
	}

	public override int GetPassthroughAmount(int outputSlot = 0)
	{
		if (Controller.serverState == CallState.Ringing || Controller.serverState == CallState.InProcess)
		{
			return base.GetPassthroughAmount(outputSlot);
		}
		return 0;
	}

	public override void Load(LoadInfo info)
	{
		base.Load(info);
		if (info.msg?.telephone != null)
		{
			Controller.PhoneNumber = info.msg.telephone.phoneNumber;
			Controller.PhoneName = info.msg.telephone.phoneName;
			Controller.lastDialedNumber = info.msg.telephone.lastNumber;
			if (!info.fromDisk)
			{
				Controller.currentPlayerRef.uid = info.msg.telephone.usingPlayer;
			}
			Controller.savedNumbers?.ResetToPool();
			Controller.savedNumbers = info.msg.telephone.savedNumbers;
			if (Controller.savedNumbers != null)
			{
				Controller.savedNumbers.ShouldPool = false;
			}
			if (info.fromDisk)
			{
				SetFlag(Flags.Busy, false);
			}
		}
	}

	public override bool CanPickup(BasePlayer player)
	{
		if (!base.CanPickup(player))
		{
			return false;
		}
		return Controller.currentPlayer == null;
	}

	public override void OnFlagsChanged(Flags old, Flags next)
	{
		base.OnFlagsChanged(old, next);
		if (base.isServer)
		{
			if (Controller.RequirePower && next.HasFlag(Flags.Busy) && !next.HasFlag(Flags.Reserved8))
			{
				Controller.ServerHangUp();
			}
			if (old.HasFlag(Flags.Busy) != next.HasFlag(Flags.Busy))
			{
				if (next.HasFlag(Flags.Busy))
				{
					if (!IsInvoking(WatchForDisconnects))
					{
						InvokeRepeating(WatchForDisconnects, 0f, 0.1f);
					}
				}
				else
				{
					CancelInvoke(WatchForDisconnects);
				}
			}
		}
		Controller.OnFlagsChanged(old, next);
	}
}
