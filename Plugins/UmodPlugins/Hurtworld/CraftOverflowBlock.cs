﻿using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Craft Overflow Block", "Mr. Blue", "1.0.2")]
    [Description("Stop items spilling out of the crafting table when full.")]
    class CraftOverflowBlock : CovalencePlugin
    {
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string> {
                { "Craft Blocked", "Storage Full" }
            }, this);
        }

        string Msg(string msg, string SteamId = null) => lang.GetMessage(msg, this, SteamId);

        object CanCraft(Crafter crafter, PlayerSession session, ICraftable recipe, int count)
        {
            ItemGeneratorAsset itemAsset = recipe.GetGeneratorAsset();
            int freeSlot = crafter.Storage.FindFreeSlot(0, itemAsset.GetIconSize());
            if (freeSlot == -1)
            {
                bool isPlayerInv = false;
                if (crafter.Storage is PlayerInventory)
                    isPlayerInv = true;

                Inventory inv = crafter.Storage;
                bool slotAvailable = false;

                for (int i = 0; i < inv.Capacity; i++)
                {
                    if (isPlayerInv && (i > 7 && i < 36)) continue; //Skip player gear/blueprint/unused slots

                    ItemObject slot = inv.GetSlot(i);
                    if (slot == null) continue;

                    if (!slot.Generator.Equals(itemAsset)) continue;

                    if (slot.Generator.DataProvider.MaxStackSize - slot.StackSize < count * recipe.GetStackSize()) continue;

                    slotAvailable = true;
                    break;
                }

                if (!slotAvailable)
                {
                    AlertManager.Instance.GenericTextNotificationServer(Msg("Craft Blocked", session.SteamId.ToString()), session.Player);
                    return false;
                }
            }
            return null;
        }
    }
}