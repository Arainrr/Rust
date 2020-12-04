using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Tell My Vote", "BuzZ[PHOQUE]", "1.0.0")]
    [Description("A Cui panel for players to vote at admin polls")]

/*======================================================================================================================= 
*
*   SET UP TO - 4 QUESTIONS / 3 ANSWERS - IN CONFIG FILE, AND USE TELLMYVOTE CUI TO VOTE AND CHECK COUNTS
*   THANKS TO THE OXIDE/UMOD TEAM for coding quality, ideas, and time spent for the community
*   
*   1.0.0   20190906    code refresh
*
*   permission : tellmyvote.admin
*   chat commands   /myvote     /myvote_poll X Y [args]
*   It is case sensitive
*   
*   example :   /myvote_poll 1 0 question       ---> set "question" for poll#1 title
*               /myvote_poll 1 1 first choice   ---> set "first choice" for poll#1 answer#1 
*               /myvote_poll 1 2 second choice   ---> set "second choice" for poll#1 answer#2
*
*   if question is set empty ---> the whole poll won't be displayed
*   if answer is set empty ---> the answer line won't be displayed
*
*   POLL#1      (myvote_poll 1 0 [args])               POLL#3      (myvote_poll 3 0 [args])
*   answer#1    (myvote_poll 1 1 [args])               answer#1    (myvote_poll 3 1 [args])          
*   answer#2    (myvote_poll 1 2 [args])               answer#2    (myvote_poll 3 2 [args])
*   answer#3    (myvote_poll 1 3 [args])               answer#3    (myvote_poll 3 3 [args])
*
*   POLL#2      (myvote_poll 2 0 [args])               POLL#4      (myvote_poll 4 0 [args])
*   answer#1    (myvote_poll 2 1 [args])               answer#1    (myvote_poll 4 1 [args])          
*   answer#2    (myvote_poll 2 2 [args])               answer#2    (myvote_poll 4 2 [args])
*   answer#3    (myvote_poll 2 3 [args])               answer#3    (myvote_poll 4 3 [args])
*=======================================================================================================================*/

    public class TellMyVote : RustPlugin
    {


        string version = "version 1.0.0";
        bool debug = false;
        const string TMVAdmin = "tellmyvote.admin"; 
        static string MyVotePanel;
        static string MyVoteInfoPanel;
        string Prefix = "[TMV] :";                       // CHAT PLUGIN PREFIX
        string PrefixColor = "#c12300";                 // CHAT PLUGIN PREFIX COLOR
        string ChatColor = "#ffcd7c";                   // CHAT MESSAGE COLOR
        ulong SteamIDIcon = 76561198215959719;          // SteamID FOR PLUGIN ICON
        private bool ConfigChanged;

        float BannerTimer = 10;

        string[] poll1 = new string[4];
        string[] poll2 = new string[4];
        string[] poll3 = new string[4];
        string[] poll4 = new string[4];
		private Timer tmvbanner;

        void Init()
        {
            LoadVariables();
            permission.RegisterPermission(TMVAdmin, this);
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("TellMyVote");
        }

#region CONFIG

        protected override void LoadDefaultConfig()
            {
                Config.Clear();
                LoadVariables();
            }

        private void LoadVariables()
        {
            Prefix = Convert.ToString(GetConfig("Chat Settings", "Prefix", "[TMV] :"));                       // CHAT PLUGIN PREFIX
            PrefixColor = Convert.ToString(GetConfig("Chat Settings", "PrefixColor", "#c12300"));                // CHAT PLUGIN PREFIX COLOR
            ChatColor = Convert.ToString(GetConfig("Chat Settings", "ChatColor", "#ffcd7c"));                    // CHAT  COLOR
            SteamIDIcon = Convert.ToUInt64(GetConfig("Settings", "SteamIDIcon", 76561198215959719));        // SteamID FOR PLUGIN ICON
            BannerTimer = Convert.ToSingle(GetConfig("TIMER", "Vote Banner will display every (in minutes)", "10"));
            poll1[0] = Convert.ToString(GetConfig("Poll #1", "Question", "set your question here"));
            poll1[1] = Convert.ToString(GetConfig("Poll #1", "Answer#1", "set answer here"));
            poll1[2] = Convert.ToString(GetConfig("Poll #1", "Answer#2", "set answer here"));
            poll1[3] = Convert.ToString(GetConfig("Poll #1", "Answer#3", "set answer here"));
            poll2[0] = Convert.ToString(GetConfig("Poll #2", "Question", "set your question here"));
            poll2[1] = Convert.ToString(GetConfig("Poll #2", "Answer#1", "set answer here"));
            poll2[2] = Convert.ToString(GetConfig("Poll #2", "Answer#2", "set answer here"));
            poll2[3] = Convert.ToString(GetConfig("Poll #2", "Answer#3", "set answer here"));
            poll3[0] = Convert.ToString(GetConfig("Poll #3", "Question", "set your question here"));
            poll3[1] = Convert.ToString(GetConfig("Poll #3", "Answer#1", "set answer here"));
            poll3[2] = Convert.ToString(GetConfig("Poll #3", "Answer#2", "set answer here"));
            poll3[3] = Convert.ToString(GetConfig("Poll #3", "Answer#3", "set answer here"));
            poll4[0] = Convert.ToString(GetConfig("Poll #4", "Question", "set your question here"));
            poll4[1] = Convert.ToString(GetConfig("Poll #4", "Answer#1", "set answer here"));
            poll4[2] = Convert.ToString(GetConfig("Poll #4", "Answer#2", "set answer here"));
            poll4[3] = Convert.ToString(GetConfig("Poll #4", "Answer#3", "set answer here"));

            if (!ConfigChanged) return;
            SaveConfig();
            ConfigChanged = false;
        }

        private object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                ConfigChanged = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                ConfigChanged = true;
            }
            return value;
        }

