﻿using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Corpse Location", "shinnova", "2.3.1")]
    [Description("Allows users to locate their latest corpse")]

    class CorpseLocation : RustPlugin
    {
        [PluginReference]
        Plugin ZoneManager;

        #region Variable Declaration
        const string UsePerm = "corpselocation.use";
        const string TPPerm = "corpselocation.tp";
        const string VIPPerm = "corpselocation.vip";
        const string AdminPerm = "corpselocation.admin";
        const float calgon = 0.0066666666666667f;
        float WorldSize = (ConVar.Server.worldsize);
        Dictionary<string, Vector3> InternalGrid = new Dictionary<string, Vector3>();
        Dictionary<string, Timer> ActiveTimers = new Dictionary<string, Timer>();
        Dictionary<string, Vector3> ReturnLocations = new Dictionary<string, Vector3>();
        #endregion

        #region Config
        Configuration config;
        public class Configuration
        {
            [JsonProperty(PropertyName = "Show grid location")]
            public bool showGrid { get; set; } = true;
            [JsonProperty(PropertyName = "Track a corpse's location for x seconds")]
            public int trackTime { get; set; } = 30;
            [JsonProperty(PropertyName = "Allow teleporting to own corpse x times per day (0 for unlimited)")]
            public int tpAmount { get; set; } = 5;
            [JsonProperty(PropertyName = "Allow teleporting to own corpse x times per day (0 for unlimited), for VIPs")]
            public int viptpAmount { get; set; } = 10;
            [JsonProperty(PropertyName = "Allow returning to original location after teleporting")]
            public bool allowReturn { get; set; } = false;
            [JsonProperty(PropertyName = "Countdown until teleporting to own corpse (0 for instant tp)")]
            public float tpCountdown { get; set; } = 5f;
            [JsonProperty(PropertyName = "Block teleports into Zone Manager's tp blocked zones")]
            public bool blockToZM { get; set; } = true;
            [JsonProperty(PropertyName = "Block teleports from Zone Manager's tp blocked zones")]
            public bool blockFromZM { get; set; } = true;
            [JsonProperty(PropertyName = "Block teleports into building blocked areas")]
            public bool blockToBuildBlocked { get; set; } = false;
            [JsonProperty(PropertyName = "Reset players' remaining teleports at this time (HH:mm:ss format)")]
            public string resetTime { get; set; } = "00:00:00";
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                    LoadDefaultConfig();
            }
            catch
            {
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            Puts("No or faulty config detected. Generating new configuration file");
            config = new Configuration();
        }

        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Data
        public class StoredData
        {
            public Dictionary<string, List<float>> deaths = new Dictionary<string, List<float>>();
            public Dictionary<string, int> teleportsRemaining = new Dictionary<string, int>();
            public Dictionary<string, Vector3> GridInfo = new Dictionary<string, Vector3>();
            public StoredData(){}
        }

        StoredData storedData;

        void SaveData() => Interface.Oxide.DataFileSystem.WriteObject("CorpseLocation", storedData);
        #endregion

        #region Hooks
        void OnNewSave(string filename)
        {
            NewData();
        }

        void OnServerInitialized()
        {
            permission.RegisterPermission(UsePerm, this);
            permission.RegisterPermission(TPPerm, this);
            permission.RegisterPermission(VIPPerm, this);
            permission.RegisterPermission(AdminPerm, this);
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("CorpseLocation");
            if (storedData == null || storedData.deaths == null)
            {
                Puts("Faulty data detected. Generating new data file");
                NewData();
            }
            if (storedData.GridInfo == null || storedData.GridInfo.Count == 0) CreateGrid();
            InternalGrid = storedData.GridInfo;
            timer.Every(1f, () => {
                if (System.DateTime.Now.ToString("HH:mm:ss") == config.resetTime)
                {
                    foreach (string PlayerID in storedData.teleportsRemaining.Keys.ToList())
                        if (permission.UserHasPermission(PlayerID, VIPPerm))
                            storedData.teleportsRemaining[PlayerID] = config.viptpAmount;
                        else
                            storedData.teleportsRemaining[PlayerID] = config.tpAmount;
                    SaveData();
                    Puts("Daily teleports were reset.");
                }
            });
        }
        new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["YouDied"] = "Your corpse was last seen {0} meters from here.",
                ["YouDiedGrid"] = "Your corpse was last seen {0} meters from here, in {1}.",
                ["TeleportingIn"] = "Teleporting to your corpse in {0} second(s).",
                ["TeleportBlockedCorpse"] = "Your corpse is in a restricted area, preventing teleportation.",
                ["TeleportBlockedPlayer"] = "You are not allowed to teleport from here.",
                ["ArrivedAtYourCorpse"] = "You have arrived at your corpse.",
                ["ArrivedAtTheCorpse"] = "You have arrived at the corpse of {0}.",
                ["ReturnAvailable"] = "You can use <color=#ffa500ff>/return</color> to return to your initial location.",
                ["ReturnUnavailable"] = "You don't have a location set to return to.",
                ["ReturnUsed"] = "You have successfully returned to your initial location.",
                ["OutOfTeleports"] = "You have no more teleports left today.",
                ["TeleportsRemaining"] = "You have {0} teleports remaining today.",
                ["UnknownLocation"] = "Your last death location is unknown.",
                ["UnknownLocationTarget"] = "{0}'s last death location is unknown.",
                ["NeedTarget"] = "You need to specify a player to teleport to the corpse of, using either their name or steam id.",
                ["InvalidPlayer"] = "{0} is not part of a known player's name/id.",
                ["NotAllowed"] = "You do not have permission to use that command.",
            }, this);
        }

        void OnPlayerRespawned(BasePlayer player)
        {
            if (storedData.deaths.ContainsKey(player.UserIDString))
                SendCorpseLocation(player);
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            var player = entity as BasePlayer;
            if (player != null)
            {
                if (entity.IsNpc)
                    return;
                string UserID = player.UserIDString;
                Vector3 DeathPosition = entity.transform.position;
                List<float> ShortDeathPosition = new List<float> { DeathPosition.x, DeathPosition.y, DeathPosition.z };
                storedData.deaths[UserID] = ShortDeathPosition;
                SaveData();
                Puts($"{player.displayName} ({UserID}) died at {DeathPosition}");
            }
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            var corpse = entity as BaseCorpse;
            if (corpse != null)
            {
                if (!corpse is PlayerCorpse || !corpse?.parentEnt?.ToPlayer() || corpse.parentEnt.ToPlayer().IsNpc)
                    return;
                BasePlayer player = corpse.parentEnt.ToPlayer();
                string UserID = player.UserIDString;
                if (ActiveTimers.ContainsKey(UserID))
                {
                    ActiveTimers[UserID].Destroy();
                    ActiveTimers.Remove(UserID);
                }
                Timer CorpseChecker = timer.Repeat(1, config.trackTime, () => {
                    if (!corpse.IsDestroyed)
                    {
                        Vector3 BodyLocation = corpse.transform.position;
                        List<float> ShortBodyLocation = new List<float> { BodyLocation.x, BodyLocation.y, BodyLocation.z };
                        storedData.deaths[UserID] = ShortBodyLocation;
                        SaveData();
                    }
                });
                ActiveTimers[UserID] = CorpseChecker;
            }
        }

        void OnUserPermissionGranted(string id, string permName)
        {
            if (permName == VIPPerm)
            {
                storedData.teleportsRemaining[id] += config.viptpAmount - config.tpAmount;
                SaveData();
            }
        }
        #endregion

        #region Functions
        void NewData()
        {
            storedData = new StoredData();
            SaveData();
            CreateGrid();
            InternalGrid = storedData.GridInfo;
        }

        float GetStepSize()
        {
            float GridWidth = (calgon * WorldSize);
            return WorldSize / GridWidth;
        }

        void CreateGrid()
        {
            if (storedData.GridInfo == null)
            {
                Dictionary<string, List<float>> DeathsBackup = storedData.deaths;
                storedData = new StoredData();
                storedData.deaths = DeathsBackup;
            }
            if (storedData.GridInfo.Count > 0) storedData.GridInfo.Clear();
            float offset = WorldSize / 2;
            float step = GetStepSize();
            string start = "";

            char letter = 'A';
            int number = 0;

            for (float zz = offset; zz > -offset; zz -= step)
            {
                for (float xx = -offset; xx < offset; xx += step)
                {
                    Vector3 GridStart = new Vector3(xx, 0, zz);
                    string GridReference = $"{start}{letter}{number}";
                    storedData.GridInfo.Add(GridReference, GridStart);
                    if (letter.ToString().ToUpper() == "Z")
                    {
                        start = "A";
                        letter = 'A';
                    }
                    else
                    {
                        letter = (char)(((int)letter) + 1);
                    }


                }
                number++;
                start = "";
                letter = 'A';
            }
            SaveData();
        }

        string GetGrid(Vector3 DeathLocation)
        {
            string DeathGrid = "the unknown";
            foreach (var Grid in InternalGrid)
            {
                if (DeathLocation.x >= Grid.Value.x && DeathLocation.x < Grid.Value.x + GetStepSize() && DeathLocation.z <= Grid.Value.z && DeathLocation.z > Grid.Value.z - GetStepSize())
                {
                    DeathGrid = Grid.Key;
                    break;
                }
            }
            return DeathGrid;
        }

        void SendCorpseLocation(BasePlayer player)
        {
            List<float> ShortDeathLocation = storedData.deaths[player.UserIDString];
            Vector3 DeathLocation = new Vector3(ShortDeathLocation[0], ShortDeathLocation[1], ShortDeathLocation[2]);
            int DistanceToCorpse = (int)Vector3.Distance(player.transform.position, DeathLocation);
            string DeathGrid = GetGrid(DeathLocation);
            if (config.showGrid)
                SendReply(player, String.Format(lang.GetMessage("YouDiedGrid", this, player.UserIDString), DistanceToCorpse, DeathGrid));
            else
                SendReply(player, String.Format(lang.GetMessage("YouDied", this, player.UserIDString), DistanceToCorpse));
        }

        Dictionary<ulong, string> GetPlayers(string NameOrID)
        {
            var pl = covalence.Players.FindPlayers(NameOrID).ToList();
            return pl.Select(p => new KeyValuePair<ulong, string>(ulong.Parse(p.Id), p.Name)).ToDictionary(x => x.Key, x => x.Value);
        }
        #endregion

        #region Commands
        [ChatCommand("where")]
        void whereCommand(BasePlayer player, string command, string[] args)
        {
            string PlayerID = player.UserIDString;
            if (args.Length > 0 && args[0] == "tp" && permission.UserHasPermission(PlayerID, TPPerm))
            {
                int TPAllowed = config.tpAmount;
                if (permission.UserHasPermission(PlayerID, VIPPerm))
                    TPAllowed = config.viptpAmount;
                if (!storedData.teleportsRemaining.ContainsKey(PlayerID) || storedData.teleportsRemaining[PlayerID] > TPAllowed)
                {
                    storedData.teleportsRemaining[PlayerID] = TPAllowed;
                    SaveData();
                }
                if (!storedData.deaths.ContainsKey(PlayerID))
                {
                    SendReply(player, lang.GetMessage("UnknownLocation", this, PlayerID));
                    return;
                }
                if (config.blockFromZM && ZoneManager && (bool)ZoneManager.CallHook("EntityHasFlag", player.GetEntity(), "notp"))
                {
                    SendReply(player, lang.GetMessage("TeleportBlockedPlayer", this, PlayerID));
                    return;
                }
                List<float> TargetCorpse = storedData.deaths[PlayerID];
                Vector3 destination = new Vector3(TargetCorpse[0], TargetCorpse[1], TargetCorpse[2]);
                if (TPAllowed > 0 && storedData.teleportsRemaining[PlayerID] == 0)
                    SendReply(player, lang.GetMessage("OutOfTeleports", this, PlayerID));
                else
                {
                    float tpCd = config.tpCountdown;
                    if (tpCd > 0)
                        SendReply(player, String.Format(lang.GetMessage("TeleportingIn", this, PlayerID), tpCd));
                    timer.Once(tpCd, () => {
                        Vector3 originalpos = player.transform.position;
                        player.Teleport(destination);
                        timer.Once(0.1f, () => {
                            if ((config.blockToZM && ZoneManager && (bool)ZoneManager.CallHook("EntityHasFlag", player.GetEntity(), "notp")) || (config.blockToBuildBlocked && player.IsBuildingBlocked()))
                            {
                                player.Teleport(originalpos);
                                SendReply(player, lang.GetMessage("TeleportBlockedCorpse", this, PlayerID));
                                return;
                            }
                            SendReply(player, lang.GetMessage("ArrivedAtYourCorpse", this, PlayerID));
                            if (config.allowReturn)
                            {
                                ReturnLocations.Add(PlayerID, originalpos);
                                SendReply(player, lang.GetMessage("ReturnAvailable", this, PlayerID));
                            }
                            if (TPAllowed > 0)
                            {
                                storedData.teleportsRemaining[PlayerID]--;
                                SaveData();
                                SendReply(player, String.Format(lang.GetMessage("TeleportsRemaining", this, PlayerID), storedData.teleportsRemaining[PlayerID]));
                            }
                        });
                    });
                }
                return; 
            }
            if (permission.UserHasPermission(PlayerID, UsePerm))
            {
                if (storedData.deaths.ContainsKey(player.UserIDString))
                    SendCorpseLocation(player);
                else
                    SendReply(player, lang.GetMessage("UnknownLocation", this, PlayerID));
            }
            else
                SendReply(player, lang.GetMessage("NotAllowed", this, PlayerID));
        }

        [ChatCommand("return")]
        void returnCommand(BasePlayer player, string command, string[] args)
        {
            string PlayerID = player.UserIDString;
            if (permission.UserHasPermission(PlayerID, TPPerm))
            {
                if (!ReturnLocations.ContainsKey(PlayerID))
                {
                    SendReply(player, lang.GetMessage("ReturnUnavailable", this, PlayerID));
                    return;
                }
                player.Teleport(ReturnLocations[PlayerID]);
                SendReply(player, lang.GetMessage("ReturnUsed", this, PlayerID));
                ReturnLocations.Remove(PlayerID);
            }
            else
                SendReply(player, lang.GetMessage("NotAllowed", this, PlayerID));
        }

        [ChatCommand("tpcorpse")]
        void tpCommand(BasePlayer player, string command, string[] args)
        {
            string PlayerID = player.UserIDString;
            if (permission.UserHasPermission(PlayerID, AdminPerm))
            {
                if (args.Length == 0)
                {
                    SendReply(player, lang.GetMessage("NeedTarget", this, PlayerID));
                    return;
                }
                Dictionary<ulong, string> FoundPlayers = GetPlayers(args[0]);
                if (FoundPlayers.Count == 0)
                {
                    SendReply(player, String.Format(lang.GetMessage("InvalidPlayer", this, PlayerID), args[0]));
                    return;
                }
                string TargetID = FoundPlayers.First().Key.ToString();
                if (storedData.deaths.ContainsKey(TargetID))
                {
                    List<float> TargetCorpse = storedData.deaths[TargetID];
                    Vector3 destination = new Vector3(TargetCorpse[0], TargetCorpse[1], TargetCorpse[2]);
                    player.Teleport(destination);
                    SendReply(player, String.Format(lang.GetMessage("ArrivedAtTheCorpse", this, PlayerID), FoundPlayers.First().Value));
                }
                else
                    SendReply(player, String.Format(lang.GetMessage("UnknownLocationTarget", this, PlayerID), FoundPlayers.First().Value));
            }
            else
                SendReply(player, lang.GetMessage("NotAllowed", this, PlayerID));
        }
        #endregion
    }
}