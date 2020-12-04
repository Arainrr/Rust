namespace ConVar
{
	[Factory("vis")]
	public class Vis : ConsoleSystem
	{
		[ClientVar]
		[Help("Turns on debug display of lerp")]
		public static bool lerp;

		[ServerVar]
		[Help("Turns on debug display of damages")]
		public static bool damage;

		[Help("Turns on debug display of attacks")]
		[ClientVar]
		[ServerVar]
		public static bool attack;

		[Help("Turns on debug display of protection")]
		[ClientVar]
		[ServerVar]
		public static bool protection;

		[ServerVar]
		[Help("Turns on debug display of weakspots")]
		public static bool weakspots;

		[ServerVar]
		[Help("Show trigger entries")]
		public static bool triggers;

		[ServerVar]
		[Help("Turns on debug display of hitboxes")]
		public static bool hitboxes;

		[ServerVar]
		[Help("Turns on debug display of line of sight checks")]
		public static bool lineofsight;

		[Help("Turns on debug display of senses, which are received by Ai")]
		[ServerVar]
		public static bool sense;
	}
}