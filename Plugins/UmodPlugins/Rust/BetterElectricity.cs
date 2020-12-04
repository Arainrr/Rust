﻿using Newtonsoft.Json;
using Oxide.Core;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Better Electricity", "dbteku", "1.2.3")]
    [Description("Allows more control over electricity.")]
    public class BetterElectricity : RustPlugin
    {
        private const string ADMIN_PERM = "betterelectricity.admin";

        private const int LARGE_BATTERY_MAX_DEFAULT = 100;
        private const int MEDIUM_BATTERY_MAX_DEFAULT = 50;
        private const int SMALL_BATTERY_MAX_DEFAULT = 10;

        private static ElectricityConfig config;

        #region Config

        private class ElectricityConfig
        {
            public SolarPanelConfig SolarPanelConfig { get; set; }
            public LargeBatteryConfig LargeBatteryConfig { get; set; }
            public MediumBatteryConfig MediumBatteryConfig { get; set; }
            public SmallBatteryConfig SmallBatteryConfig { get; set; }

            public SmallGeneratorConfig SmallGeneratorConfig { get; set; }

            public MillConfig MillConfig { get; set; }

            public ElectricityConfig()
            {
                SolarPanelConfig = new SolarPanelConfig();
                LargeBatteryConfig = new LargeBatteryConfig();
                MediumBatteryConfig = new MediumBatteryConfig();
                SmallBatteryConfig = new SmallBatteryConfig();
                MillConfig = new MillConfig();
                SmallGeneratorConfig = new SmallGeneratorConfig();
            }
        }

        private class SolarPanelConfig
        {
            public int MaxOutput { get; set; }

            public SolarPanelConfig()
            {
                MaxOutput = 100;
            }
        }

        private class MillConfig
        {
            public int MaxOutput { get; set; }

            public MillConfig()
            {
                MaxOutput = 150;
            }
        }

        private class LargeBatteryConfig
        {
            public int MaxOutput { get; set; }
            public float Efficiency { get; set; }
            public int MaxCapacitySeconds { get; set; }

            public LargeBatteryConfig()
            {
                MaxOutput = 100;
                Efficiency = 0.8f;
                MaxCapacitySeconds = 1440000;
            }
        }

        private class MediumBatteryConfig
        {
            public int MaxOutput { get; set; }
            public float Efficiency { get; set; }
            public int MaxCapacitySeconds { get; set; }

            public MediumBatteryConfig()
            {
                MaxOutput = 50;
                Efficiency = 0.8f;
                MaxCapacitySeconds = 540000;
            }
        }

        private class SmallBatteryConfig
        {
            public int MaxOutput { get; set; }
            public float Efficiency { get; set; }
            public int MaxCapacitySeconds { get; set; }

            public SmallBatteryConfig()
            {
                MaxOutput = 10;
                Efficiency = 0.8f;
                MaxCapacitySeconds = 9000;
            }
        }

        private class SmallGeneratorConfig
        {
            public int MaxOutput { get; set; }
            public SmallGeneratorConfig()
            {
                MaxOutput = 75;
            }
            
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ElectricityConfig>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning(lang.GetMessage(BetterElectricityLang.CONFIG_CREATE_OR_FIX, this));
            config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        private ElectricityConfig GetDefaultConfig()
        {
            return new ElectricityConfig();
        }

        #endregion


        #region Oxide Hooks

        private void OnServerInitialized()
        {
            permission.RegisterPermission(ADMIN_PERM, this);
            ChangeSolarPanels();
            ChangeBatteries();
            ChangeMills();
            ChangeSmallGenerators();
        }

        private void Unload()
        {
            RevertSolarPanels();
            RevertBatteries();
            RevertMills();
            RevertSmallGenerators();
        }

        private void OnEntitySpawned(BaseNetworkable networkObject)
        {
            ElectricBattery battery = networkObject.GetComponent<ElectricBattery>();
            if(battery != null)
            {
                AdjustBattery(battery);
            }
            SolarPanel panel = networkObject.GetComponent<SolarPanel>();
            if(panel != null)
            {
                AdjustSolarPanel(panel);
            }

            ElectricWindmill mill = networkObject.GetComponent<ElectricWindmill>();
            if (mill != null)
            {
                AdjustMill(mill);
            }

            FuelGenerator generator = networkObject.GetComponent<FuelGenerator>();

            if(generator != null)
            {
                AdjustGenerator(generator);
            }

        }

        #endregion

        #region lang

        private class BetterElectricityLang
        {
            public static Dictionary<string, string> lang = new Dictionary<string, string>();
            public static string FIND_SOLAR_PANELS_ADJUST = "FindSolarPanelsAdjust";
            public static string FIND_BATTERIES_ADJUST = "FindBatteriesAdjust";
            public static string FIND_MILL_ADJUST = "FindMillAdjust";
            public static string FIND_SMALL_GEN_ADJUST = "FindSmallGenAdjust";
            public static string FIND_SOLAR_PANELS_REVERT = "FindSolarPanelsRevert";
            public static string FIND_BATTERIES_REVERT = "FindBatteriesRevert";
            public static string FIND_MILL_REVERT = "FindMillRevert";
            public static string FIND_SMALL_GEN_REVERT = "FindSmallGenRevert";
            public static string HELP_PLAYER_MENU = "HelpMenu";
            public static string BE_RELOAD_HELP = "BeReloadHelp";
            public static string NO_PERMISSION = "NoPermission";
            public static string CONFIG_CREATE_OR_FIX = "ConfigUpdateOrFix";

        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [BetterElectricityLang.FIND_SOLAR_PANELS_ADJUST] = "Finding and adjusting all Solar Panels. (This may take some time)",
                [BetterElectricityLang.FIND_BATTERIES_ADJUST] = "Finding and adjusting all Batteries. (This may take some time)",
                [BetterElectricityLang.FIND_MILL_ADJUST] = "Finding and adjusting all Mill Turbines. (This may take some time)",
                [BetterElectricityLang.FIND_SMALL_GEN_ADJUST] = "Finding and adjusting all Small Generators. (This may take some time)",
                [BetterElectricityLang.FIND_SOLAR_PANELS_REVERT] = "Finding and reverting all Solar Panels. (This may take some time)",
                [BetterElectricityLang.FIND_BATTERIES_REVERT] = "Finding and reverting all Batteries. (This may take some time)",
                [BetterElectricityLang.FIND_MILL_REVERT] = "Finding and reverting all Mill Turbines. (This may take some time)",
                [BetterElectricityLang.FIND_SMALL_GEN_REVERT] = "Finding and reverting all Small Generators. (This may take some time)",
                [BetterElectricityLang.HELP_PLAYER_MENU] = "====== Player Commands ======",
                [BetterElectricityLang.BE_RELOAD_HELP] = "/belectric reload => Reloads the config.",
                [BetterElectricityLang.NO_PERMISSION] = "No Permission!",
                [BetterElectricityLang.CONFIG_CREATE_OR_FIX] = "Configuration file is corrupt (or doesn't exists), creating new one!",
            }, this);
        }

        #endregion

        #region Utils

        private void Reload()
        {
            RevertMills();
            RevertSolarPanels();
            RevertBatteries();
            RevertSmallGenerators();
            LoadConfig();
            ChangeSolarPanels();
            ChangeBatteries();
            ChangeMills();
            ChangeSmallGenerators();
        }

        private bool HasPermission(BasePlayer player, string perm)
        {
            return player.IsAdmin || permission.UserHasPermission(player.UserIDString, perm);
        }

        #endregion

        #region Core

        private void ChangeSolarPanels()
        {
            // Heavy Initial Load
            Puts(lang.GetMessage(BetterElectricityLang.FIND_SOLAR_PANELS_ADJUST, this));
            foreach (SolarPanel panel in UnityEngine.Object.FindObjectsOfType<SolarPanel>())
            {
                AdjustSolarPanel(panel);
            }
        }

        private void ChangeBatteries()
        {
            // Heavy Initial Load
            Puts(lang.GetMessage(BetterElectricityLang.FIND_BATTERIES_ADJUST, this));
            foreach (ElectricBattery battery in UnityEngine.Object.FindObjectsOfType<ElectricBattery>())
            {
                AdjustBattery(battery);
            }
        }

        private void ChangeMills()
        {
            // Heavy Initial Load
            Puts(lang.GetMessage(BetterElectricityLang.FIND_MILL_ADJUST, this));
            foreach (ElectricWindmill mill in UnityEngine.Object.FindObjectsOfType<ElectricWindmill>())
            {
                AdjustMill(mill);
            }
        }

        private void ChangeSmallGenerators()
        {
            Puts(lang.GetMessage(BetterElectricityLang.FIND_SMALL_GEN_ADJUST, this));
            foreach (FuelGenerator generator in UnityEngine.Object.FindObjectsOfType<FuelGenerator>())
            {
                AdjustGenerator(generator);
            }
        }

        private void RevertBatteries()
        {
            Puts(lang.GetMessage(BetterElectricityLang.FIND_BATTERIES_REVERT, this));
            foreach (ElectricBattery battery in UnityEngine.Object.FindObjectsOfType<ElectricBattery>())
            {
                RevertBattery(battery);
            }
        }

        private void RevertSolarPanels()
        {
            Puts(lang.GetMessage(BetterElectricityLang.FIND_SOLAR_PANELS_REVERT, this));
            foreach (SolarPanel panel in UnityEngine.Object.FindObjectsOfType<SolarPanel>())
            {
                RevertSolarPanel(panel);
            }
        }

        private void RevertMills()
        {
            Puts(lang.GetMessage(BetterElectricityLang.FIND_MILL_REVERT, this));
            foreach (ElectricWindmill mill in UnityEngine.Object.FindObjectsOfType<ElectricWindmill>())
            {
                RevertMill(mill);
            }
        }

        private void RevertSmallGenerators()
        {
            Puts(lang.GetMessage(BetterElectricityLang.FIND_SMALL_GEN_REVERT, this));
            foreach (FuelElectricGenerator generator in UnityEngine.Object.FindObjectsOfType<FuelElectricGenerator>())
            {
                RevertSmallGenerator(generator);
            }
        }

        private void AdjustBattery(ElectricBattery battery)
        {
            if(battery.maxOutput == LARGE_BATTERY_MAX_DEFAULT)
            {
                // Large Battery
                battery.maxOutput = config.LargeBatteryConfig.MaxOutput;
                battery.maxCapactiySeconds = config.LargeBatteryConfig.MaxCapacitySeconds;
                battery.chargeRatio = config.LargeBatteryConfig.Efficiency;
                //battery.maximumInboundEnergyRatio = config.LargeBatteryConfig.Efficiency * 10;
            }
            else if (battery.maxOutput == MEDIUM_BATTERY_MAX_DEFAULT)
            {
                battery.maxOutput = config.MediumBatteryConfig.MaxOutput;
                battery.maxCapactiySeconds = config.MediumBatteryConfig.MaxCapacitySeconds;
                battery.chargeRatio = config.MediumBatteryConfig.Efficiency;
                //battery.maximumInboundEnergyRatio = config.MediumBatteryConfig.Efficiency * 10;
            }
            else if(battery.maxOutput == SMALL_BATTERY_MAX_DEFAULT)
            {
                // Small Battery.
                battery.maxOutput = config.SmallBatteryConfig.MaxOutput;
                battery.maxCapactiySeconds = config.SmallBatteryConfig.MaxCapacitySeconds;
                battery.chargeRatio = config.SmallBatteryConfig.Efficiency;
                //battery.maximumInboundEnergyRatio = config.SmallBatteryConfig.Efficiency * 10;
            }
            battery.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
        }

        private void AdjustSolarPanel(SolarPanel panel)
        {
            panel.maximalPowerOutput = config.SolarPanelConfig.MaxOutput;
        }

        private void AdjustMill(ElectricWindmill mill)
        {
            mill.maxPowerGeneration = config.MillConfig.MaxOutput;
        }

        private void AdjustGenerator(FuelGenerator generator)
        {
            generator.outputEnergy = config.SmallGeneratorConfig.MaxOutput;
        }

        private void RevertBattery(ElectricBattery battery)
        {
            // Based on these values -> https://rust.facepunch.com/blog/november-update#batteryfixes
            if (battery.maxOutput == config.LargeBatteryConfig.MaxOutput)
            {
                // Large battery;
                battery.maxCapactiySeconds = 1440000;
                battery.chargeRatio = 0.8f;
                battery.maximumInboundEnergyRatio = 4;
                battery.maxOutput = LARGE_BATTERY_MAX_DEFAULT;
            }
            else if (battery.maxOutput == config.MediumBatteryConfig.MaxOutput)
            {
                battery.maxCapactiySeconds = 540000;
                battery.chargeRatio = 0.8f;
                battery.maximumInboundEnergyRatio = 4;
                battery.maxOutput = MEDIUM_BATTERY_MAX_DEFAULT;
            }
            else if(battery.maxOutput == config.SmallBatteryConfig.MaxOutput)
            {
                battery.maxCapactiySeconds = 9000;
                battery.chargeRatio = 0.8f;
                battery.maximumInboundEnergyRatio = 4;
                battery.maxOutput = SMALL_BATTERY_MAX_DEFAULT;
            }
            battery.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
        }

        private void RevertSolarPanel(SolarPanel panel)
        {
            panel.maximalPowerOutput = 20;
        }

        private void RevertMill(ElectricWindmill mill)
        {
            mill.maxPowerGeneration = 150;
        }

        private void RevertSmallGenerator(FuelElectricGenerator generator)
        {
            generator.electricAmount = 40;
        }

        #endregion

        #region Commands
        [ChatCommand("belectric")]
        void OnElectricityCommand(BasePlayer player, string command, string[] args)
        {
            if(args.Length == 0)
            {
                SendReply(player, lang.GetMessage(BetterElectricityLang.HELP_PLAYER_MENU, this, player.UserIDString));
                SendReply(player, lang.GetMessage(BetterElectricityLang.BE_RELOAD_HELP, this, player.UserIDString));
            }
            else if(args.Length == 1)
            {
                if (args[0].ToLower() == "reload")
                {
                    if (HasPermission(player, ADMIN_PERM))
                    {
                        Reload();
                    }
                    else
                    {
                        SendReply(player, lang.GetMessage(BetterElectricityLang.NO_PERMISSION, this, player.UserIDString));
                    }
                }
                else
                {
                    SendReply(player, lang.GetMessage(BetterElectricityLang.HELP_PLAYER_MENU, this, player.UserIDString));
                    SendReply(player, lang.GetMessage(BetterElectricityLang.BE_RELOAD_HELP, this, player.UserIDString));
                }
            }
        }
        #endregion

    }
}
