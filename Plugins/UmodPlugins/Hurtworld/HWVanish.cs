﻿using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("HW Vanish", "klauz24", 1.2), Description("Vanish mode for Hurtworld")]
    internal class HWVanish : HurtworldPlugin
    {
        private const string _perm = "hwvanish.use";

        private void Init() => permission.RegisterPermission(_perm, this);

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"HWVanish - Prefix", "<color=lightblue>[HW Vanish]</color>"},
                {"HWVanish - No Perm", "You do not have permission to use this command."},
                {"HWVanish - No Slot", "Please remove the item from the head slot before turning Vanish on."},
                {"HWVanish - Syntax", "Syntax: /vanish on|off."},
                {"HWVanish - Enabled", "Vanish enabled"},
                {"HWVanish - Disabled", "Vanish disabled"},
                {"HWVanish - Already Enabled", "Vanish is already enabled."},
                {"HWVanish - Not Enabled", "Vanish is not enabled."}
            }, this);
        }

        private void EnableVanish(PlayerSession session)
        {
            Inventory pInventory = session.WorldPlayerEntity.Storage;
            ItemGeneratorAsset _vanishItem = RuntimeHurtDB.Instance.GetObjectByGuid<ItemGeneratorAsset>("dcea248c43a3a9b43a3ec1a53c3141e5");
            ItemObject vanishItemObject = GlobalItemManager.Instance.CreateItem(_vanishItem, 1);
            if (pInventory.GetSlot(8) == null)
            {
                pInventory.SetSlot(8, vanishItemObject);
                session.WorldPlayerEntity.RPC("UpdateName", uLink.RPCMode.OthersExceptOwnerBuffered, " ");
                AlertManager.Instance.GenericTextNotificationServer(lang.GetMessage("HWVanish - Enabled", this, session.SteamId.ToString()), session.Player);
                return;
            }
            else
            {
                if (pInventory.GetSlot(8).Generator != vanishItemObject.Generator)
                {
                    hurt.SendChatMessage(session, lang.GetMessage("HWVanish - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWVanish - No Slot", this, session.SteamId.ToString()));
                    return;

                }
                else
                {
                    hurt.SendChatMessage(session, lang.GetMessage("HWVanish - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWVanish - Already Enabled", this, session.SteamId.ToString()));
                    return;

                }
            }
        }

        private void DisableVanish(PlayerSession session)
        {
            Inventory pInventory = session.WorldPlayerEntity.Storage;
            ItemGeneratorAsset _vanishItem = RuntimeHurtDB.Instance.GetObjectByGuid<ItemGeneratorAsset>("dcea248c43a3a9b43a3ec1a53c3141e5");
            ItemObject vanishItemObject = GlobalItemManager.Instance.CreateItem(_vanishItem, 1);
            if (pInventory.GetSlot(8) == null)
            {
                hurt.SendChatMessage(session, lang.GetMessage("HWVanish - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWVanish - Not Enabled", this, session.SteamId.ToString()));
                return;
            }
            else
            {
                if (pInventory.GetSlot(8).Generator == vanishItemObject.Generator)
                {
                    pInventory.SetSlot(8, null);
                    session.WorldPlayerEntity.RPC("UpdateName", uLink.RPCMode.OthersExceptOwnerBuffered, session.Identity.Name);
                    AlertManager.Instance.GenericTextNotificationServer(lang.GetMessage("HWVanish - Disabled", this, session.SteamId.ToString()), session.Player);
                    return;
                }
                else
                {
                    hurt.SendChatMessage(session, lang.GetMessage("HWVanish - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWVanish - Not Enabled", this, session.SteamId.ToString()));
                    return;
                }
            }
        }

        [ChatCommand("vanish")]
        private void HWVanishCommand(PlayerSession session, string command, string[] args)
        {
            if (permission.UserHasPermission(session.SteamId.ToString(), _perm) || session.IsAdmin)
            {
                if (args.Length == 0)
                {
                    hurt.SendChatMessage(session, lang.GetMessage("HWVanish - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWVanish - Syntax", this, session.SteamId.ToString()));
                }

                else
                {
                    switch (args[0].ToLower())
                    {
                        case "on":
                            EnableVanish(session);
                            break;

                        case "off":
                            DisableVanish(session);
                            break;

                        default:
                            hurt.SendChatMessage(session, lang.GetMessage("HWVanish - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWVanish - Syntax", this, session.SteamId.ToString()));
                            break;
                    }
                }
            }
            else
            {
                hurt.SendChatMessage(session, lang.GetMessage("HWVanish - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWVanish - No Perm", this, session.SteamId.ToString()));
            }
        }
    }
}