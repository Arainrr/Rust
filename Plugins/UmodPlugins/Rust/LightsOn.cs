using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("Lights On", "mspeedie", "1.6.16")]
	[Description("Toggle lights on/off either as configured or by name.")]
	// big thank you to FastBurn for helping fix Fluid Splitters and adding Flashing Lights and Siren Lights
	public class LightsOn : CovalencePlugin
	//RustPlugin
	{

		//[PluginReference] Plugin ZoneManager;
		const string perm_lightson     = "lightson.allowed";
		private bool InitialPassNight  = true;
		private bool NightToggleactive = false;
		private bool nightcross24      = false;
		private bool config_changed    = false;
		private Timer Nighttimer;
		private Timer Alwaystimer;
		private Timer Devicetimer;

		// strings to compare to check object names
		const string bbq_name                = "bbq";
		const string campfire_name           = "campfire";
		const string cctv_name               = "cctv";
		const string ceilinglight_name       = "ceilinglight";
		const string chineselantern_name     = "chineselantern";
		const string cursedcauldron_name     = "cursedcauldron";
		const string deluxe_lightstring_name = "xmas.lightstring.advanced";
		const string fireplace_name          = "fireplace";
		const string fluidswitch_name        = "fluidswitch";
		const string fogmachine_name         = "fogmachine";
		const string furnace_large_name      = "furnace.large";
		const string furnace_name            = "furnace";
		const string hatcandle_name          = "hat.candle";
		const string hatminer_name           = "hat.miner";
		const string heater_name             = "electrical.heater";
		const string hobobarrel_name         = "hobobarrel";
		const string igniter_name            = "igniter";
		const string jackolanternangry_name  = "jackolantern.angry";
		const string jackolanternhappy_name  = "jackolantern.happy";
		const string lantern_name            = "lantern";
		const string largecandleset_name     = "largecandleset";
		const string vehiclelift_name        = "modularcarlift";
		const string refinerysmall_name      = "small_refinery";
		const string searchlight_name        = "searchlight";
		const string simplelight_name        = "simplelight";
		const string skullfirepit_name       = "skull_fire_pit";
		const string smallcandleset_name     = "smallcandleset";
		const string smallrefinery_name      = "small.oil.refinery";
		const string smart_alarm_name        = "smart.alarm";
		const string smart_switch_name       = "smart.switch";
		const string snowmachine_name        = "snowmachine";
		const string tunalight_name          = "tunalight";
		const string water_purifier_name     = "poweredwaterpurifier";
		const string waterpump_name          = "water.pump";
        const string flasherlight_name       = "electric.flasherlight";
        const string sirenlight_name         = "electric.sirenlight";
        const string spookyspeaker_name      = "spookyspeaker";
        const string strobelight_name        = "strobelight";

		#region Configuration

		private Configuration config;

		public class Configuration
		{
			// True means turn them on
			[JsonProperty(PropertyName = "Hats do not use fuel (true/false)")]
			public bool Hats { get; set; } = true;

			[JsonProperty(PropertyName = "BBQs (true/false)")]
			public bool BBQs { get; set; } = false;

			[JsonProperty(PropertyName = "Campfires (true/false)")]
			public bool Campfires { get; set; } = false;

			[JsonProperty(PropertyName = "Candles (true/false)")]
			public bool Candles { get; set; } = true;

			[JsonProperty(PropertyName = "Cauldrons (true/false)")]
			public bool Cauldrons { get; set; } = false;

			[JsonProperty(PropertyName = "Ceiling Lights (true/false)")]
			public bool CeilingLights { get; set; } = true;

			[JsonProperty(PropertyName = "Fire Pits (true/false)")]
			public bool FirePits { get; set; } = false;

			[JsonProperty(PropertyName = "Fireplaces (true/false)")]
			public bool Fireplaces { get; set; } = true;

			[JsonProperty(PropertyName = "Fog Machines (true/false)")]
			public bool Fog_Machines { get; set; } = true;

			[JsonProperty(PropertyName = "Furnaces (true/false)")]
			public bool Furnaces { get; set; } = false;

			[JsonProperty(PropertyName = "Hobo Barrels (true/false)")]
			public bool HoboBarrels { get; set; } = true;

			[JsonProperty(PropertyName = "Lanterns (true/false)")]
			public bool Lanterns { get; set; } = true;

			[JsonProperty(PropertyName = "Refineries (true/false)")]
			public bool Refineries { get; set; } = false;

			[JsonProperty(PropertyName = "Search Lights (true/false)")]
			public bool SearchLights { get; set; } = true;

            [JsonProperty(PropertyName = "Simple Lights (true/false)")]
            public bool SimpleLights { get; set; } = false;

            [JsonProperty(PropertyName = "Siren Lights (true/false)")]
            public bool SirenLights { get; set; } = false;

            [JsonProperty(PropertyName = "Flasher Lights (true/false)")]
            public bool FlasherLights { get; set; } = false;

            [JsonProperty(PropertyName = "SpookySpeakers (true/false)")]
			public bool Speakers { get; set; } = false;

			[JsonProperty(PropertyName = "Strobe Lights (true/false)")]
			public bool StrobeLights { get; set; } = false;

			[JsonProperty(PropertyName = "SnowMachines (true/false)")]
			public bool Snow_Machines { get; set; } = true;

			[JsonProperty(PropertyName = "Deluxe Light Strings (true/false)")]
			public bool Deluxe_lightstrings { get; set; } = true;

			[JsonProperty(PropertyName = "CCTVs (true/false)")]
			public bool CCTVs { get; set; } = true;

			[JsonProperty(PropertyName = "Igniters (true/false)")]
			public bool Igniters { get; set; } = true;

			[JsonProperty(PropertyName = "Heaters (true/false)")]
			public bool Heaters { get; set; } = true;

			[JsonProperty(PropertyName = "FluidSwitches (true/false)")]
			public bool FluidSwitches { get; set; } = true;

			[JsonProperty(PropertyName = "Water Pumps (true/false)")]
			public bool WaterPumps { get; set; } = true;

			[JsonProperty(PropertyName = "Water Purifier s(true/false)")]
			public bool WaterPurifiers { get; set; } = true;

            [JsonProperty(PropertyName = "Smart Alarms (true/false)")]
            public bool SmartAlarms { get; set; } = false;

            [JsonProperty(PropertyName = "Smart Switches (true/false)")]
            public bool SmartSwitches { get; set; } = false;

			[JsonProperty(PropertyName = "VehicleLift (true/false)")]
			public bool VehicleLifts { get; set; } = true;

			[JsonProperty(PropertyName = "Protect BBQs (true/false)")]
			public bool ProtectBBQs { get; set; } = true;

			[JsonProperty(PropertyName = "Protect Campfires (true/false)")]
			public bool ProtectCampfires { get; set; } = true;

			[JsonProperty(PropertyName = "Protect Cauldrons (true/false)")]
			public bool ProtectCauldrons { get; set; } = true;

			[JsonProperty(PropertyName = "Protect Fire Pits (true/false)")]
			public bool ProtectFirePits { get; set; } = true;

			[JsonProperty(PropertyName = "Protect Fireplaces (true/false)")]
			public bool ProtectFireplaces { get; set; } = true;

			[JsonProperty(PropertyName = "Protect Furnaces (true/false)")]
			public bool ProtectFurnaces { get; set; } = true;

			[JsonProperty(PropertyName = "Protect Hobo Barrels (true/false)")]
			public bool ProtectHoboBarrels { get; set; } = false;

			[JsonProperty(PropertyName = "Protect Refineries (true/false)")]
			public bool ProtectRefineries { get; set; } = true;

			[JsonProperty(PropertyName = "Devices Always On (true/false)")]
			public bool DevicesAlwaysOn { get; set; } = true;

			[JsonProperty(PropertyName = "Always On (true/false)")]
			public bool AlwaysOn { get; set; } = false;

			[JsonProperty(PropertyName = "Night Toggle (true/false)")]
			public bool NightToggle { get; set; } = true;

			[JsonProperty(PropertyName = "Console Output (true/false)")]
			public bool ConsoleMsg { get; set; } = true;

			// this is checked more frequently to get the lights on/off closer to the time the operator sets
			[JsonProperty(PropertyName = "Night Toggle Check Frequency (in seconds)")]
			public int NightCheckFrequency { get; set; } = 30;

			// these less frequent checks as most devices will be on when placed
			[JsonProperty(PropertyName = "Always On Check Frequency (in seconds)")]
			public int AlwaysCheckFrequency { get; set; } = 300;

			// these less frequent checks as most devices will be on when placed
			[JsonProperty(PropertyName = "Device Check Frequency (in seconds)")]
			public int DeviceCheckFrequency { get; set; } = 300;


			[JsonProperty(PropertyName = "Dusk Time (HH in a 24 hour clock)")]
			public float DuskTime { get; set; } = 17.5f;

			[JsonProperty(PropertyName = "Dawn Time (HH in a 24 hour clock)")]
			public float DawnTime { get; set; } = 09.0f;

//			[JsonProperty(PropertyName = "Use Zone Manager Plugin")]
//			public bool UseZoneManagerPlugin { get; set; } = false;

			public string ToJson() => JsonConvert.SerializeObject(this);

			public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
		}

		protected override void LoadDefaultConfig() => config = new Configuration();

		protected override void LoadConfig()
		{
			base.LoadConfig();
			try
			{
				config = Config.ReadObject<Configuration>();
				if (config == null)
				{
					throw new JsonException();
				}

				if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys))
				{
					LogWarning("Configuration appears to be outdated; updating and saving");
					SaveConfig();
				}
			}
			catch
			{
				LogWarning($"Configuration file {Name}.json is invalid; using defaults");
				LoadDefaultConfig();
			}
			CheckConfig();
		}

		protected override void SaveConfig()
		{
			LogWarning($"Configuration changes saved to {Name}.json");
			Config.WriteObject(config, true);
		}

		#endregion Configuration

		string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

		protected override void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			{
				["bad check frequency"] = "Check frequency must be between 10 and 600",
				["bad check frequency2"] = "Check frequency must be between 60 and 6000",
				["bad prefab"] = "Bad Prefab Name, not found in devices or lights: ",
				["bad dusk time"] = "Dusk time must be between 0 and 24",
				["bad dawn time"] = "Dawn time must be between 0 and 24",
				["dawn=dusk"] = "Dawn can't be the same value as dusk",
				["dawn"] = "Lights going off.  Next lights on at ",
				["default"] = "Loading default config for LightsOn",
				["dusk"] = "Lights coming on.  Ending at ",
				["lights off"] = "Lights Off",
				["lights on"] = "Lights On",
				["nopermission"] = "You do not have permission to use that command.",
				["one or the other"] = "Please select one (and only one) of Always On or Night Toggle",
				["prefix"] = "LightsOn: ",
				["state"] = "unknown state: please use on or off",
				["syntax"] = "syntax: Lights State (on/off) Optional: prefabshortname (part of the prefab name) to change their state, use all to force all lights' state",
				["zone"] = "Zone Manager requested but not found loaded"
			}, this);
		}

		protected void CheckConfig()
		{
			config_changed = false;

			// check data is ok because people can make mistakes
			if (config.AlwaysOn && config.NightToggle)
			{
				Puts(Lang("one or the other"));
				config.NightToggle = false;
				config_changed = true;
			}
			if (config.DuskTime < 0f || config.DuskTime > 24f)
			{
				Puts(Lang("bad dusk time"));
				config.DuskTime = 17f;
				config_changed = true;
			}
			if (config.DawnTime < 0f || config.DawnTime > 24f)
			{
				Puts(Lang("bad dawn time"));
				config.DawnTime = 9f;
				config_changed = true;
			}
			if (config.DawnTime == config.DuskTime)
			{
				Puts(Lang("dawn=dusk"));
				config.DawnTime = 9f;
				config.DuskTime = 17f;
				config_changed = true;
			}
			if (config.NightCheckFrequency < 10 || config.NightCheckFrequency > 600)
			{
				Puts(Lang("bad check frequency"));
				config.NightCheckFrequency = 30;
				config_changed = true;
			}

			if (config.AlwaysCheckFrequency < 60 || config.AlwaysCheckFrequency > 6000)
			{
				Puts(Lang("bad check frequency2"));
				config.AlwaysCheckFrequency = 300;
				config_changed = true;
			}

			if (config.DeviceCheckFrequency < 60 || config.DeviceCheckFrequency > 6000)
			{
				Puts(Lang("bad check frequency2"));
				config.DeviceCheckFrequency = 300;
				config_changed = true;
			}

			// determine correct light timing logic
			if  (config.DuskTime > config.DawnTime)
				nightcross24 = true;
			else
				nightcross24 = false;

			if (config.AlwaysOn)
			{
				// start timer to lights always on
				Alwaystimer = timer.Once(config.AlwaysCheckFrequency, AlwaysTimerProcess);
			}
			else if (config.NightToggle)
			{
				// start timer to toggle lights based on time
				InitialPassNight = true;
				Nighttimer = timer.Once(config.NightCheckFrequency, NightTimerProcess);
			}

			if (config.DevicesAlwaysOn)
			{
				// start timer to toggle devices
				Devicetimer = timer.Once(config.DeviceCheckFrequency, DeviceTimerProcess);
			}

			//if (config.UseZoneManagerPlugin)
			//{
			//	if (ZoneManager == null || !ZoneManager.IsLoaded)
			//	{
			//		config.UseZoneManagerPlugin = false;
			//		Puts(Lang("zone"));
			//	}
			//}

			if (config_changed)
				SaveConfig();
		}

		private void OnServerInitialized()
		{
			if (!permission.PermissionExists(perm_lightson))
				permission.RegisterPermission(perm_lightson,this);
			// give the server time to spawn entities
			if (config.Deluxe_lightstrings)
				timer.Once(60, () => {LightStringChangePower(); return;});
            if (config.FlasherLights)
                timer.Once(60, () => { FlasherLightChangePower(); return; });
            if (config.SirenLights)
                timer.Once(60, () => { SirenLightChangePower(); return; });
            if (config.SmartAlarms)
                timer.Once(60, () => { SmartAlarmChangePower(); return; });
            if (config.SmartSwitches)
                timer.Once(60, () => { SmartSwitchChangePower(); return; });
            if (config.CCTVs)
				timer.Once(60, () => {CCTVChangePower(); return;});
			if (config.Igniters)
				timer.Once(60, () => {IgniterChangePower(); return;});
			if (config.VehicleLifts)
				timer.Once(60, () => {VehicleLiftChangePower(); return;});
			if (config.FluidSwitches)
				timer.Once(60, () => {FluidSwitchChangePower(); return;});
			if (config.Heaters)
				timer.Once(60, () => {HeaterChangePower(); return;});
			if (config.WaterPumps)
				timer.Once(60, () => {WaterPumpChangePower(); return;});
			if (config.WaterPurifiers)
				timer.Once(60, () => {WaterPurifierChangePower(); return;});
			if (config.SearchLights)
				timer.Once(60, () => {SearchLightChangePower(); return;});

		}

		string CleanedName(string prefabName)
		{
			if (string.IsNullOrEmpty(prefabName))
				return prefabName;

			string CleanedString = prefabName;
			int clean_loc = CleanedString.IndexOf("deployed");
			if (clean_loc > 1)
				CleanedString = CleanedString.Remove(clean_loc-1);
			clean_loc = CleanedString.IndexOf("static");
			if (clean_loc > 1)
				CleanedString = CleanedString.Remove(clean_loc-1);

			return CleanedString;
		}

		bool IsOvenPrefabName(string prefabName)
		{
			if (furnace_name.Contains(prefabName))
				return true;
			else if (furnace_large_name.Contains(prefabName))
				return true;
			else if (bbq_name.Contains(prefabName))
				return true;
			else if (campfire_name.Contains(prefabName))
				return true;
			else if (chineselantern_name.Contains(prefabName))
				return true;
			else if (fireplace_name.Contains(prefabName))
				return true;
			else if (hobobarrel_name.Contains(prefabName))
				return true;
			else if (lantern_name.Contains(prefabName))
				return true;
			else if (tunalight_name.Contains(prefabName))
				return true;
			else if (cursedcauldron_name.Contains(prefabName))
				return true;
			else if (skullfirepit_name.Contains(prefabName))
				return true;
			else if (refinerysmall_name.Contains(prefabName))
				return true;
			else if (smallrefinery_name.Contains(prefabName))
				return true;
			else if (jackolanternangry_name.Contains(prefabName))
				return true;
			else if (jackolanternhappy_name.Contains(prefabName))
				return true;
			else
				return false;
		}

		bool IsLightToggle(string prefabName)
		{
			if (lantern_name.Contains(prefabName))
				return true;
			else if (tunalight_name.Contains(prefabName))
				return true;
			else if (chineselantern_name.Contains(prefabName))
				return true;
			//else if (jackolanternangry_name.Contains(prefabName))
			//	return true;
			//else if (jackolanternhappy_name.Contains(prefabName))
			//	return true;
			//else if (largecandleset_name.Contains(prefabName))
			//	return true;
			//else if (smallcandleset_name.Contains(prefabName))
			//	return true;
			//else if (searchlight_name.Contains(prefabName))
			//	return true;
			//else if (simplelight_name.Contains(prefabName))
			//	return true;
			else
				return false;
		}

		bool IsLightPrefabName(string prefabName)
		{
			if (lantern_name.Contains(prefabName))
				return true;
			else if (tunalight_name.Contains(prefabName))
				return true;
			else if (ceilinglight_name.Contains(prefabName))
				return true;
            else if (deluxe_lightstring_name.Contains(prefabName))
                return true;
            else if (flasherlight_name.Contains(prefabName))
                return true;
            else if (jackolanternangry_name.Contains(prefabName))
				return true;
			else if (jackolanternhappy_name.Contains(prefabName))
				return true;
			else if (chineselantern_name.Contains(prefabName))
				return true;
			else if (largecandleset_name.Contains(prefabName))
				return true;
			else if (smallcandleset_name.Contains(prefabName))
				return true;
			else if (searchlight_name.Contains(prefabName))
				return true;
            else if (simplelight_name.Contains(prefabName))
                return true;
            else if (sirenlight_name.Contains(prefabName))
                return true;
            else
                return false;
		}

		bool IsHatPrefabName(string prefabName)
		{
			// this uses only internal names so do not need the Contains logic
			switch (prefabName)
			{
				case hatminer_name: 	return true;
				case hatcandle_name:	return true;
				default:				return false;
			}
		}

		bool IsDevicePrefabName(string prefabName)
		{
			if (fogmachine_name.Contains(prefabName))
				return true;
			else if (snowmachine_name.Contains(prefabName))
				return true;
			else if (strobelight_name.Contains(prefabName))
				return true;
			else if (spookyspeaker_name.Contains(prefabName))
				return true;
			else if (heater_name.Contains(prefabName))
				return true;
			else if (water_purifier_name.Contains(prefabName))
				return true;
			else if (waterpump_name.Contains(prefabName))
				return true;
			else if (fluidswitch_name.Contains(prefabName))
				return true;
			else if (cctv_name.Contains(prefabName))
				return true;
			else if (smart_alarm_name.Contains(prefabName))
				return true;
			else if (smart_switch_name.Contains(prefabName))
				return true;
			else
				return false;
		}

		bool CanCookShortPrefabName(string prefabName)
		{
			if (furnace_name.Contains(prefabName))
				return true;
			else if (furnace_large_name.Contains(prefabName))
				return true;
			else if (campfire_name.Contains(prefabName))
				return true;
			else if (bbq_name.Contains(prefabName))
				return true;
			else if (fireplace_name.Contains(prefabName))
				return true;
			else if (refinerysmall_name.Contains(prefabName))
				return true;
			else if (smallrefinery_name.Contains(prefabName))
				return true;
			else if (skullfirepit_name.Contains(prefabName))
				return true;
			else if (hobobarrel_name.Contains(prefabName))
				return true;
			else if (cursedcauldron_name.Contains(prefabName))
				return true;
			else
				return false;
		}

		bool ProtectShortPrefabName(string prefabName)
		{
			switch (CleanedName(prefabName))
			{
				case "bbq":					return config.ProtectBBQs;
				case "campfire":			return config.ProtectCampfires;
				case "cursedcauldron":		return config.ProtectCauldrons;
				case "fireplace":			return config.ProtectFireplaces;
				case "furnace":				return config.ProtectFurnaces;
				case "furnace.large":		return config.ProtectFurnaces;
				case "hobobarrel":			return config.ProtectHoboBarrels;
				case "refinery_small":		return config.ProtectRefineries;
				case "small.oil.refinery":	return config.ProtectRefineries;
				case "skull_fire_pit":		return config.ProtectFirePits;
				default:
				{
					return false;
				}
			}
		}

		bool ProcessShortPrefabName(string prefabName)
		{
			switch (CleanedName(prefabName))
			{
				case "bbq":					      return config.BBQs;
				case "campfire":			      return config.Campfires;
				case "cctv":                      return config.CCTVs;
				case "ceilinglight":		      return config.CeilingLights;
				case "cursedcauldron":		      return config.Cauldrons;
				case "fireplace":			      return config.Fireplaces;
				case "fluidswitch":               return config.FluidSwitches;
                case "electric.flasherlight":     return config.FlasherLights;
                case "fogmachine":			      return config.Fog_Machines;
				case "furnace":				      return config.Furnaces;
				case "furnace.large":		      return config.Furnaces;
				case "hobobarrel":			      return config.HoboBarrels;
				case "igniter":                   return config.Igniters;
				case "vehiclelift":               return config.VehicleLifts;
				case "electrical.heater":         return config.Heaters;
				case "jackolantern.angry":	      return config.Lanterns;
				case "jackolantern.happy":	      return config.Lanterns;
				case "lantern":				      return config.Lanterns;
				case "chineselantern":		      return config.Lanterns;
				case "largecandleset":		      return config.Candles;
				case "refinery_small":		      return config.Refineries;
				case "searchlight":			      return config.SearchLights;
                case "simplelight":               return config.SimpleLights;
                case "electric.sirenlight":       return config.SirenLights;
                case "skull_fire_pit":		      return config.FirePits;
				case "small.oil.refinery":	      return config.Refineries;
				case "smallcandleset":		      return config.Candles;
                case "smart.alarm":               return config.SmartAlarms;
                case "smart.switch":              return config.SmartSwitches;
				case "snowmachine":			      return config.Snow_Machines;
				case "spookyspeaker":		      return config.Speakers;
				case "strobelight":			      return config.StrobeLights;
				case "tunalight":			      return config.Lanterns;
				case "hat.miner": 			      return config.Hats;
				case "hat.candle": 			      return config.Hats;
				case "xmas.lightstring.advanced": return config.Deluxe_lightstrings;
				case "water.pump":                return config.WaterPumps;
				case "poweredwaterpurifier":      return config.WaterPurifiers;
				default:
				{
					return false;
				}
			}
		}


		private void AlwaysTimerProcess()
		{
			if (config.AlwaysOn)
			{
				ProcessLights(true, "all");
				// submit for the next pass
				Alwaystimer = timer.Once(config.AlwaysCheckFrequency, AlwaysTimerProcess);
			}
		}

		private void DeviceTimerProcess()
		{
			if (config.DevicesAlwaysOn)
			{
				ProcessDevices(true, "all");
				// submit for the next pass
				Devicetimer = timer.Once(config.DeviceCheckFrequency, DeviceTimerProcess);
			}
		}

		private void NightTimerProcess()
		{
			if (config.NightToggle)
			{
				ProcessNight();
				// clear the Inital flag as we now accurately know the state
				InitialPassNight = false;
				// submit for the next pass
				Nighttimer = timer.Once(config.NightCheckFrequency, NightTimerProcess);
			}
		}

		private void ProcessNight()
		{
			var gtime = TOD_Sky.Instance.Cycle.Hour;
			if ((nightcross24 == false && gtime >= config.DuskTime && gtime < config.DawnTime) ||
				(nightcross24 && ((gtime >= config.DuskTime && gtime < 24) || gtime < config.DawnTime))
				&& (!NightToggleactive || InitialPassNight))
			{
				NightToggleactive = true;
				ProcessLights(true,"all");
				if (!config.DevicesAlwaysOn)
					ProcessDevices(true,"all");
				if (config.ConsoleMsg)
					Puts(Lang("dusk") + config.DawnTime);
			}
			else if ((nightcross24 == false &&  gtime >= config.DawnTime) ||
					(nightcross24 && (gtime <  config.DuskTime && gtime >= config.DawnTime))
					&& (NightToggleactive || InitialPassNight))
			{
				NightToggleactive = false;
				ProcessLights(false,"all");
				if (!config.DevicesAlwaysOn)
					ProcessDevices(false,"all");
				if (config.ConsoleMsg)
					Puts(Lang("dawn") + config.DuskTime);
			}
		}

		private void ProcessLights(bool state, string prefabName)
		{
			if (prefabName == "all" || IsOvenPrefabName(prefabName))
			{
				//if (string.IsNullOrEmpty(prefabName) || prefabName == "all")
				//	Puts("all lights");
				//else
				//	Puts("turing on: " + prefabName);

				BaseOven[] ovens = BaseNetworkable.serverEntities.OfType<BaseOven>().ToArray() as BaseOven[];

				foreach (BaseOven oven in ovens)
				{
					if (oven == null || oven.IsDestroyed || oven.IsOn() == state)
						continue;
					else if (state == false && ProtectShortPrefabName(prefabName))
						continue;
					//if (config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", oven as BaseEntity, "autolights"))
					//	continue;

					// not super efficient find a better way
					if (prefabName != "all" &&
					   (furnace_name.Contains(prefabName) ||
					    furnace_large_name.Contains(prefabName) ||
					    lantern_name.Contains(prefabName) ||
					    tunalight_name.Contains(prefabName) ||
					    jackolanternangry_name.Contains(prefabName) ||
					    jackolanternhappy_name.Contains(prefabName) ||
					    campfire_name.Contains(prefabName) ||
					    fireplace_name.Contains(prefabName) ||
					    bbq_name.Contains(prefabName) ||
					    cursedcauldron_name.Contains(prefabName) ||
					    skullfirepit_name.Contains(prefabName) ||
					    hobobarrel_name.Contains(prefabName) ||
					    smallrefinery_name.Contains(prefabName) ||
					    refinerysmall_name.Contains(prefabName) ||
					    chineselantern_name.Contains(prefabName)
						))
					{
						oven.SetFlag(BaseEntity.Flags.On, state);
					}
					// not super efficient find a better way
					else
					{
						string oven_name = CleanedName(oven.ShortPrefabName).ToLower();

						if ((config.Furnaces    && (furnace_name.Contains(oven_name) ||
													 furnace_large_name.Contains(oven_name))) ||
							 (config.Lanterns    && (lantern_name.Contains(oven_name) ||
													 chineselantern_name.Contains(oven_name) ||
													 tunalight_name.Contains(oven_name) ||
													 jackolanternangry_name.Contains(oven_name) ||
													 jackolanternhappy_name.Contains(oven_name))) ||
							 (config.Campfires   && campfire_name.Contains(oven_name)) ||
							 (config.Fireplaces  && fireplace_name.Contains(oven_name)) ||
							 (config.BBQs        && bbq_name.Contains(oven_name)) ||
							 (config.Cauldrons   && cursedcauldron_name.Contains(oven_name)) ||
							 (config.FirePits    && skullfirepit_name.Contains(oven_name)) ||
							 (config.HoboBarrels && hobobarrel_name.Contains(oven_name)) ||
							 (config.Refineries  && (smallrefinery_name.Contains(oven_name) || refinerysmall_name.Contains(oven_name)))
							)
						{
							oven.SetFlag(BaseEntity.Flags.On, state);
						}
					}
				}
			}

			if ((prefabName == "all" || searchlight_name.Contains(prefabName)) && config.SearchLights)
			{
				SearchLight[] searchlights = BaseNetworkable.serverEntities.OfType<SearchLight>().ToArray() as SearchLight[];

				foreach (SearchLight search_light in searchlights)
				{
					if (search_light != null && !search_light.IsDestroyed && search_light.IsOn() != state) // &&
					   //!(config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", search_light as BaseEntity, "autolights")))
					{
						if (state == false)
							search_light.UpdateHasPower(0, 1);
						else
							search_light.UpdateHasPower(200, 1);
						search_light.SetFlag(BaseEntity.Flags.On, state);
					}
				}
			}

			if ((prefabName == "all" || (largecandleset_name.Contains(prefabName) || smallcandleset_name.Contains(prefabName))) && config.Candles)
			{
				Candle[] candles = BaseNetworkable.serverEntities.OfType<Candle>().ToArray() as Candle[];
				foreach (Candle candle in candles)
				{
					if (candle != null && !candle.IsDestroyed && candle.IsOn() != state) // &&
					   //!(config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", candle as BaseEntity, "autolights")))
					{
						candle.SetFlag(BaseEntity.Flags.On, state);
						candle.lifeTimeSeconds = 999999f;
						candle.burnRate = 0.0f;
					}
				}
			}

			if ((prefabName == "all" || ceilinglight_name.Contains(prefabName)) && config.CeilingLights)
			{
				CeilingLight[] ceilinglights = BaseNetworkable.serverEntities.OfType<CeilingLight>().ToArray() as CeilingLight[];

				foreach (CeilingLight ceiling_light in ceilinglights)
				{
					if (ceiling_light != null && !ceiling_light.IsDestroyed && ceiling_light.IsOn() != state) // &&
					   //!(config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", ceiling_light as BaseEntity, "autolights")))
					{
						ceiling_light.SetFlag(BaseEntity.Flags.On, state);
					}
				}
			}

			if ((prefabName == "all" || deluxe_lightstring_name.Contains(prefabName)) && config.Deluxe_lightstrings)
			{
				AdvancedChristmasLights[] lightstring = BaseNetworkable.serverEntities.OfType<AdvancedChristmasLights>().ToArray() as AdvancedChristmasLights[];

				foreach (AdvancedChristmasLights light_string in lightstring)
				{
					//if (config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", light_string as BaseEntity, "autolights"))
					//	continue;
					if (light_string != null && !light_string.IsDestroyed) // && light_string.IsOn() != state
					{
						if (state == false)
							light_string.UpdateHasPower(0, 1);
						else
							light_string.UpdateHasPower(200, 1);
						light_string.SetFlag(BaseEntity.Flags.On, state);
						light_string.SendNetworkUpdateImmediate();
						//light_string.lengthToPowerRatio = 0.0f;
						//light_string.ClearPoints();
						//light_string.IOEntity.UpdateHasPower(200,0);
						//light_string.UpdateHasPower(200,0);
						//light_string.ConsumptionAmount = 0;
						//light_string.IOStateChanged(200,0);
						//light_string.IsPowered = true;
						//light_string.IsStyle(AdvancedChristmasLights.AnimationType.ON);
						//light_string.ConsumptionAmount = 0.0;
					}
				}
			}

            if ((prefabName == "all" || simplelight_name.Contains(prefabName)) && config.SimpleLights)
            {
                SimpleLight[] simplelights = BaseNetworkable.serverEntities.OfType<SimpleLight>().ToArray() as SimpleLight[];

                foreach (SimpleLight simple_light in simplelights)
                {
                    if (simple_light != null && !simple_light.IsDestroyed && simple_light.IsOn() != state) // &&
                    //!(config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", simple_light as BaseEntity, "autolights")))
                    {
                        simple_light.SetFlag(BaseEntity.Flags.On, state);
                    }
                }
            }

            if ((prefabName == "all" || flasherlight_name.Contains(prefabName)) && config.FlasherLights)
            {
                FlasherLight[] flasherlights = BaseNetworkable.serverEntities.OfType<FlasherLight>().ToArray() as FlasherLight[];

                foreach (FlasherLight flasher_light in flasherlights)
                {
                    if (flasher_light != null && !flasher_light.IsDestroyed && flasher_light.IsOn() != state) // &&
                    //!(config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", flasher_light as BaseEntity, "autolights")))
                    {
                        if (state == false)
                            flasher_light.UpdateHasPower(0, 1);
                        else
                            flasher_light.UpdateHasPower(200, 1);
                        flasher_light.SetFlag(BaseEntity.Flags.On, state);
                        flasher_light.SendNetworkUpdateImmediate();
                    }
                }
            }

            if ((prefabName == "all" || sirenlight_name.Contains(prefabName)) && config.SirenLights)
            {
                SirenLight[] sirenlights = BaseNetworkable.serverEntities.OfType<SirenLight>().ToArray() as SirenLight[];

                foreach (SirenLight siren_light in sirenlights)
                {
                    if (siren_light != null && !siren_light.IsDestroyed && siren_light.IsOn() != state) // &&
                    //!(config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", siren_light as BaseEntity, "autolights")))
                    {
                        if (state == false)
                            siren_light.UpdateHasPower(0, 1);
                        else
                            siren_light.UpdateHasPower(200, 1);
                        siren_light.SetFlag(BaseEntity.Flags.On, state);
                        siren_light.SendNetworkUpdateImmediate();
                    }
                }
            }
        }

		private void ProcessDevices(bool state, string prefabName)
		{
			//Puts("In ProcessDevices ");

			if ((prefabName == "all" || fogmachine_name.Contains(prefabName)) && config.Fog_Machines)
			{
				FogMachine[] fogmachines = BaseNetworkable.serverEntities.OfType<FogMachine>().ToArray() as FogMachine[];
				foreach (FogMachine fog_machine in fogmachines)
				{
					//if (config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", fog_machine as BaseEntity, "autolights"))
					//	continue;
					if (!(fog_machine == null || fog_machine.IsDestroyed))
					{
						// there is bug with IsOn so force state
						if (state) // if (fogmachine.IsOn() != state)
						{
							fog_machine.SetFlag(BaseEntity.Flags.On, state);
							fog_machine.EnableFogField();
							fog_machine.StartFogging();
							fog_machine.SetFlag(BaseEntity.Flags.On, state);
						}
						else
						{
							fog_machine.SetFlag(BaseEntity.Flags.On, state);
							fog_machine.FinishFogging();
							fog_machine.DisableNozzle();
							fog_machine.SetFlag(BaseEntity.Flags.On, state);
						}
					}
				}
			}

			//if (!string.IsNullOrEmpty(prefabName) && snowmachine_name.Contains(prefabName)) Puts ("Snow machine"); else Puts("Not snow: " + prefabName);
			//if (config.Snow_Machines) Puts("Snow is configure"); else Puts("Snow is not active");
			if ((prefabName == "all" || snowmachine_name.Contains(prefabName)) && config.Snow_Machines)
			{
				//if (state) Puts("Snow On"); else Puts("Snow Off");
				SnowMachine[] snowmachines = BaseNetworkable.serverEntities.OfType<SnowMachine>().ToArray() as SnowMachine[];
				foreach (SnowMachine snow_machine in snowmachines)
				{
					//if (config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", snow_machine as BaseEntity, "autolights"))
					//	continue;
					if (!(snow_machine == null || snow_machine.IsDestroyed))
					{
						// there is bug with IsOn so force state
						if (state) // if (fogmachine.IsOn() != state)
						{
							snow_machine.SetFlag(BaseEntity.Flags.On, state);
							snow_machine.EnableFogField();
							snow_machine.StartFogging();
							snow_machine.SetFlag(BaseEntity.Flags.On, state);
						}
						else
						{
							snow_machine.SetFlag(BaseEntity.Flags.On, state);
							snow_machine.FinishFogging();
							snow_machine.DisableNozzle();
							snow_machine.SetFlag(BaseEntity.Flags.On, state);
						}
					}
				}
			}

			if ((prefabName == "all" || cctv_name.Contains(prefabName)) && config.CCTVs)
			{
				CCTV_RC[] cctvs = BaseNetworkable.serverEntities.OfType<CCTV_RC>().ToArray() as CCTV_RC[];

				foreach (CCTV_RC cctv in cctvs)
				{
					//if (config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", cctv as BaseEntity, "autolights"))
					//	continue;
					if (cctv != null && !cctv.IsDestroyed) // && cctv.IsOn() != state
					{
						if (state == false)
							cctv.UpdateHasPower(0, 1);
						else
							cctv.UpdateHasPower(200, 1);
						cctv.SetFlag(BaseEntity.Flags.On, state);
						cctv.SendNetworkUpdateImmediate();
					}
				}
			}

			if ((prefabName == "all" || igniter_name.Contains(prefabName)) && config.Igniters)
			{
				Igniter[] igniters = BaseNetworkable.serverEntities.OfType<Igniter>().ToArray() as Igniter[];

				foreach (Igniter igniter in igniters)
				{
					//if (config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", cctv as BaseEntity, "autolights"))
					//	continue;
					if (igniter != null && !igniter.IsDestroyed) // && igniter.IsOn() != state
					{
						if (state == false)
							igniter.UpdateHasPower(0, 1);
						else
							igniter.UpdateHasPower(200, 1);
						igniter.SetFlag(BaseEntity.Flags.On, state);
						igniter.SelfDamagePerIgnite = 0.0f;
						igniter.SendNetworkUpdateImmediate();
					}
				}
			}

			if ((prefabName == "all" || vehiclelift_name.Contains(prefabName)) && config.VehicleLifts)
			{
				ModularCarGarage[] lifts = BaseNetworkable.serverEntities.OfType<ModularCarGarage>().ToArray() as ModularCarGarage[];

				foreach (ModularCarGarage lift in lifts)
				{
					//if (config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", cctv as BaseEntity, "autolights"))
					//	continue;
					if (lift != null && !lift.IsDestroyed)
					{
						if (state == false)
							lift.UpdateHasPower(0, 1);
						else
							lift.UpdateHasPower(200, 1);
						lift.SetFlag(BaseEntity.Flags.On, state);
						lift.SendNetworkUpdateImmediate();
					}
				}
			}

			if ((prefabName == "all" || heater_name.Contains(prefabName)) && config.Heaters)
			{
				ElectricalHeater[] heaters = BaseNetworkable.serverEntities.OfType<ElectricalHeater>().ToArray() as ElectricalHeater[];
				foreach (ElectricalHeater heater in heaters)
				{
					//if (config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", ElectricalHeater as BaseEntity, "autolights"))
					//	continue;
					if (heater != null && !heater.IsDestroyed) // && heater.IsOn() != state
					{
						if (state == false)
							heater.UpdateHasPower(0, 1);
						else
							heater.UpdateHasPower(200, 1);
						heater.SetFlag(BaseEntity.Flags.On, state);
						heater.SendNetworkUpdateImmediate();
					}
				}
			}

			if ((prefabName == "all" || smart_alarm_name.Contains(prefabName)) && config.SmartAlarms)
			{
				SmartAlarm[] smartalarms = BaseNetworkable.serverEntities.OfType<SmartAlarm>().ToArray() as SmartAlarm[];

				foreach (SmartAlarm smartalarm in smartalarms)
				{
					//if (config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", ElectricalHeater as BaseEntity, "autolights"))
					//	continue;
					if (smartalarm != null && !smartalarm.IsDestroyed) // && heater.IsOn() != state
					{
						if (state == false)
							smartalarm.UpdateHasPower(0, 0);
						else
							smartalarm.UpdateHasPower(200, 0);
						smartalarm.SetFlag(BaseEntity.Flags.On, state);
						smartalarm.SendNetworkUpdateImmediate();
					}
				}
			}

			if ((prefabName == "all" || smart_switch_name.Contains(prefabName)) && config.SmartSwitches)
			{
				SmartSwitch[] smartswitches = BaseNetworkable.serverEntities.OfType<SmartSwitch>().ToArray() as SmartSwitch[];

				foreach (SmartSwitch smartswitch in smartswitches)
				{
					//if (config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", ElectricalHeater as BaseEntity, "autolights"))
					//	continue;
					if (smartswitch != null && !smartswitch.IsDestroyed) // && heater.IsOn() != state
					{
						if (state == false)
							smartswitch.UpdateHasPower(0, 0);
						else
							smartswitch.UpdateHasPower(200, 0);
						smartswitch.SetFlag(BaseEntity.Flags.On, state);
						smartswitch.SendNetworkUpdateImmediate();
					}
				}
			}

			if ((prefabName == "all" || waterpump_name.Contains(prefabName)) && config.WaterPumps)
			{
				WaterPump[] waterpumps = BaseNetworkable.serverEntities.OfType<WaterPump>().ToArray() as WaterPump[];

				foreach (WaterPump waterpump in waterpumps)
				{
					//if (config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", WaterPump as BaseEntity, "autolights"))
					//	continue;
					if (waterpump != null && !waterpump.IsDestroyed) // && waterpump.IsOn() != state
					{
						if (state == false)
						{
							waterpump.PowerConsumption = 10;
							waterpump.UpdateHasPower(0, 0);						}
						else
						{
							waterpump.PowerConsumption = 0;
							waterpump.UpdateHasPower(200, 0);
						}
						waterpump.SetFlag(BaseEntity.Flags.Reserved7, false); // short circuit
						waterpump.SetFlag(BaseEntity.Flags.Reserved8, state);  // has power
						waterpump.SendNetworkUpdateImmediate();
					}
				}
			}

			if ((prefabName == "all" || water_purifier_name.Contains(prefabName)) && config.WaterPurifiers)
			{
				WaterPurifier[] waterpurifiers = BaseNetworkable.serverEntities.OfType<WaterPurifier>().ToArray() as WaterPurifier[];

				foreach (WaterPurifier waterpurifier in waterpurifiers)
				{
					//if (config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", WaterPurifier as BaseEntity, "autolights"))
					//	continue;
					if (waterpurifier != null && !waterpurifier.IsDestroyed) // && waterpurifier.IsOn() != state
					{
						if (state == false)
							waterpurifier.UpdateHasPower(0, 1);
						else
							waterpurifier.UpdateHasPower(200, 1);

						waterpurifier.SetFlag(BaseEntity.Flags.On, state);
						waterpurifier.SendNetworkUpdateImmediate();
					}
				}
			}

			if ((prefabName == "all" || fluidswitch_name.Contains(prefabName)) && config.FluidSwitches)
			{
				FluidSwitch[] fluidswitches = BaseNetworkable.serverEntities.OfType<FluidSwitch>().ToArray() as FluidSwitch[];

				foreach (FluidSwitch fluidswitch in fluidswitches)
				{
					//if (config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", cctv as BaseEntity, "autolights"))
					//	continue;
					if (fluidswitch != null && !fluidswitch.IsDestroyed) // && fluidswitch.IsOn() != state
					{
						if (state == false)
							fluidswitch.UpdateHasPower(0, 1);
						else
							fluidswitch.UpdateHasPower(200, 1);
						fluidswitch.SetFlag(BaseEntity.Flags.On, state);
						fluidswitch.SendNetworkUpdateImmediate();
					}
				}
			}

			if ((prefabName == "all" || strobelight_name.Contains(prefabName)) && config.StrobeLights)
			{
				StrobeLight[] strobelights = BaseNetworkable.serverEntities.OfType<StrobeLight>().ToArray() as StrobeLight[];
				foreach (StrobeLight strobelight in strobelights)
				{
					//if (config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", strobelight as BaseEntity, "autolights"))
					//	continue;
					if (!(strobelight == null || strobelight.IsDestroyed) && strobelight.IsOn() != state)
					{
						strobelight.SetFlag(BaseEntity.Flags.On, state);
						strobelight.burnRate = 0.0f;
						strobelight.SendNetworkUpdateImmediate();
					}
				}
			}

			if ((prefabName == "all" || spookyspeaker_name.Contains(prefabName)) && config.Speakers)
			{
				SpookySpeaker[] spookyspeakers = BaseNetworkable.serverEntities.OfType<SpookySpeaker>().ToArray() as SpookySpeaker[];
				foreach (SpookySpeaker spookyspeaker in spookyspeakers)
				{
					//if (config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", spookyspeaker as BaseEntity, "autolights"))
					//	continue;
					if (!(spookyspeaker == null || spookyspeaker.IsDestroyed) && spookyspeaker.IsOn() != state)
					{
						spookyspeaker.SetFlag(BaseEntity.Flags.On, state);
						if (state == true)
						{
							spookyspeaker.SendPlaySound();
						}
					}
				}
			}
		}
		private object OnFindBurnable(BaseOven oven)
		{
			bool hasperm = false;

			if (oven == null || string.IsNullOrEmpty(oven.ShortPrefabName) ||
				oven.OwnerID == 0U || oven.OwnerID.ToString() == null)
				return null;

			if (!ProcessShortPrefabName(oven.ShortPrefabName) ||	!IsLightPrefabName(oven.ShortPrefabName))
				return null;
			//else if (config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", oven as BaseEntity, "autolights"))
			//	return null;
			else
			{
				//Puts("OnFindBurnable: " + oven.ShortPrefabName + " : " + oven.cookingTemperature);
				oven.StopCooking();
				oven.allowByproductCreation = false;
				oven.SetFlag(BaseEntity.Flags.On, true);
				if (oven.fuelType != null)
					return ItemManager.CreateByItemID(oven.fuelType.itemid);
				else
					return null;
			}
			// catch all
			return null;
		}

		// for jack o laterns
		private void OnConsumeFuel(BaseOven oven, Item fuel, ItemModBurnable burnable)
		{
			if (oven == null || string.IsNullOrEmpty(oven.ShortPrefabName) ||
				oven.OwnerID == 0U || oven.OwnerID.ToString() == null)
				return;

			if (!ProcessShortPrefabName(oven.ShortPrefabName) ||
				!IsLightPrefabName(oven.ShortPrefabName))
				return;
			//else if (config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", oven as BaseEntity, "autolights"))
			//	return;
			else
			{
				fuel.amount += 1;
				oven.StopCooking();
				oven.allowByproductCreation = false;
				oven.SetFlag(BaseEntity.Flags.On, true);
			}
			// catch all
			return;
		}

		// for hats
		private void OnItemUse(Item item, int amount)
		{
			if (!config.Hats) return;
			string ShortPrefabName = item?.parent?.parent?.info?.shortname ?? item?.GetRootContainer()?.entityOwner?.ShortPrefabName;
			BasePlayer player = null;

			if (string.IsNullOrEmpty(ShortPrefabName) || !IsHatPrefabName(ShortPrefabName) || !config.Hats)
				return;
			try
			{
				player = item?.GetRootContainer()?.playerOwner;
			}
			catch
			{
				player = null;
			}

			if (player == null && string.IsNullOrEmpty(player.UserIDString))
			{
				return;  // no owner so no permission
			}
			item.amount += amount;
			return;
		}

		object OnOvenToggle(BaseOven oven, BasePlayer player)
		{
			string cleanedname = null;
			if (oven == null || string.IsNullOrEmpty(oven.ShortPrefabName) ||
				player == null || player.UserIDString == null)
				return null;
			else
			{
				cleanedname = CleanedName(oven.ShortPrefabName);
				//Puts(oven.ShortPrefabName + " : " + cleanedname + " : " + oven.IsOn());
			}
			//if (config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", oven as BaseEntity, "autolights"))
			//	return null;
			if (!IsLightToggle(cleanedname)
				//(!IsLightPrefabName(cleanedname)) // && !(IsOvenPrefabName(cleanedname) && ProcessShortPrefabName(oven.ShortPrefabName))))
				)
			{
				return null;
			}
			if (!ProcessShortPrefabName(oven.ShortPrefabName))
				return null;
			else if (oven.IsOn() != true)
			{
				//Puts("off going on and allowed " +  oven.temperature.ToString() + " : " + oven.cookingTemperature);
				oven.SetFlag(BaseEntity.Flags.On, true);
				oven.StopCooking();
				oven.allowByproductCreation = false;
				oven.SetFlag(BaseEntity.Flags.On, true);
			}
			else
			{
				//Puts("on going off and allowed " +  oven.temperature.ToString() + " : " + oven.cookingTemperature);
				oven.SetFlag(BaseEntity.Flags.On, false);
				oven.StopCooking();
				oven.SetFlag(BaseEntity.Flags.On, false);
			}
			// catch all
			return null;
		}

		void LightStringChangePower()
		{
			if ((config.AlwaysOn || NightToggleactive) && config.Deluxe_lightstrings)
			{
				foreach (AdvancedChristmasLights light_string in BaseNetworkable.serverEntities.OfType<AdvancedChristmasLights>())
				{
				//if (config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", light_string as BaseEntity, "autolights"))
				//	continue;
					light_string.UpdateHasPower(200, 1);
					light_string.SetFlag(BaseEntity.Flags.On, true);
					light_string.SendNetworkUpdateImmediate();
				}
			}
		}

        void FlasherLightChangePower()
        {
			if ((config.AlwaysOn || NightToggleactive) && config.FlasherLights)
			{
				foreach (FlasherLight flasherlight in BaseNetworkable.serverEntities.OfType<FlasherLight>())
				{
                //if (config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", flasherlight as BaseEntity, "autolights"))
                //	continue;
                    flasherlight.UpdateHasPower(200, 1);
                    flasherlight.SetFlag(BaseEntity.Flags.On, true);
                    flasherlight.SendNetworkUpdateImmediate();
                }
            }
        }

        void SirenLightChangePower()
        {
			if ((config.AlwaysOn || NightToggleactive) && config.SirenLights)
			{
				foreach (SirenLight sirenlight in BaseNetworkable.serverEntities.OfType<SirenLight>())
				{
                //if (config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", sirenlight as BaseEntity, "autolights"))
                //	continue;
                    sirenlight.UpdateHasPower(200, 1);
                    sirenlight.SetFlag(BaseEntity.Flags.On, true);
                    sirenlight.SendNetworkUpdateImmediate();
                }
            }
        }

        void SmartAlarmChangePower()
        {
			if (config.SmartAlarms)
			{
				foreach (SmartAlarm smartalarm in BaseNetworkable.serverEntities.OfType<SmartAlarm>())
				{
                //if (config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", sirenlight as BaseEntity, "autolights"))
                //	continue;

                    smartalarm.UpdateHasPower(200, 0);
                    smartalarm.SetFlag(BaseEntity.Flags.On, true);
                    smartalarm.SendNetworkUpdateImmediate();
                }
            }
        }

        void SmartSwitchChangePower()
        {
			if (config.SmartSwitches)
			{
				foreach (SmartSwitch smartswitch in BaseNetworkable.serverEntities.OfType<SmartSwitch>())
				{
                //if (config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", sirenlight as BaseEntity, "autolights"))
                //	continue;

                    smartswitch.UpdateHasPower(200, 0);
                    smartswitch.SetFlag(BaseEntity.Flags.On, true);
                    smartswitch.SendNetworkUpdateImmediate();
                }
            }
        }

        void CCTVChangePower()
		{
			if (config.CCTVs)
			{
				foreach (CCTV_RC cctv in BaseNetworkable.serverEntities.OfType<CCTV_RC>())
				{
				//if (config.UseZoneManagerPlugin && !(bool)ZoneManager?.Call("EntityHasFlag", cctv as BaseEntity, "autolights"))
				//	continue;
					cctv.UpdateHasPower(200, 1);
					cctv.SetFlag(BaseEntity.Flags.On, true);
					cctv.SendNetworkUpdateImmediate();
				}
			}
		}

		void IgniterChangePower()
		{
			if (config.Igniters)
			{
				foreach (Igniter igniter in BaseNetworkable.serverEntities.OfType<Igniter>())
				{
					igniter.UpdateHasPower(200, 1);
					igniter.SetFlag(BaseEntity.Flags.On, true);
					igniter.SelfDamagePerIgnite = 0.0f;
					igniter.SendNetworkUpdateImmediate();
				}
			}
		}

		void VehicleLiftChangePower()
		{
			if (config.VehicleLifts)
			{
				foreach (ModularCarGarage lift in BaseNetworkable.serverEntities.OfType<ModularCarGarage>())
				{
					lift.UpdateHasPower(200, 1);
					lift.SetFlag(BaseEntity.Flags.On, true);
					lift.SendNetworkUpdateImmediate();
				}
			}
		}

		void HeaterChangePower()
		{
			if (config.Heaters)
			{
				foreach (ElectricalHeater heater in BaseNetworkable.serverEntities.OfType<ElectricalHeater>())
				{
					heater.UpdateHasPower(200, 1);
					heater.SetFlag(BaseEntity.Flags.On, true);
					heater.SendNetworkUpdateImmediate();
				}
			}
		}

		void SearchLightChangePower()
		{
			if (config.Heaters)
			{
				foreach (SearchLight searchlight in BaseNetworkable.serverEntities.OfType<SearchLight>())
				{
					searchlight.UpdateHasPower(200, 1);
					searchlight.IOStateChanged(200, 100);
					searchlight.SetFlag(BaseEntity.Flags.On, true);
					searchlight.SendNetworkUpdateImmediate();
				}
			}
		}

		void WaterPumpChangePower()
		{
			if (config.WaterPumps)
			{
				foreach (WaterPump waterpump in BaseNetworkable.serverEntities.OfType<WaterPump>())
				{
					waterpump.PowerConsumption = 0;
					waterpump.UpdateHasPower(30, 0);
					waterpump.SetFlag(BaseEntity.Flags.On, true);
					waterpump.SetFlag(BaseEntity.Flags.Reserved6, true);
					waterpump.SendNetworkUpdateImmediate();
				}
			}
		}

		void WaterPurifierChangePower()
		{
			if (config.WaterPurifiers)
			{
				foreach (WaterPurifier waterpurifier in BaseNetworkable.serverEntities.OfType<WaterPurifier>())
				{
					waterpurifier.UpdateHasPower(200, 1);
					waterpurifier.SetFlag(BaseEntity.Flags.On, true);
					waterpurifier.SendNetworkUpdateImmediate();
				}
			}
		}

		void FluidSwitchChangePower()
		{
			if (config.FluidSwitches)
			{
				foreach (FluidSwitch fluidswitch in BaseNetworkable.serverEntities.OfType<FluidSwitch>())
				{
					fluidswitch.UpdateHasPower(200, 1);
                    fluidswitch.SetFlag(BaseEntity.Flags.On, true);
                    fluidswitch.SendNetworkUpdateImmediate();
				}
			}
		}

		void OnEntityBuilt(Planner plan, GameObject go)
        {
            FluidSwitch fs = go.GetComponent<FluidSwitch>();
            if (fs != null && config.FluidSwitches)
			{
				fs.UpdateHasPower(30, 0);
				fs.SetFlag(BaseEntity.Flags.On, true);
				fs.SetFlag(BaseEntity.Flags.Reserved6, true);
				fs.SendNetworkUpdateImmediate();
				return;
			}
            SmartAlarm sa = go.GetComponent<SmartAlarm>();
            if (sa != null && config.SmartAlarms)
            {
				sa.UpdateHasPower(30, 0);
				sa.SetFlag(BaseEntity.Flags.On, true);
				sa.SendNetworkUpdateImmediate();            
				return;
			}
        }

		// automatically set lights on that are deployed if the lights are in the on state
		// private void OnItemDeployed(Deployer deployer, BaseEntity entity)
		private void OnEntitySpawned(BaseNetworkable entity)
		{
			if (!ProcessShortPrefabName(entity.ShortPrefabName))
				return;
			//Puts("OES: " + entity.ShortPrefabName);

			if (entity is CCTV_RC && config.CCTVs)
			{
				var cctv = entity as CCTV_RC;
				cctv.UpdateHasPower(200, 1);
				cctv.SetFlag(BaseEntity.Flags.On, true);
				cctv.SendNetworkUpdateImmediate();
				return;
			}
			if (entity is ElectricalHeater && config.Heaters)
			{
				var heater = entity as ElectricalHeater;
				heater.UpdateHasPower(200, 1);
				heater.SetFlag(BaseEntity.Flags.On, true);
				heater.SendNetworkUpdateImmediate();
				return;
			}
			if (entity is WaterPump && config.WaterPumps)
			{
				var waterpump = entity as WaterPump;
				//waterpump.UpdateHasPower(200, 1);
				waterpump.IOStateChanged(999999, 0);
				//waterpump.SetFlag(BaseEntity.Flags.On, true);
				waterpump.SetFlag(BaseEntity.Flags.Reserved7, false); // short circuit
				waterpump.SetFlag(BaseEntity.Flags.Reserved8, true);  // has power
				waterpump.SendNetworkUpdateImmediate();
				return;
			}
			if (entity is WaterPurifier && config.WaterPurifiers)
			{
				var waterpurifier = entity as WaterPurifier;
				waterpurifier.UpdateHasPower(200, 1);
				waterpurifier.SetFlag(BaseEntity.Flags.On, true);
				waterpurifier.SendNetworkUpdateImmediate();
				return;
			}
			if (entity is FluidSwitch && config.FluidSwitches)
			{
				//Puts("fluidswitch");
				var fluidswitch = entity as FluidSwitch;
				fluidswitch.SetFlag(BaseEntity.Flags.On, true);
				fluidswitch.UpdateHasPower(200, 1);
				fluidswitch.SetFlag( BaseEntity.Flags.Reserved7, false );  // Flag Short Circuit
				fluidswitch.SetFlag( BaseEntity.Flags.Reserved8, true );   // Flag Has Power
				fluidswitch.SendNetworkUpdateImmediate();
				return;
			}
			if (entity is Igniter && config.Igniters)
			{
				var igniter = entity as Igniter;
				igniter.SelfDamagePerIgnite = 0.0f;
				igniter.UpdateHasPower(200, 1);
				igniter.SetFlag(BaseEntity.Flags.On, true);
				igniter.SendNetworkUpdateImmediate();
				return;
			}
			if (config.AlwaysOn || NightToggleactive)
			{
				if (entity is BaseOven)
				{
					var bo = entity as BaseOven;
					bo.SetFlag(BaseEntity.Flags.On, true);
					bo.SendNetworkUpdateImmediate();
					return;
				}
				if (entity is CeilingLight && config.CeilingLights)
				{
					var cl = entity as CeilingLight;
					cl.UpdateHasPower(200, 1);
					cl.SetFlag(BaseEntity.Flags.On, true);
					cl.SendNetworkUpdateImmediate();
					return;
				}
                if (entity is FlasherLight && config.FlasherLights)
                {
                    var fl = entity as FlasherLight;
                    fl.UpdateHasPower(200, 1);
                    fl.SetFlag(BaseEntity.Flags.On, true);
                    fl.SendNetworkUpdateImmediate();
					return;
                }
                if (entity is SimpleLight && config.SimpleLights)
                {
                    var sl = entity as SimpleLight;
                    sl.UpdateHasPower(200, 1);
                    sl.SetFlag(BaseEntity.Flags.On, true);
                    sl.SendNetworkUpdateImmediate();
					return;
                }
                if (entity is SirenLight && config.SirenLights)
                {
                    var sil = entity as SirenLight;
                    sil.UpdateHasPower(200, 1);
                    sil.SetFlag(BaseEntity.Flags.On, true);
                    sil.SendNetworkUpdateImmediate();
					return;
                }
                if (entity is SmartAlarm && config.SmartAlarms)
                {
                    var sa = entity as SmartAlarm;
                    sa.UpdateHasPower(200, 1);
                    sa.SetFlag(BaseEntity.Flags.On, true);
                    sa.SendNetworkUpdateImmediate();
					return;
                }
                if (entity is SmartSwitch && config.SmartSwitches)
                {
                    var ss = entity as SmartSwitch;
                    ss.UpdateHasPower(200, 1);
                    ss.SetFlag(BaseEntity.Flags.On, true);
                    ss.SendNetworkUpdateImmediate();
					return;
                }
                if (entity is SearchLight && config.SearchLights)
				{
					var sel = entity as SearchLight;
                    sel.UpdateHasPower(200, 1);
                    sel.SetFlag(BaseEntity.Flags.On, true);
                    sel.SendNetworkUpdateImmediate();
					return;
				}
				if (entity is Candle && config.Candles)
				{
					var candle = entity as Candle;
					candle.lifeTimeSeconds = 999999f;
					candle.burnRate = 0.0f;
					candle.SetFlag(BaseEntity.Flags.On, true);
					candle.SendNetworkUpdateImmediate();
					return;
				}
				if (entity is AdvancedChristmasLights && config.Deluxe_lightstrings)
				{
					var light_string = entity as AdvancedChristmasLights;
					light_string.UpdateHasPower(200, 1);
					light_string.SetFlag(BaseEntity.Flags.On, true);
					light_string.SendNetworkUpdateImmediate();
					return;
				}
			}
			if (config.DevicesAlwaysOn || NightToggleactive)
			{
				if (entity is FogMachine && config.Fog_Machines)
				{
					var fm = entity as FogMachine;
					fm.SetFlag(BaseEntity.Flags.On, true);
					fm.EnableFogField();
					fm.StartFogging();
					return;
				}
				if (entity is SnowMachine && config.Snow_Machines)
				{
					var sl = entity as SnowMachine;
					sl.SetFlag(BaseEntity.Flags.On, true);
					return;
				}
				if (entity is StrobeLight && config.StrobeLights)
				{
					var sl = entity as StrobeLight;
					sl.burnRate = 0.0f;
					sl.SetFlag(BaseEntity.Flags.On, true);
					sl.SendNetworkUpdateImmediate();
					return;
				}
				if (entity is SpookySpeaker  && config.Speakers)
				{
					var ss = entity as SpookySpeaker;
					ss.SetFlag(BaseEntity.Flags.On, true);
					ss.SendPlaySound();
					return;
				}
			}
		}

		[Command("lights")]
		private void ChatCommandlo(IPlayer player, string cmd, string[] args)
		{
			if (!permission.UserHasPermission(player.Id, perm_lightson))
			{
				player.Message(String.Concat(Lang("prefix", player.Id), Lang("nopermission", player.Id)));
				return;
			}
			else if (args == null || args.Length < 1)
			{
				player.Message(String.Concat(Lang("prefix", player.Id), Lang("syntax", player.Id)));
				return;
			}

			bool   state		= false;
			string statestring	= null;
			string prefabName	= null;

			// set the parameters
			statestring = args[0].ToLower();

			// make sure we have something to process default to all on
			if (string.IsNullOrEmpty(statestring))
				state = true;
			else if (statestring == "off" || statestring == "false" || statestring == "0" || statestring == "out")
				state = false;
			else if (statestring == "on" || statestring == "true" || statestring == "1" || statestring == "go")
				state = true;
			else
			{
				player.Message(String.Concat(Lang("prefix", player.Id), Lang("state", player.Id)) + " " + statestring);
				return;
			}

			// see if there is a prefabname specified and if so that it is valid
			if (args.Length > 1)
			{
				prefabName = CleanedName(args[1].ToLower());

				if(string.IsNullOrEmpty(prefabName))
					prefabName = "all";
				else if (prefabName.ToUpper() != "ALL" &&
						!IsLightPrefabName(prefabName) &&
						!CanCookShortPrefabName(prefabName) &&
						!IsDevicePrefabName(prefabName)
						)
				{
					player.Message(String.Concat(Lang("prefix") , Lang("bad prefab", player.Id))+ " " + prefabName);
					return;
				}
			}
			else
				prefabName = "all";

			if (prefabName == "all")
			{
				ProcessLights(state, prefabName);
				ProcessDevices(state, prefabName);
			}
			else
			{
				if (IsDevicePrefabName(prefabName))
					ProcessDevices(state, prefabName);
				if (IsLightPrefabName(prefabName) || CanCookShortPrefabName(CleanedName(prefabName)))
					ProcessLights(state, prefabName);
			}

			if (state)
				player.Message(String.Concat(Lang("prefix") , Lang("lights on", player.Id)) + " " + prefabName);
			else
				player.Message(String.Concat(Lang("prefix") , Lang("lights off", player.Id)) + " " + prefabName);
		}
	}
}
