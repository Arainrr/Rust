#define UNITY_ASSERTIONS
using ConVar;
using Network;
using Oxide.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class NPCTalking : NPCShopKeeper, IConversationProvider
{
	[Serializable]
	public class NPCConversationResultAction
	{
		public string action;

		public int scrapCost;

		public string broadcastMessage;

		public float broadcastRange;
	}

	public ConversationData[] conversations;

	public NPCConversationResultAction[] conversationResultActions;

	[NonSerialized]
	public float maxConversationDistance = 5f;

	public List<BasePlayer> conversingPlayers = new List<BasePlayer>();

	public BasePlayer lastActionPlayer;

	public override bool OnRpcMessage(BasePlayer player, uint rpc, Message msg)
	{
		using (TimeWarning.New("NPCTalking.OnRpcMessage"))
		{
			RPCMessage rPCMessage;
			if (rpc == 4224060672u && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - ConversationAction ");
				}
				using (TimeWarning.New("ConversationAction"))
				{
					try
					{
						using (TimeWarning.New("Call"))
						{
							rPCMessage = default(RPCMessage);
							rPCMessage.connection = msg.connection;
							rPCMessage.player = player;
							rPCMessage.read = msg.read;
							RPCMessage msg2 = rPCMessage;
							ConversationAction(msg2);
						}
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
						player.Kick("RPC Error in ConversationAction");
					}
				}
				return true;
			}
			if (rpc == 2112414875 && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - Server_BeginTalking ");
				}
				using (TimeWarning.New("Server_BeginTalking"))
				{
					try
					{
						using (TimeWarning.New("Call"))
						{
							rPCMessage = default(RPCMessage);
							rPCMessage.connection = msg.connection;
							rPCMessage.player = player;
							rPCMessage.read = msg.read;
							RPCMessage msg3 = rPCMessage;
							Server_BeginTalking(msg3);
						}
					}
					catch (Exception exception2)
					{
						Debug.LogException(exception2);
						player.Kick("RPC Error in Server_BeginTalking");
					}
				}
				return true;
			}
			if (rpc == 1597539152 && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - Server_EndTalking ");
				}
				using (TimeWarning.New("Server_EndTalking"))
				{
					try
					{
						using (TimeWarning.New("Call"))
						{
							rPCMessage = default(RPCMessage);
							rPCMessage.connection = msg.connection;
							rPCMessage.player = player;
							rPCMessage.read = msg.read;
							RPCMessage msg4 = rPCMessage;
							Server_EndTalking(msg4);
						}
					}
					catch (Exception exception3)
					{
						Debug.LogException(exception3);
						player.Kick("RPC Error in Server_EndTalking");
					}
				}
				return true;
			}
			if (rpc == 2713250658u && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - Server_ResponsePressed ");
				}
				using (TimeWarning.New("Server_ResponsePressed"))
				{
					try
					{
						using (TimeWarning.New("Call"))
						{
							rPCMessage = default(RPCMessage);
							rPCMessage.connection = msg.connection;
							rPCMessage.player = player;
							rPCMessage.read = msg.read;
							RPCMessage msg5 = rPCMessage;
							Server_ResponsePressed(msg5);
						}
					}
					catch (Exception exception4)
					{
						Debug.LogException(exception4);
						player.Kick("RPC Error in Server_ResponsePressed");
					}
				}
				return true;
			}
		}
		return base.OnRpcMessage(player, rpc, msg);
	}

	public int GetConversationIndex(string conversationName)
	{
		for (int i = 0; i < conversations.Length; i++)
		{
			if (conversations[i].shortname == conversationName)
			{
				return i;
			}
		}
		return -1;
	}

	public virtual string GetConversationStartSpeech()
	{
		return "intro";
	}

	public ConversationData GetConversation(string conversationName)
	{
		return GetConversation(GetConversationIndex(conversationName));
	}

	public ConversationData GetConversation(int index)
	{
		return conversations[index];
	}

	public virtual ConversationData GetConversationFor(BasePlayer player)
	{
		return conversations[0];
	}

	public bool ProviderBusy()
	{
		return HasFlag(Flags.Reserved1);
	}

	public void ForceEndConversation(BasePlayer player)
	{
		ClientRPCPlayer(null, player, "Client_EndConversation");
	}

	public void ForceSpeechNode(BasePlayer player, int speechNodeIndex)
	{
		if (!(player == null))
		{
			ClientRPCPlayer(null, player, "Client_ForceSpeechNode", speechNodeIndex);
		}
	}

	public void OnConversationEnded(BasePlayer player)
	{
		if (conversingPlayers.Contains(player))
		{
			conversingPlayers.Remove(player);
		}
	}

	public void CleanupConversingPlayers()
	{
		for (int num = conversingPlayers.Count - 1; num >= 0; num--)
		{
			BasePlayer basePlayer = conversingPlayers[num];
			if (basePlayer == null || !basePlayer.IsAlive() || basePlayer.IsSleeping())
			{
				conversingPlayers.RemoveAt(num);
			}
		}
	}

	[RPC_Server]
	public void Server_BeginTalking(RPCMessage msg)
	{
		BasePlayer player = msg.player;
		CleanupConversingPlayers();
		ConversationData conversationFor = GetConversationFor(player);
		if (conversationFor != null)
		{
			if (conversingPlayers.Contains(player))
			{
				OnConversationEnded(player);
			}
			if (Interface.CallHook("OnNpcConversationStart", this, player, conversationFor) == null)
			{
				conversingPlayers.Add(player);
				UpdateFlags();
				ClientRPCPlayer(null, player, "Client_StartConversation", GetConversationIndex(conversationFor.shortname), GetConversationStartSpeech());
			}
		}
	}

	public virtual void UpdateFlags()
	{
	}

	[RPC_Server]
	public void Server_EndTalking(RPCMessage msg)
	{
		OnConversationEnded(msg.player);
	}

	[RPC_Server]
	public void ConversationAction(RPCMessage msg)
	{
		BasePlayer player = msg.player;
		string action = msg.read.String();
		OnConversationAction(player, action);
	}

	public bool ValidConversationPlayer(BasePlayer player)
	{
		if (Vector3.Distance(player.transform.position, base.transform.position) > maxConversationDistance)
		{
			return false;
		}
		if (conversingPlayers.Contains(player))
		{
			return false;
		}
		return true;
	}

	[RPC_Server]
	public void Server_ResponsePressed(RPCMessage msg)
	{
		BasePlayer player = msg.player;
		int num = msg.read.Int32();
		int num2 = msg.read.Int32();
		ConversationData conversationFor = GetConversationFor(player);
		if (conversationFor == null)
		{
			return;
		}
		ConversationData.ResponseNode responseNode = conversationFor.speeches[num].responses[num2];
		if (responseNode != null && Interface.CallHook("OnNpcConversationRespond", this, player, conversationFor, responseNode) == null)
		{
			if (responseNode.conditions.Length != 0)
			{
				UpdateFlags();
			}
			bool flag = responseNode.PassesConditions(player, this);
			if (flag && !string.IsNullOrEmpty(responseNode.actionString))
			{
				OnConversationAction(player, responseNode.actionString);
			}
			int speechNodeIndex = conversationFor.GetSpeechNodeIndex(flag ? responseNode.resultingSpeechNode : responseNode.GetFailedSpeechNode(player, this));
			if (speechNodeIndex == -1)
			{
				ForceEndConversation(player);
				return;
			}
			ForceSpeechNode(player, speechNodeIndex);
			Interface.CallHook("OnNpcConversationResponded", this, player, conversationFor, responseNode);
		}
	}

	public BasePlayer GetActionPlayer()
	{
		return lastActionPlayer;
	}

	public virtual void OnConversationAction(BasePlayer player, string action)
	{
		if (action == "openvending")
		{
			InvisibleVendingMachine vendingMachine = GetVendingMachine();
			if (vendingMachine != null && Vector3.Distance(player.transform.position, base.transform.position) < 5f)
			{
				ForceEndConversation(player);
				vendingMachine.PlayerOpenLoot(player, "vendingmachine.customer", false);
				return;
			}
		}
		ItemDefinition itemDefinition = ItemManager.FindItemDefinition("scrap");
		NPCConversationResultAction[] array = conversationResultActions;
		int num = 0;
		NPCConversationResultAction nPCConversationResultAction;
		while (true)
		{
			if (num < array.Length)
			{
				nPCConversationResultAction = array[num];
				if (nPCConversationResultAction.action == action)
				{
					break;
				}
				num++;
				continue;
			}
			return;
		}
		CleanupConversingPlayers();
		foreach (BasePlayer conversingPlayer in conversingPlayers)
		{
			if (!(conversingPlayer == player) && !(conversingPlayer == null))
			{
				int speechNodeIndex = GetConversationFor(player).GetSpeechNodeIndex("startbusy");
				ForceSpeechNode(conversingPlayer, speechNodeIndex);
			}
		}
		int num2 = nPCConversationResultAction.scrapCost;
		List<Item> list = player.inventory.FindItemIDs(itemDefinition.itemid);
		foreach (Item item in list)
		{
			num2 -= item.amount;
		}
		if (num2 > 0)
		{
			int speechNodeIndex2 = GetConversationFor(player).GetSpeechNodeIndex("toopoor");
			ForceSpeechNode(player, speechNodeIndex2);
			return;
		}
		num2 = nPCConversationResultAction.scrapCost;
		foreach (Item item2 in list)
		{
			int num3 = Mathf.Min(num2, item2.amount);
			item2.UseItem(num3);
			num2 -= num3;
			if (num2 <= 0)
			{
				break;
			}
		}
		lastActionPlayer = player;
		BroadcastEntityMessage(nPCConversationResultAction.broadcastMessage, nPCConversationResultAction.broadcastRange);
		lastActionPlayer = null;
	}
}
