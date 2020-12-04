﻿using System.IO;
using Oxide.Core;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Oxide.Plugins
{
    [Info("HW Time Control", "klauz24", 1.5), Description("Controls time on your server")]
    internal class HWTimeControl : HurtworldPlugin
    {
        private const string _perm = "hwtimecontrol.use";

        private void Init() => permission.RegisterPermission(_perm, this);

        private Configuration _config;

        private class Configuration
        {
            [JsonProperty(PropertyName = "Day Length")]
            public float DayLength { get; set; } = 900f;

            [JsonProperty(PropertyName = "Night Length")]
            public float NightLength { get; set; } = 420f;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null)
                {
                    throw new JsonException();
                }
            }
            catch
            {
                string configPath = $"{Interface.Oxide.ConfigDirectory}{Path.DirectorySeparatorChar}{Name}";
                PrintWarning($"Could not load a valid configuration file, creating a new configuration file at {configPath}.json");
                Config.WriteObject(_config, false, $"{configPath}_invalid.json");
                LoadDefaultConfig();
                SaveConfig();
            }
        }

        protected override void LoadDefaultConfig() => _config = new Configuration();

        protected override void SaveConfig() => Config.WriteObject(_config);

        private void OnServerInitialized()
        {
            TimeManager.Instance.DayLength = (float)_config.DayLength;
            TimeManager.Instance.NightLength = (float)_config.NightLength;
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"HWTimeControl - Prefix", "<color=orange>[TimeControl]</color>"},
                {"HWTimeControl - No Perm", "You do not have permission to use this command. Required: {0}."},
                {"HWTimeControl - Syntax", "Syntax:\nType /settime day to set day manually.\nType /settime night to set night manually.\nType /settime <value> to set the time manually (Example: /settime 0.3)."},
                {"HWTimeControl - Set", "Time was switched to {0} manually."},
                {"HWTimeControl - Manual", "You've set the time to: {0}."},
                {"HWTimeControl - Too Long", "Seems like the value is a bit too long, please try something shorter, type <color=lightblue>/settime syntax</color> if you need any help."}
            }, this);
        }

        [ChatCommand("settime")]
        private void HWTimeControlSetTime(PlayerSession session, string command, string[] args)
        {
            if (permission.UserHasPermission(session.SteamId.ToString(), _perm) || session.IsAdmin)
            {
                if (args.Length == 0)
                {
                    Msg(session, Lang(session, "HWTimeControl - Prefix"), Lang(session, "HWTimeControl - Syntax"));
                }
                else
                {
                    double num;
                    string arg = args[0];
                    if (double.TryParse(arg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out num))
                    {
                        if (arg.Length <= 3)
                        {
                            Msg(session, Lang(session, "HWTimeControl - Prefix"), string.Format(Lang(session, "HWTimeControl - Manual"), arg));
                            Server.Command($"settime {arg}");
                        }
                        else
                        {
                            Msg(session, Lang(session, "HWTimeControl - Prefix"), Lang(session, "HWTimeControl - Too Long"));
                        }
                    }
                    else
                    {
                        switch (arg.ToLower())
                        {
                            case "day":
                                Server.Command("settime 0.3");
                                Msg(session, Lang(session, "HWTimeControl - Prefix"), string.Format(Lang(session, "HWTimeControl - Set"), "day"));
                                break;

                            case "night":
                                Server.Command("settime 1");
                                Msg(session, Lang(session, "HWTimeControl - Prefix"), string.Format(Lang(session, "HWTimeControl - Set"), "night"));
                                break;

                            case "syntax":
                                Msg(session, Lang(session, "HWTimeControl - Prefix"), Lang(session, "HWTimeControl - Syntax"));
                                break;

                            default:
                                Msg(session, Lang(session, "HWTimeControl - Prefix"), Lang(session, "HWTimeControl - Syntax"));
                                break;
                        }
                    }
                }
            }
            else
            {
                Msg(session, Lang(session, "HWTimeControl - Prefix"), string.Format(Lang(session, "HWTimeControl - No Perm"), _perm));
            }
        }

        private void Msg(PlayerSession session, string prefix, string message) => hurt.SendChatMessage(session, prefix, message);

        private string Lang(PlayerSession session, string key)
        {
            return lang.GetMessage(key, this, session.SteamId.ToString());
        }
    }
}