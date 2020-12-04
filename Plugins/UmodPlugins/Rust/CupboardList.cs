﻿using System.Collections.Generic;
using System.Text;
using Oxide.Core;
using UnityEngine;

/*Thx to Kappasaurus the creator of this plugin upto v1.0.2*/

namespace Oxide.Plugins
{
    [Info("CupboardList", "Krungh Crow", "1.0.3")]

    class CupboardList : RustPlugin
    {
        private const string Prefab = "cupboard.tool.deployed";
        const string prefix = "<color=yellow>[Cupboard List]</color> ";
        const ulong chaticon = 76561199090290915;//steamprofile for the image
        private string msg(string key, string id = null) => lang.GetMessage(key, this, id);

        void Init()
        {
            permission.RegisterPermission("cupboardlist.able", this);
            LoadConfig();
        }

        [ChatCommand("cupauth")]
        void AuthCmd(BasePlayer player, string commanmd, string[] args)
        {
            if (!permission.UserHasPermission(player.userID.ToString(), "cupboardlist.able"))
            {
                Player.Message(player, prefix + string.Format(msg("No Permission", player.UserIDString)), chaticon);
                return;
            }

            var targetEntity = GetViewEntity(player);

            if (!IsCupboardEntity(targetEntity))
            {
                Player.Message(player, prefix + string.Format(msg("Not a Cupboard", player.UserIDString)), chaticon);
                return;
            }

            var cupboard = targetEntity.gameObject.GetComponentInParent<BuildingPrivlidge>();

            if (cupboard.authorizedPlayers.Count == 0)
            {
                Player.Message(player, prefix + string.Format(msg("No Players", player.UserIDString)), chaticon);
                return;
            }

            var output = new List<string>();

            foreach (ProtoBuf.PlayerNameID playerNameOrID in cupboard.authorizedPlayers)
                output.Add($"<color=green>{playerNameOrID.username}</color> ({playerNameOrID.userid})"); 

            Player.Message(player, prefix + string.Format(msg("Player List", player.UserIDString).Replace("{authList}", output.ToSentence())), chaticon);

        }

        [ChatCommand("cupowner")]
        void OwnerCmd(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.userID.ToString(), "cupboardlist.able"))
            {
                Player.Message(player, prefix + string.Format(msg("No Permission", player.UserIDString)), chaticon);
                return;
            }

            var targetEntity = GetViewEntity(player);

            if (!IsCupboardEntity(targetEntity))
            {
                Player.Message(player, prefix + string.Format(msg("Not a Cupboard", player.UserIDString)), chaticon);
                return;
            }

            var cupboard = targetEntity.gameObject.GetComponentInParent<BuildingPrivlidge>();
            var owner = covalence.Players.FindPlayerById(cupboard.OwnerID.ToString());

            Player.Message(player, prefix + string.Format(msg("Owner", player.UserIDString).Replace("{player}", $"\n<color=green>{owner.Name}</color> ({owner.Id})")), chaticon);
        }

        #region Helpers

        private BaseEntity GetViewEntity(BasePlayer player)
        {
            RaycastHit hit;
            bool didHit = Physics.Raycast(player.eyes.HeadRay(), out hit, 5);

            return didHit ? hit.GetEntity() : null;
        }

        private bool IsCupboardEntity(BaseEntity entity) => entity != null && entity.ShortPrefabName == Prefab;

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["No Permission"] = "Sorry, no permission.",
                ["Not a Cupboard"] = "Sorry, that's not a cupboard.",
                ["No Players"] = "Sorry, no players authorized.",
                ["Player List"] = "The following player(s) are authorized: {authList}.",
                ["Owner"] = "Tool cupboard owner: {player}."
            }, this);
        }
        #endregion
    }
}