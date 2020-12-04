﻿﻿//#define DEBUG

using System;
using System.Collections.Generic;
using ConVar;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Auto Pickup", "Arainrr", "1.2.9")]
    [Description("Automatically pickup hemp, pumpkin, ore, pickable items, corpse, etc.")]
    public class AutoPickup : RustPlugin
    {
        #region Fields

        [PluginReference] private readonly Plugin Friends, Clans;

        private static AutoPickup instance;
        private static PickupType enabledPickupTypes;
        private const string PERMISSION_USE = "autopickup.use";
        private readonly HashSet<ulong> autoClonePlayers = new HashSet<ulong>();

        [Flags]
        private enum PickupType
        {
            None = 0,
            PlantEntity = 1,
            CollectibleEntity = 1 << 1,
            MurdererCorpse = 1 << 2,
            ScientistCorpse = 1 << 3,
            PlayerCorpse = 1 << 4,
            ItemDropBackpack = 1 << 5,
            ItemDrop = 1 << 6,
            WorldItem = 1 << 7,
            LootContainer = 1 << 8,
        }

        #endregion Fields

        #region Oxide Hooks

        private void Init()
        {
            LoadData();
            UpdateConfig();
            instance = this;
            Unsubscribe(nameof(OnEntitySpawned));
            permission.RegisterPermission(PERMISSION_USE, this);
            cmd.AddChatCommand(configData.chatS.command, this, nameof(CmdAutoPickup));
            enabledPickupTypes = PickupType.None;
            foreach (var entry in configData.autoPickupS)
            {
                if (entry.Value.enabled)
                {
                    enabledPickupTypes |= entry.Key;
                }
            }
            if (!enabledPickupTypes.HasFlag(PickupType.LootContainer))
            {
                Unsubscribe(nameof(CanLootEntity));
            }
        }

        private void UpdateConfig()
        {
            foreach (PickupType pickupType in Enum.GetValues(typeof(PickupType)))
            {
                if (pickupType == PickupType.None) continue;
                if (!configData.autoPickupS.ContainsKey(pickupType))
                {
                    configData.autoPickupS.Add(pickupType, new ConfigData.PickupTypeS { enabled = true, radius = 0.5f });
                }
            }
            foreach (var itemDefinition in ItemManager.GetItemDefinitions())
            {
                if (!configData.worldItemS.itemCategoryS.ContainsKey(itemDefinition.category))
                {
                    configData.worldItemS.itemCategoryS.Add(itemDefinition.category, true);
                }
            }
            foreach (var prefab in GameManifest.Current.entities)
            {
                var entity = GameManager.server.FindPrefab(prefab.ToLower())?.GetComponent<BaseEntity>();
                if (entity == null || string.IsNullOrEmpty(entity.ShortPrefabName)) continue;
                var lootContainer = entity.GetComponent<LootContainer>();
                if (lootContainer != null)
                {
                    if (!configData.lootContainerS.ContainsKey(lootContainer.ShortPrefabName))
                    {
                        configData.lootContainerS.Add(lootContainer.ShortPrefabName, true);
                    }
                    continue;
                }
                var collectibleEntity = entity.GetComponent<CollectibleEntity>();
                if (collectibleEntity != null)
                {
                    if (!configData.collectibleEntityS.ContainsKey(collectibleEntity.ShortPrefabName))
                    {
                        configData.collectibleEntityS.Add(collectibleEntity.ShortPrefabName, true);
                    }
                    continue;
                }
                var plantEntity = entity.GetComponent<GrowableEntity>();
                if (plantEntity != null)
                {
                    if (!configData.plantEntityS.ContainsKey(plantEntity.ShortPrefabName))
                    {
                        configData.plantEntityS.Add(plantEntity.ShortPrefabName, true);
                    }
                    continue;
                }
            }
            SaveConfig();
        }

        private void OnServerInitialized()
        {
            Subscribe(nameof(OnEntitySpawned));
            foreach (var baseNetworkable in BaseNetworkable.serverEntities)
            {
                CheckEntity(baseNetworkable);
            }
        }

        private void OnServerSave() => timer.Once(UnityEngine.Random.Range(0f, 60f), SaveData);

        private void Unload()
        {
            if (AutoPickupHelper.autoPickupHelpers != null)
            {
                foreach (var autoPickupEntity in AutoPickupHelper.autoPickupHelpers.ToArray())
                {
                    UnityEngine.Object.Destroy(autoPickupEntity);
                }
                AutoPickupHelper.autoPickupHelpers.Clear();
            }

            foreach (var player in BasePlayer.activePlayerList)
            {
                DestroyUI(player);
            }
            SaveData();
            instance = null;
            configData = null;
        }

        private object CanLootEntity(BasePlayer player, LootContainer lootContainer)
        {
            if (player == null || lootContainer == null) return null;
            if (permission.UserHasPermission(player.UserIDString, PERMISSION_USE))
            {
                bool enabled;
                if (configData.lootContainerS.TryGetValue(lootContainer.ShortPrefabName, out enabled) && !enabled)
                {
                    return null;
                }
                if (configData.settings.preventPickupLoot && lootContainer.OwnerID.IsSteamId() && !AreFriends(lootContainer.OwnerID, player.userID))
                {
                    return null;
                }
                var autoPickData = GetAutoPickupData(player.userID, true);
                if (autoPickData.enabled && !autoPickData.blockPickupTypes.HasFlag(PickupType.LootContainer))
                {
                    if (CanAutoPickup(player, lootContainer) && PickupLootContainer(player, lootContainer))
                    {
                        return false;
                    }
                }
            }
            return null;
        }

        private void OnEntitySpawned(BaseNetworkable baseNetworkable) => CheckEntity(baseNetworkable, true);

        #endregion Oxide Hooks

        #region Methods

        private static void CheckEntity(BaseNetworkable baseNetworkable, bool justCreated = false)
        {
            if (baseNetworkable == null) return;
            PickupType pickupType;
            switch (baseNetworkable.ShortPrefabName)
            {
                case "murderer_corpse": pickupType = PickupType.MurdererCorpse; break;
                case "scientist_corpse": pickupType = PickupType.ScientistCorpse; break;
                case "player_corpse": pickupType = PickupType.PlayerCorpse; break;
                case "item_drop": pickupType = PickupType.ItemDrop; break;
                case "item_drop_backpack": pickupType = PickupType.ItemDropBackpack; break;
                default:
                    if (baseNetworkable is GrowableEntity)
                    {
                        pickupType = PickupType.PlantEntity;
                        break;
                    }
                    if (baseNetworkable is CollectibleEntity)
                    {
                        pickupType = PickupType.CollectibleEntity;
                        break;
                    }
                    if (baseNetworkable is WorldItem)
                    {
                        pickupType = PickupType.WorldItem;
                        break;
                    }
                    return;
            }
            if (!enabledPickupTypes.HasFlag(pickupType))
            {
                return;
            }
            var autoPickupEntity = baseNetworkable.gameObject.GetComponent<AutoPickupHelper>();
            if (autoPickupEntity != null)
            {
                UnityEngine.Object.Destroy(autoPickupEntity);
            }

            switch (pickupType)
            {
                case PickupType.CollectibleEntity:
                    {
                        bool enabled;
                        if (configData.collectibleEntityS.TryGetValue(baseNetworkable.ShortPrefabName, out enabled) && !enabled)
                        {
                            return;
                        }
                    }
                    break;

                case PickupType.PlantEntity:
                    {
                        bool enabled;
                        if (configData.plantEntityS.TryGetValue(baseNetworkable.ShortPrefabName, out enabled) && !enabled)
                        {
                            return;
                        }
                    }
                    break;

                case PickupType.MurdererCorpse:
                case PickupType.ScientistCorpse:
                case PickupType.PlayerCorpse:
                case PickupType.ItemDrop:
                case PickupType.ItemDropBackpack:
                    break;

                case PickupType.WorldItem:
                    {
                        var worldItem = baseNetworkable as WorldItem;
                        if (worldItem != null)
                        {
                            var item = worldItem.GetItem();
                            if (item != null)
                            {
                                if (configData.worldItemS.itemBlockList.Contains(item.info.shortname)) return;
                                bool enabled;
                                if (configData.worldItemS.itemCategoryS.TryGetValue(item.info.category, out enabled) && !enabled)
                                {
                                    return;
                                }
                            }
                        }
                        if (justCreated)
                        {
                            var collisionDetection = baseNetworkable.gameObject.GetComponent<WorldItemCollisionDetection>();
                            if (collisionDetection != null)
                            {
                                UnityEngine.Object.Destroy(collisionDetection);
                            }
                            baseNetworkable.gameObject.AddComponent<WorldItemCollisionDetection>();
                            return;
                        }
                    }
                    break;
            }
            CreateAutoPickupHelper(baseNetworkable.gameObject, pickupType);
        }

        private static void CreateAutoPickupHelper(GameObject entity, PickupType pickupType)
        {
            var newObject = new GameObject("AutoPickHelper");
            newObject.transform.SetParent(entity.transform);
            newObject.transform.position = entity.transform.position;
            newObject.AddComponent<AutoPickupHelper>().SetPickupType(pickupType);
        }

        private static bool PickupLootContainer(BasePlayer player, LootContainer lootContainer)
        {
            var itemContainer = lootContainer?.inventory;
            if (itemContainer != null)
            {
                for (int i = itemContainer.itemList.Count - 1; i >= 0; i--)
                {
                    player.GiveItem(itemContainer.itemList[i], BaseEntity.GiveItemReason.PickedUp);
                }
                if (itemContainer.itemList == null || itemContainer.itemList.Count <= 0)
                {
                    lootContainer.Kill();
                }
                return true;
            }
            return false;
        }

        private static bool InventoryExistItem(BasePlayer player, Item item)
        {
            return player.inventory.containerMain.FindItemByItemID(item.info.itemid) != null
                   || player.inventory.containerBelt.FindItemByItemID(item.info.itemid) != null;
        }

        private static bool InventoryIsFull(BasePlayer player, Item item)
        {
            if (player.inventory.containerMain.IsFull() && player.inventory.containerBelt.IsFull())
            {
                var item1 = player.inventory.containerMain.FindItemByItemID(item.info.itemid);
                var item2 = player.inventory.containerBelt.FindItemByItemID(item.info.itemid);
                return (item1 == null || !item.CanStack(item1)) && (item2 == null || !item.CanStack(item2));
            }
            return false;
        }

        private static bool PickupDroppedItemContainer(BasePlayer player, DroppedItemContainer droppedItemContainer)
        {
            var itemContainer = droppedItemContainer?.inventory;
            if (itemContainer != null)
            {
                for (int i = itemContainer.itemList.Count - 1; i >= 0; i--)
                {
                    player.GiveItem(itemContainer.itemList[i], BaseEntity.GiveItemReason.PickedUp);
                }
                if (itemContainer.itemList == null || itemContainer.itemList.Count <= 0)
                {
                    droppedItemContainer.Kill();
                }
                return true;
            }
            return false;
        }

        private static bool PickupPlayerCorpse(BasePlayer player, PlayerCorpse playerCorpse)
        {
            var itemContainers = playerCorpse?.containers;
            if (itemContainers != null)
            {
                for (int i = itemContainers.Length - 1; i >= 0; i--)
                {
                    var itemContainer = itemContainers[i];
                    if (itemContainer != null)
                    {
                        for (int j = itemContainer.itemList.Count - 1; j >= 0; j--)
                        {
                            player.GiveItem(itemContainer.itemList[j], BaseEntity.GiveItemReason.PickedUp);
                        }
                    }
                }
                return true;
            }
            return false;
        }

        private static bool CanAutoPickup(BasePlayer player, BaseEntity entity)
        {
            return entity != null && player.CanInteract() && Interface.CallHook("OnAutoPickupEntity", player, entity) == null;
        }

        #region AreFriends

        private bool AreFriends(ulong playerID, ulong friendID)
        {
            if (playerID == friendID) return true;
            if (configData.settings.useTeams && SameTeam(playerID, friendID)) return true;
            if (configData.settings.useFriends && HasFriend(playerID, friendID)) return true;
            if (configData.settings.useClans && SameClan(playerID, friendID)) return true;
            return false;
        }

        private static bool SameTeam(ulong playerID, ulong friendID)
        {
            if (!RelationshipManager.TeamsEnabled()) return false;
            var playerTeam = RelationshipManager.Instance.FindPlayersTeam(playerID);
            if (playerTeam == null) return false;
            var friendTeam = RelationshipManager.Instance.FindPlayersTeam(friendID);
            if (friendTeam == null) return false;
            return playerTeam == friendTeam;
        }

        private bool HasFriend(ulong playerID, ulong friendID)
        {
            if (Friends == null) return false;
            return (bool)Friends.Call("HasFriend", playerID, friendID);
        }

        private bool SameClan(ulong playerID, ulong friendID)
        {
            if (Clans == null) return false;
            //Clans
            var isMember = Clans.Call("IsClanMember", playerID.ToString(), friendID.ToString());
            if (isMember != null) return (bool)isMember;
            //Rust:IO Clans
            var playerClan = Clans.Call("GetClanOf", playerID);
            if (playerClan == null) return false;
            var friendClan = Clans.Call("GetClanOf", friendID);
            if (friendClan == null) return false;
            return (string)playerClan == (string)friendClan;
        }

        #endregion AreFriends

        #endregion Methods

        private class WorldItemCollisionDetection : MonoBehaviour
        {
            private bool waiting;

            private void OnCollisionEnter(Collision collision)
            {
                if (waiting || collision?.gameObject?.layer == null) return;
                waiting = true;
                Invoke(nameof(AddAutoPickupComponent), configData.worldItemS.pickupDelay);
            }

            private void AddAutoPickupComponent()
            {
                CreateAutoPickupHelper(gameObject, PickupType.WorldItem);
                DestroyImmediate(this);
            }
        }

        private class AutoPickupHelper : FacepunchBehaviour
        {
            public static List<AutoPickupHelper> autoPickupHelpers;

            private BaseEntity entity;
            private PickupType pickupType;
            private SphereCollider sphereCollider;

            private void Awake()
            {
                if (autoPickupHelpers == null)
                {
                    autoPickupHelpers = new List<AutoPickupHelper>();
                }
                autoPickupHelpers.Add(this);
                entity = GetComponentInParent<BaseEntity>();
            }

            public void SetPickupType(PickupType pickupType)
            {
                this.pickupType = pickupType;
                transform.position = entity.CenterPoint();
                CreateCollider();
            }

            private void CreateCollider()
            {
                sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.gameObject.layer = (int)Rust.Layer.Reserved1;
                sphereCollider.radius = configData.autoPickupS[pickupType].radius;
                sphereCollider.isTrigger = true;
#if DEBUG
                InvokeRepeating(Tick, 0f, 1f);
#endif
            }

#if DEBUG

            private void Tick()
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    if (player.IsAdmin && Vector3.Distance(transform.position, player.transform.position) < 50f)
                    {
                        player.SendConsoleCommand("ddraw.sphere", 1, Color.cyan, sphereCollider.transform.position, sphereCollider.radius);
                    }
                }
            }

