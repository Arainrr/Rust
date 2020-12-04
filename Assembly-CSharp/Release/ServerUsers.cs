using ConVar;
using Facepunch.Extend;
using Facepunch.Math;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class ServerUsers
{
	public enum UserGroup
	{
		None,
		Owner,
		Moderator,
		Banned
	}

	public class User
	{
		public ulong steamid;

		[JsonConverter(typeof(StringEnumConverter))]
		public UserGroup group;

		public string username;

		public string notes;

		public long expiry;

		[JsonIgnore]
		public bool IsExpired
		{
			get
			{
				if (expiry > 0)
				{
					return Epoch.Current > expiry;
				}
				return false;
			}
		}
	}

	public static Dictionary<ulong, User> users = new Dictionary<ulong, User>();

	public static void Remove(ulong uid)
	{
		Interface.CallHook("IOnServerUsersRemove", uid);
		users.Remove(uid);
	}

	public static void Set(ulong uid, UserGroup group, string username, string notes, long expiry = -1L)
	{
		Remove(uid);
		User value = new User
		{
			steamid = uid,
			group = group,
			username = username,
			notes = notes,
			expiry = expiry
		};
		Interface.CallHook("IOnServerUsersSet", uid, group, username, notes, expiry);
		users.Add(uid, value);
	}

	public static User Get(ulong uid)
	{
		User value;
		if (!users.TryGetValue(uid, out value))
		{
			return null;
		}
		if (!value.IsExpired)
		{
			return value;
		}
		Remove(uid);
		return null;
	}

	public static bool Is(ulong uid, UserGroup group)
	{
		User user = Get(uid);
		if (user == null)
		{
			return false;
		}
		return user.group == group;
	}

	public static IEnumerable<User> GetAll(UserGroup group)
	{
		return from x in users.Values
			where x.@group == @group
			where !x.IsExpired
			select x;
	}

	public static void Clear()
	{
		users.Clear();
	}

	public static void Load()
	{
		Clear();
		string serverFolder = Server.GetServerFolder("cfg");
		if (File.Exists(serverFolder + "/bans.cfg"))
		{
			string text = File.ReadAllText(serverFolder + "/bans.cfg");
			if (!string.IsNullOrEmpty(text))
			{
				Debug.Log("Running " + serverFolder + "/bans.cfg");
				ConsoleSystem.RunFile(ConsoleSystem.Option.Server.Quiet(), text);
			}
		}
		if (File.Exists(serverFolder + "/users.cfg"))
		{
			string text2 = File.ReadAllText(serverFolder + "/users.cfg");
			if (!string.IsNullOrEmpty(text2))
			{
				Debug.Log("Running " + serverFolder + "/users.cfg");
				ConsoleSystem.RunFile(ConsoleSystem.Option.Server.Quiet(), text2);
			}
		}
	}

	public static void Save()
	{
		foreach (ulong item in (from kv in users
			where kv.Value.IsExpired
			select kv.Key).ToList())
		{
			Remove(item);
		}
		string serverFolder = Server.GetServerFolder("cfg");
		string text = "";
		foreach (User item2 in GetAll(UserGroup.Banned))
		{
			if (!(item2.notes == "EAC"))
			{
				text += $"banid {item2.steamid} {Facepunch.Extend.StringExtensions.QuoteSafe(item2.username)} {Facepunch.Extend.StringExtensions.QuoteSafe(item2.notes)} {item2.expiry}\r\n";
			}
		}
		File.WriteAllText(serverFolder + "/bans.cfg", text);
		string text2 = "";
		foreach (User item3 in GetAll(UserGroup.Owner))
		{
			text2 += $"ownerid {item3.steamid} {Facepunch.Extend.StringExtensions.QuoteSafe(item3.username)} {Facepunch.Extend.StringExtensions.QuoteSafe(item3.notes)}\r\n";
		}
		foreach (User item4 in GetAll(UserGroup.Moderator))
		{
			text2 += $"moderatorid {item4.steamid} {Facepunch.Extend.StringExtensions.QuoteSafe(item4.username)} {Facepunch.Extend.StringExtensions.QuoteSafe(item4.notes)}\r\n";
		}
		File.WriteAllText(serverFolder + "/users.cfg", text2);
	}

	public static string BanListString(bool bHeader = false)
	{
		List<User> list = GetAll(UserGroup.Banned).ToList();
		string text = "";
		if (bHeader)
		{
			if (list.Count == 0)
			{
				return "ID filter list: empty\n";
			}
			text = ((list.Count != 1) ? $"ID filter list: {list.Count} entries\n" : "ID filter list: 1 entry\n");
		}
		int num = 1;
		foreach (User item in list)
		{
			string arg = (item.expiry > 0) ? $"{(double)(item.expiry - Epoch.Current) / 60.0:F3} min" : "permanent";
			text += $"{num} {item.steamid} : {arg}\n";
			num++;
		}
		return text;
	}

	public static string BanListStringEx()
	{
		IEnumerable<User> all = GetAll(UserGroup.Banned);
		string text = "";
		int num = 1;
		foreach (User item in all)
		{
			text += $"{num} {item.steamid} {Facepunch.Extend.StringExtensions.QuoteSafe(item.username)} {Facepunch.Extend.StringExtensions.QuoteSafe(item.notes)} {item.expiry}\n";
			num++;
		}
		return text;
	}
}
