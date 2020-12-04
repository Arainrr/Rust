﻿namespace Oxide.Plugins
{
    [Info("Shack Cooldown Modifier", "Mr. Blue", "1.0.0")]
    [Description("Change the cooldown time of respawning at a shack")]
    class ShackCooldownModifier : CovalencePlugin
    {
        protected override void LoadDefaultConfig()
        {
            if (Config["Cooldown Timer"] == null) Config.Set("Cooldown Timer", 1200f);
        }

        void OnServerInitialized() => ShackDynamicServer.SpawnFatigueTime = Config.Get<float>("Cooldown Timer");
    }
}