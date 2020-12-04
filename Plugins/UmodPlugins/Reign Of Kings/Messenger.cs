using System;
using System.Collections.Generic;

using CodeHatch.Common;
using CodeHatch.Engine.Core.Consoles;
using CodeHatch.Engine.Networking;
using CodeHatch.Networking.Events.Players;
using CodeHatch.UserInterface.Dialogues;

using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Messenger", "GeniusPlayUnique", "1.2.5")]
    [Description("A Messenger for sending letters to Players regardless of their connection status.")] /**(© GeniusPlayUnique)*/
    class Messenger : ReignOfKingsPlugin
    {
        static bool RealismModeActivated;
        static bool LicenseAgreementAccepted;
        readonly static string license = ""
            + "License Agreement for (the) 'Messenger.cs' [ReignOfKingsPlugin]: "
            + "With uploading this plugin to umod.org the rights holder (GeniusPlayUnique) grants the user the right to download and for usage of this plugin. "
            + "However these rights do NOT include the right to (re-)upload this plugin nor any modified version of it to umod.org or any other webside or to distribute it further in any way without written permission of the rights holder. "
            + "It is explicity allowed to modify this plugin at any time for personal usage ONLY. "
            + "If a modification should be made available for all users, please contact GeniusPlayUnique (rights holder) via umod.org to discuss the matter and the terms under which to gain permission to do so. "
            + "By changing 'Accept License Agreement' below to 'true' you accept this License Agreement.";

        protected override void LoadDefaultConfig()
        {
            Config["Activate Realism Mode", "(false == instant delivery)"] = new bool();
            Config["EULA", "License", "License Agreement"] = license;
            Config["EULA", "License Acceptance", "Accept License Agreement"] = new bool();
        }
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["LicenseAgreementError"] = "Before using the [i]Messenger.cs[/i] - plugin you need to accept the 'License Agreement' in the config-file.",
                ["HelpText"] = "\n"
                    + "[FFFFFF][i][u]Messenger Help[/u]:[/i][FFFFFF]" + "\n"
                    + "[FFFFFF]/Messenger.config - [666666]Displays a Popup for changing the DateStamp format.[FFFFFF]" + "\n"
                    + "[FFFFFF]/message \"<username>\" [subject line] - [666666]Displays a Popup for messaging said player.[FFFFFF]" + "\n"
                    + "[FFFFFF]/messages - [666666]Displays a Popup with all received messages.[FFFFFF]" + "\n"
                    + "[FFFFFF]/show [message index] - [666666]Displays the message with said index.[FFFFFF]" + "\n"
                    + "[FFFFFF]/reply [message index] - [666666]Displays a Popup for replying to the message with said index.[FFFFFF]" + "\n"
                    + "[FFFFFF]/delete [message index] - [666666]Delete the message with said index.[FFFFFF]" + "\n" + "{0}",
                ["AdminHelp"] = "[FFFFFF]/Messenger.init - [666666]Initializes the [i]Messenger.cs[/i]- plugin. (Admin Command)[FFFFFF]" + "\n",
                ["InitializingMessengerWarningTitle"] = "[FF0000]WARNING: Initializing Messenger?[FF0000]",
                ["InitializingMessengerWarningText"] = "You are about to initialize the \"Messenger.cs\"-plugin. \n By initializing the plugin all known Players will be added to the \"MessengerPlayersDataList.json\"-file. \n You do not need to initialize the plugin to use it, if the plugin does not get initialized the players will be added to the PlayerDataList-file as soon as they login the next time. \n\n Do you want to precede anyways?",
                ["ReinitializingMessengerWarningTitle"] = "[FF0000]WARNING: Reinitializing Messenger?[FF0000]",
                ["ReinitializingMessengerWarningText"] = "You are about to reinitialize the \"Messenger.cs\"-plugin. \n All Data will be lost. \n\n Do you want to precede?",
                ["Confirm"] = "Confirm",
                ["Cancel"] = "Cancel",
                ["MessengerInitializedWithoutAdditionsToPlayerDataListInfo"] = "\"Messenger.cs\" has been initialized. No players were added to the PlayerDataList.",
                ["MessengerInitializedWithoutAdditionsToPlayerDataListLog"] = "{0} ({1}) has initialized the plugin. No players were added to the PlayerDataList.",
                ["MessengerInitializedWithAdditionToPlayerDataListInfo"] = "\"Messenger.cs\" has been initialized. {0} player was added to the PlayerDataList.",
                ["MessengerInitializedWithAdditionToPlayerDataListLog"] = "{0} ({1}) has initialized the plugin. {2} player was added to the PlayerDataList.",
                ["MessengerInitializedWithAdditionsToPlayerDataListInfo"] = "\"Messenger.cs\" has been initialized. {0} players were added to the PlayerDataList.",
                ["MessengerInitializedWithAdditionsToPlayerDataListLog"] = "{0} ({1}) has initialized the plugin. {2} players were added to the PlayerDataList.",
                ["MessangerConfigTitle"] = "Messanger Config:",
                ["MessangerConfigText"] = "Change DateStamp Settings:" + "\n" + "([i]Insert setting id into the field below[/i])" + "\n \n" + "#1: {0}" + "\n" + "#2 {1}" + "\n" + "#3 {2}" + "\n" + "#4 {3}" + "\n" + "#5 {4}",
                ["MessangerConfigSettingIdError"] = "There is no settings id {0}.",
                ["ChangedDateStampSettingsSavedInfo"] = "Changed DateStamp settings saved.",
                ["MessageSubjectLine"] = "Subject: {0}",
                ["MessangerTitle"] = "Messanger:",
                ["MessageRecipientLine"] = "To: {0} \n \n \n",
                ["Ok"] = "Ok",
                ["WriteMessage"] = "",
                ["Send"] = "Send",
                ["ConfirmMessageTitle"] = "Send this message?",
                ["Confirm"] = "Confirm",
                ["NoRecipientError"] = "Could not find {0}.",
                ["MultipleRecipientsError"] = "{0} matches {1} players. Please be more specific.",
                ["NoPlayerDataError"] = "There is no PlayerData for the Player you are trying to send a message to. Please wait until the player was online again.",
                ["MessageReceivedInfo"] = "You have received a message from {0}.",
                ["MessageSentInfo"] = "A Messenger has been sent to [i]{0}[/i] with your message.",
                ["MessageSentLog"] = "{0} ({1}) has sent a Message to {2} ({3}). [{4}]",
                ["NoMessagesError"] = "You have no messages.",
                ["MessageSenderLine"] = "From: [u]{0}[/u]",
                ["MessagesTitle"] = "Messages:",
                ["ShowCommandHelp"] = "Usage: /show [i]<message index number>[/i].",
                ["MessageIndexError"] = "You have no message with the index {0}.",
                ["MessageTitle"] = "Message #",
                ["ReplyCommandHelp"] = "Usage: /reply [i]<message index number>[/i].",
                ["Re"] = "Re: {0}",
                ["DeleteCommandHelp"] = "Usage: /delete [i]<message index number>[/i].",
                ["DeleteMessageTitle"] = "Delete Message #{0}?",
                ["Delete"] = "Delete",
                ["MessageDeletedInfo"] = " from [i]{0}[/i] has been deleted.",
                ["NewMessageInfo"] = "You have a new message.",
                ["NewMessagesInfo"] = "You have {0} new messages.",
                ["UnreadMessageInfo"] = "You have an unread message.",
                ["UnreadMessagesInfo"] = "You have {0} unread messages.",
                ["PlayerAddedToPlayerDataListLog"] = "{0} ({1}) was added to the PlayersDataList.",
            }, this, "en");
        }

        private class Message
        {
            public ulong SenderId { get; set; }
            public string SenderName { get; set; }
            public string SubjectLine { get; set; }
            public string Text { get; set; }
            public bool Read { get; set; }
            public DateTime DateStamp { get; set; }

            public Message()
            {
            }

            public Message(Player sender, string subjectLine, string text)
            {
                SenderId = sender.Id;
                SenderName = sender.Name;
                SubjectLine = subjectLine;
                Text = text;
                Read = false;
                DateStamp = DateTime.Now;
            }
        }

        private class PlayerData
        {
            public string Id;
            public string Name { get; set; }
            public string DateTimeFormat { get; set; }
            public uint NewMessages { get; set; }
            public List<Message> MessagesList { get; set; }

            public PlayerData()
            {
            }

            public PlayerData(Player player, List<Message> messagesList)
            {
                Id = player.Id.ToString();
                Name = player.Name;
                DateTimeFormat = "dd.MM.yy HH:mm:ss UTC zzz";
                NewMessages = 0;
                MessagesList = messagesList;
            }
        }

        static Dictionary<ulong, PlayerData> PlayersDataList = new Dictionary<ulong, PlayerData>();

        void Init()
        {
            if (!Config["EULA", "License", "License Agreement"].Equals(license))
            {
                LoadDefaultConfig();
            }

            PlayersDataList = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerData>>("MessengerPlayersDataList");

            LoadConfig();
            LicenseAgreementAccepted = Convert.ToBoolean(Config["EULA", "License Acceptance", "Accept License Agreement"]);
            RealismModeActivated = Convert.ToBoolean(Config["Activate Realism Mode", "(false == instant delivery)"]);
        }

        void Unload()
        {
            SaveList();
        }

        void OnServerSave()
        {
            SaveList();
        }

        void OnServerShutdown()
        {
            SaveList();
        }

        void SaveList()
        {
            Interface.Oxide.DataFileSystem.WriteObject("MessengerPlayersDataList", PlayersDataList);
        }

        [ChatCommand("Messenger")]
        void ShowHelp(Player player)
        {
            if (!LicenseAgreementAccepted)
            {
                //player.SendError("Before using the [i]Messenger.cs[/i]-plugin you need to accept the 'License Agreement' in the config-file.");
                player.SendError(lang.GetMessage("LicenseAgreementError", this, player.Id.ToString()));
                return;
            }
            /*
            string helptext = "\n"
                + "[FFFFFF][i][u]Messenger Help[/u]:[/i][FFFFFF]" + "\n"
                + "[FFFFFF]/Messenger.config - [666666]Displays a Popup for changing the DateStamp format.[FFFFFF]" + "\n"
                + "[FFFFFF]/message \"<username>\" [subject line] - [666666]Displays a Popup for messaging said player.[FFFFFF]" + "\n"
                + "[FFFFFF]/messages - [666666]Displays a Popup with all received messages.[FFFFFF]" + "\n"
                + "[FFFFFF]/show [message index] - [666666]Displays the message with said index.[FFFFFF]" + "\n"
                + "[FFFFFF]/reply [message index] - [666666]Displays a Popup for replying to the message with said index.[FFFFFF]" + "\n"
                + "[FFFFFF]/delete [message index] - [666666]Delete the message with said index.[FFFFFF]" + "\n";
            */
            if (player.HasPermission("admin"))
            {
                //helptext = helptext + "[FFFFFF]/Messenger.init - [666666]Initializes the [i]Messenger.cs[/i]- plugin. (Admin Command)[FFFFFF]" + "\n";
                //PrintToChat(player, helptext);
                PrintToChat(player, string.Format(lang.GetMessage("HelpText", this, player.Id.ToString()), lang.GetMessage("AdminHelp", this, player.Id.ToString())));
            }
            else
            {
                //PrintToChat(player, helptext);
                PrintToChat(player, lang.GetMessage("HelpText", this, player.Id.ToString()), " ");
            }
        }

        [ChatCommand("Messenger.init")]
        void InitializeMessenger(Player player)
        {
            if (!LicenseAgreementAccepted)
            {
                //player.SendError("Before using the [i]Messenger.cs[/i]-plugin you need to accept the 'License Agreement' in the config-file.");
                player.SendError(lang.GetMessage("LicenseAgreementError", this, player.Id.ToString()));
                return;
            }

            if (!player.HasPermission("admin")) return;

            if (PlayersDataList.Count != 0)
            {
                //player.ShowConfirmPopup("[FF0000]WARNING: Reinitializing Messenger?[FF0000]", "You are about to reinitialize the \"Messenger.cs\"-plugin. \n All Data will be lost. \n\n Do you want to precede?", "Confirm", "Cancel", (selection, dialogue, contextData) => InitializingMessenger(player, selection, dialogue, contextData));
                player.ShowConfirmPopup(lang.GetMessage("ReinitializingMessengerWarningTitle", this, player.Id.ToString()), lang.GetMessage("ReinitializingMessengerWarningText", this, player.Id.ToString()), lang.GetMessage("Confirm", this, player.Id.ToString()), lang.GetMessage("Cancel", this, player.Id.ToString()), (selection, dialogue, contextData) => InitializingMessenger(player, selection, dialogue, contextData));
            }
            else
            {
                //player.ShowConfirmPopup("[FF0000]WARNING: Initializing Messenger?[FF0000]", "You are about to initialize the \"Messenger.cs\"-plugin. \n By initializing the plugin all known Players will be added to the \"MessengerPlayersDataList.json\"-file. \n You do not need to initialize the plugin to use it, if the plugin does not get initialized the players will be added to the PlayerDataList-file as soon as they login the next time. \n\n Do you want to precede anyways?", "Confirm", "Cancel", (selection, dialogue, contextData) => InitializingMessenger(player, selection, dialogue, contextData));
                player.ShowConfirmPopup(lang.GetMessage("InitializingMessengerWarningTitle", this, player.Id.ToString()), lang.GetMessage("InitializingMessengerWarningText", this, player.Id.ToString()), lang.GetMessage("Confirm", this, player.Id.ToString()), lang.GetMessage("Cancel", this, player.Id.ToString()), (selection, dialogue, contextData) => InitializingMessenger(player, selection, dialogue, contextData));
            }
        }

        void InitializingMessenger(Player player, Options selection, Dialogue dialogue, object contextData)
        {
            if (selection == Options.OK || selection == Options.Yes)
            {
                Initialize(player);
            }
            else
            {
                return;
            }
        }

        void Initialize(Player player)
        {
            PlayersDataList.Clear();
            int i = 0;

            foreach (IPlayer Iplayer in covalence.Players.All)
            {
                ulong playerId;
                ulong.TryParse(Iplayer.Id, out playerId);

                PlayersDataList.Add(playerId, new PlayerData { Id = Iplayer.Id, Name = Iplayer.Name, DateTimeFormat = "dd.MM.yy HH:mm:ss UTC zzz", NewMessages = 0, MessagesList = new List<Message>() });

                i++;
            }

            SaveList();

            if (i == 0)
            {
                //player.SendNews("\"Messenger.cs\" has been initialized. No players were added to the PlayerDataList.");
                player.SendNews(lang.GetMessage("MessengerInitializedWithoutAdditionsToPlayerDataListInfo", this, player.Id.ToString()));
                //Puts(player.Name + " (" + player.Id.ToString() + ") has initialized the plugin. No players were added to the PlayerDataList.");
                Puts(string.Format(lang.GetMessage("MessengerInitializedWithoutAdditionsToPlayerDataListLog", this), player.Name, player.Id.ToString()));
            }
            else if (i == 1)
            {
                //player.SendNews("\"Messenger.cs\" has been initialized. " + i.ToString() + " player was added to the PlayerDataList.");
                player.SendNews(string.Format(lang.GetMessage("MessengerInitializedWithAdditionToPlayerDataListInfo", this, player.Id.ToString()), i.ToString()));
                //Puts(player.Name + " (" + player.Id.ToString() + ") has initialized the plugin. " + i.ToString() + " player was added to the PlayerDataList.");
                Puts(string.Format(lang.GetMessage("MessengerInitializedWithAdditionToPlayerDataListLog", this), player.Name, player.Id.ToString(), i.ToString()));
            }
            else
            {
                //player.SendNews("\"Messenger.cs\" has been initialized. " + i.ToString() + " players were added to the PlayerDataList.");
                player.SendNews(string.Format(lang.GetMessage("MessengerInitializedWithAdditionsToPlayerDataListInfo", this, player.Id.ToString()), i.ToString()));
                //Puts(player.Name + " (" + player.Id.ToString() + ") has initialized the plugin. " + i.ToString() + " players were added to the PlayerDataList.");
                Puts(string.Format(lang.GetMessage("MessengerInitializedWithAdditionsToPlayerDataListLog", this), player.Name, player.Id.ToString(), i.ToString()));
            }
        }

        [ChatCommand("Messenger.config")]
        void ConfigurateMessenger(Player player)
        {
            if (!LicenseAgreementAccepted)
            {
                //player.SendError("Before using the [i]Messenger.cs[/i]-plugin you need to accept the 'License Agreement' in the config-file.");
                player.SendError(lang.GetMessage("LicenseAgreementError", this, player.Id.ToString()));
                return;
            }

            //player.ShowInputPopup("Messanger Config:", "Change DateStamp Settings:" + "\n" + "([i]Insert setting id into the field below[/i])" + "\n \n" + "#1: " + DateTime.Now.ToString("        dd.MM.yy  HH:mm:ss       UTC zzz") + "\n" + "#2 " + DateTime.Now.ToString("        dd.MM.yy  hh:mm:ss tt UTC zzz") + "\n" + "#3 " + DateTime.Now.ToString("ddd. dd.MM.yy  HH:mm:ss       UTC zzz") + "\n" + "#4 " + DateTime.Now.ToString("ddd. dd.MM.yy  hh:mm:ss tt UTC zzz") + "\n" + "#5 " + DateTime.Now.ToString("                         yyyyMMddHHmmss zzz"), "", "Confirm", "Cancel", (options, dialogue, data) => ChangeMessengerConfig(player, options, dialogue, data));
            player.ShowInputPopup(lang.GetMessage("MessangerConfigTitle", this, player.Id.ToString()), string.Format(lang.GetMessage("MessangerConfigText", this, player.Id.ToString()), DateTime.Now.ToString("        dd.MM.yy  HH:mm:ss       UTC zzz"), DateTime.Now.ToString("        dd.MM.yy  hh:mm:ss tt UTC zzz"), DateTime.Now.ToString("ddd. dd.MM.yy  HH:mm:ss       UTC zzz"), DateTime.Now.ToString("ddd. dd.MM.yy  hh:mm:ss tt UTC zzz"), DateTime.Now.ToString("                         yyyyMMddHHmmss zzz")), "", lang.GetMessage("Confirm", this, player.Id.ToString()), lang.GetMessage("Cancel", this, player.Id.ToString()), (options, dialogue, data) => ChangeMessengerConfig(player, options, dialogue, data));
        }

        void ChangeMessengerConfig(Player player, Options options, Dialogue dialogue, object data)
        {
            if (options.Equals(Options.Cancel) || options.Equals(Options.No) || dialogue.ValueMessage.IsNullEmptyOrWhite()) return;

            int setting = 0;
            Int32.TryParse(string.Concat(dialogue.ValueMessage), out setting);

            switch (setting)
            {
                case 1:
                    PlayersDataList[player.Id].DateTimeFormat = "dd.MM.yy HH:mm:ss UTC zzz";
                    break;
                case 2:
                    PlayersDataList[player.Id].DateTimeFormat = "dd.MM.yy hh:mm:ss tt UTC zzz";
                    break;
                case 3:
                    PlayersDataList[player.Id].DateTimeFormat = "ddd. dd.MM.yy HH:mm:ss UTC zzz";
                    break;
                case 4:
                    PlayersDataList[player.Id].DateTimeFormat = "ddd. dd.MM.yy hh:mm:ss tt UTC zzz";
                    break;
                case 5:
                    PlayersDataList[player.Id].DateTimeFormat = "yyyyMMddHHmmss zzz";
                    break;
                default:
                    //player.SendError("There is no settings id " + setting.ToString());
                    player.SendError(string.Format(lang.GetMessage("MessangerConfigSettingIdError", this, player.Id.ToString()), setting.ToString()));
                    return;
            }

            SaveList();

            //player.SendNews("Changed DateStamp settings saved.");
            player.SendNews(lang.GetMessage("ChangedDateStampSettingsSavedInfo", this, player.Id.ToString()));
        }

        [ChatCommand("message")]
        void MessageInputPopup(Player player, string command, string[] args)
        {
            ShowMessageInputPopup(player, command, true, -1, args);
        }

        void ShowMessageInputPopup(Player player, string command, bool chat, int MessageIndex, string[] args)
        {
            if (!LicenseAgreementAccepted)
            {
                //player.SendError("Before using the [i]Messenger.cs[/i]-plugin you need to accept the 'License Agreement' in the config-file.");
                player.SendError(lang.GetMessage("LicenseAgreementError", this, player.Id.ToString()));
                return;
            }

            int possibleRecipients = 0;
            string recipientsName = "";

            foreach (IPlayer possibleRecipient in covalence.Players.All)
            {
                if (possibleRecipient.Name.ToLower().Contains(args[0].ToLower()))
                {
                    recipientsName = possibleRecipient.Name;
                    possibleRecipients++;
                }
            }

            if (possibleRecipients == 1)
            {
                IPlayer Irecipient = covalence.Players.FindPlayer(recipientsName);
                string subjectLine = "";
                if (args.Length > 1)
                {
                    //subjectLine = "Subject: " + args.AllExcept<string>(0).JoinToString<string>(" ");
                    subjectLine = args.AllExcept<string>(0).JoinToString<string>(" ");
                }

                //player.ShowInputPopup("Messanger:", "To: " + Irecipient.Name + "\n \n \n" + subjectLine, "Write Message", "Send", "Cancel", (options, dialogue, data) => SendOrSaveMessage(player, Irecipient, subjectLine, options, dialogue, data));
                player.ShowInputPopup(lang.GetMessage("MessangerTitle", this, player.Id.ToString()), string.Format(lang.GetMessage("MessageRecipientLine", this, player.Id.ToString()), Irecipient.Name) + string.Format(lang.GetMessage("MessageSubjectLine", this, player.Id.ToString()), subjectLine), lang.GetMessage("WriteMessage", this, player.Id.ToString()), lang.GetMessage("Send", this, player.Id.ToString()), lang.GetMessage("Cancel", this, player.Id.ToString()), (options, dialogue, data) => SendOrSaveMessage(player, Irecipient, subjectLine, chat, MessageIndex, options, dialogue, data));
            }
            else if (possibleRecipients == 0)
            {
                //player.SendError("Could not find " + args[0] + ".");
                player.SendError(string.Format(lang.GetMessage("NoRecipientError", this, player.Id.ToString()), args[0]));
                return;
            }
            else if (possibleRecipients > 1)
            {
                //player.SendError(args[0] + " matches " + possibleRecipients.ToString() + " players. Please be more specific.");
                player.SendError(string.Format(lang.GetMessage("MultipleRecipientsError", this, player.Id.ToString()), args[0], possibleRecipients.ToString()));
                return;
            }
        }

        void SendOrSaveMessage(Player sender, IPlayer Irecipient, string subjectLine, bool chat, int MessageIndex, Options options, Dialogue dialogue, object data)
        {
            if (options.Equals(Options.Cancel) || options.Equals(Options.No)) return;

            string text;

            if (dialogue.ValueMessage.IsNullEmptyOrWhite())
            {
                text = " ";
            }
            else
            {
                text = string.Concat(dialogue.ValueMessage);
            }

            Message message = new Message(sender, subjectLine, text);
            ulong recipientId = 0;
            ulong.TryParse(Irecipient.Id, out recipientId);

            if (PlayersDataList.ContainsKey(recipientId))
            {
                sender.ShowConfirmPopup(lang.GetMessage("ConfirmMessageTitle", this, sender.Id.ToString()), string.Format(lang.GetMessage("MessageRecipientLine", this, sender.Id.ToString()), Irecipient.Name) + string.Format(lang.GetMessage("MessageSubjectLine", this, sender.Id.ToString()), message.SubjectLine) + "\n \n" + message.Text, lang.GetMessage("Confirm", this, sender.Id.ToString()), lang.GetMessage("Cancel", this, sender.Id.ToString()), (optionsNew, dialogueNew, dataNew) => ConfirmSending(sender, Irecipient, recipientId, message, chat, MessageIndex, optionsNew, dialogueNew, dataNew));
            }
            else
            {
                //sender.SendError("There is no PlayerData for the Player you are trying to send a message to. Please wait until the player was online again.");
                sender.SendError(lang.GetMessage("NoPlayerDataError", this, sender.Id.ToString()));
                return;
            }
        }

        void ConfirmSending(Player sender, IPlayer Irecipient, ulong recipientId, Message message, bool chat, int MessageIndex, Options options, Dialogue dialogue, object data)
        {
            if (options.Equals(Options.Cancel) || options.Equals(Options.No)) return;

            PlayersDataList[recipientId].MessagesList.Insert(0, message);
            SaveList();

            if (Irecipient.IsConnected && !Irecipient.IsSleeping)
            {
                Player recipient = Server.GetPlayerById(recipientId);
                if (RealismModeActivated)
                {
                    float senderPosX;
                    float senderPosY;
                    float senderPosZ;
                    covalence.Players.FindPlayerById(sender.Id.ToString()).Position(out senderPosX, out senderPosY, out senderPosZ);

                    float recipientPosX;
                    float recipientPosY;
                    float recipientPosZ;
                    Irecipient.Position(out recipientPosX, out recipientPosY, out recipientPosZ);

                    double heightDifference = Math.Abs((senderPosY - recipientPosY));
                    double horizontalDistance = (Math.Sqrt((Math.Pow((recipientPosX - senderPosX), 2.0) + (Math.Pow((recipientPosZ - senderPosZ), 2.0)))) * 1.1);

                    float DeliveryTimeInSeconds = (float)(((heightDifference / 400) + (horizontalDistance / 4000) * 3600) / 2);

                    timer.In(DeliveryTimeInSeconds, () => recipient.SendNews(string.Format(lang.GetMessage("MessageReceivedInfo", this, recipient.Id.ToString()), sender.DisplayName)));
                }
                else
                {
                    //recipient.SendNews("You have received a message from " + sender.DisplayName + ".");
                    recipient.SendNews(string.Format(lang.GetMessage("MessageReceivedInfo", this, recipient.Id.ToString()), sender.DisplayName));
                }
            }
            else if (!Irecipient.IsConnected && Irecipient.IsSleeping)
            {
                uint currentNewMessagesCounter = PlayersDataList[recipientId].NewMessages;
                PlayersDataList[recipientId].NewMessages = (currentNewMessagesCounter + 1);
                SaveList();
            }

            //sender.SendNews("A Messenger has been sent to [i]" + Irecipient.Name + "[/i] with your message.");
            sender.SendNews(string.Format(lang.GetMessage("MessageSentInfo", this, sender.Id.ToString()), Irecipient.Name));
            //Puts(sender.Name + " (" + sender.Id.ToString() + ") " + "has sent a Message to " + Irecipient.Name + " (" + Irecipient.Id.ToString() + "). [" + message.SubjectLine + "]");
            Puts(string.Format(lang.GetMessage("MessageSentLog", this), sender.Name, sender.Id.ToString(), Irecipient.Name, Irecipient.Id.ToString(), string.Format(lang.GetMessage("MessageSubjectLine", this, lang.GetMessage("ServerLogLanguage", this)), message.SubjectLine)));

            if (!chat)
            {
                double messageIndex = (0.0 + MessageIndex);
                double page = (messageIndex / 7.0);

                int pageNumber = (int)Math.Floor(page);

                ShowMessagesPopup(sender, pageNumber);
            }
        }

        [ChatCommand("messages")]
        void ShowMessages(Player player)
        {
            ShowMessagesPopup(player, 0);
        }

        void ShowMessagesPopup(Player player, int pageNumber)
        {
            if (!LicenseAgreementAccepted)
            {
                //player.SendError("Before using the [i]Messenger.cs[/i]-plugin you need to accept the 'License Agreement' in the config-file.");
                player.SendError(lang.GetMessage("LicenseAgreementError", this, player.Id.ToString()));
                return;
            }

            if (!PlayersDataList.ContainsKey(player.Id))
            {
                //player.SendError("You have no messages.");
                player.SendError(lang.GetMessage("NoMessagesError", this, player.Id.ToString()));
                return;
            }
            else if (PlayersDataList[player.Id].MessagesList.Count == 0)
            {
                //player.SendError("You have no messages.");
                player.SendError(lang.GetMessage("NoMessagesError", this, player.Id.ToString()));
                return;
            }

            int page = 1;
            string messages = "\n";

            if (PlayersDataList[player.Id].MessagesList.Count < 8)
            {
                for (int i = 0; i < PlayersDataList[player.Id].MessagesList.Count; i++)
                {
                    Message message = PlayersDataList[player.Id].MessagesList.GetAt<Message>(i);
                    int ii = i + 1;

                    string DateStamp = message.DateStamp.ToString(PlayersDataList[player.Id].DateTimeFormat);

                    if (message.Read.Equals(false))
                    {
                        //messages = messages + "#" + ii.ToString() + "   " + "[b]" + "From: " + "[u]" + message.SenderName + "[/u]" + "[/b]" + "\n" + "[" + DateStamp + "]" + "\n      " + "[b][i]" + message.SubjectLine + "[/i][/b]" + "\n";
                        messages = messages + "#" + ii.ToString() + "   " + "[b]" + string.Format(lang.GetMessage("MessageSenderLine", this, player.Id.ToString()), message.SenderName) + "[/b]" + "\n" + "[" + DateStamp + "]" + "\n      " + "[b][i]" + string.Format(lang.GetMessage("MessageSubjectLine", this, player.Id.ToString()), message.SubjectLine) + "[/i][/b]" + "\n\n";
                    }
                    else
                    {
                        //messages = messages + "#" + ii.ToString() + "   " + "From: " + "[u]" + message.SenderName + "[/u]" + "\n" + "[" + DateStamp + "]" + "\n      " + message.SubjectLine + "\n";
                        messages = messages + "#" + ii.ToString() + "   " + string.Format(lang.GetMessage("MessageSenderLine", this, player.Id.ToString()), message.SenderName) + "\n" + "[" + DateStamp + "]" + "\n      " + string.Format(lang.GetMessage("MessageSubjectLine", this, player.Id.ToString()), message.SubjectLine) + "\n\n";
                    }
                }
            }
            else
            {
                int pageMessagesCount;
                int pageCount = (PlayersDataList[player.Id].MessagesList.Count / 7);

                if ((pageCount * 7) < PlayersDataList[player.Id].MessagesList.Count)
                {
                    pageCount = (pageCount + 1);
                }

                if (pageNumber.IsNullOrDeleted() || pageNumber < 1)
                {
                    pageNumber = 0;
                }
                else if (pageNumber > (pageCount - 1))
                {
                    pageNumber = (pageCount - 1);
                }

                page = (pageNumber + 1);

                if (((pageNumber + 1) * 7) > PlayersDataList[player.Id].MessagesList.Count)
                {
                    pageMessagesCount = (PlayersDataList[player.Id].MessagesList.Count - (pageNumber * 7));
                }
                else
                {
                    pageMessagesCount = 7;
                }

                for (int i = (pageNumber * 7); i < ((pageNumber * 7) + pageMessagesCount); i++)
                {
                    Message message = PlayersDataList[player.Id].MessagesList.GetAt<Message>(i);
                    int ii = (i + 1);

                    string DateStamp = message.DateStamp.ToString(PlayersDataList[player.Id].DateTimeFormat);

                    if (message.Read.Equals(false))
                    {
                        //messages = messages + "#" + ii.ToString() + "   " + "[b]" + "From: " + "[u]" + message.SenderName + "[/u]" + "[/b]" + "\n" + "[" + DateStamp + "]" + "\n      " + "[b][i]" + message.SubjectLine + "[/i][/b]" + "\n";
                        messages = messages + "#" + ii.ToString() + "   " + "[b]" + string.Format(lang.GetMessage("MessageSenderLine", this, player.Id.ToString()), message.SenderName) + "[/b]" + "\n" + "[" + DateStamp + "]" + "\n      " + "[b][i]" + string.Format(lang.GetMessage("MessageSubjectLine", this, player.Id.ToString()), message.SubjectLine) + "[/i][/b]" + "\n\n";
                    }
                    else
                    {
                        //messages = messages + "#" + ii.ToString() + "   " + "From: " + "[u]" + message.SenderName + "[/u]" + "\n" + "[" + DateStamp + "]" + "\n      " + message.SubjectLine + "\n";
                        messages = messages + "#" + ii.ToString() + "   " + string.Format(lang.GetMessage("MessageSenderLine", this, player.Id.ToString()), message.SenderName) + "\n" + "[" + DateStamp + "]" + "\n      " + string.Format(lang.GetMessage("MessageSubjectLine", this, player.Id.ToString()), message.SubjectLine) + "\n\n";
                    }
                }

                messages = messages + "[" + page + "/" + pageCount.ToString() + "]";
            }

            //player.ShowPopup("Messages:", messages);
            //player.ShowPopup(lang.GetMessage("MessagesTitle", this), messages);
            player.ShowInputPopup(lang.GetMessage("MessagesTitle", this, player.Id.ToString()), messages, lang.GetMessage("/", this, player.Id.ToString()), lang.GetMessage("Ok", this, player.Id.ToString()), lang.GetMessage("Cancel", this, player.Id.ToString()), (options, dialogue, data) => ToPageOrShowOrReplyToOrDeleteMessage(player, options, dialogue, data));
        }

        void ToPageOrShowOrReplyToOrDeleteMessage(Player player, Options options, Dialogue dialogue, object data)
        {
            int pageNumber = 0;

            if (options.Equals(Options.Cancel) || options.Equals(Options.No)) return;

            if (dialogue.ValueMessage.StartsWith("/show"))
            {
                string command = dialogue.ValueMessage.Remove(0, 5);
                string[] args = new string[] { command };

                ShowMessagePopup(player, "show", false, args);
            }
            else if (dialogue.ValueMessage.StartsWith("/reply"))
            {
                string command = dialogue.ValueMessage.Remove(0, 6);
                string[] args = new string[] { command };

                ReplyToMessagePopup(player, "reply", false, args);
            }
            else if (dialogue.ValueMessage.StartsWith("/delete"))
            {
                string command = dialogue.ValueMessage.Remove(0, 7);
                string[] args = new string[] { command };

                DeleteMessagePopup(player, "delete", false, args);
            }
            else if (int.TryParse(dialogue.ValueMessage.Remove(0, 1).ToString(), out pageNumber))
            {
                int.TryParse(dialogue.ValueMessage.Remove(0, 1).ToString(), out pageNumber);
                pageNumber = (pageNumber - 1);

                ShowMessagesPopup(player, pageNumber);
            }
        }

        [ChatCommand("show")]
        void ShowMessage(Player player, string command, string[] args)
        {
            ShowMessagePopup(player, command, true, args);
        }

        void ShowMessagePopup(Player player, string command, bool chat, string[] args)
        {
            if (!LicenseAgreementAccepted)
            {
                //player.SendError("Before using the [i]Messenger.cs[/i]-plugin you need to accept the 'License Agreement' in the config-file.");
                player.SendError(lang.GetMessage("LicenseAgreementError", this, player.Id.ToString()));
                return;
            }

            if (args.Length != 1)
            {
                //player.SendError("Usage: /show [i]<message index number>[/i].");
                player.SendError(lang.GetMessage("ShowCommandHelp", this, player.Id.ToString()));
            }

            int MessageIndex = 0;

            if (Int32.TryParse(args[0], out MessageIndex))
            {
                if (!PlayersDataList.ContainsKey(player.Id))
                {
                    //player.SendError("You have no messages.");
                    player.SendError(lang.GetMessage("NoMessagesError", this, player.Id.ToString()));
                    return;
                }
                else if (PlayersDataList[player.Id].MessagesList.Count == 0)
                {
                    //player.SendError("You have no messages.");
                    player.SendError(lang.GetMessage("NoMessagesError", this, player.Id.ToString()));
                    return;
                }
                else if (PlayersDataList[player.Id].MessagesList.Count < MessageIndex)
                {
                    //player.SendError("You have no message with the index " + MessageIndex.ToString() + ".");
                    player.SendError(string.Format(lang.GetMessage("MessageIndexError", this, player.Id.ToString()), MessageIndex.ToString()));
                    return;
                }

                Message message = PlayersDataList[player.Id].MessagesList.GetAt<Message>(MessageIndex - 1);

                //player.ShowPopup("Message #" + MessageIndex.ToString(), "From: " + message.SenderName + "\n \n \n" + message.SubjectLine + "\n \n" + message.Text);
                //player.ShowPopup(lang.GetMessage("MessageTitle", this) + MessageIndex.ToString(), string.Format(lang.GetMessage("MessageSenderLine", this), message.SenderName) + "\n \n \n" + string.Format(lang.GetMessage("MessageSubjectLine", this), message.SubjectLine) + "\n \n" + message.Text);
                PlayersDataList[player.Id].MessagesList.GetAt<Message>(MessageIndex - 1).Read = true;
                player.ShowInputPopup(lang.GetMessage("MessageTitle", this, player.Id.ToString()) + MessageIndex.ToString(), string.Format(lang.GetMessage("MessageSenderLine", this, player.Id.ToString()), message.SenderName) + "\n \n \n" + string.Format(lang.GetMessage("MessageSubjectLine", this, player.Id.ToString()), message.SubjectLine) + "\n \n" + message.Text, lang.GetMessage("/", this, player.Id.ToString()), lang.GetMessage("Ok", this, player.Id.ToString()), lang.GetMessage("Cancel", this, player.Id.ToString()), (options, dialogue, data) => ReplyToOrDeleteMessage(player, MessageIndex, chat, options, dialogue, data));
            }
        }

        void ReplyToOrDeleteMessage(Player player, int MessageIndex, bool chat, Options options, Dialogue dialogue, object data)
        {
            if (dialogue.ValueMessage.StartsWith("/reply"))
            {
                string[] args = new string[] { MessageIndex.ToString() };
                ReplyToMessagePopup(player, "reply", chat, args);
                return;
            }
            else if (dialogue.ValueMessage.StartsWith("/delete"))
            {
                string[] args = new string[] { MessageIndex.ToString() };
                DeleteMessagePopup(player, "delete", chat, args);
                return;
            }

            if (!chat)
            {
                double messageIndex = (0.0 + MessageIndex);
                double page = (messageIndex / 7.0);

                int pageNumber = (int)Math.Floor(page);

                ShowMessagesPopup(player, pageNumber);
            }
        }

        [ChatCommand("reply")]
        void ReplyToMessage(Player player, string command, string[] args)
        {
            ReplyToMessagePopup(player, command, true, args);
        }

        void ReplyToMessagePopup(Player player, string command, bool chat, string[] args)
        {
            if (!LicenseAgreementAccepted)
            {
                //player.SendError("Before using the [i]Messenger.cs[/i]-plugin you need to accept the 'License Agreement' in the config-file.");
                player.SendError(lang.GetMessage("LicenseAgreementError", this, player.Id.ToString()));
                return;
            }

            if (args.Length != 1)
            {
                //player.SendError("Usage: /reply [i]<message index number>[/i].");
                player.SendError(lang.GetMessage("ReplyCommandHelp", this, player.Id.ToString()));
            }

            int MessageIndex = 0;

            if (Int32.TryParse(args[0], out MessageIndex))
            {
                if (!PlayersDataList.ContainsKey(player.Id))
                {
                    //player.SendError("You have no messages.");
                    player.SendError(lang.GetMessage("NoMessagesError", this, player.Id.ToString()));
                    return;
                }
                else if (PlayersDataList[player.Id].MessagesList.Count == 0)
                {
                    //player.SendError("You have no messages.");
                    player.SendError(lang.GetMessage("NoMessagesError", this, player.Id.ToString()));
                    return;
                }
                else if (PlayersDataList[player.Id].MessagesList.Count < MessageIndex)
                {
                    //player.SendError("You have no message with the index " + MessageIndex.ToString() + ".");
                    player.SendError(string.Format(lang.GetMessage("MessageIndexError", this, player.Id.ToString()), MessageIndex.ToString()));
                    return;
                }

                Message message = PlayersDataList[player.Id].MessagesList.GetAt<Message>(MessageIndex - 1);

                //covalence.Players.FindPlayer(player.Id.ToString()).Command("message", "\"" + message.SenderName + "\"", "Re: " + message.SubjectLine.ReplaceFirst("Subject: ", ""));
                //covalence.Players.FindPlayer(player.Id.ToString()).Command("message", "\"" + message.SenderName + "\"", string.Format(lang.GetMessage("Re", this), message.SubjectLine));
                string[] Margs = new string[] { message.SenderName, string.Format(lang.GetMessage("Re", this, player.Id.ToString()), message.SubjectLine) };

                ShowMessageInputPopup(player, "message", chat, MessageIndex, Margs);
            }
        }

        [ChatCommand("delete")]
        void DeleteMessage(Player player, string command, string[] args)
        {
            DeleteMessagePopup(player, command, true, args);
        }

        void DeleteMessagePopup(Player player, string command, bool chat, string[] args)
        {
            if (!LicenseAgreementAccepted)
            {
                //player.SendError("Before using the [i]Messenger.cs[/i]-plugin you need to accept the 'License Agreement' in the config-file.");
                player.SendError(lang.GetMessage("LicenseAgreementError", this, player.Id.ToString()));
                return;
            }

            if (args.Length != 1)
            {
                //player.SendError("Usage: /delete [i]<message index number>[/i].");
                player.SendError(lang.GetMessage("DeleteCommandHelp", this, player.Id.ToString()));
            }

            int MessageIndex = 0;

            if (Int32.TryParse(args[0], out MessageIndex))
            {
                if (!PlayersDataList.ContainsKey(player.Id))
                {
                    //player.SendError("You have no messages.");
                    player.SendError(lang.GetMessage("NoMessagesError", this, player.Id.ToString()));
                    return;
                }
                else if (PlayersDataList[player.Id].MessagesList.Count == 0)
                {
                    //player.SendError("You have no messages.");
                    player.SendError(lang.GetMessage("NoMessagesError", this, player.Id.ToString()));
                    return;
                }
                else if (PlayersDataList[player.Id].MessagesList.Count < MessageIndex)
                {
                    //player.SendError("You have no message with the index " + MessageIndex.ToString() + ".");
                    player.SendError(string.Format(lang.GetMessage("MessageIndexError", this, player.Id.ToString()), MessageIndex.ToString()));
                    return;
                }

                Message message = PlayersDataList[player.Id].MessagesList.GetAt<Message>(MessageIndex - 1);

                SaveList();

                //player.ShowConfirmPopup("Delete Message #" + MessageIndex.ToString() + "?", "From: " + message.SenderName + "\n \n \n" + message.SubjectLine + "\n \n" + message.Text, "Delete", "Cancel", (options, dialogue, data) => DeletingMessage(player, message, MessageIndex, options, dialogue, data));
                player.ShowConfirmPopup(string.Format(lang.GetMessage("DeleteMessageTitle", this, player.Id.ToString()), MessageIndex.ToString()), string.Format(lang.GetMessage("MessageSenderLine", this, player.Id.ToString()), message.SenderName) + "\n \n \n" + string.Format(lang.GetMessage("MessageSubjectLine", this, player.Id.ToString()), message.SubjectLine) + "\n \n" + message.Text, lang.GetMessage("Delete", this, player.Id.ToString()), lang.GetMessage("Cancel", this, player.Id.ToString()), (options, dialogue, data) => DeletingMessage(player, message, MessageIndex, chat, options, dialogue, data));
            }
        }

        void DeletingMessage(Player player, Message message, int MessageIndex, bool chat, Options options, Dialogue dialogue, object data)
        {
            if (options.Equals(Options.Cancel) || options.Equals(Options.No)) return;

            PlayersDataList[player.Id].MessagesList.Remove(message);
            //player.SendNews("Message #" + MessageIndex.ToString() + " from [i]" + message.SenderName + "[/i] has been deleted.");
            player.SendNews(lang.GetMessage("MessageTitle", this, player.Id.ToString()) + MessageIndex.ToString() + string.Format(lang.GetMessage("MessageDeletedInfo", this, player.Id.ToString()), message.SenderName));

            if (!chat)
            {
                double messageIndex = (0.0 + MessageIndex);
                double page = (messageIndex / 7.0);

                int pageNumber = (int)Math.Floor(page);

                ShowMessagesPopup(player, pageNumber);
            }
        }

        void OnPlayerSpawn(PlayerFirstSpawnEvent pfse)
        {
            if (!LicenseAgreementAccepted) return;

            if (PlayersDataList.ContainsKey(pfse.Player.Id))
            {
                PlayersDataList[pfse.Player.Id].Name = pfse.Player.Name;
                uint NewMessages = PlayersDataList[pfse.Player.Id].NewMessages;
                PlayersDataList[pfse.Player.Id].NewMessages = 0;
                SaveList();

                if (NewMessages != 0)
                {
                    if (NewMessages == 1)
                    {
                        //timer.Once(30, () => pfse.Player.SendNews("You have a new message."));
                        timer.Once(30, () => pfse.Player.SendNews(lang.GetMessage("NewMessageInfo", this, pfse.Player.Id.ToString())));
                    }
                    else
                    {
                        //timer.Once(30, () => pfse.Player.SendNews("You have " + NewMessages.ToString() + " new messages."));
                        timer.Once(30, () => pfse.Player.SendNews(string.Format(lang.GetMessage("NewMessagesInfo", this, pfse.Player.Id.ToString()), NewMessages.ToString())));
                    }
                }

                uint UnreadMessages = 0;

                for (int i = 0; i < PlayersDataList[pfse.Player.Id].MessagesList.Count; i++)
                {
                    if (!PlayersDataList[pfse.Player.Id].MessagesList.GetAt<Message>(i).Read)
                    {
                        UnreadMessages++;
                    }
                }

                if (UnreadMessages != 0)
                {
                    if (UnreadMessages == 1)
                    {
                        //timer.Once(30, () => pfse.Player.SendNews("You have an unread message."));
                        timer.Once(30, () => pfse.Player.SendNews(lang.GetMessage("UnreadMessageInfo", this, pfse.Player.Id.ToString())));
                    }
                    else
                    {
                        //timer.Once(30, () => pfse.Player.SendNews("You have " + NewMessages.ToString() + " unread messages."));
                        timer.Once(30, () => pfse.Player.SendNews(string.Format(lang.GetMessage("UnreadMessagesInfo", this, pfse.Player.Id.ToString()), UnreadMessages.ToString())));
                    }
                }
            }
            else
            {
                PlayersDataList.Add(pfse.Player.Id, new PlayerData(pfse.Player, new List<Message>()));
                SaveList();
                //Puts(pfse.Player.Name + " (" + pfse.Player.Id.ToString() +") was added to the PlayersDataList.");
                Puts(string.Format(lang.GetMessage("PlayerAddedToPlayerDataListLog", this), pfse.Player.Name, pfse.Player.Id.ToString()));
            }
        }
    }
}