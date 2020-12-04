using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Clan Limit", "Mr. Blue", "0.0.1")]
    [Description("Limits the amount of players allowed in a clan.")]

    class ClanLimit : HurtworldPlugin
    {
        const string perm = "clanlimit.allow";
        private static int MaxPlayers;

        void Init()
        {
            MaxPlayers = Convert.ToInt32(Config["MaxPlayers"]);
            permission.RegisterPermission(perm, this);
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating a new configuration file");
            Config["MaxPlayers"] = 10;
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(
                new Dictionary<string, string> {
                    { "MaxPlayersExeeded", "<color=orange>Clan full!</color>" }
                }, this);
        }

        string Msg(string msg, string SteamId = null) => lang.GetMessage(msg, this, SteamId);

        object OnClanMemberAdd(Clan clan, PlayerSession applicant, PlayerSession session)
        {
            bool allowed = permission.UserHasPermission(applicant.SteamId.ToString(), perm);
            HashSet<ulong> members = clan.GetMemebers();
            if (members.Count >= MaxPlayers && !allowed)
            {
                Singleton<AlertManager>.Instance.GenericTextNotificationServer(Msg("MaxPlayersExeeded", applicant.SteamId.ToString()), applicant.Player);
                Singleton<AlertManager>.Instance.GenericTextNotificationServer(Msg("MaxPlayersExeeded", session.SteamId.ToString()), session.Player);
                return false;
            }
            return null;
        }
    }
}