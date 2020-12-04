using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Custom Vending Setup", "Iv Misticos", "1.0.1")]
    [Description("Make your NPC vending machines sell custom items")]
    class CustomVendingSetup : CovalencePlugin
    {
        #region Variables

        private static CustomVendingSetup _ins;

        private const string PermissionUse = "customvendingsetup.use";

        private const string Command = "customvendingsetup.edit";

        #endregion

        #region Work with Data

        private PluginData _data;

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _data);

        private void LoadData()
        {
            try
            {
                _data = Interface.Oxide.DataFileSystem.ReadObject<PluginData>(Name);
            }
            catch (Exception e)
            {
                PrintError(e.ToString());
            }

            if (_data == null) _data = new PluginData();
        }

        private class PluginData
        {
            public List<VendingInfo> Vendings = new List<VendingInfo>();

            public class VendingInfo
            {
                public string Id;
                public List<VendingOffer> Offers = new List<VendingOffer>();

                public string Shortname;
                public Vector3 WorldPosition;
                public Vector3 RelativePosition;
                public string RelativeMonument;

                public bool DetectByShortname = false;

                public static int FindIndex(string id)
                {
                    for (var i = 0; i < _ins._data.Vendings.Count; i++)
                    {
                        if (_ins._data.Vendings[i].Id == id)
                            return i;
                    }

                    return -1;
                }

                public static VendingInfo Find(string id)
                {
                    var index = FindIndex(id);
                    return index == -1 ? null : _ins._data.Vendings[index];
                }

                public static int FindIndex(BaseNetworkable entity)
                {
                    for (var i = 0; i < _ins._data.Vendings.Count; i++)
                    {
                        if (_ins.VendingMachineCorresponds(entity, _ins._data.Vendings[i]))
                            return i;
                    }

                    return -1;
                }
            }

            public class VendingOffer
            {
                public VendingItem Currency = new VendingItem();
                public VendingItem SellItem = new VendingItem();
            }

            public class VendingItem
            {
                public string Shortname = string.Empty;
                public string DisplayName = string.Empty;
                public int Amount = 1;
                public ulong Skin = 0;
                public bool IsBlueprint = false;

                public bool Corresponds(Item item)
                {
                    if (item.skin != Skin)
                        return false;

                    return string.IsNullOrEmpty(item.name) && string.IsNullOrEmpty(DisplayName) ||
                           item.name == DisplayName;
                }
            }
        }

        #endregion

        #region Hooks

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"No Permission", "You shall not pass!"},
                {
                    "Command: Syntax", "Syntax:\n" +
                                       "register (ID) - Register a vending machine under a specified ID OR update an existing one\n" +
                                       "remove (ID) - Remove this vending from data-file\n" +
                                       "clear (ID) - Clear all orders\n" +
                                       "refill - Refill all vendings from data-file"
                },
                {"Command: Only Players", "Only players can use this command"},
                {"Command: Vending Not Found", "You are not looking at a vending machine!"},
                {"Command: Vending ID Not Found", "Vending wasn't found, make sure you specified a correct ID."},
                {"Command: Registered", "This vending was registered or updated. Continue setup in data-file"},
                {"Command: Cleared", "This vending was cleared."},
                {"Command: Vending Disappeared", "This vending machine entity has disappeared."},
                {"Command: Removed", "This vending was removed from data-file."},
                {"Command: Refilled", "All vendings were refilled."}
            }, this);
        }

        private void Init()
        {
            _ins = this;

            LoadData();

            permission.RegisterPermission(PermissionUse, this);

            AddCovalenceCommand(Command, nameof(CustomVendingCommand));
        }

        private void OnServerInitialized()
        {
            // I feel like invokes are not always stopped when I do this with existing behaviours for some reason like InvokeHandler.Instance or ServerMgr.
            // This was such an issue and this is the simplest way to fix it haha.

            new GameObject().AddComponent<CustomVendingSetupBehaviour>();
            CustomVendingSetupBehaviour.Instance.InvokeRepeating(Refill, 0f, 60f);
        }

        private void Unload()
        {
            UnityEngine.Object.DestroyImmediate(CustomVendingSetupBehaviour.Instance?.gameObject);

            _ins = null;
        }

        /*
         * Override default transaction stuff
         * Why? This fixes skinned orders
         * NPCVendingMachine usually creates some custom items and puts into storage without the needed skin so that is why I..
         * Copy paste their code and change it a bit.
         */

        private object OnVendingTransaction(VendingMachine machine, BasePlayer buyer, int sellOrderId,
            int numberOfTransactions)
        {
            var vendingInfoIndex = PluginData.VendingInfo.FindIndex(machine);
            if (vendingInfoIndex == -1) // Not applying for unknown vendings
                return null;

            var info = _data.Vendings[vendingInfoIndex];
            var offer = info.Offers[sellOrderId];
            var sellOrder = machine.sellOrders.sellOrders[sellOrderId];

            List<Item> sellItems;
            if (sellOrder.itemToSellIsBP)
            {
                sellItems = machine.inventory.FindItemsByItemID(machine.blueprintBaseDef.itemid).Where(x =>
                    x.blueprintTarget == sellOrder.itemToSellID && offer.SellItem.Corresponds(x)).ToList();
            }
            else
            {
                sellItems = machine.inventory.FindItemsByItemID(sellOrder.itemToSellID)
                    .Where(offer.SellItem.Corresponds).ToList();
            }

            if (sellItems.Count <= 0)
                return false;

            numberOfTransactions = Mathf.Clamp(numberOfTransactions, 1, sellItems[0].hasCondition ? 1 : 1000000);

            var toSell = sellOrder.itemToSellAmount * numberOfTransactions;
            var availableSellItemsAmount = sellItems.Sum(x => x.amount);
            if (toSell > availableSellItemsAmount)
                return false;

            List<Item> currencyItems;
            if (sellOrder.currencyIsBP)
            {
                currencyItems = buyer.inventory.FindItemIDs(machine.blueprintBaseDef.itemid).Where(x =>
                    x.blueprintTarget == sellOrder.currencyID && offer.Currency.Corresponds(x)).ToList();
            }
            else
            {
                currencyItems = buyer.inventory.FindItemIDs(sellOrder.currencyID)
                    .Where(offer.Currency.Corresponds).ToList();
            }

            currencyItems = (from x in currencyItems
                where !x.hasCondition || x.conditionNormalized >= 0.5f && x.maxConditionNormalized > 0.5f
                select x).ToList();

            if (currencyItems.Count == 0)
                return false;

            var toTakeCurrency = sellOrder.currencyAmountPerItem * numberOfTransactions;
            var availableCurrencyItemsAmount = currencyItems.Sum(x => x.amount);
            if (availableCurrencyItemsAmount < toTakeCurrency)
                return false;

            machine.transactionActive = true;

            var alreadyTookCurrency = 0;
            foreach (var item in currencyItems)
            {
                var amountToTake = Mathf.Min(toTakeCurrency - alreadyTookCurrency, item.amount);
                var takenCurrencyItem = item.amount <= amountToTake ? item : item.SplitItem(amountToTake);

                machine.TakeCurrencyItem(takenCurrencyItem);

                alreadyTookCurrency += amountToTake;
                if (alreadyTookCurrency >= toTakeCurrency)
                    break;
            }

            var sellItem = sellItems[0];
            sellItem.amount += toSell;

            sellItem = sellItem.SplitItem(toSell);
            if (sellItem == null)
            {
                Debug.LogError("Vending machine error, contact developers!");
            }
            else
            {
                sellItem.skin = offer.SellItem.Skin;
                sellItem.name = offer.SellItem.DisplayName;

                buyer.GiveItem(sellItem);
            }

            machine.UpdateEmptyFlag();
            machine.transactionActive = false;

            return true;
        }

        #endregion

        #region Commands

        private void CustomVendingCommand(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(PermissionUse))
            {
                player.Reply(GetMsg("No Permission", player.Id));
                return;
            }

            if (args == null || args.Length == 0)
            {
                goto syntax;
            }

            switch (args[0].ToLower())
            {
                case "reg":
                case "register":
                case "update":
                case "new":
                case "create":
                {
                    if (args.Length != 2)
                        goto syntax;

                    var basePlayer = player.Object as BasePlayer;
                    if (basePlayer == null)
                    {
                        player.Reply(GetMsg("Command: Only Players", player.Id));
                        return;
                    }

                    RaycastHit info;
                    NPCVendingMachine vendingMachine;
                    if (!Physics.Raycast(basePlayer.eyes.HeadRay(), out info) ||
                        (vendingMachine = info.GetEntity() as NPCVendingMachine) == null)
                    {
                        NPCShopKeeper keeper;
                        if ((keeper = info.GetEntity() as NPCShopKeeper) != null)
                        {
                            var foundMachines = new List<NPCVendingMachine>();
                            Vis.Entities(keeper.transform.position, 2f, foundMachines);

                            if (foundMachines.Count == 1)
                            {
                                vendingMachine = foundMachines[0];
                                goto foundVending;
                            }
                        }

                        player.Reply(GetMsg("Command: Vending Not Found", player.Id));
                        return;
                    }

                    foundVending:
                    var vending = PluginData.VendingInfo.Find(args[1]);
                    if (vending == null)
                    {
                        vending = new PluginData.VendingInfo
                        {
                            Id = args[1]
                        };

                        foreach (var order in vendingMachine.vendingOrders.orders)
                        {
                            vending.Offers.Add(new PluginData.VendingOffer
                            {
                                Currency = new PluginData.VendingItem
                                {
                                    Shortname = order.currencyItem.shortname,
                                    Amount = order.currencyAmount,
                                    IsBlueprint = order.currencyAsBP
                                },
                                SellItem = new PluginData.VendingItem
                                {
                                    Shortname = order.sellItem.shortname,
                                    Amount = order.sellItemAmount,
                                    IsBlueprint = order.sellItemAsBP
                                }
                            });
                        }

                        _data.Vendings.Add(vending);
                    }

                    var position = vendingMachine.transform.position;
                    var monument = GetMonument(position);

                    vending.Shortname = vendingMachine.ShortPrefabName;
                    vending.WorldPosition = position;
                    vending.RelativeMonument = monument == null ? string.Empty : monument.name;
                    vending.RelativePosition = monument == null
                        ? Vector3.zero
                        : vendingMachine.transform.InverseTransformPoint(monument.transform.position);

                    SaveData();

                    player.Reply(GetMsg("Command: Registered", player.Id));
                    return;
                }

                case "clear":
                {
                    if (args.Length != 2)
                        goto syntax;
                    
                    var vending = PluginData.VendingInfo.Find(args[1]);
                    if (vending == null)
                    {
                        player.Reply(GetMsg("Command: Vending ID Not Found", player.Id));
                        return;
                    }

                    var vendingMachine = FindVendingMachine(vending);
                    if (vendingMachine == null)
                    {
                        player.Reply(GetMsg("Command: Vending Disappeared", player.Id));
                        return;
                    }
                    
                    vending.Offers.Clear();
                    Refill(vending, vendingMachine);
                    
                    SaveData();
                    
                    player.Reply(GetMsg("Command: Cleared", player.Id));
                    return;

                }

                case "remove":
                case "del":
                case "delete":
                {
                    if (args.Length != 2)
                        goto syntax;

                    var vending = PluginData.VendingInfo.FindIndex(args[1]);
                    if (vending == -1)
                    {
                        player.Reply(GetMsg("Command: Vending ID Not Found", player.Id));
                        return;
                    }

                    _data.Vendings.RemoveAt(vending);
                    SaveData();

                    player.Reply(GetMsg("Command: Removed", player.Id));
                    return;
                }

                case "refill":
                {
                    Refill();

                    player.Reply(GetMsg("Command: Refilled", player.Id));
                    return;
                }
            }

            return;

            syntax:
            player.Reply(GetMsg("Command: Syntax", player.Id));
        }

        #endregion

        #region Helpers

        private void Refill()
        {
            foreach (var info in _data.Vendings)
            {
                var machine = FindVendingMachine(info);
                if (machine == null)
                {
                    PrintWarning($"Vending entity with info ID {info.Id} was not found!");
                    continue;
                }

                Refill(info, machine);
            }
        }

        private void Refill(PluginData.VendingInfo info, NPCVendingMachine machine)
        {
            machine.sellOrders.sellOrders.Clear();
            machine.vendingOrders.orders = new NPCVendingOrder.Entry[info.Offers.Count];

            if (machine.inventory?.itemList != null)
            {
                foreach (var obj in machine.inventory.itemList.ToArray())
                    obj?.Remove();

                machine.inventory.itemList.Clear();
            }

            ItemManager.DoRemoves();

            for (var i = 0; i < info.Offers.Count; i++)
            {
                var offer = info.Offers[i];

                machine.vendingOrders.orders[i] = new NPCVendingOrder.Entry
                {
                    currencyItem = ItemManager.FindItemDefinition(offer.Currency.Shortname),
                    currencyAmount = offer.Currency.Amount,
                    currencyAsBP = offer.Currency.IsBlueprint,
                    sellItem = ItemManager.FindItemDefinition(offer.SellItem.Shortname),
                    sellItemAmount = offer.SellItem.Amount,
                    sellItemAsBP = offer.SellItem.IsBlueprint
                };

                InstallFromVendingOrder(offer, machine);
            }
        }

        private void InstallFromVendingOrder(PluginData.VendingOffer offer, NPCVendingMachine machine)
        {
            var bpState = machine.GetBPState(offer.SellItem.IsBlueprint, offer.Currency.IsBlueprint);
            var order = AddSellOrder(machine, ItemManager.FindItemDefinition(offer.SellItem.Shortname)?.itemid ?? 0,
                offer.SellItem.Amount, ItemManager.FindItemDefinition(offer.Currency.Shortname)?.itemid ?? 0,
                offer.Currency.Amount, bpState);

            machine.transactionActive = true;

            if (bpState == 1 || bpState == 3)
            {
                var item = ItemManager.CreateByItemID(machine.blueprintBaseDef.itemid, offer.SellItem.Amount * 10);
                
                item.blueprintTarget = ItemManager.FindItemDefinition(offer.SellItem.Shortname)?.itemid ?? 0;
                item.name = offer.SellItem.DisplayName;
                
                machine.inventory.Insert(item);
            }
            else
            {
                var item = ItemManager.CreateByName(offer.SellItem.Shortname, offer.SellItem.Amount * 10,
                    offer.SellItem.Skin);
                
                item.name = offer.SellItem.DisplayName;
                
                machine.inventory.Insert(item);
            }

            machine.transactionActive = false;

            RefreshSellOrderStockLevel(machine, order, offer);
        }

        private ProtoBuf.VendingMachine.SellOrder AddSellOrder(VendingMachine machine, int itemToSellId,
            int itemToSellAmount,
            int currencyToUseId, int currencyAmount, byte bpState)
        {
            var itemDefSell = ItemManager.FindItemDefinition(itemToSellId);
            if (itemDefSell == null)
                return null;
            
            var itemDefCurrency = ItemManager.FindItemDefinition(currencyToUseId);
            if (itemDefCurrency == null)
                return null;

            currencyAmount = Mathf.Clamp(currencyAmount, 1, 10000);
            itemToSellAmount = Mathf.Clamp(itemToSellAmount, 1, itemDefSell.stackable);

            var sellOrder = new ProtoBuf.VendingMachine.SellOrder
            {
                ShouldPool = false,
                itemToSellID = itemToSellId,
                itemToSellAmount = itemToSellAmount,
                currencyID = currencyToUseId,
                currencyAmountPerItem = currencyAmount,
                currencyIsBP = bpState == 3 || bpState == 2,
                itemToSellIsBP = bpState == 3 || bpState == 1
            };

            Interface.CallHook("OnAddVendingOffer", this, sellOrder);

            machine.sellOrders.sellOrders.Add(sellOrder);
            machine.RefreshSellOrderStockLevel(itemDefSell);

            machine.UpdateMapMarker();
            machine.SendNetworkUpdate();

            return sellOrder;
        }

        private void RefreshSellOrderStockLevel(VendingMachine machine, ProtoBuf.VendingMachine.SellOrder order,
            PluginData.VendingOffer offer)
        {
            IEnumerable<Item> items;
            if (order.itemToSellIsBP)
            {
                items = machine.inventory.FindItemsByItemID(machine.blueprintBaseDef.itemid)
                    .Where(x => x.blueprintTarget == order.itemToSellID);
            }
            else
            {
                items = machine.inventory.FindItemsByItemID(order.itemToSellID)
                    .Where(x => x.skin == offer.SellItem.Skin && x.name == offer.SellItem.DisplayName);
            }

            int num;
            if (items.Count() < 0)
            {
                num = 0;
            }
            else
            {
                Interface.CallHook("OnRefreshVendingStock", this, null);
                num = items.Sum(x => x.amount) / order.itemToSellAmount;
            }

            order.inStock = num;
        }

        private NPCVendingMachine FindVendingMachine(PluginData.VendingInfo info)
        {
            foreach (var vending in UnityEngine.Object.FindObjectsOfType<NPCVendingMachine>())
            {
                if (VendingMachineCorresponds(vending, info))
                    return vending;
            }

            return null;
        }

        private bool VendingMachineCorresponds(BaseNetworkable vending, PluginData.VendingInfo info)
        {
            // Shortname always matches
            if (vending.ShortPrefabName != info.Shortname)
                return false;

            // If it's on the same WORLD position, this must be the same one
            if (vending.transform.position == info.WorldPosition)
                return true;

            // Shortname always matches
            if (info.DetectByShortname && vending.ShortPrefabName == info.Shortname)
                return true;

            // If relative monument name is invalid, don't even care about it
            if (string.IsNullOrEmpty(info.RelativeMonument))
                return false;

            // If monument is not found or not the same, skip it
            var monument = GetMonument(vending.transform.position);
            if (monument == null || monument.name != info.RelativeMonument)
                return false;

            // If it's in the same relative position, it's definitely the needed one
            return Vector3.Distance(vending.transform.InverseTransformPoint(monument.transform.position),
                info.RelativePosition) <= 0.1f;
        }

        private string GetMsg(string key, string userId) => lang.GetMessage(key, this, userId);

        private MonumentInfo GetMonument(Vector3 position)
        {
            MonumentInfo nearestMonument = null;
            var nearestDistance = float.NaN;
            for (var i = 0; i < TerrainMeta.Path.Monuments.Count; i++)
            {
                var monument = TerrainMeta.Path.Monuments[i];
                var distance = Vector3.Distance(position, monument.transform.position);

                if (!float.IsNaN(nearestDistance) && !(distance <= nearestDistance))
                    continue;

                nearestDistance = distance;
                nearestMonument = monument;
            }

            return nearestMonument;
        }

        private class CustomVendingSetupBehaviour : SingletonComponent<CustomVendingSetupBehaviour>
        {
        }

        #endregion
    }
}