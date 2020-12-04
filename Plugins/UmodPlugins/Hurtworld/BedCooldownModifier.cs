﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Bed Cooldown Modifier", "Mr. Blue", "1.0.0")]
    [Description("Change the cooldown time of respawning at a bed")]
    class BedCooldownModifier : CovalencePlugin
    {
        #region Configuration
        private PluginConfig _config;

        class PluginConfig
        {
            [JsonProperty("Default Cooldown")]
            public float DefaultCooldown = 120f;

            [JsonProperty("Cooldowns")]
            public Dictionary<string, float> Cooldowns = new Dictionary<string, float>();
        }

        public float GetPlayerCooldown(PlayerIdentity identity)
        {
            float output = _config.DefaultCooldown;

            foreach (var cooldown in _config.Cooldowns)
                if (cooldown.Value > output && permission.UserHasPermission(identity.SteamId.ToString(), cooldown.Key))
                    output = cooldown.Value;

            return output;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<PluginConfig>();
                if (_config == null) throw new Exception();

                foreach (var cooldown in _config.Cooldowns)
                    permission.RegisterPermission(cooldown.Key, this);
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        protected override void LoadDefaultConfig() {
            _config = new PluginConfig();
            _config.Cooldowns.Add("bedcooldownmodifier.fast", 20f);
            SaveConfig();
        }
        #endregion

        #region BedHandling
        void OnServerInitialized() => UpdateAllRespawnTimers();

        private void UpdateAllRespawnTimers()
        {
            Dictionary<Transform, BedMachineServer>.Enumerator enumerator = RefTrackedBehavior<BedMachineServer>.GetEnumerator();
            while (enumerator.MoveNext())
                UpdateBed(enumerator.Current.Value);
        }

        void OnPlayerDeath(PlayerSession session, EntityEffectSourceData source)
        {
            BedMachineServer bed = GedPlayerBed(session);
            if (bed == null) return;
            UpdateBed(bed);
        }

        private BedMachineServer GedPlayerBed(PlayerSession session)
        {
            Dictionary<Transform, BedMachineServer>.Enumerator enumerator = RefTrackedBehavior<BedMachineServer>.GetEnumerator();
            while (enumerator.MoveNext())
                if (enumerator.Current.Value.CanSpawnPlayer(session.Identity))
                    return enumerator.Current.Value;

            return null;
        }

        private void UpdateBed(BedMachineServer bed)
        {
            bed.RespawnCooldownTime = GetPlayerCooldown(bed.Owner);

            if (bed.Owner == null) return;

            if (bed.Buyback() && bed.Owner.ConnectedSession.IPlayer.Health > 0)
                bed.ResetSpawnTimer(bed.Owner);
        }

        void OnEntitySpawned(HNetworkView networkView)
        {
            BedMachineServer bed = networkView?.gameObject?.GetComponent<BedMachineServer>();
            if (bed == null) return;
            NextTick(() => UpdateBed(bed));
        }
        #endregion
    }
}