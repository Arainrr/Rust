using CompanionServer;
using Facepunch;
using Facepunch.Math;
using Network;
using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace ConVar
{
	[Factory("chat")]
	public class Chat : ConsoleSystem
	{
		public enum ChatChannel
		{
			Global,
			Team,
			Server
		}

		public struct ChatEntry
		{
			public ChatChannel Channel
			{
				get;
				set;
			}

			public string Message
			{
				get;
				set;
			}

			public string UserId
			{
				get;
				set;
			}

			public string Username
			{
				get;
				set;
			}

			public string Color
			{
				get;
				set;
			}

			public int Time
			{
				get;
				set;
			}
		}

		private const float textRange = 50f;

		private const float textVolumeBoost = 0.2f;

		[ClientVar]
		[ServerVar]
		public static bool enabled = true;

		public static List<ChatEntry> History = new List<ChatEntry>();

		[ServerVar]
		public static bool serverlog = true;

		public static void Broadcast(string message, string username = "SERVER", string color = "#eee", ulong userid = 0uL)
		{
			if (Interface.CallHook("OnServerMessage", message, username, color, userid) == null)
			{
				string text = username.EscapeRichText();
				ConsoleNetwork.BroadcastToAllClients("chat.add", 2, 0, "<color=" + color + ">" + text + "</color> " + message);
				ChatEntry chatEntry = default(ChatEntry);
				chatEntry.Channel = ChatChannel.Server;
				chatEntry.Message = message;
				chatEntry.UserId = userid.ToString();
				chatEntry.Username = username;
				chatEntry.Color = color;
				chatEntry.Time = Epoch.Current;
				ChatEntry chatEntry2 = chatEntry;
				History.Add(chatEntry2);
				RCon.Broadcast(RCon.LogType.Chat, chatEntry2);
			}
		}

		[ServerUserVar]
		public static void say(Arg arg)
		{
			sayImpl(ChatChannel.Global, arg);
		}

		[ServerUserVar]
		public static void teamsay(Arg arg)
		{
			sayImpl(ChatChannel.Team, arg);
		}

		private static void sayImpl(ChatChannel targetChannel, Arg arg)
		{
			if (!enabled)
			{
				arg.ReplyWith("Chat is disabled.");
				return;
			}
			BasePlayer basePlayer = ArgEx.Player(arg);
			if (!basePlayer || basePlayer.HasPlayerFlag(BasePlayer.PlayerFlags.ChatMute))
			{
				return;
			}
			if (!basePlayer.IsAdmin && !basePlayer.IsDeveloper)
			{
				if (basePlayer.NextChatTime == 0f)
				{
					basePlayer.NextChatTime = UnityEngine.Time.realtimeSinceStartup - 30f;
				}
				if (basePlayer.NextChatTime > UnityEngine.Time.realtimeSinceStartup)
				{
					basePlayer.NextChatTime += 2f;
					float num = basePlayer.NextChatTime - UnityEngine.Time.realtimeSinceStartup;
					ConsoleNetwork.SendClientCommand(basePlayer.net.connection, "chat.add", 2, 0, "You're chatting too fast - try again in " + (num + 0.5f).ToString("0") + " seconds");
					if (num > 120f)
					{
						basePlayer.Kick("Chatting too fast");
					}
					return;
				}
			}
			string @string = arg.GetString(0, "text");
			if (sayAs(targetChannel, basePlayer.userID, basePlayer.displayName, @string, basePlayer))
			{
				basePlayer.NextChatTime = UnityEngine.Time.realtimeSinceStartup + 1.5f;
			}
		}

		internal static bool sayAs(ChatChannel targetChannel, ulong userId, string username, string message, BasePlayer player = null)
		{
			if (!player)
			{
				player = null;
			}
			if (!enabled)
			{
				return false;
			}
			if (player != null && player.HasPlayerFlag(BasePlayer.PlayerFlags.ChatMute))
			{
				return false;
			}
			ServerUsers.UserGroup userGroup = ServerUsers.Get(userId)?.group ?? ServerUsers.UserGroup.None;
			if (userGroup == ServerUsers.UserGroup.Banned)
			{
				return false;
			}
			string text = message.Replace("\n", "").Replace("\r", "").Trim();
			if (text.Length > 128)
			{
				text = text.Substring(0, 128);
			}
			if (text.Length <= 0)
			{
				return false;
			}
			if (text.StartsWith("/") || text.StartsWith("\\"))
			{
				Interface.CallHook("IOnPlayerCommand", player, message);
				return false;
			}
			text = text.EscapeRichText();
			object obj = Interface.CallHook("IOnPlayerChat", userId, username, text, targetChannel, player);
			if (obj is bool)
			{
				return (bool)obj;
			}
			if (serverlog)
			{
				ServerConsole.PrintColoured(ConsoleColor.DarkYellow, "[" + targetChannel + "] " + username + ": ", ConsoleColor.DarkGreen, text);
				string str = player?.ToString() ?? $"{username}[{userId}]";
				if (targetChannel == ChatChannel.Team)
				{
					DebugEx.Log("[TEAM CHAT] " + str + " : " + text);
				}
				else
				{
					DebugEx.Log("[CHAT] " + str + " : " + text);
				}
			}
			bool flag = userGroup == ServerUsers.UserGroup.Owner || userGroup == ServerUsers.UserGroup.Moderator;
			bool num = (player != null) ? player.IsDeveloper : DeveloperList.Contains(userId);
			string text2 = "#5af";
			if (flag)
			{
				text2 = "#af5";
			}
			if (num)
			{
				text2 = "#fa5";
			}
			string text3 = username.EscapeRichText();
			ChatEntry chatEntry = default(ChatEntry);
			chatEntry.Channel = targetChannel;
			chatEntry.Message = text;
			chatEntry.UserId = ((player != null) ? player.UserIDString : userId.ToString());
			chatEntry.Username = username;
			chatEntry.Color = text2;
			chatEntry.Time = Epoch.Current;
			ChatEntry chatEntry2 = chatEntry;
			History.Add(chatEntry2);
			RCon.Broadcast(RCon.LogType.Chat, chatEntry2);
			switch (targetChannel)
			{
			case ChatChannel.Global:
				if (Server.globalchat)
				{
					ConsoleNetwork.BroadcastToAllClients("chat.add2", 0, userId, text, text3, text2, 1f);
					return true;
				}
				break;
			case ChatChannel.Team:
			{
				RelationshipManager.PlayerTeam playerTeam = RelationshipManager.Instance.FindPlayersTeam(userId);
				if (playerTeam == null)
				{
					return false;
				}
				List<Network.Connection> onlineMemberConnections = playerTeam.GetOnlineMemberConnections();
				if (onlineMemberConnections != null)
				{
					ConsoleNetwork.SendClientCommand(onlineMemberConnections, "chat.add2", 1, userId, text, text3, text2, 1f);
				}
				Util.BroadcastTeamChat(playerTeam, userId, text3, text, text2);
				return true;
			}
			}
			if (player != null)
			{
				float num2 = 2500f;
				foreach (BasePlayer activePlayer in BasePlayer.activePlayerList)
				{
					float sqrMagnitude = (activePlayer.transform.position - player.transform.position).sqrMagnitude;
					if (!(sqrMagnitude > num2))
					{
						ConsoleNetwork.SendClientCommand(activePlayer.net.connection, "chat.add2", 0, userId, text, text3, text2, Mathf.Clamp01(num2 - sqrMagnitude + 0.2f));
					}
				}
				return true;
			}
			return false;
		}

		[ServerVar]
		[Help("Return the last x lines of the console. Default is 200")]
		public static IEnumerable<ChatEntry> tail(Arg arg)
		{
			int @int = arg.GetInt(0, 200);
			int num = History.Count - @int;
			if (num < 0)
			{
				num = 0;
			}
			return History.Skip(num);
		}

		[Help("Search the console for a particular string")]
		[ServerVar]
		public static IEnumerable<ChatEntry> search(Arg arg)
		{
			string search = arg.GetString(0, null);
			if (search == null)
			{
				return Enumerable.Empty<ChatEntry>();
			}
			return History.Where((ChatEntry x) => x.Message.Length < 4096 && x.Message.Contains(search, CompareOptions.IgnoreCase));
		}
	}
}
