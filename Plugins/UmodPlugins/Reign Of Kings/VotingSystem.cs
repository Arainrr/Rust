using System;
using System.Collections.Generic;
using System.Linq;
using CodeHatch.Engine.Networking;
using CodeHatch.Networking.Events.WorldEvents;
using CodeHatch.Networking.Events.WorldEvents.TimeEvents;
using CodeHatch.Networking.Events;
using CodeHatch.Common;
using Oxide.Core.Plugins;
using Oxide.Core;
using CodeHatch.UserInterface.Dialogues;

namespace Oxide.Plugins
{
    [Info("Voting System", "Sydney & D-Kay", "1.5.0", ResourceId = 1190)]
    class VotingSystem : ReignOfKingsPlugin
    {
        #region Variables

        [PluginReference("LevelSystem")]
        Plugin LevelSystem;

        [PluginReference("GrandExchange")]
        Plugin GrandExchange;

        private static int TimeVoteCooldown { get; set; }
        private static int WeatherVoteCooldown { get; set; }
        private static int RequiredStoreGoldTime { get; set; }
        private static int RequiredStoreGoldWeather { get; set; }

        private bool UseYNCommands => GetConfig("UseYNCommands", true);
        private int VoteDuration => GetConfig("VoteDuration", 30);
        private bool UseStoreGoldTime => GetConfig("UseStoreGoldTime", false);
        private bool UseStoreGoldWeather => GetConfig("UseStoreGoldWeather", false);
        private bool UseLevel => GetConfig("UseLevel", false);
        private int RequiredLevel => GetConfig("RequiredLevel", 3);
        private bool UsePermissions => GetConfig("UsePermissions", false);

        private Poll OngoingPoll { get; set; }

        private enum VoteType
        {
            Day,
            Night,
            Clear,
            Storm
        }

        private class Poll
        {
            public VoteType Type { get; private set; }
            public string Permission { get; private set; }
            public int Price { get; private set; }
            public bool Finished { get; private set; }
            public TimeSpan Cooldown { get; private set; }
            public HashSet<Vote> Votes { get; private set; }

            public Poll(VoteType type)
            {
                this.Type = type;
                switch (type)
                {
                    case VoteType.Day:
                        this.Permission = "VoteDay";
                        this.Price = RequiredStoreGoldTime;
                        this.Cooldown = TimeSpan.FromSeconds(TimeVoteCooldown);
                        break;
                    case VoteType.Night:
                        this.Permission = "VoteNight";
                        this.Price = RequiredStoreGoldTime;
                        this.Cooldown = TimeSpan.FromSeconds(TimeVoteCooldown);
                        break;
                    case VoteType.Clear:
                        this.Permission = "VoteWClear";
                        this.Price = RequiredStoreGoldWeather;
                        this.Cooldown = TimeSpan.FromSeconds(WeatherVoteCooldown);
                        break;
                    case VoteType.Storm:
                        this.Permission = "VoteWHeavy";
                        this.Price = RequiredStoreGoldWeather;
                        this.Cooldown = TimeSpan.FromSeconds(WeatherVoteCooldown);
                        break;
                }
                this.Finished = false;
                this.Votes = new HashSet<Vote>();
            }

            public void AddVote(Player player, bool accepted)
            {
                Votes.Add(new Vote(player, accepted));
            }

            public bool ChangeVote(Player player, bool accepted)
            {
                var vote = Votes.First(v => v.Player.Equals(player.Id));
                return vote.ChangeVote(accepted);
            }

            public bool HasVoted(Player player)
            {
                return this.Votes.Any(v => v.Player.Equals(player.Id));
            }

            public double GetStatus()
            {
                double yesCount = Votes.Count(v => v.Accepted);
                return yesCount / Votes.Count * 100;
            }

            public void Finish()
            {
                this.Finished = true;
            }
        }

        private class Vote
        {
            public ulong Player { get; private set; }
            public bool Accepted { get; private set; }

            public Vote(Player player, bool accepted)
            {
                this.Player = player.Id;
                this.Accepted = accepted;
            }

