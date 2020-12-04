﻿using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins {
    [Info ("Remove Claim", "Mr. Blue", "2.0.2")]
    [Description ("Removes the claim of the vehicle player is driving")]
    class RemoveClaim : HurtworldPlugin {
        #region Loading
        void Init () {
            permission.RegisterPermission ("removeclaim.use", this);
        }

        protected override void LoadDefaultMessages () {
            lang.RegisterMessages (new Dictionary<string, string> {
                { "NoPermission", "You don't have permission to use this command ({perm})" },
                { "NoClaim", "Vehicle is not claimed." },
                { "NoCar", "You have to be in the drivers seat to execute this command." },
                { "RemovedClaim", "Claim was removed." },
                { "RemoveFail", "Removing claim failed." }
            }, this);
        }
        #endregion

        #region Commands
        [ChatCommand ("rc")]
        void RCCMD (PlayerSession s, string c, string[] a) => RemoveClaimCMD (s, c, a);

        [ChatCommand ("removeclaim")]
        void RemoveClaimCMD (PlayerSession session, string command, string[] args) {
            if (!permission.UserHasPermission (session.SteamId.ToString (), "removeclaim.use")) {
                Player.Message (session, GetMsg ("NoPermission", session.SteamId.ToString ()).Replace ("{perm}", "removeclaim.use"));
            }
            else {
                VehicleOwnershipManager vm = Singleton<VehicleOwnershipManager>.Instance;
                VehicleOwnership[] VOs = Resources.FindObjectsOfTypeAll<VehicleOwnership> ();
                foreach (VehicleOwnership v in VOs) {
                    if (v == null) continue;
                    VehicleStatManager sm = v.StatManager;
                    if (sm == null) continue;
                    GameObject driver = sm.GetDriver ();
                    if (driver == null) continue;
                    HNetworkView nwv = driver.GetComponent<HNetworkView> ();
                    if (nwv == null) continue;
                    PlayerSession ses = GameManager.Instance.GetSession (nwv.Owner);
                    if (ses == null) continue;
                    if (ses.SteamId == session.SteamId) {
                        if (vm.GetClaimant (v) == "") {
                            Player.Message (session, GetMsg ("NoClaim", session.SteamId.ToString ()));
                        } else {
                            vm.Unclaim (v);
                            Player.Message (session, GetMsg ("RemovedClaim", session.SteamId.ToString ()));
                        }
                        return;
                    }
                }
                Player.Message (session, GetMsg ("NoCar", session.SteamId.ToString ()));
            }
        }
        #endregion
        #region Helpers
        string GetMsg (string key, string session) => lang.GetMessage (key, this, session);
        #endregion
    }
}