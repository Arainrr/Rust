﻿using System.Collections.Generic;
using Newtonsoft.Json;


namespace Oxide.Plugins
{
    [Info("Bed Rename Blocker", "Gimax", "1.1.2")]
    [Description("Blocks people of renaming a bed/sleeping bag if they are not the owner of it")]
    class BedRenameBlocker : RustPlugin
    {
        private Cfg _cfg;
        readonly string perm = "bedrenameblocker.block";

        #region Config
        private class Cfg
        {
            [JsonProperty(PropertyName = "Use permission (Default false)")]
            public bool UsePermission { get; set; }
        }

        protected override void LoadDefaultConfig() => _cfg = new Cfg
        {
            UsePermission = false,
        };

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _cfg = Config.ReadObject<Cfg>();
        }

        protected override void SaveConfig() => Config.WriteObject(_cfg);
        #endregion

        #region Oxide Hook
        private void Init()
        {
            permission.RegisterPermission(perm, this);
        }
        #endregion

        #region Languages
        protected override void LoadDefaultMessages() => lang.RegisterMessages(new Dictionary<string, string>
        {
            ["NotOwner"] = "You can not rename the bed if you are not the owner of it!",
            ["NoPermission"] = "You've been blocked to rename any sleeping bags/beds!"
        }, this, "en");

        #endregion

        #region Hook
        private object CanRenameBed(BasePlayer player, SleepingBag bed, string bedName)
        {
            if (_cfg.UsePermission)
            {
                if (permission.UserHasPermission(player.UserIDString, perm))
                {
                    player.ChatMessage(Lang("NoPermission", player.UserIDString));
                    return true;
                }
                return null;
            }

            if (bed == null || bed.OwnerID.ToString() == null || player == null || bedName == null) return true;
            if (bed.OwnerID.ToString() != player.UserIDString)
            {
                player.ChatMessage(Lang("NotOwner", player.UserIDString));
                return true;
            }
            return null;           
        }
        #endregion

        #region Helper
        private string Lang(string key, string id, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        #endregion

    }
}

























