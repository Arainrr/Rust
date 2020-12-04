using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Linq;
using System.Reflection;
using Oxide.Core.Plugins;
using Oxide.Core.CSharp;
using Oxide.Core;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
[Info("Rust Rewards", "MSpeedie", "2.2.72")]
[Description("Rewards players for activities using Economic, ServerRewards or Scrap")]
// Big Thank you to Tarek the original author of this plugin!
// redBDGR, for maintaining the Barrel Points plugin
// Scriptzyy, the original author of the Barrel Points plugin
// Mr. Bubbles, the original author of the Gather Rewards plugin
// CanopySheep and Wulf, for maintaining the Gather Rewards plugin

public class RustRewards : RustPlugin
{
	[PluginReference] Plugin Economics;
	[PluginReference] Plugin ServerRewards;
	[PluginReference] Plugin Clans;
	[PluginReference] Plugin Friends;
	[PluginReference] Plugin ZoneManager;
	[PluginReference] Plugin GUIAnnouncements;

    public Oxide.Core.VersionNumber VersionNum { get; set; }
	readonly DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetFile("RustRewards");
	readonly DynamicConfigFile zdataFile = Interface.Oxide.DataFileSystem.GetFile("RustRewards-Zones");
	private Timer Cleanertimer; // used to clean up tracking of folks damaging entities
	private Timer timecheck;

	// currency formatting
	readonly private CultureInfo CurrencyCulture = CultureInfo.CreateSpecificCulture("en-US");  // change this to change the currency symbol

	private const string HarvestPermission  = "rustrewards.harvest";
	private const string KillPermission     = "rustrewards.kill";
	private const string OpenPermission     = "rustrewards.open";
	private const string PickupPermission   = "rustrewards.pickup";
	private const string ActivityPermission = "rustrewards.activity";
	private const string WelcomePermission  = "rustrewards.welcome";

	private struct TrackPlayer
	{
		public IPlayer iplayer;
		public float   time;  // using TOD_Sky.Instance.Cycle.Hour
	}
	readonly private float shotclock = 0.9f;  // how long after an action is done is it ignored in "hours" (to function that process this has some interesting decimal math)

	private Dictionary<uint, TrackPlayer> EntityCollectionCache = new Dictionary<uint, TrackPlayer>(); // float has TOD_Sky.Instance.Cycle.Hour")
	private Dictionary<string, string> playerPrefs = new Dictionary<string, string>();
	private Dictionary<string, double> zonemultipliers = new Dictionary<string, double>();
	private Dictionary<string, double> groupmultiplier = new Dictionary<string, double>();

	const string permVIP = "rustrewards.vip";

	// to indicate I need to update the json file
	bool _didConfigChange;

	//private Oxide.Core.VersionNumber ConfigVersion;

	private bool serverrewardsloaded = false;
	private bool economicsloaded = false;
	private bool clansloaded = false;
	private bool friendsloaded = false;
	private bool GUIAnnouncementsloaded = false;
	private bool zonemanagerloaded = false;
	private bool happyhouractive = false;
	private bool happyhourcross24 = false;
	private bool NPCReward_Enabled = true;
	private bool VIPMultiplier_Enabled = false;
	private bool ActivityReward_Enabled = false;
	private bool Simpler_ActivityReward_Enabled = false;
	private bool OpenReward_Enabled = false;
	private bool KillReward_Enabled = false;
	private bool HarvestReward_Enabled = false;
	private bool PickupReward_Enabled = false;
	private bool WelcomeMoney_Enabled = false;
	private bool WeaponMultiplier_Enabled = false;
	private bool DistanceMultiplier_Enabled = false;
	private bool DynamicDistanceMultiplier_Enabled = false;
	private bool UseGUIAnnouncementsForRewards = false;
	private bool UseGUIAnnouncementsPlugin = false;
	private bool UseEconomicsPlugin = false;
	private bool UseServerRewardsPlugin = true;
	private bool UseScrap = false;
	private bool UseFriendsPlugin = true;
	private bool UseClansPlugin = true;
	private bool UseTeams = true;
	private bool UseZoneManagerPlugin = true;
	private bool TakeMoneyfromVictim = false;
	private bool PrintInConsole = false;
	private bool DoLogging = true;
	private bool DoAdvancedVIP = false;
	private bool ShowcurrencySymbol = true;
	private bool HappyHour_Enabled = true;
	private bool Permissions_Enabled = true;

	private int ActivityReward_Minutes = 15;
	private int ActivityReward_Seconds = 900;
	private int HappyHour_BeginHour = 18;
	private int HappyHour_EndHour = 21;

	private double mult_40mm_grenade_he;
	private double mult_40mm_grenade_buckshot;
	private double mult_assaultrifle = 1.0;
	private double mult_axe_salvaged = 1.0;
	private double mult_beancangrenade = 1.0;
	private double mult_boltactionrifle = 1.0;
	private double mult_boneclub = 1.0;
	private double mult_boneknife = 1.0;
	private double mult_butcherknife = 1.0;
	private double mult_candycaneclub = 1.0;
	private double mult_chainsaw = 1.0;
	private double mult_combatknife = 1.0;
	private double mult_compoundbow = 1.0;
	private double mult_crossbow = 1.0;
	private double mult_customsmg = 1.0;
	private double mult_doublebarrelshotgun = 1.0;
	private double mult_eokapistol = 1.0;
	private double mult_f1grenade = 1.0;
	private double mult_flamethrower = 1.0;
	private double mult_flashlight = 1.0;
	private double mult_hammer_salvaged = 1.0;
	private double mult_handmadefishingrod = 1.0;
	private double mult_hatchet = 1.0;
	private double mult_huntingbow = 1.0;
	private double mult_icepick_salvaged = 1.0;
	private double mult_jackhammer = 1.0;
	private double mult_l96rifle = 1.0;
	private double mult_longsword = 1.0;
	private double mult_lr300 = 1.0;
	private double mult_m249 = 1.0;
	private double mult_m39 = 1.0;
	private double mult_m92pistol = 1.0;
	private double mult_mace = 1.0;
	private double mult_machete = 1.0;
	private double mult_multiplegrenadelauncher = 1.0;
	private double mult_mp5a4 = 1.0;
	private double mult_nailgun = 1.0;
	private double mult_pickaxe = 1.0;
	private double mult_pitchfork = 1.0;
	private double mult_pumpshotgun = 1.0;
	private double mult_pythonrevolver = 1.0;
	private double mult_revolver = 1.0;
	private double mult_rocket = 1.0;
	private double mult_rock = 1.0;
	private double mult_rocketlauncher = 1.0;
	private double mult_salvagedcleaver = 1.0;
	private double mult_salvagedsword = 1.0;
	private double mult_satchelcharge = 1.0;
	private double mult_semiautomaticpistol = 1.0;
	private double mult_semiautomaticrifle = 1.0;
	private double mult_sickle = 1.0;
	private double mult_snowball = 1.0;
	private double mult_spas12shotgun = 1.0;
	private double mult_stone_pickaxe = 1.0;
	private double mult_stonehatchet = 1.0;
	private double mult_stonespear = 1.0;
	private double mult_thompson = 1.0;
	private double mult_timedexplosivecharge = 1.0;
	private double mult_torch = 1.0;
	private double mult_waterpipeshotgun = 1.0;
	private double mult_woodenspear = 1.0;
	private double mult_VIPMultiplier = 1.0;
	private double mult_HappyHourMultiplier = 1.0;
	private double mult_dynamicdistance = 0.01;
	private double mult_distance_100 = 1.0;
	private double mult_distance_200 = 1.0;
	private double mult_distance_300 = 1.0;
	private double mult_distance_400 = 1.0;
	private double mult_distance_50 = 1.0;

	private double rate_sam = 1.0;
	private double rate_trap = 1.0;
	private double rate_autoturret = 1.0;
	private double rate_barrel = 1.0;
	private double rate_balloon = 1.0;
	private double rate_bear = 1.0;
	private double rate_berry = 1.0;
	private double rate_boar = 1.0;
	private double rate_boat = 1.0;
	private double rate_bones = 1.0;
	private double rate_bradley = 1.0;
	private double rate_cactus = 1.0;
	private double rate_chicken = 1.0;
	private double rate_chinook = 1.0;
	private double rate_corn = 1.0;
	private double rate_crate = 1.0;
	private double rate_foodbox = 1.0;
	private double rate_giftbox = 1.0;
	private double rate_helicopter = 1.0;
	private double rate_hemp = 1.0;
	private double rate_horse = 1.0;
	private double rate_player = 1.0;
	private double rate_suicide = -1.0;
	private double rate_minicopter = 1.0;
	private double rate_murderer = 1.0;
	private double rate_minecart = 1.0;
	private double rate_mushrooms = 1.0;
	private double rate_ore = 1.0;
	private double rate_potato = 1.0;
	private double rate_pumpkin = 1.0;
	private double rate_ridablehorse = -1.0;
	private double rate_rhib = 1.0;
	private double rate_scrapcopter = 1.0;
	private double rate_scientist = 1.0;
	private double rate_heavyscientist = 1.0;
	private double rate_bandit_guard = 1.0;
	private double rate_scientist_peacekeeper = 1.0;
	private double rate_scarecrow = 1.0;
	private double rate_stag = 1.0;
	private double rate_stones = 1.0;
	private double rate_sulfur = 1.0;
	private double rate_supplycrate = 1.0;
	private double rate_wolf = 1.0;
	private double rate_wood = 1.0;
	private double rate_npckill = 1.0;
	private double rate_activityreward = 1.0;
	private double rate_welcomemoney = 1.0;

	private string player_default_settings = "hkopa";
	private string prestring = "<color=#CCBB00>";
	private string midstring =  "</color><color=#FFFFFF>";
	private string poststring = "</color>";
	private string blnk = " ";
	private string GUIA_BannerColor = "#CCBB00";
	private string GUIA_TextColor   = "#FFFFFF";

	private Dictionary<IPlayer, float> Activity_Reward = new Dictionary<IPlayer, float>();

	protected override void LoadDefaultConfig() { }

	object GetConfigValue(string category, string setting, object defaultValue)
	{
		Dictionary<string, object> data = new Dictionary<string, object>();
		object value = defaultValue;
		if (category == null || category == String.Empty)
		{
			Puts("Tell MSpeedie No Category for config");
		}
		if (setting == null || setting == String.Empty)
		{
			Puts("Tell MSpeedie No Setting for config");
		}

		try
		{
			data = Config[category] as Dictionary<string, object>;
		}
		catch
		{
			Puts("Tell MSpeedie Error getting config");
		}

		if (data == null)
		{
			data = new Dictionary<string, object>();
			Config[category] = data;
			_didConfigChange = true;
		}

		try
		{
			if (data.TryGetValue(setting, out value)) return value;

		}
		catch
		{
			value = defaultValue;
		}

		value = defaultValue;
		data[setting] = value;
		_didConfigChange = true;
		return value;
	}

    private void CheckCfg<T>(string Key, ref T var)
    {
        if (Config[Key] is T)
            var = (T)Config[Key];
        else
            Config[Key] = var;
    }

	object SetConfigValue(string category, string setting, object defaultValue)
	{
		var data = Config[category] as Dictionary<string, object>;
		object value;

		if (data == null)
		{
			data = new Dictionary<string, object>();
			Config[category] = data;
			_didConfigChange = true;
		}

		value = defaultValue;
		data[setting] = value;
		_didConfigChange = true;
		return value;
	}

