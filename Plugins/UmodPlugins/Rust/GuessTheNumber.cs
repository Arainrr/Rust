using System;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using System.Text;

namespace Oxide.Plugins
{
    /*==============================================================================================================
    *    
    *    THANKS to Dora the original creator of this plugin
    *    THANKS to redBDGR the previous maintainer of this plugin upto v2.0.2
    *    
    *    2.1.0 : Fixed all messaging and game events
    *    2.1.1 : Fixed Random event from being always 0 and giving incorrect chat messages
    *    2.1.2 : Fixed continuous timer not being destroyed
    *            Added destroy remaining timers on unload
    *            Fixed random number to be shown on each event to rcon
    *    2.1.3 : Added more checks to make sure number = 0 is skipped and does another random Without showing it to chat
    *            Changed float for int for timers
    *            Organised cfg
    *            Added prefix and steamicon
    *            Fixed rewards not been given
    *            Added support for Battlepass plugin
    *            Added min/max reward settings
    *            This Requires a complete new install cfg/lang
    *    
    *    Todo :
    *    
     ==============================================================================================================*/

    [Info("GuessTheNumber", "Krungh Crow", "2.1.3")]
    [Description("An event that requires player to guess the correct number")]

    class GuessTheNumber : RustPlugin
    {
        [PluginReference]
        Plugin Battlepass, ServerRewards, Economics/*, GUIAnnouncements*/;

        public Dictionary<ulong, int> playerInfo = new Dictionary<ulong, int>();

        bool useEconomics = true;
        bool useEconomicsloss = true;
        bool useServerRewards = false;
        bool useServerRewardsloss = false;
        bool useBattlepass1 = false;
        bool useBattlepass2 = false;
        bool useBattlepassloss = false;
        //bool useGUIAnnouncements = false;
        bool autoEventsEnabled = false;
        bool showAttempts = false;
        float autoEventTime = 600f;
        float eventLength = 30f;
        int minDefault = 1;
        int maxDefault = 1000;
        int maxTries = 1;
        int MinPlayer = 1;
        int economicsWinReward = 20;
        int economicsLossReward = 10;
        int serverRewardsWinReward = 20;
        int serverRewardsLossReward = 10;
        int battlepassWinReward1 = 20;
        int battlepassWinReward2 = 20;
        int battlepassLossReward1 = 10;
        int battlepassLossReward2 = 10;

        string Prefix = "[<color=green>GuessTheNumber</color>] ";
        ulong SteamIDIcon = 76561199090290915;
        private bool AddSecondCurrency;
        private bool AddFirstCurrency;

        const string permissionNameADMIN = "guessthenumber.admin";
        const string permissionNameENTER = "guessthenumber.enter";

        bool Changed = false;
        bool eventActive = false;
        Timer eventTimer;
        Timer autoRepeatTimer;
        int minNumber = 0;
        int maxNumber = 0;
        bool hasEconomics = false;
        bool hasServerRewards = false;
        bool hasBattlepass = false;
        //bool hasGUIAnnouncements = false;
        int number = 0;

