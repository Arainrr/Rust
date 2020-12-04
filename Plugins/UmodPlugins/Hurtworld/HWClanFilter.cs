﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("HW Clan Filter", "Mr. Blue", "1.0.0")]
    [Description("Filter out bad words and characters on clan creation")]

    public class HWClanFilter : HurtworldPlugin
    {
        #region Variables
        private List<string> bannedTags;
        private List<string> bannedNames;
        private List<string> bannedDescriptions;
        private bool onlyASCII;
        private string allowedPermission = "hwclanfilter.allow";
        #endregion

        #region Loading
        private void Init()
        {
            permission.RegisterPermission(allowedPermission, this);
            bannedTags = Config.Get<List<string>>("BannedWords - Tags");
            bannedNames = Config.Get<List<string>>("BannedWords - Names");
            bannedDescriptions = Config.Get<List<string>>("BannedWords - Description");
            onlyASCII = Config.Get<bool>("Only ASCII");
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string> { { "NameBlocked", "Invalid Name" },
                { "TagBlocked", "Invalid Tag" },
                { "DescriptionBlocked", "Invalid Description" },
                { "NonASCII", "Invalid Characters" }
            }, this);
        }
        private string Msg(string msg, object SteamId = null) => lang.GetMessage(msg, this, SteamId.ToString());

        protected override void LoadDefaultConfig()
        {
            Config.Set("BannedWords - Tags", new List<object>() { "gay", "mod", "vip" });
            Config.Set("BannedWords - Names", new List<object>() { "nigger", "dick", "admin", "mod" });
            Config.Set("BannedWords - Description", new List<object>() { "fuck", "nigger", "dick" });
            Config.Set("Only ASCII", false);
        }
        #endregion

        #region Hooks
        object OnClanCreate(string clanName, string clanTag, Color color, string description, PlayerSession session)
        {
            string steamId = session.SteamId.ToString();
            if (permission.UserHasPermission(steamId, allowedPermission))
                return null;

            if (bannedNames.Any(clanName.Contains))
                return Msg("NameBlocked", steamId);

            if (bannedTags.Any(clanTag.Contains))
                return Msg("TagBlocked", steamId); ;

            if (bannedDescriptions.Any(description.Contains))
                return Msg("DescriptionBlocked", steamId);

            if (onlyASCII)
            {
                string check = clanName + clanTag + description;
                if (Encoding.UTF8.GetByteCount(check) != check.Length)
                    return Msg("NonASCII", steamId);
            }

            return null;
        }
        #endregion
    }
}