	private void CheckConfig()
	{
		if (VIPMultiplier_Enabled && DoAdvancedVIP)
		{
			Puts("Warning: you are running VIP Multiplier enabled and Do Advanced VIP which can lead to big multipliers!");
		}

		if (DoAdvancedVIP && groupmultiplier == null)
		{
			Puts("Error: You have selected Do Advanced VIP but did not specify and group with rates");
		}

		if (!UseEconomicsPlugin && !UseServerRewardsPlugin && !UseScrap)
			PrintWarning("Error: You need to select Scrap, Economics or ServerReward or this plugin is pointless.");

		if (UseEconomicsPlugin && UseServerRewardsPlugin)
		{
			PrintWarning("Error: You need to select Scrap, Economics or ServerReward but not both!");
			if (Economics.IsLoaded == true)
			{
				UseServerRewardsPlugin = false;
				Puts("Warning: Switched to Economics as it is loaded");
			}
			else if (ServerRewards.IsLoaded == true)
			{
				UseEconomicsPlugin = false;
				Puts("Warning: Switched to Server Rewards as Economics is not loaded");
			}
			else
			{
				UseScrap = true;
				Puts("Warning: Switched to Scrap since Server Rewards and Economics is not loaded");
			}
		}

		try
		{
			economicsloaded = false;
			if (UseEconomicsPlugin)
			{
				if (Economics != null && Economics.IsLoaded == true)
					economicsloaded = true;
				else
				{
					UseEconomicsPlugin = false;
					PrintWarning("Error: Economics plugin was not found! Can't reward players using Economics.");
				}
			}
		}
		catch
		{
			economicsloaded = false;
		}

		try
		{
			serverrewardsloaded = false;
			if (UseServerRewardsPlugin)
			{
				if (ServerRewards != null && ServerRewards.IsLoaded == true)
					serverrewardsloaded = true;
				else
				{
					UseServerRewardsPlugin = false;
					PrintWarning("Error: ServerRewards plugin was not found! Can't reward players using ServerRewards.");
				}
			}
		}
		catch
		{
			serverrewardsloaded = false;
		}

		try
		{
			friendsloaded = false;
			if (UseFriendsPlugin)
			{
				if (Friends != null && Friends.IsLoaded == true)
					friendsloaded = true;
				else
				{
					UseFriendsPlugin = false;
					PrintWarning("Warning: Friends plugin was not found! Can't check if victim is friend to killer.");
				}
			}
		}
		catch
		{
			friendsloaded = false;
		}

		try
		{
			clansloaded = false;
			if (UseClansPlugin)
			{
				if (Clans != null && Clans.IsLoaded == true)
					clansloaded = true;
				else
				{
					UseClansPlugin = false;
					PrintWarning("Warning: Clans plugin was not found! Can't check if victim is in the same clan of killer.");
				}
			}
		}
		catch
		{
			clansloaded = false;
		}

		try
		{
			zonemanagerloaded = false;
			if (UseZoneManagerPlugin)
			{
				if (ZoneManager != null && ZoneManager.IsLoaded == true)
					zonemanagerloaded = true;
				else
				{
					UseZoneManagerPlugin = false;
					PrintWarning("Error: ZoneManager plugin was not found! Can't reward players using ZoneManager.");
				}
			}
		}
		catch
		{
			zonemanagerloaded = false;
		}

		try
		{
			if (GUIAnnouncements != null && GUIAnnouncements.IsLoaded == true)
				GUIAnnouncementsloaded = true;
			else
				GUIAnnouncementsloaded = false;
			if (UseGUIAnnouncementsPlugin && !GUIAnnouncementsloaded)
			{
				UseGUIAnnouncementsPlugin = false;
				PrintWarning("Warning: GUIAnnouncements plugin was not found! Messages will be sent directly to players.");
			}
		}
		catch
		{
			UseGUIAnnouncementsPlugin = false;
			GUIAnnouncementsloaded = false;
		}

		if (!GUIAnnouncementsloaded && UseGUIAnnouncementsForRewards)
		{
			UseGUIAnnouncementsForRewards = false;
			PrintWarning("Warning: GUIAnnouncements plugin was not found! Messages will be sent directly to players.");
		}
	}

	protected override void LoadDefaultMessages()
	{
		lang.RegisterMessages(new Dictionary<string, string>
		{
			["activity"] = "You received {0} Reward for activity.",
			["autoturret"] = "You received {0} for destroying an autoturret",
			["barrel"] = "You received {0} Reward for looting a barrel",
			["balloon"] = "You received {0} for destroying a balloon.",
			["bear"] = "You received {0} Reward for killing a bear",
			["berry"] = "You received {0} Reward for collecting berry",
			["boar"] = "You received {0} Reward for killing a boar",
			["boat"] = "You received {0} Reward for sinking a boat",
			["bones"] = "You received {0} Reward for picking up bones",
			["bradley"] = "You received {0} Reward for killing a Bradley APC",
			["cactus"] = "You received {0} Reward for collecting cactus",
			["chicken"] = "You received {0} Reward for killing a chicken",
			["chinook"] = "You received {0} Reward for killing a chinook CH47",
			["collect"] = "You received {0} Reward for collecting {1}.",
			["corn"] = "You received {0} Reward for collecting corn.",
			["crate"] = "You received {0} Reward for looting a crate",
			["foodbox"] = "You received {0} for looting a food box.",
			["giftbox"] = "You received {0} for looting a gift box.",
			["helicopter"] = "You received {0} Reward for killing a helicopter",
			["hemp"] = "You received {0} Reward for collecting hemp",
			["horse"] = "You received {0} Reward for killing a horse",
			["kill"] = "You received {0} Reward for killing {1}.",
			["minecart"] = "You received {0} Reward for looting a mine cart",
			["minecart"] = "You received {0} for looting a minecart.",
			["minicopter"] = "You received {0} Reward for killing a minicopter",
			["murderer"] = "You received {0} Reward for killing a zombie/murderer",
			["mushrooms"] = "You received {0} Reward for collecting mushrooms",
			["npc"] = "You received {0} Reward for killing a NPC",
			["ore"] = "You received {0} Reward for collecting ore",
			["player"] = "You received {0} Reward for killing a player",
			["potato"] = "You received {0} Reward for collecting potato",
			["pumpkin"] = "You received {0} Reward for collecting pumpkin",
			["rhib"] = "You received {0} Reward for sinking a rhib",
			["ridablehorse"] = "You received {0} Reward for killing a ridable horse",
			["sam"] = "You received {0} for destroying a SAM",
			["scrapTake"] = "You gained {0} scrap",
			["scrapTake"] = "You lost {0} scrap",
			["scrapcopter"] = "You received {0} Reward for killing a scrap transport helicopter",
			["scarecrow"] = "You received {0} Reward for killing a scarecrow",
			["bandit_guard"] = "You received {0} Reward for killing a bandit guard",
			["scientist_peacekeeper"] = "You received {0} Reward for killing a scientist peacekeeper",
			["heavyscientist"] = "You received {0} Reward for killing a heavy scientist",
			["scientist"] = "You received {0} Reward for killing a scientist",
			["stag"] = "You received {0} Reward for killing a stag",
			["stones"] = "You received {0} Reward for collecting stones",
			["suicide"] = "You lost {0} Reward for suicide",
			["sulfur"] = "You received {0} Reward for collecting sulfur",
			["supplycrate"] = "You received {0} Reward for looting a supply crate",
			["trap"] = "You received {0} for destroying a trap",
			["welcomemoney"] = "Welcome to server! You received {0} as a welcome reward.",
			["wolf"] = "You received {0} Reward for killing a wolf",
			["wood"] = "You received {0} Reward for collecting wood",
			["happyhourend"] = "Happy Hour(s) ended.",
			["happyhourstart"] = "Happy Hour(s) started.",
			["Prefix"] = "Rust Rewards:",
			["rrm changed"] = "Rewards Messages for {0} is now {1}. Currently on are: {2}",
			["rrm syntax"] = "/rrm syntax:  /rrm type state  Type is one of a, h, o, p or k (Activity, Havest, Open, Pickup or Kill).  State is on or off.  for example /rrm h off",
			["rrm type"] = "type must be one of: a, h, o, p or k only. (Activity, Havest, Open, Pickup or Kill",
			["rrm state"] = "state need to be one of: on or off.",
			["VictimNoMoney"] = "{0} doesn't have enough money.",
			["VictimKilled"] = "You lost {0} Reward for being killed by a player",
			["rewardset"] = "Reward was set",
			["setrewards"] = "Variables you can set:"
		}, this);
	}

