#define UNITY_ASSERTIONS
using ConVar;
using Network;
using Oxide.Core;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class VendingMachine : StorageContainer
{
	public static class VendingMachineFlags
	{
		public const Flags EmptyInv = Flags.Reserved1;

		public const Flags IsVending = Flags.Reserved2;

		public const Flags Broadcasting = Flags.Reserved4;

		public const Flags OutOfStock = Flags.Reserved5;

		public const Flags NoDirectAccess = Flags.Reserved6;
	}

	[Header("VendingMachine")]
	public static readonly Translate.Phrase WaitForVendingMessage = new Translate.Phrase("vendingmachine.wait", "Please wait...");

	public GameObjectRef adminMenuPrefab;

	public string customerPanel = "";

	public ProtoBuf.VendingMachine.SellOrderContainer sellOrders;

	public SoundPlayer buySound;

	public string shopName = "A Shop";

	public GameObjectRef mapMarkerPrefab;

	public ItemDefinition blueprintBaseDef;

	protected BasePlayer vend_Player;

	private int vend_sellOrderID;

	private int vend_numberOfTransactions;

	public bool transactionActive;

	private VendingMachineMapMarker myMarker;

	public override bool OnRpcMessage(BasePlayer player, uint rpc, Message msg)
	{
		using (TimeWarning.New("VendingMachine.OnRpcMessage"))
		{
			RPCMessage rPCMessage;
			if (rpc == 3011053703u && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - BuyItem ");
				}
				using (TimeWarning.New("BuyItem"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.IsVisible.Test(3011053703u, "BuyItem", this, player, 3f))
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
							RPCMessage rpc2 = rPCMessage;
							BuyItem(rpc2);
						}
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
						player.Kick("RPC Error in BuyItem");
					}
				}
				return true;
			}
			if (rpc == 1626480840 && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - RPC_AddSellOrder ");
				}
				using (TimeWarning.New("RPC_AddSellOrder"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.IsVisible.Test(1626480840u, "RPC_AddSellOrder", this, player, 3f))
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
							RPC_AddSellOrder(msg2);
						}
					}
					catch (Exception exception2)
					{
						Debug.LogException(exception2);
						player.Kick("RPC Error in RPC_AddSellOrder");
					}
				}
				return true;
			}
			if (rpc == 169239598 && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - RPC_Broadcast ");
				}
				using (TimeWarning.New("RPC_Broadcast"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.IsVisible.Test(169239598u, "RPC_Broadcast", this, player, 3f))
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
							RPC_Broadcast(msg3);
						}
					}
					catch (Exception exception3)
					{
						Debug.LogException(exception3);
						player.Kick("RPC Error in RPC_Broadcast");
					}
				}
				return true;
			}
			if (rpc == 3680901137u && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - RPC_DeleteSellOrder ");
				}
				using (TimeWarning.New("RPC_DeleteSellOrder"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.IsVisible.Test(3680901137u, "RPC_DeleteSellOrder", this, player, 3f))
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
							RPC_DeleteSellOrder(msg4);
						}
					}
					catch (Exception exception4)
					{
						Debug.LogException(exception4);
						player.Kick("RPC Error in RPC_DeleteSellOrder");
					}
				}
				return true;
			}
			if (rpc == 2555993359u && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - RPC_OpenAdmin ");
				}
				using (TimeWarning.New("RPC_OpenAdmin"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.IsVisible.Test(2555993359u, "RPC_OpenAdmin", this, player, 3f))
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
							RPC_OpenAdmin(msg5);
						}
					}
					catch (Exception exception5)
					{
						Debug.LogException(exception5);
						player.Kick("RPC Error in RPC_OpenAdmin");
					}
				}
				return true;
			}
			if (rpc == 36164441 && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - RPC_OpenShop ");
				}
				using (TimeWarning.New("RPC_OpenShop"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.IsVisible.Test(36164441u, "RPC_OpenShop", this, player, 3f))
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
							RPC_OpenShop(msg6);
						}
					}
					catch (Exception exception6)
					{
						Debug.LogException(exception6);
						player.Kick("RPC Error in RPC_OpenShop");
					}
				}
				return true;
			}
			if (rpc == 3346513099u && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - RPC_RotateVM ");
				}
				using (TimeWarning.New("RPC_RotateVM"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.IsVisible.Test(3346513099u, "RPC_RotateVM", this, player, 3f))
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
							RPC_RotateVM(msg7);
						}
					}
					catch (Exception exception7)
					{
						Debug.LogException(exception7);
						player.Kick("RPC Error in RPC_RotateVM");
					}
				}
				return true;
			}
			if (rpc == 1012779214 && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - RPC_UpdateShopName ");
				}
				using (TimeWarning.New("RPC_UpdateShopName"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.IsVisible.Test(1012779214u, "RPC_UpdateShopName", this, player, 3f))
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
							RPCMessage msg8 = rPCMessage;
							RPC_UpdateShopName(msg8);
						}
					}
					catch (Exception exception8)
					{
						Debug.LogException(exception8);
						player.Kick("RPC Error in RPC_UpdateShopName");
					}
				}
				return true;
			}
			if (rpc == 3559014831u && player != null)
			{
				Assert.IsTrue(player.isServer, "SV_RPC Message is using a clientside player!");
				if (Global.developer > 2)
				{
					Debug.Log("SV_RPCMessage: " + player + " - TransactionStart ");
				}
				using (TimeWarning.New("TransactionStart"))
				{
					using (TimeWarning.New("Conditions"))
					{
						if (!RPC_Server.IsVisible.Test(3559014831u, "TransactionStart", this, player, 3f))
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
							RPCMessage rpc3 = rPCMessage;
							TransactionStart(rpc3);
						}
					}
					catch (Exception exception9)
					{
						Debug.LogException(exception9);
						player.Kick("RPC Error in TransactionStart");
					}
				}
				return true;
			}
		}
		return base.OnRpcMessage(player, rpc, msg);
	}

	public override void Load(LoadInfo info)
	{
		base.Load(info);
		if (info.msg.vendingMachine != null)
		{
			shopName = info.msg.vendingMachine.shopName;
			if (info.msg.vendingMachine.sellOrderContainer != null)
			{
				sellOrders = info.msg.vendingMachine.sellOrderContainer;
				sellOrders.ShouldPool = false;
			}
			if (info.fromDisk && base.isServer)
			{
				RefreshSellOrderStockLevel();
			}
		}
	}

	public override void Save(SaveInfo info)
	{
		base.Save(info);
		info.msg.vendingMachine = new ProtoBuf.VendingMachine();
		info.msg.vendingMachine.ShouldPool = false;
		info.msg.vendingMachine.shopName = shopName;
		if (sellOrders != null)
		{
			info.msg.vendingMachine.sellOrderContainer = new ProtoBuf.VendingMachine.SellOrderContainer();
			info.msg.vendingMachine.sellOrderContainer.ShouldPool = false;
			info.msg.vendingMachine.sellOrderContainer.sellOrders = new List<ProtoBuf.VendingMachine.SellOrder>();
			foreach (ProtoBuf.VendingMachine.SellOrder sellOrder2 in sellOrders.sellOrders)
			{
				ProtoBuf.VendingMachine.SellOrder sellOrder = new ProtoBuf.VendingMachine.SellOrder
				{
					ShouldPool = false
				};
				sellOrder2.CopyTo(sellOrder);
				info.msg.vendingMachine.sellOrderContainer.sellOrders.Add(sellOrder);
			}
		}
	}

	public override void ServerInit()
	{
		base.ServerInit();
		if (base.isServer)
		{
			InstallDefaultSellOrders();
			SetFlag(Flags.Reserved2, false);
			base.inventory.onItemAddedRemoved = OnItemAddedOrRemoved;
			RefreshSellOrderStockLevel();
			ItemContainer inventory = base.inventory;
			inventory.canAcceptItem = (Func<Item, int, bool>)Delegate.Combine(inventory.canAcceptItem, new Func<Item, int, bool>(CanAcceptItem));
			UpdateMapMarker();
		}
	}

	public override void DestroyShared()
	{
		if ((bool)myMarker)
		{
			myMarker.Kill();
			myMarker = null;
		}
		base.DestroyShared();
	}

	public override void OnItemAddedOrRemoved(Item item, bool added)
	{
		base.OnItemAddedOrRemoved(item, added);
	}

	public void FullUpdate()
	{
		RefreshSellOrderStockLevel();
		UpdateMapMarker();
		SendNetworkUpdate();
	}

	protected override void OnInventoryDirty()
	{
		base.OnInventoryDirty();
		CancelInvoke(FullUpdate);
		Invoke(FullUpdate, 0.2f);
	}

	public void RefreshSellOrderStockLevel(ItemDefinition itemDef = null)
	{
		foreach (ProtoBuf.VendingMachine.SellOrder so in sellOrders.sellOrders)
		{
			if (itemDef == null || itemDef.itemid == so.itemToSellID)
			{
				if (so.itemToSellIsBP)
				{
					List<Item> list = (from x in base.inventory.FindItemsByItemID(blueprintBaseDef.itemid)
						where x.blueprintTarget == so.itemToSellID
						select x).ToList();
					ProtoBuf.VendingMachine.SellOrder sellOrder = so;
					int inStock;
					if (list == null || list.Count() < 0)
					{
						inStock = 0;
					}
					else
					{
						Interface.CallHook("OnRefreshVendingStock", this, itemDef);
						inStock = list.Sum((Item x) => x.amount) / so.itemToSellAmount;
					}
					sellOrder.inStock = inStock;
				}
				else
				{
					List<Item> list2 = base.inventory.FindItemsByItemID(so.itemToSellID);
					ProtoBuf.VendingMachine.SellOrder sellOrder2 = so;
					int inStock2;
					if (list2 == null || list2.Count < 0)
					{
						inStock2 = 0;
					}
					else
					{
						Interface.CallHook("OnRefreshVendingStock", this, itemDef);
						inStock2 = list2.Sum((Item x) => x.amount) / so.itemToSellAmount;
					}
					sellOrder2.inStock = inStock2;
				}
			}
		}
	}

	public bool OutOfStock()
	{
		foreach (ProtoBuf.VendingMachine.SellOrder sellOrder in sellOrders.sellOrders)
		{
			if (sellOrder.inStock > 0)
			{
				return true;
			}
		}
		return false;
	}

	public override void PostServerLoad()
	{
		base.PostServerLoad();
		SetFlag(Flags.Reserved2, false);
		RefreshSellOrderStockLevel();
		UpdateMapMarker();
	}

	public void UpdateEmptyFlag()
	{
		SetFlag(Flags.Reserved1, base.inventory.itemList.Count == 0);
	}

	public override void PlayerStoppedLooting(BasePlayer player)
	{
		base.PlayerStoppedLooting(player);
		UpdateEmptyFlag();
		if (vend_Player != null && vend_Player == player)
		{
			ClearPendingOrder();
		}
	}

	public virtual void InstallDefaultSellOrders()
	{
		sellOrders = new ProtoBuf.VendingMachine.SellOrderContainer();
		sellOrders.ShouldPool = false;
		sellOrders.sellOrders = new List<ProtoBuf.VendingMachine.SellOrder>();
	}

	public virtual bool HasVendingSounds()
	{
		return true;
	}

	public virtual float GetBuyDuration()
	{
		return 2.5f;
	}

	public void SetPendingOrder(BasePlayer buyer, int sellOrderId, int numberOfTransactions)
	{
		ClearPendingOrder();
		vend_Player = buyer;
		vend_sellOrderID = sellOrderId;
		vend_numberOfTransactions = numberOfTransactions;
		SetFlag(Flags.Reserved2, true);
		if (HasVendingSounds())
		{
			ClientRPC(null, "CLIENT_StartVendingSounds", sellOrderId);
		}
	}

	public void ClearPendingOrder()
	{
		CancelInvoke(CompletePendingOrder);
		vend_Player = null;
		vend_sellOrderID = -1;
		vend_numberOfTransactions = -1;
		SetFlag(Flags.Reserved2, false);
		ClientRPC(null, "CLIENT_CancelVendingSounds");
	}

	[RPC_Server.IsVisible(3f)]
	[RPC_Server]
	public void BuyItem(RPCMessage rpc)
	{
		if (OccupiedCheck(rpc.player))
		{
			int num = rpc.read.Int32();
			int num2 = rpc.read.Int32();
			if (IsVending())
			{
				rpc.player.ShowToast(1, WaitForVendingMessage);
			}
			else if (Interface.CallHook("OnBuyVendingItem", this, rpc.player, num, num2) == null)
			{
				SetPendingOrder(rpc.player, num, num2);
				Invoke(CompletePendingOrder, GetBuyDuration());
			}
		}
	}

	public virtual void CompletePendingOrder()
	{
		DoTransaction(vend_Player, vend_sellOrderID, vend_numberOfTransactions);
		ClearPendingOrder();
		Decay.RadialDecayTouch(base.transform.position, 40f, 2097408);
	}

	[RPC_Server.IsVisible(3f)]
	[RPC_Server]
	public void TransactionStart(RPCMessage rpc)
	{
	}

	public bool DoTransaction(BasePlayer buyer, int sellOrderId, int numberOfTransactions = 1)
	{
		if (sellOrderId < 0 || sellOrderId > sellOrders.sellOrders.Count)
		{
			return false;
		}
		if (Vector3.Distance(buyer.transform.position, base.transform.position) > 4f)
		{
			return false;
		}
		object obj = Interface.CallHook("OnVendingTransaction", this, buyer, sellOrderId, numberOfTransactions);
		if (obj is bool)
		{
			return (bool)obj;
		}
		ProtoBuf.VendingMachine.SellOrder sellOrder = sellOrders.sellOrders[sellOrderId];
		List<Item> list = base.inventory.FindItemsByItemID(sellOrder.itemToSellID);
		if (sellOrder.itemToSellIsBP)
		{
			list = (from x in base.inventory.FindItemsByItemID(blueprintBaseDef.itemid)
				where x.blueprintTarget == sellOrder.itemToSellID
				select x).ToList();
		}
		if (list == null || list.Count == 0)
		{
			return false;
		}
		numberOfTransactions = Mathf.Clamp(numberOfTransactions, 1, list[0].hasCondition ? 1 : 1000000);
		int num = sellOrder.itemToSellAmount * numberOfTransactions;
		int num2 = list.Sum((Item x) => x.amount);
		if (num > num2)
		{
			return false;
		}
		List<Item> source = buyer.inventory.FindItemIDs(sellOrder.currencyID);
		if (sellOrder.currencyIsBP)
		{
			source = (from x in buyer.inventory.FindItemIDs(blueprintBaseDef.itemid)
				where x.blueprintTarget == sellOrder.currencyID
				select x).ToList();
		}
		source = source.Where((Item x) => !x.hasCondition || (x.conditionNormalized >= 0.5f && x.maxConditionNormalized > 0.5f)).ToList();
		if (source.Count == 0)
		{
			return false;
		}
		int num3 = source.Sum((Item x) => x.amount);
		int num4 = sellOrder.currencyAmountPerItem * numberOfTransactions;
		if (num3 < num4)
		{
			return false;
		}
		transactionActive = true;
		int num5 = 0;
		foreach (Item item2 in source)
		{
			int num6 = Mathf.Min(num4 - num5, item2.amount);
			Item takenCurrencyItem = (item2.amount > num6) ? item2.SplitItem(num6) : item2;
			TakeCurrencyItem(takenCurrencyItem);
			num5 += num6;
			if (num5 >= num4)
			{
				break;
			}
		}
		int num7 = 0;
		foreach (Item item3 in list)
		{
			int num8 = num - num7;
			Item item = (item3.amount > num8) ? item3.SplitItem(num8) : item3;
			if (item == null)
			{
				Debug.LogError("Vending machine error, contact developers!");
			}
			else
			{
				num7 += item.amount;
				GiveSoldItem(item, buyer);
			}
			if (num7 >= num)
			{
				break;
			}
		}
		UpdateEmptyFlag();
		transactionActive = false;
		return true;
	}

	public virtual void TakeCurrencyItem(Item takenCurrencyItem)
	{
		if (Interface.CallHook("OnTakeCurrencyItem", this, takenCurrencyItem) == null && !takenCurrencyItem.MoveToContainer(base.inventory))
		{
			takenCurrencyItem.Drop(base.inventory.dropPosition, Vector3.zero);
		}
	}

	public virtual void GiveSoldItem(Item soldItem, BasePlayer buyer)
	{
		if (Interface.CallHook("OnGiveSoldItem", this, soldItem, buyer) == null)
		{
			buyer.GiveItem(soldItem, GiveItemReason.PickedUp);
		}
	}

	public void SendSellOrders(BasePlayer player = null)
	{
		if ((bool)player)
		{
			ClientRPCPlayer(null, player, "CLIENT_ReceiveSellOrders", sellOrders);
		}
		else
		{
			ClientRPC(null, "CLIENT_ReceiveSellOrders", sellOrders);
		}
	}

	[RPC_Server.IsVisible(3f)]
	[RPC_Server]
	public void RPC_Broadcast(RPCMessage msg)
	{
		BasePlayer player = msg.player;
		bool b = msg.read.Bit();
		if (CanPlayerAdmin(player))
		{
			SetFlag(Flags.Reserved4, b);
			Interface.CallHook("OnToggleVendingBroadcast", this, player);
			UpdateMapMarker();
		}
	}

	[RPC_Server.IsVisible(3f)]
	[RPC_Server]
	public void RPC_UpdateShopName(RPCMessage msg)
	{
		BasePlayer player = msg.player;
		string obj = msg.read.String(32);
		if (CanPlayerAdmin(player) && Interface.CallHook("OnVendingShopRename", this, obj, player) == null)
		{
			shopName = obj;
			UpdateMapMarker();
		}
	}

	public void UpdateMapMarker()
	{
		if (IsBroadcasting())
		{
			bool flag = false;
			if (myMarker == null)
			{
				myMarker = (GameManager.server.CreateEntity(mapMarkerPrefab.resourcePath, base.transform.position, Quaternion.identity) as VendingMachineMapMarker);
				flag = true;
			}
			myMarker.SetFlag(Flags.Busy, OutOfStock());
			myMarker.markerShopName = shopName;
			myMarker.server_vendingMachine = this;
			if (flag)
			{
				myMarker.Spawn();
			}
			else
			{
				myMarker.SendNetworkUpdate();
			}
		}
		else if ((bool)myMarker)
		{
			myMarker.Kill();
			myMarker = null;
		}
	}

	[RPC_Server.IsVisible(3f)]
	[RPC_Server]
	public void RPC_OpenShop(RPCMessage msg)
	{
		if (OccupiedCheck(msg.player))
		{
			SendSellOrders(msg.player);
			PlayerOpenLoot(msg.player, customerPanel);
			Interface.CallHook("OnOpenVendingShop", this, msg.player);
		}
	}

	[RPC_Server]
	[RPC_Server.IsVisible(3f)]
	public void RPC_OpenAdmin(RPCMessage msg)
	{
		BasePlayer player = msg.player;
		if (CanPlayerAdmin(player))
		{
			SendSellOrders(player);
			PlayerOpenLoot(player);
			ClientRPCPlayer(null, player, "CLIENT_OpenAdminMenu");
			Interface.CallHook("OnOpenVendingAdmin", this, player);
		}
	}

	public bool CanAcceptItem(Item item, int targetSlot)
	{
		object obj = Interface.CallHook("CanVendingAcceptItem", this, item, targetSlot);
		if (obj is bool)
		{
			return (bool)obj;
		}
		BasePlayer ownerPlayer = item.GetOwnerPlayer();
		if (transactionActive)
		{
			return true;
		}
		if (item.parent == null)
		{
			return true;
		}
		if (base.inventory.itemList.Contains(item))
		{
			return true;
		}
		if (ownerPlayer == null)
		{
			return false;
		}
		return CanPlayerAdmin(ownerPlayer);
	}

	public override bool CanMoveFrom(BasePlayer player, Item item)
	{
		return CanPlayerAdmin(player);
	}

	public override bool CanOpenLootPanel(BasePlayer player, string panelName = "")
	{
		object obj = Interface.CallHook("CanUseVending", player, this);
		if (obj is bool)
		{
			return (bool)obj;
		}
		if (panelName == customerPanel)
		{
			return true;
		}
		if (base.CanOpenLootPanel(player, panelName))
		{
			return CanPlayerAdmin(player);
		}
		return false;
	}

	[RPC_Server]
	[RPC_Server.IsVisible(3f)]
	public void RPC_DeleteSellOrder(RPCMessage msg)
	{
		BasePlayer player = msg.player;
		if (CanPlayerAdmin(player))
		{
			int num = msg.read.Int32();
			Interface.CallHook("OnDeleteVendingOffer", this, num);
			if (num >= 0 && num < sellOrders.sellOrders.Count)
			{
				sellOrders.sellOrders.RemoveAt(num);
			}
			RefreshSellOrderStockLevel();
			UpdateMapMarker();
			SendSellOrders(player);
		}
	}

	[RPC_Server]
	[RPC_Server.IsVisible(3f)]
	public void RPC_RotateVM(RPCMessage msg)
	{
		if (Interface.CallHook("OnRotateVendingMachine", this, msg.player) == null && CanRotate())
		{
			UpdateEmptyFlag();
			if (msg.player.CanBuild() && IsInventoryEmpty())
			{
				base.transform.rotation = Quaternion.LookRotation(-base.transform.forward, base.transform.up);
				SendNetworkUpdate();
			}
		}
	}

	[RPC_Server]
	[RPC_Server.IsVisible(3f)]
	public void RPC_AddSellOrder(RPCMessage msg)
	{
		BasePlayer player = msg.player;
		if (CanPlayerAdmin(player))
		{
			if (sellOrders.sellOrders.Count >= 7)
			{
				player.ChatMessage("Too many sell orders - remove some");
				return;
			}
			int itemToSellID = msg.read.Int32();
			int itemToSellAmount = msg.read.Int32();
			int currencyToUseID = msg.read.Int32();
			int currencyAmount = msg.read.Int32();
			byte bpState = msg.read.UInt8();
			AddSellOrder(itemToSellID, itemToSellAmount, currencyToUseID, currencyAmount, bpState);
		}
	}

	public void AddSellOrder(int itemToSellID, int itemToSellAmount, int currencyToUseID, int currencyAmount, byte bpState)
	{
		ItemDefinition itemDefinition = ItemManager.FindItemDefinition(itemToSellID);
		ItemDefinition x = ItemManager.FindItemDefinition(currencyToUseID);
		if (!(itemDefinition == null) && !(x == null))
		{
			currencyAmount = Mathf.Clamp(currencyAmount, 1, 10000);
			itemToSellAmount = Mathf.Clamp(itemToSellAmount, 1, itemDefinition.stackable);
			ProtoBuf.VendingMachine.SellOrder sellOrder = new ProtoBuf.VendingMachine.SellOrder();
			sellOrder.ShouldPool = false;
			sellOrder.itemToSellID = itemToSellID;
			sellOrder.itemToSellAmount = itemToSellAmount;
			sellOrder.currencyID = currencyToUseID;
			sellOrder.currencyAmountPerItem = currencyAmount;
			sellOrder.currencyIsBP = (bpState == 3 || bpState == 2);
			sellOrder.itemToSellIsBP = (bpState == 3 || bpState == 1);
			Interface.CallHook("OnAddVendingOffer", this, sellOrder);
			sellOrders.sellOrders.Add(sellOrder);
			RefreshSellOrderStockLevel(itemDefinition);
			UpdateMapMarker();
			SendNetworkUpdate();
		}
	}

	public void RefreshAndSendNetworkUpdate()
	{
		RefreshSellOrderStockLevel();
		SendNetworkUpdate();
	}

	public void UpdateOrCreateSalesSheet()
	{
		ItemDefinition itemDefinition = ItemManager.FindItemDefinition("note");
		List<Item> list = base.inventory.FindItemsByItemID(itemDefinition.itemid);
		Item item = null;
		foreach (Item item4 in list)
		{
			if (item4.text.Length == 0)
			{
				item = item4;
				break;
			}
		}
		if (item == null)
		{
			ItemDefinition itemDefinition2 = ItemManager.FindItemDefinition("paper");
			Item item2 = base.inventory.FindItemByItemID(itemDefinition2.itemid);
			if (item2 != null)
			{
				item = ItemManager.CreateByItemID(itemDefinition.itemid, 1, 0uL);
				if (!item.MoveToContainer(base.inventory))
				{
					item.Drop(GetDropPosition(), GetDropVelocity());
				}
				item2.UseItem();
			}
		}
		if (item != null)
		{
			foreach (ProtoBuf.VendingMachine.SellOrder sellOrder in sellOrders.sellOrders)
			{
				ItemDefinition itemDefinition3 = ItemManager.FindItemDefinition(sellOrder.itemToSellID);
				Item item3 = item;
				item3.text = item3.text + itemDefinition3.displayName.translated + "\n";
			}
			item.MarkDirty();
		}
	}

	protected virtual bool CanRotate()
	{
		return true;
	}

	public bool IsBroadcasting()
	{
		return HasFlag(Flags.Reserved4);
	}

	public bool IsInventoryEmpty()
	{
		return HasFlag(Flags.Reserved1);
	}

	public bool IsVending()
	{
		return HasFlag(Flags.Reserved2);
	}

	public bool PlayerBehind(BasePlayer player)
	{
		return Vector3.Dot(base.transform.forward, (player.transform.position - base.transform.position).normalized) <= -0.7f;
	}

	public bool PlayerInfront(BasePlayer player)
	{
		return Vector3.Dot(base.transform.forward, (player.transform.position - base.transform.position).normalized) >= 0.7f;
	}

	public virtual bool CanPlayerAdmin(BasePlayer player)
	{
		object obj = Interface.CallHook("CanAdministerVending", player, this);
		if (obj is bool)
		{
			return (bool)obj;
		}
		if (PlayerBehind(player))
		{
			return OccupiedCheck(player);
		}
		return false;
	}
}
