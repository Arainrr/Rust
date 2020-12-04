namespace CCTVRender
{
	[Factory("cctvrender")]
	public class Settings : ConsoleSystem
	{
		public const int MaxImageSize = 153600;

		[ServerVar]
		public static bool Enabled = false;

		[ServerVar]
		public static float MaxDistance = 100f;

		[ServerVar]
		public static float CombatTime = 15f;

		[ServerVar]
		public static float IdleTime = 5f;

		[ServerVar]
		public static float AssignmentTimeout = 2f;

		[ServerVar]
		public static float AssignmentCooldown = 0.5f;
	}
}