	void LoadConfigValues()
	{
		//ConfigVersion = GetConfigValue("version", "version", VersionNum);
		NPCReward_Enabled = Convert.ToBoolean(GetConfigValue("settings", "NPCReward_Enabled", "true"));
		VIPMultiplier_Enabled = Convert.ToBoolean(GetConfigValue("settings", "VIPMultiplier_Enabled", "false"));
		DoAdvancedVIP = Convert.ToBoolean(GetConfigValue("settings", "Do_Advanced_VIP", "false"));
		ActivityReward_Enabled = Convert.ToBoolean(GetConfigValue("settings", "ActivityReward_Enabled", "true"));
		Simpler_ActivityReward_Enabled = Convert.ToBoolean(GetConfigValue("settings", "Simpler_ActivityReward_Enabled", "true"));
		WelcomeMoney_Enabled = Convert.ToBoolean(GetConfigValue("settings", "WelcomeMoney_Enabled", "true"));
		OpenReward_Enabled = Convert.ToBoolean(GetConfigValue("settings", "OpenReward_Enabled", "true"));
		KillReward_Enabled = Convert.ToBoolean(GetConfigValue("settings", "KillReward_Enabled", "true"));
		PickupReward_Enabled = Convert.ToBoolean(GetConfigValue("settings", "PickupReward_Enabled", "true"));
		HarvestReward_Enabled = Convert.ToBoolean(GetConfigValue("settings", "HarvestReward_Enabled", "true"));
		WeaponMultiplier_Enabled = Convert.ToBoolean(GetConfigValue("settings", "WeaponMultiplier_Enabled", "true"));
		DistanceMultiplier_Enabled = Convert.ToBoolean(GetConfigValue("settings", "DistanceMultiplier_Enabled", "true"));
		DynamicDistanceMultiplier_Enabled = Convert.ToBoolean(GetConfigValue("settings", "DynamicDistanceMultiplier_Enabled", "false"));
		UseEconomicsPlugin = Convert.ToBoolean(GetConfigValue("settings", "UseEconomicsPlugin", "false"));
		UseServerRewardsPlugin = Convert.ToBoolean(GetConfigValue("settings", "UseServerRewardsPlugin", "false"));
		UseScrap = Convert.ToBoolean(GetConfigValue("settings", "UseScrap", "false"));
		UseFriendsPlugin = Convert.ToBoolean(GetConfigValue("settings", "UseFriendsPlugin", "true"));
		UseClansPlugin = Convert.ToBoolean(GetConfigValue("settings", "UseClansPlugin", "true"));
		UseGUIAnnouncementsForRewards = Convert.ToBoolean(GetConfigValue("settings", "UseGUIAnnouncementsForRewards", "false"));
		UseGUIAnnouncementsPlugin = Convert.ToBoolean(GetConfigValue("settings", "UseGUIAnnouncementsPlugin", "false"));
		UseZoneManagerPlugin = Convert.ToBoolean(GetConfigValue("settings", "UseZoneManagerPlugin", "false"));
		TakeMoneyfromVictim = Convert.ToBoolean(GetConfigValue("settings", "TakeMoneyfromVictim", "false"));
		PrintInConsole = Convert.ToBoolean(GetConfigValue("settings", "PrintInConsole", "false"));
		DoLogging = Convert.ToBoolean(GetConfigValue("settings", "DoLogging", "true"));
		ShowcurrencySymbol = Convert.ToBoolean(GetConfigValue("settings", "ShowcurrencySymbol", "true"));
		HappyHour_Enabled = Convert.ToBoolean(GetConfigValue("settings", "HappyHour_Enabled", "true"));
		Permissions_Enabled = Convert.ToBoolean(GetConfigValue("settings", "Permissions_Enabled", "false"));

		ActivityReward_Minutes = Convert.ToInt32(GetConfigValue("settings", "ActivityReward_Minutes", 15));
		ActivityReward_Seconds = ActivityReward_Minutes * 60;
		HappyHour_BeginHour = Convert.ToInt32(GetConfigValue("settings", "HappyHour_BeginHour", 17));
		HappyHour_EndHour = Convert.ToInt32(GetConfigValue("settings", "HappyHour_EndHour", 21));

		player_default_settings = Convert.ToString(GetConfigValue("settings", "Player Default Settings", "hkopa"));

		prestring = Convert.ToString(GetConfigValue("settings", "Pre String", "<color=#CCBB00>"));
		midstring = Convert.ToString(GetConfigValue("settings", "Mid String", "</color><color=#FFFFFF>"));
		poststring = Convert.ToString(GetConfigValue("settings", "Post String", "</color>"));
		GUIA_BannerColor = Convert.ToString(GetConfigValue("settings", "GUI Announcment Banner Colour", "Blue"));
		GUIA_TextColor = Convert.ToString(GetConfigValue("settings", "GUI Announcment Text Colour", "Yellow"));

		mult_40mm_grenade_he = Convert.ToDouble(GetConfigValue("multipliers", "40mm_grenade_he", 1));
		mult_40mm_grenade_buckshot = Convert.ToDouble(GetConfigValue("multipliers", "40mm_grenade_buckshot", 1));
		mult_multiplegrenadelauncher = Convert.ToDouble(GetConfigValue("multipliers", "multiplegrenadelauncher", 1));
		mult_combatknife = Convert.ToDouble(GetConfigValue("multipliers", "combatknife", 1));
		mult_rock = Convert.ToDouble(GetConfigValue("multipliers", "rock", 1));
		mult_rocket = Convert.ToDouble(GetConfigValue("multipliers", "rocket", 1));
		mult_flamethrower = Convert.ToDouble(GetConfigValue("multipliers", "flamethrower", 1));
		mult_hammer_salvaged = Convert.ToDouble(GetConfigValue("multipliers", "hammer_salvaged", 1));
		mult_icepick_salvaged = Convert.ToDouble(GetConfigValue("multipliers", "icepick_salvaged", 1));
		mult_axe_salvaged = Convert.ToDouble(GetConfigValue("multipliers", "axe_salvaged", 1));
		mult_stone_pickaxe = Convert.ToDouble(GetConfigValue("multipliers", "stone_pickaxe", 1));
		mult_pickaxe = Convert.ToDouble(GetConfigValue("multipliers", "pickaxe", 1));
		mult_stonehatchet = Convert.ToDouble(GetConfigValue("multipliers", "stonehatchet", 1));
		mult_hatchet = Convert.ToDouble(GetConfigValue("multipliers", "hatchet", 1));
		mult_butcherknife = Convert.ToDouble(GetConfigValue("multipliers", "butcherknife", 1));
		mult_pitchfork = Convert.ToDouble(GetConfigValue("multipliers", "pitchfork", 1));
		mult_sickle = Convert.ToDouble(GetConfigValue("multipliers", "sickle", 1));
		mult_torch = Convert.ToDouble(GetConfigValue("multipliers", "torch", 1));
		mult_flashlight = Convert.ToDouble(GetConfigValue("multipliers", "flashlight", 1));
		mult_chainsaw = Convert.ToDouble(GetConfigValue("multipliers", "chainsaw", 1));
		mult_jackhammer = Convert.ToDouble(GetConfigValue("multipliers", "jackhammer", 1));
		mult_assaultrifle = Convert.ToDouble(GetConfigValue("multipliers", "assaultrifle", 1));
		mult_beancangrenade = Convert.ToDouble(GetConfigValue("multipliers", "beancangrenade", 1));
		mult_boltactionrifle = Convert.ToDouble(GetConfigValue("multipliers", "boltactionrifle", 1));
		mult_boneclub = Convert.ToDouble(GetConfigValue("multipliers", "boneclub", 1.5));
		mult_boneknife = Convert.ToDouble(GetConfigValue("multipliers", "boneknife", 1.5));
		mult_candycaneclub = Convert.ToDouble(GetConfigValue("multipliers", "candycaneclub", 1.5));
		mult_compoundbow = Convert.ToDouble(GetConfigValue("multipliers", "compoundbow", 1.25));
		mult_crossbow = Convert.ToDouble(GetConfigValue("multipliers", "crossbow", 1.25));
		mult_customsmg = Convert.ToDouble(GetConfigValue("multipliers", "customsmg", 1));
		mult_doublebarrelshotgun = Convert.ToDouble(GetConfigValue("multipliers", "doublebarrelshotgun", 1));
		mult_eokapistol = Convert.ToDouble(GetConfigValue("multipliers", "eokapistol", 1.25));
		mult_f1grenade = Convert.ToDouble(GetConfigValue("multipliers", "f1grenade", 1));
		mult_handmadefishingrod = Convert.ToDouble(GetConfigValue("multipliers", "handmadefishingrod", 2));
		mult_huntingbow = Convert.ToDouble(GetConfigValue("multipliers", "huntingbow", 1.5));
		mult_l96rifle = Convert.ToDouble(GetConfigValue("multipliers", "l96rifle", 1));
		mult_lr300 = Convert.ToDouble(GetConfigValue("multipliers", "lr300", 1));
		mult_longsword = Convert.ToDouble(GetConfigValue("multipliers", "longsword", 1.5));
		mult_m249 = Convert.ToDouble(GetConfigValue("multipliers", "m249", 1));
		mult_m39 = Convert.ToDouble(GetConfigValue("multipliers", "m39", 1));
		mult_m92pistol = Convert.ToDouble(GetConfigValue("multipliers", "m92pistol", 1));
		mult_mp5a4 = Convert.ToDouble(GetConfigValue("multipliers", "mp5a4", 1));
		mult_mace = Convert.ToDouble(GetConfigValue("multipliers", "mace", 1.5));
		mult_machete = Convert.ToDouble(GetConfigValue("multipliers", "machete", 1.5));
		mult_nailgun = Convert.ToDouble(GetConfigValue("multipliers", "nailgun", 1.25));
		mult_pumpshotgun = Convert.ToDouble(GetConfigValue("multipliers", "pumpshotgun", 1));
		mult_pythonrevolver = Convert.ToDouble(GetConfigValue("multipliers", "pythonrevolver", 1));
		mult_revolver = Convert.ToDouble(GetConfigValue("multipliers", "revolver", 1));
		mult_rocketlauncher = Convert.ToDouble(GetConfigValue("multipliers", "rocketlauncher", 1));
		mult_salvagedcleaver = Convert.ToDouble(GetConfigValue("multipliers", "salvagedcleaver", 1));
		mult_salvagedsword = Convert.ToDouble(GetConfigValue("multipliers", "salvagedsword", 1.5));
		mult_satchelcharge = Convert.ToDouble(GetConfigValue("multipliers", "satchelcharge", 1));
		mult_semiautomaticpistol = Convert.ToDouble(GetConfigValue("multipliers", "semiautomaticpistol", 1));
		mult_semiautomaticrifle = Convert.ToDouble(GetConfigValue("multipliers", "semiautomaticrifle", 1));
		mult_snowball = Convert.ToDouble(GetConfigValue("multipliers", "snowball", 2));
		mult_spas12shotgun = Convert.ToDouble(GetConfigValue("multipliers", "spas12shotgun", 1));
		mult_stonespear = Convert.ToDouble(GetConfigValue("multipliers", "stonespear", 1.25));
		mult_thompson = Convert.ToDouble(GetConfigValue("multipliers", "thompson", 1));
		mult_timedexplosivecharge = Convert.ToDouble(GetConfigValue("multipliers", "timedexplosivecharge", 1));
		mult_waterpipeshotgun = Convert.ToDouble(GetConfigValue("multipliers", "waterpipeshotgun", 1));
		mult_woodenspear = Convert.ToDouble(GetConfigValue("multipliers", "woodenspear", 1.75));
		mult_VIPMultiplier = Convert.ToDouble(GetConfigValue("multipliers", "vipmultiplier", 2));
		mult_HappyHourMultiplier = Convert.ToDouble(GetConfigValue("multipliers", "happyhourmultiplier", 2));
		mult_dynamicdistance = Convert.ToDouble(GetConfigValue("multipliers", "dynamicdistance", 0.01));
		mult_distance_50 = Convert.ToDouble(GetConfigValue("multipliers", "distance_50", 1.5));
		mult_distance_100 = Convert.ToDouble(GetConfigValue("multipliers", "distance_100", 2));
		mult_distance_200 = Convert.ToDouble(GetConfigValue("multipliers", "distance_200", 2.5));
		mult_distance_300 = Convert.ToDouble(GetConfigValue("multipliers", "distance_300", 3));
		mult_distance_400 = Convert.ToDouble(GetConfigValue("multipliers", "distance_400", 3.5));
		rate_autoturret = Convert.ToDouble(GetConfigValue("rates", "autoturret", 10));
		rate_balloon = Convert.ToDouble(GetConfigValue("rates", "balloon", 3));
		rate_barrel = Convert.ToDouble(GetConfigValue("rates", "barrel", 2));
		rate_bear = Convert.ToDouble(GetConfigValue("rates", "bear", 7));
		rate_berry = Convert.ToDouble(GetConfigValue("rates", "berry", 1));
		rate_boar = Convert.ToDouble(GetConfigValue("rates", "boar", 3));
		rate_boat = Convert.ToDouble(GetConfigValue("rates", "boat", 1));
		rate_bradley = Convert.ToDouble(GetConfigValue("rates", "bradley", 50));
		rate_cactus = Convert.ToDouble(GetConfigValue("rates", "cactus", 1));
		rate_chicken = Convert.ToDouble(GetConfigValue("rates", "chicken", 1));
		rate_chinook = Convert.ToDouble(GetConfigValue("rates", "chinook", 50));
		rate_corn = Convert.ToDouble(GetConfigValue("rates", "corn", 1));
		rate_crate = Convert.ToDouble(GetConfigValue("rates", "crate", 2));
		rate_foodbox = Convert.ToDouble(GetConfigValue("rates", "foodbox", 1));
		rate_giftbox = Convert.ToDouble(GetConfigValue("rates", "giftbox", 1));
		rate_heavyscientist = Convert.ToDouble(GetConfigValue("rates", "heavyscientist", 8));
		rate_bandit_guard = Convert.ToDouble(GetConfigValue("rates", "bandit_guard", 1));
		rate_scientist_peacekeeper = Convert.ToDouble(GetConfigValue("rates", "scientist_peacekeeper", 1));
		rate_helicopter = Convert.ToDouble(GetConfigValue("rates", "helicopter", 75));
		rate_hemp = Convert.ToDouble(GetConfigValue("rates", "hemp", 1));
		rate_horse = Convert.ToDouble(GetConfigValue("rates", "horse", 2));
		rate_minecart = Convert.ToDouble(GetConfigValue("rates", "minecart", 2));
		rate_minicopter = Convert.ToDouble(GetConfigValue("rates", "minicopter", 75));
		rate_murderer = Convert.ToDouble(GetConfigValue("rates", "murderer", 6));
		rate_mushrooms = Convert.ToDouble(GetConfigValue("rates", "mushrooms", 2));
		rate_npckill = Convert.ToDouble(GetConfigValue("rates", "npckill", 8));
		rate_ore = Convert.ToDouble(GetConfigValue("rates", "ore", 2));
		rate_player = Convert.ToDouble(GetConfigValue("rates", "player", 10));
		rate_potato = Convert.ToDouble(GetConfigValue("rates", "potato", 1));
		rate_pumpkin = Convert.ToDouble(GetConfigValue("rates", "pumpkin", 1));
		rate_bones = Convert.ToDouble(GetConfigValue("rates", "bones", 1));
		rate_rhib = Convert.ToDouble(GetConfigValue("rates", "rhib", 1));
		rate_ridablehorse = Convert.ToDouble(GetConfigValue("rates", "ridablehorse", 2));
		rate_sam = Convert.ToDouble(GetConfigValue("rates", "sam", 5));
		rate_scarecrow = Convert.ToDouble(GetConfigValue("rates", "scarecrow", 8));
		rate_scientist = Convert.ToDouble(GetConfigValue("rates", "scientist", 8));
		rate_scrapcopter = Convert.ToDouble(GetConfigValue("rates", "scrapcopter", 75));
		rate_stag = Convert.ToDouble(GetConfigValue("rates", "stag", 2));
		rate_stones = Convert.ToDouble(GetConfigValue("rates", "stones", 1));
		rate_suicide = Convert.ToDouble(GetConfigValue("rates", "suicide", -10));
		rate_sulfur = Convert.ToDouble(GetConfigValue("rates", "sulfur", 1));
		rate_supplycrate = Convert.ToDouble(GetConfigValue("rates", "supplycrate", 5));
		rate_trap = Convert.ToDouble(GetConfigValue("rates", "trap", 2));
		rate_wolf = Convert.ToDouble(GetConfigValue("rates", "wolf", 8));
		rate_wood = Convert.ToDouble(GetConfigValue("rates", "wood", 1));
		rate_activityreward = Convert.ToDouble(GetConfigValue("rates", "activityreward", 15));
		rate_welcomemoney = Convert.ToDouble(GetConfigValue("rates", "welcomemoney", 50));

		//sample group
		Dictionary<string, double> samplegroup = new Dictionary<string, double>();
		samplegroup.Add("vip", 1.5);
		samplegroup.Add("default", 1.0);
		//samplegroup.Add("admin", 2.0);
		//samplegroup.Add("vip", 1.5);
		//samplegroup.Add("mentor", 1.2);
		//samplegroup.Add("esteemed", 1.1);
		//samplegroup.Add("regular", 1.1);
		//samplegroup.Add("default", 1.0);

		var json = JsonConvert.SerializeObject(GetConfigValue("groupsettings", "groupmultipliers", samplegroup));
		groupmultiplier = JsonConvert.DeserializeObject<Dictionary<string, double>>(json);

		//if (groupmultiplier == null)
		//	Puts("MT GM loaded :(");
		//else Puts("gm count " + groupmultiplier.Count.ToString());

		if (HappyHour_BeginHour > HappyHour_EndHour)
			happyhourcross24 = true;
		else
			happyhourcross24 = false;

		CheckConfig();
		CurrencyCulture.NumberFormat.CurrencyNegativePattern = 1;  // make it show negative signs

		//if (ConfigVersion != VersionNum || _didConfigChange)
		if (_didConfigChange)
		{
			Puts("Configuration file updated.");
			SaveConfig();
		}

		Cleanertimer = timer.Once(600, CleanerTimerProcess);
	}

