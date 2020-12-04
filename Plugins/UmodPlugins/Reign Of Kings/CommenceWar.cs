using System;
using System.Collections.Generic;

using CodeHatch.Blocks.Networking.Events;
using CodeHatch.Common;
using CodeHatch.Engine.Core.Consoles;
using CodeHatch.Engine.Modules.SocialSystem;
using CodeHatch.Engine.Networking;
using CodeHatch.Networking.Events.Entities;
using CodeHatch.Thrones.SocialSystem;
using CodeHatch.UserInterface.Dialogues;

using Oxide.Core;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Commence War", "GeniusPlayUnique", "1.0.3")]
    [Description("Commence war and crush your enemies.")] /**(© GeniusPlayUnique)*/
    class CommenceWar : ReignOfKingsPlugin
    {
        static int PrepTime = 30;
        static int WarLength = 90;
        static int PeaceTime = 6;
        static int PeaceAgreementLength = 7;
        static float BallistaDamageMultiplicator = 1.0f;
        static float TrebuchetDamageMultiplicator = 1.0f;
        static Player damageSourceOwner = Server.GetPlayerById(9999999999);
        static bool SeparateCounters;
        static readonly string ServerId = "9999999999";
        readonly static string license = ""
            + "License Agreement for (the) 'CommenceWar.cs' [ReignOfKingsPlugin]: "
            + "With uploading this plugin to umod.org the rights holder (GeniusPlayUnique) grants the user the right to download and for usage of this plugin. "
            + "However these rights do NOT include the right to (re-)upload this plugin nor any modified version of it to umod.org or any other webside or to distribute it further in any way without written permission of the rights holder. "
            + "It is explicity allowed to modify this plugin at any time for personal usage ONLY. "
            + "If a modification should be made available for all users, please contact GeniusPlayUnique (rights holder) via umod.org to discuss the matter and the terms under which to gain permission to do so. "
            + "By changing 'Accept License Agreement' below to 'true' you accept this License Agreement.";
        static bool LicenseAgreementAccepted;

        protected override void LoadDefaultConfig()
        {
            Config["1. War Settings:", "Preparation time [in minutes]:"] = 30;
            Config["1. War Settings:", "War length [in minutes]:"] = 90;
            Config["2. Peace Settings:", "Peace time [in hours]:"] = 6;
            Config["2. Peace Settings:", "Peace agreement length [in days]:"] = 7;
            Config["3. Damage Settings:", "Ballista Damage Multiplicator [1.00 == 100%] [more > 1.00 > less]:"] = 1.00;
            Config["3. Damage Settings:", "Trebuchet Damage Multiplicator [1.00 == 100%] [more > 1.00 > less]:"] = 1.00;
            Config["Separate (kill) counters?"] = new bool();
            Config["EULA", "License", "License Agreement"] = license;
            Config["EULA", "License Acceptance", "Accept License Agreement"] = new bool();
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["LicenseAgreementError"] = "Before using the [i]CommenceWar.cs[/i] - plugin you need to accept the 'License Agreement' in the config-file.",
                ["CommenceWarPopupTitle"] = "[FF0000][i]Commence War[/i]:[FFFFFF]",
                ["HelpText"] = "\n"
                    + "[FFFFFF][i][u]War Help[/u]:[/i][FFFFFF]" + "\n"
                    + "[FFFFFF]/declarewar \"<playername>\" - [666666]Declares War on said players guild.[FFFFFF]" + "\n"
                    + "[FFFFFF]/offertruce \"<playername>\" [time in minutes] - [666666]Sends an offer for a truce to said player's guild leader.[FFFFFF]" + "\n"
                    + "[FFFFFF]/offerpeace \"<playername>\" - [666666]Sends a peace-offering to said player's guild leader.[FFFFFF]"
                    + "{0}",
                ["AdminHelpText"] = "\n"
                    + "[FFFFFF]/endwar \"<playername>\" - [666666]Ends the war that said player's guild started.[FFFFFF]" + "\n"
                    + "[FFFFFF]/endallwars - [666666]Ends all wars.[FFFFFF]" + "\n",
                ["NotGuildLeaderWarError"] = "You are not the leader of your guild. Only guild leaders can declare war.",
                ["AlreadyDeclaredWarError"] = "You have already declared war to {0}. A two-front war is always a bad idea. Ask Germany, they tried it twice.",
                ["AlreadyAtWarError"] = "You are already at war with {0}.",
                ["OnlineTargetPlayerNotFoundError"] = "Unable to find player online with the name '{0}'.",
                ["AtPeaceError"] = "It's been less than " + PeaceTime.ToString() + " hours that you last went to war with {0}. It is unwise to attack again so early. You should use the time to prepare.",
                ["ActivePeaceAgreementError"] = "There is an active peace agreement between you and {0}. What you are attempting is very dishonorable. I will not stand idly by while you are bringing great shame and disgrace upon {1}.",
                ["DefenderGuildLeaderOfflineError"] = "The leader of {0} is not online. You can only declare war on guilds whose leader is online.",
                ["ServerError"] = "ARE YOU INSANE?! YOU CAN'T DECLARE WAR TO THE GODS!!!",
                ["WarDeclaredNotice"] = "[b][FF0000]WAR:[00FF00][/b] [0000FF][{0}][i]{1}[/i][00FF00] declared war on [0000FF][{2}][i]{3}[/i][00FF00]![FFFFFF]",
                ["WarPrepTimeNotice"] = "[b][FF0000]WAR:[00FF00][/b] [0000FF][{0}][i]{1}[/i][00FF00] and [0000FF][{2}][i]{3}[/i][00FF00] are preparing for war! ({4} left)[FFFFFF]",
                ["WarStartedNotice"] = "[b][FF0000]WAR:[00FF00][/b] The war between [0000FF][{0}][i]{1}[/i][00FF00] and [0000FF][{2}][i]{3}[/i][00FF00] has begun![FFFFFF]",
                ["WarUpdateNotice"] = "[b][FF0000]WAR:[00FF00][/b] [0000FF][{0}][i]{1}[/i][00FF00] suffered {2} whilst [0000FF][{3}][i]{4}[/i][00FF00] suffered {5} while the vicious fighting continues. ({6} left)[FFFFFF]",
                ["casualty"] = "{0} casualty",
                ["casualties"] = "{0} casualties",
                ["lostStronghold"] = " and lost {0} stronghold",
                ["lostStrongholds"] = " and lost {0} strongholds",
                ["OpposingGuildLeaderOfflineError"] = "The leader of {0} is not online. Only the guild leader can accept your offer for everyone else it would be an act of treason.",
                ["OfferSentNotice"] = "Your offer was sent to the leader of {0}.",
                ["NotGuildLeaderTruceError"] = "You are not the leader of your guild. Only guild leaders can offer truces to the enemy. For you it would be an act of treason.",
                ["TruceTimeError"] = "\"{0}\" could not be resolved as number (uint).",
                ["NotAtWarError"] = "You are not at war with {0}.",
                ["WarHasNotStartedYetError"] = "The war between you and {0} has not started, yet.",
                ["TruceAlreadyAgreedUponError"] = "There war already a truce agreed upon in the war with {0}.",
                ["TruceOfferPopupText"] = "Your opponent {0} of {1} is offering you a truce of {2}min.",
                ["PeaceOfferPopupText"] = "Your opponent {0} of {1} is offering you peace.",
                ["Accept"] = "Accept",
                ["Deny"] = "Deny",
                ["WarTruceAgreedUponNotice"] = "[b][FF0000]WAR:[00FF00][/b] In the war between [0000FF][{0}][i]{1}[/i][00FF00] and [0000FF][{2}][i]{3}[/i][00FF00] a truce was agreed upon which gives both sides {4}min. to treat the wounded.[FFFFFF]",
                ["WarTruceBrokenNotice"] = "[b][FF0000]WAR:[00FF00][/b] The truce between [0000FF][{0}][i]{1}[/i][00FF00] and [0000FF][{2}][i]{3}[/i][00FF00] was broken by {4}[00FF00]! The fighting resumes. {4}[00FF00] has brought great shame and disgrace upon {5}self and (the) {6}[00FF00].[FFFFFF]",
                ["WarTruceIsOverNotice"] = "[b][FF0000]WAR:[00FF00][/b] The truce between [0000FF][{0}][i]{1}[/i][00FF00] and [0000FF][{2}][i]{3}[/i][00FF00] is over. The fighting resumes.[FFFFFF]",
                ["StrongholdLostNotice"] = "[b][FF0000]WAR:[00FF00][/b] [0000FF][{0}][i]{1}[/i][00FF00] just lost a stronghold to [0000FF][{2}][i]{3}[/i][00FF00].[FFFFFF]",
                ["NotGuildLeaderPeaceError"] = "You are not the leader of your guild. Only guild leaders can offer peace to the enemy. For you it would be an act of treason.[FFFFFF]",
                ["PeaceAgreementReached"] = "[b][00FF00]Peace:[FFFFFF][/b] A peace agreement was reached! The war between [0000FF][{0}][i]{1}[/i][FFFFFF] and [0000FF][{2}][i]{3}[/i][FFFFFF] is over. [0000FF][{0}][i]{1}[/i][00FF00] suffered {4} whilst [0000FF][{2}][i]{3}[/i][00FF00] suffered {5}.[FFFFFF]",
                ["WarIsOverNotice"] = "[b][FF0000]War:[0000FF][/b] The war between [0000FF][{0}][i]{1}[/i][0000FF] and [0000FF][{2}][i]{3}[/i][0000FF] is over. [0000FF][{0}][i]{1}[/i][0000FF] suffered {4} whilst [0000FF][{2}][i]{3}[/i][0000FF] suffered {5}.[FFFFFF]",
                ["CrestDamageNotAtWarError"] = "[808080][[950415]Server[808080]][FFFFFF]: You cannot damage another guild's crests when you are not at war with them.[FFFFFF]",
                ["BaseDamageNotAtWarError"] = "[808080][[950415]Server[808080]][FFFFFF]: You cannot damage another guild's bases when you are not at war with them.[FFFFFF]",
                ["BaseDamageTruceError"] = "[808080][[950415]Server[808080]][FFFFFF]: You cannot damage another guild's bases when there is an active truce.[FFFFFF]",
                ["WarEndedByAdminNotice"] = "{0} ended the war between {1} and {2}.",
                ["AllWarsEndedByAdmin"] = "{0} ended all wars. Once more the Admins rule the land and we shall  have  peace.",
                ["him"] = "him",
                ["her"] = "her",
            }, this, "en");
        }

        static List<War> WarsList = new List<War>();
        static List<Peace> PeaceList = new List<Peace>();

        private class War
        {
            public string AggressorGuildName { get; set; }
            public ulong AggressorGuildId { get; set; }
            public string AggressorGuildColorCode { get; set; }
            public string DefenderGuildName { get; set; }
            public ulong DefenderGuildId { get; set; }
            public string DefenderGuildColorCode { get; set; }
            public DateTime WarStartTime { get; set; }
            public DateTime WarEndTime { get; set; }
            public bool Truce { get; set; }
            public uint AggressorStrongholdsLost { get; set; }
            public uint DefenderStrongholdsLost { get; set; }
            public uint AggressorCasualties { get; set; }
            public uint DefenderCasualties { get; set; }

            public War()
            {
            }

            public War(Player aggressor, Player defender)
            {
                AggressorGuildName = aggressor.GetGuild().Name;
                AggressorGuildId = aggressor.GetGuild().BaseID;
                AggressorGuildColorCode = aggressor.GetGuild().Banner.CurrentColor.ToHexStringRGB();
                DefenderGuildName = defender.GetGuild().Name;
                DefenderGuildId = defender.GetGuild().BaseID;
                DefenderGuildColorCode = defender.GetGuild().Banner.CurrentColor.ToHexStringRGB();
                WarStartTime = DateTime.Now.AddMinutes(PrepTime);
                WarEndTime = DateTime.Now.AddMinutes((PrepTime + WarLength));
                Truce = false;
                AggressorStrongholdsLost = 0;
                DefenderStrongholdsLost = 0;
                AggressorCasualties = 0;
                DefenderCasualties = 0;
            }
        }

        private class Peace
        {
            public string GuildOneName { get; set; }
            public ulong GuildOneId { get; set; }
            public string GuildTwoName { get; set; }
            public ulong GuildTwoId { get; set; }
            public DateTime WarEndTime { get; set; }
            public DateTime PeaceEndTime { get; set; }
            public bool PeaceAgreementReached { get; set; }

            public Peace()
            {
            }

            public Peace(Guild guildOne, Guild guildTwo, bool peaceAgreementReached)
            {
                GuildOneName = guildOne.Name;
                GuildOneId = guildOne.BaseID;
                GuildTwoName = guildTwo.Name;
                GuildTwoId = guildTwo.BaseID;
                WarEndTime = DateTime.Now;

                if (peaceAgreementReached)
                {
                    PeaceEndTime = DateTime.Now.AddDays(PeaceAgreementLength);
                }
                else
                {
                    PeaceEndTime = DateTime.Now.AddHours(PeaceTime);
                }

                PeaceAgreementReached = peaceAgreementReached;
            }
        }

        void Init()
        {
            if (!Config["EULA", "License", "License Agreement"].Equals(license))
            {
                LoadDefaultConfig();
            }

            LoadConfig();
            PrepTime = Convert.ToInt32(Config["1. War Settings:", "Preparation time [in minutes]:"]);
            WarLength = Convert.ToInt32(Config["1. War Settings:", "War length [in minutes]:"]);
            PeaceTime = Convert.ToInt32(Config["2. Peace Settings:", "Peace time [in hours]:"]);
            PeaceAgreementLength = Convert.ToInt32(Config["2. Peace Settings:", "Peace agreement length [in days]:"]);
            BallistaDamageMultiplicator = Convert.ToSingle(Config["3. Damage Settings:", "Ballista Damage Multiplicator [1.00 == 100%] [more > 1.00 > less]:"]);
            TrebuchetDamageMultiplicator = Convert.ToSingle(Config["3. Damage Settings:", "Trebuchet Damage Multiplicator [1.00 == 100%] [more > 1.00 > less]:"]);
            SeparateCounters = Convert.ToBoolean(Config["Separate (kill) counters?"]);
            LicenseAgreementAccepted = Convert.ToBoolean(Config["EULA", "License Acceptance", "Accept License Agreement"]);

            if (!LicenseAgreementAccepted)
            {
                Unsubscribe(nameof(OnEntityHealthChange));
                Unsubscribe(nameof(OnCubeTakeDamage));
                Unsubscribe(nameof(OnEntityDeath));
            }

            LoadLists();
        }

        void Unload()
        {
            SaveLists();
        }

        void OnServerSave()
        {
            SaveLists();
        }

        void OnServerShutdown()
        {
            SaveLists();
        }

        void LoadLists()
        {
            WarsList = Interface.Oxide.DataFileSystem.ReadObject<List<War>>("CommenceWarWarsList");
            PeaceList = Interface.Oxide.DataFileSystem.ReadObject<List<Peace>>("CommenceWarPeaceList");
        }

        void SaveLists()
        {
            Interface.Oxide.DataFileSystem.WriteObject("CommenceWarWarsList", WarsList);
            Interface.Oxide.DataFileSystem.WriteObject("CommenceWarPeaceList", PeaceList);
        }

        [ChatCommand("warhelp")]
        void SendHelp(Player player, string command, string[] args)
        {
            if (!LicenseAgreementAccepted)
            {
                player.SendError(lang.GetMessage("LicenseAgreementError", this, player.Id.ToString()));
                return;
            }

            if (player.HasPermission("admin"))
            {
                player.SendMessage(string.Format(lang.GetMessage("HelpText", this, player.Id.ToString()), lang.GetMessage("AdminHelpText", this, player.Id.ToString())));
            }
            else
            {
                player.SendMessage(string.Format(lang.GetMessage("HelpText", this, player.Id.ToString()), "\n"));
            }
        }

        [ChatCommand("declarewar")]
        void DeclarationOfWar(Player player, string command, string[] args)
        {
            if (!LicenseAgreementAccepted)
            {
                player.SendError(lang.GetMessage("LicenseAgreementError", this, player.Id.ToString()));
                return;
            }

            bool AlreadyDeclaredWar = false;
            bool AlreadyAtWar = false;
            bool AtPeace = false;
            string GuildAlreadyDeclaredWarTo = "";
            Player defender;
            Player defenderGuildLeader;

            if (!player.GetGuild().Members().GetAllMembers().GetAt(0).PlayerId.Equals(player.Id))
            {
                player.SendError(lang.GetMessage("NotGuildLeaderWarError", this, player.Id.ToString()));
                return;
            }

            if (WarsList.Count != 0)
            {
                for (int i = 0; i < WarsList.Count; i++)
                {
                    if (WarsList.GetAt(i).AggressorGuildId.Equals(player.GetGuild().BaseID))
                    {
                        AlreadyDeclaredWar = true;
                        GuildAlreadyDeclaredWarTo = WarsList.GetAt(i).DefenderGuildName;
                    }
                }
            }

            if (AlreadyDeclaredWar)
            {
                player.SendError(string.Format(lang.GetMessage("AlreadyDeclaredWarError", this, player.Id.ToString()), GuildAlreadyDeclaredWarTo));
                return;
            }

            defender = Server.GetPlayerByName(args[0]);

            if (defender == null)
            {
                player.SendError(string.Format(lang.GetMessage("OnlineTargetPlayerNotFoundError", this, player.Id.ToString()), args[0]));
                return;
            }

            if (defender.Id.ToString().Equals(ServerId))
            {
                player.SendError(lang.GetMessage("ServerError", this, player.Id.ToString()));
                return;
            }

            if (player.GetGuild().BaseID.Equals(defender.GetGuild().BaseID)) return;

            if (PeaceList.Count != 0)
            {
                for (int i = 0; i < PeaceList.Count; i++)
                {
                    Peace peace = PeaceList.GetAt(i);

                    if (peace.GuildOneId.Equals(player.GetGuild().BaseID) && peace.GuildTwoId.Equals(defender.GetGuild().BaseID))
                    {
                        if (DateTime.Compare(peace.PeaceEndTime, DateTime.Now) <= 0)
                        {
                            PeaceList.Remove(peace);
                            return;
                        }
                        else
                        {
                            AtPeace = true;
                        }

                        if (peace.PeaceAgreementReached)
                        {
                            player.SendError(string.Format(lang.GetMessage("ActivePeaceAgreementError", this, player.Id.ToString()), defender.GetGuild().Name, player.GetGuild().Name));
                        }
                        else
                        {
                            player.SendError(string.Format(lang.GetMessage("AtPeaceError", this, player.Id.ToString()), defender.GetGuild().Name));
                        }
                    }
                    else if (peace.GuildOneId.Equals(defender.GetGuild().BaseID) && peace.GuildTwoId.Equals(player.GetGuild().BaseID))
                    {
                        if (DateTime.Compare(peace.PeaceEndTime, DateTime.Now) <= 0)
                        {
                            PeaceList.Remove(peace);
                            return;
                        }
                        else
                        {
                            AtPeace = true;
                        }

                        if (peace.PeaceAgreementReached)
                        {
                            player.SendError(string.Format(lang.GetMessage("ActivePeaceAgreementError", this, player.Id.ToString()), defender.GetGuild().Name, player.GetGuild().Name));
                        }
                        else
                        {
                            player.SendError(string.Format(lang.GetMessage("AtPeaceError", this, player.Id.ToString()), defender.GetGuild().Name));
                        }
                    }
                }
            }

            if (AtPeace) return;

            if (WarsList.Count != 0)
            {
                for (int i = 0; i < WarsList.Count; i++)
                {
                    War thisWar = WarsList.GetAt(i);

                    if (thisWar.AggressorGuildId.Equals(defender.GetGuild().BaseID) && thisWar.DefenderGuildId.Equals(player.GetGuild().BaseID))
                    {
                        AlreadyAtWar = true;
                    }
                }
            }

            if (AlreadyAtWar)
            {
                player.SendError(string.Format(lang.GetMessage("AlreadyAtWarError", this, player.Id.ToString()), defender.GetGuild().Name));
                return;
            }

            defenderGuildLeader = Server.GetPlayerById(defender.GetGuild().Members().GetAllMembers().GetAt(0).PlayerId);

            if (defenderGuildLeader == null)
            {
                player.SendError(string.Format(lang.GetMessage("DefenderGuildLeaderOfflineError", this, player.Id.ToString()), defender.GetGuild().Name));
                return;
            }

            War war = new War(player, defenderGuildLeader);
            WarsList.Add(war);

            PrintToChat(string.Format(lang.GetMessage("WarDeclaredNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName));

            timer.Once((PrepTime * 60), () => PrintToChat(string.Format(lang.GetMessage("WarStartedNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName)));

            WarUpdate(war);
        }

        void WarUpdate(War war)
        {
            if (WarsList.Count == 0) return;

            if (DateTime.Compare(war.WarStartTime, DateTime.Now) >= 0)
            {
                DateTime warStartTime = war.WarStartTime;
                TimeSpan PrepTimeLeft = warStartTime.Subtract(DateTime.Now);
                string prepTimeLeft = PrepTimeLeft.Hours.ToString() + "h " + PrepTimeLeft.Minutes.ToString() + "min. " + PrepTimeLeft.Seconds.ToString() + "sec.";


                PrintToChat(string.Format(lang.GetMessage("WarPrepTimeNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, prepTimeLeft));

                timer.Once(600, () => WarUpdate(war));
                return;
            }
            else if (DateTime.Compare(war.WarEndTime, DateTime.Now) <= 0)
            {
                if (war.AggressorCasualties == 1)
                {
                    if (war.AggressorStrongholdsLost == 0 && war.DefenderStrongholdsLost == 0)
                    {
                        PrintToChat(string.Format(lang.GetMessage("WarIsOverNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualty", this), war.AggressorCasualties.ToString()), war.DefenderCasualties.ToString()));
                    }
                    else if (war.AggressorStrongholdsLost != 0 && war.DefenderStrongholdsLost == 0)
                    {
                        if (war.AggressorStrongholdsLost == 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarIsOverNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualty", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStronghold", this), war.AggressorStrongholdsLost.ToString()), war.DefenderCasualties.ToString()));
                        }
                        else if (war.AggressorStrongholdsLost > 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarIsOverNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualty", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStrongholds", this), war.AggressorStrongholdsLost.ToString()), war.DefenderCasualties.ToString()));
                        }
                    }
                    else if (war.AggressorStrongholdsLost == 0 && war.DefenderStrongholdsLost != 0)
                    {
                        if (war.DefenderStrongholdsLost == 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarIsOverNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualty", this), war.AggressorCasualties.ToString()), war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStronghold", this), war.DefenderStrongholdsLost.ToString())));
                        }
                        else if (war.DefenderStrongholdsLost > 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarIsOverNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualty", this), war.AggressorCasualties.ToString()), war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStrongholds", this), war.DefenderStrongholdsLost.ToString())));
                        }
                    }
                    else if (war.AggressorStrongholdsLost != 0 && war.DefenderStrongholdsLost != 0)
                    {
                        if (war.AggressorStrongholdsLost == 1 && war.DefenderStrongholdsLost == 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarIsOverNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualty", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStronghold", this), war.AggressorStrongholdsLost.ToString()), war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStronghold", this), war.DefenderStrongholdsLost.ToString())));
                        }
                        else if (war.AggressorStrongholdsLost > 1 && war.DefenderStrongholdsLost == 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarIsOverNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualty", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStrongholds", this), war.AggressorStrongholdsLost.ToString()), war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStronghold", this), war.DefenderStrongholdsLost.ToString())));
                        }
                        if (war.AggressorStrongholdsLost == 1 && war.DefenderStrongholdsLost > 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarIsOverNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualty", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStronghold", this), war.AggressorStrongholdsLost.ToString()), war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStrongholds", this), war.DefenderStrongholdsLost.ToString())));
                        }
                        else if (war.AggressorStrongholdsLost > 1 && war.DefenderStrongholdsLost > 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarIsOverNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualty", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStrongholds", this), war.AggressorStrongholdsLost.ToString()), war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStrongholds", this), war.DefenderStrongholdsLost.ToString())));
                        }
                    }
                }
                else
                {
                    if (war.AggressorStrongholdsLost == 0 && war.DefenderStrongholdsLost == 0)
                    {
                        PrintToChat(string.Format(lang.GetMessage("WarIsOverNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualties", this), war.AggressorCasualties.ToString()), war.DefenderCasualties.ToString()));
                    }
                    else if (war.AggressorStrongholdsLost != 0 && war.DefenderStrongholdsLost == 0)
                    {
                        if (war.AggressorStrongholdsLost == 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarIsOverNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualties", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStronghold", this), war.AggressorStrongholdsLost.ToString()), war.DefenderCasualties.ToString()));
                        }
                        else if (war.AggressorStrongholdsLost > 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarIsOverNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualties", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStrongholds", this), war.AggressorStrongholdsLost.ToString()), war.DefenderCasualties.ToString()));
                        }
                    }
                    else if (war.AggressorStrongholdsLost == 0 && war.DefenderStrongholdsLost != 0)
                    {
                        if (war.DefenderStrongholdsLost == 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarIsOverNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualties", this), war.AggressorCasualties.ToString()), war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStronghold", this), war.DefenderStrongholdsLost.ToString())));
                        }
                        else if (war.DefenderStrongholdsLost > 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarIsOverNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualties", this), war.AggressorCasualties.ToString()), war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStrongholds", this), war.DefenderStrongholdsLost.ToString())));
                        }
                    }
                    else if (war.AggressorStrongholdsLost != 0 && war.DefenderStrongholdsLost != 0)
                    {
                        if (war.AggressorStrongholdsLost == 1 && war.DefenderStrongholdsLost == 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarIsOverNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualties", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStronghold", this), war.AggressorStrongholdsLost.ToString()), war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStronghold", this), war.DefenderStrongholdsLost.ToString())));
                        }
                        else if (war.AggressorStrongholdsLost > 1 && war.DefenderStrongholdsLost == 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarIsOverNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualties", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStrongholds", this), war.AggressorStrongholdsLost.ToString()), war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStronghold", this), war.DefenderStrongholdsLost.ToString())));
                        }
                        if (war.AggressorStrongholdsLost == 1 && war.DefenderStrongholdsLost > 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarIsOverNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualties", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStronghold", this), war.AggressorStrongholdsLost.ToString()), war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStrongholds", this), war.DefenderStrongholdsLost.ToString())));
                        }
                        else if (war.AggressorStrongholdsLost > 1 && war.DefenderStrongholdsLost > 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarIsOverNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualties", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStrongholds", this), war.AggressorStrongholdsLost.ToString()), war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStrongholds", this), war.DefenderStrongholdsLost.ToString())));
                        }
                    }
                }

                Guild aggressorGuild = SocialAPI.Get<GuildScheme>().TryGetGuild(war.AggressorGuildId);
                Guild defenderGuild = SocialAPI.Get<GuildScheme>().TryGetGuild(war.DefenderGuildId);

                PeaceList.Add(new Peace(aggressorGuild, defenderGuild, false));

                WarsList.Remove(war);
                return;
            }
            else
            {
                DateTime warEndTime = war.WarEndTime;
                TimeSpan WarTimeLeft = warEndTime.Subtract(DateTime.Now);
                string warTimeLeft = WarTimeLeft.Hours.ToString() + "h " + WarTimeLeft.Minutes.ToString() + "min. " + WarTimeLeft.Seconds.ToString() + "sec.";

                if (war.AggressorCasualties == 1)
                {
                    if (war.AggressorStrongholdsLost == 0 && war.DefenderStrongholdsLost == 0)
                    {
                        PrintToChat(string.Format(lang.GetMessage("WarUpdateNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, string.Format(lang.GetMessage("casualty", this), war.AggressorCasualties.ToString()), war.DefenderGuildColorCode, war.DefenderGuildName, war.DefenderCasualties.ToString(), warTimeLeft));
                    }
                    else if (war.AggressorStrongholdsLost != 0 && war.DefenderStrongholdsLost == 0)
                    {
                        if (war.AggressorStrongholdsLost == 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarUpdateNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, string.Format(lang.GetMessage("casualty", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStronghold", this), war.AggressorStrongholdsLost.ToString()), war.DefenderGuildColorCode, war.DefenderGuildName, war.DefenderCasualties.ToString(), warTimeLeft));
                        }
                        else if (war.AggressorStrongholdsLost > 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarUpdateNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, string.Format(lang.GetMessage("casualty", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStrongholds", this), war.AggressorStrongholdsLost.ToString()), war.DefenderGuildColorCode, war.DefenderGuildName, war.DefenderCasualties.ToString(), warTimeLeft));
                        }
                    }
                    else if (war.AggressorStrongholdsLost == 0 && war.DefenderStrongholdsLost != 0)
                    {
                        if (war.DefenderStrongholdsLost == 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarUpdateNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, string.Format(lang.GetMessage("casualty", this), war.AggressorCasualties.ToString()), war.DefenderGuildColorCode, war.DefenderGuildName, war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStronghold", this), war.DefenderStrongholdsLost.ToString()), warTimeLeft));
                        }
                        else if (war.DefenderStrongholdsLost > 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarUpdateNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, string.Format(lang.GetMessage("casualty", this), war.AggressorCasualties.ToString()), war.DefenderGuildColorCode, war.DefenderGuildName, war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStrongholds", this), war.DefenderStrongholdsLost.ToString()), warTimeLeft));
                        }
                    }
                    else if (war.AggressorStrongholdsLost != 0 && war.DefenderStrongholdsLost != 0)
                    {
                        if (war.AggressorStrongholdsLost == 1 && war.DefenderStrongholdsLost == 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarUpdateNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, string.Format(lang.GetMessage("casualty", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStronghold", this), war.AggressorStrongholdsLost.ToString()), war.DefenderGuildColorCode, war.DefenderGuildName, war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStronghold", this), war.DefenderStrongholdsLost.ToString()), warTimeLeft));
                        }
                        else if (war.AggressorStrongholdsLost > 1 && war.DefenderStrongholdsLost == 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarUpdateNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, string.Format(lang.GetMessage("casualty", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStrongholds", this), war.AggressorStrongholdsLost.ToString()), war.DefenderGuildColorCode, war.DefenderGuildName, war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStronghold", this), war.DefenderStrongholdsLost.ToString()), warTimeLeft));
                        }
                        if (war.AggressorStrongholdsLost == 1 && war.DefenderStrongholdsLost > 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarUpdateNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, string.Format(lang.GetMessage("casualty", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStronghold", this), war.AggressorStrongholdsLost.ToString()), war.DefenderGuildColorCode, war.DefenderGuildName, war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStrongholds", this), war.DefenderStrongholdsLost.ToString()), warTimeLeft));
                        }
                        else if (war.AggressorStrongholdsLost > 1 && war.DefenderStrongholdsLost > 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarUpdateNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, string.Format(lang.GetMessage("casualty", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStrongholds", this), war.AggressorStrongholdsLost.ToString()), war.DefenderGuildColorCode, war.DefenderGuildName, war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStrongholds", this), war.DefenderStrongholdsLost.ToString()), warTimeLeft));
                        }
                    }
                }
                else
                {
                    if (war.AggressorStrongholdsLost == 0 && war.DefenderStrongholdsLost == 0)
                    {
                        PrintToChat(string.Format(lang.GetMessage("WarUpdateNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, string.Format(lang.GetMessage("casualties", this), war.AggressorCasualties.ToString()), war.DefenderGuildColorCode, war.DefenderGuildName, war.DefenderCasualties.ToString(), warTimeLeft));
                    }
                    else if (war.AggressorStrongholdsLost != 0 && war.DefenderStrongholdsLost == 0)
                    {
                        if (war.AggressorStrongholdsLost == 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarUpdateNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, string.Format(lang.GetMessage("casualties", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStronghold", this), war.AggressorStrongholdsLost.ToString()), war.DefenderGuildColorCode, war.DefenderGuildName, war.DefenderCasualties.ToString(), warTimeLeft));
                        }
                        else if (war.AggressorStrongholdsLost > 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarUpdateNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, string.Format(lang.GetMessage("casualties", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStrongholds", this), war.AggressorStrongholdsLost.ToString()), war.DefenderGuildColorCode, war.DefenderGuildName, war.DefenderCasualties.ToString(), warTimeLeft));
                        }
                    }
                    else if (war.AggressorStrongholdsLost == 0 && war.DefenderStrongholdsLost != 0)
                    {
                        if (war.DefenderStrongholdsLost == 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarUpdateNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, string.Format(lang.GetMessage("casualties", this), war.AggressorCasualties.ToString()), war.DefenderGuildColorCode, war.DefenderGuildName, war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStronghold", this), war.DefenderStrongholdsLost.ToString()), warTimeLeft));
                        }
                        else if (war.DefenderStrongholdsLost > 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarUpdateNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, string.Format(lang.GetMessage("casualties", this), war.AggressorCasualties.ToString()), war.DefenderGuildColorCode, war.DefenderGuildName, war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStrongholds", this), war.DefenderStrongholdsLost.ToString()), warTimeLeft));
                        }
                    }
                    else if (war.AggressorStrongholdsLost != 0 && war.DefenderStrongholdsLost != 0)
                    {
                        if (war.AggressorStrongholdsLost == 1 && war.DefenderStrongholdsLost == 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarUpdateNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, string.Format(lang.GetMessage("casualties", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStronghold", this), war.AggressorStrongholdsLost.ToString()), war.DefenderGuildColorCode, war.DefenderGuildName, war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStronghold", this), war.DefenderStrongholdsLost.ToString()), warTimeLeft));
                        }
                        else if (war.AggressorStrongholdsLost > 1 && war.DefenderStrongholdsLost == 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarUpdateNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, string.Format(lang.GetMessage("casualties", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStrongholds", this), war.AggressorStrongholdsLost.ToString()), war.DefenderGuildColorCode, war.DefenderGuildName, war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStronghold", this), war.DefenderStrongholdsLost.ToString()), warTimeLeft));
                        }
                        if (war.AggressorStrongholdsLost == 1 && war.DefenderStrongholdsLost > 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarUpdateNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, string.Format(lang.GetMessage("casualties", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStronghold", this), war.AggressorStrongholdsLost.ToString()), war.DefenderGuildColorCode, war.DefenderGuildName, war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStrongholds", this), war.DefenderStrongholdsLost.ToString()), warTimeLeft));
                        }
                        else if (war.AggressorStrongholdsLost > 1 && war.DefenderStrongholdsLost > 1)
                        {
                            PrintToChat(string.Format(lang.GetMessage("WarUpdateNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, string.Format(lang.GetMessage("casualties", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStrongholds", this), war.AggressorStrongholdsLost.ToString()), war.DefenderGuildColorCode, war.DefenderGuildName, war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStrongholds", this), war.DefenderStrongholdsLost.ToString()), warTimeLeft));
                        }
                    }
                }

                if (WarTimeLeft.TotalSeconds < 600.0)
                {
                    float timerSeconds;
                    float.TryParse(WarTimeLeft.TotalSeconds.ToString(), out timerSeconds);

                    timer.Once(timerSeconds, () => WarUpdate(war));
                }
                else
                {
                    timer.Once(600, () => WarUpdate(war));
                }
            }
        }

        [ChatCommand("offertruce")]
        void OfferingTruce(Player player, string command, string[] args)
        {
            if (!LicenseAgreementAccepted)
            {
                player.SendError(lang.GetMessage("LicenseAgreementError", this, player.Id.ToString()));
                return;
            }

            Player opposingGuildMember;
            Player opposingGuildLeader;
            uint truceTime;

            bool atWar = false;
            bool truce = false;
            bool warStarted = false;

            if (WarsList.Count == 0) return;
            if (args.Length < 2) return;


            if (!player.GetGuild().Members().GetAllMembers().GetAt(0).PlayerId.Equals(player.Id))
            {
                player.SendError(lang.GetMessage("NotGuildLeaderTruceError", this, player.Id.ToString()));
                return;
            }

            opposingGuildMember = Server.GetPlayerByName(args[0]);

            if (opposingGuildMember == null)
            {
                player.SendError(string.Format(lang.GetMessage("OnlineTargetPlayerNotFoundError", this, player.Id.ToString()), args[0]));
                return;
            }

            opposingGuildLeader = Server.GetPlayerById(opposingGuildMember.GetGuild().Members().GetAllMembers().GetAt(0).PlayerId);

            if (opposingGuildLeader == null)
            {
                player.SendError(string.Format(lang.GetMessage("OpposingGuildLeaderOfflineError", this, player.Id.ToString()), opposingGuildMember.GetGuild().Name));
                return;
            }

            if (WarsList.Count != 0)
            {
                for (int i = 0; i < WarsList.Count; i++)
                {
                    War war = WarsList.GetAt(i);

                    if (war.AggressorGuildId.Equals(player.GetGuild().BaseID) && war.DefenderGuildId.Equals(opposingGuildLeader.GetGuild().BaseID))
                    {
                        atWar = true;

                        if (DateTime.Compare(war.WarStartTime, DateTime.Now) <= 0)
                        {
                            warStarted = true;
                        }

                        if (war.Truce)
                        {
                            truce = true;
                        }
                    }
                    else if (war.AggressorGuildId.Equals(opposingGuildLeader.GetGuild().BaseID) && war.DefenderGuildId.Equals(player.GetGuild().BaseID))
                    {
                        atWar = true;

                        if (DateTime.Compare(war.WarStartTime, DateTime.Now) <= 0)
                        {
                            warStarted = true;
                        }

                        if (war.Truce)
                        {
                            truce = true;
                        }
                    }
                }
            }

            if (!atWar)
            {
                player.SendError(string.Format(lang.GetMessage("NotAtWarError", this, player.Id.ToString()), opposingGuildLeader.GetGuild().Name));
                return;
            }
            else if (!warStarted)
            {
                player.SendError(string.Format(lang.GetMessage("WarHasNotStartedYetError", this, player.Id.ToString()), opposingGuildLeader.GetGuild().Name));
                return;
            }
            else if (truce)
            {
                player.SendError(string.Format(lang.GetMessage("TruceAlreadyAgreedUponError", this, player.Id.ToString()), opposingGuildLeader.GetGuild().Name));
                return;
            }

            if (!uint.TryParse(args[1], out truceTime))
            {
                player.SendError(string.Format(lang.GetMessage("TruceTimeError", this, player.Id.ToString()), args[1]));
                return;
            }
            else
            {
                uint.TryParse(args[1], out truceTime);
            }

            player.SendNews(string.Format(lang.GetMessage("OfferSentNotice", this, player.Id.ToString()), opposingGuildLeader.GetGuild().Name));

            opposingGuildLeader.ShowConfirmPopup(lang.GetMessage("CommenceWarPopupTitle", this, opposingGuildLeader.Id.ToString()), string.Format(lang.GetMessage("TruceOfferPopupText", this, opposingGuildLeader.Id.ToString()), player.DisplayName, player.GetGuild().Name, truceTime.ToString()), lang.GetMessage("Accept", this, opposingGuildLeader.Id.ToString()), lang.GetMessage("Deny", this, opposingGuildLeader.Id.ToString()), (options, dialogue, data) => ApplyTruce(player, opposingGuildLeader, truceTime, options, dialogue, data));
        }

        void ApplyTruce(Player player, Player opposingGuildLeader, uint truceTime, Options options, Dialogue dialogue, object data)
        {
            if (WarsList.Count == 0) return;

            if (options.Equals(Options.Cancel) || options.Equals(Options.No))
            {
                return;
            }
            else if (options.Equals(Options.OK) || options.Equals(Options.Yes))
            {
                for (int i = 0; i < WarsList.Count; i++)
                {
                    War war = WarsList.GetAt(i);

                    if ((war.AggressorGuildId.Equals(player.GetGuild().BaseID) && war.DefenderGuildId.Equals(opposingGuildLeader.GetGuild().BaseID)) || (war.AggressorGuildId.Equals(opposingGuildLeader.GetGuild().BaseID) && war.DefenderGuildId.Equals(player.GetGuild().BaseID)))
                    {
                        DateTime newWarEndTime = war.WarEndTime;
                        newWarEndTime.AddMinutes(truceTime);

                        war.WarEndTime = newWarEndTime;
                        war.Truce = true;

                        PrintToChat(string.Format(lang.GetMessage("WarTruceAgreedUponNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, truceTime.ToString()));

                        timer.Once((60 * truceTime), () => EndTruce(war));
                    }
                }
            }
        }

        void EndTruce(War war)
        {
            if (!war.Truce) return;

            war.Truce = false;

            PrintToChat(string.Format(lang.GetMessage("WarTruceIsOverNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName));
        }

        [ChatCommand("offerpeace")]
        void OfferingPeace(Player player, string command, string[] args)
        {
            if (!LicenseAgreementAccepted)
            {
                player.SendError(lang.GetMessage("LicenseAgreementError", this, player.Id.ToString()));
                return;
            }

            Player opposingGuildMember;
            Player opposingGuildLeader;

            bool atWar = false;
            bool warStarted = false;

            if (WarsList.Count == 0) return;

            if (!player.GetGuild().Members().GetAllMembers().GetAt(0).PlayerId.Equals(player.Id))
            {
                player.SendError(lang.GetMessage("NotGuildLeaderPeaceError", this, player.Id.ToString()));
                return;
            }

            opposingGuildMember = Server.GetPlayerByName(args[0]);

            if (opposingGuildMember == null)
            {
                player.SendError(string.Format(lang.GetMessage("OnlineTargetPlayerNotFoundError", this, player.Id.ToString()), args[0]));
                return;
            }

            opposingGuildLeader = Server.GetPlayerById(opposingGuildMember.GetGuild().Members().GetAllMembers().GetAt(0).PlayerId);

            if (opposingGuildLeader == null)
            {
                player.SendError(string.Format(lang.GetMessage("OpposingGuildLeaderOfflineError", this, player.Id.ToString()), opposingGuildMember.GetGuild().Name));
                return;
            }

            if (WarsList.Count != 0)
            {
                for (int i = 0; i < WarsList.Count; i++)
                {
                    War war = WarsList.GetAt(i);

                    if (war.AggressorGuildId.Equals(player.GetGuild().BaseID) && war.DefenderGuildId.Equals(opposingGuildLeader.GetGuild().BaseID))
                    {
                        atWar = true;

                        if (DateTime.Compare(war.WarStartTime, DateTime.Now) <= 0)
                        {
                            warStarted = true;
                        }
                    }
                    else if (war.AggressorGuildId.Equals(opposingGuildLeader.GetGuild().BaseID) && war.DefenderGuildId.Equals(player.GetGuild().BaseID))
                    {
                        atWar = true;

                        if (DateTime.Compare(war.WarStartTime, DateTime.Now) <= 0)
                        {
                            warStarted = true;
                        }
                    }
                }
            }

            if (!atWar)
            {
                player.SendError(string.Format(lang.GetMessage("NotAtWarError", this, player.Id.ToString()), opposingGuildLeader.GetGuild().Name));
                return;
            }
            else if (!warStarted)
            {
                player.SendError(string.Format(lang.GetMessage("WarHasNotStartedYetError", this, player.Id.ToString()), opposingGuildLeader.GetGuild().Name));
                return;
            }

            player.SendNews(string.Format(lang.GetMessage("OfferSentNotice", this, player.Id.ToString()), opposingGuildLeader.GetGuild().Name));

            opposingGuildLeader.ShowConfirmPopup(lang.GetMessage("CommenceWarPopupTitle", this, opposingGuildLeader.Id.ToString()), string.Format(lang.GetMessage("PeaceOfferPopupText", this, opposingGuildLeader.Id.ToString()), player.DisplayName, player.GetGuild().Name), lang.GetMessage("Accept", this, opposingGuildLeader.Id.ToString()), lang.GetMessage("Deny", this, opposingGuildLeader.Id.ToString()), (options, dialogue, data) => BringPeace(player, opposingGuildLeader, options, dialogue, data));
        }

        void BringPeace(Player player, Player opposingGuildLeader, Options options, Dialogue dialogue, object data)
        {
            if (WarsList.Count == 0) return;

            if (options.Equals(Options.Cancel) || options.Equals(Options.No))
            {
                return;
            }
            else if (options.Equals(Options.OK) || options.Equals(Options.Yes))
            {
                for (int i = 0; i < WarsList.Count; i++)
                {
                    War war = WarsList.GetAt(i);

                    if ((war.AggressorGuildId.Equals(player.GetGuild().BaseID) && war.DefenderGuildId.Equals(opposingGuildLeader.GetGuild().BaseID)) || (war.AggressorGuildId.Equals(opposingGuildLeader.GetGuild().BaseID) && war.DefenderGuildId.Equals(player.GetGuild().BaseID)))
                    {
                        if (war.AggressorCasualties == 1)
                        {
                            if (war.AggressorStrongholdsLost == 0 && war.DefenderStrongholdsLost == 0)
                            {
                                PrintToChat(string.Format(lang.GetMessage("PeaceAgreementReached", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualty", this), war.AggressorCasualties.ToString()), war.DefenderCasualties.ToString()));
                            }
                            else if (war.AggressorStrongholdsLost != 0 && war.DefenderStrongholdsLost == 0)
                            {
                                if (war.AggressorStrongholdsLost == 1)
                                {
                                    PrintToChat(string.Format(lang.GetMessage("PeaceAgreementReached", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualty", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStronghold", this), war.AggressorStrongholdsLost.ToString()), war.DefenderCasualties.ToString()));
                                }
                                else if (war.AggressorStrongholdsLost > 1)
                                {
                                    PrintToChat(string.Format(lang.GetMessage("PeaceAgreementReached", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualty", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStrongholds", this), war.AggressorStrongholdsLost.ToString()), war.DefenderCasualties.ToString()));
                                }
                            }
                            else if (war.AggressorStrongholdsLost == 0 && war.DefenderStrongholdsLost != 0)
                            {
                                if (war.DefenderStrongholdsLost == 1)
                                {
                                    PrintToChat(string.Format(lang.GetMessage("PeaceAgreementReached", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualty", this), war.AggressorCasualties.ToString()), war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStronghold", this), war.DefenderStrongholdsLost.ToString())));
                                }
                                else if (war.DefenderStrongholdsLost > 1)
                                {
                                    PrintToChat(string.Format(lang.GetMessage("PeaceAgreementReached", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualty", this), war.AggressorCasualties.ToString()), war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStrongholds", this), war.DefenderStrongholdsLost.ToString())));
                                }
                            }
                            else if (war.AggressorStrongholdsLost != 0 && war.DefenderStrongholdsLost != 0)
                            {
                                if (war.AggressorStrongholdsLost == 1 && war.DefenderStrongholdsLost == 1)
                                {
                                    PrintToChat(string.Format(lang.GetMessage("PeaceAgreementReached", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualty", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStronghold", this), war.AggressorStrongholdsLost.ToString()), war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStronghold", this), war.DefenderStrongholdsLost.ToString())));
                                }
                                else if (war.AggressorStrongholdsLost > 1 && war.DefenderStrongholdsLost == 1)
                                {
                                    PrintToChat(string.Format(lang.GetMessage("PeaceAgreementReached", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualty", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStrongholds", this), war.AggressorStrongholdsLost.ToString()), war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStronghold", this), war.DefenderStrongholdsLost.ToString())));
                                }
                                if (war.AggressorStrongholdsLost == 1 && war.DefenderStrongholdsLost > 1)
                                {
                                    PrintToChat(string.Format(lang.GetMessage("PeaceAgreementReached", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualty", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStronghold", this), war.AggressorStrongholdsLost.ToString()), war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStrongholds", this), war.DefenderStrongholdsLost.ToString())));
                                }
                                else if (war.AggressorStrongholdsLost > 1 && war.DefenderStrongholdsLost > 1)
                                {
                                    PrintToChat(string.Format(lang.GetMessage("PeaceAgreementReached", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualty", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStrongholds", this), war.AggressorStrongholdsLost.ToString()), war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStrongholds", this), war.DefenderStrongholdsLost.ToString())));
                                }
                            }
                        }
                        else
                        {
                            if (war.AggressorStrongholdsLost == 0 && war.DefenderStrongholdsLost == 0)
                            {
                                PrintToChat(string.Format(lang.GetMessage("PeaceAgreementReached", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualties", this), war.AggressorCasualties.ToString()), war.DefenderCasualties.ToString()));
                            }
                            else if (war.AggressorStrongholdsLost != 0 && war.DefenderStrongholdsLost == 0)
                            {
                                if (war.AggressorStrongholdsLost == 1)
                                {
                                    PrintToChat(string.Format(lang.GetMessage("PeaceAgreementReached", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualties", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStronghold", this), war.AggressorStrongholdsLost.ToString()), war.DefenderCasualties.ToString()));
                                }
                                else if (war.AggressorStrongholdsLost > 1)
                                {
                                    PrintToChat(string.Format(lang.GetMessage("PeaceAgreementReached", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualties", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStrongholds", this), war.AggressorStrongholdsLost.ToString()), war.DefenderCasualties.ToString()));
                                }
                            }
                            else if (war.AggressorStrongholdsLost == 0 && war.DefenderStrongholdsLost != 0)
                            {
                                if (war.DefenderStrongholdsLost == 1)
                                {
                                    PrintToChat(string.Format(lang.GetMessage("PeaceAgreementReached", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualties", this), war.AggressorCasualties.ToString()), war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStronghold", this), war.DefenderStrongholdsLost.ToString())));
                                }
                                else if (war.DefenderStrongholdsLost > 1)
                                {
                                    PrintToChat(string.Format(lang.GetMessage("PeaceAgreementReached", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualties", this), war.AggressorCasualties.ToString()), war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStrongholds", this), war.DefenderStrongholdsLost.ToString())));
                                }
                            }
                            else if (war.AggressorStrongholdsLost != 0 && war.DefenderStrongholdsLost != 0)
                            {
                                if (war.AggressorStrongholdsLost == 1 && war.DefenderStrongholdsLost == 1)
                                {
                                    PrintToChat(string.Format(lang.GetMessage("PeaceAgreementReached", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualties", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStronghold", this), war.AggressorStrongholdsLost.ToString()), war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStronghold", this), war.DefenderStrongholdsLost.ToString())));
                                }
                                else if (war.AggressorStrongholdsLost > 1 && war.DefenderStrongholdsLost == 1)
                                {
                                    PrintToChat(string.Format(lang.GetMessage("PeaceAgreementReached", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualties", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStrongholds", this), war.AggressorStrongholdsLost.ToString()), war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStronghold", this), war.DefenderStrongholdsLost.ToString())));
                                }
                                if (war.AggressorStrongholdsLost == 1 && war.DefenderStrongholdsLost > 1)
                                {
                                    PrintToChat(string.Format(lang.GetMessage("PeaceAgreementReached", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualties", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStronghold", this), war.AggressorStrongholdsLost.ToString()), war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStrongholds", this), war.DefenderStrongholdsLost.ToString())));
                                }
                                else if (war.AggressorStrongholdsLost > 1 && war.DefenderStrongholdsLost > 1)
                                {
                                    PrintToChat(string.Format(lang.GetMessage("PeaceAgreementReached", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, string.Format(lang.GetMessage("casualties", this), war.AggressorCasualties.ToString()) + string.Format(lang.GetMessage("lostStrongholds", this), war.AggressorStrongholdsLost.ToString()), war.DefenderCasualties.ToString() + string.Format(lang.GetMessage("lostStrongholds", this), war.DefenderStrongholdsLost.ToString())));
                                }
                            }
                        }

                        Guild aggressorGuild = SocialAPI.Get<GuildScheme>().TryGetGuild(war.AggressorGuildId);
                        Guild defenderGuild = SocialAPI.Get<GuildScheme>().TryGetGuild(war.DefenderGuildId);

                        PeaceList.Add(new Peace(aggressorGuild, defenderGuild, true));

                        WarsList.Remove(war);
                    }
                }
            }
        }

        [ChatCommand("endwar")]
        void EndWar(Player player, string command, string[] args)
        {
            if (!LicenseAgreementAccepted)
            {
                player.SendError(lang.GetMessage("LicenseAgreementError", this, player.Id.ToString()));
                return;
            }

            if (!player.HasPermission("admin")) return;

            Player warrior;

            warrior = Server.GetPlayerByName(args[0]);

            if (warrior == null)
            {
                player.SendError(string.Format(lang.GetMessage("OnlineTargetPlayerNotFoundError", this, player.Id.ToString()), args[0]));
                return;
            }

            for (int i = 0; i < WarsList.Count; i++)
            {
                War war = WarsList.GetAt(i);

                if (war.AggressorGuildId.Equals(warrior.GetGuild().BaseID))
                {
                    Guild AggressorGuild = SocialAPI.Get<GuildScheme>().TryGetGuild(war.AggressorGuildId);
                    Guild DefenderGuild = SocialAPI.Get<GuildScheme>().TryGetGuild(war.DefenderGuildId);

                    List<Player> AggressorGuildMembers = AggressorGuild.Members().GetMemberPlayers();
                    List<Player> DefenderGuildMembers = DefenderGuild.Members().GetMemberPlayers();

                    for (i = 0; i < AggressorGuildMembers.Count; i++)
                    {
                        AggressorGuildMembers.GetAt(i).SendNews(string.Format(lang.GetMessage("WarEndedByAdminNotice", this, AggressorGuildMembers.GetAt(i).Id.ToString()), player.DisplayName, war.AggressorGuildName, war.DefenderGuildName));
                    }
                    for (i = 0; i < DefenderGuildMembers.Count; i++)
                    {
                        DefenderGuildMembers.GetAt(i).SendNews(string.Format(lang.GetMessage("WarEndedByAdminNotice", this, DefenderGuildMembers.GetAt(i).Id.ToString()), player.DisplayName, war.AggressorGuildName, war.DefenderGuildName));
                    }

                    PrintToChat(string.Format(lang.GetMessage("WarEndedByAdminNotice", this), player.DisplayName, war.AggressorGuildName, war.DefenderGuildName));

                    WarsList.Remove(war);
                }
            }
        }

        [ChatCommand("endallwars")]
        void EndAllWars(Player player, string command)
        {
            if (!LicenseAgreementAccepted)
            {
                player.SendError(lang.GetMessage("LicenseAgreementError", this, player.Id.ToString()));
                return;
            }

            if (!player.HasPermission("admin")) return;

            PrintToChat(string.Format(lang.GetMessage("AllWarsEndedByAdmin", this), player.DisplayName));

            WarsList.Clear();
        }

        void OnEntityHealthChange(EntityDamageEvent ede)
        {
            if (!LicenseAgreementAccepted) return;
            if (ede.Damage.DamageSource.Owner.Id.ToString().Equals(ServerId)) return;

            bool atWar = false;

            Vector3 worldCoordinate = ede.Entity.Position;

            Crest crest = SocialAPI.Get<CrestScheme>().GetCrestAt(worldCoordinate);

            if (crest == null) return;

            if (crest.SocialId == ede.Damage.DamageSource.Owner.GetGuild().BaseID) return;

            if (ede.Damage.Amount < 0) return;

            timer.Once(1, () => damageSourceOwner = Server.GetPlayerById(9999999999));

            if (WarsList.Count == 0)
            {
                if (!ede.Entity.IsPlayer)
                {
                    if (ede.Entity.SocialOwner() != ede.Damage.DamageSource.Owner.GetGuild().BaseID)
                    {
                        if (ede.Entity.name.Contains("Crest"))
                        {
                            ede.Cancel(lang.GetMessage("CrestDamageNotAtWarError", this));
                            ede.Damage.Amount = 0f;
                            SendReply(ede.Damage.DamageSource.Owner, lang.GetMessage("CrestDamageNotAtWarError", this, ede.Damage.DamageSource.Owner.Id.ToString()));
                            return;
                        }
                        else
                        {
                            if (ede.Entity.SocialOwner() != 0)
                            {
                                ede.Cancel(lang.GetMessage("BaseDamageNotAtWarError", this));
                                ede.Damage.Amount = 0f;

                                if (damageSourceOwner != ede.Damage.DamageSource.Owner)
                                {
                                    damageSourceOwner = ede.Damage.DamageSource.Owner;

                                    SendReply(ede.Damage.DamageSource.Owner, lang.GetMessage("BaseDamageNotAtWarError", this, ede.Damage.DamageSource.Owner.Id.ToString()));
                                }

                                return;
                            }
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < WarsList.Count; i++)
                {
                    War war = WarsList.GetAt(i);

                    if (war.AggressorGuildId.Equals(ede.Damage.DamageSource.Owner.GetGuild().BaseID) && war.DefenderGuildId.Equals(ede.Entity.Owner.GetGuild().BaseID))
                    {
                        if (DateTime.Compare(war.WarStartTime, DateTime.Now) <= 0)
                        {
                            atWar = true;
                        }

                        if (DateTime.Compare(war.WarStartTime, DateTime.Now) > 0)
                        {
                            timer.Once(1, () => ede.Damage.DamageSource.Owner.SendError(string.Format(lang.GetMessage("WarHasNotStartedYetError", this, ede.Damage.DamageSource.Owner.Id.ToString()), war.DefenderGuildName)));
                        }
                    }
                    else if (war.AggressorGuildId.Equals(ede.Entity.Owner.GetGuild().BaseID) && war.DefenderGuildId.Equals(ede.Damage.DamageSource.Owner.GetGuild().BaseID))
                    {
                        if (DateTime.Compare(war.WarStartTime, DateTime.Now) <= 0)
                        {
                            atWar = true;
                        }

                        if (DateTime.Compare(war.WarStartTime, DateTime.Now) > 0)
                        {
                            timer.Once(1, () => ede.Damage.DamageSource.Owner.SendError(string.Format(lang.GetMessage("WarHasNotStartedYetError", this, ede.Damage.DamageSource.Owner.Id.ToString()), war.AggressorGuildName)));
                        }
                    }
                }
            }

            if (!atWar)
            {
                if (!ede.Entity.IsPlayer)
                {
                    if (ede.Entity.SocialOwner() != ede.Damage.DamageSource.Owner.GetGuild().BaseID)
                    {
                        if (ede.Entity.name.Contains("Crest"))
                        {
                            ede.Cancel(lang.GetMessage("CrestDamageNotAtWarError", this));
                            ede.Damage.Amount = 0f;
                            SendReply(ede.Damage.DamageSource.Owner, lang.GetMessage("CrestDamageNotAtWarError", this, ede.Damage.DamageSource.Owner.Id.ToString()));
                            return;
                        }
                        else
                        {
                            if (ede.Entity.SocialOwner() != 0)
                            {
                                ede.Cancel(lang.GetMessage("BaseDamageNotAtWarError", this));
                                ede.Damage.Amount = 0f;

                                if (damageSourceOwner != ede.Damage.DamageSource.Owner)
                                {
                                    damageSourceOwner = ede.Damage.DamageSource.Owner;

                                    SendReply(ede.Damage.DamageSource.Owner, lang.GetMessage("BaseDamageNotAtWarError", this, ede.Damage.DamageSource.Owner.Id.ToString()));
                                }

                                return;
                            }
                        }
                    }
                }
            }
            else if (ede.Damage.Damager.name.Contains("Ballista"))
            {
                float newDamageAmount = (ede.Damage.Amount * BallistaDamageMultiplicator);
                ede.Damage.Amount = newDamageAmount;
            }
        }

        void OnCubeTakeDamage(CubeDamageEvent cde)
        {
            if (!LicenseAgreementAccepted) return;
            if (cde.Damage.DamageSource.Owner.Id.ToString().Equals(ServerId)) return;

            bool atWar = false;
            bool truce = false;

            Vector3 worldCoordinate = cde.Grid.LocalToWorldCoordinate(cde.Position);

            Crest crest = SocialAPI.Get<CrestScheme>().GetCrestAt(worldCoordinate);

            if (crest == null) return;

            if (crest.SocialId == cde.Damage.DamageSource.Owner.GetGuild().BaseID) return;

            if (cde.Damage.Amount < 0) return;

            timer.Once(1, () => damageSourceOwner = Server.GetPlayerById(9999999999));

            if (WarsList.Count == 0)
            {
                cde.Cancel(lang.GetMessage("BaseDamageNotAtWarError", this));
                cde.Damage.Amount = 0f;

                if (damageSourceOwner != cde.Damage.DamageSource.Owner)
                {
                    damageSourceOwner = cde.Damage.DamageSource.Owner;

                    SendReply(cde.Damage.DamageSource.Owner, lang.GetMessage("BaseDamageNotAtWarError", this, cde.Damage.DamageSource.Owner.Id.ToString()));
                }
            }
            else
            {
                for (int i = 0; i < WarsList.Count; i++)
                {
                    War war = WarsList.GetAt(i);

                    if (war.AggressorGuildId.Equals(cde.Damage.DamageSource.Owner.GetGuild().BaseID) && war.DefenderGuildId.Equals(crest.SocialId))
                    {
                        if (DateTime.Compare(war.WarStartTime, DateTime.Now) <= 0)
                        {
                            atWar = true;
                        }

                        if (DateTime.Compare(war.WarStartTime, DateTime.Now) > 0)
                        {
                            timer.Once(1, () => cde.Damage.DamageSource.Owner.SendError(string.Format(lang.GetMessage("WarHasNotStartedYetError", this, cde.Damage.DamageSource.Owner.Id.ToString()), war.DefenderGuildName)));
                        }

                        truce = war.Truce;
                    }
                    else if (war.AggressorGuildId.Equals(crest.SocialId) && war.DefenderGuildId.Equals(cde.Damage.DamageSource.Owner.GetGuild().BaseID))
                    {
                        if (DateTime.Compare(war.WarStartTime, DateTime.Now) <= 0)
                        {
                            atWar = true;
                        }

                        if (DateTime.Compare(war.WarStartTime, DateTime.Now) > 0)
                        {
                            timer.Once(1, () => cde.Damage.DamageSource.Owner.SendError(string.Format(lang.GetMessage("WarHasNotStartedYetError", this, cde.Damage.DamageSource.Owner.Id.ToString()), war.AggressorGuildName)));
                        }

                        truce = war.Truce;
                    }
                }

                if (!atWar)
                {
                    cde.Cancel(lang.GetMessage("BaseDamageNotAtWarError", this));
                    cde.Damage.Amount = 0f;

                    if (damageSourceOwner != cde.Damage.DamageSource.Owner)
                    {
                        damageSourceOwner = cde.Damage.DamageSource.Owner;

                        SendReply(cde.Damage.DamageSource.Owner, lang.GetMessage("BaseDamageNotAtWarError", this, cde.Damage.DamageSource.Owner.Id.ToString()));
                    }
                }
                else if (truce)
                {
                    cde.Cancel(lang.GetMessage("BaseDamageTruceError", this));
                    cde.Damage.Amount = 0f;

                    if (damageSourceOwner != cde.Damage.DamageSource.Owner)
                    {
                        damageSourceOwner = cde.Damage.DamageSource.Owner;

                        SendReply(cde.Damage.DamageSource.Owner, lang.GetMessage("BaseDamageTruceError", this, cde.Damage.DamageSource.Owner.Id.ToString()));
                    }
                }
                else if (cde.Damage.Damager.name.Contains("Trebuchet"))
                {
                    float newDamageAmount = (cde.Damage.Amount * TrebuchetDamageMultiplicator);
                    cde.Damage.Amount = newDamageAmount;
                }
            }
        }

        void OnEntityDeath(EntityDeathEvent ede)
        {
            if (ede.KillingDamage.DamageSource.Owner.Id.ToString().Equals(ServerId)) return;
            if (!ede.Entity.IsPlayer && !ede.Entity.name.Contains("Crest")) return;
            if (WarsList.Count == 0) return;

            for (int i = 0; i < WarsList.Count; i++)
            {
                War war = WarsList.GetAt(i);

                if (ede.Entity.Owner.GetGuild().BaseID.Equals(war.AggressorGuildId))
                {
                    if (SeparateCounters && !ede.KillingDamage.DamageSource.Owner.GetGuild().BaseID.Equals(war.DefenderGuildId)) return;

                    if (ede.Entity.IsPlayer)
                    {
                        uint casualties = war.AggressorCasualties + 1;
                        war.AggressorCasualties = casualties;
                    }
                    else if (ede.Entity.name.Contains("Crest"))
                    {
                        uint strongholdsLost = war.AggressorStrongholdsLost + 1;
                        war.AggressorStrongholdsLost = strongholdsLost;
                    }

                    if (war.Truce)
                    {
                        string gender = "";
                        war.Truce = false;

                        if (ede.KillingDamage.DamageSource.Owner.GetCharacterGender())
                        {
                            gender = lang.GetMessage("him", this);
                        }
                        else
                        {
                            gender = lang.GetMessage("her", this);
                        }

                        PrintToChat(string.Format(lang.GetMessage("WarTruceBrokenNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, ede.KillingDamage.DamageSource.Owner.DisplayName, gender, ede.KillingDamage.DamageSource.Owner.GetGuild().Name));
                    }
                }
                else if (ede.Entity.Owner.GetGuild().BaseID.Equals(war.DefenderGuildId))
                {
                    if (SeparateCounters && !ede.KillingDamage.DamageSource.Owner.GetGuild().BaseID.Equals(war.AggressorGuildId)) return;

                        if (ede.Entity.IsPlayer)
                        {
                            uint casualties = war.DefenderCasualties + 1;
                            war.DefenderCasualties = casualties;
                        }
                        else if (ede.Entity.name.Contains("Crest"))
                        {
                            uint strongholdsLost = war.DefenderStrongholdsLost + 1;
                            war.DefenderStrongholdsLost = strongholdsLost;
                        }

                    if (war.Truce)
                    {
                        string gender = "";
                        war.Truce = false;

                        if (ede.KillingDamage.DamageSource.Owner.GetCharacterGender())
                        {
                            gender = lang.GetMessage("him", this);
                        }
                        else
                        {
                            gender = lang.GetMessage("her", this);
                        }

                        PrintToChat(string.Format(lang.GetMessage("WarTruceBrokenNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, war.DefenderGuildColorCode, war.DefenderGuildName, ede.KillingDamage.DamageSource.Owner.DisplayName, gender, ede.KillingDamage.DamageSource.Owner.GetGuild().Name));
                    }
                }

                if (ede.Entity.Owner.GetGuild().BaseID.Equals(war.AggressorGuildId))
                {
                    if (ede.Entity.name.Contains("Crest"))
                    {
                        PrintToChat(string.Format(lang.GetMessage("StrongholdLostNotice", this), war.AggressorGuildColorCode, war.AggressorGuildName, ede.KillingDamage.DamageSource.Owner.GetGuild().Banner.CurrentColor.ToHexStringRGB(), ede.KillingDamage.DamageSource.Owner.GetGuild().Name));
                    }
                }
                else if (ede.Entity.Owner.GetGuild().BaseID.Equals(war.DefenderGuildId))
                {
                    if (ede.Entity.name.Contains("Crest"))
                    {
                        PrintToChat(string.Format(lang.GetMessage("StrongholdLostNotice", this), war.DefenderGuildColorCode, war.DefenderGuildName, ede.KillingDamage.DamageSource.Owner.GetGuild().Banner.CurrentColor.ToHexStringRGB(), ede.KillingDamage.DamageSource.Owner.GetGuild().Name));
                    }
                }
            }
        }
    }
}