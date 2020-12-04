﻿using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("UpgradeWorkbenches", "Created by Jake_Rich, Edited by MJSU", "2.0.4")]
    [Description("Lets players upgrade workbenches")]
    public class UpgradeWorkbenches : RustPlugin
    {
        private readonly List<int> _workbenchItemIds = new List<int>();

        private const string UsePermission = "upgradeworkbenches.use";
        private const string UpgradePermission = "upgradeworkbenches.upgrade";
        private const string DowngradePermission = "upgradeworkbenches.downgrade";
        private const string RefundPermission = "upgradeworkbenches.refund";
        private const string AccentColor = "#de8732";
        
        private void Init()
        {
            permission.RegisterPermission(UsePermission, this);
            permission.RegisterPermission(UpgradePermission, this);
            permission.RegisterPermission(DowngradePermission, this);
            permission.RegisterPermission(RefundPermission, this);
        }
        
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [LangKeys.Chat] = $"<color=#bebebe>[<color={AccentColor}>{Title}</color>] {{0}}</color>",
                [LangKeys.InfoMessage] = "Workbenches can be upgraded!\nTo upgrade, drag a workbench item into a placed workbench's inventory!",
                [LangKeys.UpgradeNotAllowed] = "You're not allowed to upgrade the workbench",
                [LangKeys.DowngradeNotAllow] = "You're not allowed to downgrade the workbench",
                
            }, this);
        }

        private void OnServerInitialized()
        {
           _workbenchItemIds.Add(ItemManager.itemDictionaryByName["workbench1"].itemid);
           _workbenchItemIds.Add(ItemManager.itemDictionaryByName["workbench2"].itemid);
           _workbenchItemIds.Add(ItemManager.itemDictionaryByName["workbench3"].itemid);
        }

        private object CanMoveItem(Item movedItem, PlayerInventory playerInventory, uint targetContainerId, int targetSlot, int amount)
        {
            if (!_workbenchItemIds.Contains(movedItem.info.itemid))
            {
                return null;
            }

            ItemContainer container = playerInventory.FindContainer(targetContainerId);
            Workbench oldBench = container?.entityOwner as Workbench;
            if (oldBench == null)
            {
                return null;
            }

            BasePlayer player = playerInventory.GetComponent<BasePlayer>();
            if (!HasPermission(player, UsePermission))
            {
                return null;
            }

            int newBenchLevel = int.Parse(movedItem.info.shortname.Replace("workbench", ""));
            if (newBenchLevel == oldBench.Workbenchlevel)
            {
                return null;
            }

            if (newBenchLevel > oldBench.Workbenchlevel && !HasPermission(player, UpgradePermission))
            {
                Chat(player, LangKeys.UpgradeNotAllowed);
                return null;
            }

            if (newBenchLevel < oldBench.Workbenchlevel && !HasPermission(player, DowngradePermission))
            {
                Chat(player, LangKeys.DowngradeNotAllow);
                return null;
            }

            Planner planner = movedItem.GetHeldEntity() as Planner;
            Deployable deployable = planner?.GetDeployable();
            if (deployable == null)
            {
                return null;
            }
            
            Workbench newBench = GameManager.server.CreateEntity(deployable.fullName, container.entityOwner.transform.position, container.entityOwner.transform.rotation) as Workbench;
            if (newBench == null)
            {
                return null;
            }
            
            newBench.OwnerID = container.entityOwner.OwnerID;
            newBench.Spawn();
            newBench.AttachToBuilding(oldBench.buildingID);
            
            if (deployable.placeEffect.isValid)
            {
                Effect.server.Run(deployable.placeEffect.resourcePath, newBench.transform.position, Vector3.up);
            }

            movedItem.UseItem();
            
            foreach(Item item in oldBench.inventory.itemList.ToList())
            {
                player.GiveItem(item);
            }

            if (HasPermission(player, RefundPermission))
            {
                Item oldItem = ItemManager.CreateByName($"workbench{oldBench.Workbenchlevel}");
                player.GiveItem(oldItem);
            }

            player.EndLooting();
            oldBench.Kill();
            newBench.PlayerOpenLoot(player);
            return true;
        }

        private void OnEntityBuilt(Planner planner, GameObject go)
        {
            BasePlayer player = planner.GetOwnerPlayer();
            if (player == null)
            {
                return;
            }
            
            Workbench workbench = go.GetComponent<Workbench>();
            if (workbench == null)
            {
                return;
            }
            
            Chat(player, LangKeys.InfoMessage);
        }

        #region Helper Methods
        private void Chat(BasePlayer player, string key, params object[] args) => PrintToChat(player, Lang(LangKeys.Chat, player, Lang(key, player, args)));

        private string Lang(string key, BasePlayer player = null, params object[] args)
        {
            try
            {
                return string.Format(lang.GetMessage(key, this, player?.UserIDString), args);
            }
            catch (Exception ex)
            {
                PrintError($"Lang Key '{key}' threw exception:\n{ex}");
                throw;
            }
        }

        private bool HasPermission(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);
        #endregion

        #region Classes
        private class LangKeys
        {
            public const string Chat = "Chat";
            public const string InfoMessage = "InfoMessage";
            public const string UpgradeNotAllowed = "UpgradeNotAllowed";
            public const string DowngradeNotAllow = "DowngradeNotAllow";
        }

        #endregion

    }
}