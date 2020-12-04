﻿using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Disable Voice Chat", "Mr. Blue", "1.0.2")]
    [Description("Disables voice chat in the server")]

    public class DisableVoiceChat : HurtworldPlugin
    {
        public const string perm = "disablevoicechat.ignore";
        void Init()
        {
            permission.RegisterPermission(perm, this);
        }
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["VoiceDisabled"] = "<color=red>Voice Disabled</color>"
            }, this);
        }
        private string Msg(string msg, object SteamId = null) => lang.GetMessage(msg, this, SteamId.ToString());

        object OnPlayerVoice(PlayerSession session)
        {
            string steamID = session.SteamId.ToString();
            if (permission.UserHasPermission(steamID, perm))
            {
                AlertManager.Instance.GenericTextNotificationServer(Msg("VoiceDisabled", steamID), session.Player);
                return true;
            }
            return null;
        }
    }
}