using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Item Ping Cooldown", "Mr. Blue", "0.0.2")]
    [Description("Adds cooldown to item pinging")]

    class ItemPingCooldown : HurtworldPlugin
    {
        private Dictionary<PlayerSession, DateTime> playerPings = new Dictionary<PlayerSession, DateTime>();
        private float pingCooldown;

        void Init()
        {
            pingCooldown = Config.Get<float>("Cooldown");
        }

        protected override void LoadDefaultConfig()
        {
            if (Config["Cooldown"] == null) Config.Set("Cooldown", 60f);
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(
                new Dictionary<string, string> {
                    { "ItemPingOnCooldown", "<color=orange>Anti Item Ping Spam, Disabled for {time} seconds</color>" }
                }, this);
        }
        string Msg(string msg, string SteamId = null) => lang.GetMessage(msg, this, SteamId);

        object OnPlayerItemPing(PlayerSession session, ItemPingMessage message)
        {
            if (playerPings.ContainsKey(session))
            {
                DateTime lastMessage = playerPings[session];
                DateTime timeNow = DateTime.Now;
                if (timeNow < lastMessage.AddSeconds(pingCooldown))
                {
                    double secondsLeft = (lastMessage.AddSeconds(pingCooldown) - timeNow).TotalSeconds; 
                    AlertManager.Instance.GenericTextNotificationServer(Msg("ItemPingOnCooldown", session.SteamId.ToString()).Replace("{time}", Math.Round(secondsLeft).ToString()), session.Player);

                    return true;
                }
                playerPings.Remove(session);
            }
            playerPings.Add(session, DateTime.Now);
            return null;
        }
    }
}