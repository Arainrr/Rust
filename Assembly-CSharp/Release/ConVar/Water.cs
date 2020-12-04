namespace ConVar
{
	[Factory("water")]
	public class Water : ConsoleSystem
	{
		[ClientVar(Saved = true)]
		public static int quality = 1;

		[ClientVar(Saved = true)]
		public static int reflections = 1;
	}
}
