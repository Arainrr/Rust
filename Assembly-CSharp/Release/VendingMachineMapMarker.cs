using Facepunch;
using ProtoBuf;
using System.Collections.Generic;

public class VendingMachineMapMarker : MapMarker
{
	public string markerShopName;

	public VendingMachine server_vendingMachine;

	public ProtoBuf.VendingMachine.SellOrderContainer client_sellOrders;

	public GameObjectRef clusterMarkerObj;

	public override void Save(SaveInfo info)
	{
		base.Save(info);
		info.msg.vendingMachine = new ProtoBuf.VendingMachine();
		info.msg.vendingMachine.shopName = markerShopName;
		if (server_vendingMachine != null)
		{
			info.msg.vendingMachine.sellOrderContainer = new ProtoBuf.VendingMachine.SellOrderContainer();
			info.msg.vendingMachine.sellOrderContainer.ShouldPool = false;
			info.msg.vendingMachine.sellOrderContainer.sellOrders = new List<ProtoBuf.VendingMachine.SellOrder>();
			foreach (ProtoBuf.VendingMachine.SellOrder sellOrder2 in server_vendingMachine.sellOrders.sellOrders)
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

	public override AppMarker GetAppMarkerData()
	{
		AppMarker appMarkerData = base.GetAppMarkerData();
		appMarkerData.name = (markerShopName ?? "");
		appMarkerData.outOfStock = !HasFlag(Flags.Busy);
		if (server_vendingMachine != null)
		{
			appMarkerData.sellOrders = Pool.GetList<AppMarker.SellOrder>();
			{
				foreach (ProtoBuf.VendingMachine.SellOrder sellOrder2 in server_vendingMachine.sellOrders.sellOrders)
				{
					AppMarker.SellOrder sellOrder = Pool.Get<AppMarker.SellOrder>();
					sellOrder.itemId = sellOrder2.itemToSellID;
					sellOrder.quantity = sellOrder2.itemToSellAmount;
					sellOrder.currencyId = sellOrder2.currencyID;
					sellOrder.costPerItem = sellOrder2.currencyAmountPerItem;
					sellOrder.amountInStock = sellOrder2.inStock;
					sellOrder.itemIsBlueprint = sellOrder2.itemToSellIsBP;
					sellOrder.currencyIsBlueprint = sellOrder2.currencyIsBP;
					appMarkerData.sellOrders.Add(sellOrder);
				}
				return appMarkerData;
			}
		}
		return appMarkerData;
	}
}
