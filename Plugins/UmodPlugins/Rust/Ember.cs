using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;

namespace Oxide.Plugins {
	[Info ("Ember", "Mkekala", "1.1.0")]
	[Description ("Integrates Ember store & ban management with Rust")]
	public class Ember : RustPlugin {
		#region Configuration
		private class PluginConfig {
			public string Host = "http://127.0.0.1";
			public string Token;
			public string BanScope = "server";
			public bool BanLog = true;
			public int PollingInterval = 300;
			public Dictionary<string, bool> RoleSync = new Dictionary<string, bool> () {
				{ "Get", true },
				{ "Post", true }
			};
		}

		private PluginConfig config;
		private Dictionary<string, string> headers;

		protected override void LoadDefaultConfig () {
			Config.WriteObject (new PluginConfig (), true);
		}
		#endregion

		#region Variables
		private Dictionary<String, JToken> userData;
		private List<String> usersToPost;
		private List<String> usersProcessed;
		#endregion

		#region Methods
		void QueueUserToPost (string steamid) {
			if (!usersToPost.Contains (steamid)) {
				usersToPost.Add (steamid);
			}
		}

		void PostUsers (List<string> users) {
			if (users.Count == 0) {
				return;
			}

			Puts ($"Posting {users.Count} user(s)");

			webrequest.Enqueue (config.Host + "/api/server/users?banScope=" + config.BanScope, "steamids=" + string.Join (",", users), (code, response) => {
				if (code != 200 || response == null) {
					Puts ($"Request failed. Response: {response}");
					return;
				}

				JObject json = JObject.Parse (response);

				foreach (var userobj in (JObject) json["users"]) {
					userData[userobj.Key] = userobj.Value;
					BasePlayer player = GetPlayerBySteamID (userobj.Key);
					if (player != null) {
						ProcessUser (player);
					}
				}

			}, this, RequestMethod.POST, headers);
		}

		void ProcessUser (BasePlayer player) {
			string steamid = player.UserIDString;
			JToken userjson = userData[steamid];
			string name = player.displayName;

			Puts ($"Processing user data for {name} ({steamid})");
			bool banned = (bool) userjson["banned"];

			if (banned) {
				player.Kick (lang.GetMessage ("Banned", this, player.UserIDString));
				userData.Remove (steamid);
				return;
			}

			foreach (var package in userjson["expiring_packages"]) {
				SendReply (player, string.Format (lang.GetMessage ("PackageExpired", this, player.UserIDString), package["store_package"]["name"]));
				string groupToRevoke = (string) package["store_package"]["role"];
				if (groupToRevoke != null) {
					Puts ($"Revoking the \"{groupToRevoke}\" group from {name} ({steamid})");
					permission.RemoveUserGroup (steamid, groupToRevoke);
					SendReply (player, string.Format (lang.GetMessage ("GroupRevoked", this, player.UserIDString), groupToRevoke));
				}
				RunCommands (package["store_package"]["expiry_commands"], steamid, name, (string) package["store_package"]["name"], (string) package["store_package"]["cost"]);
			}

			foreach (var package in userjson["unredeemed_packages"]) {
				SendReply (player, string.Format (lang.GetMessage ("RedeemingPackage", this, player.UserIDString), package["store_package"]["name"]));
				string group = (string) package["store_package"]["role"];
				if (group != null) {
					if (!permission.GroupExists (group)) {
						Puts ($"Creating group \"{group}\"");
						permission.CreateGroup (group, group, 0);
					}
					Puts ($"Granting the \"{group}\" group to {name} ({steamid})");
					permission.AddUserGroup (steamid, group);
					SendReply (player, string.Format (lang.GetMessage ("GroupGranted", this, player.UserIDString), group));
				}
				RunCommands (package["store_package"]["commands"], steamid, name, (string) package["store_package"]["name"], (string) package["store_package"]["cost"]);
			}

			if (config.RoleSync["Get"] == true || config.RoleSync["Post"] == true) {
				Puts ($"Syncing roles for {name} ({steamid})");

				if (config.RoleSync["Get"] == true) {
					foreach (string role in userjson["roles"]) {
						if (!permission.UserHasGroup (steamid, role)) {
							if (!permission.GroupExists (role)) {
								Puts ($"Creating group \"{role}\"");
								permission.CreateGroup (role, role, 0);
							}
							Puts ($"Granting the \"{role}\" group to {name} ({steamid})");
							permission.AddUserGroup (steamid, role);
							SendReply (player, string.Format (lang.GetMessage ("GroupGranted", this, player.UserIDString), role));
						}
					}
					foreach (string role in userjson["revoked_roles"]) {
						if (permission.UserHasGroup (steamid, role)) {
							Puts ($"Revoking the \"{role}\" group from {name} ({steamid})");
							permission.RemoveUserGroup (steamid, role);
							SendReply (player, string.Format (lang.GetMessage ("GroupRevoked", this, player.UserIDString), role));
						}
					}
				}

				if (config.RoleSync["Post"] == true) {
					string[] roles = userjson["roles"].ToObject<string[]> ();
					foreach (string group in permission.GetGroups ()) {
						if (permission.UserHasGroup (steamid, group)) {
							if (Array.IndexOf (roles, group) == -1) {
								PostRole (steamid, group);
							}
						}
					}
				}
			}

			usersProcessed.Add (steamid);
			userData.Remove (steamid);
		}

