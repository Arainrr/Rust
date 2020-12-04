using Facepunch;
using System.Collections.Generic;

namespace CompanionServer
{
	public class ChatLog
	{
		public struct Entry
		{
			public ulong SteamId;

			public string Name;

			public string Message;

			public string Color;

			public uint Time;
		}

		private class ChatState : Pool.IPooled
		{
			public List<Entry> History;

			public void EnterPool()
			{
				if (History != null)
				{
					Pool.FreeList(ref History);
				}
			}

			public void LeavePool()
			{
			}
		}

		private const int MaxBacklog = 20;

		private readonly Dictionary<ulong, ChatState> States = new Dictionary<ulong, ChatState>();

		public void Record(ulong teamId, ulong steamId, string name, string message, string color, uint time)
		{
			ChatState value;
			if (!States.TryGetValue(teamId, out value))
			{
				value = Pool.Get<ChatState>();
				value.History = Pool.GetList<Entry>();
				States.Add(teamId, value);
			}
			while (value.History.Count >= 20)
			{
				value.History.RemoveAt(0);
			}
			value.History.Add(new Entry
			{
				SteamId = steamId,
				Name = name,
				Message = message,
				Color = color,
				Time = time
			});
		}

		public void Remove(ulong teamId)
		{
			ChatState value;
			if (States.TryGetValue(teamId, out value))
			{
				States.Remove(teamId);
				Pool.Free(ref value);
			}
		}

		public IReadOnlyList<Entry> GetHistory(ulong teamId)
		{
			ChatState value;
			if (!States.TryGetValue(teamId, out value))
			{
				return null;
			}
			return value.History;
		}
	}
}