	void OnServerInitialized()
	{
		LoadDefaultMessages();

		playerPrefs     =  dataFile.ReadObject<Dictionary<string, string>>();
		zonemultipliers = zdataFile.ReadObject<Dictionary<string, double>>();

		LoadConfigValues();
		SaveConfig();

		if (ActivityReward_Enabled)
		{
			if (Simpler_ActivityReward_Enabled)
				timecheck = timer.Once(ActivityReward_Seconds, CheckActivityCurrentTime);
			else
				timecheck = timer.Once(60, CheckActivityCurrentTime);
		}
		if (HappyHour_Enabled)
			timecheck = timer.Once(60, CheckHappyCurrentTime);

	}

	private void Loaded()
	{
		permission.RegisterPermission(ActivityPermission , this);
		permission.RegisterPermission(HarvestPermission  , this);
		permission.RegisterPermission(KillPermission     , this);
		permission.RegisterPermission(OpenPermission     , this);
		permission.RegisterPermission(PickupPermission   , this);
		permission.RegisterPermission(WelcomePermission  , this);
		permission.RegisterPermission(permVIP            , this);
	}

	private void CleanerTimerProcess()
	{
		// look through cache of entities and remove ones that are too old
		foreach(KeyValuePair<uint, TrackPlayer> x in EntityCollectionCache.ToList())
		{
			if (x.Value.time > shotclock * 3)
				try
				{
					EntityCollectionCache.Remove(x.Key);
				}
				catch {} // probably deleted in the code while this was running
		}
		Cleanertimer = timer.Once(600, CleanerTimerProcess);
	}

	string TrimPunctuation(string value)
	{
		return Regex.Replace(value, "[^A-Za-z0-9]", "");
	}

	private void CheckActivityCurrentTime()
	{
		var gtime = TOD_Sky.Instance.Cycle.Hour;
		IPlayer ip = null;


		if (ActivityReward_Enabled)
		{
			foreach (var p in BasePlayer.activePlayerList.ToArray()) //players.Connected)
			{
				ip = p.IPlayer;
				//Puts(ip.Id);
				if (ip == null  && !ip.IsConnected)  return;
				if (Permissions_Enabled && !permission.UserHasPermission(ip.Id, ActivityPermission)) return;
				if (Simpler_ActivityReward_Enabled)
				{
					GiveReward(ip, "activity", "a", null, 1);
				}
				else
				{
					try
					{
						if (Activity_Reward.ContainsKey(ip))
						{
							if ((p.Connection.GetSecondsConnected() - Activity_Reward[ip]) > ActivityReward_Seconds)
							{
								GiveReward(ip, "activity", "a", null, 1);
								try
								{
									Activity_Reward[ip] = p.Connection.GetSecondsConnected();
								}
								catch
								{
									Puts("Tell MSpeedie bug with adding to Activity_Reward");
								}
							}
						}
						else
						{
							try
							{
								Activity_Reward.Add(ip, p.secondsConnected);
								if (Simpler_ActivityReward_Enabled)
									GiveReward(ip, "activity", "a", null, 1);

							}
							catch
							{ }
						}
					}
					catch
					{
						try
						{
							Activity_Reward.Add(ip, p.secondsConnected);
						}
						catch
						{ }
					}
				}
			}
		}
		// add a 5 second off set to allow for lag
		if (Simpler_ActivityReward_Enabled)
			timecheck = timer.Once(ActivityReward_Seconds+5, CheckActivityCurrentTime);
		else
			timecheck = timer.Once(60, CheckActivityCurrentTime);
	}

	private void CheckHappyCurrentTime()
	{
		var gtime = TOD_Sky.Instance.Cycle.Hour;

		if (HappyHour_Enabled)
		{
			if (!happyhouractive)
			{
				if ((happyhourcross24 == false && gtime >= HappyHour_BeginHour && gtime < HappyHour_EndHour) ||
					(happyhourcross24 == true && ((gtime >= HappyHour_BeginHour && gtime < 24) || gtime < HappyHour_EndHour))
					)
				{
					happyhouractive = true;
					if (PrintInConsole)
						Puts("Happy hour(s) started.  Ending at " + HappyHour_EndHour);
					MessagePlayers("happyhourstart");
				}
			}
			else
			{
				if ((happyhourcross24 == false && gtime >= HappyHour_EndHour) ||
					(happyhourcross24 == true && (gtime < HappyHour_BeginHour && gtime >= HappyHour_EndHour))
					)
				{
					happyhouractive = false;
					if (PrintInConsole)
						Puts("Happy Hour(s) ended.  Next Happy Hour(s) starts at " + HappyHour_BeginHour);
					MessagePlayers("happyhourend");
				}
			}
			timecheck = timer.Once(60, CheckHappyCurrentTime);
		}
	}

	string CleanIP(string ipaddress)
	{
		if (string.IsNullOrEmpty(ipaddress)) return " ";

		if (!ipaddress.Contains(":") || ipaddress.LastIndexOf(":") == 0) return ipaddress;
			return ipaddress.Substring(0, ipaddress.LastIndexOf(":"));
	}

	private void OnPlayerConnected(BasePlayer player)
	{
		if (!Economics && !ServerRewards && !UseScrap) return;
		// if (player.IsNpc) return;
		if (player is NPCPlayerApex || player is NPCPlayer || player is Scientist || player is NPCMurderer)
			return;

		IPlayer iplayer = player.IPlayer;

		if (iplayer == null || iplayer.Id == null) return;
		if (playerPrefs.ContainsKey(iplayer.Id)) return;
		else
		{
			playerPrefs.Add(iplayer.Id, player_default_settings);
			dataFile.WriteObject(playerPrefs);
			if (PrintInConsole)
				Puts("New Player: " + iplayer.Name);
			if (WelcomeMoney_Enabled)
			{
				if (Permissions_Enabled && !permission.UserHasPermission(iplayer.Id, WelcomePermission)) return;
				GiveReward(iplayer, "welcomemoney", "w", null, 1);
			}
		}
	}

	private void Unload()
	{
		if (timecheck != null)
			timecheck.Destroy();
	}

	string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

	bool HasPerm(IPlayer p, string pe) => p.HasPermission(pe);

	static string SortString(string input)
	{
		char[] characters = input.ToArray();
		Array.Sort(characters);
		return new string(characters);
	}

	private void MessagePlayers(string key)
	{
		string mess = blnk;
		if (GUIAnnouncementsloaded && UseGUIAnnouncementsPlugin)
			foreach (var player in BasePlayer.activePlayerList)
			{
				mess = String.Concat(Lang("Prefix", player.UserIDString) ?? blnk,
						Lang(key,player.UserIDString) ?? blnk);
				GUIAnnouncements?.Call("CreateAnnouncement", mess, GUIA_BannerColor, GUIA_TextColor, player);
			}
		else
			foreach (var player in BasePlayer.activePlayerList)
			{
				mess = String.Concat( prestring ?? blnk , Lang("Prefix", player.UserIDString) ?? blnk , ": " ,
						midstring ?? blnk, Lang(key, player.UserIDString) ?? blnk, poststring ?? blnk);
				SendReply(player, mess);
			}
	}

	private void MessagePlayer(IPlayer player, string msg, string prefix, string ptype)
	{
		// we check ptype (prefence type) to see if the player wants to see these
		string pref = player_default_settings;
		string mess = blnk;

		if (player == null || String.IsNullOrWhiteSpace(msg) || player.Id == null || !player.IsConnected) return;
		else
		{
			try
			{
				playerPrefs.TryGetValue(player.Id, out pref);
				// catch and correct any corrupted preferences
				if (pref.Length > 5)
					pref = player_default_settings;
			}
			catch
			{
				pref = player_default_settings;
			}
		}

		if (String.IsNullOrWhiteSpace(ptype) || (!String.IsNullOrWhiteSpace(pref) && (pref.Contains(ptype) || ptype == "w")))
		{

			if (!(String.IsNullOrWhiteSpace(prefix)))
				mess = String.Concat(prestring ?? blnk , prefix ?? blnk , " ", midstring ?? blnk , msg ?? blnk, poststring ?? blnk).Trim();
			else
				mess = msg;
			if (GUIAnnouncementsloaded && UseGUIAnnouncementsForRewards)
			{
				BasePlayer bplayer =  player.Object as BasePlayer;
				GUIAnnouncements?.Call("CreateAnnouncement", mess, GUIA_BannerColor, GUIA_TextColor, bplayer);
			}
			else
				player.Reply(mess);
		}
	}

	private void TakeScrap(IPlayer player, int itemAmount)
	{
		int ScrapId = -932201673;
        BasePlayer basePlayer = player.Object as BasePlayer;
		if (basePlayer == null)
			return;
		if (basePlayer.inventory.Take(null, ScrapId, itemAmount) > 0)
		{
			basePlayer.SendConsoleCommand("note.inv", ScrapId, itemAmount * -1);
		}
	}

	private void TakeScrap(BasePlayer basePlayer, int itemAmount)
	{
		int ScrapId = -932201673;
		if (basePlayer == null)
			return;
		if (basePlayer.inventory.Take(null, ScrapId, itemAmount) > 0)
		{
			basePlayer.SendConsoleCommand("note.inv", ScrapId, itemAmount * -1);
		}
	}

    private object GiveScrap(BasePlayer basePlayer, int amount = 1)
    {
        Item item = ItemManager.Create(ItemManager.FindItemDefinition(-932201673));
        if (item == null)
        {
            return false;
        }

        item.amount = amount;

        if (basePlayer == null)
        {
			item.Remove();
            return false;
        }

        if (!basePlayer.inventory.GiveItem(item, basePlayer.inventory.containerMain))
        {
            item.Remove();
            return false;
        }
        return true;
    }

    private object GiveScrap(IPlayer iplayer, int amount = 1)
    {
        Item item = ItemManager.Create(ItemManager.FindItemDefinition(-932201673));
        if (item == null)
        {
            return false;
        }

        item.amount = amount;

        BasePlayer basePlayer = iplayer.Object as BasePlayer;
        if (basePlayer == null)
        {
			item.Remove();
            return false;
        }

        if (!basePlayer.inventory.GiveItem(item, basePlayer.inventory.containerMain))
        {
            item.Remove();
            return false;
        }
        return true;
    }

	void PayPlayer(BasePlayer baseplayer, double amount)
	{
		if (UseScrap || UseServerRewardsPlugin)
			amount = Math.Round(amount, 0);
		if (amount == 0.0d)
			return;

		if (UseScrap)
		{
			if (amount < 0.0d)
			{
				//Puts("Rust Rewards does not currently support taking scrap from players");
				//TakeScrap(baseplayer, (int)(amount));
				return;
			}
			else
				GiveScrap(baseplayer, (int)(amount));
		}
		else if (UseServerRewardsPlugin)
		{
			if (amount < 0.0d)
				ServerRewards?.Call("TakePoints", baseplayer.UserIDString, -1*(int) (amount));
			else
				ServerRewards?.Call("AddPoints", baseplayer.UserIDString, (int)(amount));
		}
		else if (UseEconomicsPlugin)
		{
			if (amount < 0.0d)
			{
				Economics?.Call("Withdraw", baseplayer.UserIDString, -1*amount);
			}
			else
				Economics?.Call("Deposit", baseplayer.UserIDString, amount);
		}
	}

	void PayPlayer(IPlayer player, double amount)
	{
		if (UseScrap || UseServerRewardsPlugin)
			amount = Math.Round(amount, 0);
		if (amount == 0.0d)
			return;

		if (UseScrap)
		{
			if (amount < 0.0d)
			{
				//Puts("Rust Rewards does not currently support taking scrap from players");
				//TakeScrap(player, (int)(amount));
				return;
			}
			else
				GiveScrap(player, (int)(amount));
		}
		else if (UseServerRewardsPlugin)
		{
			if (amount < 0.0d)
				ServerRewards?.Call("TakePoints", player.Id, -1*(int) (amount));
			else
				ServerRewards?.Call("AddPoints", player.Id, (int)(amount));
		}
		else if (UseEconomicsPlugin)
		{
			if (amount < 0.0d)
			{
				//Puts("E withdrawl");
				Economics?.Call("Withdraw", player.Id, -1*amount);
			}
			else
				Economics?.Call("Deposit", player.Id, amount);
		}
	}