#endregion

        void Loaded()
        {
            if (storedData.myvoteison == true)
            {
                PopUpVote("start");
            }
        }

        void Unload()
        {
            Interface.Oxide.DataFileSystem.WriteObject("TellMyVote", storedData);
			if (tmvbanner != null) tmvbanner.Destroy();
        }

#region MESSAGES

        void LoadDefaultMessages()
        {

            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"NoPermMsg", "You don't have admin permission."},
                {"AdminPermMsg", "You are allowed as admin. You can start/end/clear the votes."},
                {"ThankVoteMsg", "Thank you for your vote."},
                {"PopNewMsg", "Check and vote at the new poll with /myvote"},
                {"QAlreadyMsg", "You already have voted for this Question"},
                {"VoteLogMsg", "Thank you, we recorded your vote for Question"},
                {"VoteBannerMsg", "To help our community : please vote with /myvote"},
                {"TMVoffMsg", "Vote session is now over."},
                {"TMVonMsg", "A new vote session is open."},
                {"PurgeMsg", "Counters has been reset"},
                {"Info01Msg", "Players with admin permission can start/end/clear votes from main panel"},
                {"Info02Msg", "Questions/Answers has to be set from TellMyVote.json config file or with chat command /myvote_poll."},
                {"Info03Msg", "IF A QUESTION IS SET EMPTY : it and its answers won't be displayed."},
                {"Info04Msg", "IF AN ANSWER IS SET EMPTY : its button won't be displayed."}, 
                {"HowToMsg", "Please use this format :\n/myvote_poll 1 0 here the words for the poll#1 title - check plugin webpage"},

            }, this, "en");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"NoPermMsg", "Vous n'avez pas la permission administrateur."},
                {"AdminPermMsg", "Vous êtes admin. et avez accès aux commandes start/end/clear."},
                {"ThankVoteMsg", "Thank you for your vote."},
                {"PopNewMsg", "Jetez un coup d'oeil au sondage /myvote !"},
                {"QAlreadyMsg", "Vous avez déjà voté à cette Question"},
                {"VoteLogMsg", "Merci, nous avons enregistré votre choix."},
                {"VoteBannerMsg", "Pour aider la communaté : votez avec /myvote"},
                {"TMVoffMsg", "Le sondage est maintenant terminé."},
                {"TMVonMsg", "Un nouveau sondage est lancé."},
                {"PurgeMsg", "Les compteurs sont remis à zéro"},
                {"Info01Msg", "La permission .admin permet de lancer/stopper/purger depuis le panneau principal"},
                {"Info02Msg", "Les Questions/Réponses sont à définir depuis le fichier de config TellMyVote.json ou avec la commande chat /myvote_poll."},
                {"Info03Msg", "SI UNE QUESTION EST LAISSéE VIDE : elle et ses questions ne seront pas affichés."},
                {"Info04Msg", "SI UNE REPONSE EST VIDE : son bouton ne s'affichera pas."}, 
                {"HowToMsg", "S'il vous plait utilisez ce format :\n/myvote_poll 1 0 taper ici le titre#1 - consultez la page du plugin"},

            }, this, "fr");
        }

#endregion

    class StoredData
    {
        public Dictionary<int, int> answers = new Dictionary<int, int>();
        public List<ulong> voted01 = new List<ulong>();
        public List<ulong> voted02 = new List<ulong>();
        public List<ulong> voted03 = new List<ulong>();
        public List<ulong> voted04 = new List<ulong>();
        public bool myvoteison;

            public StoredData()
            {
            }
    }
        private StoredData storedData;

