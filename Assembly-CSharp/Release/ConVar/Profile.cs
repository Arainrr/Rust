using System.IO;

namespace ConVar
{
	[Factory("profile")]
	public class Profile : ConsoleSystem
	{
		private static void NeedProfileFolder()
		{
			if (!Directory.Exists("profile"))
			{
				Directory.CreateDirectory("profile");
			}
		}

		[ServerVar]
		[ClientVar]
		public static void start(Arg arg)
		{
		}

		[ClientVar]
		[ServerVar]
		public static void stop(Arg arg)
		{
		}
	}
}
