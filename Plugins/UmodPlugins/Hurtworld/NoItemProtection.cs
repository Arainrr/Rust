namespace Oxide.Plugins
{
    [Info("No Item Protection", "Norn", "0.1")]
    [Description("Drop all items on death regardless of protection.")]

    class NoItemProtection : HurtworldPlugin
    {
        private void RemoveProtection(PlayerSession player, ItemObject item)
        {
            if (item == null) { return; }
            var protectedItem = item.GetComponent<ItemComponentAmberProtectionStorage>();
            if (protectedItem != null)
            {
                if (protectedItem.IsProtected != 0)
                {
                    protectedItem.IsProtected = 0;
                    item.InvalidateBaseline();
                }
            }
        }

        private void OnPlayerDeath(PlayerSession session, EntityEffectSourceData source)
        {
            Inventory inventory = session.WorldPlayerEntity.GetComponent<Inventory>();
            for (var i = 0; i < inventory.Capacity; i++)
            {
                var item = inventory.GetSlot(i);
                if (item == null) { continue; }
                RemoveProtection(session, item);
            }
        }
    }
}