using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Map Flood", "ziptie", "0.4.1")]
    [Description("Floods the map on command")]
    public class MapFlood : CovalencePlugin
    {
        private Timer floodTimer;

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["FloodingStart"] = "Flooding map",
                ["FloodingStop"] = "Flooding stopped",
                ["FloodingStopped"] = "Flooding already stopped",
                ["ResetOceanLevel"] = "Reset ocean level to 0",
                ["SyntaxError"] = "Incorrect syntax, use /flood <level> <rate>"
            }, this);
        }

        [Command("flood"), Permission("mapflood.use")]
        private void FloodCommand(IPlayer player, string command, string[] args)
        {
            float floodLevel;
            float floodRate;

            if (args.Length == 2 && float.TryParse(args[0], out floodLevel) && float.TryParse(args[1], out floodRate))
            {
                player.Reply(lang.GetMessage("FloodingStart", this, player.Id));
                floodTimer = timer.Every(floodRate, () =>
                {
                    server.Command("meta.add", "oceanlevel", floodLevel);
                });
                return;
            }

            player.Reply(lang.GetMessage("SyntaxError", this, player.Id));
        }

        [Command("stopflood"), Permission("mapflood.use")]
        private void StopFloodCommand(IPlayer player, string command, string[] args)
        {
            if (!(floodTimer != null && !floodTimer.Destroyed))
            {
                player.Reply(lang.GetMessage("FloodingStopped", this, player.Id));
            }
            else
            {
                floodTimer.Destroy();
                player.Reply(lang.GetMessage("FloodingStop", this, player.Id));
            }
        }

        [Command("resetoceanlevel"), Permission("mapflood.resetoceanlevel")]
        private void ResetOceanLevel(IPlayer player, string command, string[] args)
        {
            server.Command("oceanlevel", 0);
            player.Reply(lang.GetMessage("ResetOceanLevel", this, player.Id));
        }
    }
}