        void LoadVariables()
        {
            showAttempts = Convert.ToBoolean(GetConfig("Announce Settings", "Show all Guess Attempts to chat", false));
            //useGUIAnnouncements = Convert.ToBoolean(GetConfig("Announce Settings", "Use GUIAnnouncements", false));
            //Online
            MinPlayer = Convert.ToInt32(GetConfig("Online Settings", "Minimum amount of players to be online to start the game", "1"));
            //Events
            autoEventsEnabled = Convert.ToBoolean(GetConfig("Event Settings", "Auto Events Enabled", false));
            autoEventTime = Convert.ToInt32(GetConfig("Event Settings", "Auto Event Repeat Time", 600));
            eventLength = Convert.ToInt32(GetConfig("Event Settings", "Event Length", 30));
            minDefault = Convert.ToInt32(GetConfig("Event Settings", "Default Number Min", 1));
            maxDefault = Convert.ToInt32(GetConfig("Event Settings", "Default Number Max", 100));
            maxTries = Convert.ToInt32(GetConfig("Event Settings", "Max Tries", 1));
            //Economics
            useEconomics = Convert.ToBoolean(GetConfig("Reward Economics Settings", "Use Economics", true));
            useEconomicsloss = Convert.ToBoolean(GetConfig("Reward Economics Settings", "Use Economics on loss", true));
            economicsWinReward = Convert.ToInt32(GetConfig("Reward Economics Settings", "Amount (win)", 20));
            economicsLossReward = Convert.ToInt32(GetConfig("Reward Economics Settings", "Amount (loss)", 10));
            //ServerRewards
            useServerRewards = Convert.ToBoolean(GetConfig("Reward ServerRewards Settings", "Use ServerRewards", false));
            useServerRewardsloss = Convert.ToBoolean(GetConfig("Reward ServerRewards Settings", "Use ServerRewards on loss", false));
            serverRewardsWinReward = Convert.ToInt32(GetConfig("Reward ServerRewards Settings", "Amount (win)", 20));
            serverRewardsLossReward = Convert.ToInt32(GetConfig("Reward ServerRewards Settings", "Amount (loss)", 10));
            //Battlepass
            useBattlepass1 = Convert.ToBoolean(GetConfig("Reward Battlepass Settings", "Use Battlepass 1st currency", false));
            useBattlepass2 = Convert.ToBoolean(GetConfig("Reward Battlepass Settings", "Use Battlepass 2nd currency", false));
            useBattlepassloss = Convert.ToBoolean(GetConfig("Reward Battlepass Settings", "Use Battlepass on loss", false));
            battlepassWinReward1 = Convert.ToInt32(GetConfig("Reward Battlepass Settings", "Amount 1st currency (win)", 20));
            battlepassWinReward2 = Convert.ToInt32(GetConfig("Reward Battlepass Settings", "Amount 2nd currency (win)", 20));
            battlepassLossReward1 = Convert.ToInt32(GetConfig("Reward Battlepass Settings", "Amount 1st currency (loss)", 10));
            battlepassLossReward2 = Convert.ToInt32(GetConfig("Reward Battlepass Settings", "Amount 2nd currency (loss)", 10));

            if (!Changed) return;
            SaveConfig();
            Changed = false;
        }

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            LoadVariables();
        }

        void Init()
        {
            permission.RegisterPermission(permissionNameADMIN, this);
            permission.RegisterPermission(permissionNameENTER, this);
            LoadVariables();
        }

        void Unload()
        {
            if (autoEventsEnabled)
                if (!autoRepeatTimer.Destroyed)
                {
                    autoRepeatTimer.Destroy();
                }
            return;
        }

        private void OnServerInitialized()
        {
            LoadVariables();

            if (autoEventsEnabled)
            {
                if (eventActive)
                {
                    return;
                }
                autoRepeatTimer = timer.Repeat(autoEventTime, 0, () =>
                {
                    if (BasePlayer.activePlayerList.Count >= MinPlayer)
                    {
                        minNumber = minDefault;
                        maxNumber = maxDefault;
                        number = Convert.ToInt32(Math.Round(Convert.ToDouble(UnityEngine.Random.Range(Convert.ToSingle(minNumber), Convert.ToSingle(maxNumber)))));
                        StartEvent();
                    }
                    else
                    {
                        return;
                    }
                });
            }
            
            // External plugin checking
            if (!Economics)
                hasEconomics = false;
            else
                hasEconomics = true;

            if (!ServerRewards)
                hasServerRewards = false;
            else
                hasServerRewards = true;

            if (!Battlepass)
                hasBattlepass = false;
            else
                hasBattlepass = true;
        }

