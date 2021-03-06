#define UNITY_ASSERTIONS
using ConVar;
using Facepunch;
using Network;
using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class WireTool : HeldEntity
{
	public struct PendingPlug_t
	{
		public IOEntity ent;

		public bool input;

		public int index;

		public GameObject tempLine;
	}

	public Sprite InputSprite;

	public Sprite OutputSprite;

	public Sprite ClearSprite;

	public static float maxWireLength = 30f;

	private const int maxLineNodes = 16;

	public GameObjectRef plugEffect;

	public GameObjectRef ioLine;

	public IOEntity.IOType wireType;

	public PendingPlug_t pending;

	public override bool OnRpcMessage(BasePlayer player, uint rpc, Message msg)
	{
		using (TimeWarning.New("WireTool.OnRpcMessage"))
		{
			RPCMessage rPCMessage;
			if (rpc == 678101026 && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - AddLine ");
				}
				using (TimeWarning.New("AddLine"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.IsActiveItem.Test(678101026u, "AddLine", this, player))
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
							AddLine(msg2);
						}
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
						player.Kick("RPC Error in AddLine");
					}
				}
				return true;
			}
			if (rpc == 40328523 && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - MakeConnection ");
				}
				using (TimeWarning.New("MakeConnection"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.IsActiveItem.Test(40328523u, "MakeConnection", this, player))
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
							MakeConnection(msg3);
						}
					}
					catch (Exception exception2)
					{
						Debug.LogException(exception2);
						player.Kick("RPC Error in MakeConnection");
					}
				}
				return true;
			}
			if (rpc == 2469840259u && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - RequestClear ");
				}
				using (TimeWarning.New("RequestClear"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.IsActiveItem.Test(2469840259u, "RequestClear", this, player))
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
							RequestClear(msg4);
						}
					}
					catch (Exception exception3)
					{
						Debug.LogException(exception3);
						player.Kick("RPC Error in RequestClear");
					}
				}
				return true;
			}
			if (rpc == 2596458392u && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - SetPlugged ");
				}
				using (TimeWarning.New("SetPlugged"))
				{
					try
					{
						using (TimeWarning.New("Call"))
						{
							rPCMessage = default(RPCMessage);
							rPCMessage.connection = msg.connection;
							rPCMessage.player = player;
							rPCMessage.read = msg.read;
							RPCMessage plugged = rPCMessage;
							SetPlugged(plugged);
						}
					}
					catch (Exception exception4)
					{
						Debug.LogException(exception4);
						player.Kick("RPC Error in SetPlugged");
					}
				}
				return true;
			}
			if (rpc == 210386477 && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - TryClear ");
				}
				using (TimeWarning.New("TryClear"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.IsActiveItem.Test(210386477u, "TryClear", this, player))
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
							TryClear(msg5);
						}
					}
					catch (Exception exception5)
					{
						Debug.LogException(exception5);
						player.Kick("RPC Error in TryClear");
					}
				}
				return true;
			}
		}
		return base.OnRpcMessage(player, rpc, msg);
	}

	public void ClearPendingPlug()
	{
		pending.ent = null;
		pending.index = -1;
	}

	public bool HasPendingPlug()
	{
		if (pending.ent != null)
		{
			return pending.index != -1;
		}
		return false;
	}

	public bool PendingPlugIsInput()
	{
		if (pending.ent != null && pending.index != -1)
		{
			return pending.input;
		}
		return false;
	}

	public bool PendingPlugIsType(IOEntity.IOType type)
	{
		if (pending.ent != null && pending.index != -1)
		{
			if (!pending.input || pending.ent.inputs[pending.index].type != type)
			{
				if (!pending.input)
				{
					return pending.ent.outputs[pending.index].type == type;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public bool PendingPlugIsOutput()
	{
		if (pending.ent != null && pending.index != -1)
		{
			return !pending.input;
		}
		return false;
	}

	public Vector3 PendingPlugWorldPos()
	{
		if (pending.ent == null || pending.index == -1)
		{
			return Vector3.zero;
		}
		if (pending.input)
		{
			return pending.ent.transform.TransformPoint(pending.ent.inputs[pending.index].handlePosition);
		}
		return pending.ent.transform.TransformPoint(pending.ent.outputs[pending.index].handlePosition);
	}

	public static bool CanPlayerUseWires(BasePlayer player)
	{
		object obj = Interface.CallHook("CanUseWires", player);
		if (obj is bool)
		{
			return (bool)obj;
		}
		if (!player.CanBuild())
		{
			return false;
		}
		List<Collider> obj2 = Facepunch.Pool.GetList<Collider>();
		GamePhysics.OverlapSphere(player.eyes.position, 0.1f, obj2, 536870912, QueryTriggerInteraction.Collide);
		bool result = obj2.All((Collider collider) => collider.gameObject.CompareTag("IgnoreWireCheck"));
		Facepunch.Pool.FreeList(ref obj2);
		return result;
	}

	public static bool CanModifyEntity(BasePlayer player, BaseEntity ent)
	{
		return player.CanBuild(ent.transform.position, ent.transform.rotation, ent.bounds);
	}

	public bool PendingPlugRoot()
	{
		if (pending.ent != null)
		{
			return pending.ent.IsRootEntity();
		}
		return false;
	}

	[RPC_Server]
	[RPC_Server.IsActiveItem]
	public void TryClear(RPCMessage msg)
	{
		BasePlayer player = msg.player;
		uint uid = msg.read.UInt32();
		BaseNetworkable baseNetworkable = BaseNetworkable.serverEntities.Find(uid);
		IOEntity iOEntity = (baseNetworkable == null) ? null : baseNetworkable.GetComponent<IOEntity>();
		if (!(iOEntity == null) && CanPlayerUseWires(player) && CanModifyEntity(player, iOEntity))
		{
			iOEntity.ClearConnections();
			iOEntity.SendNetworkUpdate();
		}
	}

	[RPC_Server]
	[RPC_Server.IsActiveItem]
	public void MakeConnection(RPCMessage msg)
	{
		BasePlayer player = msg.player;
		if (!CanPlayerUseWires(player))
		{
			return;
		}
		uint uid = msg.read.UInt32();
		int num = msg.read.Int32();
		uint uid2 = msg.read.UInt32();
		int num2 = msg.read.Int32();
		BaseNetworkable baseNetworkable = BaseNetworkable.serverEntities.Find(uid);
		IOEntity iOEntity = (baseNetworkable == null) ? null : baseNetworkable.GetComponent<IOEntity>();
		if (iOEntity == null)
		{
			return;
		}
		BaseNetworkable baseNetworkable2 = BaseNetworkable.serverEntities.Find(uid2);
		IOEntity iOEntity2 = (baseNetworkable2 == null) ? null : baseNetworkable2.GetComponent<IOEntity>();
		if (!(iOEntity2 == null) && !(Vector3.Distance(baseNetworkable2.transform.position, baseNetworkable.transform.position) > maxWireLength) && num < iOEntity.inputs.Length && num2 < iOEntity2.outputs.Length && !(iOEntity.inputs[num].connectedTo.Get() != null) && !(iOEntity2.outputs[num2].connectedTo.Get() != null) && (!iOEntity.inputs[num].rootConnectionsOnly || iOEntity2.IsRootEntity()) && CanModifyEntity(player, iOEntity) && CanModifyEntity(player, iOEntity2))
		{
			if (Interface.CallHook("OnWireConnect", msg.player, iOEntity, num, iOEntity2, num2) != null)
			{
				iOEntity2.outputs[num2].linePoints = null;
				return;
			}
			iOEntity.inputs[num].connectedTo.Set(iOEntity2);
			iOEntity.inputs[num].connectedToSlot = num2;
			iOEntity.inputs[num].connectedTo.Init();
			iOEntity2.outputs[num2].connectedTo.Set(iOEntity);
			iOEntity2.outputs[num2].connectedToSlot = num;
			iOEntity2.outputs[num2].connectedTo.Init();
			iOEntity2.MarkDirtyForceUpdateOutputs();
			iOEntity2.SendNetworkUpdate();
			iOEntity.SendNetworkUpdate();
			iOEntity2.SendChangedToRoot(true);
		}
	}

	[RPC_Server]
	public void SetPlugged(RPCMessage msg)
	{
	}

	[RPC_Server.IsActiveItem]
	[RPC_Server]
	public void RequestClear(RPCMessage msg)
	{
		BasePlayer player = msg.player;
		if (!CanPlayerUseWires(player))
		{
			return;
		}
		uint uid = msg.read.UInt32();
		int num = msg.read.Int32();
		bool flag = msg.read.Bit();
		BaseNetworkable baseNetworkable = BaseNetworkable.serverEntities.Find(uid);
		IOEntity iOEntity = (baseNetworkable == null) ? null : baseNetworkable.GetComponent<IOEntity>();
		if (iOEntity == null || !CanModifyEntity(player, iOEntity) || num >= (flag ? iOEntity.inputs.Length : iOEntity.outputs.Length))
		{
			return;
		}
		IOEntity.IOSlot iOSlot = flag ? iOEntity.inputs[num] : iOEntity.outputs[num];
		if (iOSlot.connectedTo.Get() == null)
		{
			return;
		}
		IOEntity iOEntity2 = iOSlot.connectedTo.Get();
		if (Interface.CallHook("OnWireClear", msg.player, iOEntity, num, iOEntity2, flag) != null)
		{
			return;
		}
		IOEntity.IOSlot obj = flag ? iOEntity2.outputs[iOSlot.connectedToSlot] : iOEntity2.inputs[iOSlot.connectedToSlot];
		if (flag)
		{
			iOEntity.UpdateFromInput(0, num);
		}
		else if ((bool)iOEntity2)
		{
			iOEntity2.UpdateFromInput(0, iOSlot.connectedToSlot);
		}
		iOSlot.Clear();
		obj.Clear();
		iOEntity.MarkDirtyForceUpdateOutputs();
		iOEntity.SendNetworkUpdate();
		if (flag && iOEntity2 != null)
		{
			iOEntity2.SendChangedToRoot(true);
		}
		else if (!flag)
		{
			IOEntity.IOSlot[] inputs = iOEntity.inputs;
			foreach (IOEntity.IOSlot iOSlot2 in inputs)
			{
				if (iOSlot2.mainPowerSlot && (bool)iOSlot2.connectedTo.Get())
				{
					iOSlot2.connectedTo.Get().SendChangedToRoot(true);
				}
			}
		}
		iOEntity2.SendNetworkUpdate();
	}

	[RPC_Server]
	[RPC_Server.IsActiveItem]
	public void AddLine(RPCMessage msg)
	{
		BasePlayer player = msg.player;
		if (!CanPlayerUseWires(player))
		{
			return;
		}
		int num = msg.read.Int32();
		if (num > 18)
		{
			return;
		}
		List<Vector3> list = new List<Vector3>();
		for (int i = 0; i < num; i++)
		{
			Vector3 item = msg.read.Vector3();
			list.Add(item);
		}
		if (!ValidateLine(list))
		{
			return;
		}
		uint uid = msg.read.UInt32();
		int num2 = msg.read.Int32();
		uint uid2 = msg.read.UInt32();
		int num3 = msg.read.Int32();
		BaseNetworkable baseNetworkable = BaseNetworkable.serverEntities.Find(uid);
		IOEntity iOEntity = (baseNetworkable == null) ? null : baseNetworkable.GetComponent<IOEntity>();
		if (!(iOEntity == null))
		{
			BaseNetworkable baseNetworkable2 = BaseNetworkable.serverEntities.Find(uid2);
			IOEntity iOEntity2 = (baseNetworkable2 == null) ? null : baseNetworkable2.GetComponent<IOEntity>();
			if (!(iOEntity2 == null) && num2 < iOEntity.inputs.Length && num3 < iOEntity2.outputs.Length && !(iOEntity.inputs[num2].connectedTo.Get() != null) && !(iOEntity2.outputs[num3].connectedTo.Get() != null) && (!iOEntity.inputs[num2].rootConnectionsOnly || iOEntity2.IsRootEntity()) && CanModifyEntity(player, iOEntity2) && CanModifyEntity(player, iOEntity))
			{
				iOEntity2.outputs[num3].linePoints = list.ToArray();
				iOEntity2.SendNetworkUpdate();
			}
		}
	}

	public bool ValidateLine(List<Vector3> lineList)
	{
		if (lineList.Count < 2)
		{
			return false;
		}
		Vector3 a = lineList[0];
		float num = 0f;
		for (int i = 1; i < lineList.Count; i++)
		{
			Vector3 vector = lineList[i];
			num += Vector3.Distance(a, vector);
			if (num > maxWireLength)
			{
				return false;
			}
			a = vector;
		}
		return true;
	}
}