		void PostUsersProcessed (List<String> users) {
			if (users.Count == 0) {
				return;
			}

			webrequest.Enqueue (config.Host + "/api/server/usersprocessed", "steamids=" + string.Join (",", users), (code, response) => {
				if (code != 200 || response == null) {
					Puts ($"Request failed. Response: {response}");
					return;
				}
			}, this, RequestMethod.POST, headers);
		}

		private void PollUsers () {
			if (config.PollingInterval > 0) {
				string steamids = "";
				foreach (BasePlayer ply in BasePlayer.activePlayerList) {
					if (!usersToPost.Contains (ply.UserIDString) && !usersProcessed.Contains (ply.UserIDString) && !userData.ContainsKey(ply.UserIDString))
						steamids += ply.UserIDString + ",";
				}

				if (!string.IsNullOrEmpty (steamids)) {
					webrequest.Enqueue (config.Host + "/api/server/users/poll?banScope=" + config.BanScope + "&steamids=" + steamids, null, (code, response) => {
						if (code != 200 || response == null) {
							Puts ($"Connection failed. Response: {response}");
							return;
						}

						JObject json = JObject.Parse (response);
						string status = (string) json["status"];

						if (status == "success") {
							foreach (string steamid in json["users"]) {
								QueueUserToPost (steamid);
							}
						}
					}, this, RequestMethod.GET, headers);
				}
			}
		}

		void Ban (string offenderSteamid, string expiryMinutes, string reason, bool global, string adminSteamid, BasePlayer caller) {
			var globalStr = "";
			if (global == true) {
				globalStr = "global=true&";
			}
			var adminStr = "";
			if (adminSteamid != null) {
				adminStr = "&admin_steamid=" + adminSteamid;
			}
			webrequest.Enqueue (config.Host + "/api/server/users/" + offenderSteamid + "/bans", globalStr + "expiry_minutes=" + expiryMinutes + "&reason=" + reason + adminStr, (code, response) => {
				if (code != 200 || response == null) {
					Puts ($"Failed to ban user. Response: {response}");
					return;
				}

				Puts ($"Player banned: {response}");

				var json = JObject.Parse (response);
				var status = (string) json["status"];

				if (status == "success") {
					if (caller != null) {
						SendReply (caller, lang.GetMessage ("PlayerBanned", this, caller.UserIDString));
					}
				}
			}, this, RequestMethod.POST, headers);
		}

		void Unban (string offenderSteamid, BasePlayer caller) {
			webrequest.Enqueue (config.Host + "/api/server/users/" + offenderSteamid + "/bans", null, (code, response) => {
				if (code != 200 || response == null) {
					Puts ($"Failed to unban user. Response: {response}");
					return;
				}

				Puts ($"Player unbanned: {response}");

				var json = JObject.Parse (response);
				var status = (string) json["status"];

				if (status == "success") {
					if (caller != null) {
						SendReply (caller, lang.GetMessage ("PlayerUnbanned", this, caller.UserIDString));
					}
				}
			}, this, RequestMethod.DELETE, headers);
		}

