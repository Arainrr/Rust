//Requires: BetterChat
using System;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins {
    [Info ("HWClanTags", "Mr. Blue", "0.0.2")]
    [Description ("Displays ingame clan tag in BetterChat message")]

    class HWClanTags : HurtworldPlugin {
        [PluginReference]
        private Plugin BetterChat;

        protected override void LoadDefaultConfig () {
            Config["UseClanColor"] = true;
            Config["DefaultClanColor"] = "#ffffff";
            Config["FullName"] = true;
            Config["TagBefore"] = "[";
            Config["TagAfter"] = "]";
        }

        public static string HexString (Color32 aColor) {
            String rs = Convert.ToString (aColor.r, 16).ToUpper ();
            String gs = Convert.ToString (aColor.g, 16).ToUpper ();
            String bs = Convert.ToString (aColor.b, 16).ToUpper ();
            while (rs.Length < 2) rs = "0" + rs;
            while (gs.Length < 2) gs = "0" + gs;
            while (bs.Length < 2) bs = "0" + bs;
            return "#" + rs + gs + bs;
        }

        private string GetClanTag (IPlayer player) {
            string clantag, clancolor;

            Clan clan = (player.Object as PlayerSession).Identity.Clan;
            if (clan == null) return null;

            if ((bool) Config["FullName"])
                clantag = clan.ClanName;
            else
                clantag = clan.ClanTag;

            if ((bool) Config["UseClanColor"])
                clancolor = HexString (clan.ClanColor);
            else
                clancolor = (string) Config["DefaultClanColor"];

            return $"<color={clancolor}>{Config["TagBefore"]}{clantag}{Config["TagAfter"]}</color>";
        }
        void OnServerInitialized () {
            BetterChat.CallHook ("API_RegisterThirdPartyTitle", this, new Func<IPlayer, string> (GetClanTag));
        }
    }
}