using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Timed Command Blocker", "Orange / MONaH", "1.2.6")]
    [Description("Block commands temporarily or permanently")]
    public class TimedCommandBlocker : CovalencePlugin
    {
        private const string PERMISSION_IMMUNE = "timedcommandblocker.immune";
        #region Oxide Hooks

        private void Init()
        {
            OnStart();
        }

        private object OnUserCommand(IPlayer player, string command, string[] args)
        {
            if (args != null && args.Length != 0)
            {
                foreach (var arg in args)
                {
                    command += $" {arg}";
                }
            }
            return CheckCommand(player, command);
        }

        #if RUST
        private object OnServerCommand(ConsoleSystem.Arg console)
        {
            var player = console.Player();
            if (player == null) {return null;}
            var command = console.cmd.FullName;
            if (console.Args != null && console.Args.Length != 0)
            {
                foreach (var arg in console.Args)
                {
                    command += $" {arg}";
                }
            }
            return CheckCommand(player.IPlayer, command);
        }
        #endif

        #endregion

        #region Helpers

        private void OnStart()
        {
            LoadDefaultMessages();
            permission.RegisterPermission(PERMISSION_IMMUNE, this);
        }
        
        private object CheckCommand(IPlayer player, string command)
        {

            if (player == null)
            {
                return null;
            }

            if (configData.globalS.adminsIgnored && player.IsAdmin)
            {
                return null;
            }

            if (configData.globalS.usePermission && permission.UserHasPermission(player.Id, PERMISSION_IMMUNE))
            {
                return null;
            }

            command = command.Replace("chat.say /", string.Empty);
            
            foreach (var item in configData.commands)
            {
                if (command.StartsWith(item.Key, StringComparison.OrdinalIgnoreCase))
                {
                    var blockTime = item.Value;
                    if (blockTime == 0)
                    {
                        Print(player, Lang("Blocked", player.Id, command, configData.chatS.chatCommandColor));
                        return false;
                    }

                    var left = blockTime - Passed(SaveTime());

                    if (left > 0)
                    {
                        Print(player, Lang("Unblock", player.Id, command, TimeText(player.Id, left), configData.chatS.chatCommandColor));
                        return false;
                    }
                }
            }

            return null;
        }

        private static double Now()
        {
            return DateTime.UtcNow.Subtract(new DateTime(2019, 1, 1, 0, 0, 0)).TotalSeconds;            
        }

        private int Passed(double a)
        {
            return Convert.ToInt32(Now() - a);
        }
        
        private double SaveTime()
        {
            return SaveRestore.SaveCreatedTime.Subtract(new DateTime(2019, 1, 1, 0, 0, 0)).TotalSeconds;
        }

        private string TimeText(string id, int seconds)
        {
            var dd = string.Join("\\", ($"<color={configData.chatS.chatCommandArgumentColor}>*</color>").ToCharArray());
            dd = dd.Replace("\\*", "dd");
            var hh = string.Join("\\", ($"<color={configData.chatS.chatCommandArgumentColor}>*</color>").ToCharArray());
            hh = hh.Replace("\\*", "hh");
            var mm = string.Join("\\", ($"<color={configData.chatS.chatCommandArgumentColor}>*</color>").ToCharArray());
            mm = mm.Replace("\\*", "mm");
            var ss = string.Join("\\", ($"<color={configData.chatS.chatCommandArgumentColor}>*</color>").ToCharArray());
            ss = ss.Replace("\\*", "ss");

            var ddt = string.Join("\\", Lang("Days", id).ToCharArray());
            var hht = string.Join("\\", Lang("Hours", id).ToCharArray());
            var mmt = string.Join("\\", Lang("Minutes", id).ToCharArray());
            var sst = string.Join("\\", Lang("Seconds", id).ToCharArray());

            var tFormat = "";
            if (seconds > 86400)
            {
                tFormat = "\\" + dd + "\\ \\" + ddt + "\\ \\" + hh + "\\ \\" + hht + "\\ \\" + mm + "\\ \\" + mmt + "\\ \\" + ss + "\\ \\" + sst;
            }
            else if (seconds > 3600)
            {
                tFormat = "\\" + hh + "\\ \\" + hht + "\\ \\" + mm + "\\ \\" + mmt + "\\ \\" + ss + "\\ \\" + sst;
            }
            else if (seconds > 60)
            {
                tFormat = "\\" + mm + "\\ \\" + mmt + "\\ \\" + ss + "\\ \\" + sst;
            }
            else if (seconds > 0)
            {
                tFormat = "\\" + ss + "\\ \\" + sst;
            }

            return TimeSpan.FromSeconds(seconds).ToString(@"" + tFormat);
        }

        #endregion
        
        #region ConfigurationFile
        
        private ConfigData configData;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Global settings")]
            public GlobalSettings globalS = new GlobalSettings();

            [JsonProperty(PropertyName = "Chat settings")]
            public ChatSettings chatS = new ChatSettings();

            [JsonProperty(PropertyName = "Command - time in seconds")]
            public Dictionary<string, int> commands = new Dictionary<string, int>
            {
                ["test"] = 0,
                ["kit orange"] = 86400
            };

            public class GlobalSettings
            {
                [JsonProperty(PropertyName = "Use permissions")]
                public bool usePermission = true;

                [JsonProperty(PropertyName = "Ignore admins")]
                public bool adminsIgnored = true;
            }

            public class ChatSettings
            {
                [JsonProperty(PropertyName = "Chat prefix")]
                public string prefix = "<color=#00FFFF>[Command Blocker]</color>: ";

                [JsonProperty(PropertyName = "Chat steamID icon")]
                public ulong steamIDIcon = 0;
                
                [JsonProperty(PropertyName = "Chat command color")]
                public string chatCommandColor = "#FFFF00";
                
                [JsonProperty(PropertyName = "Chat command argument color")]
                public string chatCommandArgumentColor = "#FFA500";
            }                    
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                configData = Config.ReadObject<ConfigData>();
                if (configData == null)
                    LoadDefaultConfig();
            }
            catch
            {
                PrintError("The configuration file is corrupted");
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file");
            configData = new ConfigData();
        }

        protected override void SaveConfig() => Config.WriteObject(configData);

        #endregion ConfigurationFile

        #region Localization

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Blocked"] = "Command <color={1}>{0}</color> is blocked.",
                ["Unblock"] = "Command <color={2}>{0}</color> is blocked.\nUnblocking in {1}.",
                
                ["Days"] = "Days",
                ["Hours"] = "Hours",
                ["Minutes"] = "Minutes",
                ["Seconds"] = "Seconds",
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Blocked"] = "Команда <color={1}>{0}</color> заблокирована.",
                ["Unblock"] = "Команда <color={2}>{0}</color> заблокирована.\nРазблокировка через: {1}.",
                
                ["Days"] = "дней",
                ["Hours"] = "часов",
                ["Minutes"] = "минут",
                ["Seconds"] = "секунд",
            }, this, "ru");           
        }

        private void Print(IPlayer player, string message)
        {
            var text= string.IsNullOrEmpty(configData.chatS.prefix) ? string.Empty : $"{configData.chatS.prefix}{message}";
            #if RUST
            (player.Object as BasePlayer).SendConsoleCommand("chat.add", 2, configData.chatS.steamIDIcon, text);
            return;
            #endif
            player.Message(text);
        }

        #endregion
    }
}