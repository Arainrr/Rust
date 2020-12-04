using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Biome Chat Prefix", "BuzZ", "1.0.2")]
    [Description("Adds a biome prefix to player chat")]
    public class BiomeChatPrefix : RustPlugin
    {
        [PluginReference]     
        Plugin BetterChat;

        const string ShowBiome = "biomechatprefix.show"; 

        bool debug = false;
        public List<BasePlayer> blabla = new List<BasePlayer>();
        bool ConfigChanged;

        bool color_use = false;
        bool color_auth2_use = false;
        string color_auth2 = "#FB6FFF";
        string color_arid = "#FFE952";
        string color_temperate = "#42B508";
        string color_tundra = "#DB8300";
        string color_arctic = "#52E3FF";
        string name_arid = "[ARID] ";
        string name_temperate = "[TEMPERATE] ";
        string name_tundra = "[TUNDRA] ";
        string name_arctic = "[ARCTIC] ";

        private void OnServerInitialized()
        {
            LoadVariables();
            permission.RegisterPermission(ShowBiome, this);   
            if (BetterChat != null && BetterChat.IsLoaded) Unsubscribe(nameof(OnPlayerChat));
        }

#region CONFIG

    protected override void LoadDefaultConfig()
        {
            LoadVariables();
        }

        private void LoadVariables()
        {
            color_use = Convert.ToBoolean(GetConfig("Color Settings", "Use colored Biome Prefix", false));
            color_auth2_use = Convert.ToBoolean(GetConfig("Color Settings", "Use colored server Admin (auth2) Prefix", false));
            color_auth2 = Convert.ToString(GetConfig("Color Settings", "server Admin", "#FB6FFF"));
            color_arid = Convert.ToString(GetConfig("Color Settings", "Biome ARID", "#FFE952"));
            color_temperate = Convert.ToString(GetConfig("Color Settings", "Biome TEMPERATE", "#42B508"));
            color_tundra = Convert.ToString(GetConfig("Color Settings", "Biome TUNDRA", "#DB8300"));
            color_arctic = Convert.ToString(GetConfig("Color Settings", "Biome ARCTIC", "#52E3FF"));
            name_arid = Convert.ToString(GetConfig("Name Settings", "Biome ARID", "[ARID] "));
            name_temperate = Convert.ToString(GetConfig("Name Settings", "Biome TEMPERATE", "[TEMPERATE] "));
            name_tundra = Convert.ToString(GetConfig("Name Settings", "Biome TUNDRA", "[TUNDRA] "));
            name_arctic = Convert.ToString(GetConfig("Name Settings", "Biome ARCTIC", "[ARCTIC] "));

            if (!ConfigChanged) return;
            SaveConfig();
            ConfigChanged = false;
        }

        private object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                ConfigChanged = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                ConfigChanged = true;
            }
            return value;
        }

#endregion

////////////////////////////////////////////

#region PLAYERCHAT
        object OnPlayerChat(BasePlayer player, string message)
        {
            //if (BetterChat) return;
            if (debug) Puts("OnPlayerChat");
            if (blabla.Contains(player)) return false;
            string biomeprefix = LetsStringThisBiome(player.transform.position);
            if (biomeprefix != "NOBIOME")
            {
                blabla.Add(player);
                timer.Once(1f, () =>
                {
                    blabla.Remove(player);                    
                });
                string playername = player.displayName + " ";
                if (player.net.connection.authLevel == 2 && color_auth2_use) playername = $"<color={color_auth2}>{playername}</color>";
                if (debug) Puts("OnPlayerChat - is in biome !");
                if (!color_use) Server.Broadcast(message, $"{biomeprefix} {playername}", player.userID);
                else Server.Broadcast(message, $"{biomeprefix} {playername}", player.userID);
                return false;
            }
            else
            {
                if (debug) Puts("Not in Biome");
                return null;
            }

        }
#endregion

        object OnBetterChat(Dictionary<string, object> data)
        {
            string chatbetter = data["Message"].ToString();
            IPlayer iplayer = data["Player"] as IPlayer;
            BasePlayer player = iplayer?.Object as BasePlayer;
            string biomeprefix = LetsStringThisBiome(player.transform.position);
            chatbetter = biomeprefix + chatbetter;
            data["Text"] = chatbetter;
            return data;
        }

#region BIOME GET
        string LetsStringThisBiome(Vector3 Position)
        {
// arid
           if (TerrainMeta.BiomeMap.GetBiome(Position, 1) > 0.5f )
           {
                if (debug) Puts($"arid > 0.5");
                if (color_use) return $"<color={color_arid}>{name_arid} </color>";
                else return $"{name_arid} ";
           }
// temperate
           if (TerrainMeta.BiomeMap.GetBiome(Position, 2) > 0.5f )
           {
                if (debug) Puts($"temperate > 0.5");
                if (color_use) return $"<color={color_temperate}>{name_temperate} </color>";
                else return $"{name_temperate} ";
           }
//tundra
           if (TerrainMeta.BiomeMap.GetBiome(Position, 4) > 0.5f )
           {
                if (debug) Puts($"tundra > 0.5");
                if (color_use) return $"<color={color_tundra}>{name_tundra} </color>";
                else return $"{name_tundra} ";
           }           
//arctic
           if (TerrainMeta.BiomeMap.GetBiome(Position, 8) > 0.5f )
           {
                if (debug) Puts($"arctic > 0.5");
                if (color_use) return $"<color={color_arctic}>{name_arctic} </color>";
                else return $"{name_arctic} ";
           }
           return "NOBIOME";
        }
#endregion
    }
}