#region CHAT SET Q/A

        [ChatCommand("myvote_poll")]         
        private void TellMyVotePollSet(BasePlayer player, string command, string[] args)
        {
            bool isadmin = permission.UserHasPermission(player.UserIDString, TMVAdmin);
            string sentence = string.Empty;
            
            if (isadmin == false)
            {
                if (debug) {Puts($"-> NOT ADMIN access to set polls");}  
                Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("NoPermMsg", this, player.UserIDString)}</color>",$"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);                  
                return;
            }
            if (args.Length == 0)
            {
                Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("HowToMsg", this, player.UserIDString)}</color>",$"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
                if (debug) {Puts($"-> SETTING POLLS with no arguments");}  
                return;
            }            

            if (args.Length == 1)
            {
                Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("HowToMsg", this, player.UserIDString)}</color>",$"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
                if (args[0] == "1") poll1[0] = "";
                if (args[0] == "2") poll2[0] = "";
                if (args[0] == "3") poll3[0] = "";
                if (args[0] == "4") poll4[0] = "";
                Player.Message(player, $"Poll#{args[0]} has been set to empty",$"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
                if (debug == true) {Puts($"-> SETTING POLL {args[0]}, with no arguments");}  
                return;
            }
            if (args.Length >= 2)
            {
                int Round = 3;
                int round = 1;
                for (Round = 3; Round <= args.Length ; Round++)            
                {
                    round = round + 1;
                    sentence = sentence + " " + args[round];
                }
                if (args[0] == "1")
                {
                    if (args[1] == "0")//poll
                    {
                        poll1[0] = sentence;
                        Player.Message(player, $"Poll#1 has been set to : {sentence}",$"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
                    }
                    if (args[1] == "1") { poll1[1] = sentence; PlayerMessage(player, "1", "1", sentence); }
                    if (args[1] == "2") { poll1[2] = sentence; PlayerMessage(player, "1", "2", sentence); }
                    if (args[1] == "3") { poll1[3] = sentence; PlayerMessage(player, "1", "3", sentence); }
                }
                if (args[0] == "2")
                {
                    if (args[1] == "0")//poll
                    {
                        poll2[0] = sentence;
                        Player.Message(player, $"Poll#2 has been set to : {sentence}",$"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
                    }
                    if (args[1] == "1") { poll2[1] = sentence; PlayerMessage(player, "2", "1", sentence); }
                    if (args[1] == "2") { poll2[2] = sentence; PlayerMessage(player, "2", "2", sentence); }
                    if (args[1] == "3") { poll2[3] = sentence; PlayerMessage(player, "2", "3", sentence); }
                }
                if (args[0] == "3")
                {
                    if (args[1] == "0")//poll
                    {
                        poll3[0] = sentence;
                        Player.Message(player, $"Poll#3 has been set to : {sentence}",$"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
                    }
                    if (args[1] == "1") { poll3[1] = sentence; PlayerMessage(player, "3", "1", sentence); }
                    if (args[1] == "2") { poll3[2] = sentence; PlayerMessage(player, "3", "2", sentence); }
                    if (args[1] == "3") { poll3[3] = sentence; PlayerMessage(player, "3", "3", sentence); }
                }
                if (args[0] == "4")
                {
                    if (args[1] == "0")//poll
                    {
                        poll4[0] = sentence;
                        Player.Message(player, $"Poll#4 has been set to : {sentence}",$"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
                    }
                    if (args[1] == "1") { poll4[1] = sentence; PlayerMessage(player, "4", "1", sentence); }
                    if (args[1] == "2") { poll4[2] = sentence; PlayerMessage(player, "4", "2", sentence); }
                    if (args[1] == "3") { poll4[3] = sentence; PlayerMessage(player, "4", "3", sentence); }
                }
            }
        }

        void PlayerMessage(BasePlayer player, string poll, string answer, string sentence)
        {
            Player.Message(player, $"Poll#{poll}/Answer#{answer} has been set to : {sentence}",$"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
        }

#endregion

#region VOTING

        [ConsoleCommand("TellMyVote")]
        private void MySurveySpotOnly(ConsoleSystem.Arg arg)       
        {
            var player = arg.Connection.player as BasePlayer;
            ulong playerID = player.userID;
            string[] blabla;
            int answernumber;
            if (storedData.myvoteison == false)
            {
                Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("TMVoffMsg", this, player.UserIDString)} #1</color>",$"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
                return;
            }
            blabla = arg.Args.ToArray();
            answernumber = Int32.Parse(blabla[0]);
            if (answernumber <= 3)
            {
                if (storedData.voted01.Contains(playerID)) {if (debug == true) {Puts($"-> answernumber = {answernumber} - POLL #1 - already voted");}
                Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("QAlreadyMsg", this, player.UserIDString)} #1</color>",$"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
                return;
                }
                if (debug == true) {Puts($"-> answernumber = {answernumber} - POLL #1 vote recorded");}
                storedData.voted01.Add(playerID);
                Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("VoteLogMsg", this, player.UserIDString)} #1</color>",$"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
            }
            if (answernumber >= 4 && answernumber <= 6)
            {
                if (storedData.voted02.Contains(playerID)) {if (debug == true) {Puts($"-> answernumber = {answernumber} - POLL #2 - already voted");}
                Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("QAlreadyMsg", this, player.UserIDString)} #2</color>",$"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
                return;
                }       
                if (debug == true) {Puts($"-> answernumber = {answernumber} - POLL #2 vote recorded");}
                storedData.voted02.Add(playerID);
                Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("VoteLogMsg", this, player.UserIDString)} #2</color>",$"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);                }
            if (answernumber >= 7 && answernumber <= 9)
            {
                if (storedData.voted03.Contains(playerID)) {if (debug == true) {Puts($"-> answernumber = {answernumber} - POLL #3 - already voted");}
                Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("QAlreadyMsg", this, player.UserIDString)} #3</color>",$"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
                return;
                }    
                if (debug == true) {Puts($"-> answernumber = {answernumber} - POLL #3 vote recorded");}               
                storedData.voted03.Add(playerID);
                Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("VoteLogMsg", this, player.UserIDString)} #3</color>",$"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
            }
            if (answernumber >= 10 && answernumber <= 12)
            {
                if (storedData.voted04.Contains(playerID)) {if (debug == true) {Puts($"-> answernumber = {answernumber} - POLL #4 - already voted");}
                Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("QAlreadyMsg", this, player.UserIDString)} #4</color>",$"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
                return;
                }                        
                storedData.voted04.Add(playerID);
                Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("VoteLogMsg", this, player.UserIDString)} #4</color>",$"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
            }
            int oldcount;
            storedData.answers.TryGetValue(answernumber, out oldcount);
            int newcount = oldcount + 1;
            if (oldcount != 0) {storedData.answers.Remove(answernumber);}
            storedData.answers.Add(answernumber, newcount);
            RefreshMyVotePanel(player);
        }
