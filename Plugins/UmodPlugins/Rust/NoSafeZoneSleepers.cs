﻿// Requires: ZoneManager



using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System.Collections.Generic;


namespace Oxide.Plugins
{
    [Info("No Safe Zone Sleepers", "NooBlet", "1.5")]
    [Description("Automaticly Creates a Zone to remove sleeping players from Outpost and Bandit Camp")]
    public class NoSafeZoneSleepers : CovalencePlugin
    {
        List<string> createdZones = new List<string>();
        int number = 0;

        #region Config

        protected override void LoadDefaultConfig()
        {
            LogWarning("Creating a new configuration file");

            Config["1. Kill Sleepers on disconnect"] = false;   // Default is false . If true seepers will be ejected.         
            Config["2. Time to delay Kill"] = 60;               // Default is 60 . denomination in minutes.            
            Config["3. OupostZoneRadius"] = 150;                // less than 150 might still place player in safezone.
            Config["4. BanditCampZoneRadius"] = 150;
            Config["5. Use Enter and Leave Messages?"] = false;  // Default is false 
            Config["6. Enter Message"] = "You are now entering a No Sleep Zone";
            Config["7. Leave Message"] = "You are now Leaving a No Sleep Zone";
            Config["8. Enter/Leave Message color"] = "#95a5a6";
         
        }



        #endregion config

        
        #region Hooks

        [PluginReference]
        private Plugin ZoneManager;


        private void OnServerInitialized()
        {
            AddZones(); 
        }

        void Unload()
        {
            ClearZones();
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            RemoveSleeper(player);
        }

        #endregion Hooks

        
        #region Methods


        private void RemoveSleeper(BasePlayer sleeper)
        {
            foreach (string zone in createdZones)
            {
                if (ZoneManager.Call<bool>("isPlayerInZone", zone, sleeper))
                {
                    if ((bool)Config["1. Kill Sleepers on disconnect"])
                    {
                        float time = float.Parse(Config["2. Time to delay Kill"].ToString()) * 60;
                        timer.Once(time, () =>
                        {
                            if (!sleeper.IsConnected)
                            {
                               sleeper.Kill();
                            }
                        });

                       
                    }
                    else
                    {
                        Addflag(zone);
                    }
                    
                }
            }
        }
       

        private void Addflag(string zone)
        {
            ZoneManager?.Call("AddFlag", zone, "ejectsleepers");
           

            timer.Once(10f, () =>
            {
               ZoneManager?.Call("RemoveFlag", zone, "ejectsleepers");
            });

        }

        private void ClearZones()
        {
            if (createdZones != null)
            {
                foreach (string zone in createdZones)
                {
                    ZoneManager?.Call("EraseZone", zone);
                    Puts($"{zone} has been Removed");
                }
            }
           
        }

        private void AddZones()
        {
           

            foreach (var current in TerrainMeta.Path.Monuments)
            {
                if (current.name == "assets/bundled/prefabs/autospawn/monument/medium/compound.prefab" || current.name.Contains("assets/bundled/prefabs/autospawn/monument/medium/bandit_town.prefab"))
                {
                    string[] messages = new string[4];
                    string name = "";
                    if ((bool)Config["5. Use Enter and Leave Messages?"])
                    {
                         messages = new string[8];
                    }
                    else
                    {
                         messages = new string[4];
                    }

                    if (current.displayPhrase.english.StartsWith("Bandit"))
                    {
                        name = "BanditCamp";
                    }
                    else
                    {
                        name = "OutPost";
                    }
                   
                    string zoneId = $"{name}.{number}";
                    string friendlyname = name;
                    string ID = zoneId;

                    
                    messages[0] = "name";
                    messages[1] = friendlyname;
                    //messages[2] = "ejectsleepers";
                    //messages[3] = "true";
                    messages[2] = "radius";
                    if (name == "OutPost")
                    {
                        messages[3] = Config["3. OupostZoneRadius"].ToString();
                    }
                    else
                    {
                        messages[3] = Config["4. BanditCampZoneRadius"].ToString();
                    }

                    if ((bool)Config["5. Use Enter and Leave Messages?"])
                    {
                        string entermsg = $"<color=red>[NSZS]</color> :<color={Config["8. Enter/Leave Message color"]}> {Config["6. Enter Message"].ToString()} </color> ";
                        string leavemsg = $"<color=red>[NSZS]</color> :<color={Config["8. Enter/Leave Message color"]}> {Config["7. Leave Message"].ToString()} </color> ";

                        messages[4] = "enter_message";
                        messages[5] = entermsg;
                        messages[6] = "leave_message";
                        messages[7] = leavemsg;
                    }

                    ZoneManager?.CallHook("CreateOrUpdateZone", ID, messages, current.transform.position);
                    number++;
                    createdZones.Add(zoneId);
                    Puts($"{ID} has been created");
                }
            }
        }



        #endregion Methods


        //#region Debug Testing
        //private BasePlayer findPlayer(string name)
        //{
        //    BasePlayer target = BasePlayer.FindAwakeOrSleeping(name);

        //    return target;
        //}

        //[Command("sleep")]
        //private void sleepCommand(IPlayer iplayer, string command, string[] args)
        //{
        //    BasePlayer player = findPlayer(iplayer.Id);
        //    player.StartSleeping();
        //}

        //#endregion Debug Testing
    }
}