        void Loaded()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                //chat
                ["No Permission"] = "You cannot use this command!",
                ["Event Already Active"] = "There is currently already an event that is active!",
                ["Event Started"] = "A random number event has started, correctly guess the random number to win a prize!\nUse /guess <number> to enter",
                ["Help Message"] = "<color=#cccc00>/gtn start</color> (this will use the default min/max set in the config)",
                ["Help Message1"] = "<color=#cccc00>/gtn start <min number> <max number></color> (allows you to set custom min/max numbers)",
                ["Help Message2"] = "<color=#cccc00>/gtn end</color> (will end the current event)",
                ["No Event"] = "There are no current events active",
                ["Max Tries"] = "You have already guessed the maximum number of times",
                ["Event Win"] = "{0} has won the event! (correct number was {1})",
                ["Battlepass Reward1"] = "For winning you are rewarded (BP1) : ",
                ["Battlepass loss Reward1"] = "Incorrect answer you get (BP1) : ",
                ["Battlepass Reward2"] = "For winning you are rewarded (BP2) : ",
                ["Battlepass loss Reward2"] = "Incorrect answer you get (BP2) : ",
                ["Economics Reward"] = "For winning you are rewarded $",
                ["Economics loss Reward"] = "Incorrect answer you get $",
                ["ServerRewards Reward"] = "For winning you are rewarded RP",
                ["ServerRewards loss Reward"] = "Incorrect answer you get RP",
                ["Wrong Number"] = "You guessed the wrong number\nGuesses remaining this round : ",
                ["/guess Invalid Syntax"] = "Invalid syntax! /guess <number>",
                ["Event Timed Out"] = "The event time has run out and no one successfully guessed the number!\nThe Number to guess was : ",
                ["Invalid Guess Entry"] = "The guess you entered was invalid! numbers only please",
                ["Event Created"] = "The event has been succesfully created, the winning number is ",
                ["GTN console invalid syntax"] = "Invalid syntax! gtn <start/end> <min number> <max number>",

            }, this);
        }

        [ConsoleCommand("gtn")]
        void GTNCONSOLECMD(ConsoleSystem.Arg args)
        {
            //args.ReplyWith("test");
            if (args.Connection != null)
                return;
            if (args.Args == null)
            {
                args.ReplyWith(msg("GTN console invalid syntax"));
                return;
            }
            if (args.Args.Length == 0)
            {
                args.ReplyWith(msg("GTN console invalid syntax"));
                return;
            }
            if (args.Args.Length > 3)
            {
                args.ReplyWith(msg("GTN console invalid syntax"));
                return;
            }
            if (args.Args[0] == null)
            {
                args.ReplyWith(msg("GTN console invalid syntax"));
                return;
            }
            if (args.Args[0] == "start")
            {
                if (eventActive)
                {
                    args.ReplyWith(msg("Event Already Active"));
                    return;
                }
                if (args.Args.Length == 3)
                {
                    minNumber = Convert.ToInt32(args.Args[1]);
                    maxNumber = Convert.ToInt32(args.Args[2]);
                    if (minNumber != 0 && maxNumber != 0)
                    {
                        number = Convert.ToInt32(Math.Round(Convert.ToDouble(UnityEngine.Random.Range(Convert.ToSingle(minNumber), Convert.ToSingle(maxNumber)))));
                        StartEvent();
                        args.ReplyWith(string.Format(msg("Event Created"), number.ToString()));
                    }
                    else
                    {
                        args.ReplyWith(msg("Invalid Params"));
                        return;
                    }
                }
                else
                {
                    minNumber = minDefault;
                    maxNumber = maxDefault;
                    number = Convert.ToInt32(Math.Round(Convert.ToDouble(UnityEngine.Random.Range(Convert.ToSingle(minNumber), Convert.ToSingle(maxNumber)))));
                    StartEvent();
                    args.ReplyWith(string.Format(msg("Event Created"), number.ToString()));
                }
                if (autoEventsEnabled)
                    if (!autoRepeatTimer.Destroyed)
                    {
                        autoRepeatTimer.Destroy();
                        autoRepeatTimer = timer.Repeat(autoEventTime, 0, () => StartEvent());
                    }
                return;
            }
            else if (args.Args[0] == "end")
            {
                if (eventActive == false)
                {
                    args.ReplyWith(msg("No Event"));
                    return;
                }
                if (!eventTimer.Destroyed || eventTimer != null)
                    eventTimer.Destroy();
                if (autoEventsEnabled)
                    if (!autoRepeatTimer.Destroyed)
                    {
                        autoRepeatTimer.Destroy();
                        autoRepeatTimer = timer.Repeat(autoEventTime, 0, () => StartEvent());
                    }
                eventActive = false;
                args.ReplyWith("The current event has been cancelled");
                Server.Broadcast(msg("Event Timed Out"));
            }
            else
                args.ReplyWith(msg("GTN console invalid syntax"));
            return;
        }

        [ChatCommand("gtn")]
        private void startGuessNumberEvent(BasePlayer player, string cmd, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, permissionNameADMIN))
            {
                Player.Message(player, $"{lang.GetMessage("No Permission", this, player.UserIDString)}", Prefix, SteamIDIcon);
                return;
            }
            if (args.Length == 0)
            {
                player.ChatMessage(DoHelpMenu());
                return;
            }
            if (args.Length > 3)
            {
                player.ChatMessage(DoHelpMenu());
                return;
            }
            if (args[0] == null)
            {
                player.ChatMessage(DoHelpMenu());
                return;
            }
            if (args[0] == "start")
            {
                if (eventActive)
                {
                    Player.Message(player, $"{lang.GetMessage("Event Already Active", this, player.UserIDString)}", Prefix, SteamIDIcon);
                    return;
                }
                if (args.Length == 3)
                {
                    minNumber = Convert.ToInt32(args[1]);
                    maxNumber = Convert.ToInt32(args[2]);
                    if (minNumber != 0 && maxNumber != 0)
                    {
                        number = Convert.ToInt32(Math.Round(Convert.ToDouble(UnityEngine.Random.Range(Convert.ToSingle(minNumber), Convert.ToSingle(maxNumber)))));
                        StartEvent();
                        Player.Message(player, $"{lang.GetMessage("Event Created", this, player.UserIDString)}<color=green>{number.ToString()}</color>", Prefix, SteamIDIcon);
                    }
                    else
                    {
                        Player.Message(player, $"{lang.GetMessage("Invalid Params", this, player.UserIDString)}", Prefix, SteamIDIcon);
                        return;
                    }
                }
                else
                {
                    minNumber = minDefault;
                    maxNumber = maxDefault;
                    number = Convert.ToInt32(Math.Round(Convert.ToDouble(UnityEngine.Random.Range(Convert.ToSingle(minNumber), Convert.ToSingle(maxNumber)))));
                    StartEvent();
                    Player.Message(player, $"{lang.GetMessage("Event Created", this, player.UserIDString)}<color=green>{number.ToString()}</color>", Prefix, SteamIDIcon);
                }
                if (autoEventsEnabled)
                    if (!autoRepeatTimer.Destroyed)
                    {
                        autoRepeatTimer.Destroy();
                        autoRepeatTimer = timer.Repeat(autoEventTime, 0, () => StartEvent());
                    }
                return;
            }
            else if (args[0] == "end")
            {
                if (eventActive == false)
                {
                    Player.Message(player, $"{lang.GetMessage("No Event", this, player.UserIDString)}", Prefix, SteamIDIcon);
                    return;
                }
                if (!eventTimer.Destroyed || eventTimer != null)
                    eventTimer.Destroy();
                if (autoEventsEnabled)
                    if (!autoRepeatTimer.Destroyed)
                    {
                        autoRepeatTimer.Destroy();
                        autoRepeatTimer = timer.Repeat(autoEventTime, 0, () => StartEvent());
                    }
                eventActive = false;
                Server.Broadcast(msg("Event Timed Out"));
            }
            else
            Player.Message(player, $"{lang.GetMessage("Help Message", this, player.UserIDString)}", Prefix, SteamIDIcon);
            return;
        }

        [ChatCommand("guess")]
        private void numberReply(BasePlayer player, string cmd, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, permissionNameENTER))
            {
                Player.Message(player, $"{lang.GetMessage("No Permission", this, player.UserIDString)}", Prefix, SteamIDIcon);
                return;
            }
            if (!eventActive)
            {
                Player.Message(player, $"{lang.GetMessage("No Event", this, player.UserIDString)}", Prefix, SteamIDIcon);
                return;
            }

            if (args.Length == 1)
            {
                if (!IsNumber(args[0]))
                {
                    Player.Message(player, $"{lang.GetMessage("Invalid Guess Entry", this, player.UserIDString)}", Prefix, SteamIDIcon);
                    return;
                }
                int playerNum = Convert.ToInt32(args[0]);
                if (!playerInfo.ContainsKey(player.userID))
                    playerInfo.Add(player.userID, 0);
                if (playerInfo[player.userID] >= maxTries)
                {
                    Player.Message(player, $"{lang.GetMessage("Max Tries", this, player.UserIDString)}", Prefix, SteamIDIcon);
                    return;
                }
                if (args[0] == "0")
                {
                    Player.Message(player, $"{lang.GetMessage("You are not allowed to guess this number", this, player.UserIDString)}", Prefix, SteamIDIcon);
                    return;
                }

                if (showAttempts == true)
                {
                    Server.Broadcast($"<color=yellow>{player.displayName}</color> guessed {args[0].ToString()}", Prefix, SteamIDIcon);
                }
                if (playerNum == number)
                {
                    Server.Broadcast(string.Format(msg("Event Win", player.UserIDString), player.displayName, number.ToString()), Prefix, SteamIDIcon);
                    if (hasEconomics)
                    {
                        if (useEconomics)
                        {
                            if ((bool)Economics?.Call("Deposit", player.userID, (double)economicsWinReward))
                            {
                                Player.Message(player, $"{lang.GetMessage("Economics Reward", this, player.UserIDString)}{economicsWinReward.ToString()}", Prefix, SteamIDIcon);
                            }
                        }
                    }

                    if (hasServerRewards)
                    {
                        if (useServerRewards)
                        {
                            ServerRewards?.Call("AddPoints", player.userID, (int)serverRewardsWinReward);
                            {
                                Player.Message(player, $"{lang.GetMessage("ServerRewards Reward", this, player.UserIDString)}{serverRewardsWinReward.ToString()}", Prefix, SteamIDIcon);
                            }
                        }
                    }

                    if (hasBattlepass)
                    {
                        if (useBattlepass1)
                        {
                            Battlepass?.Call("AddFirstCurrency", player.userID, battlepassWinReward1);
                            {
                                Player.Message(player, $"{lang.GetMessage("Battlepass Reward1", this, player.UserIDString)}{battlepassWinReward1.ToString()}", Prefix, SteamIDIcon);
                            }
                        }

                        if (useBattlepass2)
                        {
                            Battlepass?.Call("AddSecondCurrency", player.userID, battlepassWinReward2);
                            {
                                Player.Message(player, $"{lang.GetMessage("Battlepass Reward2", this, player.UserIDString)}{battlepassWinReward2.ToString()}", Prefix, SteamIDIcon);
                            }
                        }
                    }
                    number = 0;
                    eventActive = false;
                    playerInfo.Clear();
                    eventTimer.Destroy();
                    autoRepeatTimer = timer.Repeat(autoEventTime, 0, () => StartEvent());
                }
                else
                {
                    playerInfo[player.userID]++;
                    Player.Message(player, $"{lang.GetMessage("Wrong Number", this, player.UserIDString)}<color=green>{(playerInfo[player.userID] - maxTries).ToString()}</color>", Prefix, SteamIDIcon);
                    if (hasEconomics)
                    {
                        if (useEconomics)
                        {
                            if (useEconomicsloss)
                            {
                                if ((bool)Economics?.Call("Deposit", player.userID, (double)economicsLossReward))
                                {
                                    Player.Message(player, $"{lang.GetMessage("Economics loss Reward", this, player.UserIDString)}{economicsLossReward.ToString()}", Prefix, SteamIDIcon);
                                }
                            }
                        }
                    }

                    if (hasServerRewards)
                    {
                        if (useServerRewards)
                        {
                            if (useServerRewardsloss)
                            {
                                ServerRewards?.Call("AddPoints", player.userID, (int)serverRewardsLossReward);
                                {
                                    Player.Message(player, $"{lang.GetMessage("ServerRewards loss Reward", this, player.UserIDString)}{serverRewardsLossReward.ToString()}", Prefix, SteamIDIcon);
                                }
                            }
                        }
                    }
                    if (hasBattlepass)
                    {
                        if (useBattlepass1)
                        {
                            if (useBattlepassloss)
                            {
                                Battlepass?.Call("AddFirstCurrency", player.userID, battlepassLossReward1);
                                {
                                    Player.Message(player, $"{lang.GetMessage("Battlepass loss Reward1", this, player.UserIDString)}{battlepassLossReward1.ToString()}", Prefix, SteamIDIcon);
                                }
                            }
                        }

                        if (useBattlepass2)
                        {
                            if (useBattlepassloss)
                            {
                                Battlepass?.Call("AddSecondCurrency", player.userID, battlepassWinReward2);
                                {
                                    Player.Message(player, $"{lang.GetMessage("Battlepass loss Reward2", this, player.UserIDString)}{battlepassLossReward2.ToString()}", Prefix, SteamIDIcon);
                                }
                            }
                        }
                    }
                }
            }
            else
            Player.Message(player, $"{lang.GetMessage("/guess Invalid Syntax", this, player.UserIDString)}", Prefix, SteamIDIcon);
            return;
        }

        void StartEvent()
        {
            if (eventActive)
            {
                return;
            }
            if (number == 0)
            {
                //Puts("Check if number is null then return");
                return;
            }
            else
            {
                Server.Broadcast($"{lang.GetMessage("Event Started", this)} between (<color=green>{minNumber.ToString()} and {maxNumber.ToString()}</color>)", Prefix, SteamIDIcon);
                Puts($"Started a random game and the number to guess is {number.ToString()}");
                eventActive = true;
                eventTimer = timer.Once(eventLength, () =>
                {
                    Server.Broadcast($"{lang.GetMessage("Event Timed Out", this)}<color=green>{number.ToString()}</color>", Prefix, SteamIDIcon);
                    eventActive = false;
                    playerInfo.Clear();
                });
            }
        }

        string DoHelpMenu()
        {
            StringBuilder x = new StringBuilder();
            x.AppendLine(msg("Help Message"));
            x.AppendLine(msg("Help Message1"));
            x.AppendLine(msg("Help Message2"));
            return x.ToString().TrimEnd();
        }

        bool IsNumber(string str)
        {
            foreach (char c in str)
                if (c < '0' || c > '9')
                    return false;
            return true;
        }

        object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }
            return value;
        }

        string msg(string key, string id = null) => lang.GetMessage(key, this, id);
    }
}
