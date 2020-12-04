﻿using Newtonsoft.Json;
using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
    [Info("Remote Explosives", "Orange Doggo", "1.1.2")]
    [Description("Allows you to detonate explosives remotely with a RF Transmitter.")]

    public class RemoteExplosive : RustPlugin
    {
        private Dictionary<ulong, int> frequencies = new Dictionary<ulong, int>();
        private const string usePerm = "remoteexplosive.use";
        private Dictionary<ulong, bool> toggles = new Dictionary<ulong, bool>();

        private bool HasPermission(BasePlayer plr, string name)
        {
            return permission.UserHasPermission(plr.UserIDString, name);
        }

        void OnServerInitialized()
        {
            foreach (BasePlayer plr in BasePlayer.activePlayerList)
            {
                InitializePlayer(plr);
            }
        }

        void Init()
        {
            permission.RegisterPermission(usePerm, this);
            LoadData();
        }

        void Unload()
        {
            foreach (BasePlayer plr in BasePlayer.activePlayerList)
            {
                if (_data.players.ContainsKey(plr.userID))
                    _data.players[plr.userID] = frequencies[plr.userID];
                OnPlayerDisconnected(plr, string.Empty);
            }
            SaveData();

            _data = null;
            _config = null;
            foreach(TimedExplosive explosive in GameObject.FindObjectsOfType<TimedExplosive>())
            {
                FrequencyListener obj = explosive.GetComponent<FrequencyListener>();
                if (obj != null)
                {
                    GameObject.Destroy(obj);
                    explosive.SetFuse(10f);
                }
            }
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Prefix","<color=#838383>[</color><color=orange>RemoteExplosives</color><color=#838383>]</color>" },
                {"Information", "<color=red>\nInformation:</color>\n<color=#ffd863>/re set</color> <color=#7aadff>1-9999</color> -</color> <color=#aeff63>Sets your frequency</color>\n<color=#ffd863>/re toggle -</color> <color=#aeff63>Toggles the requirement for remote activation</color>\n<color=#ffd863>Your frequency is </color><color=#7aadff>{0}</color>\n<color=#ffd863>Your explosives are</color> {1}"},
                {"SetUsage","<color=green>Usage: /re set 1-9999</color>" },
                {"FrequencySet","<color=green>Frequency set to </color><color=#7aadff>{0}</color>"},
                {"Toggled","<color=green>Toggled {0}</color>"},
                {"ToggledOn","on"},
                {"ToggledOff","off"},
                {"InvalidFrequency","<color=green>Enter a frequency between 1 and 9999</color>" },
                {"YourFrequency","<color=#ffd863>Your frequency is </color><color=#7aadff>{0}</color>\n<color=#ffd863>/re for more information</color>" },
                {"IsRemote","<color=#aeff63>remote!</color>" },
                {"NotRemote","<color=red>not remote!</color>" },
                {"NoPermission","<color=red>No permission to use this command!</color>" }
            }, this);
        }

        void OnPlayerConnected(BasePlayer plr)
        {
            InitializePlayer(plr);
        }

        void OnPlayerDisconnected(BasePlayer plr, string reason)
        {
            if (!frequencies.ContainsKey(plr.userID))
                return;
            frequencies.Remove(plr.userID);
            toggles.Remove(plr.userID);
        }

        private string FormatMessage(BasePlayer plr, string msg)
        {
            return string.Format("{0} {1}", GetMsg("Prefix", plr), GetMsg(msg, plr));
        }

        void InitializePlayer(BasePlayer plr)
        {
            if (!HasPermission(plr, usePerm) && _config.usePermission)
            {
                return;
            }

            if (!toggles.ContainsKey(plr.userID))
                toggles.Add(plr.userID, false);
            int frequency = Random.Range(0, 9999);
            if (_data.players.ContainsKey(plr.userID))
                frequencies.Add(plr.userID, _data.players[plr.userID]);

            if (!frequencies.TryGetValue(plr.userID, out frequency))
            {
                _data.players.Add(plr.userID, frequency);
                frequencies.Add(plr.userID, frequency);
                plr.ChatMessage(string.Format(FormatMessage(plr, "YourFrequency"), frequency));
            }
            else
            {
                plr.ChatMessage(string.Format(FormatMessage(plr, "YourFrequency"), frequency));
            }


            SaveData();
        }

        [ChatCommand("re")]
        void RemoteExplosiveCmd(BasePlayer plr, string cmd, string[] args)
        {
            if (!HasPermission(plr, usePerm) && _config.usePermission)
            {
                plr.ChatMessage(FormatMessage(plr, "NoPermission"));
                return;
            }
            int frequency = Random.Range(0, 9999);
            bool isToggled;
            if (!frequencies.TryGetValue(plr.userID, out frequency) || !toggles.TryGetValue(plr.userID, out isToggled))
                InitializePlayer(plr);
            switch (args.Length)
            {
                case 0:
                    plr.ChatMessage(string.Format(FormatMessage(plr, "Information"), frequency, toggles[plr.userID] ? GetMsg("IsRemote", plr) : GetMsg("NotRemote", plr)));
                    break;
                case 1:
                    switch (args[0].ToLower())
                    {
                        case "toggle":
                            if (!toggles.Any(x => plr.userID == x.Key))
                                InitializePlayer(plr);
                            toggles[plr.userID] = !toggles[plr.userID];
                            plr.ChatMessage(string.Format(FormatMessage(plr, "Toggled"), toggles[plr.userID] ? GetMsg("ToggledOn",plr) : GetMsg("ToggledOff", plr)));
                            break;
                        case "set":
                            plr.ChatMessage(FormatMessage(plr, "SetUsage"));
                            break;
                        default:
                            plr.ChatMessage(string.Format(FormatMessage(plr, "Information"), frequency, toggles[plr.userID] ? GetMsg("IsRemote", plr) : GetMsg("NotRemote", plr)));
                            break;
                    }
                    break;
                case 2:
                    switch (args[0].ToLower())
                    {
                        case "set":
                            int result;
                            int.TryParse(args[1], out result);
                            if (result > 0 && result <= 9999)
                            {
                                frequencies[plr.userID] = result;
                                if (_data.players.ContainsKey(plr.userID))
                                    _data.players[plr.userID] = result;
                                plr.ChatMessage(string.Format(FormatMessage(plr, "FrequencySet"), result));
                            }
                            else
                            {
                                plr.ChatMessage(FormatMessage(plr, "InvalidFrequency"));
                            }
                            SaveData();
                            break;
                        default:
                            plr.ChatMessage(string.Format(FormatMessage(plr, "Information"), frequency, toggles[plr.userID] ? GetMsg("IsRemote", plr) : GetMsg("NotRemote", plr)));
                            break;
                    }
                    break;
                default:
                    plr.ChatMessage(string.Format(FormatMessage(plr, "Information"), frequency, toggles[plr.userID] ? GetMsg("IsRemote", plr) : GetMsg("NotRemote", plr)));
                    break;
            }
        }

        void OnExplosiveThrown(BasePlayer plr, TimedExplosive explosive, ThrownWeapon item)
        {
            if (!HasPermission(plr, usePerm))
                return;
            if (!_config.whitelistedExplosives.ContainsKey(item.ShortPrefabName))
                return;
            if (!_config.whitelistedExplosives[item.ShortPrefabName])
                return;
            bool isToggled;
            int frequency = 0;
            if (!toggles.TryGetValue(plr.userID, out isToggled) || !frequencies.TryGetValue(plr.userID, out frequency))
                InitializePlayer(plr);

            if (!isToggled)
                return;

            explosive.timerAmountMin = int.MaxValue;
            explosive.timerAmountMax = int.MaxValue;
            explosive.SetFuse(int.MaxValue);
            explosive.SendNetworkUpdateImmediate();
            FrequencyListener controller = explosive.gameObject.AddComponent<FrequencyListener>() as FrequencyListener;
            controller.currentPos = explosive.ServerPosition;
            controller.explosive = explosive;
            controller.frequency = frequency;
            controller.plugin = this;
            controller.timers = timer;
            controller.owner = plr;
            RFManager.AddListener(frequency, controller);

        }

        #region Configuration

        private static Configuration _config;

        private class Configuration
        {
            [JsonProperty(PropertyName = "Use Permission")]
            public bool usePermission = true;

            //[JsonProperty(PropertyName = "Links explosives to the deployer (player)")]
            //public bool bindToUser = false;

            [JsonProperty(PropertyName = "Detonate Range")]
            public int range = 100000;

            [JsonProperty(PropertyName = "Whitelisted explosives", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, bool> whitelistedExplosives = new Dictionary<string, bool> { { "explosive.timed.entity", true }, { "explosive.satchel.entity", true }, { "grenade.f1.entity", true }, { "grenade.beancan.entity", true } };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new Exception();
                SaveConfig();
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        protected override void LoadDefaultConfig() => _config = new Configuration();

        #endregion

        #region Data

        private static PluginData _data;

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _data);

        private void LoadData()
        {
            try
            {
                _data = Interface.Oxide.DataFileSystem.ReadObject<PluginData>(Name);
            }
            catch (Exception e)
            {
                PrintError(e.ToString());
            }

            if (_data == null) _data = new PluginData();
        }

        private class PluginData
        {
            [JsonProperty(PropertyName = "Players", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<ulong, int> players = new Dictionary<ulong, int>();
        }

        #endregion

        class FrequencyListener : TimedExplosive, IRFObject
        {
            public Vector3 currentPos;
            public TimedExplosive explosive;
            public int frequency;
            public BasePlayer owner;
            public PluginTimers timers;
            public RemoteExplosive plugin;
            private int distance;
            public int GetFrequency()
            {
                return frequency;
            }

            public float GetMaxRange()
            {
                return _config.range;
            }

            public Vector3 GetPosition()
            {
                return explosive ? explosive.ServerPosition : currentPos;
            }


            public void RFSignalUpdate(bool on)
            {
                if(explosive != null && explosive.ServerPosition != null)
                    distance = (int)Vector3.Distance(explosive.ServerPosition, owner.transform.position);
                if (distance > _config.range)
                    return;
                if (on)
                {
                    explosive.Explode();
                    timers.Once(0.25f, () =>
                    {
                        RFManager.RemoveListener(frequency, this);
                    });
                }
            }
        }
        string GetMsg(string key, BasePlayer source) { return lang.GetMessage(key, this, source.UserIDString); }
    }
}