	bool CheckPlayer(ulong playerId, double amount)
	{
		double balance = 0.0d;
		if (UseServerRewardsPlugin)
		{
			balance = (double) ServerRewards?.Call("Check", playerId);
		}
		else if (UseEconomicsPlugin)
		{
			balance = (double) Economics?.Call("Balance", playerId);
		}

		//Puts(String.Format("{0:0.##}",balance));

		if (!(amount.CompareTo(balance) < 0.0d))
			return true;
		else
			return false;
	}

	bool CheckPlayer(string playerId, double amount)
	{
		double balance = 0.0d;
		if (UseServerRewardsPlugin)
		{
			balance = (double) ServerRewards?.Call("Check", playerId);
		}
		else if (UseEconomicsPlugin)
		{
			balance = (double) Economics?.Call("Balance", playerId);
		}

		//Puts(String.Format("{0:0.##}",balance));

		if (!(amount.CompareTo(balance) < 0))
			return true;
		else
			return false;

	}

	private double GetDistance(float distance)
	{
		if (distance < 50.0f) return 1;
		else if (distance < 100.0f) return mult_distance_50;
		else if (distance < 200.0f) return mult_distance_100;
		else if (distance < 300.0f) return mult_distance_200;
		else if (distance < 400.0f) return mult_distance_300;
		else if (distance >= 400.0f) return mult_distance_400;
		else return 1;
	}

	private double GetDynamicDistance(float distance)
	{
		return 1.0f + (distance * mult_dynamicdistance);
	}

	private double GetWeapon(string weaponshortname)
	{
		string weaponname = null;

		if (String.IsNullOrWhiteSpace(weaponshortname))
			return 1;
		else
			weaponname = weaponshortname.Replace('_', '.');

		if (weaponname.Contains("ak47u")) return mult_assaultrifle;
		else if (weaponname.Contains("40mm.grenade.buckshot")) return mult_40mm_grenade_buckshot;
		else if (weaponname.Contains("40mm.grenade.he")) return mult_40mm_grenade_he;
		else if (weaponname.Contains("axe.salvaged")) return mult_axe_salvaged;
		else if (weaponname.Contains("bolt.rifle")) return mult_boltactionrifle;
		else if (weaponname.Contains("bone.club")) return mult_boneclub;
		else if (weaponname.Contains("bow")) return mult_huntingbow;
		else if (weaponname.Contains("butcherknife")) return mult_butcherknife;
		else if (weaponname.Contains("candy.cane")) return mult_candycaneclub;
		else if (weaponname.Contains("candycaneclub")) return mult_candycaneclub;
		else if (weaponname.Contains("chainsaw")) return mult_chainsaw;
		else if (weaponname.Contains("cleaver")) return mult_salvagedcleaver;
		else if (weaponname.Contains("knife.combat")) return mult_combatknife;
		else if (weaponname.Contains("compound")) return mult_compoundbow;
		else if (weaponname.Contains("crossbow")) return mult_crossbow;
		else if (weaponname.Contains("double.shotgun")) return mult_doublebarrelshotgun;
		else if (weaponname.Contains("eoka")) return mult_eokapistol;
		else if (weaponname.Contains("explosive.satchel")) return mult_satchelcharge;
		else if (weaponname.Contains("explosive.timed")) return mult_timedexplosivecharge;
		else if (weaponname.Contains("fishingrod.handmade")) return mult_handmadefishingrod;
		else if (weaponname.Contains("flamethrower")) return mult_flamethrower;
		else if (weaponname.Contains("flashlight")) return mult_flashlight;
		else if (weaponname.Contains("grenade.beancan")) return mult_beancangrenade;
		else if (weaponname.Contains("grenade.f1")) return mult_f1grenade;
		else if (weaponname.Contains("hammer.salvaged")) return mult_hammer_salvaged;
		else if (weaponname.Contains("hatchet")) return mult_hatchet;
		else if (weaponname.Contains("icepick.salvaged")) return mult_icepick_salvaged;
		else if (weaponname.Contains("jackhammer")) return mult_jackhammer;
		else if (weaponname.Contains("knife.bone")) return mult_boneknife;
		else if (weaponname.Contains("l96")) return mult_l96rifle;
		else if (weaponname.Contains("longsword")) return mult_longsword;
		else if (weaponname.Contains("lr300")) return mult_lr300;
		else if (weaponname.Contains("m249")) return mult_m249;
		else if (weaponname.Contains("m39")) return mult_m39;
		else if (weaponname.Contains("m92")) return mult_m92pistol;
		else if (weaponname.Contains("mace")) return mult_mace;
		else if (weaponname.Contains("machete")) return mult_machete;
		else if (weaponname.Contains("mp5")) return mult_mp5a4;
		else if (weaponname.Contains("mgl")) return mult_multiplegrenadelauncher;
		else if (weaponname.Contains("nailgun")) return mult_nailgun;
		else if (weaponname.Contains("pickaxe")) return mult_pickaxe;
		else if (weaponname.Contains("pistol.revolver")) return mult_revolver;
		else if (weaponname.Contains("pistol.semiauto")) return mult_semiautomaticpistol;
		else if (weaponname.Contains("pitchfork")) return mult_pitchfork;
		else if (weaponname.Contains("python")) return mult_pythonrevolver;
		else if (weaponname.Contains("rocket")) return mult_rocket;
		else if (weaponname.Contains("rocket.launcher")) return mult_rocketlauncher;
		else if (weaponname.Contains("semi.auto.rifle")) return mult_semiautomaticrifle;
		else if (weaponname.Contains("shotgun.pump")) return mult_pumpshotgun;
		else if (weaponname.Contains("shotgun.waterpipe")) return mult_waterpipeshotgun;
		else if (weaponname.Contains("sickle")) return mult_sickle;
		else if (weaponname.Contains("smg")) return mult_customsmg;
		else if (weaponname.Contains("snowball")) return mult_snowball;
		else if (weaponname.Contains("spas12")) return mult_spas12shotgun;
		else if (weaponname.Contains("spear.stone")) return mult_stonespear;
		else if (weaponname.Contains("spear.wood")) return mult_woodenspear;
		else if (weaponname.Contains("stone.pickaxe")) return mult_stone_pickaxe;
		else if (weaponname.Contains("stonehatchet")) return mult_stonehatchet;
		else if (weaponname.Contains("sword")) return mult_salvagedsword;
		else if (weaponname.Contains("thompson")) return mult_thompson;
		else if (weaponname.Contains("torch")) return mult_torch;
		else if (weaponname.Contains("rock")) return mult_rock;
		else
		{
			Puts("Rust Rewards, Unknown weapon: " + weaponname);
			return 1;
		}
	}

	private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
	{
		//Puts("In ODG");
		if (!HarvestReward_Enabled) return;
		if (String.IsNullOrWhiteSpace(item?.info?.shortname) && !(dispenser.GetComponent<BaseEntity>() is TreeEntity)) return;
		if (entity == null) return;
		if (entity is BaseNpc || entity is NPCPlayerApex || entity is NPCPlayer || entity is NPCMurderer) return;
		if (entity.ToPlayer().IPlayer == null) return;
		if (Permissions_Enabled && !permission.UserHasPermission(entity.ToPlayer().UserIDString, HarvestPermission)) return;
		if (dispenser?.gameObject?.ToBaseEntity() == null) return;

		uint beId = dispenser.gameObject.ToBaseEntity().net.ID;

		if (dispenser.GetComponent<BaseEntity>() is TreeEntity ||
		    item.info.shortname.Contains("stone") || item.info.shortname.Contains("sulfur") ||
		    item.info.shortname.Contains(".ore") || item.info.shortname.Contains("cactus") ||
			item.info.shortname.Contains("driftwood") ||
			item.info.shortname.Contains("douglas") ||
			item.info.shortname.Contains("fir") ||
			item.info.shortname.Contains("birch") ||
			item.info.shortname.Contains("oak") ||
			item.info.shortname.Contains("pine") ||
			item.info.shortname.Contains("juniper") ||
			item.info.shortname.Contains("deadtree") ||
			item.info.shortname.Contains("swamp_tree") ||
			item.info.shortname.Contains("palm") ||
			item.info.shortname.Contains("wood") ||
			item.info.shortname.Contains("log")
			)
		{
			TrackPlayer ECEData;
			ECEData.iplayer = entity.ToPlayer().IPlayer;
			ECEData.time    = TOD_Sky.Instance.Cycle.Hour;

			if (EntityCollectionCache.ContainsKey(beId))
				EntityCollectionCache[beId] = ECEData;
			else
				EntityCollectionCache.Add(beId, ECEData);
		}
		//else
		//	Puts("ODG shortName: " +item.info.shortname);
	}

	void OnGrowableGathered(GrowableEntity growable, Item item, BasePlayer player)
	{
		//Puts("In OGG");
		if (!PickupReward_Enabled) return;
		if (Permissions_Enabled && !permission.UserHasPermission(player.UserIDString, PickupPermission)) return;
		if (String.IsNullOrWhiteSpace(item?.info?.shortname)) return;
		//else Puts(item?.info?.shortname);
		if (player == null) return;
		if (player is NPCPlayerApex || player is NPCPlayer || player is NPCMurderer) return;
		IPlayer iplayer = player.IPlayer;
		if (iplayer == null) return;

		string shortName = item.info.shortname;
		string resource = null;

		if (shortName.Contains("berry"))
			resource = "berry";
		else if (shortName.Contains("corn"))
			resource = "corn";
		else if (shortName.Contains("hemp") || shortName.Contains("cloth"))
			resource = "hemp";
		else if (shortName.Contains("potato"))
			resource = "potato";
		else if (shortName.Contains("pumpkin"))
			resource = "pumpkin";
		//else
		//	Puts("Rust Rewards OnGrowableGather missing shortName: " + shortName);

		if (resource != null)
		{
			double totalmultiplier = 1;
			totalmultiplier = (happyhouractive ? mult_HappyHourMultiplier : 1) * ((VIPMultiplier_Enabled && HasPerm(iplayer, permVIP)) ? mult_VIPMultiplier : 1);
			GiveReward(iplayer, resource, "p", null, totalmultiplier);
		}

	}

	private void OnCollectiblePickup(Item item, BasePlayer player)
	{

		//Puts("In OCP");
		if (!PickupReward_Enabled) return;
		if (Permissions_Enabled && !permission.UserHasPermission(player.UserIDString, PickupPermission)) return;
		if (String.IsNullOrWhiteSpace(item?.info?.shortname)) return;
		if (player == null) return;
		if (player is NPCPlayerApex || player is NPCPlayer || player is NPCMurderer) return;
		IPlayer iplayer = player.IPlayer;
		if (iplayer == null) return;

		string shortName = item.info.shortname;
		string resource = null;

		//Puts("pickup: " + shortName);

		if (shortName.Contains("stone"))
			resource = "stones";
		else if (shortName.Contains("sulfur"))
			resource = "sulfur";
		else if (shortName.Contains(".ore"))
			resource = "ore";
		else if (shortName.Contains("wood") || shortName.Contains("log"))
			resource = "wood";
		else if (shortName.Contains("mushroom"))
			resource = "mushrooms";
		else if (shortName.Contains("seed.corn"))
			resource = "corn";
		else if (shortName.Contains("seed.hemp"))
			resource = "hemp";
		else if (shortName.Contains("seed.potato"))
			resource = "potato";
		else if (shortName.Contains("seed.pumpkin"))
			resource = "pumpkin";
		else if (shortName.Contains("bone.fragments"))
			resource = "bones";
		else if (shortName.Contains("seed") && shortName.Contains("berry"))
			resource = "berry";
		//else
		//	Puts("OEC shortName: " + shortName);

		if (resource != null)
		{
	        double totalmultiplier = 1;

			totalmultiplier = (happyhouractive ? mult_HappyHourMultiplier : 1) * ((VIPMultiplier_Enabled && HasPerm(iplayer, permVIP)) ? mult_VIPMultiplier : 1);
			GiveReward(iplayer, resource, "p", null, totalmultiplier);
		}
	}

