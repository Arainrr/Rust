#define UNITY_ASSERTIONS
using ConVar;
using Facepunch;
using Network;
using Oxide.Core;
using ProtoBuf;
using System;
using UnityEngine;
using UnityEngine.Assertions;

public class Signage : BaseCombatEntity, ILOD
{
	public GameObjectRef changeTextDialog;

	public MeshPaintableSource paintableSource;

	[NonSerialized]
	public uint textureID;

	public override bool OnRpcMessage(BasePlayer player, uint rpc, Message msg)
	{
		using (TimeWarning.New("Signage.OnRpcMessage"))
		{
			RPCMessage rPCMessage;
			if (rpc == 1455609404 && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - LockSign ");
				}
				using (TimeWarning.New("LockSign"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.MaxDistance.Test(1455609404u, "LockSign", this, player, 3f))
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
							LockSign(msg2);
						}
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
						player.Kick("RPC Error in LockSign");
					}
				}
				return true;
			}
			if (rpc == 4149904254u && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - UnLockSign ");
				}
				using (TimeWarning.New("UnLockSign"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.MaxDistance.Test(4149904254u, "UnLockSign", this, player, 3f))
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
							UnLockSign(msg3);
						}
					}
					catch (Exception exception2)
					{
						Debug.LogException(exception2);
						player.Kick("RPC Error in UnLockSign");
					}
				}
				return true;
			}
			if (rpc == 1255380462 && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - UpdateSign ");
				}
				using (TimeWarning.New("UpdateSign"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.MaxDistance.Test(1255380462u, "UpdateSign", this, player, 3f))
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
							UpdateSign(msg4);
						}
					}
					catch (Exception exception3)
					{
						Debug.LogException(exception3);
						player.Kick("RPC Error in UpdateSign");
					}
				}
				return true;
			}
		}
		return base.OnRpcMessage(player, rpc, msg);
	}

	[RPC_Server]
	[RPC_Server.MaxDistance(3f)]
	public void UpdateSign(RPCMessage msg)
	{
		if (msg.player == null || UnityEngine.Time.realtimeSinceStartup - msg.player.lastSignUpdate < ConVar.AntiHack.signpause)
		{
			return;
		}
		msg.player.lastSignUpdate = UnityEngine.Time.realtimeSinceStartup;
		if (CanUpdateSign(msg.player))
		{
			byte[] array = msg.read.BytesWithSize();
			if (array != null && ImageProcessing.IsValidPNG(array, 1024, 1024))
			{
				Interface.CallHook("OnSignUpdated", this, msg.player);
				FileStorage.server.RemoveAllByEntity(net.ID);
				textureID = FileStorage.server.Store(array, FileStorage.Type.png, net.ID);
				SendNetworkUpdate();
			}
		}
	}

	public virtual bool CanUpdateSign(BasePlayer player)
	{
		object obj = Interface.CallHook("CanUpdateSign", player, this);
		if (obj is bool)
		{
			return (bool)obj;
		}
		if (player.IsAdmin || player.IsDeveloper)
		{
			return true;
		}
		if (!player.CanBuild())
		{
			return false;
		}
		if (IsLocked())
		{
			return player.userID == base.OwnerID;
		}
		return true;
	}

	public bool CanUnlockSign(BasePlayer player)
	{
		if (!IsLocked())
		{
			return false;
		}
		return CanUpdateSign(player);
	}

	public bool CanLockSign(BasePlayer player)
	{
		if (IsLocked())
		{
			return false;
		}
		return CanUpdateSign(player);
	}

	public override void Load(LoadInfo info)
	{
		base.Load(info);
		if (info.msg.sign != null && info.msg.sign.imageid != textureID)
		{
			textureID = info.msg.sign.imageid;
		}
		if (base.isServer)
		{
			if (textureID != 0 && FileStorage.server.Get(textureID, FileStorage.Type.png, net.ID) == null)
			{
				textureID = 0u;
			}
			if (textureID == 0)
			{
				SetFlag(Flags.Locked, false);
			}
		}
	}

	[RPC_Server]
	[RPC_Server.MaxDistance(3f)]
	public void LockSign(RPCMessage msg)
	{
		if (msg.player.CanInteract() && CanUpdateSign(msg.player))
		{
			SetFlag(Flags.Locked, true);
			SendNetworkUpdate();
			base.OwnerID = msg.player.userID;
			Interface.CallHook("OnSignLocked", this, msg.player);
		}
	}

	[RPC_Server]
	[RPC_Server.MaxDistance(3f)]
	public void UnLockSign(RPCMessage msg)
	{
		if (msg.player.CanInteract() && CanUnlockSign(msg.player))
		{
			SetFlag(Flags.Locked, false);
			SendNetworkUpdate();
		}
	}

	public override void Save(SaveInfo info)
	{
		base.Save(info);
		info.msg.sign = Facepunch.Pool.Get<Sign>();
		info.msg.sign.imageid = textureID;
	}

	public override void OnKilled(HitInfo info)
	{
		if (net != null)
		{
			FileStorage.server.RemoveAllByEntity(net.ID);
		}
		textureID = 0u;
		base.OnKilled(info);
	}

	public override bool ShouldNetworkOwnerInfo()
	{
		return true;
	}

	public override string Categorize()
	{
		return "sign";
	}
}