		void PostRole (string steamid, string role) {
			if (role != "default") {
				webrequest.Enqueue (config.Host + "/api/server/users/" + steamid + "/roles", "role=" + role, (code, response) => {
					if (code != 200 || response == null) {
						Puts ($"Failed to add role \"{role}\". Response: {response}");
						return;
					}
					Puts ($"Role \"{role}\" added. Response: {response}");
				}, this, RequestMethod.POST, headers);
			}
		}

		void DeleteRole (string steamid, string role) {
			webrequest.Enqueue (config.Host + "/api/server/users/" + steamid + "/roles/" + role, null, (code, response) => {
				if (code != 200 || response == null) {
					Puts ($"Failed to revoke role \"{role}\". Response: {response}");
					return;
				}
				Puts ($"Role \"{role}\" revoked. Response: {response}");
			}, this, RequestMethod.DELETE, headers);
		}

		void RunCommands (JToken commands, string steamid, string name, string packageName, string packageCost) {
			foreach (string command in commands) {
				rust.RunServerCommand (command.Replace ("{ply.steamid}", steamid).Replace ("{ply.name}", name).Replace ("{package.name}", packageName).Replace ("{package.cost}", packageCost));
			}
		}

		static List<BasePlayer> GetPlayersByName (string name) {
			List<BasePlayer> matches = new List<BasePlayer> ();
			foreach (BasePlayer ply in BasePlayer.activePlayerList) {
				if (ply.displayName.ToLower ().Contains (name.ToLower ())) {
					matches.Add (ply);
				}
			}
			if (matches.Count () > 0) {
				return matches;
			}
			return null;
		}

		static BasePlayer GetPlayerBySteamID (string steamid) {
			foreach (BasePlayer ply in BasePlayer.activePlayerList) {
				if (ply.UserIDString == steamid)
					return ply;
			}
			return null;
		}
		#endregion

		#region Hooks
		private void Init () {
			config = Config.ReadObject<PluginConfig> ();
			Config.WriteObject (config);
			headers = new Dictionary<string, string> { { "Authorization", "Bearer " + config.Token }, { "Accept", "application/json" } };

			permission.RegisterPermission (this.Title.ToLower () + ".ban", this);
			permission.RegisterPermission (this.Title.ToLower () + ".unban", this);

			Puts ("Checking connection to server");
			webrequest.Enqueue (config.Host + "/api/server/connectioncheck", null, (code, response) => {
				if (code != 200 || response == null) {
					Puts ($"Connection failed. Response: {response}");
					return;
				}
				Puts ("Connection established and token validated successfully");
			}, this, RequestMethod.GET, headers);

			if (config.PollingInterval > 0) {
				timer.Every (config.PollingInterval, () => {
					PollUsers ();
				});
			}

			usersToPost = new List<string> ();
			usersProcessed = new List<string> ();
			userData = new Dictionary<string, JToken> ();
			timer.Every (5, () => {
				PostUsers (usersToPost);
				usersToPost = new List<string> ();
				PostUsersProcessed (usersProcessed);
				usersProcessed = new List<string> ();
			});
		}

		void OnUserBanned (string name, string id, string ipAddress, string reason) {
			if (config.BanLog == true) {
				Ban (id, "0", reason, false, null, null);
			}
		}

		void OnUserGroupAdded (string id, string groupName) {
			if (config.RoleSync["Post"] == true) {
				PostRole (id, groupName);
			}
		}

		void OnUserGroupRemoved (string id, string groupName) {
			if (config.RoleSync["Post"] == true) {
				DeleteRole (id, groupName);
			}
		}

		void OnUserApproved (string name, string id, string ipAddress) {
			QueueUserToPost (id);
		}

		void OnPlayerConnected (BasePlayer player) {
			ProcessUser (player);
		}
		#endregion