#endregion

#region REFRESH VOTE PANEL

        private void RefreshMyVotePanel(BasePlayer player)
        {
            foreach(BasePlayer player2 in BasePlayer.activePlayerList)
            {
                if (player == player2)
                {
                    CuiHelper.DestroyUi(player2, MyVotePanel);             
                    TellMyVotePanel(player2, null, null);
                }
            }
        }
#endregion

#region CHANGE STATUS

        [ConsoleCommand("TellMyVoteChangeStatus")]
        private void TellMyVoteChangeStatus(ConsoleSystem.Arg arg)       
        {
            var player = arg.Connection.player as BasePlayer;
            ulong playerID = player.userID;
            string[] blabla;
            blabla = arg.Args.ToArray();
            if (blabla.Contains("start"))
            {
                if (debug) {Puts($"-> START OF MY VOTE");}
                if (storedData.myvoteison == true)
                {
                    if (debug) {Puts($"-> START ASKED, BUT MY VOTE ALREADY ON.");}
                    return;                    
                }          
                storedData.myvoteison = true;
                PopUpVote("start");
                RefreshMyVotePanel(player);                
            }
            if (blabla.Contains("end"))
            {
                if (debug) {Puts($"-> END OF MY VOTE SESSION");}
                if (storedData.myvoteison == false)
                {
                    if (debug) {Puts($"-> END ASKED, BUT MY ALREADY OFF.");}
                    return;                    
                }  
                storedData.myvoteison = false;
                PopUpVote("end"); 
                RefreshMyVotePanel(player);              
            }
            if (blabla.Contains("purge"))
            {
                if (debug) {Puts($"-> PURGE OF DATAS");}  
                Purge();
                RefreshMyVotePanel(player);
                Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("PurgeMsg", this, player.UserIDString)}</color>",$"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);            
            }
            if (blabla.Contains("info"))
            {
                if (debug) {Puts($"-> DISPLAY INFO PANEL");}  
                CuiHelper.DestroyUi(player, MyVotePanel);
                TellMyVoteInfoPanel(player);       
            }
            if (blabla.Contains("back"))
            {
                if (debug) {Puts($"-> BACK TO MAIN MY VOTE PANEL");}  
                CuiHelper.DestroyUi(player, MyVoteInfoPanel);             
                TellMyVotePanel(player, null, null);          
            }
        }
#endregion

       private void Purge()       
        {
            storedData.answers.Clear();
            storedData.voted01.Clear();
            storedData.voted02.Clear();
            storedData.voted03.Clear();
            storedData.voted04.Clear();
        }

#region POPUP BANNER

        private void PopUpVote(string newstate)       
        {
            string bannertxt = "";
            foreach(BasePlayer player in BasePlayer.activePlayerList)
            {
                if (newstate == "start")
                {
                    bannertxt = $"{lang.GetMessage("VoteBannerMsg", this, player.UserIDString)}";
                }
                if (newstate == "end")
                {
                    bannertxt = $"{lang.GetMessage("TMVoffMsg", this, player.UserIDString)}";
                    tmvbanner.Destroy();
                }

                var CuiElement = new CuiElementContainer();
                var MyVoteBanner = CuiElement.Add(new CuiPanel {Image = {Color = "0.5 1.0 0.5 0.5"}, RectTransform = { AnchorMin = "0.20 0.85", AnchorMax = "0.80 0.90"}, CursorEnabled = false});    
                var closeButton = new CuiButton {Button = {Close = MyVoteBanner, Color = "0.0 0.0 0.0 0.6"}, RectTransform = {AnchorMin = "0.90 0.01", AnchorMax = "0.99 0.99"}, Text = {Text = "X", FontSize = 18, Align = TextAnchor.MiddleCenter}};
                    CuiElement.Add(closeButton, MyVoteBanner);   
                    CuiElement.Add(new CuiLabel {Text = {Text = $"{bannertxt}", FontSize = 20, Align = TextAnchor.MiddleCenter,Color = "0.0 0.0 0.0 1"}, RectTransform = {AnchorMin = "0.10 0.10", AnchorMax = "0.90 0.90"}}, MyVoteBanner);
                    CuiHelper.AddUi(player, CuiElement);
                timer.Once(12f, () =>
                {
                       CuiHelper.DestroyUi(player, MyVoteBanner);
                });
                if (debug) { Puts($"-> TIMER IS SET TO {BannerTimer} minutes"); }  
                if (storedData.myvoteison == true)
                {
                    if (debug) { Puts($"-> TIMER LOOP {BannerTimer*60} seconds"); }  
                    tmvbanner = timer.Repeat(BannerTimer * 60, 0,() =>
                    {
                        PopUpVote("start");
                    });
                }
            }
        }

#endregion

