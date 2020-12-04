﻿using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Admin Auto Time", "Dana", "0.1.2")]
    [Description("Changes the time of day for Admins automatically upon connect.")]
    internal class AdminAutoTime : RustPlugin
    {
        private const string UsePermission = "adminautotime.use";
        #region Config

        Configuration config;

        public class Configuration
        {
            [JsonProperty("Time")]
            public int Time = 9;
        }

        private void Init()
        {
            permission.RegisterPermission(UsePermission, this);
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<Configuration>();
            }
            catch
            {
                PrintError("Loading default config! Error loading your config, it's corrupt!");
                config = null;
            }

            if (config == null)
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
        }

        #endregion Config

        #region Hooks

        void OnPlayerConnected(BasePlayer player)
        {
            if (player != null && player.IsAdmin && permission.UserHasPermission(player.UserIDString, UsePermission))
            {
                player.SendConsoleCommand("admintime", config.Time);
                // PrintWarning($"Called admintime {config.Time} for {player.UserIDString}.");
            }
        }

        #endregion Hooks
    }
}