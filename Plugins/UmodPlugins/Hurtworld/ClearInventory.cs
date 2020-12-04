﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins {
    [Info ("Clear Inventory", "Mr. Blue", "2.0.4")]
    [Description ("Allows players to clear their own or another player's inventory")]
    class ClearInventory : HurtworldPlugin {
        private void Init () {
            permission.RegisterPermission ("clearinventory.self", this);
            permission.RegisterPermission ("clearinventory.other", this);
        }

        protected override void LoadDefaultMessages () {
            lang.RegisterMessages (new Dictionary<string, string> {
                ["ClearedInventory"] = "Cleared the inventory of '{name}'",
                ["ClearedInventoryTarget"] = "{name} cleared your inventory",
                ["PermissionDenied"] = "You don't have permission to use '/clear'",
                ["NoPlayerFound"] = "Can't find the player '{name}'",
                ["MultiplePlayersFound"] = "Found multiple players with '{name}': {names}"
            }, this);
            lang.RegisterMessages (new Dictionary<string, string> {
                ["ClearedInventory"] = "Inventory van '{name}' geleegd",
                ["ClearedInventoryTarget"] = "{name} heeft je inventory geleegd",
                ["PermissionDenied"] = "Je hebt geen bevoegdheden om '/clear' te gebruiken",
                ["NoPlayerFound"] = "Speler niet gevonden '{name}'",
                ["MultiplePlayersFound"] = "Meerdere spelers gevonden met '{name}': {names}"
            }, this, "nl");
        }

        private PlayerSession FindPlayer (string name, PlayerSession session) {
            List<PlayerSession> sessions = new List<PlayerSession> ();
            foreach (PlayerSession player in GameManager.Instance.GetSessions ().Values.ToList ())
                if (player.Identity.Name.ToLower ().Contains (name.ToLower ()))
                    sessions.Add (player);
            if (sessions.Count == 0)
                Player.Message (session, lang.GetMessage ("NoPlayerFound", this, session.SteamId.ToString ()).Replace ("{name}", name));
            else if (sessions.Count > 1)
                Player.Message (session, lang.GetMessage ("MultiplePlayersFound", this, session.SteamId.ToString ()).Replace ("{name}", name).Replace ("{names}", String.Join (", ", sessions.Select (o => o.Identity.Name).ToArray ())));
            else
                return sessions.First ();
            return null;
        }

        private void DoClearInventory (PlayerSession target, PlayerSession execute) {
            var pInventory = target.WorldPlayerEntity.GetComponent<Inventory> ();
            for (var i = 0; i < pInventory.Capacity; i++) pInventory.RemoveItem (i);
            pInventory.Invalidate ();
            Player.Message (execute, lang.GetMessage ("ClearedInventory", this, execute.SteamId.ToString ()).Replace ("{name}", target.Identity.Name));
        }

        [ChatCommand ("clear")]
        void ClearCommand (PlayerSession session, string command, string[] args) {
            string steamID = session.SteamId.ToString ();
            if (permission.UserHasPermission (steamID, "clearinventory.self") && args.Length == 0)
                DoClearInventory (session, session);
            else if (permission.UserHasPermission (steamID, "clearinventory.other")) {
                var target = FindPlayer (args[0], session);
                if (target != null) {
                    DoClearInventory (target, session);
                    Player.Message (target, lang.GetMessage ("ClearedInventoryTarget", this, target.SteamId.ToString ()).Replace ("{name}", session.Identity.Name));
                }
            } else
                Player.Message (session, lang.GetMessage ("PermissionDenied", this, steamID));
        }
    }
}