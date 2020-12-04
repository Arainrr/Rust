﻿using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using UnityEngine;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("RestoreUponDeath", "k1lly0u", "0.3.4")]
    [Description("Restores player inventories on death and removes the items from their corpse")]
    class RestoreUponDeath : RustPlugin
    {
        #region Fields        
        private RestoreData restoreData;
        private DynamicConfigFile restorationData;
        #endregion

        #region Oxide Hooks        
        private void OnServerInitialized()
        {
            restorationData = Interface.Oxide.DataFileSystem.GetFile("restoreupondeath_data");
            LoadData();

            if (configData.DropActiveItem)
                Unsubscribe(nameof(CanDropActiveItem));

            foreach (string perm in configData.Permissions.Keys)
                permission.RegisterPermission(!perm.StartsWith("restoreupondeath.") ? $"restoreupondeath.{perm}" : perm, this);
        }

        private void OnServerSave() => SaveData();

        private object CanDropActiveItem(BasePlayer player) => Interface.Call("IsEventPlayer", player) != null ? null : (object)(!HasAnyPermission(player.UserIDString));
        
        private void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (player == null || restoreData.HasRestoreData(player.userID))
                return;

            if (Interface.Call("IsEventPlayer", player) != null)
                return;

            ConfigData.LossAmounts lossAmounts;
            if (HasAnyPermission(player.UserIDString, out lossAmounts))
            {
                restoreData.AddData(player, lossAmounts);
            }
        }

        private void OnPlayerRespawned(BasePlayer player)
        {
            if (player == null || !restoreData.HasRestoreData(player.userID))
                return;

            if (Interface.Call("IsEventPlayer", player) != null)
                return;

            TryRestorePlayer(player);
        }
        #endregion

        #region Functions
        private void TryRestorePlayer(BasePlayer player)
        {
            if (player == null || player.IsDead())
                return;

            if (!player.IsSleeping())
            {
                if (!configData.DefaultItems)
                    StripContainer(player.inventory.containerBelt);

                restoreData.RestorePlayer(player);
            }
            else player.Invoke(() => TryRestorePlayer(player), 0.25f);
        }

        private bool HasAnyPermission(string playerId)
        {
            foreach (string key in configData.Permissions.Keys)
            {
                if (permission.UserHasPermission(playerId, key))
                    return true;
            }
            return false;
        }

        private bool HasAnyPermission(string playerId, out ConfigData.LossAmounts lossAmounts)
        {
            foreach (KeyValuePair<string, ConfigData.LossAmounts> kvp in configData.Permissions)
            {
                if (permission.UserHasPermission(playerId, kvp.Key))
                {
                    lossAmounts = kvp.Value;
                    return true;
                }
            }
            lossAmounts = null;
            return false;
        }

        private void StripContainer(ItemContainer container)
        {
            for (int i = container.itemList.Count - 1; i >= 0; i--)
            {
                Item item = container.itemList[i];
                item.RemoveFromContainer();
                item.Remove();
            }
        }
        #endregion

        #region Config        
        private ConfigData configData;
        private class ConfigData
        {
            [JsonProperty("Give default items upon respawn if the players is having items restored")]
            public bool DefaultItems { get; set; }

            [JsonProperty("Can drop active item on death")]
            public bool DropActiveItem { get; set; }

            [JsonProperty("Percentage of total items lost (Permission Name | Percentage (0 - 100))")]
            public Dictionary<string, LossAmounts> Permissions { get; set; }

            public class LossAmounts
            {
                public int Belt { get; set; }
                public int Wear { get; set; }
                public int Main { get; set; }
            }

            public VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            configData = Config.ReadObject<ConfigData>();

            if (configData.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(configData, true);
        }

        protected override void LoadDefaultConfig() => configData = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                DefaultItems = false,
                DropActiveItem = false,
                Permissions = new Dictionary<string, ConfigData.LossAmounts>
                {
                    ["restoreupondeath.default"] = new ConfigData.LossAmounts()
                    {
                        Belt = 75,
                        Main = 75,
                        Wear = 75
                    },
                    ["restoreupondeath.beltonly"] = new ConfigData.LossAmounts()
                    {
                        Belt = 100,
                        Main = 0,
                        Wear = 0
                    },
                    ["restoreupondeath.admin"] = new ConfigData.LossAmounts()
                    {
                        Belt = 0,
                        Main = 0,
                        Wear = 0
                    }
                },
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (configData.Version < new VersionNumber(0, 3, 0))
                configData.Permissions = baseConfig.Permissions;

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion

        #region Data Management
        public enum ContainerType { Main, Wear, Belt }

        private void SaveData() => restorationData.WriteObject(restoreData);

        private void LoadData()
        {
            try
            {
                restoreData = restorationData.ReadObject<RestoreData>();
            }
            catch
            {
                restoreData = new RestoreData();
            }

            if (restoreData?.restoreData == null)
                restoreData = new RestoreData();
        }

        private class RestoreData
        {
            public Hash<ulong, PlayerData> restoreData = new Hash<ulong, PlayerData>();

            public void AddData(BasePlayer player, ConfigData.LossAmounts lossAmounts)
            {
                restoreData[player.userID] = new PlayerData(player, lossAmounts);
            }

            public void RemoveData(ulong playerId)
            {
                if (HasRestoreData(playerId))
                    restoreData.Remove(playerId);
            }

            public bool HasRestoreData(ulong playerId) => restoreData.ContainsKey(playerId);

            public void RestorePlayer(BasePlayer player)
            {
                PlayerData playerData;
                if (restoreData.TryGetValue(player.userID, out playerData))
                {
                    if (playerData == null || !player.IsConnected)
                        return;

                    RestoreAllItems(player, playerData);
                }
            }

            private void RestoreAllItems(BasePlayer player, PlayerData playerData)
            {                
                if (RestoreItems(player, playerData.containerBelt, ContainerType.Belt) && RestoreItems(player, playerData.containerWear, ContainerType.Wear) && RestoreItems(player, playerData.containerMain, ContainerType.Main))
                {
                    RemoveData(player.userID);
                    player.ChatMessage("Your inventory has been restored");
                }
            }

            internal static bool RestoreItems(BasePlayer player, ItemData[] itemData, ContainerType containerType)
            {
                ItemContainer container = containerType == ContainerType.Belt ? player.inventory.containerBelt : containerType == ContainerType.Wear ? player.inventory.containerWear : player.inventory.containerMain;

                for (int i = 0; i < itemData.Length; i++)
                {
                    Item item = CreateItem(itemData[i]);
                    item.position = itemData[i].position;
                    item.SetParent(container);
                }
                return true;
            }

            internal static Item CreateItem(ItemData itemData)
            {
                Item item = ItemManager.CreateByItemID(itemData.itemid, Mathf.Max(1, itemData.amount), itemData.skin);
                item.condition = itemData.condition;
                item.maxCondition = itemData.maxCondition;

                if (itemData.frequency > 0)
                {
                    ItemModRFListener rfListener = item.info.GetComponentInChildren<ItemModRFListener>();
                    if (rfListener != null)
                    {
                        PagerEntity pagerEntity = BaseNetworkable.serverEntities.Find(item.instanceData.subEntity) as PagerEntity;
                        if (pagerEntity != null)
                        {
                            pagerEntity.ChangeFrequency(itemData.frequency);
                            item.MarkDirty();
                        }
                    }
                }

                if (itemData.instanceData?.IsValid() ?? false)
                    itemData.instanceData.Restore(item);

                BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
                if (weapon != null)
                {
                    if (!string.IsNullOrEmpty(itemData.ammotype))
                        weapon.primaryMagazine.ammoType = ItemManager.FindItemDefinition(itemData.ammotype);
                    weapon.primaryMagazine.contents = itemData.ammo;
                }

                FlameThrower flameThrower = item.GetHeldEntity() as FlameThrower;
                if (flameThrower != null)
                    flameThrower.ammo = itemData.ammo;

                if (itemData.contents != null && item.contents != null)
                {
                    foreach (ItemData contentData in itemData.contents)
                    {
                        Item newContent = ItemManager.CreateByItemID(contentData.itemid, Mathf.Max(1, contentData.amount));
                        if (newContent != null)
                        {
                            newContent.condition = contentData.condition;
                            newContent.MoveToContainer(item.contents);
                        }
                    }
                }
                return item;
            }

            public class PlayerData
            {
                public ItemData[] containerMain;
                public ItemData[] containerWear;
                public ItemData[] containerBelt;

                public PlayerData() { }

                public PlayerData(BasePlayer player, ConfigData.LossAmounts lossAmounts)
                {
                    containerBelt = GetItems(player.inventory.containerBelt, Mathf.Clamp(lossAmounts.Belt, 0, 100));
                    containerMain = GetItems(player.inventory.containerMain, Mathf.Clamp(lossAmounts.Main, 0, 100));
                    containerWear = GetItems(player.inventory.containerWear, Mathf.Clamp(lossAmounts.Wear, 0, 100));
                }

                internal static ItemData[] GetItems(ItemContainer container, int lossPercentage)
                {
                    int keepPercentage = 100 - lossPercentage;

                    int itemCount = keepPercentage == 100 ? container.itemList.Count : Mathf.CeilToInt((float)container.itemList.Count * (float)(keepPercentage / 100f));
                    if (itemCount == 0)
                        return new ItemData[0];

                    ItemData[] itemData = new ItemData[itemCount];

                    for (int i = 0; i < itemCount; i++)
                    {
                        Item item = container.itemList.GetRandom();
                        
                        itemData[i] = new ItemData
                        {
                            itemid = item.info.itemid,
                            amount = item.amount,
                            ammo = item.GetHeldEntity() is BaseProjectile ? (item.GetHeldEntity() as BaseProjectile).primaryMagazine.contents : item.GetHeldEntity() is FlameThrower ? (item.GetHeldEntity() as FlameThrower).ammo : 0,
                            ammotype = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.ammoType.shortname ?? null,
                            position = item.position,
                            skin = item.skin,
                            condition = item.condition,
                            maxCondition = item.maxCondition,
                            frequency = ItemModAssociatedEntity<PagerEntity>.GetAssociatedEntity(item)?.GetFrequency() ?? -1,
                            instanceData = new ItemData.InstanceData(item),
                            contents = item.contents?.itemList.Select(item1 => new ItemData
                            {
                                itemid = item1.info.itemid,
                                amount = item1.amount,
                                condition = item1.condition
                            }).ToArray()
                        };

                        item.RemoveFromContainer();
                        item.Remove();
                    }

                    return itemData;
                }
            }
        }

        public class ItemData
        {
            public int itemid;
            public ulong skin;
            public int amount;
            public float condition;
            public float maxCondition;
            public int ammo;
            public string ammotype;
            public int position;
            public int frequency;
            public InstanceData instanceData;
            public ItemData[] contents;

            public class InstanceData
            {
                public int dataInt;
                public int blueprintTarget;
                public int blueprintAmount;

                public InstanceData() { }
                public InstanceData(Item item)
                {
                    if (item.instanceData == null)
                        return;

                    dataInt = item.instanceData.dataInt;
                    blueprintAmount = item.instanceData.blueprintAmount;
                    blueprintTarget = item.instanceData.blueprintTarget;
                }

                public void Restore(Item item)
                {
                    if (item.instanceData == null)
                        item.instanceData = new ProtoBuf.Item.InstanceData();

                    item.instanceData.ShouldPool = false;

                    item.instanceData.blueprintAmount = blueprintAmount;
                    item.instanceData.blueprintTarget = blueprintTarget;
                    item.instanceData.dataInt = dataInt;

                    item.MarkDirty();
                }

                public bool IsValid()
                {
                    return dataInt != 0 || blueprintAmount != 0 || blueprintTarget != 0;
                }
            }
        }
    }
    #endregion
}
