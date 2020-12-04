﻿using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Better Health", "birthdates & Default", "1.1.4")]
    [Description("Ability to customize the max health")]
    public class BetterHealth : RustPlugin
    {
        #region Variables
        private readonly List<BasePlayer> InUI = new List<BasePlayer>();
        private const string permission_use = "betterhealth.use";
        #endregion

        #region Hooks
        void Init()
        {
            permission.RegisterPermission(permission_use, this);
            LoadConfig();
            if(_config.MaxHealth <= 100)
            {
                Unsubscribe(nameof(OnPlayerDie));
                Unsubscribe(nameof(OnPlayerHealthChange));
            }

            foreach (var P in _config.Permissions.Keys)
            {
                var Perm = $"betterhealth.{P}";
                if(!permission.PermissionExists(Perm, this)) permission.RegisterPermission(Perm, this);
            }
        }

        void OnPlayerConnected(BasePlayer player) => timer.In(1f, () =>
        {
            TryMax(player);
        });

        void Unload()
        {
            InUI.ForEach(player =>
            {
                CloseUI(player, true);
            });
        }

        void OnPlayerDie(BasePlayer player, HitInfo info) => CloseUI(player);

        void OnPlayerHealthChange(BasePlayer player, float oldValue, float newValue)
        {
                if (player.IPlayer.HasPermission(permission_use) == false) return;

            OpenUI(player, GetHealth(player), true);
        }

        void TryMax(BasePlayer player)
        {
            if(!player.IPlayer.HasPermission(permission_use)) return;
            var h = GetHealth(player);
            player._maxHealth = h;
            if(h > 100)
            {
                OpenUI(player, h);
            }
        }


        void OnPlayerRespawned(BasePlayer player) => TryMax(player);
        #endregion

        #region UI
        void OpenUI(BasePlayer Player, float MaxHealth, bool Close = false)
        {
            if(Close)
            {
                CloseUI(Player);
            }
            var Health = Player.Health();
            var Max = MaxHealth;
            var HealthX = Health / Max;
            var PendingX = Mathf.Clamp(HealthX + Player.metabolism.pending_health.value / Max, 0, 1);

            CuiHelper.AddUi(Player, $"[{{\"name\":\"HealthOverlay\",\"parent\":\"Overlay\",\"components\":[{{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.4375 0.4375 0.4375 1\"}},{{\"type\":\"RectTransform\",\"anchormin\":\"0.856 0.104\",\"anchormax\":\"0.984 0.1309\"}}]}},{{\"name\":\"HealthBar\",\"parent\":\"HealthOverlay\",\"components\":[{{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.5546875 0.7265625 0.3125 1\"}},{{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"{HealthX} 0.97\"}}]}},{{\"name\":\"HealthText\",\"parent\":\"HealthBar\",\"components\":[{{\"type\":\"UnityEngine.UI.Text\",\"text\":\"  {Math.Round(Health).ToString()}\",\"fontSize\":15,\"align\":\"MiddleLeft\",\"color\":\"1 1 1 0.65\"}},{{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\"}}]}},{{\"name\":\"PendingHealth\",\"parent\":\"HealthOverlay\",\"components\":[{{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.5546875 0.7265625 0.3125 0.5\"}}, {{ \"type\":\"RectTransform\", \"anchormin\":\"0 0\", \"anchormax\":\"{PendingX} 0.97\"}}]}} ]");
            InUI.Add(Player);
        }

        float GetHealth(BasePlayer Player)
        {
            var Health = _config.Permissions.Where(p => Player.IPlayer.HasPermission($"betterhealth.{p.Key}"));
            if (Health.Count() < 1) return _config.MaxHealth;
            return Health.Max(a => a.Value);
        }

        void CloseUI(BasePlayer Player, bool Unload = false)
        {
            CuiHelper.DestroyUi(Player, "HealthOverlay");
            if(!Unload)
            {
                InUI.Remove(Player);
            }
        }
        #endregion

        #region Configuration, Language & Data
        public ConfigFile _config;

        public class ConfigFile
        {
            [JsonProperty("Default Max Health")]
            public float MaxHealth = 200f;
            [JsonProperty("Max Health Permissions")]
            public Dictionary<string, float> Permissions = new Dictionary<string, float>
            {
                {"vip", 300f}
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<ConfigFile>();
            if(_config == null)
            {
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig()
        {
            _config = new ConfigFile();
            PrintWarning("Default configuration has been loaded.");
        }

        protected override void SaveConfig() => Config.WriteObject(_config);
        #endregion
    }
}
//Generated with birthdates' Plugin Maker
