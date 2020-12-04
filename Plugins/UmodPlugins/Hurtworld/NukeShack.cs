﻿using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Nuke Shack", "Mr. Blue", "1.0.0")]
    [Description("Allows players with permissions to remove their own shacks.")]
    class NukeShack : CovalencePlugin
    {
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string> {
                { "No Shack", "<color=orange>[Nuke Shack]</color> You do not have a shack!" },
                { "Shack Removed", "<color=orange>[Nuke Shack]</color> Your shack is removed!" }
            }, this);
        }

        string Msg(string msg, IPlayer player) => lang.GetMessage(msg, this, player.Id);

        [Command("nukeshack"), Permission("nukeshack.use")]
        private void removeShack(IPlayer player, string command, string[] args)
        {
            PlayerIdentity identity = (player.Object as PlayerSession).Identity;

            ShackDynamicServer shack = ShackDynamicServer.GetPlayerShack(identity);
            if(shack == null)
            {
                player.Reply(Msg("No Shack", player));
                return;
            }

            HNetworkManager.Instance.NetDestroy(shack.GetComponent<HNetworkView>());
            player.Reply(Msg("Shack Removed", player));
        }
    }
}