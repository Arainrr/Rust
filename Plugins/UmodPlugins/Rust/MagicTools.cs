﻿using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Magic Tools", "Tricky", "2.0.0")]
    [Description("Automatically smelts mined resources")]
    class MagicTools : RustPlugin
    {
        #region Declared
        private readonly string usePerm = "magictools.use";
        #endregion

        #region Oxide Hooks
        private void Init() => permission.RegisterPermission(usePerm, this);

        private object OnDispenserGather(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (dispenser == null || player == null || item == null)
                return null;

            if (!HasPermission(player, usePerm))
                return null;

            ItemModCookable cookable = item.info.GetComponent<ItemModCookable>();
            if (cookable == null)
                return null;

            var cookedItem = ItemManager.Create(cookable.becomeOnCooked, item.amount);
            player.GiveItem(cookedItem, BaseEntity.GiveItemReason.ResourceHarvested);

            return true;
        }
        #endregion

        #region Helpers
        private bool HasPermission(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);
        #endregion
    }
}
