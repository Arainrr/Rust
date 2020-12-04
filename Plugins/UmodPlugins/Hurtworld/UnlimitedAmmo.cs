﻿using System.Collections.Generic;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Unlimited Ammo", "Mr. Blue", "2.0.1")]
    [Description("Allows you to have unlimited ammo.")]
    class UnlimitedAmmo : HurtworldPlugin
    {
        private List<PlayerSession> active = new List<PlayerSession>();
        private const string perm = "unlimitedammo.use";

        #region Hooks
        private void Init()
        {
            permission.RegisterPermission(perm, this);

            timer.Every(1f, () => {
                foreach (PlayerSession player in active)
                    FillAmmo(player);
            });
        }

        private void OnPlayerDisconnected(PlayerSession session)
        {
            if (active.Contains(session))
                active.Remove(session);
        }
        #endregion

        #region Localization
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"No Permission", "You don't have the permission to use this command."},
                {"Enabled", "<color=green>Unlimited Ammo Activated!</color>"},
                {"Disabled", "<color=red>Unlimited Ammo Deactivated!</color>"}
            }, this);
        }
        string Msg(string msg, string SteamId = null) => lang.GetMessage(msg, this, SteamId);
        #endregion

        #region Commands
        [ChatCommand("toggleammo")]
        private void cmdToggleAmmo(PlayerSession session, string command, string[] args)
        {
            string steamId = session.SteamId.ToString();
            if(!permission.UserHasPermission(steamId, perm))
            {
                Player.Message(session, Msg("No Permission", steamId));
                return;
            }

            if (active.Contains(session))
            {
                active.Remove(session);
                AlertManager.Instance.GenericTextNotificationServer(Msg("Disabled", steamId), session.Player);
            }
            else
            {
                active.Add(session);
                AlertManager.Instance.GenericTextNotificationServer(Msg("Enabled", steamId), session.Player);
            }
        }
        #endregion

        #region Ammo / Item Related
        private void FillAmmo(PlayerSession player)
        {
            if (!player.IsLoaded || !player.Player.isConnected)
                return;

            if (player?.WorldPlayerEntity?.GetComponent<EquippedHandlerBase>() == null) return;
            EquippedHandlerBase equippedHandler = player.WorldPlayerEntity.GetComponent<EquippedHandlerBase>();

            if (equippedHandler?.EquipSession?.AmmoStorage != null && equippedHandler?.EquipSession?.AmmoStorage.MaxAmmo != 0)
            {
                //Gun
                ItemComponentAmmoStorage ammoStorage = equippedHandler.EquipSession.AmmoStorage;

                ammoStorage.CurrentAmmo = ammoStorage.MaxAmmo;
                ammoStorage.InitEquip(equippedHandler.EquipSession);
                ammoStorage.TeardownEquip(equippedHandler.EquipSession);
            }
            else
            {
                //Bow
                if (equippedHandler?.GetEquippedItem()?.GetComponent<ItemComponentAmmoConfig>() == null) return;
                ItemComponentAmmoConfig ammoConfig = equippedHandler.GetEquippedItem().GetComponent<ItemComponentAmmoConfig>();

                if (player?.WorldPlayerEntity?.GetComponent<Inventory>() == null) return;
                Inventory inventory = player.WorldPlayerEntity.GetComponent<Inventory>();

                if (!inventory.HasItem(ammoConfig.AmmoType, 1))
                {
                    GlobalItemManager.Instance.GiveItem(ammoConfig.AmmoType, 1, inventory);
                    inventory.Invalidate();
                }
            }
        }
        #endregion
    }
}