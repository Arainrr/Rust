﻿using System.Collections.Generic;
using Rust;
using System;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Wood Swap", "Orange", "1.0.1")]
    [Description("Swaps wood for charcoal with one command")]
    public class WoodSwap : RustPlugin
    {
        #region Vars
        
        private const string permUse = "woodswap.use";
        
        #endregion

        #region Oxide Hooks
        
        private void Init()
        {
            permission.RegisterPermission(permUse, this);
            cmd.AddChatCommand(config.command, this, nameof(cmdSwapChat));
            cmd.AddConsoleCommand(config.command, this, nameof(cmdSwapConsole));
        }
        
        #endregion

        #region Commands
        
        private void cmdSwapConsole(ConsoleSystem.Arg arg)
        {
            SwapWood(arg.Player());
        }

        private void cmdSwapChat(BasePlayer player)
        {
            SwapWood(player);
        }

        #endregion

        #region Core

        private void SwapWood(BasePlayer player)
        {
            var woodItemList = player.inventory.AllItems().Where(x => x.info.shortname == "wood").ToList();
            if (woodItemList.Count == 0)
            {
                Message(player, "No Wood");
                return;
            }
            
            var countWood = woodItemList.Sum(x => x.amount);
            var countCharcoal = Convert.ToInt32(countWood * config.rate);
            var charcoal = ItemManager.CreateByName("charcoal", countCharcoal);
            
            foreach (var wood in woodItemList)
            {
                wood.GetHeldEntity()?.Kill();
                wood.DoRemove();
            }
            
            player.GiveItem(charcoal);
            Message(player, "Swap Success", countWood, countCharcoal);
        }

        #endregion

        #region Configuration 1.1.0

        private static ConfigData config;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Command")]
            public string command;

            [JsonProperty(PropertyName = "Rate")]
            public float rate;
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                command = "wswap",
                rate = 1.1f
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigData>();

                if (config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                PrintError("Configuration file is corrupt! Unloading plugin...");
                Interface.Oxide.RootPluginManager.RemovePlugin(this);
                return;
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion
        
        #region Localization 1.1.1
        
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"No Wood", "You don't have wood in your inventory!"},
                {"Swap Success", "You successfully swapped wood x{0} into charcoal x{1}"},
            }, this);
        }

        private void Message(BasePlayer player, string messageKey, params object[] args)
        {
            if (player == null)
            {
                return;
            }

            var message = GetMessage(messageKey, player.UserIDString, args);
            player.SendConsoleCommand("chat.add", (object) 0, (object) message);
        }

        private string GetMessage(string messageKey, string playerID, params object[] args)
        {
            return string.Format(lang.GetMessage(messageKey, this, playerID), args);
        }

        #endregion
    }
}