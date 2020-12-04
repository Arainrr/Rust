﻿using System;
using CodeHatch.Common;
using CodeHatch.Damaging;
using CodeHatch.Engine.Networking;
using CodeHatch.Networking.Events.Entities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("DuelsManager", "D-Kay & PierreA", "1.1.0")]
    public class DuelsManager : ReignOfKingsPlugin
    {
        #region Variables

        private int Interval { get; set; } = 10;
        private int InviteTime { get; set; } = 1;
        private int ActivityTime { get; set; } = 2;
        private HashSet<Duel> Duels { get; set; } = new HashSet<Duel>();

        public class Duel
        {
            public PlayerInfo Challenger { get; set; }
            public PlayerInfo Challenged { get; set; }
            public bool Accepted { get; set; }
            public TimeSpan RemainingTime { get; set; }

            public Duel() { }

            public Duel(Player challenger, Player challenged, int time = 1)
            {
                Challenger = new PlayerInfo(challenger);
                Challenged = new PlayerInfo(challenged);
                Accepted = false;
                RemainingTime = TimeSpan.FromMinutes(time);
            }

            public void Accept(int time = 10)
            {
                Accepted = true;
                RemainingTime = TimeSpan.FromMinutes(time);
            }

            public bool HasParticipant(Player player)
            {
                return Challenger.Equals(player) || Challenged.Equals(player);
            }

            public bool HasParticipants(Player player1, Player player2)
            {
                return Challenger.Equals(player1) && Challenged.Equals(player2) ||
                       Challenger.Equals(player2) && Challenged.Equals(player1);
            }

            public void ResetTimer(int time = 2)
            {
                RemainingTime = TimeSpan.FromMinutes(time);
            }

            public bool Update(int time = 10)
            {
                RemainingTime -= TimeSpan.FromSeconds(time);
                return RemainingTime <= TimeSpan.Zero;
            }
        }

        public class PlayerInfo
        {
            public ulong Id { get; set; }
            public string Name { get; set; }

            public PlayerInfo() { }

            public PlayerInfo(ulong id, string name)
            {
                this.Id = id;
                this.Name = name;
            }

            public PlayerInfo(Player player) : this(player.Id, player.Name) { }

            public override bool Equals(object obj)
            {
                var playerInfo = obj as PlayerInfo;
                if (playerInfo != null) return this.Id.Equals(playerInfo.Id);
                var player = obj as Player;
                if (player != null) return this.Id.Equals(player.Id);
                return false;
            }

            public override string ToString()
            {
                return Name;
            }
        }

        #endregion

        #region Save and Load Data

        private void Loaded()
        {
            timer.Repeat(Interval, 0, UpdateDuels);
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "IncorrectCmd", "Syntaxe incorrecte: /defier (pseudoJoueur)" },
                { "UnknownPlayer", "Joueur inconnu ou offline" },
                { "NoSelfDuel", "Vous ne pouvez pas vous défier vous-même..." },
                { "MaxRange", "Votre adversaire doit se trouver à 15m de vous au maximum." },
                { "AlreadyInDuelWith", "Vous êtes déjà en duel avec {0}." },
                { "AlreadyAsk", "Vous avez déjà demandé un duel à {0}." },
                { "DuelStarted", "Un défi à débuté entre [0080FF]{0}[FFFFFF] et [0080FF]{1}[FFFFFF]!" },
                { "DuelReceived", "{0} vous a lancé un défi. Tapez /defier (pseudoJoueur) pour l'accepter" },
                { "DuelSentTo", "Défi envoyé à {0}." },
                { "DuelExpired", "Le duel entre [0080FF]{0}[FFFFFF] et [0080FF]{1}[FFFFFF] a pris fin en raison de l'interférence." },
                { "WonADuel", "[0080FF]{0}[FFFFFF] a gagné un duel contre [0080FF]{1}[FFFFFF]!" },
                { "HelpTitle", "[0000FF]Duel Commande[FFFFFF]" },
                { "HelpCmd", "[00FF00]/defier (pseudoJoueur)[FFFFFF] - Envoyez une demande de duel à un joueur ou acceptez une demande de duel d'un autre joueur." }
            }, this, "fr");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "IncorrectCmd", "Incorrect command: /duel (playername)" },
                { "UnknownPlayer", "Player offline or unknown." },
                { "NoSelfDuel", "You cannot duel yourself..." },
                { "MaxRange", "Max duel range is 15m." },
                { "AlreadyInDuelWith", "You are already in a duel with {0}." },
                { "AlreadyAsk", "You already send a duel request to {0}." },
                { "DuelStarted", "Duel started between [0080FF]{0}[FFFFFF] and [0080FF]{1}[FFFFFF]!" },
                { "DuelReceived", "{0} sent you a duel request. Type /duel (playername) to accept it." },
                { "DuelSentTo", "Duel request sent to {0}." },
                { "DuelExpired", "The duel between [0080FF]{0}[FFFFFF] and [0080FF]{1}[FFFFFF] has ended due to inactivity." },
                { "DuelPostponed", "The duel between [0080FF]{0}[FFFFFF] and [0080FF]{1}[FFFFFF] has ended due to interference." },
                { "WonADuel", "[0080FF]{0}[FFFFFF] won a duel against [0080FF]{1}[FFFFFF]!" },
                { "HelpTitle", "[0000FF]Duel Commands[FFFFFF]" },
                { "HelpCmd", "[00FF00]/duel (playername)[FFFFFF] - Send a duel request to a player or accept a duel request from another player." }
            }, this, "en");
        }

        #endregion

        #region Commands

        [ChatCommand("defier")]
        private void CmdDefier(Player player, string cmd, string[] args)
        {
            SendDuel(player, args);
        }

        [ChatCommand("defy")]
        private void CmdDefy(Player player, string cmd, string[] args)
        {
            SendDuel(player, args);
        }

        [ChatCommand("duel")]
        private void CmdDuel(Player player, string cmd, string[] args)
        {
            SendDuel(player, args);
        }

        #endregion

        #region Functions

        private void SendDuel(Player player, string[] args)
        {
            if (!args.Any()) { SendError(player, "IncorrectCmd"); return; }

            var target = Server.GetPlayerByName(args.JoinToString(" "));
            if (target == null) { SendError(player, "UnknownPlayer"); return; }

            if (player.Id == target.Id) { SendError(player, "NoSelfDuel"); return; }
            if (GetDistance(player.Entity.Position, target.Entity.Position) > 15f) { SendError(player, "MaxRange"); return; }

            var duel = Duels.FirstOrDefault(d => d.HasParticipants(player, target));
            if (duel == null)
            {
                Duels.Add(new Duel(player, target, InviteTime));

                SendMessage(player, "DuelSentTo", target.Name);
                SendMessage(target, "DuelReceived", player.Name);
                return;
            }

            if (duel.Accepted) { SendError(player, "AlreadyInDuelWith", target.Name); return; }

            if (duel.Challenger.Equals(player)) { SendError(player, "AlreadyAsk"); return; }

            SendMessage("DuelStarted", player.Name, target.Name);
            duel.Accept(ActivityTime);
        }

        private void UpdateDuels()
        {
            try
            {
                foreach (var duel in Duels.ToList())
                {
                    if (!duel.Update(Interval)) continue;
                    SendMessage("DuelExpired", duel.Challenger, duel.Challenged);
                    Duels.Remove(duel);
                }
            }
            catch
            {

            }
        }

        private void CancelDuels(params Player[] players)
        {
            foreach (var player in players)
            {
                var duels = Duels.Where(d => d.HasParticipant(player));
                if (!duels.Any()) continue;

                foreach (var d in duels)
                {
                    SendMessage("DuelPostponed", d.Challenger, d.Challenged);
                    Duels.Remove(d);
                }
            }
        }

        private bool isKillingDamage(Player victim, Damage damage)
        {
            var willBeKilled = false;
            var humanBodyBones = damage.HitBoxBone;
            var health = victim.GetHealth();
            if (health.TorsoHealth.Bones.Contains(humanBodyBones))
            {
                if (health.TorsoHealth.CurrentHealth - damage.Amount < 1)
                {
                    willBeKilled = true;
                }
            }
            else if (health.HeadHealth.Bones.Contains(humanBodyBones))
            {
                if (health.HeadHealth.CurrentHealth - damage.Amount < 1)
                {
                    willBeKilled = true;
                }
            }
            else if (health.LegsHealth.Bones.Contains(humanBodyBones))
            {
                if (health.TorsoHealth.CurrentHealth + health.LegsHealth.CurrentHealth - damage.Amount < 1)
                {
                    willBeKilled = true;
                }
            }
            else
            {
                var num = health.HeadHealth.MaxHealth + health.TorsoHealth.MaxHealth + health.LegsHealth.MaxHealth;
                var torsoHealtAfter = health.HeadHealth.CurrentHealth - damage.Amount * (health.HeadHealth.MaxHealth / num);
                var headHealtAfter = health.TorsoHealth.CurrentHealth -
                                       damage.Amount * (health.TorsoHealth.MaxHealth / num);
                if (torsoHealtAfter < 1 || headHealtAfter < 1) willBeKilled = true;
            }
            return willBeKilled;
        }

        private float GetDistance(Vector3 pos1, Vector3 pos2)
        {
            return Vector3.Distance(pos1, pos2);
        }

        #endregion

        #region API

        private bool IsInDuel(Player player1, Player player2)
        {
            return Duels.Any(x => x.HasParticipants(player1, player2) && x.Accepted);
        }

        #endregion

        #region Hooks

        private void OnEntityHealthChange(EntityDamageEvent e)
        {
            #region Checks
            if (e == null) return;
            if (e.Cancelled) return;
            if (e.Damage == null) return;
            if (e.Damage.DamageSource == null) return;
            if (!e.Damage.DamageSource.IsPlayer) return;
            if (e.Entity == null) return;
            if (!e.Entity.IsPlayer) return;
            if (e.Entity == e.Damage.DamageSource) return;
            #endregion

            var player = e.Damage.DamageSource.Owner;
            var victim = e.Entity.Owner;
            if (player == null) return;
            if (victim == null) return;

            var duel = Duels.FirstOrDefault(d => d.HasParticipants(player, victim));
            if (duel == null)
            {
                CancelDuels(player, victim);
                return;
            }

            if (!duel.Accepted) return;
            if (!isKillingDamage(victim, e.Damage))
            {
                duel.ResetTimer(ActivityTime);
                return;
            }

            SendMessage("WonADuel", player.Name, victim.Name);

            player.Heal(-1f);
            victim.Heal(-1f);
            Duels.Remove(duel);
            e.Damage.Amount = 0f;
            e.Cancel();
        }

        private void SendHelpText(Player player)
        {
            player.SendMessage(GetMessage("HelpTitle", player));
            player.SendMessage(GetMessage("HelpCmd", player));
        }

        #endregion

        #region Utility

        private void SendMessage(string key, params object[] obj) => PrintToChat(GetMessage(key), obj);
        private void SendMessage(Player player, string key, params object[] obj) => player.SendMessage(GetMessage(key, player), obj);
        private void SendError(Player player, string key, params object[] obj) => player.SendError(GetMessage(key, player), obj);

        private string GetMessage(string key, Player player = null) => lang.GetMessage(key, this, player?.Id.ToString());

        #endregion
    }
}