#endif

            private void OnTriggerEnter(Collider collider)
            {
                if (collider?.gameObject?.layer != (int)Rust.Layer.Player_Server) return;
                var player = collider.gameObject.GetComponentInParent<BasePlayer>();
                if (player == null || !player.userID.IsSteamId()) return;
                if (instance.permission.UserHasPermission(player.UserIDString, PERMISSION_USE))
                {
                    var autoPickData = instance.GetAutoPickupData(player.userID, true);
                    if (autoPickData.enabled && !autoPickData.blockPickupTypes.HasFlag(pickupType))
                    {
                        switch (pickupType)
                        {
                            case PickupType.PlantEntity:
                                var plantEntity = entity as GrowableEntity;
                                if (configData.settings.preventPickupPlant && plantEntity.OwnerID.IsSteamId() && !instance.AreFriends(plantEntity.OwnerID, player.userID)) return;
                                if (CanAutoPickup(player, plantEntity))
                                {
                                    if (instance.autoClonePlayers.Contains(player.userID))
                                    {
                                        plantEntity.TakeClones(player);
                                    }
                                    else
                                    {
                                        plantEntity.PickFruit(player);
                                    }
                                    plantEntity.RemoveDying(player);
                                }
                                return;

                            case PickupType.CollectibleEntity:
                                var collectibleEntity = entity as CollectibleEntity;
                                if (CanAutoPickup(player, collectibleEntity))
                                {
                                    collectibleEntity.DoPickup(player);
                                    Destroy(this);
                                }
                                return;

                            case PickupType.ItemDrop:
                            case PickupType.ItemDropBackpack:
                                var droppedItemContainer = entity as DroppedItemContainer;
                                if (configData.settings.preventPickupBackpack && droppedItemContainer.playerSteamID.IsSteamId() && !instance.AreFriends(droppedItemContainer.playerSteamID, player.userID)) return;
                                if (CanAutoPickup(player, droppedItemContainer))
                                {
                                    if (PickupDroppedItemContainer(player, droppedItemContainer))
                                    {
                                        Destroy(this);
                                    }
                                }
                                return;

                            case PickupType.MurdererCorpse:
                            case PickupType.ScientistCorpse:
                            case PickupType.PlayerCorpse:
                                var playerCorpse = entity as PlayerCorpse;
                                if (!playerCorpse.CanLoot()) return;
                                if (configData.settings.preventPickupCorpse && playerCorpse.playerSteamID.IsSteamId() && !instance.AreFriends(playerCorpse.playerSteamID, player.userID)) return;
                                if (CanAutoPickup(player, playerCorpse))
                                {
                                    if (PickupPlayerCorpse(player, playerCorpse))
                                    {
                                        Destroy(this);
                                    }
                                }
                                return;

                            case PickupType.WorldItem:
                                var worldItem = entity as WorldItem;
                                if (CanAutoPickup(player, worldItem))
                                {
                                    var item = worldItem.GetItem();
                                    if (item != null)
                                    {
                                        if (configData.worldItemS.onlyPickupExistItem && !InventoryExistItem(player, item)) return;
                                        if (configData.worldItemS.checkInventoryFull && InventoryIsFull(player, item)) return;
                                        var rpcMessage = default(BaseEntity.RPCMessage);
                                        rpcMessage.player = player;
                                        worldItem.Pickup(rpcMessage);
                                        Destroy(this);
                                    }
                                }
                                return;

                            default:
                                return;
                        }
                    }
                }
            }

            private void OnDestroy()
            {
                Destroy(gameObject);
                autoPickupHelpers?.Remove(this);
            }
        }

        private StoredData.AutoPickData GetAutoPickupData(ulong playerID, bool readOnly = false)
        {
            StoredData.AutoPickData autoPickData;
            if (!storedData.playerAutoPickupData.TryGetValue(playerID, out autoPickData))
            {
                autoPickData = new StoredData.AutoPickData
                {
                    enabled = configData.settings.defaultEnabled
                };
                if (readOnly)
                {
                    return autoPickData;
                }
                storedData.playerAutoPickupData.Add(playerID, autoPickData);
            }

            return autoPickData;
        }

        #region UI

        private const string UINAME_MAIN = "AutoPickupUI_Main";
        private const string UINAME_MENU = "AutoPickupUI_Menu";

        private static void CreateMainUI(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-210 -180", OffsetMax = "210 220" },
                CursorEnabled = true
            }, "Hud", UINAME_MAIN);
            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0.6" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
            }, UINAME_MAIN);
            var titlePanel = container.Add(new CuiPanel
            {
                Image = { Color = "0.42 0.88 0.88 1" },
                RectTransform = { AnchorMin = "0 0.902", AnchorMax = "0.995 1" },
            }, UINAME_MAIN);
            container.Add(new CuiElement
            {
                Parent = titlePanel,
                Components =
                {
                    new CuiTextComponent { Text = instance.Lang("Title", player.UserIDString), FontSize = 20, Align = TextAnchor.MiddleCenter, Color ="1 0 0 1" },
                    new CuiOutlineComponent { Distance = "0.5 0.5", Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.2 0",  AnchorMax = "0.8 1" }
                }
            });
            container.Add(new CuiButton
            {
                Button = { Color = "0.95 0.1 0.1 0.95", Close = UINAME_MAIN },
                Text = { Text = "X", Align = TextAnchor.MiddleCenter, Color = "0 0 0 1", FontSize = 22 },
                RectTransform = { AnchorMin = "0.885 0.05", AnchorMax = "0.995 0.95" }
            }, titlePanel);
            CuiHelper.DestroyUi(player, UINAME_MAIN);
            CuiHelper.AddUi(player, container);
            var autoPickData = instance.GetAutoPickupData(player.userID);
            UpdateMenuUI(player, autoPickData);
        }

        private static void UpdateMenuUI(BasePlayer player, StoredData.AutoPickData autoPickData, PickupType type = PickupType.None)
        {
            if (player == null) return;
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = { Color = "0.1 0.1 0.1 0.4" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.898" },
            }, UINAME_MAIN, UINAME_MENU);
            int i = 0;
            var spacing = 1f / 11;
            var anchors = GetEntryAnchors(i++, spacing);
            CreateEntry(ref container, $"AutoPickupUI Toggle",
                instance.Lang("Status", player.UserIDString),
                autoPickData.enabled
                    ? instance.Lang("Enabled", player.UserIDString)
                    : instance.Lang("Disabled", player.UserIDString), $"0 {anchors[0]}", $"0.995 {anchors[1]}");
            foreach (PickupType pickupType in Enum.GetValues(typeof(PickupType)))
            {
                if (pickupType == PickupType.None || !enabledPickupTypes.HasFlag(pickupType)) continue;
                anchors = GetEntryAnchors(i++, spacing);
                CreateEntry(ref container, $"AutoPickupUI {pickupType}",
                    instance.Lang(pickupType.ToString(), player.UserIDString),
                    autoPickData.blockPickupTypes.HasFlag(pickupType)
                        ? instance.Lang("Disabled", player.UserIDString)
                        : instance.Lang("Enabled", player.UserIDString), $"0 {anchors[0]}", $"0.995 {anchors[1]}");

                if (pickupType == PickupType.PlantEntity && !autoPickData.blockPickupTypes.HasFlag(pickupType))
                {
                    anchors = GetEntryAnchors(i++, spacing);
                    CreateEntry(ref container, $"AutoPickupUI Clone",
                        instance.Lang("AutoClonePlants", player.UserIDString),
                        instance.autoClonePlayers.Contains(player.userID)
                            ? instance.Lang("Enabled", player.UserIDString)
                            : instance.Lang("Disabled", player.UserIDString), $"0 {anchors[0]}", $"0.995 {anchors[1]}");
                }
            }

            CuiHelper.DestroyUi(player, UINAME_MENU);
            CuiHelper.AddUi(player, container);
        }

        private static void CreateEntry(ref CuiElementContainer container, string command, string leftText, string rightText, string anchorMin, string anchorMax)
        {
            var panelName = container.Add(new CuiPanel
            {
                Image = { Color = "0.1 0.1 0.1 0.6" },
                RectTransform = { AnchorMin = anchorMin, AnchorMax = anchorMax },
            }, UINAME_MENU);
            container.Add(new CuiLabel
            {
                Text = { Color = "0 1 1 1", FontSize = 14, Align = TextAnchor.MiddleLeft, Text = leftText },
                RectTransform = { AnchorMin = "0.1 0", AnchorMax = "0.795 1" }
            }, panelName);
            container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0.7", Command = command },
                Text = { Text = rightText, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", FontSize = 14 },
                RectTransform = { AnchorMin = "0.8 0.01", AnchorMax = "0.995 0.99" },
            }, panelName);
        }

        private static float[] GetEntryAnchors(int i, float spacing)
        {
            return new[] { 1f - (i + 1) * spacing, 1f - i * spacing };
        }

        private static void DestroyUI(BasePlayer player) => CuiHelper.DestroyUi(player, UINAME_MAIN);

        #endregion UI

        #region Commands

        [ConsoleCommand("AutoPickupUI")]
        private void CCmdAutoPickupUI(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            var autoPickData = GetAutoPickupData(player.userID);
            switch (arg.Args[0].ToLower())
            {
                case "toggle":
                    autoPickData.enabled = !autoPickData.enabled;
                    break;

                case "clone":
                    if (autoClonePlayers.Contains(player.userID))
                    {
                        autoClonePlayers.Remove(player.userID);
                    }
                    else
                    {
                        autoClonePlayers.Add(player.userID);
                    }
                    break;

                default:
                    PickupType pickupType;
                    if (Enum.TryParse(arg.Args[0], true, out pickupType))
                    {
                        if (autoPickData.blockPickupTypes.HasFlag(pickupType))
                        {
                            autoPickData.blockPickupTypes &= ~pickupType;
                        }
                        else
                        {
                            autoPickData.blockPickupTypes |= pickupType;
                        }
                    }

                    break;
            }
            UpdateMenuUI(player, autoPickData);
        }

        private void CmdAutoPickup(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_USE))
            {
                Print(player, Lang("NotAllowed", player.UserIDString));
                return;
            }
            CreateMainUI(player);
        }

        #endregion Commands

        #region ConfigurationFile

        private static ConfigData configData;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Settings")]
            public Settings settings = new Settings();

            [JsonProperty(PropertyName = "Chat Settings")]
            public ChatS chatS = new ChatS();

            [JsonProperty(PropertyName = "Auto Pickup Settings")]
            public Dictionary<PickupType, PickupTypeS> autoPickupS = new Dictionary<PickupType, PickupTypeS>();

            [JsonProperty(PropertyName = "World Item Pickup Settings")]
            public WorldItemPickupS worldItemS = new WorldItemPickupS();

            [JsonProperty(PropertyName = "Loot Container Pickup Settings")]
            public Dictionary<string, bool> lootContainerS = new Dictionary<string, bool>();

            [JsonProperty(PropertyName = "Collectible Entity Pickup Settings")]
            public Dictionary<string, bool> collectibleEntityS = new Dictionary<string, bool>();

            [JsonProperty(PropertyName = "Plant Entity Pickup Settings")]
            public Dictionary<string, bool> plantEntityS = new Dictionary<string, bool>();

            public class Settings
            {
                [JsonProperty(PropertyName = "Use Teams")]
                public bool useTeams = false;

                [JsonProperty(PropertyName = "Use Clans")]
                public bool useClans = true;

                [JsonProperty(PropertyName = "Use Friends")]
                public bool useFriends = true;

                [JsonProperty(PropertyName = "Auto pickup is enabled by default")]
                public bool defaultEnabled = true;

                [JsonProperty(PropertyName = "Prevent pickup other player's backpack")]
                public bool preventPickupBackpack;

                [JsonProperty(PropertyName = "Prevent pickup other player's corpse")]
                public bool preventPickupCorpse;

                [JsonProperty(PropertyName = "Prevent pickup other player's plant entity")]
                public bool preventPickupPlant;

                [JsonProperty(PropertyName = "Prevent pickup other player's loot container")]
                public bool preventPickupLoot = true;
            }

            public class ChatS
            {
                [JsonProperty(PropertyName = "Chat Command")]
                public string command = "ap";

                [JsonProperty(PropertyName = "Chat Prefix")]
                public string prefix = "<color=#00FFFF>[AutoPickup]</color>: ";

                [JsonProperty(PropertyName = "Chat SteamID Icon")]
                public ulong steamIDIcon = 0;
            }

            public class PickupTypeS
            {
                [JsonProperty(PropertyName = "Enabled")]
                public bool enabled = true;

                [JsonProperty(PropertyName = "Check Radius")]
                public float radius = 0.5f;
            }

            public class WorldItemPickupS
            {
                [JsonProperty(PropertyName = "Auto Pickup Delay")]
                public float pickupDelay = 0.5f;

                [JsonProperty(PropertyName = "Check that player's inventory is full")]
                public bool checkInventoryFull = true;

                [JsonProperty(PropertyName = "Only pickup items that exist in player's inventory")]
                public bool onlyPickupExistItem;

                [JsonProperty(PropertyName = "Item Block List (Item shortname)")]
                public HashSet<string> itemBlockList = new HashSet<string>();

                [JsonProperty(PropertyName = "Allow Pickup Item Category")]
                public Dictionary<ItemCategory, bool> itemCategoryS = new Dictionary<ItemCategory, bool>();
            }

            [JsonProperty(PropertyName = "Version")]
            public VersionNumber version = new VersionNumber(1, 2, 7);
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                configData = Config.ReadObject<ConfigData>();
                if (configData == null)
                {
                    LoadDefaultConfig();
                }
                else
                {
                    UpdateConfigValues();
                }
            }
            catch
            {
                PrintError("The configuration file is corrupted");
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file");
            configData = new ConfigData();
        }

        protected override void SaveConfig() => Config.WriteObject(configData);

        private void UpdateConfigValues()
        {
            if (configData.version < Version)
            {
                if (configData.version <= new VersionNumber(1, 2, 7))
                {
                    if (configData.chatS.prefix == "[AutoPickup]:")
                    {
                        configData.chatS.prefix = "<color=#00FFFF>[AutoPickup]</color>: ";
                    }
                }
                configData.version = Version;
            }
        }

        #endregion ConfigurationFile

        #region DataFile

        private StoredData storedData;

        private class StoredData
        {
            public readonly Dictionary<ulong, AutoPickData> playerAutoPickupData = new Dictionary<ulong, AutoPickData>();

            public class AutoPickData
            {
                public bool enabled;

                //[JsonConverter(typeof(StringEnumConverter))]
                public PickupType blockPickupTypes;
            }
        }

        private void LoadData()
        {
            try
            {
                storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch
            {
                storedData = null;
            }
            finally
            {
                if (storedData == null)
                {
                    ClearData();
                }
            }
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, storedData);

        private void ClearData()
        {
            storedData = new StoredData();
            SaveData();
        }

        #endregion DataFile

        #region LanguageFile

        private void Print(BasePlayer player, string message)
        {
            Player.Message(player, message, configData.chatS.prefix, configData.chatS.steamIDIcon);
        }

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NotAllowed"] = "You do not have permission to use this command",
                ["Enabled"] = "<color=#8ee700>Enabled</color>",
                ["Disabled"] = "<color=#ce422b>Disabled</color>",
                ["Status"] = "Auto Pickup Status",
                ["Title"] = "Auto Pickup UI",
                ["PlantEntity"] = "Auto Pickup Plant Entity",
                ["CollectibleEntity"] = "Auto Pickup Collectible Entity",
                ["MurdererCorpse"] = "Auto Pickup Murderer Corpse",
                ["ScientistCorpse"] = "Auto Pickup Scientist Corpse",
                ["PlayerCorpse"] = "Auto Pickup Player Corpse",
                ["ItemDropBackpack"] = "Auto Pickup Item Drop Backpack",
                ["ItemDrop"] = "Auto Pickup Item Drop",
                ["WorldItem"] = "Auto Pickup World Item",
                ["LootContainer"] = "Auto Pickup Loot Container",
                ["AutoClonePlants"] = "Auto Clone Plants",
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NotAllowed"] = "您没有使用该命令的权限",
                ["Enabled"] = "<color=#8ee700>已启用</color>",
                ["Disabled"] = "<color=#ce422b>已禁用</color>",
                ["Status"] = "自动拾取状态",
                ["Title"] = "自动拾取设置",
                ["PlantEntity"] = "自动拾取农作物",
                ["CollectibleEntity"] = "自动拾取收藏品",
                ["MurdererCorpse"] = "自动拾取僵尸尸体",
                ["ScientistCorpse"] = "自动拾取科学家尸体",
                ["PlayerCorpse"] = "自动拾取玩家尸体",
                ["ItemDropBackpack"] = "自动拾取尸体背包",
                ["ItemDrop"] = "自动拾取掉落容器",
                ["WorldItem"] = "自动拾取掉落物品",
                ["LootContainer"] = "自动拾取战利品容器",
                ["AutoClonePlants"] = "自动克隆植物",
            }, this, "zh-CN");
        }

        #endregion LanguageFile
    }
}