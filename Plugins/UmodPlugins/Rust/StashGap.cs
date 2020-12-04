﻿using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Stash Gap", "nivex", "2.0.1")]
    [Description("Bring balance to stashes.")]
    class StashGap : RustPlugin
    {
        [PluginReference] Plugin Clans, Friends;

        int constructLayer = LayerMask.GetMask("Construction", "Deployed");
        int stashLayer = LayerMask.GetMask("Default");
        string itemDropPrefab;

        void Init()
        {
            Unsubscribe(nameof(OnEntityBuilt));
            Unsubscribe(nameof(OnHammerHit));
        }

        void OnServerInitialized()
        {
            LoadVariables();
            itemDropPrefab = StringPool.Get(545786656);
            Subscribe(nameof(OnEntityBuilt));
            Subscribe(nameof(OnHammerHit));
        }

        void OnEntityBuilt(Planner planner, GameObject gameObject)
        {
            var player = planner?.GetOwnerPlayer();

            if (!player.IsValid())
            {
                return;
            }

            var entity = gameObject?.ToBaseEntity();

            if (!entity.IsValid())
            {
                return;
            }

            int hits = Physics.OverlapSphereNonAlloc(entity.transform.position, 3f, Vis.colBuffer, stashLayer, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits; i++)
            {
                var e = Vis.colBuffer[i].ToBaseEntity() as StashContainer;

                if (e != null && e.inventory.itemList.Count > 0 && IsAllied(e.OwnerID, player.userID))
                {
                    var position = player.transform.position + new Vector3(-0.1f, 0.25f, -0.1f);
                    e.inventory.Drop(itemDropPrefab, position, e.transform.rotation);
                    player.ChatMessage(msg("NotAllowed", player.UserIDString));
                }

                Vis.colBuffer[i] = null;
            }
        }

        void OnHammerHit(BasePlayer player, HitInfo info)
        {
            if (!player.IsAdmin || info?.HitEntity == null || !(info.HitEntity is StashContainer))
                return;

            var stash = (StashContainer)info.HitEntity;
            string contents = GetContents(stash);

            player.ChatMessage(msg("Owner", player.UserIDString, covalence.Players.FindPlayerById(stash.OwnerID.ToString())?.Name ?? msg("Unknown", player.UserIDString), stash.OwnerID));
            player.ChatMessage(msg("Contents", player.UserIDString, string.IsNullOrEmpty(contents) ? msg("NoInventory", player.UserIDString) : contents));
        }

        void cmdStashGap(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
                return;

            int drawn = 0;

            foreach (var stash in BaseNetworkable.serverEntities.OfType<StashContainer>())
            {
                double distance = Math.Round(Vector3.Distance(stash.transform.position, player.transform.position), 2);

                if (distance > drawDistance)
                    continue;

                string text = showDistantContents ? string.Format("S <color=orange>{0}</color> {1}", distance, GetContents(stash)) : string.Format("S <color=orange>{0}</color>", distance);

                player.SendConsoleCommand("ddraw.text", drawTime, Color.yellow, stash.transform.position, text);
                drawn++;
            }

            if (drawn > 0)
                player.ChatMessage(msg("Drawn", player.UserIDString, drawn, drawDistance));
            else
                player.ChatMessage(msg("None", player.UserIDString, drawDistance));
        }

        string GetContents(StashContainer stash)
        {
            var items = stash.inventory?.itemList?.Select(item => string.Format("{0} ({1})", item.info.displayName.translated, item.amount))?.ToArray() ?? new string[0];

            return items.Length > 0 ? string.Join(", ", items) : string.Empty;
        }

        public bool IsAllied(ulong playerId, ulong targetId)
        {
            return playerId == targetId || IsOnSameTeam(playerId, targetId) || IsInSameClan(playerId, targetId) || IsFriends(playerId, targetId) || IsAuthorizing(playerId, targetId) || IsBunked(playerId, targetId) || IsCodeAuthed(playerId, targetId) || IsInSameBase(playerId, targetId);
        }

        bool IsOnSameTeam(ulong playerId, ulong targetId)
        {
            RelationshipManager.PlayerTeam team1;
            if (!RelationshipManager.Instance.playerToTeam.TryGetValue(playerId, out team1))
            {
                return false;
            }

            RelationshipManager.PlayerTeam team2;
            if (!RelationshipManager.Instance.playerToTeam.TryGetValue(targetId, out team2))
            {
                return false;
            }

            return team1.teamID == team2.teamID;
        }

        private bool IsInSameClan(ulong playerId, ulong targetId)
        {
            var playerClan = Clans?.Call<string>("GetClanOf", playerId.ToString());
            var targetClan = Clans?.Call<string>("GetClanOf", targetId.ToString());

            return !string.IsNullOrEmpty(playerClan) && !string.IsNullOrEmpty(targetClan) && playerClan == targetClan;
        }

        private bool IsFriends(ulong playerId, ulong targetId)
        {
            return (Friends?.Call<bool>("AreFriends", playerId.ToString(), targetId.ToString()) ?? false);
        }

        private bool IsAuthorizing(ulong playerId, ulong targetId)
        {
            foreach (var priv in BaseNetworkable.serverEntities.OfType<BuildingPrivlidge>())
            {
                if (priv.authorizedPlayers.Any(x => x.userid == playerId) && priv.authorizedPlayers.Any(x => x.userid == targetId))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsBunked(ulong playerId, ulong targetId)
        {
            var t = SleepingBag.FindForPlayer(targetId, true);

            if (t.Length == 0)
            {
                return false;
            }

            var p = SleepingBag.FindForPlayer(playerId, true);

            if (p.Length == 0)
            {
                return false;
            }

            foreach (var a in p)
            {
                foreach (var b in t)
                {
                    if (Vector3Ex.Distance2D(a.transform.position, b.transform.position) <= 25f)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsCodeAuthed(ulong playerId, ulong targetId)
        {
            foreach (var codelock in BaseNetworkable.serverEntities.OfType<CodeLock>())
            {
                if (codelock.whitelistPlayers.Contains(playerId) && codelock.whitelistPlayers.Contains(targetId))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsInSameBase(ulong playerId, ulong targetId)
        {
            bool _sharesBase = false;
            var privs = BaseNetworkable.serverEntities.OfType<BuildingPrivlidge>();

            foreach (var priv in privs)
            {
                if (!priv.authorizedPlayers.Any(x => x.userid == playerId)) continue;
                int hits = Physics.OverlapSphereNonAlloc(priv.transform.position, 30f, Vis.colBuffer, constructLayer, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < hits; i++)
                {
                    var e = Vis.colBuffer[i].ToBaseEntity();

                    if (e != null && e.OwnerID == targetId)
                    {
                        _sharesBase = true;
                    }

                    Vis.colBuffer[i] = null;
                }

                if (_sharesBase)
                {
                    return true;
                }
            }

            foreach (var priv in privs)
            {
                if (!priv.authorizedPlayers.Any(x => x.userid == targetId)) continue;
                int hits = Physics.OverlapSphereNonAlloc(priv.transform.position, 30f, Vis.colBuffer, constructLayer, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < hits; i++)
                {
                    var e = Vis.colBuffer[i].ToBaseEntity();

                    if (e != null && e.OwnerID == playerId)
                    {
                        _sharesBase = true;
                    }

                    Vis.colBuffer[i] = null;
                }
            }

            return _sharesBase;
        }

        #region Config
        private bool Changed;
        string szChatCommand;
        float drawTime;
        float drawDistance;
        bool showDistantContents;

        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Unknown"] = "No owner",
                ["Owner"] = "<color=yellow>Owner</color>: {0} ({1})",
                ["Contents"] = "<color=yellow>Contents</color>: {0}",
                ["Drawn"] = "Showing <color=yellow>{0}</color> stashes within <color=orange>{1}m</color>",
                ["None"] = "No stashes within <color=orange>{0}m</color>.",
                ["NoInventory"] = "No inventory.",
                ["NotAllowed"] = "You are not allowed to put this object near a stash. The stash contents have been dropped at your feet.",
            }, this);
        }

        void LoadVariables()
        {
            base.LoadConfig();

            szChatCommand = Convert.ToString(GetConfig("Settings", "Command Name", "sg"));

            if (!string.IsNullOrEmpty(szChatCommand))
                cmd.AddChatCommand(szChatCommand, this, cmdStashGap);

            drawTime = Convert.ToSingle(GetConfig("Settings", "Draw Time", 30f));
            drawDistance = Convert.ToSingle(GetConfig("Settings", "Draw Distance", 500f));
            showDistantContents = Convert.ToBoolean(GetConfig("Settings", "Show Distant Contents", true));

            if (Changed)
            {
                SaveConfig();
                Changed = false;
            }
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

        string msg(string key, string id = null, params object[] args) => string.Format(id == null ? RemoveFormatting(lang.GetMessage(key, this, id)) : lang.GetMessage(key, this, id), args);
        string RemoveFormatting(string source) => source.Contains(">") ? System.Text.RegularExpressions.Regex.Replace(source, "<.*?>", string.Empty) : source;

        #endregion
    }
}