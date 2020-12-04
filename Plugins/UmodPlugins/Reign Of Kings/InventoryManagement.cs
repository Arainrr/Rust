﻿using CodeHatch;
using CodeHatch.Common;
using CodeHatch.Engine.Networking;
using CodeHatch.Inventory.Blueprints;
using CodeHatch.Inventory.Blueprints.Components;
using CodeHatch.ItemContainer;
using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Inventory Management", "D-Kay", "1.2.2")]
    [Description("System to manage the inventory of a player.")]
    public class InventoryManagement : ReignOfKingsPlugin
    {
        #region Hook Methods

        #region Validate Items

        private bool IsValidItem(string name)
        {
            InvItemBlueprint blueprint;
            return TryGetItem(name, out blueprint);
        }

        private bool AreValidItems(List<string> names)
        {
            if (!names.Any()) return false;
            InvItemBlueprint item;
            foreach (var name in names) if (!TryGetItem(name, out item)) return false;
            return true;
        }

        #endregion

        #region Has Space

        private bool HasSpace(Player player, int amount = 1)
        {
            var collection = GetPlayerInventory(player);
            return collection.FreeSlotCount >= amount;
        }

        private bool HasSpace(Player player, string item, int amount = 1)
        {
            InvItemBlueprint blueprint;
            return TryGetItem(item, out blueprint) && HasSpace(player, blueprint, amount);
        }

        private bool HasSpace(Player player, InvItemBlueprint item, int amount = 1)
        {
            var collection = GetPlayerInventory(player);
            var stacks = (int)Math.Ceiling((double)amount / GetStackLimit(item));
            return collection.FreeSlotCount >= stacks;
        }

        private bool HasSpace(Player player, Dictionary<string, int> items)
        {
            Dictionary<InvItemBlueprint, int> blueprints;
            return TryGetItems(items, out blueprints) && HasSpace(player, blueprints);
        }

        private bool HasSpace(Player player, Dictionary<InvItemBlueprint, int> items)
        {
            var collection = GetPlayerInventory(player);
            var stacks = 0;
            items.Foreach(i => stacks += (int)Math.Ceiling((double)i.Value / GetStackLimit(i.Key)));
            return collection.FreeSlotCount >= stacks;
        }

        #endregion

        #region Has Items

        private bool HasItem(Player player, string item, int amount = 1)
        {
            InvItemBlueprint blueprint;
            return TryGetItem(item, out blueprint) && HasItem(player, blueprint, amount);
        }

        private bool HasItem(Player player, InvItemBlueprint item, int amount = 1)
        {
            var collection = GetPlayerInventory(player);
            var totalAmount = collection.AllItemsOfType(item).Sum(i => i.StackAmount);
            return totalAmount >= amount;
        }

        private bool HasItems(Player player, Dictionary<string, int> items)
        {
            Dictionary<InvItemBlueprint, int> blueprints;
            return TryGetItems(items, out blueprints) && HasItems(player, blueprints);
        }

        private bool HasItems(Player player, Dictionary<InvItemBlueprint, int> items)
        {
            var collection = GetPlayerInventory(player);
            foreach (var item in items)
            {
                var totalAmount = collection.AllItemsOfType(item.Key).Sum(i => i.StackAmount);
                if (totalAmount < item.Value) return false;
            }
            return true;
        }

        #endregion

        #region Add Items

        private bool AddItem(Player player, string item, int amount = 1)
        {
            InvItemBlueprint blueprint;
            return TryGetItem(item, out blueprint) && AddItem(player, blueprint, amount);
        }

        private bool AddItem(Player player, InvItemBlueprint item, int amount = 1)
        {
            if (!HasSpace(player, item, amount)) return false;
            var collection = GetPlayerInventory(player);
            var stackLimit = GetStackLimit(item);
            var stacks = (int)Math.Ceiling((double)amount / stackLimit);
            var amountRemaining = amount;
            for (var i = 0; i < stacks; i++)
            {
                var invGameItemStack = new InvGameItemStack(item, amountRemaining, null);
                collection.AddItem(invGameItemStack);
                amountRemaining -= stackLimit;
            }
            Log($"Added {amount} {item} to {player.Name} ({player.Id})");
            return true;
        }

        private bool AddItems(Player player, Dictionary<string, int> items)
        {
            Dictionary<InvItemBlueprint, int> blueprints;
            return TryGetItems(items, out blueprints) && AddItems(player, blueprints);
        }

        private bool AddItems(Player player, Dictionary<InvItemBlueprint, int> items)
        {
            if (!HasSpace(player, items)) return false;
            var collection = GetPlayerInventory(player);
            foreach (var item in items)
            {
                var stackLimit = GetStackLimit(item.Key);
                var stacks = (int)Math.Ceiling((double)item.Value / stackLimit);
                var amountRemaining = item.Value;
                for (var i = 0; i < stacks; i++)
                {
                    var invGameItemStack = new InvGameItemStack(item.Key, amountRemaining, null);
                    collection.AddItem(invGameItemStack);
                    amountRemaining -= stackLimit;
                }
                Log($"Added {item.Value} {item.Key} to {player.Name} ({player.Id})");
            }
            return true;
        }

        #endregion

        #region Remove Items

        private bool RemoveItem(Player player, string item, int amount = 1)
        {
            InvItemBlueprint blueprint;
            return TryGetItem(item, out blueprint) && RemoveItem(player, blueprint, amount);
        }

        private bool RemoveItem(Player player, InvItemBlueprint item, int amount = 1)
        {
            if (!HasItem(player, item, amount)) return false;
            var collection = GetPlayerInventory(player);
            var amountRemaining = amount;
            var items = collection.AllItemsOfType(item);
            foreach (var itemStack in items)
            {
                var removeAmount = itemStack.StackAmount < amountRemaining ? itemStack.StackAmount : amountRemaining;
                collection.SplitItem(itemStack, removeAmount);
                amountRemaining -= removeAmount;
                if (amountRemaining <= 0) break;
            }
            Log($"Removed {amount} {item} from {player.Name} ({player.Id})");
            return true;
        }

        private bool RemoveItems(Player player, Dictionary<string, int> items)
        {
            Dictionary<InvItemBlueprint, int> blueprints;
            return TryGetItems(items, out blueprints) && RemoveItems(player, blueprints);
        }

        private bool RemoveItems(Player player, Dictionary<InvItemBlueprint, int> items)
        {
            if (!HasItems(player, items)) return false;
            var collection = GetPlayerInventory(player);
            foreach (var item in items)
            {
                var amountRemaining = item.Value;
                var itemsOfType = collection.AllItemsOfType(item.Key);
                foreach (var itemStack in itemsOfType)
                {
                    var removeAmount = itemStack.StackAmount < amountRemaining ? itemStack.StackAmount : amountRemaining;
                    collection.SplitItem(itemStack, removeAmount);
                    amountRemaining -= removeAmount;
                    if (amountRemaining <= 0) break;
                }
                Log($"Removed {item.Value} {item.Key} from {player.Name} ({player.Id})");
            }
            
            return true;
        }

        #endregion
        
        private List<InvGameItemStack> GetInventory(Player player)
        {
            var collection = GetPlayerInventory(player, true);
            return collection.GetItems();
        }

        private void DropInventory(Player player)
        {
            var corpse = player.Entity.TryGet<CreateCorpseOnDeath>();
            if (corpse == null) return;

            var gameObject = CustomNetworkInstantiate.ServerInstantiate(corpse.corpsePrefab, player.Entity.Position, player.Entity.Rotation);
            var entity = gameObject.TryGetEntity();
            var container = entity.TryGet<Container>();

            var inv = GetPlayerInventory(player, true);
            InvEquipment.DropInventory(inv, container.Contents);
            Log($"Dropped inventory of {player.Name} ({player.Id}) at {player.Entity.Position}");
        }

        #endregion

        #region System Methods

        private ItemCollection GetPlayerInventory(Player player, bool includeArmor = false)
        {
            if (!includeArmor) return player.GetInventory().Contents;

            var collection = new ItemCollection(player.GetInventory().Contents.MaxSlotCount + 8);
            var containers = player.Entity.TryGetArray<Container>();
            foreach (var t in containers)
            {
                if (!t.Contents.IsUnique) continue;
                foreach (var c in t.Contents)
                {
                    if (c == null) continue;
                    collection.AddItem(c);
                }
            }
            return collection;
        }

        private bool TryGetItem(string item, out InvItemBlueprint blueprint)
        {
            blueprint = InvBlueprints.GetBlueprint(item, true, true);
            return blueprint != null;
        }

        private bool TryGetItems(Dictionary<string, int> items, out Dictionary<InvItemBlueprint, int> blueprints)
        {
            blueprints = new Dictionary<InvItemBlueprint, int>();
            foreach (var item in items)
            {
                InvItemBlueprint blueprint;
                if (!TryGetItem(item.Key, out blueprint)) return false;
                blueprints.Add(blueprint, item.Value);
            }
            return true;
        }

        private int GetStackLimit(InvItemBlueprint item)
        {
            var containerManagement = item.TryGet<ContainerManagement>();
            return containerManagement?.StackLimit ?? 0;
        }

        #endregion

        #region Utility
        
        private void Log(string msg) => LogFileUtil.LogTextToFile($"{Interface.Oxide.LogDirectory}\\Inventory {DateTime.Now:yyyy-MM-dd}.txt", $"[{DateTime.Now:h:mm:ss tt}] {msg}\r\n");

        #endregion
    }
}