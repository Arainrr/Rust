﻿using Facepunch;
using Network;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Clear Night", "Clearshot", "2.2.0")]
    [Description("Always bright nights")]
    class ClearNight : CovalencePlugin
    {
        private PluginConfig _config;
        private EnvSync _envSync;
        private List<DateTime> _fullMoonDates = new List<DateTime> {
            new DateTime(2024, 1, 25),
            new DateTime(2024, 2, 24),
            new DateTime(2024, 3, 25),
            new DateTime(2024, 4, 23),
            new DateTime(2024, 5, 23),
            new DateTime(2024, 6, 21),
            new DateTime(2024, 7, 21),
            new DateTime(2024, 8, 19),
            new DateTime(2024, 9, 17),
            new DateTime(2024, 10, 17),
            new DateTime(2024, 11, 15),
            new DateTime(2024, 12, 15)
        };
        private DateTime _date;
        private int _current = 0;
        private bool _playSound = false;

        [PluginReference("NightVision")]
        Plugin NightVisionRef;
        VersionNumber NightVisionMinVersion = new VersionNumber(1, 4, 0);

        void OnServerInitialized()
        {            
            _envSync = BaseNetworkable.serverEntities.OfType<EnvSync>().FirstOrDefault();
            _date = _fullMoonDates[_current];

            TOD_Sky.Instance.Components.Time.OnSunrise += OnSunrise;
            TOD_Sky.Instance.Components.Time.OnSunset += OnSunset;

            if (_envSync == null)
            {
                NextTick(() => {
                    LogError("Unable to find EnvSync! Are you using a custom map?");
                    Interface.Oxide.UnloadPlugin(Name);
                });
                return;
            }

            if (NightVisionRef != null && NightVisionRef.Version < NightVisionMinVersion)
            {
                NextTick(() => {
                    LogError($"NightVision version: v{NightVisionRef.Version}");
                    LogError($"Please update NightVision to v{NightVisionMinVersion} or higher!");
                    Interface.Oxide.UnloadPlugin(Name);
                });
                return;
            }

            timer.Every(_config.syncInterval, () => {
                if (!_envSync.limitNetworking)
                {
                    _envSync.limitNetworking = true;
                }

                if (NightVisionRef != null)
                {
                    NightVisionRef?.CallHook("BlockEnvUpdates", true);
                }

                List<Connection> subscribers = _envSync.net.group.subscribers;
                if (subscribers != null && subscribers.Count > 0)
                {
                    for (int i = 0; i < subscribers.Count; i++)
                    {
                        Connection connection = subscribers[i];
                        global::BasePlayer basePlayer = connection.player as global::BasePlayer;

                        if (!(basePlayer == null))
                        {
                            if (NightVisionRef != null && (bool)NightVisionRef?.CallHook("IsPlayerTimeLocked", basePlayer)) continue;

                            UpdatePlayerDateTime(connection, _config.freezeMoon && TOD_Sky.Instance.IsNight ? _date : _date.AddHours(TOD_Sky.Instance.Cycle.Hour));
                            if (TOD_Sky.Instance.IsNight && _config.weatherAtNight.Count > 0)
                            {
                                UpdatePlayerWeather(connection, _config.weatherAtNight);
                            }

                            if (_playSound)
                            {
                                var effect = new Effect(_config.sound, basePlayer, 0, Vector3.zero, Vector3.forward);
                                EffectNetwork.Send(effect, connection);
                            }
                        }
                    }
                }

                _playSound = false;
            });
        }

        void Unload()
        {
            TOD_Sky.Instance.Components.Time.OnSunrise -= OnSunrise;
            TOD_Sky.Instance.Components.Time.OnSunset -= OnSunset;

            if (_envSync != null)
            {
                _envSync.limitNetworking = false;
            }

            if (NightVisionRef != null)
            {
                NightVisionRef?.CallHook("BlockEnvUpdates", false);
            }

            ServerMgr.SendReplicatedVars("weather.");
        }

        void OnSunrise()
        {
            ServerMgr.SendReplicatedVars("weather.");
        }

        void OnSunset()
        {
            _playSound = _config.playSoundAtSunset;
            if (_config.randomizeDates)
            {
                _current = UnityEngine.Random.Range(0, _fullMoonDates.Count - 1);
                _date = _fullMoonDates[_current];
            }
            else
            {
                _current = _current >= _fullMoonDates.Count ? 0 : _current;
                _date = _fullMoonDates[_current];
                _current++;
            }
        }

        private void UpdatePlayerDateTime(Connection connection, DateTime date)
        {
            if (Net.sv.write.Start())
            {
                connection.validate.entityUpdates = connection.validate.entityUpdates + 1;
                BaseNetworkable.SaveInfo saveInfo = new global::BaseNetworkable.SaveInfo
                {
                    forConnection = connection,
                    forDisk = false
                };
                Net.sv.write.PacketID(Message.Type.Entities);
                Net.sv.write.UInt32(connection.validate.entityUpdates);
                using (saveInfo.msg = Pool.Get<Entity>())
                {
                    _envSync.Save(saveInfo);
                    saveInfo.msg.environment.dateTime = date.ToBinary();
                    saveInfo.msg.environment.fog = 0;
                    saveInfo.msg.environment.rain = 0;
                    saveInfo.msg.environment.clouds = 0;
                    saveInfo.msg.environment.wind = 0;
                    if (saveInfo.msg.baseEntity == null)
                    {
                        LogError(this + ": ToStream - no BaseEntity!?");
                    }
                    if (saveInfo.msg.baseNetworkable == null)
                    {
                        LogError(this + ": ToStream - no baseNetworkable!?");
                    }
                    saveInfo.msg.ToProto(Net.sv.write);
                    _envSync.PostSave(saveInfo);
                    Net.sv.write.Send(new SendInfo(connection));
                }
            }
        }

        private void UpdatePlayerWeather(Connection connection, Dictionary<string, string> list)
        {
            if (Net.sv.write.Start())
            {
                Net.sv.write.PacketID(Message.Type.ConsoleReplicatedVars);
                Net.sv.write.Int32(list.Count);
                foreach (KeyValuePair<string, string> item in list)
                {
                    Net.sv.write.String(item.Key);
                    Net.sv.write.String(item.Value);
                }
                Net.sv.write.Send(new SendInfo(connection));
            }
        }

        [Command("clearnight.debug")]
        private void DebugCommand(Core.Libraries.Covalence.IPlayer player, string command, string[] args)
        {
            player.Message("clearnight.debug");
            if (!player.IsAdmin && !player.IsServer) return;

            StringBuilder _sb = new StringBuilder();
            _sb.AppendLine("\n*** DEBUG START ***\n");
            _sb.AppendLine($"ClearNight version: {Version}");
            _sb.AppendLine($"ClearNight date: {(_config.freezeMoon && TOD_Sky.Instance.IsNight ? _date : _date.AddHours(TOD_Sky.Instance.Cycle.Hour))}");

            _sb.AppendLine($"\n[Server date and time]");
            _sb.AppendLine($"Year: {TOD_Sky.Instance.Cycle.Year}");
            _sb.AppendLine($"Month: {TOD_Sky.Instance.Cycle.Month}");
            _sb.AppendLine($"Day: {TOD_Sky.Instance.Cycle.Day}");
            _sb.AppendLine($"Hour: {TOD_Sky.Instance.Cycle.Hour}");
            _sb.AppendLine($"IsNight: {TOD_Sky.Instance.IsNight}");
            _sb.AppendLine($"IsDay: {TOD_Sky.Instance.IsDay}");

            _sb.AppendLine($"\n[Config]");
            _sb.AppendLine(JsonConvert.SerializeObject(_config, Formatting.Indented, Config.Settings));

            _sb.AppendLine($"\nNightVision installed: {NightVisionRef != null}");
            if (NightVisionRef != null)
            {
                _sb.AppendLine($"NightVision version: {NightVisionRef.Version}");
            }

            _sb.AppendLine("\n*** DEBUG END ***");
            Puts(_sb.ToString());
            LogToFile("debug", _sb.ToString(), this);
        }

        #region Config

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(GetDefaultConfig(), true);
        }

        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<PluginConfig>();

            bool invalidDates = true;
            if (_config.fullMoonDates.Length > 0)
            {
                List<DateTime> tempDates = new List<DateTime>();
                foreach(string date in _config.fullMoonDates)
                {
                    DateTime dt;
                    if (DateTime.TryParse(date, out dt))
                    {
                        tempDates.Add(dt);
                    }
                    else
                    {
                        Puts($"invalid date: {date}");
                    }
                }

                if (tempDates.Count > 0)
                {
                    invalidDates = false;
                    _fullMoonDates = tempDates;
                    Puts($"registered {_fullMoonDates.Count} {(_fullMoonDates.Count == 1 ? "date" : "dates")} from config");
                }
            }

            if (invalidDates)
            {
                Puts("no valid dates registered, using default dates");
            }

            Config.WriteObject(_config, true);
        }

        private class PluginConfig
        {
            public string[] fullMoonDates = {
                "1/25/2024",
                "2/24/2024",
                "3/25/2024",
                "4/23/2024",
                "5/23/2024",
                "6/21/2024",
                "7/21/2024",
                "8/19/2024",
                "9/17/2024",
                "10/17/2024",
                "11/15/2024",
                "12/15/2024"
            };
            public Dictionary<string, string> weatherAtNight = new Dictionary<string, string> {
                { "weather.atmosphere_brightness", "1" },
                { "weather.atmosphere_contrast", "1.5" },
                { "weather.cloud_coverage", "0" },
                { "weather.cloud_size", "0" },
                { "weather.fog", "0" },
                { "weather.fog_chance", "0" }
            };
            public bool randomizeDates = false;
            public bool freezeMoon = false;
            public bool playSoundAtSunset = false;
            public string sound = "assets/bundled/prefabs/fx/player/howl.prefab";
            public float syncInterval = 5f;
        }

        #endregion
    }
}
