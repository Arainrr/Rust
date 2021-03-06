#define UNITY_ASSERTIONS
using ConVar;
using Facepunch;
using Network;
using Oxide.Core;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class SleepingBag : DecayEntity
{
	[NonSerialized]
	public ulong deployerUserID;

	public GameObject renameDialog;

	public GameObject assignDialog;

	public float secondsBetweenReuses = 300f;

	public string niceName = "Unnamed Bag";

	public Vector3 spawnOffset = Vector3.zero;

	public RespawnInformation.SpawnOptions.RespawnType RespawnType = RespawnInformation.SpawnOptions.RespawnType.SleepingBag;

	public bool canBePublic;

	public float unlockTime;

	public static List<SleepingBag> sleepingBags = new List<SleepingBag>();

	public float unlockSeconds
	{
		get
		{
			if (unlockTime < UnityEngine.Time.realtimeSinceStartup)
			{
				return 0f;
			}
			return unlockTime - UnityEngine.Time.realtimeSinceStartup;
		}
	}

	public override bool OnRpcMessage(BasePlayer player, uint rpc, Message msg)
	{
		using (TimeWarning.New("SleepingBag.OnRpcMessage"))
		{
			RPCMessage rPCMessage;
			if (rpc == 3057055788u && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - AssignToFriend ");
				}
				using (TimeWarning.New("AssignToFriend"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.IsVisible.Test(3057055788u, "AssignToFriend", this, player, 3f))
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
							AssignToFriend(msg2);
						}
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
						player.Kick("RPC Error in AssignToFriend");
					}
				}
				return true;
			}
			if (rpc == 1335950295 && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - Rename ");
				}
				using (TimeWarning.New("Rename"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.IsVisible.Test(1335950295u, "Rename", this, player, 3f))
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
							Rename(msg3);
						}
					}
					catch (Exception exception2)
					{
						Debug.LogException(exception2);
						player.Kick("RPC Error in Rename");
					}
				}
				return true;
			}
			if (rpc == 42669546 && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - RPC_MakeBed ");
				}
				using (TimeWarning.New("RPC_MakeBed"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.IsVisible.Test(42669546u, "RPC_MakeBed", this, player, 3f))
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
							RPC_MakeBed(msg4);
						}
					}
					catch (Exception exception3)
					{
						Debug.LogException(exception3);
						player.Kick("RPC Error in RPC_MakeBed");
					}
				}
				return true;
			}
			if (rpc == 393812086 && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - RPC_MakePublic ");
				}
				using (TimeWarning.New("RPC_MakePublic"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.IsVisible.Test(393812086u, "RPC_MakePublic", this, player, 3f))
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
							RPC_MakePublic(msg5);
						}
					}
					catch (Exception exception4)
					{
						Debug.LogException(exception4);
						player.Kick("RPC Error in RPC_MakePublic");
					}
				}
				return true;
			}
		}
		return base.OnRpcMessage(player, rpc, msg);
	}

	public bool IsPublic()
	{
		return HasFlag(Flags.Reserved3);
	}

	public static SleepingBag[] FindForPlayer(ulong playerID, bool ignoreTimers)
	{
		return sleepingBags.Where((SleepingBag x) => x.deployerUserID == playerID && (ignoreTimers || x.unlockTime < UnityEngine.Time.realtimeSinceStartup)).ToArray();
	}

	public static SleepingBag FindForPlayer(ulong playerID, uint sleepingBagID, bool ignoreTimers)
	{
		return sleepingBags.FirstOrDefault((SleepingBag x) => x.deployerUserID == playerID && x.net.ID == sleepingBagID && (ignoreTimers || x.unlockTime < UnityEngine.Time.realtimeSinceStartup));
	}

	public static bool SpawnPlayer(BasePlayer player, uint sleepingBag)
	{
		SleepingBag[] array = FindForPlayer(player.userID, true);
		SleepingBag sleepingBag2 = array.FirstOrDefault((SleepingBag x) => x.deployerUserID == player.userID && x.net.ID == sleepingBag && x.unlockTime < UnityEngine.Time.realtimeSinceStartup);
		if (sleepingBag2 == null)
		{
			return false;
		}
		object obj = Interface.CallHook("OnPlayerRespawn", player, sleepingBag2);
		if (obj is SleepingBag)
		{
			sleepingBag2 = (SleepingBag)obj;
		}
		Vector3 vector = sleepingBag2.transform.position + sleepingBag2.spawnOffset;
		Quaternion rotation = Quaternion.Euler(0f, sleepingBag2.transform.rotation.eulerAngles.y, 0f);
		player.RespawnAt(vector, rotation);
		SleepingBag[] array2 = array;
		foreach (SleepingBag sleepingBag3 in array2)
		{
			if (Vector3.Distance(vector, sleepingBag3.transform.position) <= ConVar.Server.respawnresetrange)
			{
				sleepingBag3.unlockTime = UnityEngine.Time.realtimeSinceStartup + sleepingBag3.secondsBetweenReuses;
			}
		}
		return true;
	}

	public static bool DestroyBag(BasePlayer player, uint sleepingBag)
	{
		SleepingBag sleepingBag2 = FindForPlayer(player.userID, sleepingBag, false);
		if (sleepingBag2 == null)
		{
			return false;
		}
		if (Interface.CallHook("OnSleepingBagDestroy", sleepingBag2, player) != null)
		{
			return false;
		}
		if (sleepingBag2.canBePublic)
		{
			sleepingBag2.SetPublic(true);
			sleepingBag2.deployerUserID = 0uL;
		}
		else
		{
			sleepingBag2.Kill();
		}
		player.SendRespawnOptions();
		Interface.CallHook("OnSleepingBagDestroyed", sleepingBag2, player);
		return true;
	}

	public void SetPublic(bool isPublic)
	{
		SetFlag(Flags.Reserved3, isPublic);
	}

	private void SetDeployedBy(BasePlayer player)
	{
		if (player == null)
		{
			return;
		}
		deployerUserID = player.userID;
		float realtimeSinceStartup = UnityEngine.Time.realtimeSinceStartup;
		SleepingBag[] array = sleepingBags.Where((SleepingBag x) => x.deployerUserID == player.userID && x.unlockTime > UnityEngine.Time.realtimeSinceStartup).ToArray();
		foreach (SleepingBag sleepingBag in array)
		{
			if (sleepingBag.unlockTime > realtimeSinceStartup && Vector3.Distance(sleepingBag.transform.position, base.transform.position) <= ConVar.Server.respawnresetrange)
			{
				realtimeSinceStartup = sleepingBag.unlockTime;
			}
		}
		unlockTime = Mathf.Max(realtimeSinceStartup, UnityEngine.Time.realtimeSinceStartup + secondsBetweenReuses);
		SendNetworkUpdate();
	}

	public override void ServerInit()
	{
		base.ServerInit();
		if (!sleepingBags.Contains(this))
		{
			sleepingBags.Add(this);
		}
	}

	internal override void DoServerDestroy()
	{
		base.DoServerDestroy();
		sleepingBags.RemoveAll((SleepingBag x) => x == this);
	}

	public override void Save(SaveInfo info)
	{
		base.Save(info);
		info.msg.sleepingBag = Facepunch.Pool.Get<ProtoBuf.SleepingBag>();
		info.msg.sleepingBag.name = niceName;
		info.msg.sleepingBag.deployerID = deployerUserID;
	}

	[RPC_Server]
	[RPC_Server.IsVisible(3f)]
	public void Rename(RPCMessage msg)
	{
		if (!msg.player.CanInteract())
		{
			return;
		}
		string text = msg.read.String();
		if (Interface.CallHook("CanRenameBed", msg.player, this, text) == null)
		{
			text = WordFilter.Filter(text);
			if (string.IsNullOrEmpty(text))
			{
				text = "Unnamed Sleeping Bag";
			}
			if (text.Length > 24)
			{
				text = text.Substring(0, 22) + "..";
			}
			niceName = text;
			SendNetworkUpdate();
		}
	}

	[RPC_Server]
	[RPC_Server.IsVisible(3f)]
	public void AssignToFriend(RPCMessage msg)
	{
		if (msg.player.CanInteract() && deployerUserID == msg.player.userID)
		{
			ulong num = msg.read.UInt64();
			if (num != 0L && Interface.CallHook("CanAssignBed", msg.player, this, num) == null)
			{
				deployerUserID = num;
				SendNetworkUpdate();
			}
		}
	}

	[RPC_Server.IsVisible(3f)]
	[RPC_Server]
	public void RPC_MakePublic(RPCMessage msg)
	{
		if (!canBePublic || !msg.player.CanInteract() || (deployerUserID != msg.player.userID && !msg.player.CanBuild()))
		{
			return;
		}
		bool flag = msg.read.Bit();
		if (flag != IsPublic() && Interface.CallHook("CanSetBedPublic", msg.player, this) == null)
		{
			SetPublic(flag);
			if (!IsPublic())
			{
				deployerUserID = msg.player.userID;
			}
			SendNetworkUpdate();
		}
	}

	[RPC_Server]
	[RPC_Server.IsVisible(3f)]
	public void RPC_MakeBed(RPCMessage msg)
	{
		if (canBePublic && IsPublic() && msg.player.CanInteract())
		{
			deployerUserID = msg.player.userID;
			SendNetworkUpdate();
		}
	}

	public override void Load(LoadInfo info)
	{
		base.Load(info);
		if (info.msg.sleepingBag != null)
		{
			niceName = info.msg.sleepingBag.name;
			deployerUserID = info.msg.sleepingBag.deployerID;
		}
	}

	public override bool CanPickup(BasePlayer player)
	{
		if (base.CanPickup(player))
		{
			return player.userID == deployerUserID;
		}
		return false;
	}
}
