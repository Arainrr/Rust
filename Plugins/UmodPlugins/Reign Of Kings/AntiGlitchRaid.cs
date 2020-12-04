﻿using CodeHatch.Common;
using CodeHatch.Engine.Core.Cache;
using CodeHatch.Engine.Core.Interaction.Behaviours.Networking;
using CodeHatch.Engine.Modules.SocialSystem;
using CodeHatch.Engine.Networking;
using CodeHatch.Networking.Events.Entities;
using CodeHatch.Thrones.SocialSystem;
using Oxide.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Anti Glitch Raid", "D-Kay", "1.0.2")]
    [Description("Prevent players from raiding after logging in.")]
    public class AntiGlitchRaid : ReignOfKingsPlugin
    {
        #region Variables

        private HashSet<ulong> Blacklist { get; set; } = new HashSet<ulong>();

        #endregion

        #region Save and Load Data

        private void Init()
        {
            LoadBlacklist();
        }

        private void LoadBlacklist()
        {
            Blacklist = Interface.Oxide.DataFileSystem.ReadObject<HashSet<ulong>>($"{Name}_Blacklist");
        }

        private void SaveBlacklist()
        {
            Interface.Oxide.DataFileSystem.WriteObject($"{Name}_Blacklist", Blacklist);
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "Login notice 1", "To prevent people from glitching into bases, we kindly ask of you to perform the command `/unblock` while you're outside of a crest zone, or inside your own." },
                { "Login notice 2", "Until you do, you will not be able to damage or open chest or stations from other players." },

                { "Invalid location", "You can only perform this command outside of a crest area or on your own guild territory." },
                { "Not on blacklist", "You are already free to open and damage chests and stations of other players." },
                { "Removed from blacklist", "Thank you. You may now open and damage chests and stations of other players." },

                { "Blacklisted", "Sorry, you cannot damage nor open chests or stations from other players until you performed the command `/unblock` outside of a crest area or on your own guild territory." },
            }, this);
        }

        #endregion

        #region Commands

        [ChatCommand("unblock")]
        private void CmdUnblock(Player player, string cmd)
        {
            UnBlock(player);
        }

        #endregion

        #region Command Functions

        private void UnBlock(Player player)
        {
            if (!Blacklist.Contains(player.Id))
            {
                SendError(player, "Not on blacklist");
                return;
            }

            var guild = GetGuild(player.Entity.Position);
            if (guild != null && !guild.BaseID.Equals(player.GetGuild().BaseID))
            {
                SendError(player, "Invalid location");
                return;
            }

            Blacklist.Remove(player.Id);
            SaveBlacklist();

            SendMessage(player, "Removed from blacklist");
        }

        #endregion

        #region System Functions

        private Guild GetGuild(Vector3 position)
        {
            var crestScheme = SocialAPI.Get<CrestScheme>();
            var crest = crestScheme.GetCrestAt(position);
            if (crest == null) return null;
            var guildScheme = SocialAPI.Get<GuildScheme>();
            return guildScheme.TryGetGuild(crest.SocialId);
        }

        private bool IsAnimal(Entity e)
        {
            return e.Has<MonsterEntity>() || e.Has<CritterEntity>();
        }

        #endregion

        #region Hooks

        private void OnPlayerDisconnected(Player player)
        {
            #region Null Checks
            if (player == null) return;
            #endregion

            if (!Blacklist.Contains(player.Id)) return;

            Blacklist.Remove(player.Id);
            SaveBlacklist();
        }

        private void OnPlayerConnected(Player player)
        {
            #region Checks
            if (player == null) return;
            #endregion

            Blacklist.Add(player.Id);
            SaveBlacklist();

            player.ShowConfirmPopup("Alert", GetMessage("Login notice 1", player) + GetMessage("Login notice 2", player));
            SendMessage(player, "Login notice 1");
            SendMessage(player, "Login notice 2");
        }

        private void OnEntityHealthChange(EntityDamageEvent damageEvent)
        {
            #region Checks
            if (damageEvent?.Damage?.DamageSource == null) return;
            if (!damageEvent.Damage.DamageSource.IsPlayer) return;
            if (damageEvent.Entity == null) return;
            if (damageEvent.Entity.IsPlayer) return;
            if (damageEvent.Entity == damageEvent.Damage.DamageSource) return;
            if (IsAnimal(damageEvent.Entity)) return;
            #endregion

            var player = damageEvent.Damage.DamageSource.Owner;
            if (!Blacklist.Contains(player.Id)) return;

            var guild = GetGuild(damageEvent.Entity.Position);
            if (guild == null) return;
            if (guild.BaseID.Equals(player.GetGuild().BaseID)) return;

            damageEvent.Damage.Amount = 0f;
            damageEvent.Cancel();

            SendError(player, "Blacklisted");
        }

        private void OnPlayerInteract(InteractEvent interactEvent)
        {
            #region Checks
            if (interactEvent?.Sender == null) return;
            if (interactEvent.Sender.IsServer) return;
            if (interactEvent.Entity == null) return;
            #endregion

            var player = interactEvent.Sender;
            if (!Blacklist.Contains(player.Id)) return;

            var guild = GetGuild(interactEvent.Entity.Position);
            if (guild == null) return;
            if (guild.BaseID.Equals(player.GetGuild().BaseID)) return;

            interactEvent.Cancel();

            SendError(player, "Blacklisted");
        }

        #endregion

        #region Utility

        private void SendMessage(Player player, string key, params object[] obj) => player?.SendMessage(GetMessage(key, player, obj));
        private void SendError(Player player, string key, params object[] obj) => player?.SendError(GetMessage(key, player, obj));

        public string GetMessage(string key, Player player, params object[] args)
        {
            return string.Format(lang.GetMessage(key, this, player?.Id.ToString()), args);
        }

        #endregion
    }
}
