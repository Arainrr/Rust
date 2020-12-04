using JSON;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SteamNewsSource
{
	public struct Story
	{
		public string name;

		public string url;

		public int date;

		public string text;

		public string author;
	}

	public static Story[] Stories;

	public static IEnumerator GetStories()
	{
		WWW www = new WWW("http://api.steampowered.com/ISteamNews/GetNewsForApp/v0002/?appid=252490&count=8&format=json&feeds=steam_community_announcements");
		yield return www;
		JSON.Object @object = JSON.Object.Parse(www.text);
		www.Dispose();
		if (@object != null)
		{
			Array array = @object.GetObject("appnews").GetArray("newsitems");
			List<Story> list = new List<Story>();
			foreach (Value item in array)
			{
				string @string = item.Obj.GetString("contents", "Missing URL");
				list.Add(new Story
				{
					name = item.Obj.GetString("title", "Missing Title"),
					url = item.Obj.GetString("url", "Missing URL"),
					date = item.Obj.GetInt("date"),
					text = @string,
					author = item.Obj.GetString("author", "Missing Author")
				});
			}
			Stories = list.ToArray();
		}
	}
}