	void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
	{

		if (entity == null || entity.IsDestroyed) return;
		if (!KillReward_Enabled) return;
		if (entity == null || String.IsNullOrWhiteSpace(entity?.ShortPrefabName))  return;
		//Puts("OETD: " + entity.ShortPrefabName);
		if (info == null || info.Initiator == null) return;

	    // removed from next if
		//||entity.ShortPrefabName.Contains("bradleyapc") ||
		//entity.ShortPrefabName.Contains("ch47") ||
		//entity.ShortPrefabName.Contains("patrolhelicopter"))

		// used to track who killed it. last hit wins ;-)
		if (!(entity is BaseHelicopter || entity is CH47HelicopterAIController || entity is BradleyAPC))
			return;

		 // (info.Initiator as BasePlayer).IsNpc ||
		if (!(info.Initiator is BasePlayer) ||
			info.Initiator is BaseNpc || info.Initiator is NPCPlayerApex ||
			info.Initiator is NPCPlayer || info.Initiator is NPCMurderer ||
			info.Initiator.name.Contains("scarecrow.prefab") ||
			info.Initiator.name.Contains("assets/rust.ai/agents/npcplayer") ||
			info.Initiator.name.Contains("scientist") ||
			info.Initiator.name.Contains("human")
			)
			return;

		BasePlayer bplayer = info?.Initiator?.ToPlayer();
		if (bplayer == null) return;
		IPlayer iplayer = bplayer.IPlayer;
		if (iplayer == null) return;
		if (Permissions_Enabled && !permission.UserHasPermission(bplayer.UserIDString, KillPermission)) return;

		TrackPlayer ECEData;

		ECEData.iplayer = iplayer;
		ECEData.time = TOD_Sky.Instance.Cycle.Hour;

		//Puts("OETD BaseCombatEntity: " + entity.ShortPrefabName);
		if (EntityCollectionCache.ContainsKey(entity.net.ID))
			EntityCollectionCache[entity.net.ID] = ECEData;
		else
			EntityCollectionCache.Add(entity.net.ID, ECEData);
	}

	private void OnLootEntity(BasePlayer player, BaseEntity entity)
	{
		if (entity == null || entity.IsDestroyed) return;
		if (!OpenReward_Enabled) return;
		if (Permissions_Enabled && !permission.UserHasPermission(player.UserIDString, OpenPermission)) return;

		if (entity == null || entity.net.ID == null || String.IsNullOrWhiteSpace(entity?.ShortPrefabName)) return;
		if (player == null || player is NPCPlayerApex || player is NPCPlayer || player is NPCMurderer) return;

		IPlayer iplayer = player.IPlayer;
		if (iplayer == null) return;

		if (!(entity.ShortPrefabName.Contains("crate") || entity.ShortPrefabName.Contains("foodbox") ||
			  entity.ShortPrefabName.Contains("trash") || entity.ShortPrefabName.Contains("minecart") ||
			  entity.ShortPrefabName.Contains("supply")
			  ))
			return;

		TrackPlayer ECEData;
		ECEData.iplayer = iplayer;
		ECEData.time    = TOD_Sky.Instance.Cycle.Hour;

		if (EntityCollectionCache.ContainsKey(entity.net.ID))
			EntityCollectionCache[entity.net.ID] = ECEData;
		else
			EntityCollectionCache.Add(entity.net.ID, ECEData);
	}

	private void OnEntityKill(BaseNetworkable entity)
	{
		if (entity == null || entity.IsDestroyed) return;
		if (!OpenReward_Enabled && !HarvestReward_Enabled) return;
		if (entity == null || entity.net == null || entity.net.ID == null ||
		    String.IsNullOrWhiteSpace(entity.ShortPrefabName)) return;

		TrackPlayer ECEData;
		ECEData.iplayer = null;
		ECEData.time = 0f;

		IPlayer player = null;
		float   ptime = 0f;

		try
		{
			if (EntityCollectionCache.TryGetValue(entity.net.ID, out ECEData))
			{
				if (ECEData.iplayer == null)
					return;
				player    = ECEData.iplayer;
				ptime     = ECEData.time;
				//EntityCollectionCache.Remove(entity.net.ID);
			}
			else return;
		}
		catch {}

		if (player == null || player.Id == null || ptime == null)
			return;
		if ((TOD_Sky.Instance.Cycle.Hour - ptime) >= shotclock)
		{
			//Puts("Too old: " + (TOD_Sky.Instance.Cycle.Hour - ptime));
			return;  // no data or action too old
		}

		//if (!(entity.ShortPrefabName.Contains("planner") ||
		//	 entity.ShortPrefabName.Contains("junkpile") ||
		//	 entity.ShortPrefabName.Contains("divesite") ||
		//	 entity.ShortPrefabName.Contains("barrel") ||
		//	 entity.ShortPrefabName.Contains("hammer") ||
		//	 entity.ShortPrefabName.Contains("guitar") ||
		//	 entity.ShortPrefabName.Contains("junkpile") ||
		//	 entity.ShortPrefabName.Contains("waterbottle") ||
		//	 entity.ShortPrefabName.Contains("jug") ||
		//	 entity.ShortPrefabName.Contains("salvage") ||
		//	 entity.ShortPrefabName.Contains("generic") ||
		//	 entity.ShortPrefabName.Contains("bow") ||
		//	 entity.ShortPrefabName.Contains("boat") ||
		//	 entity.ShortPrefabName.Contains("rhib") ||
		//	 entity.ShortPrefabName.Contains("fuel") ||
		//	 entity.ShortPrefabName.Contains("foodbox") ||
		//	 entity.ShortPrefabName.Contains("giftbox") ||
		//	 entity.ShortPrefabName.Contains("standingdriver") ||
		//	 entity.ShortPrefabName.Contains("crate") ||
		//	 entity.ShortPrefabName.Contains("supply") ||
		//	 entity.ShortPrefabName.Contains("oilfireball") ||
		//	 entity.ShortPrefabName.Contains("rocket_basic") ||
		//	 entity.ShortPrefabName.Contains("entity") ||
		//	 entity.ShortPrefabName.Contains("weapon")
		//    ))
		//{
		//	Puts("OEK:" + entity.ShortPrefabName);
		//}

		string resource = null;
		string ptype = null;

		if (HarvestReward_Enabled)
		{
			if (Permissions_Enabled && !permission.UserHasPermission(player.Id, HarvestPermission)) return;

			if (entity.ShortPrefabName.Contains("stone"))
			{
				resource = "stones";
				ptype = "h";
			}
			else if (entity.ShortPrefabName.Contains("sulfur"))
			{
				resource = "sulfur";
				ptype = "h";
			}
			else if (entity.ShortPrefabName.Contains("-ore") ||
					entity.ShortPrefabName.Contains("ore_") ||
					entity.ShortPrefabName.Contains(".ore"))
			{
				resource = "ore";
				ptype = "h";
			}
			else if (entity.ShortPrefabName.Contains("cactus"))
			{
				resource = "cactus";
				ptype = "h";
			}
			else if (entity.ShortPrefabName.Contains("driftwood") ||
					entity.ShortPrefabName.Contains("douglas_fir") ||
					entity.ShortPrefabName.Contains("beech") ||
					entity.ShortPrefabName.Contains("birch") ||
					entity.ShortPrefabName.Contains("oak") ||
					entity.ShortPrefabName.Contains("pine") ||
					entity.ShortPrefabName.Contains("juniper") ||
					entity.ShortPrefabName.Contains("deadtree") ||
					entity.ShortPrefabName.Contains("dead_log") ||
					entity.ShortPrefabName.Contains("wood") ||
					entity.ShortPrefabName.Contains("swamp_tree") ||
					entity.ShortPrefabName.Contains("palm"))
			{
				resource = "wood";
				ptype = "h";
			}
		}

		if (OpenReward_Enabled)
		{
			if (Permissions_Enabled && !permission.UserHasPermission(player.Id, OpenPermission)) return;

			if (entity.ShortPrefabName.Contains("minecart"))
			{
				resource = "minecart";
				ptype = "o";
			}
			else if (entity.ShortPrefabName.Contains("supply"))
			{
				resource = "supplycrate";
				ptype = "o";
			}
			else if (entity.ShortPrefabName.Contains("foodbox") || entity.ShortPrefabName.Contains("trash-pile"))
			{
				resource = "foodbox";
				ptype = "o";
			}
			else if (entity.ShortPrefabName.Contains("giftbox"))
			{
				resource = "giftbox";
				ptype = "o";
			}
			else if (entity.ShortPrefabName.Contains("crate"))
			{
				resource = "crate";
				ptype = "o";
			}
		}

		if (EntityCollectionCache.ContainsKey(entity.net.ID))
		{
			EntityCollectionCache.Remove(entity.net.ID);
		}

		if (ptype != null && resource != null)
		{
				double totalmultiplier = 1;
				totalmultiplier = (happyhouractive ? mult_HappyHourMultiplier : 1) * ((VIPMultiplier_Enabled && HasPerm(player, permVIP)) ? mult_VIPMultiplier : 1);
				GiveReward(player, resource, ptype, null, totalmultiplier);
		}
	}