            public bool ChangeVote(bool accepted)
            {
                if (this.Accepted == accepted) return false;
                this.Accepted = accepted;
                return true;
            }
        }

        private readonly HashSet<string> _permissions = new HashSet<string>
        {
            "VoteDay",
            "VoteNight",
            "VoteWClear",
            "VoteWHeavy"
        };

        #endregion

        #region Save and Load data

        private void Loaded()
        {
            LoadconfigData();

            if (UseYNCommands)
            {
                cmd.AddChatCommand("y", this, "YesCommand");
                cmd.AddChatCommand("n", this, "NoCommand");
            }

            _permissions.Foreach(p => permission.RegisterPermission($"{Name}.{p}", this));
        }

        private void LoadconfigData()
        {
            TimeVoteCooldown = GetConfig("TimeVoteCooldown", 600);
            WeatherVoteCooldown = GetConfig("WeatherVoteCooldown", 180);
            RequiredStoreGoldTime = GetConfig("RequiredStoreGoldTime", 1000);
            RequiredStoreGoldWeather = GetConfig("RequiredStoreGoldWeather", 1000);
        }

        protected override void LoadDefaultConfig()
        {
            Config["UseYNCommands"] = UseYNCommands;
            Config["VoteDuration"] = VoteDuration;
            Config["TimeVoteCooldown"] = TimeVoteCooldown;
            Config["WeatherVoteCooldown"] = WeatherVoteCooldown;

            Config["UseStoreGoldTime"] = UseStoreGoldTime;
            Config["UseStoreGoldWeather"] = UseStoreGoldWeather;
            Config["RequiredStoreGoldTime"] = RequiredStoreGoldTime;
            Config["RequiredStoreGoldWeather"] = RequiredStoreGoldWeather;
            Config["UseLevel"] = UseLevel;
            Config["RequiredLevel"] = RequiredLevel;
            Config["UsePermissions"] = UsePermissions;
            SaveConfig();
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "voteDayStart", "{0} wants to change the [4444FF]time[-] to [4444FF]day[-]." },
                { "voteNightStart", "{0} wants to change the [4444FF]time[-] to [4444FF]night[-]." },
                { "voteClearStart", "{0} wants to [4444FF]clear[-] the [4444FF]weather[-]." },
                { "voteStormStart", "{0} wants to make it [4444FF]storm[-]." },
                { "passed", "passed" },
                { "failed", "failed" },
                { "voteDayFinished", "The vote to set the time to day has {0}. ({1}% of the votes were yes)" },
                { "voteNightFinished", "The vote to set the time to night has {0}. ({1}% of the votes were yes)" },
                { "voteClearFinished", "The vote to set the weather to clear has {0}. ({1}% of the votes were yes)" },
                { "voteStormFinished", "The vote to make it storm has {0}. ({1}% of the votes were yes)" },
                { "voteReset", "The vote timer has reset and a new vote can be started." },
                { "voteCommands", "Type [33CC33](/y)es[-] or [FF0000](/n)o[-] to participate in the vote." },
                { "voteDuration", "The vote will end in {0} seconds." },
                { "noOngoingVote", "There isn't an ongoing vote right now." },
                { "ongoingVote", "There is already an ongoing vote." },
                { "yes", "[33CC33]yes[-]" },
                { "no", "[ff0000]no[-]" },
                { "voteChanged", "{0} has changed their vote to {1}." },
                { "voted", "{0} has voted {1} to the current vote." },
                { "alreadyVoted", "You already cast your vote." },
                { "timeVoteCooldown", "There was a vote recently. There must be a {0} minutes delay between each time vote." },
                { "weatherVoteCooldown", "There was a vote recently. There must be a {0} minutes delay between each weather vote." },
                { "voteAlreadyStarted", "Sorry, someone started a vote already. Please try again in {0} minutes." },

                { "noVotePermission", "You don't have the permission to use this vote." },
                { "notHighEnoughLevel", "You don't meet the level requirements to start a vote (Level {0})." },
                { "notEnoughGold", "You do not have enough gold to start a vote ({0} gold)." },
                { "startVoteForGold", "Do you want to start the vote for {0} gold?" },

