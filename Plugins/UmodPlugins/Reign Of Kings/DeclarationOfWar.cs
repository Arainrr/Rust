﻿using CodeHatch.Blocks.Networking.Events;
using CodeHatch.Common;
using CodeHatch.Engine;
using CodeHatch.Engine.Core.Cache;
using CodeHatch.Engine.Modules.SocialSystem;
using CodeHatch.Engine.Networking;
using CodeHatch.Thrones.SocialSystem;
using Oxide.Core;
using Oxide.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CodeHatch.Networking.Events.Entities;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("DeclarationOfWar", "D-Kay & juk3b0x & Scorpyon", "3.0.0")]
    public class DeclarationOfWar : ReignOfKingsPlugin
    {
        #region Variables

        #region Fields

        private Configuration _config;

        #endregion

        #region Properties

        private new Configuration Config
        {
            get { return _config ?? (_config = new Configuration(base.Config)); }
        }

        private readonly List<string> _permissions = new List<string>
        {
            "Modify"
        };

        private WarManager Manager { get; set; } = new WarManager();

        #endregion

        #region Enums

        public enum MarkResult
        {
            None,
            AddedFirst,
            AddedSecond,
            AddedCenter,
            AddedSize,
            Finished,
            Removed,
            AlreadyExists,
            NonExisting
        }

        #endregion

        #region Classes

        public class Configuration
        {
            public static Configuration Instance { get; private set; }

            private DynamicConfigFile Config { get; }
            private bool ConfigHasChanged { get; set; }

            public TimeSpan TimePreparation { get; set; }
            public TimeSpan TimeWar { get; set; }
            public TimeSpan TimeCooldown { get; set; }

            public int IntervalReport { get; set; }

            public bool WarFamilyOnly { get; set; }

            public bool WarDeclineEnabled { get; set; }
            public ulong WarDeclinePrice { get; set; }

            public bool WarForceEnabled { get; set; }
            public ulong WarForcePrice { get; set; }

            public bool WarEndEnabled { get; set; }
            public ulong WarEndPrice { get; set; }

            public Configuration(DynamicConfigFile config)
            {
                this.Config = config;
                this.ConfigHasChanged = false;
                Instance = this;
            }

            public void Load()
            {
                TimePreparation = TimeSpan.FromSeconds(GetConfig(600, "Time", "Preparation"));
                TimeWar = TimeSpan.FromSeconds(GetConfig(3600, "Time", "War"));
                TimeCooldown = TimeSpan.FromSeconds(GetConfig(3600, "Time", "Cooldown"));

                IntervalReport = GetConfig(300, "Interval", "Report");

                WarFamilyOnly = GetConfig(false, "War", "Family only");

                WarDeclineEnabled = GetConfig(true, "War", "Decline", "Enabled");
                WarDeclinePrice = GetConfig(10000ul, "War", "Decline", "Price");

                WarForceEnabled = GetConfig(true, "War", "Force", "Enabled");
                WarForcePrice = GetConfig(10000ul, "War", "Force", "Price");

                WarEndEnabled = GetConfig(true, "War", "End", "Enabled");
                WarEndPrice = GetConfig(10000ul, "War", "End", "Price");

                if (!ConfigHasChanged) return;
                this.Save();
                ConfigHasChanged = false;
            }

            public void Save()
            {
                Config.Set("Time", "Preparation", TimePreparation.TotalSeconds);
                Config.Set("Time", "War", TimeWar.TotalSeconds);
                Config.Set("Time", "Cooldown", TimeCooldown.TotalSeconds);

                Config.Set("Interval", "Report", IntervalReport);

                Config.Set("War", "Family only", WarFamilyOnly);

                Config.Set("War", "Decline", "Enabled", WarDeclineEnabled);
                Config.Set("War", "Decline", "Price", WarDeclinePrice);

                Config.Set("War", "Force", "Enabled", WarForceEnabled);
                Config.Set("War", "Force", "Price", WarForcePrice);

                Config.Set("War", "End", "Enabled", WarEndEnabled);
                Config.Set("War", "End", "Price", WarEndPrice);

                Config.Save();
            }

            public void LoadDefault()
            {
                this.Load();
                this.Save();
            }

            private T GetConfig<T>(T defaultValue, params string[] path)
            {
                try
                {
                    var value = Config.Get(path);
                    if (value == null) throw new Exception("There is no return value with provided arguments.");
                    return Config.ConvertValue<T>(value);
                }
                catch (Exception)
                {
                    ConfigHasChanged = true;
                    return defaultValue;
                }
            }
        }

        public class WarManager
        {
            public List<Area> Areas = new List<Area>();
            public List<Preparation> Preparations { get; set; } = new List<Preparation>();
            public List<War> Wars { get; set; } = new List<War>();
            public List<Cooldown> Cooldowns { get; set; } = new List<Cooldown>();

            public WarManager() { }

            public int WarCount()
            {
                return Preparations.Count + Wars.Count;
            }

            public WarInformation GetWar(ulong guildId)
            {
                WarInformation war = Preparations.FirstOrDefault(p => p.HasParticipant(guildId));
                return war ?? Wars.FirstOrDefault(w => w.HasParticipant(guildId));
            }

            public War GetWarByPlayer(ulong playerId)
            {
                return Wars.FirstOrDefault(w => w.HasMember(playerId));
            }

            public void PrepareWar(Guild declarer, Guild target)
            {
                Preparations.Add(new Preparation(declarer, target));
            }

            public void DeclareWar(Guild declarer, Guild target)
            {
                Wars.Add(new War(declarer, target));
            }

            public void JoinWar(Guild declarer, Guild target)
            {
                var war = GetWar(target.BaseID);
                war.Join(declarer);
            }

            public bool IsInPreparation(ulong guildId)
            {
                return Preparations.Any(p => p.HasParticipant(guildId));
            }

            public bool IsInWar(ulong guildId)
            {
                return IsInPreparation(guildId) || Wars.Any(w => w.HasParticipant(guildId));
            }

            public bool IsInCooldown(ulong guildId)
            {
                return Cooldowns.Any(c => c.HasParticipant(guildId));
            }

            public bool IsAtWar(ulong guild1, ulong guild2)
            {
                return Wars.Any(w => w.HasParticipants(guild1, guild2));
            }

            public bool IsSiegeArea(Vector3 position)
            {
                return Areas.Any(x => x.Contains2D(position));
            }

            public bool HasMarks()
            {
                return Areas.Any();
            }

            public MarkResult AddMark(Vector3 position)
            {
                Area area = null;
                if (Areas.Any()) area = Areas.FirstOrDefault(x => !x.Completed);
                if (area == null)
                {
                    area = new Area();
                    Areas.Add(area);
                }

                return area.AddPosition(position);
            }

            public MarkResult RemoveLastMark()
            {
                var area = Areas.LastOrDefault();
                if (area == null) return MarkResult.NonExisting;
                Areas.Remove(area);
                return MarkResult.Removed;
            }

            public MarkResult RemoveAllMarks()
            {
                if (!Areas.Any()) return MarkResult.NonExisting;
                Areas.Clear();
                return MarkResult.Removed;
            }

            public List<Preparation> GetFinishedPreparations()
            {
                var list = new List<Preparation>();
                foreach (var preparation in Preparations.ToList())
                {
                    if (!preparation.IsFinished()) continue;
                    list.Add(preparation);
                    Preparations.Remove(preparation);
                    if (Configuration.Instance.TimeWar.TotalSeconds <= 0) continue;
                    Wars.Add(new War(preparation));
                }
                return list;
            }

            public List<War> GetFinishedWars()
            {
                var list = new List<War>();
                foreach (var war in Wars.ToList())
                {
                    if (!war.IsFinished()) continue;
                    list.Add(war);
                    Wars.Remove(war);
                    if (Configuration.Instance.TimeCooldown.TotalSeconds <= 0) continue;
                    Cooldowns.Add(new Cooldown(war));
                }
                return list;
            }

            public List<Cooldown> GetFinishedCooldowns()
            {
                var list = new List<Cooldown>();
                foreach (var cooldown in Cooldowns.ToList())
                {
                    if (!cooldown.IsFinished()) continue;
                    list.Add(cooldown);
                    Cooldowns.Remove(cooldown);
                }
                return list;
            }
        }

        public class WarInformation
        {
            public List<GuildData> Participants { get; set; } = new List<GuildData>();
            public DateTime EndDate { get; set; }

            public WarInformation() { }

            public WarInformation(GuildData declarer, GuildData defender, TimeSpan duration)
            {
                this.Participants.Add(declarer);
                this.Participants.Add(defender);
                this.EndDate = SafeTime.ServerDate.Add(duration);
            }

            public WarInformation(WarInformation information, TimeSpan duration)
            {
                this.Participants = information.Participants;
                this.EndDate = SafeTime.ServerDate.Add(duration);
            }

            public void Join(GuildData guild)
            {
                Participants.Add(guild);
            }

            public bool HasParticipant(ulong guildId)
            {
                return Participants.Any(p => p.Equals(guildId));
            }

            public bool HasParticipants(ulong guild1, ulong guild2)
            {
                return Participants.Any(p => p.Equals(guild1)) && Participants.Any(p => p.Equals(guild2));
            }

            public TimeSpan GetRemainingTime()
            {
                return EndDate.Subtract(SafeTime.ServerDate);
            }

            public bool IsFinished()
            {
                return SafeTime.ServerDate > EndDate;
            }
        }

        public class Preparation : WarInformation
        {
            public Preparation() { }

            public Preparation(GuildData declarer, GuildData defender) :
                base(declarer, defender, Configuration.Instance.TimePreparation) { }
        }

        public class War : WarInformation
        {
            public War() { }

            public War(Preparation preparation) :
                base(preparation, Configuration.Instance.TimeWar) { }

            public War(GuildData declarer, GuildData defender) :
                base(declarer, defender, Configuration.Instance.TimeWar) { }

            public bool HasMember(ulong playerId)
            {
                return Participants.Any(p => p.HasMember(playerId));
            }
        }
        
        public class Cooldown : WarInformation
        {
            public Cooldown() { }

            public Cooldown(War war) :
                base(war, Configuration.Instance.TimeCooldown) { }
        }
        
        public class GuildData
        {
            public ulong Id { get; set; }
            public string Name { get; set; }
            public HashSet<ulong> Members { get; set; } = new HashSet<ulong>();

            public GuildData() { }

            public GuildData(Guild guild)
            {
                this.Id = guild.BaseID;
                this.Name = guild.Name;
                this.UpdateMembers(guild);
            }

            public Guild GetGuild()
            {
                return SocialAPI.Get<GuildScheme>().TryGetGuild(this.Id);
            }

            public bool HasMember(ulong playerId)
            {
                return this.Members.Contains(playerId);
            }

            public bool Equals(ulong id)
            {
                return this.Id == id;
            }

            public void UpdateMembers(Guild guild)
            {
                this.Members.Clear();
                guild.Members().GetAllMembers().Foreach(m => this.Members.Add(m.PlayerId));
            }

            public void Update()
            {
                var guild = SocialAPI.Get<GuildScheme>().TryGetGuild(this.Id);
                if (guild == null) return;
                this.Name = guild.Name;
            }

            public static implicit operator GuildData(Guild guild) => new GuildData(guild);
            public static implicit operator Guild(GuildData guild) => guild.GetGuild();
        }

        public class Area
        {
            public bool Completed => Center != null && Size != null;

            public Vector3D Center { get; set; }
            public Vector3D Size { get; set; }

            public Vector3D FirstPosition { get; set; }
            public Vector3D SecondPosition { get; set; }

            public Bounds? Bounds()
            {
                if (Center == null || Size == null) return null;
                return new Bounds(Center, Size);
            }

            public Rect? Rect()
            {
                if (Center == null || Size == null) return null;
                return new Rect(Center - Size / 2, Size);
            }

            public Area() { }

            public Area(Vector3 position)
            {
                FirstPosition = position;
            }

            public Area(Vector3 position1, Vector3 position2)
            {
                FirstPosition = position1;
                SecondPosition = position2;
            }

            public bool CalculateCenterSize()
            {
                if (GetPositions() != 2) return false;

                var scale = FirstPosition - SecondPosition;
                scale.X = Mathf.Abs(scale.X);
                scale.Y = Mathf.Abs(scale.Y);
                scale.Z = Mathf.Abs(scale.Z);

                Center = (FirstPosition + SecondPosition) * 0.5f;
                Size = scale;
                return true;
            }

            public void Clear()
            {
                Center = null;
                Size = null;
                FirstPosition = null;
                SecondPosition = null;
            }

            public int GetPositions()
            {
                if (FirstPosition == null) return 0;
                if (SecondPosition == null) return 1;
                return 2;
            }

            public MarkResult AddCenter(Vector3 position)
            {
                if (Center != null) return MarkResult.AlreadyExists;
                Center = position;
                return MarkResult.AddedCenter;
            }

            public MarkResult AddSize(Vector3 size)
            {
                if (Size != null) return MarkResult.AlreadyExists;
                Size = size;
                return MarkResult.AddedSize;
            }

            public MarkResult AddPosition(Vector3 position)
            {
                switch (GetPositions())
                {
                    case 0:
                        FirstPosition = position;
                        return MarkResult.AddedFirst;
                    case 1:
                        SecondPosition = position;
                        CalculateCenterSize();
                        return MarkResult.AddedSecond;
                    case 2:
                        return MarkResult.AlreadyExists;
                    default:
                        return MarkResult.None;
                }
            }

            public bool Contains3D(Vector3 position)
            {
                var bounds = Bounds();
                return bounds != null && ((Bounds)bounds).Contains(position);
            }

            public bool Contains2D(Vector3 position)
            {
                var rect = Rect();
                return rect != null && ((Rect)rect).Contains(position.GetXZ());
            }

            public bool Contains3D(Area area)
            {
                var bounds = Bounds();
                if (bounds == null) return false;
                var bounds2 = area.Bounds();
                if (bounds2 == null) return false;
                return ((Bounds)bounds).Intersects((Bounds)bounds2);
            }

            public bool Contains2D(Area area)
            {
                var rect = Rect();
                if (rect == null) return false;
                var rect2 = area.Rect();
                if (rect2 == null) return false;
                return ((Rect)rect).Overlaps((Rect)rect2);
            }

            public override string ToString()
            {
                return GetPositions() == 2 ?
                    $"Position 1: {FirstPosition}; Position 2: {SecondPosition}" :
                    $"Position 1: {FirstPosition}; Position 2: not set";
            }
        }

        public class Vector3D
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }

            public Vector3D() { }

            public Vector3D(float x, float z)
            {
                this.X = x;
                this.Y = 0f;
                this.Z = z;
            }

            public Vector3D(float x, float y, float z)
            {
                this.X = x;
                this.Y = y;
                this.Z = z;
            }

            private Vector3D(Vector2 position)
            {
                X = position.x;
                Y = 0f;
                Z = position.y;
            }

            private Vector3D(Vector3 position)
            {
                X = position.x;
                Y = position.y;
                Z = position.z;
            }

            public override string ToString()
            {
                return $"{X}, {Y}, {Z}";
            }

            public static Vector3D operator -(Vector3D obj1, Vector3D obj2)
            {
                return new Vector3D(obj1.X - obj2.X, obj1.Y - obj2.Y, obj1.Z - obj2.Z);
            }
            public static Vector3D operator +(Vector3D obj1, Vector3D obj2)
            {
                return new Vector3D(obj1.X + obj2.X, obj1.Y + obj2.Y, obj1.Z + obj2.Z);
            }
            public static Vector3D operator *(Vector3D obj1, Vector3D obj2)
            {
                return new Vector3D(obj1.X * obj2.X, obj1.Y * obj2.Y, obj1.Z * obj2.Z);
            }
            public static Vector3D operator *(Vector3D obj1, float obj2)
            {
                return new Vector3D(obj1.X * obj2, obj1.Y * obj2, obj1.Z * obj2);
            }
            public static Vector3D operator /(Vector3D obj1, Vector3D obj2)
            {
                return new Vector3D(obj1.X / obj2.X, obj1.Y / obj2.Y, obj1.Z / obj2.Z);
            }
            public static Vector3D operator /(Vector3D obj1, float obj2)
            {
                return new Vector3D(obj1.X / obj2, obj1.Y / obj2, obj1.Z / obj2);
            }

            public static implicit operator Vector3(Vector3D vector3D) => new Vector3(vector3D.X, vector3D.Y, vector3D.Z);
            public static implicit operator Vector2(Vector3D vector3D) => new Vector2(vector3D.X, vector3D.Z);
            public static implicit operator Vector3D(Vector3 vector3) => new Vector3D(vector3);
            public static implicit operator Vector3D(Vector2 vector2) => new Vector3D(vector2);
        }

        #endregion

        #endregion

        #region Save and Load Data

        private void Loaded()
        {
            LoadPluginData();
            Config.Load();
            RegisterPermissions();
        }

        private void OnServerInitialized()
        {
            timer.Repeat(Config.IntervalReport, 0, WarReport);
            timer.Repeat(1, 0, Interval);
        }

        private void LoadPluginData()
        {
            Manager = Interface.Oxide.DataFileSystem.ReadObject<WarManager>("Declaration of War");
        }

        private void SavePluginData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("Declaration of War", Manager);
        }

        protected override void LoadDefaultConfig()
        {
            Config.LoadDefault();
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "Message prefix", "[FF0000]War Squire[-] : " },
                { "No permission", "You do not have permission to use this command." },
                { "Invalid args", "It looks like you made a mistake somewhere. Use /Plugin help for a list of available commands" },
                { "Invalid war", "Unable to find an active war with that participant." },

                { "Invalid player", "I could not find a player by the name of '{0}', my lord." },
                { "Invalid guild", "I could not find a guild by the name of '{0}', my lord." },

                { "On cooldown", "[00FF00]{0}[-] is still recovering from their last battle, my lord. We cannot attack them now." },
                

                { "Member change leave", "My lord, you cannot abandon your guild during a war!" },
                { "Member change banished", "My lord, you cannot banish a guild member during a war!" },
                { "Member change invited", "My lord, you cannot invite more guild members during a war!" },
                { "Member change joined", "My lord, I cannot allow you to join the guild during a war." },


                { "Declare occupied", "My lord, we're already in a war! We cannot afford to fight on two fronts!" },
                { "Declare cooldown", "My lord, we are still recovering from our last battle. Our men need some rest." },
                { "Declare offline", "My lord, we cannot attack people in their sleep, that would just be rude." },
                { "Declare self", "You can't declare war upon thyself, my Lord! This is crazy talk!" },
                { "Declare commence", "[FF0000]War Report[-] : [00FF00]{0}[-] has declared war on [00FF00]{1}[-]! You may aid either side by joining the war." },
                { "Declare join", "[FF0000]War Report[-] : [00FF00]{0}[-] has joined the war between [00FF00]{1}[-]." },
                { "Declare join separator", "[-], [00FF00]" },

                { "Commence preparation", "[FF0000]War Report[-] : They have [00FF00]{0}[-] to prepare for war!" },
                { "Commence war", "[FF0000]War Report[-] : [00FF00]{0}[-] have started the war!" },
                { "Commence war separator", "[-], [00FF00]" },
                { "End war", "[FF0000]War Report[-] : [00FF00]{0}[-] have ended their war!" },
                { "End war separator", "[-], [00FF00]" },
                { "End cooldown", "[FF0000]War Report[-] : [00FF00]{0}[-] have recovered from their battle!" },
                { "End cooldown separator", "[-], [00FF00]" },


                { "War report global", "[FF0000]War Report[-] : There are currently [00FF00]{0}[-] wars going on. Use /war report for a detailed list." },

                { "War report preparation title", "[0000FF]WAR REPORT - PREPARATIONS[-]" },
                { "War report preparation item", "[00FF00]{0}[-] are preparing for war. Time remaining: [00FF00]{1}[-]" },
                { "War report preparation item separator", "[-], [00FF00]" },

                { "War report war title", "[0000FF]WAR REPORT - WARS[-]" },
                { "War report war item", "[00FF00]{0}[-] are at war. Time remaining: [00FF00]{1}[-]" },
                { "War report war item separator", "[-], [00FF00]" },

                { "War report cooldown title", "[0000FF]WAR REPORT - COOLDOWN[-]" },
                { "War report cooldown item", "[00FF00]{0}[-] are recovering from their fight. Time remaining: [00FF00]{1}[-]" },
                { "War report cooldown item separator", "[-], [00FF00]" },

                { "War report none", "[FF0000]Report[-] : There are currently no active wars. The land is finally at peace once more." },

                
                { "Marks error", "An error occured during area modification. Please contact D-Kay to have it fixed." },
                { "Marks none", "There are no marks to remove." },
                { "Marks removed last", "The last added siege area marks have been removed." },
                { "Marks removed all", "All siege area marks have been removed." },
                { "Marks added first", "Added the first corner position for this siege area." },
                { "Marks added second", "Added the second and final position for this siege area." },


                { "Help title", "[0000FF]War Commands[-]" },
                { "Help war declare", "[00FF00]/war declare <guildname>[-] - Declare war against this guild or join in the war against them." },
                { "Help war declare player", "[00FF00]/war declare player <playername>[-] - Declare war against the guild of this player or join in the war against them." },
                { "Help war report", "[00FF00]/war report[-] - Get a report of all active wars." },
            }, this);
        }

        #endregion

        #region Commands

        [ChatCommand("war")]
        private void CmdWar(Player player, string cmd, string[] args)
        {
            if (!CheckArgs(player, args)) return;

            switch (args.First().ToLower())
            {
                case "declare":
                    CmdDeclare(player, args.Skip(1));
                    break;
                //case "force":
                //    CmdDeclare(player, args.Skip(1));
                //    break;
                //case "end":
                //    CmdEnd(player, args.Skip(1));
                //    break;
                case "report":
                    CmdReport(player);
                    break;
                case "marks":
                    CmdMarks(player, args.Skip(1));
                    break;
                case "help":
                    SendHelpText(player);
                    break;
                default:
                    SendError(player, "Invalid args");
                    break;
            }
        }

        #endregion

        #region Command Functions

        private void CmdDeclare(Player player, IEnumerable<string> args)
        {
            if (!CheckArgs(player, args)) return;

            var guild = player.GetGuild();
            if (Manager.IsInWar(guild.BaseID))
            {
                SendErrorTitle(player, "Declare occupied");
                return;
            }
            if (Manager.IsInCooldown(guild.BaseID))
            {
                SendErrorTitle(player, "Declare cooldown");
                return;
            }

            Guild targetGuild = null;
            if (args.First().EqualsIgnoreCase("player"))
            {
                args = args.Skip(1);
                if (!CheckArgs(player, args)) return;
                var target = Server.GetPlayerByName(args.JoinToString(" "));
                if (target == null)
                {
                    SendErrorTitle(player, "Invalid player", args.JoinToString(" "));
                    return;
                }

                targetGuild = target.GetGuild();
            }
            else
            {
                targetGuild = GetGuild(args.JoinToString(" "));
                if (targetGuild == null)
                {
                    SendErrorTitle(player, "Invalid guild", args.JoinToString(" "));
                    return;
                }
            }

            if (player.GetGuild().BaseID.Equals(targetGuild.BaseID))
            {
                SendErrorTitle(player, "Declare self");
                return;
            }

            if (!HasMemberOnline(targetGuild))
            {
                SendErrorTitle(player, "Declare offline");
                return;
            }

            if (Manager.IsInCooldown(targetGuild.BaseID))
            {
                SendErrorTitle(player, "On cooldown", targetGuild.Name);
                return;
            }

            if (Manager.IsInWar(targetGuild.BaseID))
            {
                var participants = Manager.GetWar(targetGuild.BaseID).Participants;
                Manager.JoinWar(player.GetGuild(), targetGuild);
                var msg = string.Join(GetMessage("Declare join separator"), participants.Select(p => p.Name).ToArray());
                SendMessage("Declare join", player.GetGuild().Name, msg);
                SavePluginData();
                return;
            }

            SendMessage("Declare commence", player.GetGuild().Name, targetGuild.Name);
            if (Config.TimePreparation.TotalSeconds > 0)
            {
                Manager.PrepareWar(player.GetGuild(), targetGuild);
                SendMessage("Commence preparation", Config.TimePreparation);
            }
            else
            {
                Manager.DeclareWar(player.GetGuild(), targetGuild);
                var participants = string.Join(GetMessage("Commence war separator"), new [] { player.GetGuild().Name, targetGuild.Name });
                SendMessage("Commence war", participants);
            }
            SavePluginData();
        }

        private void CmdForce(Player player, IEnumerable<string> args)
        {
            if (!CheckPermission(player, "modify")) return;

            SendMessage(player, "Ended war");
        }

        private void CmdEnd(Player player, IEnumerable<string> args)
        {
            if (!CheckPermission(player, "modify")) return;

            SendMessage(player, "Ended war");
        }

        private void CmdReport(Player player)
        {
            bool hadReport = false;
            if (Manager.Preparations.Any())
            {
                hadReport = true;
                SendMessage(player, "War report preparation title");
                foreach (var preparation in Manager.Preparations)
                {
                    var participants = string.Join(GetMessage("War report preparation item separator", player),
                        preparation.Participants.Select(p => p.Name).ToArray());
                    SendMessage(player, "War report preparation item", participants, preparation.GetRemainingTime());
                }
            }
            if (Manager.Wars.Any())
            {
                hadReport = true;
                SendMessage(player, "War report war title");
                foreach (var war in Manager.Wars)
                {
                    var participants = string.Join(GetMessage("War report war item separator", player),
                        war.Participants.Select(p => p.Name).ToArray());
                    SendMessage(player, "War report war item", participants, war.GetRemainingTime());
                }

            }
            if (Manager.Cooldowns.Any())
            {
                hadReport = true;
                SendMessage(player, "War report cooldown title");
                foreach (var cooldown in Manager.Cooldowns)
                {
                    var participants = string.Join(GetMessage("War report cooldown item separator", player),
                        cooldown.Participants.Select(p => p.Name).ToArray());
                    SendMessage(player, "War report cooldown item", participants, cooldown.GetRemainingTime());
                }
            }
            if (hadReport) return;

            SendMessage(player, "War report none");
        }

        private void CmdMarks(Player player, IEnumerable<string> args)
        {
            if (!CheckPermission(player, "modify")) return;
            if (!CheckArgs(player, args)) return;

            switch (args.First().ToLower())
            {
                case "add":
                    CmdAddMark(player);
                    break;
                case "remove":
                    CmdRemoveMarks(player, args.Skip(1));
                    break;
                default:
                    SendError(player, "Invalid args");
                    return;
            }
        }

        private void CmdAddMark(Player player)
        {
            switch (Manager.AddMark(player.Entity.Position))
            {
                case MarkResult.AddedFirst:
                    SendMessage(player, "Marks added first");
                    break;
                case MarkResult.AddedSecond:
                    SendMessage(player, "Marks added second");
                    break;
                default:
                    SendError(player, "Marks error");
                    return;
            }
            SavePluginData();
        }

        private void CmdRemoveMarks(Player player, IEnumerable<string> args)
        {
            if (!CheckArgs(player, args)) return;

            if (!Manager.HasMarks())
            {
                SendError(player, "Marks none");
                return;
            }

            switch (args.First().ToLower())
            {
                case "last":
                    Manager.RemoveLastMark();
                    SendMessage(player, "Marks removed all");
                    break;
                case "all":
                    Manager.RemoveAllMarks();
                    SendMessage(player, "Marks removed all");
                    break;
                default:
                    SendError(player, "Invalid args");
                    return;
            }
            
            SavePluginData();
        }

        #endregion

        #region System Functions

        private void WarReport()
        {
            if (Manager.WarCount() == 0) return;
            SendMessage("War report global", Manager.WarCount());
        }

        private void Interval()
        {
            var hasChanges = false;

            var preparations = Manager.GetFinishedPreparations();
            if (preparations.Any())
            {
                hasChanges = true;
                foreach (var preparation in preparations)
                {
                    var participants = string.Join(GetMessage("Commence war separator"),
                        preparation.Participants.Select(p => p.Name).ToArray());
                    SendMessage("Commence war", participants);
                }
            }

            var wars = Manager.GetFinishedWars();
            if (wars.Any())
            {
                hasChanges = true;
                foreach (var war in wars)
                {
                    var participants = string.Join(GetMessage("End war separator"),
                        war.Participants.Select(p => p.Name).ToArray());
                    SendMessage("End war", participants);
                }
            }

            var cooldowns = Manager.GetFinishedCooldowns();
            if (cooldowns.Any())
            {
                hasChanges = true;
                foreach (var cooldown in cooldowns)
                {
                    var participants = string.Join(GetMessage("End cooldown separator"),
                        cooldown.Participants.Select(p => p.Name).ToArray());
                    SendMessage("End cooldown", participants);
                }
            }

            if (hasChanges) SavePluginData();
        }


        private IEnumerable<Guild> GetAllGuilds()
        {
            var guildScheme = SocialAPI.Get<GuildScheme>();
            var groups = guildScheme.Storage.TryGetAllGroups();
            return groups.Select(g => guildScheme.TryGetGuild(g.BaseID)).Where(g => g != null && g.Members().MemberCount() > 0);
        }

        private Guild GetGuild(ulong guildid)
        {
            var scheme = SocialAPI.Get<GuildScheme>();
            return scheme.TryGetGuild(guildid);
        }

        private Guild GetGuildByMember(ulong memberId)
        {
            var scheme = SocialAPI.Get<GuildScheme>();
            return scheme.TryGetGuildByMember(memberId);
        }

        private Guild GetGuild(string guildName)
        {
            guildName = StripColor(guildName);
            var guilds = GetAllGuilds();
            var guild = guilds.FirstOrDefault(g => g.Name.EqualsIgnoreCase(guildName));
            return guild ?? guilds.FirstOrDefault(g => g.Name.ContainsIgnoreCase(guildName));
        }

        private Guild GetGuild(Vector3 position)
        {
            var crestScheme = SocialAPI.Get<CrestScheme>();
            var crest = crestScheme.GetCrestAt(position);
            if (crest == null) return null;
            var guildScheme = SocialAPI.Get<GuildScheme>();
            return guildScheme.TryGetGuild(crest.SocialId);
        }

        private bool HasMemberOnline(Guild guild)
        {
            foreach (var member in guild.Members().GetAllMembers())
            {
                if (Server.ClientPlayers.Any(p => p.Id.Equals(member.PlayerId))) return true;
            }
            return false;
        }

        private bool IsAnimal(Entity e)
        {
            return e.Has<MonsterEntity>() || e.Has<CritterEntity>();
        }

        private bool CanShootWeapon(Player player, Vector3 position)
        {
            var guild = GetGuild(position);
            return guild == null ||
                   player.GetGuild().BaseID.Equals(guild.BaseID);
        }

        private string StripColor(string value)
        {
            return Regex.Replace(value, @"\[[^][]*]", string.Empty);
        }

        #endregion

        #region API

        private bool IsAtWar(ulong guild1, ulong guild2)
        {
            return Manager.IsAtWar(guild1, guild2);
        }

        #endregion

        #region Hooks

        private object OnGuildAbandon(RemoveMembershipInfoEvent removeEvent)
        {
            var player = Server.GetPlayerById(removeEvent.PlayerId);
            var guild = player.GetGuild();
            if (!Manager.IsInWar(guild.BaseID)) return null;
            SendErrorTitle(player, "Member change leave");
            removeEvent.Cancel();
            return new object();
        }

        private object OnGuildBanish(BanishMemberEvent banishEvent)
        {
            var player = Server.GetPlayerById(banishEvent.VotingPlayer);
            var guild = player.GetGuild();
            if (!Manager.IsInWar(guild.BaseID)) return null;
            SendErrorTitle(player, "Member change banished");
            banishEvent.Cancel();
            return new object();
        }

        private object OnGuildInvite(GuildInvitationEvent invitationEvent)
        {
            var player = Server.GetPlayerById(invitationEvent.Inviter);
            var guild = GetGuild(invitationEvent.Information.GuildId);
            if (!Manager.IsInWar(guild.BaseID)) return null;
            SendErrorTitle(player, "Member change invited");
            invitationEvent.Cancel();
            return new object();
        }

        private object OnGuildInviteAccept(GuildInvitationAcceptEvent acceptEvent)
        {
            var player = Server.GetPlayerById(acceptEvent.PlayerId);
            var guild = GetGuild(acceptEvent.GroupId);
            if (!Manager.IsInWar(guild.BaseID)) return null;
            SendErrorTitle(player, "Member change joined");
            acceptEvent.Cancel();
            return new object();
        }
        

        private void OnCubeTakeDamage(CubeDamageEvent damageEvent)
        {
            #region Checks
            if (damageEvent?.Damage?.Damager == null) return;
            if (!damageEvent.Damage.Damager.name.Contains("Trebuchet") && 
                !damageEvent.Damage.Damager.name.Contains("Ballista")) return;
            #endregion

            if (damageEvent.Damage.DamageSource?.Owner == null || !damageEvent.Damage.DamageSource.IsPlayer)
            {
                damageEvent.Damage.Amount = 0f;
                damageEvent.Cancel();
                return;
            }

            var player = damageEvent.Damage.DamageSource.Owner;
            var position = damageEvent.Grid.LocalToWorldCoordinate(damageEvent.Position);
            if (Manager.IsSiegeArea(position)) return;

            var target = GetGuild(position);
            if (target == null) return;
            var guild = player.GetGuild();
            if (guild.BaseID.Equals(target.BaseID)) return;
            if (CanShootWeapon(player, position)) return;
            if (Manager.IsAtWar(guild.BaseID, target.BaseID)) return;

            damageEvent.Damage.Amount = 0f;
            damageEvent.Cancel();
        }

        private void OnEntityHealthChange(EntityDamageEvent damageEvent)
        {
            #region Null Checks
            if (damageEvent?.Damage?.Damager == null) return;
            if (!damageEvent.Damage.Damager.name.Contains("Trebuchet") && 
                !damageEvent.Damage.Damager.name.Contains("Ballista")) return;
            #endregion

            if (damageEvent.Damage.DamageSource?.Owner == null || !damageEvent.Damage.DamageSource.IsPlayer)
            {
                damageEvent.Damage.Amount = 0f;
                damageEvent.Cancel();
                return;
            }

            var player = damageEvent.Damage.DamageSource.Owner;
            var position = damageEvent.Entity.Position;
            if (Manager.IsSiegeArea(position)) return;
            if (damageEvent.Entity.IsPlayer) return;

            var target = GetGuild(position);
            if (target == null) return;
            var guild = player.GetGuild();
            if (guild.BaseID.Equals(target.BaseID)) return;
            if (CanShootWeapon(player, position)) return;
            if (Manager.IsAtWar(guild.BaseID, target.BaseID)) return;

            damageEvent.Damage.Amount = 0f;
            damageEvent.Cancel();
        }


        private void SendHelpText(Player player)
        {
            SendMessage(player, "Help title");
            SendMessage(player, "Help war declare");
            SendMessage(player, "Help war declare player");
            SendMessage(player, "Help war report");
        }

        #endregion

        #region Utility

        private void RegisterPermissions()
        {
            _permissions.Foreach(p => permission.RegisterPermission($"{Name}.{p}", this));
        }
        private bool CheckPermission(Player player, string permission)
        {
            if (HasPermission(player, permission)) return true;
            SendError(player, "No permission");
            return false;
        }
        private bool HasPermission(Player player, string permission)
        {
            return player.HasPermission($"{Name}.{permission}");
        }
        private bool CheckArgs(Player player, IEnumerable<string> args, int count = 1)
        {
            if (!args.Any())
            {
                SendError(player, "Invalid args");
                return false;
            }
            if (count <= 1) return true;

            if (args.Count() >= count) return true;
            SendError(player, "Invalid args");
            return false;
        }

        private void SendMessage(string key, params object[] obj) => Server.BroadcastMessage(GetMessage(key, obj));
        private void SendMessage(Player player, string key, params object[] obj) => player?.SendMessage(GetMessage(key, player, obj));
        private void SendError(Player player, string key, params object[] obj) => player?.SendError(GetMessage(key, player, obj));

        private void SendMessageTitle(string key, params object[] obj) => Server.BroadcastMessage(GetMessage(key, true, obj));
        private void SendMessageTitle(Player player, string key, params object[] obj) => player?.SendMessage(GetMessage(key, player, true, obj));
        private void SendErrorTitle(Player player, string key, params object[] obj) => player?.SendError(GetMessage(key, player, true, obj));

        public string GetMessage(string key, params object[] obj)
        {
            return GetMessage(key, null, obj);
        }
        public string GetMessage(string key, bool hasTitle, params object[] obj)
        {
            return GetMessage(key, null, hasTitle, obj);
        }
        public string GetMessage(string key, Player player, params object[] obj)
        {
            return GetMessage(key, player, false, obj);
        }
        public string GetMessage(string key, Player player, bool hasTitle, params object[] args)
        {
            var title = hasTitle ? lang.GetMessage("Message prefix", this, player?.Id.ToString()) : "";
            return string.Format(title + lang.GetMessage(key, this, player?.Id.ToString()), args);
        }

        private void Log(string msg) => LogFileUtil.LogTextToFile($"..\\oxide\\logs\\Plugin_{DateTime.Now:yyyy-MM-dd}.txt", $"[{DateTime.Now:h:mm:ss tt}] {msg}\r\n");

        #endregion
    }
}