		#region Chat commands
		[ChatCommand ("ban")]
		void cmdBan (BasePlayer player, string command, string[] args) {
			var steamid = player.UserIDString;

			if (player.net.connection.authLevel != 2 && !permission.UserHasPermission (player.UserIDString, this.Title.ToLower () + ".ban")) {
				SendReply (player, lang.GetMessage ("NoPermission", this, player.UserIDString));
				return;
			} else if (args == null || args.Length < 3) {
				SendReply (player, lang.GetMessage ("BanCommandUsage", this, player.UserIDString));
				return;
			}

			var offenderSteamid = "";
			if (args[0].IsSteamId ()) {
				offenderSteamid = args[0];
			} else {
				List<BasePlayer> offenderMatches = GetPlayersByName (args[0]);
				if (offenderMatches != null) {
					if (offenderMatches.Count () == 1) {
						offenderSteamid = offenderMatches.First ().UserIDString;
					} else {
						SendReply (player, lang.GetMessage ("MultiplePlayersFound", this, player.UserIDString));
					}
				} else {
					SendReply (player, string.Format (lang.GetMessage ("NoPlayersFoundByName", this, player.UserIDString), args[0]));
					return;
				}
			}

			int n;
			if (!int.TryParse (args[1], out n)) {
				SendReply (player, lang.GetMessage ("InvalidTime", this, player.UserIDString));
				return;
			}

			bool global = false;
			if (args.Length == 4 && args[3] == "true") {
				global = true;
			}

			BasePlayer offender = GetPlayerBySteamID (offenderSteamid);
			if (offender != null) {
				offender.Kick (lang.GetMessage ("Banned", this, offender.UserIDString));
			}

			Ban (offenderSteamid, args[1], args[2], global, steamid, player);
		}

		[ChatCommand ("unban")]
		void cmdUnban (BasePlayer player, string command, string[] args) {
			if (player.net.connection.authLevel != 2 && !permission.UserHasPermission (player.UserIDString, this.Title.ToLower () + ".unban")) {
				SendReply (player, lang.GetMessage ("NoPermission", this, player.UserIDString));
				return;
			} else if (args == null || args.Length < 1) {
				SendReply (player, lang.GetMessage ("UnbanCommandUsage", this, player.UserIDString));
				return;
			} else if (!args[0].IsSteamId ()) {
				SendReply (player, string.Format (lang.GetMessage ("InvalidSteamid", this, player.UserIDString), args[0]));
				return;
			}

			Unban (args[0].ToString (), player);
		}

		[ChatCommand ("sync")]
		void cmdSync (BasePlayer player, string command, string[] args) {
			QueueUserToPost (player.UserIDString);
			SendReply (player, lang.GetMessage ("Syncing", this, player.UserIDString));
		}
		#endregion

		#region Localization
		protected override void LoadDefaultMessages () {
			lang.RegisterMessages (new Dictionary<string, string> {
				{ "Banned", "You've been banned from the server" },
				{ "PlayerBanned", "Player banned" },
				{ "PlayerUnbanned", "Player unbanned" },
				{ "NoPermission", "You don't have the required permissions to do that" },
				{ "GroupGranted", "You've been granted the {0} group" },
				{ "GroupRevoked", "Your {0} group has been revoked" },
				{ "BanCommandUsage", "Usage: /ban <(partial) name/SteamID64> <time in minutes (0 for permanent)> \"<reason>\" <global?>" },
				{ "UnbanCommandUsage", "Usage: /unban <SteamID64>" },
				{ "MultiplePlayersFound", "Multiple players found, please be more specific" },
				{ "NoPlayersFoundByName", "Player not found by name \"{0}\"" },
				{ "InvalidSteamid", "Invalid SteamID" },
				{ "InvalidTime", "Time must be a number, 0 for permanent" },
				{ "RedeemingPackage", "Redeeming the {0} package" },
				{ "PackageExpired", "Your {0} package has expired" },
				{ "Syncing", "Synchronizing groups & purchases" }
			}, this, "en");
			Puts ("Default messages created");
		}
		#endregion
	}
}