#define UNITY_ASSERTIONS
using ConVar;
using Facepunch;
using Network;
using Oxide.Core;
using ProtoBuf;
using System;
using UnityEngine;
using UnityEngine.Assertions;

public class ContainerIOEntity : IOEntity, IItemContainerEntity
{
	public ItemDefinition onlyAllowedItem;

	public ItemContainer.ContentsType allowedContents = ItemContainer.ContentsType.Generic;

	public int maxStackSize = 1;

	public int numSlots;

	public string lootPanelName = "generic";

	public bool needsBuildingPrivilegeToUse;

	public bool isLootable = true;

	public float dropChance;

	public bool onlyOneUser;

	public ItemContainer inventory
	{
		get;
		private set;
	}

	public override bool OnRpcMessage(BasePlayer player, uint rpc, Message msg)
	{
		using (TimeWarning.New("ContainerIOEntity.OnRpcMessage"))
		{
			if (rpc == 331989034 && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - RPC_OpenLoot ");
				}
				using (TimeWarning.New("RPC_OpenLoot"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.IsVisible.Test(331989034u, "RPC_OpenLoot", this, player, 3f))
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
							RPCMessage rpc2 = rPCMessage;
							RPC_OpenLoot(rpc2);
						}
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
						player.Kick("RPC Error in RPC_OpenLoot");
					}
				}
				return true;
			}
		}
		return base.OnRpcMessage(player, rpc, msg);
	}

	public override bool CanPickup(BasePlayer player)
	{
		if (!pickup.requireEmptyInv || inventory == null || inventory.itemList.Count == 0)
		{
			return base.CanPickup(player);
		}
		return false;
	}

	public override void ServerInit()
	{
		if (inventory == null)
		{
			CreateInventory(true);
			OnInventoryFirstCreated(inventory);
		}
		base.ServerInit();
	}

	public override void PreServerLoad()
	{
		base.PreServerLoad();
		CreateInventory(false);
	}

	public override void PostServerLoad()
	{
		base.PostServerLoad();
		if (inventory != null && inventory.uid == 0)
		{
			inventory.GiveUID();
		}
		SetFlag(Flags.Open, false);
	}

	public void CreateInventory(bool giveUID)
	{
		inventory = new ItemContainer();
		inventory.entityOwner = this;
		inventory.allowedContents = ((allowedContents == (ItemContainer.ContentsType)0) ? ItemContainer.ContentsType.Generic : allowedContents);
		inventory.onlyAllowedItem = onlyAllowedItem;
		inventory.maxStackSize = maxStackSize;
		inventory.ServerInitialize(null, numSlots);
		if (giveUID)
		{
			inventory.GiveUID();
		}
		inventory.onItemAddedRemoved = OnItemAddedOrRemoved;
		inventory.onDirty += OnInventoryDirty;
	}

	public override void Save(SaveInfo info)
	{
		base.Save(info);
		if (info.forDisk)
		{
			if (inventory != null)
			{
				info.msg.storageBox = Facepunch.Pool.Get<StorageBox>();
				info.msg.storageBox.contents = inventory.Save();
			}
			else
			{
				Debug.LogWarning("Storage container without inventory: " + ToString());
			}
		}
	}

	public virtual void OnInventoryFirstCreated(ItemContainer container)
	{
	}

	public virtual void OnItemAddedOrRemoved(Item item, bool added)
	{
	}

	protected virtual void OnInventoryDirty()
	{
	}

	public override void OnKilled(HitInfo info)
	{
		DropItems();
		base.OnKilled(info);
	}

	public void DropItems(BaseEntity initiator = null)
	{
		if (inventory != null && inventory.itemList != null && inventory.itemList.Count != 0 && dropChance != 0f)
		{
			DropUtil.DropItems(inventory, GetDropPosition(), dropChance);
		}
	}

	[RPC_Server]
	[RPC_Server.IsVisible(3f)]
	private void RPC_OpenLoot(RPCMessage rpc)
	{
		if (inventory != null)
		{
			BasePlayer player = rpc.player;
			if ((bool)player && player.CanInteract())
			{
				PlayerOpenLoot(player, lootPanelName);
			}
		}
	}

	public virtual bool PlayerOpenLoot(BasePlayer player, string panelToOpen = "", bool doPositionChecks = true)
	{
		if (Interface.CallHook("CanLootEntity", player, this) != null)
		{
			return false;
		}
		if (needsBuildingPrivilegeToUse && !player.CanBuild())
		{
			return false;
		}
		if (onlyOneUser && IsOpen())
		{
			player.ChatMessage("Already in use");
			return false;
		}
		if (panelToOpen == "")
		{
			panelToOpen = lootPanelName;
		}
		if (player.inventory.loot.StartLootingEntity(this, doPositionChecks))
		{
			SetFlag(Flags.Open, true);
			player.inventory.loot.AddContainer(inventory);
			player.inventory.loot.SendImmediate();
			player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", lootPanelName);
			SendNetworkUpdate();
			return true;
		}
		return false;
	}

	public virtual void PlayerStoppedLooting(BasePlayer player)
	{
		Interface.CallHook("OnLootEntityEnd", player, this);
		SetFlag(Flags.Open, false);
		SendNetworkUpdate();
	}

	public bool OccupiedCheck(BasePlayer player = null)
	{
		if (player != null && player.inventory.loot.entitySource == this)
		{
			return true;
		}
		if (onlyOneUser)
		{
			return !IsOpen();
		}
		return true;
	}

	public override void Load(LoadInfo info)
	{
		base.Load(info);
		if (info.fromDisk && info.msg.storageBox != null)
		{
			if (inventory != null)
			{
				inventory.Load(info.msg.storageBox.contents);
				inventory.capacity = numSlots;
			}
			else
			{
				Debug.LogWarning("Storage container without inventory: " + ToString());
			}
		}
	}
}
