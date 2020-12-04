﻿using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins {
    [Info ("No Build Zones", "Mr. Blue", "0.0.2")]
    [Description ("Blocks placing of items and constructions in defined cells")]
    class NoBuildZones : HurtworldPlugin {
        private List<int> protectedcells = new List<int> ();

        private const string perm = "nobuildzones.admin";

        void Init () {
            LoadCells ();
            permission.RegisterPermission (perm, this);
        }

        protected override void LoadDefaultMessages () {
            lang.RegisterMessages(new Dictionary<string, string> {
                    { "NBZUsage", "<color=orange>[NBZ]</color> > Usage: /NBZ add|remove|check." },
                    { "NBZAdded", "<color=orange>[NBZ]</color> > No Build Zone added on this cell." },
                    { "HasNBZ", "<color=orange>[NBZ]</color> > This cell is a No Build Zone." },
                    { "NBZExists", "<color=orange>[NBZ]</color> > No Build Zone already exists in this cell." },
                    { "NBZRemoved", "<color=orange>[NBZ]</color> > No Build Zone removed from this cell." },
                    { "NoNBZ", "<color=orange>[NBZ]</color> > No No Build Zone set in this cell." },
                    { "NBZAlert", "<color=orange>NO BUILD ZONE!</color>" },
                    { "NoPermission", "<color=orange>[NBZ]</color> > You do not have the permissions to use this command. (Perm: {perm})" }
                }, this);
        }

        string Msg (string msg, string SteamId = null) => lang.GetMessage (msg, this, SteamId);

        object OnEntityDeploy (EquipEventData data) {
            Vector3 pos = data.Session.Handler.RefCache.PlayerConstructionManager.ServerConstructionPrefab.Mover.GetGameObject ().transform.position;
            var session = data.Session.Handler.RefCache.InteractServer.OwnerIdentity.ConnectedSession;
            int cell = ConstructionUtilities.GetOwnershipCell (pos);
            if (protectedcells.Contains (cell)) {
                AlertManager.Instance.GenericTextNotificationServer (Msg ("NBZAlert", session.SteamId.ToString ()), session.Player);
                return true;
            }

            return null;
        }

        #region DataHandling
        void LoadCells () {
            protectedcells = Interface.Oxide.DataFileSystem.ReadObject<List<int>> (Name);
        }
        void SaveCells () {
            Interface.Oxide.DataFileSystem.WriteObject (Name, protectedcells);
        }
        #endregion

        #region ChatCommand
        [ChatCommand ("NBZ")]
        void NBZCommand (PlayerSession session, string command, string[] args) {
            if (!permission.UserHasPermission (session.SteamId.ToString (), perm)) {
                Player.Message (session, Msg ("NoPermission", session.SteamId.ToString ()).Replace ("{perm}", perm));
                return;
            }

            Vector3 pos = session.WorldPlayerEntity.transform.position;
            int cell = ConstructionUtilities.GetOwnershipCell (pos);
            if (args.Length == 1) {
                switch (args[0].ToLower()) {
                    case "add":
                        if (protectedcells.Contains (cell))
                            Player.Message (session, Msg ("NBZExists", session.SteamId.ToString ()));
                        else {
                            protectedcells.Add (cell);
                            Player.Message (session, Msg ("NBZAdded", session.SteamId.ToString ()));
                            SaveCells ();
                        }
                        break;

                    case "remove":
                        if (!protectedcells.Contains (cell))
                            Player.Message (session, Msg ("NoNBZ", session.SteamId.ToString ()));
                        else {
                            protectedcells.Remove (cell);
                            Player.Message (session, Msg ("NBZRemoved", session.SteamId.ToString ()));
                            SaveCells ();
                        }
                        break;
                    case "check":
                        if (protectedcells.Contains (cell))
                            Player.Message (session, Msg ("HasNBZ", session.SteamId.ToString ()));
                        else
                            Player.Message (session, Msg ("NoNBZ", session.SteamId.ToString ()));
                        break;
                    default:
                        Player.Message (session, Msg ("NBZUsage", session.SteamId.ToString ()));
                        break;
                }
            } else {
                Player.Message (session, Msg ("NBZUsage", session.SteamId.ToString ()));
            }
        }
        #endregion
    }
}