	private void OnEntityDeath(BaseCombatEntity victim, HitInfo info)
	{

		if (victim == null || victim.IsDestroyed || String.IsNullOrWhiteSpace(victim.name)) return;
		if (!OpenReward_Enabled && !KillReward_Enabled && !HarvestReward_Enabled) return;
		if ((victim.name.Contains("servergibs") || victim.name.Contains("corpse")) || victim.name.Contains("assets/prefabs/plants/")) return;  // no money for cleaning up the left over crash/corpse/plants

		BasePlayer	bplayer  = null;
		IPlayer		iplayer  = null;
		string		resource = null;
		string		ptype    = null;

		if (info != null && info.Initiator != null && !String.IsNullOrWhiteSpace(info.Initiator.name))
		{
			if (!(info.Initiator is BasePlayer) || (info.Initiator as BasePlayer).IsNpc) return;
			// second check as I dont trust that to always work
			if (info.Initiator is BaseNpc || info.Initiator is NPCPlayerApex ||
				info.Initiator is NPCPlayer || info.Initiator is NPCMurderer ||
				info.Initiator.name.Contains("scarecrow.prefab")) return;
			if (info.Initiator is BasePlayer)
			{
				try
				{
					bplayer = info.Initiator.ToPlayer();
					if (bplayer != null && bplayer.IPlayer != null)
						iplayer = bplayer.IPlayer;
				}
				catch {}
			}
		}

		// did not get player from info check the damage logs for special cases
		if (bplayer == null)
		{
			float        ptime = 0f;
			TrackPlayer  ECEData;
			ECEData.iplayer = null as IPlayer;
			ECEData.time = 0f;

			// no data to do lookup
			if(victim.net == null || victim.net.ID == null)
				return;

			if (victim is BaseHelicopter || victim is CH47HelicopterAIController || victim is BradleyAPC ||
				victim.name.Contains("patrolhelicopter") || victim.name.Contains("ch47") || victim.name.Contains("bradleyapc"))
			{
				try
				{
					if (EntityCollectionCache.TryGetValue(victim.net.ID, out ECEData))
					{
						ptime = ECEData.time;
						iplayer = ECEData.iplayer;
						if (iplayer != null)
							bplayer = iplayer.Object as BasePlayer;
						EntityCollectionCache.Remove(victim.net.ID);
					}
					else
						//Puts("they already got credit in kill");
						return;  // they already got credit in kill
				}
				catch
				{
					//Puts("error getting cache");
					return;
				}
				if (iplayer == null || bplayer == null) // could not find player from victim
				{
					//Puts("OED no player on heli/bradley/ch47");
					return;
				}
				else if (ptime != 0f && (TOD_Sky.Instance.Cycle.Hour - ptime) >=  shotclock)  // no data or action too old
				{
					//Puts ("OED ptime too old: " + ptime.ToString() + " : " + (TOD_Sky.Instance.Cycle.Hour - shotclock));
					return;
				}
			}
		}

		if (iplayer == null || iplayer.Id == null || !iplayer.IsConnected) // if we did not find the player no one to give the reward to, we can exit
		{
			//Puts("No player found to reward");
			return;
		}

		if (victim.OwnerID != null && info?.InitiatorPlayer?.userID != null && victim.OwnerID == info?.InitiatorPlayer?.userID)
		{
			return;  // no payout for destroying your own stuff
		}

		BasePlayer victimplayer = null as BasePlayer;

		if (ptype == null || resource == null)
		{
			//Puts("RR victim.name: " + victim.name);
			if (victim.name.Contains("loot-barrel") || victim.name.Contains("loot_barrel") || victim.name.Contains("oil_barrel"))
			{
				resource = "barrel";
				ptype = "o";
			}
			else if (victim.name.Contains("foodbox") || victim.name.Contains("trash-pile"))
			{
				resource = "foodbox";
				ptype = "o";
			}
			else if (victim.name.Contains("giftbox"))
			{
				resource = "giftbox";
				ptype = "o";
			}
			else if (victim is BaseHelicopter || victim.name.Contains("patrolhelicopter"))
			{
				resource = "helicopter";
				ptype = "k";
			}
			else if (victim.name.Contains("assets/content/vehicles/minicopter/minicopter.entity.prefab"))
			{
				resource = "minicopter";
				ptype = "k";
			}
			else if (victim.name.Contains("assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab"))
			{
				resource = "scrapcopter";
				ptype = "k";
			}
			else if (victim.name.Contains("assets/content/vehicles/boats/rhib/rhib.prefab"))
			{
				resource = "rhib";
				ptype = "k";
			}
			else if (victim.name.Contains("assets/content/vehicles/boats/rowboat/rowboat.prefab"))
			{
				resource = "boat";
				ptype = "k";
			}
			else if (victim.name.Contains("assets/prefabs/deployable/hot air balloon/hotairballoon.prefabb"))
			{
				resource = "balloon";
				ptype = "k";
			}
			else if (victim is BradleyAPC || victim.name.Contains("bradleyapc"))
			{
				resource = "bradley";
				ptype = "k";
			}
			else if (victim is CH47HelicopterAIController || victim.name.Contains("ch47"))
			{
				resource = "chinook";
				ptype = "k";
			}
			else if (victim.name == "assets/prefabs/npc/autoturret/autoturret_deployed.prefab")
			{
				resource = "autoturret";
				ptype = "k";
			}
			else if (victim.name == "assets/prefabs/npc/sam_site_turret/sam_site_turret_deployed.prefab")
			{
				resource = "sam";
				ptype = "k";
			}
			else if (victim.name == "assets/prefabs/deployable/bear trap/beartrap.prefab" ||
					victim.name == "assets/prefabs/deployable/landmine/landmine.prefab" ||
					victim.name == "assets/prefabs/deployable/floor spikes/spikes.floor.prefab" ||
					victim.name == "assets/prefabs/npc/flame turret/flameturret.deployed.prefab" ||
					victim.name == "assets/prefabs/deployable/single shot trap/guntrap.deployed.prefab" ||
					victim.name == "assets/bundled/prefabs/static/spikes_static.prefab")
			{
				resource = "trap";
				ptype = "k";
			}
			else if (victim.name.Contains("log"))
			{
				resource = "wood";
				ptype = "h";
			}
			// (victim is BasePlayer && (victim as BasePlayer).IsNpc) ||
			else if (victim is NPCPlayerApex || victim is NPCPlayer || victim is Scientist || victim is NPCMurderer ||
			         victim.name.Contains("assets/rust.ai/agents/npcplayer") ||
					 victim.name.Contains("assets/prefabs/npc/scarecrow") ||
					 victim.name.Contains("scientist") || victim.name.Contains("human")
			         )
			{
				ptype = "k";
				if (!NPCReward_Enabled || String.IsNullOrWhiteSpace(victim.name)) return;
				else if (victim.name.Contains("bandit_guard"))
				{
					resource = "bandit_guard";
				}
				else if (victim.name.Contains("scientistpeacekeeper"))
				{
					resource = "scientist_peacekeeper";
				}
				else if (victim.name.Contains("scarecrow"))
				{
					resource = "scarecrow";
				}
				else if (victim.name.Contains("heavyscientist"))
				{
					resource = "heavyscientist";
				}

				else if (victim is NPCMurderer)
				{
					resource = "murderer";
				}
				else if (victim is Scientist || victim is NPCPlayerApex || victim.name.Contains("scientist"))
				{
					resource = "scientist";
				}
				else
				{
					resource = "npc";
				}
			}
			else if (!victim.name.Contains("corpse") && (victim is BaseNpc || victim.name.Contains("assets/rust.ai/")))
			{
				//Puts(victim.name);
				ptype = "k";
				if (victim.name.Contains("bear"))
				{
					resource = "bear";
				}
				else if (victim.name.Contains("stag"))
				{
					resource = "stag";
				}
				else if (victim.name.Contains("boar"))
				{
					resource = "boar";
				}
				else if (victim.name.Contains("ridablehorse"))
				{
					resource = "ridablehorse";
				}
				else if (victim.name.Contains("horse"))
				{
					resource = "horse";
				}
				else if (victim.name.Contains("wolf"))
				{
					resource = "wolf";
				}
				else if (victim.name.Contains("chicken"))
				{
					resource = "chicken";
				}
				else if (victim.name.Contains("zombie")) // lumped these in with Murderers
				{
					if (!NPCReward_Enabled) return;
					resource = "murderer";
				}
				else
				{
					Puts("tell mspeedie: OED missing animal: " + victim.name);
				}
			}
			else if (victim is BasePlayer)
			{
				bool isFriend = false;
				victimplayer = victim.ToPlayer();

				if (victimplayer == null || victimplayer.userID == null ||
					String.IsNullOrWhiteSpace(bplayer.UserIDString) ||
					String.IsNullOrWhiteSpace(victimplayer.UserIDString))
					return;  // probably killed themselves but cant tell for sure
				else if (String.Compare(bplayer.UserIDString, "75000000000000000") < 1)  // catches sneaky NPCs
					return;
				else if (String.Compare(victimplayer.UserIDString,"75000000000000000") < 1)  // catches sneaky NPCs
				{
					resource = "npc";
					ptype = "k";
				}
				else if (String.Compare(iplayer.Id, victimplayer.userID.ToString()) == 0 || String.Compare(bplayer.UserIDString,victimplayer.UserIDString) == 0)
				{
					resource = "suicide";  // killed themselves
					ptype = "k";
				}
				else if (bplayer.currentTeam != 0 && victimplayer.currentTeam != 0 && bplayer.currentTeam == victimplayer.currentTeam) // killed a teammate
					return;
				else
				{
					if (friendsloaded)
						try
						{
							isFriend = (bool)Friends?.CallHook("HasFriend",bplayer.UserIDString, victimplayer.UserIDString);
						}
						catch
						{
							isFriend = false;
						}

					if (isFriend) return;  // killing friends is not a profitable strategy
					else if (clansloaded)
					{
						try
						{
							if ((bool)Clans?.CallHook("IsMemberOrAlly", bplayer.UserIDString, victimplayer.userID.ToString()))
								isFriend = true;
						}
						catch
						{
							isFriend = false;
						}
					}
					else if (UseTeams)
					{
						try
						{
							isFriend = bplayer.currentTeam != 0 && victimplayer.currentTeam != 0 && bplayer.currentTeam == victimplayer.currentTeam;
						}
						catch
						{
							isFriend = false;
						}
					}

					if (isFriend) return;  // killing friends is not a profitable strategy
					else
					{
						resource = "player";
						ptype = "k";
					}
				}
			}
		}

		//Puts("OED: Resource :PType: " + resource + " : "+ ptype);

		// nothing to process
		if (String.IsNullOrWhiteSpace(resource) || String.IsNullOrWhiteSpace(ptype)) return;  // did not find one to process
		if (ptype.ToLower() == "k" && Permissions_Enabled && !permission.UserHasPermission(iplayer.Id, KillPermission)) return;
		if (ptype.ToLower() == "o" && Permissions_Enabled && !permission.UserHasPermission(iplayer.Id, OpenPermission)) return;
		if (ptype.ToLower() == "h" && Permissions_Enabled && !permission.UserHasPermission(iplayer.Id, HarvestPermission)) return;

		double     totalmultiplier = 1;
		// compute applicable multipliers
		if (ptype.ToLower() == "k" && (DistanceMultiplier_Enabled || DynamicDistanceMultiplier_Enabled || WeaponMultiplier_Enabled) &&
			info != null && info.Initiator != null && info.Initiator.ToPlayer() != null)
		{

			if (info.WeaponPrefab != null  && !String.IsNullOrWhiteSpace(info.WeaponPrefab.ShortPrefabName))
			{
				string weaponname = info?.WeaponPrefab?.ShortPrefabName;
				//Puts(weaponname + " GetWeapon: " + GetWeapon(weaponname) + " Distance: " + GetDistance(victim.Distance2D(info?.Initiator?.ToPlayer())));
				totalmultiplier = (DistanceMultiplier_Enabled ? GetDistance(victim.Distance2D(info?.Initiator?.ToPlayer())) : 1) *
								  (DynamicDistanceMultiplier_Enabled ? GetDynamicDistance(victim.Distance2D(info?.Initiator?.ToPlayer())) : 1) *
								  (WeaponMultiplier_Enabled ? GetWeapon(weaponname) : 1) *
								  (happyhouractive ? mult_HappyHourMultiplier : 1) * ((VIPMultiplier_Enabled && HasPerm(iplayer, permVIP)) ? mult_VIPMultiplier : 1);
			}
			else
				totalmultiplier = (DistanceMultiplier_Enabled ? GetDistance(victim.Distance2D(info?.Initiator?.ToPlayer())) : 1) *
								  (DynamicDistanceMultiplier_Enabled ? GetDynamicDistance(victim.Distance2D(info?.Initiator?.ToPlayer())) : 1) *
								  (happyhouractive ? mult_HappyHourMultiplier : 1) * ((VIPMultiplier_Enabled && HasPerm(iplayer, permVIP)) ? mult_VIPMultiplier : 1);
		}
		else
		 totalmultiplier = (happyhouractive ? mult_HappyHourMultiplier : 1) * ((VIPMultiplier_Enabled && HasPerm(iplayer, permVIP)) ? mult_VIPMultiplier : 1);

		//if (resource == "player")
		//Puts("resource : ptype: " + resource + " : " + ptype + " : " + victim.name);

		// Give Reward
		GiveReward(iplayer, resource, ptype, victimplayer, totalmultiplier);
	}