#region INFOPANEL

        private void TellMyVoteInfoPanel(BasePlayer player)
        {
            string BackButtonTxt = "0.0 1.0 1.0 0.5";
            string BackButtonColor ="0.0 0.5 1.0 0.5";
            string PanelColor = "0.0 0.0 0.0 0.8";
            string buttonCloseColor = "0.6 0.26 0.2 1";
            string information = $"{lang.GetMessage("Info01Msg", this, player.UserIDString)}\n\n{lang.GetMessage("Info02Msg", this, player.UserIDString)}\n\n\n\n{lang.GetMessage("Info03Msg", this, player.UserIDString)}\n\n{lang.GetMessage("Info04Msg", this, player.UserIDString)}";
            bool isadmin = permission.UserHasPermission(player.UserIDString, TMVAdmin);
            if (isadmin == true)
            {
                if (debug) { Puts($"-> ADMIN access to info panel"); }  
                Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("AdminPermMsg", this, player.UserIDString)}</color>",$"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);                  
            }
            if (isadmin == false)
            {
                if (debug) { Puts($"-> NOT ADMIN access to info panel"); }  
                Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("NoPermMsg", this, player.UserIDString)}</color>",$"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);                  
            }
            var CuiElement = new CuiElementContainer();
                MyVoteInfoPanel = CuiElement.Add(new CuiPanel{Image = {Color = $"{PanelColor}"},RectTransform = {AnchorMin = "0.25 0.25", AnchorMax = "0.75 0.80"}, CursorEnabled = true});
            var closeButton = new CuiButton{Button = {Close = MyVoteInfoPanel, Color = $"{buttonCloseColor}"}, RectTransform = {AnchorMin = "0.85 0.85", AnchorMax = "0.95 0.95"}, Text = {Text = "[X]\nClose", FontSize = 16, Align = TextAnchor.MiddleCenter}};
                CuiElement.Add(closeButton, MyVoteInfoPanel);
            var BackButton = CuiElement.Add(new CuiButton {Button = {Command = "TellMyVoteChangeStatus back", Color = $"0.0 0.5 1.0 0.5"}, RectTransform = {AnchorMin = $"0.78 0.85", AnchorMax = $"0.83 0.95"},
                Text = {Text = "BACK", Color = "1.0 1.0 1.0 0.8", FontSize = 10, Align = TextAnchor.MiddleCenter}
                }, MyVoteInfoPanel);
            var TextIntro = CuiElement.Add(new CuiLabel {Text = {Color = "1.0 1.0 1.0 1.0", Text = "Tell My Vote Panel", FontSize = 22, Align = TextAnchor.MiddleCenter},
                RectTransform = {AnchorMin = $"0.30 0.87", AnchorMax = "0.70 0.95"}
                }, MyVoteInfoPanel);            
            var ButtonAnswer1 = CuiElement.Add(new CuiButton {Button = {Command = "", Color = $"0.5 1.0 0.5 0.5"},
                RectTransform = {AnchorMin = $"0.05 0.05", AnchorMax = $"0.95 0.70"},
                Text = {Text = $"{information}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter}
                }, MyVoteInfoPanel);
            CuiHelper.AddUi(player, CuiElement);
        }
#endregion

