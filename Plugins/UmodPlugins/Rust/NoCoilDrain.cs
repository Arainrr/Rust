namespace Oxide.Plugins
{
    [Info("No Coil Drain", "Lincoln/Orange", "1.0.3")]
    [Description("Prevent Tesla coils from damaging themselves while in use.")]

    public class NoCoilDrain : RustPlugin
    {
        private void OnServerInitialized()
        {
            permission.RegisterPermission("NoCoilDrain.unlimited", this);

            foreach (var entity in UnityEngine.Object.FindObjectsOfType<TeslaCoil>())
            {
                OnEntitySpawned(entity);
            }
        }

        private void OnEntitySpawned(TeslaCoil entity)
        {
            var player = entity.OwnerID.ToString();
            if (!permission.UserHasPermission(player, "NoCoilDrain.unlimited"))
            {
                return;
            }
            else
            {
                entity.maxDischargeSelfDamageSeconds = 0f;
            }
        }

        private void Unload()
        {
            foreach (var entity in UnityEngine.Object.FindObjectsOfType<TeslaCoil>())
            {
                entity.maxDischargeSelfDamageSeconds = 120f;
            }
        }
    }
}