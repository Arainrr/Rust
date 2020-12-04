﻿using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Troll Ban System", "Grave", "0.5.0")]
    [Description("A easier way to troll ban someone in rust! doing /tban announces a server message where it says that a player was banned, and /tunban announces that a player was unbanned all without actually banning or unbanning anyone!")]
    class TrollBanSystem : CovalencePlugin
    {
        private void Init()
        {
            permission.RegisterPermission("trollbansystem.ban", this);
            permission.RegisterPermission("trollbansystem.unban", this);
        }
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["BanMessage"] = "{Banner} banned {Bannedname}",
                ["UnBanMessage"] = "{UnBanner} unbanned {Bannedname}"
            }, this);
        }
       

        [Command("tban"),Permission("trollbansystem.ban")]
        private void Cmdtban(IPlayer player, string command, string[] args)
        {
            server.Broadcast($"{player.Name} banned {string.Join(" ", args)}");
            string message = lang.GetMessage("BanMessage", this, player.Id)
     .Replace("{Banner}", player.Name)
     .Replace("{Bannedname}", string.Join(" ", args));
            Puts(message);
        }

        [Command("tunban"),Permission("trollbansystem.unban")]
        private void Cmdtunban(IPlayer player, string command, string[] args)
        {
            server.Broadcast($"{player.Name} unbanned {string.Join(" ", args)}");
            string message = lang.GetMessage("UnBanMessage", this, player.Id)
     .Replace("{UnBanner}", player.Name)
     .Replace("{Bannedname}", string.Join(" ", args));
            Puts(message);
        }

    }
}
