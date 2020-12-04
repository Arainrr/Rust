using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;

namespace Oxide.Plugins
{
	[Info("Clan Chat", "Obito", "1.0.1")]
	[Description("Send a private message to all members of your clan at once.")]

	class ClanChat : HurtworldPlugin
	{
		#region uMod Hooks

		void Init() => cmd.AddChatCommand(config.msgCommand, this, nameof(cmdClanMsg));

		#endregion


		#region Commands

		private void cmdClanMsg(PlayerSession session, string command, string[] args)
		{
			if (session.Identity.Clan == null)
			{
				SendMessage(session, "No clan");
				return;
			}
			if (args.Length < 1)
			{
				SendMessage(session, "Syntax error", config.msgCommand);
				return;
			}
			var onlineClan = GetOnlineMembers(session.Identity.Clan.GetMemebers());
			if (onlineClan.Count == 1)
			{
				SendMessage(session, "Solo");
				return;
			}

			var message = string.Join(" ", args);
			if (config.lowercaseMsg)
				message = message.ToLower();

			TrySendPMs(session, message, onlineClan);
		}

		#endregion


		#region Functions

		private void TrySendPMs(PlayerSession from, string message, HashSet<ulong> to)
		{
			foreach (var id in to)
			{
				var toMember = Player.FindById(id.ToString());
				if (toMember != null)
					Player.Message(toMember, string.Format(config.chatFormat, 
						$"<color={config.colorName}>{from.Identity.Name}</color>",
						$"<color={config.colorMsg}>{message}</color>"));
			}
		}

		#endregion


		#region Helpers

		private HashSet<ulong> GetOnlineMembers(HashSet<ulong> clanMembers)
		{
			HashSet<ulong> onlineMembers = new HashSet<ulong>();
			foreach (var id in clanMembers)
				if (Player.FindById(id.ToString()) != null)
					onlineMembers.Add(id);

			return onlineMembers;
		}

		private void SendMessage(PlayerSession session, string message, params object[] args)
			=> Player.Message(session, GetMessage(message, session.IPlayer.Id, args), config.chatTag);

		#endregion


		#region Localization

		protected override void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			{
				["No clan"] = "You are not part of any clan.",
				["Solo"] = "You are the only member of your clan",
				["Syntax error"] = "Syntax error. Use <color=orange>/{0} 'message'</color> to send a message to all members of your clan."
			}, this);
		}

		private string GetMessage(string message, string userID, params object[] args)
			=> string.Format(lang.GetMessage(message, this, userID), args);

		#endregion


		#region Configuration

		private static Configuration config;
		private class Configuration
		{
			[JsonProperty("Chat tag")]
			public string chatTag = "<color=#ff0000>ClanChat:</color>";

			[JsonProperty("Chat format")]
			public string chatFormat = "(Clan)[{0}]: {1}";

			[JsonProperty("Name color")]
			public string colorName = "#ffff00";

			[JsonProperty("Message color")]
			public string colorMsg = "#00ffff";

			[JsonProperty("Lowercase message")]
			public bool lowercaseMsg = true;

			[JsonProperty("Message command")]
			public string msgCommand = "c";
		}

		protected override void LoadConfig()
		{
			base.LoadConfig();
			try
			{
				config = Config.ReadObject<Configuration>();
				if (config == null) LoadDefaultConfig();
			}
			catch
			{
				LoadDefaultConfig();
				PrintError("Your config file is corrupt. Loading the defaults and unloading the plugin..");
				Interface.Oxide.UnloadPlugin(this.Name);
			}
			SaveConfig();
		}
		protected override void LoadDefaultConfig() => config = new Configuration();
		protected override void SaveConfig() => Config.WriteObject(config);

		#endregion
	}
}