﻿using ConVar;
using Newtonsoft.Json;
using System;

namespace Oxide.Plugins
{
    [Info("Dynamic Player Limit", "Pho3niX90", "0.0.3")]
    [Description("Increases the player limit by demand")]
    class DynamicPlayerLimit : RustPlugin
    {
        int _originalLimit = 0;
        Timer updateTimer = null;
        void OnServerInitialized(bool serverInitialized) {
            _originalLimit = Admin.ServerInfo().MaxPlayers;

            int startPlayerLimit = Math.Max(Admin.ServerInfo().Players, config.startPlayerSlots);
            UpdatePlayerLimit(startPlayerLimit);
            updateTimer = timer.Every(config.incrementInterval * 60, () => IncrementPlayers());
        }

        void Unload() {
            UpdatePlayerLimit(Math.Max(BasePlayer.activePlayerList.Count, _originalLimit));
            if (updateTimer != null) updateTimer.Destroy();
        }

        #region Helpers
        private void IncrementPlayers() {
            int currentSlots = Admin.ServerInfo().MaxPlayers;
            if (currentSlots >= config.maxPlayerSlots) return;

            int slotsOpen = (currentSlots - Admin.ServerInfo().Players);
            if (slotsOpen <= config.incrementSlotsOpen) {
                int newSlots = Math.Min(config.incrementPlayerSlots + currentSlots, config.maxPlayerSlots);

                Puts($"Incrementing player slots from `{currentSlots}` to `{newSlots}\n Queued players `{Admin.ServerInfo().Queued}`\n Joining players `{Admin.ServerInfo().Joining}`");
                UpdatePlayerLimit(newSlots);
            }
        }

        private void UpdatePlayerLimit(int limit) => rust.RunServerCommand($"server.maxplayers {limit}");
        #endregion

        #region Configuration
        private static ConfigData config = new ConfigData();

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Starting Player Slots")]
            public int startPlayerSlots = 75;

            [JsonProperty(PropertyName = "Maximum Player Slots")]
            public int maxPlayerSlots = 125;

            [JsonProperty(PropertyName = "Increment Player Slots")]
            public int incrementPlayerSlots = 2;

            [JsonProperty(PropertyName = "Increment Interval Minutes")]
            public int incrementInterval = 3;

            [JsonProperty(PropertyName = "Increment When Slots Available")]
            public int incrementSlotsOpen = 0;
        }

        protected override void LoadConfig() {
            base.LoadConfig();

            try {
                config = Config.ReadObject<ConfigData>();
                if (config == null) LoadDefaultConfig();
            } catch {
                PrintError("Your config seems to be corrupted. Will load defaults.");
                LoadDefaultConfig();
                return;
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig() {
            config = new ConfigData();
        }

        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion
    }
}