                { "helpTitle", "[0000FF]Voting System[-]" },
                { "helpDay", "[00FF00]/voteday[-] - Will start a vote to set the time to day." },
                { "helpNight", "[00FF00]/votenight[-] - Will start a vote to set the time to night." },
                { "helpWClear", "[00FF00]/votewclear[-] - Will start a vote to clear he weather." },
                { "helpWHeavy", "[00FF00]/votewheavy[-] - Will start a vote to make it storm." },
                { "helpYes", "[00FF00]/yes[-] - Vote yes." },
                { "helpNo", "[00FF00]/no[-] - Vote no." },
                { "helpYesAndY", "[00FF00]/y [-]or [00FF00]/yes[-] - Vote yes." },
                { "helpNoAndN", "[00FF00]/n [-]or [00FF00]/no[-] - Vote no." }
            }, this);
        }

        #endregion

        #region Commands

        [ChatCommand("voteday")]
        private void VoteDayCommand(Player player)
        {
            CheckVoteRequirements(player, VoteType.Day);
        }

        [ChatCommand("votenight")]
        private void VoteNightCommand(Player player)
        {
            CheckVoteRequirements(player, VoteType.Night);
        }

        [ChatCommand("votewclear")]
        private void VoteWClearCommand(Player player)
        {
            CheckVoteRequirements(player, VoteType.Clear);
        }

        [ChatCommand("votewheavy")]
        private void VoteWHeavyCommand(Player player)
        {
            CheckVoteRequirements(player, VoteType.Storm);
        }

        [ChatCommand("no")]
        private void NoCommand(Player player)
        {
            AddVote(player, false);
        }

        [ChatCommand("yes")]
        private void YesCommand(Player player)
        {
            AddVote(player, true);
        }

        #endregion

        #region Functions

        private void CheckVoteRequirements(Player player, VoteType type)
        {
            if (OngoingPoll != null)
            {
                string message;
                if (!OngoingPoll.Finished) message = "ongoingVote";
                else switch (OngoingPoll.Type)
                    {
                        case VoteType.Day:
                        case VoteType.Night:
                            message = "timeVoteCooldown";
                            break;
                        case VoteType.Clear:
                        case VoteType.Storm:
                            message = "weatherVoteCooldown";
                            break;
                        default:
                            return;
                    }
                SendError(player, message, OngoingPoll.Cooldown.TotalMinutes);
                return;
            }

            var poll = new Poll(type);

            if (UsePermissions && !CheckPermission(player, poll.Permission)) return;

            if (UseLevel && LevelSystem)
            {
                if ((int)LevelSystem.Call("GetCurrentLevel", player) < RequiredLevel)
                {
                    SendError(player, "notHighEnoughLevel", RequiredLevel);
                    return;
                }
            }
            if (GrandExchange)
            {
                switch (OngoingPoll.Type)
                {
                    case VoteType.Day:
                    case VoteType.Night:
                        if (!UseStoreGoldTime) break;
                        player.ShowConfirmPopup("Voting", string.Format(GetMessage("startVoteForGold", player), poll.Price), "Yes", "No", (options, dialogue1, data) => RemovePlayerGold(player, options, poll));
                        return;
                    case VoteType.Clear:
                    case VoteType.Storm:
                        if (!UseStoreGoldWeather) break;
                        player.ShowConfirmPopup("Voting", string.Format(GetMessage("startVoteForGold", player), poll.Price), "Yes", "No", (options, dialogue1, data) => RemovePlayerGold(player, options, poll));
                        return;
                    default:
                        return;
                }
            }

            VoteStart(player, poll);
        }
        
        private void RemovePlayerGold(Player player, Options options, Poll poll)
        {
            if (options != Options.Yes) return;
            if (OngoingPoll != null)
            {
                SendError(player, "voteAlreadyStarted", OngoingPoll.Cooldown.TotalMinutes);
                return;
            }
            if (!(bool) GrandExchange.Call("CanRemoveGold", player, (long) poll.Price))
            {
                SendError(player, "notEnoughGold", poll.Price);
                return;
            }

            GrandExchange.Call("RemoveGold", player, poll.Price);

            VoteStart(player, poll);
        }

        private void VoteStart(Player player, Poll poll)
        {
            OngoingPoll = poll;

            SendMessage($"vote{poll.Type}Start", player.DisplayName);
            SendMessage("voteCommands");
            SendMessage("voteDuration", VoteDuration);

            poll.AddVote(player, true);

            timer.In(VoteDuration, VoteFinish);
        }

        private void AddVote(Player player, bool accepted)
        {
            if (OngoingPoll == null || OngoingPoll.Finished)
            {
                SendError(player, "noOngoingVote");
                return;
            }

            if (OngoingPoll.HasVoted(player))
            {
                if (OngoingPoll.ChangeVote(player, accepted)) SendMessage("voteChanged", player.DisplayName, GetMessage(accepted ? "yes" : "no"));
                else SendError(player, "alreadyVoted");
                return;
            }

            OngoingPoll.AddVote(player, accepted);
            SendMessage("voted", player.DisplayName, GetMessage(accepted ? "yes" : "no"));
        }

        private void VoteFinish()
        {
            OngoingPoll.Finish();
            var percentage = OngoingPoll.GetStatus();
            SendMessage($"vote{OngoingPoll.Type}Finished", GetMessage(percentage < 50 ? "failed" : "passed"), percentage);
            if (percentage >= 50f)
            {
                switch (OngoingPoll.Type)
                {
                    case VoteType.Day:
                        EventManager.CallEvent(new TimeSetEvent(GameClock.Instance.HourOfSunriseStart, GameClock.Instance.DaySpeed));
                        break;
                    case VoteType.Night:
                        EventManager.CallEvent(new TimeSetEvent(GameClock.Instance.HourOfSunsetStart, GameClock.Instance.DaySpeed));
                        break;
                    case VoteType.Clear:
                        EventManager.CallEvent(new WeatherSetEvent(Weather.WeatherType.Clear));
                        break;
                    case VoteType.Storm:
                        EventManager.CallEvent(new WeatherSetEvent(Weather.WeatherType.PrecipitateHeavy));
                        break;
                }
            }
            timer.In((float)OngoingPoll.Cooldown.TotalSeconds, VotetimerReset);
        }

        private void VotetimerReset()
        {
            OngoingPoll = null;
            SendMessage("voteReset");
        }

        #endregion

        #region Hooks

        private void SendHelpText(Player player)
        {
            PrintToChat(player, GetMessage("helpTitle", player));
            PrintToChat(player, GetMessage("helpDay", player));
            PrintToChat(player, GetMessage("helpNight", player));
            PrintToChat(player, GetMessage("helpWClear", player));
            PrintToChat(player, GetMessage("helpWHeavy", player));
            if (UseYNCommands)
            {
                PrintToChat(player, GetMessage("helpYesAndY", player));
                PrintToChat(player, GetMessage("helpNoAndN", player));
            }
            else
            {
                PrintToChat(player, GetMessage("helpYes", player));
                PrintToChat(player, GetMessage("helpNo", player));
            }
        }

        #endregion

        #region Utility

        private bool CheckPermission(Player player, string permission)
        {
            if (HasPermission(player, permission)) return true;
            player.SendError(GetMessage("noVotePermission", player));
            return false;
        }
        private bool HasPermission(Player player, string permission)
        {
            return player.HasPermission($"{Name}.{permission}");
        }

        private void SendMessage(string key, params object[] obj) => PrintToChat(GetMessage(key), obj);
        private void SendMessage(Player player, string key, params object[] obj) => player.SendMessage(GetMessage(key, player), obj);
        private void SendError(Player player, string key, params object[] obj) => player.SendError(GetMessage(key, player), obj);

        private string GetMessage(string key, Player player = null) => lang.GetMessage(key, this, player?.Id.ToString());

        private T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null) return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        #endregion
    }
}