#region TELLMYVOTE PANEL START

        [ChatCommand("myvote")]         
        private void TellMyVotePanel(BasePlayer player, string command, string[] args)
        {
            string debutcolonne1 = "0.03";
            string fincolonne1 = "0.37";
            string debutcolonne1b = "0.38";
            string fincolonne1b = "0.48";
            string debutcolonne2 = "0.52";
            string fincolonne2 = "0.86";
            string debutcolonne2b = "0.87";
            string fincolonne2b = "0.97";
            string basligne8 = "0.05";
            string hautligne8 = "0.13";
            string basligne7 = "0.14";
            string hautligne7 = "0.22";
            string basligne6 = "0.23";
            string hautligne6 = "0.31";
            string basligne5 = "0.32";
            string hautligne5 = "0.40";
            string basligne4 = "0.41";
            string hautligne4 = "0.49";
            string basligne3 = "0.50";
            string hautligne3 = "0.59";
            string basligne2 = "0.60";
            string hautligne2 = "0.68";
            string basligne1 = "0.69";
            string hautligne1 = "0.77";
            string HelpButtonTxt = "0.0 1.0 1.0 0.5";
            string HelpButtonColor ="0.0 0.5 1.0 0.5";
            string PanelColor = "0.0 0.0 0.0 0.8";
            string buttonCloseColor = "0.6 0.26 0.2 1";
            string QuestionColor = "1.0 1.0 1.0 1.0";
            string AnswerColor ="0.5 1.0 0.5 0.5";
            string CountColor = "0.0 1.0 1.0 0.5";
            string StatusColor = "";
            string Status = "";
            bool isadmin = permission.UserHasPermission(player.UserIDString, TMVAdmin);
            int[] answersarray = new int[12];
            int counted;
            int Round = 1;
            int round = -1;
            for (Round = 1; Round <= 12 ; Round++)            
            {
                round = round + 1;
                storedData.answers.TryGetValue(Round, out counted);                                    
                answersarray[round] = counted;
                if (debug == true) {Puts($"-> round(array) = {round} - counted {counted} - string ");}
            }
            if (storedData.myvoteison == true)
            {
                Status = "SESSION IS OPEN : CHOOSE YOUR ANSWERS !";
                StatusColor = "0.2 1.0 0.2 0.8";
            }
            if (storedData.myvoteison == false)
            {
                Status = "SESSION HAS ENDED.";
                StatusColor = "1.0 0.1 0.1 0.8";
            }

#endregion

#region PANEL AND CLOSE BUTTON

            var CuiElement = new CuiElementContainer();
            MyVotePanel = CuiElement.Add(new CuiPanel{Image = {Color = $"{PanelColor}"},RectTransform = {AnchorMin = "0.25 0.25", AnchorMax = "0.75 0.80"}, CursorEnabled = true});
            var closeButton = new CuiButton{Button = {Close = MyVotePanel, Color = $"{buttonCloseColor}"}, RectTransform = {AnchorMin = "0.85 0.85", AnchorMax = "0.95 0.95"}, Text = {Text = "[X]\nClose", FontSize = 16, Align = TextAnchor.MiddleCenter}};
            CuiElement.Add(closeButton, MyVotePanel);
            var HelpButton = CuiElement.Add(new CuiButton {Button = {Command = "TellMyVoteChangeStatus info", Color = $"{HelpButtonColor}"}, RectTransform = {AnchorMin = $"0.78 0.85", AnchorMax = $"0.83 0.95"},
                Text = {Text = "?", Color = $"{HelpButtonTxt}", FontSize = 18, Align = TextAnchor.MiddleCenter}
                }, MyVotePanel);
            var TextVersion = CuiElement.Add(new CuiLabel {Text = {Color = "1.0 1.0 1.0 1.0", Text = $"<i>{version}</i>", FontSize = 11, Align = TextAnchor.MiddleCenter},
                RectTransform = {AnchorMin = $"0.78 0.78", AnchorMax = "0.95 0.84"}
                }, MyVotePanel);
            var TextIntro = CuiElement.Add(new CuiLabel {Text = {Color = "1.0 1.0 1.0 1.0", Text = "Tell My Vote Panel", FontSize = 22, Align = TextAnchor.MiddleCenter},
                RectTransform = {AnchorMin = $"0.30 0.87", AnchorMax = "0.70 0.95"}
                }, MyVotePanel);
            var TextStatus = CuiElement.Add(new CuiLabel {Text = {Color = $"{StatusColor}", Text = $"{Status}", FontSize = 16, Align = TextAnchor.MiddleCenter},
                RectTransform = {AnchorMin = $"0.23 0.78", AnchorMax = "0.77 0.86"}
                }, MyVotePanel);
            if (isadmin == true)
            {
                var StartButton = CuiElement.Add(new CuiButton {Button = {Command = "TellMyVoteChangeStatus start", Color = "0.2 0.6 0.2 0.8"}, RectTransform = {AnchorMin = $"0.05 0.85", AnchorMax = $"0.15 0.95"},
                    Text = {Text = "START", Color = "1.0 1.0 1.0 1.0", FontSize = 10, Align = TextAnchor.MiddleCenter}
                    }, MyVotePanel);
                var StopButton = CuiElement.Add(new CuiButton {Button = {Command = "TellMyVoteChangeStatus end", Color = "1.0 0.2 0.2 0.8"}, RectTransform = {AnchorMin = $"0.16 0.85", AnchorMax = $"0.22 0.95"},
                    Text = {Text = "END", Color = "1.0 1.0 1.0 1.0", FontSize = 10, Align = TextAnchor.MiddleCenter}
                    }, MyVotePanel);    
                var PurgeButton = CuiElement.Add(new CuiButton {Button = {Command = "TellMyVoteChangeStatus purge", Color = "1.0 0.5 0.0 0.8"}, RectTransform = {AnchorMin = $"0.05 0.78", AnchorMax = $"0.22 0.84"},
                    Text = {Text = "RESET COUNTERS", Color = "1.0 1.0 1.0 1.0", FontSize = 10, Align = TextAnchor.MiddleCenter}
                    }, MyVotePanel);             
            }

#endregion

#region COLONNE GAUCHE

            if (poll1[0] != string.Empty)
            {
                var TextQuestion01 = CuiElement.Add(new CuiLabel {
                    RectTransform = {AnchorMin = $"{debutcolonne1} {basligne1}", AnchorMax = $"{fincolonne1b} {hautligne1}"},
                    Text = {Text = $"#1. {poll1[0]} ?", Color = $"{QuestionColor}", FontSize = 16, Align = TextAnchor.MiddleLeft}
                }, MyVotePanel);

                if (poll1[1] != string.Empty)
                {
                    var ButtonAnswer1 = CuiElement.Add(new CuiButton {Button = {Command = "TellMyVote 1", Color = $"{AnswerColor}"},
                        RectTransform = {AnchorMin = $"{debutcolonne1} {basligne2}", AnchorMax = $"{fincolonne1} {hautligne2}"},
                        Text = {Text = $"{poll1[1]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter}
                    }, MyVotePanel);

                    var ButtonAnswer1b = CuiElement.Add(new CuiButton {Button = {Command = "", Color = $"{CountColor}"},
                        RectTransform = {AnchorMin = $"{debutcolonne1b} {basligne2}", AnchorMax = $"{fincolonne1b} {hautligne2}"},
                        Text = {Text = $"{answersarray[0]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter}
                    }, MyVotePanel);
                }

                if (poll1[2] != string.Empty)
                {
                    var ButtonAnswer2 = CuiElement.Add(new CuiButton {Button = {Command = "TellMyVote 2", Color = $"{AnswerColor}"},
                        RectTransform = {AnchorMin = $"{debutcolonne1} {basligne3}", AnchorMax = $"{fincolonne1} {hautligne3}"},
                        Text = {Text = $"{poll1[2]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter}
                    }, MyVotePanel);

                    var ButtonAnswer2b = CuiElement.Add(new CuiButton {Button = {Command = "", Color = $"{CountColor}"},
                        RectTransform = {AnchorMin = $"{debutcolonne1b} {basligne3}", AnchorMax = $"{fincolonne1b} {hautligne3}"},
                        Text = {Text = $"{answersarray[1]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter}
                    }, MyVotePanel);
                }
                if (poll1[3] != string.Empty)
                {
                    var ButtonAnswer3 = CuiElement.Add(new CuiButton {Button = {Command = "TellMyVote 3", Color = $"{AnswerColor}"},
                    RectTransform = {AnchorMin = $"{debutcolonne1} {basligne4}", AnchorMax = $"{fincolonne1} {hautligne4}"},
                    Text = {Text = $"{poll1[3]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter}
                    }, MyVotePanel);

                    var ButtonAnswer3b = CuiElement.Add(new CuiButton {Button = {Command = "", Color = $"{CountColor}"},
                        RectTransform = {AnchorMin = $"{debutcolonne1b} {basligne4}", AnchorMax = $"{fincolonne1b} {hautligne4}"},
                        Text = {Text = $"{answersarray[2]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter}
                    }, MyVotePanel);
                }
            }

            if (poll2[0] != string.Empty)
            {
                var TextQuestion02 = CuiElement.Add(new CuiLabel {
                    RectTransform = {AnchorMin = $"{debutcolonne1} {basligne5}", AnchorMax = $"{fincolonne1b} {hautligne5}"},
                    Text = {Text = $"#2. {poll2[0]} ?", Color = $"{QuestionColor}", FontSize = 16, Align = TextAnchor.MiddleLeft}
                }, MyVotePanel);

                if (poll2[1] != string.Empty)
                {
                    var ButtonAnswer4 = CuiElement.Add(new CuiButton {Button = {Command = "TellMyVote 4", Color = $"{AnswerColor}"},
                        RectTransform = {AnchorMin = $"{debutcolonne1} {basligne6}", AnchorMax = $"{fincolonne1} {hautligne6}"},
                        Text = {Text = $"{poll2[1]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter}
                    }, MyVotePanel);

                    var ButtonAnswer4b = CuiElement.Add(new CuiButton {Button = {Command = "", Color = $"{CountColor}"},
                        RectTransform = {AnchorMin = $"{debutcolonne1b} {basligne6}", AnchorMax = $"{fincolonne1b} {hautligne6}"},
                        Text = {Text = $"{answersarray[3]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter}
                    }, MyVotePanel);
                }
                if (poll2[2] != string.Empty)
                {
                    var ButtonAnswer5 = CuiElement.Add(new CuiButton {Button = {Command = "TellMyVote 5", Color = $"{AnswerColor}"},
                        RectTransform = {AnchorMin = $"{debutcolonne1} {basligne7}", AnchorMax = $"{fincolonne1} {hautligne7}"},
                        Text = {Text = $"{poll2[2]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter}
                    }, MyVotePanel);

                    var ButtonAnswer5b = CuiElement.Add(new CuiButton {Button = {Command = "", Color = $"{CountColor}"},
                        RectTransform = {AnchorMin = $"{debutcolonne1b} {basligne7}", AnchorMax = $"{fincolonne1b} {hautligne7}"},
                        Text = {Text = $"{answersarray[4]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter}
                    }, MyVotePanel);
                }
                if (poll2[3] != string.Empty)
                {
                    var ButtonAnswer6 = CuiElement.Add(new CuiButton
                    {
                        Button = {Command = "TellMyVote 6", Color = $"{AnswerColor}"},
                        RectTransform = {AnchorMin = $"{debutcolonne1} {basligne8}", AnchorMax = $"{fincolonne1} {hautligne8}"},
                        Text = {Text = $"{poll2[3]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter}
                    }, MyVotePanel);
                
                    var ButtonAnswer6b = CuiElement.Add(new CuiButton
                    {
                        Button = {Command = "", Color = $"{CountColor}"},
                        RectTransform = {AnchorMin = $"{debutcolonne1b} {basligne8}", AnchorMax = $"{fincolonne1b} {hautligne8}"},
                        Text = {Text = $"{answersarray[5]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter}
                    }, MyVotePanel);
                }
            }


#endregion

#region COLONNE DROITE

            if (poll3[0] != string.Empty)
            {
                var TextQuestion03 = CuiElement.Add(new CuiLabel {
                    RectTransform = {AnchorMin = $"{debutcolonne2} {basligne1}", AnchorMax = $"{fincolonne2b} {hautligne1}"},
                    Text = {Text = $"#3. {poll3[0]} ?", Color = $"{QuestionColor}", FontSize = 16, Align = TextAnchor.MiddleLeft}
                }, MyVotePanel);

                if (poll3[1] != string.Empty)
                {
                    var ButtonAnswer7 = CuiElement.Add(new CuiButton {Button = {Command = "TellMyVote 7", Color = $"{AnswerColor}"},
                        RectTransform = {AnchorMin = $"{debutcolonne2} {basligne2}", AnchorMax = $"{fincolonne2} {hautligne2}"},
                        Text = {Text = $"{poll3[1]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter}
                    }, MyVotePanel);

                    var ButtonAnswer7b = CuiElement.Add(new CuiButton {Button = {Command = "", Color = $"{CountColor}"},
                        RectTransform = {AnchorMin = $"{debutcolonne2b} {basligne2}", AnchorMax = $"{fincolonne2b} {hautligne2}"},
                        Text = {Text = $"{answersarray[6]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter}
                    }, MyVotePanel);
                }

                if (poll3[2] != string.Empty)
                {
                    var ButtonAnswer8 = CuiElement.Add(new CuiButton {Button = {Command = "TellMyVote 8", Color = $"{AnswerColor}"},
                        RectTransform = {AnchorMin = $"{debutcolonne2} {basligne3}", AnchorMax = $"{fincolonne2} {hautligne3}"},
                        Text = {Text = $"{poll3[2]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter}
                    }, MyVotePanel);

                    var ButtonAnswer8b = CuiElement.Add(new CuiButton {Button = {Command = "", Color = $"{CountColor}"},
                        RectTransform = {AnchorMin = $"{debutcolonne2b} {basligne3}", AnchorMax = $"{fincolonne2b} {hautligne3}"},
                        Text = {Text = $"{answersarray[7]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter}
                    }, MyVotePanel);
                }
                if (poll3[3] != string.Empty)
                {
                    var ButtonAnswer9 = CuiElement.Add(new CuiButton {Button = {Command = "TellMyVote 9", Color = $"{AnswerColor}"},
                        RectTransform = {AnchorMin = $"{debutcolonne2} {basligne4}", AnchorMax = $"{fincolonne2} {hautligne4}"},
                        Text = {Text = $"{poll3[3]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter}
                    }, MyVotePanel);

                    var ButtonAnswer9b = CuiElement.Add(new CuiButton {Button = {Command = "", Color = $"{CountColor}"},
                        RectTransform = {AnchorMin = $"{debutcolonne2b} {basligne4}", AnchorMax = $"{fincolonne2b} {hautligne4}"},
                        Text = {Text = $"{answersarray[8]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter}
                    }, MyVotePanel);
                }
            }

            if (poll4[0] != string.Empty)
            {
                var TextQuestion04 = CuiElement.Add(new CuiLabel {
                    RectTransform = {AnchorMin = $"{debutcolonne2} {basligne5}", AnchorMax = $"{fincolonne2b} {hautligne5}"},
                    Text = {Text = $"#4. {poll4[0]} ?", Color = $"{QuestionColor}", FontSize = 16, Align = TextAnchor.MiddleLeft}
                }, MyVotePanel);
                if (poll4[1] != string.Empty)
                {
                    var ButtonAnswer10 = CuiElement.Add(new CuiButton {Button = {Command = "TellMyVote 10", Color = $"{AnswerColor}"},
                        RectTransform = {AnchorMin = $"{debutcolonne2} {basligne6}", AnchorMax = $"{fincolonne2} {hautligne6}"},
                        Text = {Text = $"{poll4[1]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter}
                    }, MyVotePanel);

                    var ButtonAnswer10b = CuiElement.Add(new CuiButton {Button = {Command = "", Color = $"{CountColor}"},
                        RectTransform = {AnchorMin = $"{debutcolonne2b} {basligne6}", AnchorMax = $"{fincolonne2b} {hautligne6}"},
                        Text = {Text = $"{answersarray[9]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter}
                    }, MyVotePanel);
                }
                if (poll4[2]!= string.Empty)
                {
                    var ButtonAnswer11 = CuiElement.Add(new CuiButton {Button = {Command = "TellMyVote 11", Color = $"{AnswerColor}"},
                        RectTransform = {AnchorMin = $"{debutcolonne2} {basligne7}", AnchorMax = $"{fincolonne2} {hautligne7}"},
                        Text = {Text = $"{poll4[2]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter}
                    }, MyVotePanel);


                    var ButtonAnswer11b = CuiElement.Add(new CuiButton {Button = {Command = "", Color = $"{CountColor}"},
                        RectTransform = {AnchorMin = $"{debutcolonne2b} {basligne7}", AnchorMax = $"{fincolonne2b} {hautligne7}"},
                        Text = {Text = $"{answersarray[10]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter}
                    }, MyVotePanel);
                }
                if (poll4[3] != string.Empty)
                {
                    var ButtonAnswer12 = CuiElement.Add(new CuiButton {Button = {Command = "TellMyVote 12", Color = $"{AnswerColor}"},
                        RectTransform = {AnchorMin = $"{debutcolonne2} {basligne8}", AnchorMax = $"{fincolonne2} {hautligne8}"},
                        Text = {Text = $"{poll4[3]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter}
                    }, MyVotePanel);

                    var ButtonAnswer12b = CuiElement.Add(new CuiButton {Button = {Command = "", Color = $"{CountColor}"},
                        RectTransform = {AnchorMin = $"{debutcolonne2b} {basligne8}", AnchorMax = $"{fincolonne2b} {hautligne8}"},
                        Text = {Text = $"{answersarray[11]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter}
                    }, MyVotePanel);
                }
            }
        
            CuiHelper.AddUi(player, CuiElement);
        }
#endregion

    }
}

/*
banner all other players when a player votes
server rewards points when votes
*/