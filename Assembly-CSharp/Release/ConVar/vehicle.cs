using UnityEngine;

namespace ConVar
{
	[Factory("vehicle")]
	public class vehicle : ConsoleSystem
	{
		[Help("how long until boat corpses despawn")]
		[ServerVar]
		public static float boat_corpse_seconds = 300f;

		[ServerVar(Help = "Determines whether modular cars turn into wrecks when destroyed, or just immediately gib. Default: true")]
		public static bool carwrecks = true;

		[ServerVar(Help = "Determines whether modular cars drop storage items when destroyed. Default: true")]
		public static bool carsdroploot = true;

		[ServerUserVar]
		public static void swapseats(Arg arg)
		{
			int targetSeat = 0;
			BasePlayer basePlayer = ArgEx.Player(arg);
			if (basePlayer == null || basePlayer.SwapSeatCooldown())
			{
				return;
			}
			BaseMountable mounted = basePlayer.GetMounted();
			if (!(mounted == null))
			{
				BaseVehicle baseVehicle = mounted.GetComponent<BaseVehicle>();
				if (baseVehicle == null)
				{
					baseVehicle = mounted.VehicleParent();
				}
				if (!(baseVehicle == null))
				{
					baseVehicle.SwapSeats(basePlayer, targetSeat);
				}
			}
		}

		[ServerVar]
		public static void fixcars(Arg arg)
		{
			BasePlayer basePlayer = ArgEx.Player(arg);
			if (basePlayer == null)
			{
				arg.ReplyWith("Null player.");
				return;
			}
			if (!basePlayer.IsAdmin)
			{
				arg.ReplyWith("Must be an admin to use fixcars.");
				return;
			}
			int @int = arg.GetInt(0, 2);
			@int = Mathf.Clamp(@int, 1, 3);
			ModularCar[] array = Object.FindObjectsOfType<ModularCar>();
			int num = 0;
			ModularCar[] array2 = array;
			foreach (ModularCar modularCar in array2)
			{
				if (modularCar.isServer && Vector3.Distance(modularCar.transform.position, basePlayer.transform.position) <= 5f && modularCar.AdminFixUp(@int))
				{
					num++;
				}
			}
			arg.ReplyWith($"Fixed up {num} cars.");
		}
	}
}
