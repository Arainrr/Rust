using Facepunch;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace ConVar
{
	[Factory("console")]
	public class Console : ConsoleSystem
	{
		[Help("Return the last x lines of the console. Default is 200")]
		[ServerVar]
		public static IEnumerable<Output.Entry> tail(Arg arg)
		{
			int @int = arg.GetInt(0, 200);
			int num = Output.HistoryOutput.Count - @int;
			if (num < 0)
			{
				num = 0;
			}
			return Output.HistoryOutput.Skip(num);
		}

		[Help("Search the console for a particular string")]
		[ServerVar]
		public static IEnumerable<Output.Entry> search(Arg arg)
		{
			string search = arg.GetString(0, null);
			if (search == null)
			{
				return Enumerable.Empty<Output.Entry>();
			}
			return Output.HistoryOutput.Where((Output.Entry x) => x.Message.Length < 4096 && x.Message.Contains(search, CompareOptions.IgnoreCase));
		}
	}
}
