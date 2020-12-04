using System;
using System.Collections.Generic;
using CodeHatch.Engine.Networking;
using CodeHatch.Networking.Events.Entities;
using CodeHatch.Networking.Events.Players;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("TopKDR", "PaiN/Mordeus", "0.3.3", ResourceId = 1525)]
    [Description("Kill and death ratio with a top list")]
    public class TopKDR : ReignOfKingsPlugin
    {
        private bool changed;        
        private object tags;
        public string ChatTitle;
        private bool EnableScoreTags;
        private bool AutoAnnouncement;
        private int AutoAnnouncementPlayers;
        private int AutoAnnouncementTime;

        private void LoadVariables()
        {
            ChatTitle = Convert.ToString(GetConfig("Settings", "Title", "[4F9BFF]Server:"));
            EnableScoreTags = Convert.ToBoolean(GetConfig("Settings", "EnableScoreTags", false));
            AutoAnnouncement = Convert.ToBoolean(GetConfig("AutoAnnounce", "AutomaticAnnouncement", false));
            AutoAnnouncementPlayers = Convert.ToInt32(GetConfig("AutoAnnounce", "AutomaticAnnouncementPlayers", 5));
            AutoAnnouncementTime = Convert.ToInt32(GetConfig("AutoAnnounce", "AutomaticAnnouncementTime(seconds)", 900));

            tags = GetConfig("ScoreTags", "Tags", new Dictionary<object, object>{
                {"[Tag1]", 5},
                {"(Tag2)", 10},
                {"[Tag3]", 15},
                {"{Tag4}", 20},
                {"$Tag5$", 25}
            });

            if (changed)
            {
                SaveConfig();
                changed = false;
            }
        }

        private object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                changed = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                changed = true;
            }
            return value;
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating a new configuration file!");
            Config.Clear();
            LoadVariables();
        }

        private StoredData data;

        private class StoredData
        {
            public Dictionary<ulong, int> Kills = new Dictionary<ulong, int>();
            public Dictionary<ulong, int> Deaths = new Dictionary<ulong, int>();
        }

        private void Init()
        {
            LoadVariables();
            LoadDefaultMessages();
            data = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("TopKDR_data");
            
        }
        private void OnServerInitialized()
        {
            if (AutoAnnouncement)
                timer.Repeat(AutoAnnouncementTime, 0, () => AutoAnnouncementStart(AutoAnnouncementPlayers));
        }
        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["SynError"] = "{Title} [FF0000]Syntax: /top[number] || ex. /top 5, /top 10[FFFFFF]",
                ["TopList"] = "[4F9BFF]Name: [FFFFFF]{0},[4F9BFF] Kills: [FFFFFF]{1}, [4F9BFF]Deaths: [FFFFFF]{2}, [4F9BFF]Score: [FFFFFF]{3}",
                ["TopTitle"] = "[4F9BFF]Top {0} Players:[FFFFFF]"
            }, this);
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject("TopKDR_data", data);

        private void OnEntityDeath(EntityDeathEvent e)
        {            
            if (e.Entity == null) return;
            if (e.KillingDamage == null) return;
            if (e.KillingDamage.DamageSource == null) return;
            if (e == null) return;
            if (e.KillingDamage.DamageSource == e.Entity) return;
            ulong victimid = e.Entity.OwnerId;
                ulong attackerid = e.KillingDamage.DamageSource.OwnerId;

                if (data.Kills.ContainsKey(attackerid))
                data.Kills[attackerid] = data.Kills[attackerid] + 1;
                else
                data.Kills.Add(attackerid, 1);

                if (data.Deaths.ContainsKey(victimid))
                data.Deaths[victimid] = data.Deaths[victimid] + 1;
                else
                data.Deaths.Add(victimid, 1);

                SaveData();
        }

        private void OnPlayerChat(PlayerEvent e)
        {
            if (!EnableScoreTags) return;

            var player = e.Player;
            player.DisplayNameFormat = $"{GetPlayerTag(player)} %name%";
        }

        [ChatCommand("top")]
        private void TopCommand(Player player, string command, string[] args)
        {
            string playerId = player.Id.ToString();
            var list = data.Kills.OrderByDescending(pair => pair.Value).ToList();            
            if (args.Length == 0)
            {
                SendReply(player, Message("SynError", playerId));
                return;
            }

            for (int i = 0; i < Convert.ToInt32(args[0]); i++)
            {
                if (list.Count < i + 1) break;

                var kills = list[i].Value;
                var deaths = 0;
                if (!data.Deaths.ContainsKey(list[i].Key))
                    deaths = 0;
                else
                    deaths = data.Deaths[list[i].Key];

                var score = kills - deaths;
                if (score <= 0) score = 0;                
                if (list[i].Key == 9999999999) continue;//removes server from list
                SendReply(player, Message("TopTitle", playerId), args[0]);
                SendReply(player, $"{i+1}. " + Message("TopList", playerId), FindPlayer(list[i].Key.ToString()).Name, kills.ToString(), deaths.ToString(), score.ToString());
                               
            }
        }

        private int GetPlayerScore(Player player)
        {
            var kills = 0;
            if (!data.Kills.ContainsKey(player.Id))
                kills = 0;
            else
                kills = data.Kills[player.Id];

            var deaths = 0;
            if (!data.Deaths.ContainsKey(player.Id))
                deaths = 0;
            else
                deaths = data.Deaths[player.Id];

            var score = kills - deaths;
            if (score <= 0) score = 0;

            return score;
        }
        private void AutoAnnouncementStart(int topamount)
        {                       
            foreach (var player in Server.AllPlayers)
            {
                if (player.Id == 9999999999) continue;
                string playerId = player.Id.ToString();
                var list = data.Kills.OrderByDescending(pair => pair.Value).ToList();
                
                for (int i = 0; i < topamount; i++)
                {
                    if (list.Count < i + 1) break;

                    var kills = list[i].Value;
                    var deaths = 0;
                    if (!data.Deaths.ContainsKey(list[i].Key))
                        deaths = 0;
                    else
                        deaths = data.Deaths[list[i].Key];

                    var score = kills - deaths;
                    if (score <= 0) score = 0;
                    if (list[i].Key == 9999999999) continue;//removes server from list
                    SendReply(player, Message("TopTitle", playerId), topamount);
                    SendReply(player, $"{i + 1}. " + Message("TopList", playerId), FindPlayer(list[i].Key.ToString()).Name, kills.ToString(), deaths.ToString(), score.ToString());                    
                }

            }
        }
        private string GetPlayerTag(Player player)
        {
            var playertag = "";
            foreach (var c in Config["ScoreTags", "Tags"] as Dictionary<string, object>)
            {
                if (GetPlayerScore(player) >= Convert.ToInt32(c.Value)) playertag = c.Key;
                return playertag;
            }
            return null;
        }        
        private string Message(string key, string id = null, params object[] args)
        {
            return lang.GetMessage(key, this, id).Replace("{Title}", ChatTitle);
        }
        private IPlayer FindPlayer(string playerid)
        {
            if (playerid == "9999999999") return null;
            return this.covalence.Players.FindPlayerById(playerid);
        }
    }
}
