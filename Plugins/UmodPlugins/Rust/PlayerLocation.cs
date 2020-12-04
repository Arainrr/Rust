﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using ConVar;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;


namespace Oxide.Plugins
{
    [Info("Player Location", "seacowz", "0.1.4")]
    [Description("Find the location of players on the server.")]

    // Revision history
    //
    // 0.1.0 Initial release.
    // 0.1.1 Code cleanup.  Reports no players found on empty searches.
    // 0.1.2 Console now shows Steam ID number.  Can also search by Steam ID.
    // 0.1.3 Now shows when a player is flying.  Converted to CovalencePlugin.
    // 0.1.4 Added amount of time sleeping in console and sorts players by sleep time.

    public class PlayerLocation : CovalencePlugin
    {
        #region Variables

        const string permstring = "playerlocation.admin";
        const string datafilename = "PlayerLocation";

        Dictionary<string, DateTime> lastawake = new Dictionary<string, DateTime>();

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You do not have permission to use that command.",
                ["NotFound"] = "No players found."
            }, this);
        }

        #endregion Localization

        #region Initialization

        private void Init()
        {
            #if !RUST
                throw new NotSupportedException("This plugin does not support this game.");
            #endif

            permission.RegisterPermission(permstring, this);
        }

        #endregion Initialization

        #region Oxide hooks

        void Loaded ()
        {
            LoadSleepTimes();
            UpdateSleepTimes();
            SaveSleepTimes();
        }

        void OnPlayerSleep(BasePlayer player)
        {
            string id = player.UserIDString;

            if (!lastawake.ContainsKey(id))
            {
                lastawake[id] = DateTime.Now;
            }
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            string id = player.UserIDString;
            lastawake.Remove(id);
        }

        void Unload ()
        {
            SaveSleepTimes();
        }

        #endregion Oxide hooks

        #region Commands

        [Command("location")]
        void LocationCmd(IPlayer player, string command, string[] args)
        {
            if (player.HasPermission(permstring) == false)
            {
                player.Reply(lang.GetMessage("NoPermission", this, player.Id));
                return;
            }

            string search = string.Empty;

            if (args.Length > 0)
            {
                search = args[0];
            }

            List<Tuple<double, string>> locationlist = new List<Tuple<double, string>>();

            FindLocation(player, search, ref locationlist);

            if (locationlist.IsEmpty())
            {
                player.Reply(lang.GetMessage("NotFound", this, player.Id));
            }
            else
            {
                foreach (Tuple<double, string> item in locationlist.OrderByDescending(i => i.Item1))
                {
                    player.Reply(item.Item2);
                }
            }
        }

        #endregion Commands

        #region Helper

        void LoadSleepTimes()
        {
            DynamicConfigFile file = Interface.Oxide.DataFileSystem.GetDatafile(datafilename);
            lastawake = file.ReadObject<Dictionary<string, DateTime>>();
        }

        void UpdateSleepTimes()
        {
            foreach (BasePlayer player in BasePlayer.allPlayerList)
            {
                string id = player.UserIDString;
                
                if (player.IsSleeping() && !lastawake.ContainsKey(id))
                {
                    lastawake[id] = DateTime.Now;
                }
            }
        }

        void SaveSleepTimes()
        {
            DynamicConfigFile file = Interface.Oxide.DataFileSystem.GetDatafile(datafilename);
            file.WriteObject<Dictionary<string, DateTime>>(lastawake);
        }

        void FindLocation(IPlayer callingplayer, string search, ref List<Tuple<double, string>> locationlist)
        {
            bool console = false;

            if (callingplayer.LastCommand == CommandType.Console)
            {
                console = true;
            }
            
            foreach (BasePlayer player in BasePlayer.allPlayerList)
            {
                string options = string.Empty;
                string name = player.displayName;
                string id = player.UserIDString;
                double sleeptime = 0;

                if (search.Length > 0 && name.Contains(search, System.Globalization.CompareOptions.IgnoreCase) == false && id.Contains(search) == false)
                {
                    continue;
                }

                if (player.IsSleeping())
                {
                    DateTime date;

                    if (lastawake.TryGetValue(id, out date))
                    {
                        TimeSpan diff = DateTime.Now - date;
                        sleeptime = diff.TotalSeconds;
                    }

                    if (!console || sleeptime <= 0)
                    {
                        options += " (sleeping)";
                    }
                    else if (sleeptime < 3600) // hour
                    {
                        options += " (sleeping " + Math.Floor(sleeptime / 60) + " minutes)";
                    }
                    else if (sleeptime < 86400) // day
                    {
                        options += " (sleeping " + Math.Floor(sleeptime / 3600) + " hours)";
                    }
                    else
                    {
                        options += " (sleeping " + Math.Floor(sleeptime / 86400) + " days)";
                    }
                }
                else if (player.IsDead())
                {
                    options += " (dead)";
                }
                else if (player.IsFlying)
                {
                    options += " (flying)";
                }

                if (player.IsAdmin)
                {
                    options += " (admin)";
                }

                if (player.IsGod())
                {
                    options += " (god)";
                }

                Vector3 location = player.transform.position;

                string pos = MapPosition(location);
                string locationstr;

                if (console)
                {
                    locationstr = string.Concat(id, " ", name, options, ": ", pos, ", ", location.x, ", ", location.z);
                }
                else
                {
                    locationstr = string.Concat(name, options, ": ", pos, ", ", location.x, ", ", location.z);
                }

                locationlist.Add(Tuple.Create(sleeptime, locationstr));
            }
        }

        string MapPosition (Vector3 position)
        {
            var chars = new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "AA", "AB", "AC", "AD", "AE", "AF", "AG", "AH", "AI", "AJ", "AK", "AL", "AM", "AN", "AO", "AP", "AQ", "AR", "AS", "AT", "AU", "AV", "AW", "AX", "AY", "AZ" };

            const float block = 146;

            float size = ConVar.Server.worldsize;
            float offset = size / 2;

            float xpos = position.x + offset;
            float zpos = position.z + offset;

            int maxgrid = (int)(size / block);

            float xcoord = Mathf.Clamp(xpos / block, 0, maxgrid - 1);
            float zcoord = Mathf.Clamp(maxgrid - (zpos / block), 0, maxgrid - 1);

            string pos = string.Concat(chars[(int)xcoord], (int)zcoord);

            return (pos);
        }

        #endregion Helper
    }
}