	private void GiveReward(IPlayer player, string reason, string ptype, BasePlayer victim, double multiplier = 1.0d)
	{
		if (!Economics && !ServerRewards) return;

		//if (String.IsNullOrWhiteSpace(ptype))
		//	Puts("GiveReward reason: " + reason);
		//if (Permissions_Enabled) Puts ("Permissions enabled");
		//if (!Permissions_Enabled) Puts ("Permissions NOT enabled");
		// safety checks
		if (player == null || String.IsNullOrWhiteSpace(reason) || multiplier == 00d ||
		   (multiplier < 0.001 && multiplier > 0) || (multiplier > -0.001d && multiplier < 0.0d) ||
			player is BaseNpc || player is NPCPlayerApex || player is NPCPlayer || player is NPCMurderer)
			return;

		if (ptype.ToLower() == "o")
		{
			if(!OpenReward_Enabled)
				return;
			//if (permission.UserHasPermission(player.Id, OpenPermission)) Puts("Player has Open");
			//if (false == permission.UserHasPermission(player.Id, OpenPermission)) Puts("Player does not have Open");
			else if (Permissions_Enabled && !permission.UserHasPermission(player.Id, OpenPermission))
				return;
		}
		else if (ptype.ToLower() == "k")
		{
			if(!KillReward_Enabled)
				return;
			//if (permission.UserHasPermission(player.Id, KillPermission)) Puts("Player has Kill");
			//if (!(permission.UserHasPermission(player.Id, KillPermission))) Puts("Player does not have Kill");
			else if (Permissions_Enabled && !permission.UserHasPermission(player.Id, KillPermission))
				return;
		}
		else if (ptype.ToLower() == "h")
		{
			if (!HarvestReward_Enabled)
				return;
			//if (permission.UserHasPermission(player.Id, HarvestPermission)) Puts("Player has Harvest");
			//if (!(permission.UserHasPermission(player.Id, HarvestPermission))) Puts("Player does not have Harvest");
			else if (Permissions_Enabled && !permission.UserHasPermission(player.Id, HarvestPermission))
				return;
		}
		else if (ptype.ToLower() == "p")
		{
			if(!PickupReward_Enabled)
				return;
			//if (permission.UserHasPermission(player.Id, PickupPermission)) Puts("Player has Pickup");
			//if (!(permission.UserHasPermission(player.Id, PickupPermission))) Puts("Player does not have Pickup");
			else if (Permissions_Enabled && !permission.UserHasPermission(player.Id, PickupPermission))
				return;
		}
		else if (ptype.ToLower() == "a")
		{
			if(!ActivityReward_Enabled)
			{
				return;
			}
			//if (permission.UserHasPermission(player.Id, ActivityPermission)) Puts("Player has Activity");
			//if (!(permission.UserHasPermission(player.Id, ActivityPermission))) Puts("Player does not have Activity");
			else if (Permissions_Enabled && !permission.UserHasPermission(player.Id, ActivityPermission))
			{
				return;
			}
		}
		else if (ptype.ToLower() == "w")
		{
			if(!WelcomeMoney_Enabled)
				return;
			//if (permission.UserHasPermission(player.Id, WelcomePermission)) Puts("Player has Welcome");
			//if (!(permission.UserHasPermission(player.Id, WelcomePermission))) Puts("Player does not have Welcome");
			else if (Permissions_Enabled && !permission.UserHasPermission(player.Id, WelcomePermission))
				return;
		}

		double amount = 0.0d;

		if (reason.Contains("barrel"))
			amount = rate_barrel;
		else if (reason.Contains("supplycrate"))
			amount = rate_supplycrate;
		else if (reason.Contains("foodbox"))
			amount = rate_foodbox;
		else if (reason.Contains("giftbox"))
			amount = rate_giftbox;
		else if (reason.Contains("minecart"))
			amount = rate_minecart;
		else if (reason.Contains("crate"))
			amount = rate_crate;
		else if (reason == "player")
			amount = rate_player;
		else if (reason == "suicide")
			amount = rate_suicide;
		else if (reason == "bear")
			amount = rate_bear;
		else if (reason == "wolf")
			amount = rate_wolf;
		else if (reason == "chicken")
			amount = rate_chicken;
		else if (reason == "ridablehorse")
			amount = rate_ridablehorse;
		else if (reason == "horse")
			amount = rate_horse;
		else if (reason == "boar")
			amount = rate_boar;
		else if (reason == "stag")
			amount = rate_stag;
		else if (reason.Contains("cactus"))
			amount = rate_cactus;
		else if (reason == "wood")
			amount = rate_wood;
		else if (reason == "stones")
			amount = rate_stones;
		else if (reason == "sulfur")
			amount = rate_sulfur;
		else if (reason == "ore")
			amount = rate_ore;
		else if (reason == "berry")
			amount = rate_berry;
		else if (reason == "corn")
			amount = rate_corn;
		else if (reason == "hemp")
			amount = rate_hemp;
		else if (reason == "mushrooms")
			amount = rate_mushrooms;
		else if (reason == "potato")
			amount = rate_potato;
		else if (reason == "pumpkin")
			amount = rate_pumpkin;
		else if (reason == "bones")
			amount = rate_bones;
		else if (reason == "helicopter")
			amount = rate_helicopter;
		else if (reason == "chinook")
			amount = rate_chinook;
		else if (reason == "murderer")
			amount = rate_murderer;
		else if (reason == "scarecrow")
			amount = rate_scarecrow;
		else if (reason == "heavyscientist")
			amount = rate_heavyscientist;
		else if (reason == "scientist_peacekeeper")
			amount = rate_scientist_peacekeeper;
		else if (reason == "bandit_guard")
			amount = rate_bandit_guard;
		else if (reason == "scientist")
			amount = rate_scientist;
		else if (reason == "bradley")
			amount = rate_bradley;
		else if (reason == "trap")
			amount = rate_trap;
		else if (reason == "autoturret")
			amount = rate_autoturret;
		else if (reason == "sam")
			amount = rate_sam;
		else if (reason == "activity")
			amount = rate_activityreward;
		else if (reason == "welcomemoney")
			amount = rate_welcomemoney;
		else if (reason == "npc")
			amount = rate_npckill;
		else if (reason == "balloon")
			amount = rate_balloon;
		else if (reason == "boat")
			amount = rate_boat;
		else if (reason == "rhib")
			amount = rate_rhib;
		else if (reason == "minicopter")
			amount = rate_minicopter;
		else if (reason == "scrapcopter")
			amount = rate_scrapcopter;
		else
		{
			amount = 0;
			Puts("Rust Rewards Unknown reason:" + reason);
		}


		//Puts("1: reason : ptype : amount: "  + reason + " : " + ptype + " : " + amount);

		// no reward nothing to process
		if (amount == 0.0d)
			return;
		else if (DoAdvancedVIP && groupmultiplier.Count != 0)
		{
			double temp_mult = 1.0d;
			// loop through groupmultiplier till there is a hit on the table or none left
			//Puts("count gm: " + groupmultiplier.Count);
			foreach(KeyValuePair<string, double> gm in groupmultiplier)
			{
				if (String.IsNullOrWhiteSpace(gm.Key))
				{
					Puts("Empty Group Multiplier name please check your json");
				}
				//else
				//	Puts(gm.Key + " : " + gm.Value.ToString());

				if (!String.IsNullOrWhiteSpace(gm.Key) &&  player.BelongsToGroup(gm.Key))
				{
					if (gm.Value > temp_mult) temp_mult = gm.Value;
				}
			}
			//Puts("multiplier: " + multiplier.ToString());
			//Puts("temp_mult: " + temp_mult.ToString());
			multiplier = multiplier * temp_mult;
		}

		//Puts("count zm: " + zonemultipliers.Count);
		if (UseZoneManagerPlugin && zonemanagerloaded && zonemultipliers.Count > 0)
		{
			double temp_mult = 1.0d;
			// loop through zonemultipliers till there is a hit on the table or none left
			//Puts("count zm: " + zonemultipliers.Count);
			foreach(KeyValuePair<string, double> zm in zonemultipliers)
			{
				if (String.IsNullOrWhiteSpace(zm.Key))
				{
					Puts("Empty Zone Multiplier name please check your json");
				}
				else if ((bool)ZoneManager?.Call("isPlayerInZone",zm.Key, player.Object as BasePlayer))
				{
					if (zm.Value > temp_mult) temp_mult = zm.Value;
					//	Puts(zm.Key + " : " + zm.Value.ToString());
					break;
				}
			}
			//Puts("multiplier: " + multiplier.ToString());
			//Puts("temp_mult: " + temp_mult.ToString());
			multiplier = multiplier * temp_mult;
		}

		// make sure multipler is not zero
		if (multiplier == 0.0d)
		{
			Puts("Rust Rewards Multipler should be greater than zero. reason:" + reason);
			return;
		}

		if (multiplier < 0.0d)
		{
			if (amount < 0.0d)
				multiplier = multiplier * -1.0d;
		}

		if (reason != "suicide")
			amount = amount * multiplier;
		// make sure net amount is not zero or too small
		if (amount == 0)
		{
			Puts("Net amount (amount * multipler) should not be zero. reason:" + reason);
			return;
		}
		else if ((Math.Abs(amount) < 0.01d && UseEconomicsPlugin) || (Math.Abs(amount) < 1.0d && UseServerRewardsPlugin))
		{
			Puts("Net amount is too small: " + amount.ToString() + " for reason: " + reason);
		}

		// Economics
		string formatted_amount = amount.ToString();
		//  these use to be both if but it seems odd to me to pay in two currencies at the same rate
		if (UseServerRewardsPlugin)
		{
			amount = Math.Round(amount, 0);
			formatted_amount = string.Format("{0:#;-#;0}", amount);
		}
		

		if (reason == "player" && TakeMoneyfromVictim && victim != null && victim.userID != null && amount > 0)
		{
			//if (CheckPlayer( victim.userID, (amount)))
			try
			{
				PayPlayer(victim, -1.0d*(amount));
				MessagePlayer(victim.IPlayer, Lang("VictimKilled", victim.UserIDString, victim.displayName), Lang("Prefix", victim.UserIDString), "k");
				if (DoLogging)
				{
					if (ShowcurrencySymbol)
						LogToFile(Name, $"[{DateTime.Now}] " + victim.displayName + " ( " + victim.UserIDString +  " / " + CleanIP(victim.IPlayer.Address) + " ) " + " lost " + amount.ToString("C", CurrencyCulture) + " for " + reason, this);
					else
						LogToFile(Name, $"[{DateTime.Now}] " + victim.displayName + " ( " + victim.UserIDString +  " / " + CleanIP(victim.IPlayer.Address) + " ) " + " lost " + formatted_amount + " for " + reason, this);
				}
				if (PrintInConsole)
					Puts(victim.displayName + " ( " + victim.UserIDString +  " / " + CleanIP(victim.IPlayer.Address) + " ) "  + " lost " + formatted_amount + " for " + reason);
			}
			catch //else
			{
				MessagePlayer(player, Lang("VictimNoMoney", player.Id, victim.displayName), Lang("Prefix", player.Id), "k");
				return;
			}
		}
		//Puts("Amount: " + amount.ToString());
		PayPlayer(player, (amount));

		// Puts("Reason: " + reason);

		if (ShowcurrencySymbol)
			MessagePlayer(player, Lang(reason, player.Id, amount.ToString("C", CurrencyCulture)), Lang("Prefix", player.Id), ptype);
		else
			MessagePlayer(player, Lang(reason, player.Id, formatted_amount), Lang("Prefix", player.Id), ptype);

		if (DoLogging)
		{
			if (ShowcurrencySymbol)
				LogToFile(Name, $"[{DateTime.Now}] " + player.Name + " ( " + player.Id +  " / " + CleanIP(player.Address) + " ) " + " got " + amount.ToString("C", CurrencyCulture) + " for " + reason, this);
			else
				LogToFile(Name, $"[{DateTime.Now}] " + player.Name + " ( " + player.Id +  " / " + CleanIP(player.Address) + " ) "  + " got " + formatted_amount + " for " + reason, this);
		}
		if (PrintInConsole)
			Puts(player.Name + " ( " + player.Id +  " / " + CleanIP(player.Address) + " ) "  + " got " + formatted_amount + " for " + reason);
	}

	#region Commands
	[ChatCommand("rrm")]
	void ChatCommandRRM(BasePlayer player, string command, string[] args)
	{
		IPlayer iplayer = player.IPlayer;

		bool   pstate = true;
		string pref = player_default_settings;   // Havest, Kill, Open, Pickup and Activity
		string pstateString = null;
		string ptype = null;
		string ptypeString = null;

		if (args.Length == 0)
		{
			MessagePlayer(iplayer, Lang("rrm syntax", iplayer.Id), Lang("Prefix", iplayer.Id), null);
			return;
		}
		else if (args.Length == 2)
		{
			if (args[1].Length < 2)
			{
				MessagePlayer(iplayer, Lang("rrm syntax", iplayer.Id), Lang("Prefix", iplayer.Id), null);
				return;
			}

			if (String.IsNullOrWhiteSpace(args[0]) || args[0].Length != 1)
			{
				MessagePlayer(iplayer, Lang("rrm type", iplayer.Id), Lang("Prefix", iplayer.Id), null);
				return;
			}
			
			if (String.IsNullOrWhiteSpace(args[1]) || args[1].Length < 2)
			{
				MessagePlayer(iplayer, Lang("rrm state", iplayer.Id), Lang("Prefix", iplayer.Id), null);
				return;
			}

			ptype = args[0].ToLower();
			if (ptype == "h")
				ptypeString = "harvesting";
			else if (ptype == "k")
				ptypeString = "killing";
			else if (ptype == "o")
				ptypeString = "opening";
			else if (ptype == "p")
				ptypeString = "pickup";
			else if (ptype == "a")
				ptypeString = "activity";
			else
			{
				MessagePlayer(iplayer, Lang("rrm type", iplayer.Id), Lang("Prefix", iplayer.Id), null);
				return;
			}
			pstateString = args[1].ToLower().Substring(0, 2);
			if (pstateString != "on" && pstateString != "of" && pstateString != "tr" && pstateString != "fa" && pstateString != "ye" && pstateString != "no")
			{
				MessagePlayer(iplayer, Lang("rrm state", iplayer.Id), Lang("Prefix", iplayer.Id), null);
				return;
			}
			else
			{
				if (pstateString == "of" || pstateString == "fa" || pstateString == "no")
					pstate = false;
				else
					pstate = true;
			}
			try
			{
				if (playerPrefs.ContainsKey(iplayer.Id))
					playerPrefs.TryGetValue(iplayer.Id, out pref);
				else
				{
					pref = player_default_settings;
					playerPrefs.Add(iplayer.Id, pref);
				}
			}
			catch
			{
				try
				{
					pref = player_default_settings;
					playerPrefs.Add(iplayer.Id, pref);
				}
				catch
				{
					Puts("Error setting player settings in RRM Chat Command, contact MSpeedie");
					return;
				}
			}
			if (pstate)
			{
				pstateString = "on";
				if (pref.IndexOf(ptype) == -1)
					pref = pref + ptype;
			}
			else
			{
				pstateString = "off";
				pref = pref.Replace(ptype, "");
			}
			
			// tidy up the string
			pref = SortString(pref);
			MessagePlayer(iplayer, Lang("rrm changed", iplayer.Id, ptype, pstateString, pref), Lang("Prefix", iplayer.Id), null);
		    playerPrefs[iplayer.Id] = pref;
		    dataFile.WriteObject(playerPrefs);
			return;
		}
		else
		{
			MessagePlayer(iplayer, Lang("rrm syntax", iplayer.Id), Lang("Prefix", iplayer.Id), null);
			return;
		}
	}
	#endregion
}
}