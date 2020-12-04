﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Rust.Modular;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Modular Car Turrets", "WhiteThunder", "1.0.2")]
    [Description("Allows players to deploy auto turrets onto modular cars.")]
    internal class CarTurrets : CovalencePlugin
    {
        #region Fields

        [PluginReference]
        Plugin VehicleDeployedLocks;

        private const string Permission_DeployCommand = "carturrets.deploy.command";
        private const string Permission_DeployInventory = "carturrets.deploy.inventory";
        private const string Permission_Free = "carturrets.free";

        private const string Permission_Limit_2 = "carturrets.limit.2";
        private const string Permission_Limit_3 = "carturrets.limit.3";
        private const string Permission_Limit_4 = "carturrets.limit.4";

        private const string Permission_AllModules = "carturrets.allmodules";
        private const string Permission_ModuleFormat = "carturrets.{0}";

        private const string Prefab_Entity_AutoTurret = "assets/prefabs/npc/autoturret/autoturret_deployed.prefab";
        private const string Prefab_Entity_ElectricSwitch = "assets/prefabs/deployable/playerioents/simpleswitch/switch.prefab";
        private const string Prefab_Effect_DeployAutoTurret = "assets/prefabs/npc/autoturret/effects/autoturret-deploy.prefab";

        private const int ItemId_AutoTurret = -2139580305;

        private readonly Vector3 TurretSwitchPosition = new Vector3(0, -0.64f, -0.32f);
        private readonly Quaternion TurretBackwardRotation = Quaternion.Euler(0, 180, 0);
        private readonly Quaternion TurretSwitchRotation = Quaternion.Euler(0, 180, 0);

        private Configuration pluginConfig;

        #endregion

        #region Hooks

        private void Init()
        {
            permission.RegisterPermission(Permission_DeployCommand, this);
            permission.RegisterPermission(Permission_DeployInventory, this);
            permission.RegisterPermission(Permission_Free, this);

            permission.RegisterPermission(Permission_Limit_2, this);
            permission.RegisterPermission(Permission_Limit_3, this);
            permission.RegisterPermission(Permission_Limit_4, this);

            permission.RegisterPermission(Permission_AllModules, this);
            foreach (var moduleItemShortName in pluginConfig.ModulePositions.Keys)
                permission.RegisterPermission(GetAutoTurretPermission(moduleItemShortName), this);
        }

        private void OnServerInitialized(bool initialBoot)
        {
            if (initialBoot)
                InitializeAutoTurrets();
        }

        private object CanMoveItem(Item item, PlayerInventory playerInventory, uint targetContainerId, int targetSlot, int amount)
        {
            if (item == null || playerInventory == null) return null;

            var basePlayer = playerInventory.GetComponent<BasePlayer>();
            if (basePlayer == null) return null;

            var targetContainer = playerInventory.FindContainer(targetContainerId);
            if (item.parent == null || item.parent == targetContainer) return null;

            var fromCar = item.parent.entityOwner as ModularCar;
            if (fromCar != null)
                return HandleRemoveTurret(basePlayer, item, fromCar, targetContainer);

            if (targetContainer == null) return null;

            var toCar = targetContainer.entityOwner as ModularCar;
            if (toCar != null)
                return HandleAddTurret(basePlayer, item, toCar, targetContainer, targetSlot);

            return null;
        }

        private object HandleAddTurret(BasePlayer basePlayer, Item item, ModularCar car, ItemContainer targetContainer, int targetSlot)
        {
            var player = basePlayer.IPlayer;

            var itemid = item.info.itemid;
            if (itemid != ItemId_AutoTurret) return null;

            // In case a future update or a plugin adds another storage container to the car
            if (car.Inventory.ModuleContainer != targetContainer) return null;

            if (!player.HasPermission(Permission_DeployInventory))
            {
                ChatMessage(basePlayer, "Generic.Error.NoPermission");
                return null;
            }

            if (!VerifyCarHasAutoTurretCapacity(player, car, replyInChat: true)) return null;

            if (targetSlot == -1)
                targetSlot = FindFirstSuitableSocketIndex(car, basePlayer);

            if (targetSlot == -1)
            {
                ChatMessage(basePlayer, "Deploy.Error.NoSuitableModule");
                return null;
            }

            var moduleItem = targetContainer.GetSlot(targetSlot);
            if (moduleItem == null) return null;

            var vehicleModule = car.GetModuleForItem(moduleItem);
            if (vehicleModule == null) return null;

            if (!HasPermissionToVehicleModule(player, vehicleModule))
            {
                ChatMessage(basePlayer, "Deploy.Error.NoPermissionToModule");
                return null;
            }

            if (GetModuleAutoTurret(vehicleModule) != null)
            {
                ChatMessage(basePlayer, "Deploy.Error.ModuleAlreadyHasTurret");
                return null;
            }

            Vector3 position;
            if (!TryGetAutoTurretPositionForModule(vehicleModule, out position) ||
                DeployWasBlocked(vehicleModule, basePlayer))
                return null;

            if (DeployAutoTurretForPlayer(car, vehicleModule, position, basePlayer, GetItemConditionFraction(item)) == null) return null;

            if (!player.HasPermission(Permission_Free))
                UseItem(basePlayer, item);

            return false;
        }

        private object HandleRemoveTurret(BasePlayer basePlayer, Item moduleItem, ModularCar car, ItemContainer targetContainer)
        {
            if (car.Inventory.ModuleContainer != moduleItem.parent) return null;

            var vehicleModule = car.GetModuleForItem(moduleItem);
            if (vehicleModule == null) return null;
            
            var autoTurret = GetModuleAutoTurret(vehicleModule);
            if (autoTurret == null) return null;

            var turretItem = ItemManager.CreateByItemID(ItemId_AutoTurret);
            if (turretItem == null) return null;

            if (turretItem.info.condition.enabled)
                turretItem.condition = autoTurret.healthFraction * 100;

            if (targetContainer == null)
            {
                if (!basePlayer.inventory.GiveItem(turretItem))
                {
                    turretItem.Remove();
                    return false;
                }
            }
            else if (!turretItem.MoveToContainer(targetContainer))
            {
                turretItem.Remove();
                return false;
            }

            basePlayer.Command("note.inv", ItemId_AutoTurret, 1);
            autoTurret.Kill();
            return null;
        }

        private void OnItemDropped(Item item, BaseEntity itemEntity)
        {
            if (item == null || item.parent == null) return;
            
            var car = item.parent.entityOwner as ModularCar;
            if (car == null) return;

            if (item.info.GetComponent<ItemModVehicleModule>() == null) return;

            var vehicleModule = car.GetModuleForItem(item);
            if (vehicleModule == null) return;

            var autoTurret = GetModuleAutoTurret(vehicleModule);
            if (autoTurret == null) return;

            var turretItem = CreateItemFromAutoTurret(autoTurret);
            if (turretItem == null) return;

            var rigidBody = itemEntity.GetComponent<Rigidbody>();
            turretItem.Drop(itemEntity.transform.position, rigidBody?.velocity ?? Vector3.zero, itemEntity.transform.rotation);
        }

        // Automatically move a deployed turret when a module moves
        // This is not done in the CanMoveItem hook since we don't know if it's being moved yet
        private void OnEntityKill(BaseVehicleModule vehicleModule)
        {
            var moduleItem = vehicleModule.AssociatedItemInstance;
            if (moduleItem == null) return;

            var car = vehicleModule.Vehicle as ModularCar;
            if (car == null) return;

            var autoTurret = GetModuleAutoTurret(vehicleModule);
            if (autoTurret == null) return;

            autoTurret.SetParent(null);

            NextTick(() =>
            {
                if (car == null)
                    autoTurret.Kill();
                else
                {
                    var newModule = car.GetModuleForItem(moduleItem);
                    if (newModule == null)
                        autoTurret.Kill();
                    else
                        autoTurret.SetParent(newModule);
                }
            });
        }

        private object OnSwitchToggle(ElectricSwitch electricSwitch, BasePlayer basePlayer)
        {
            var autoTurret = GetSwitchTurret(electricSwitch);
            if (autoTurret == null) return null;

            var vehicleModule = GetParentVehicleModule(autoTurret);
            if (vehicleModule == null) return null;

            // Since there is currently no OnSwitchToggle-post hook, have to call plugin specifically for compatibility
            if (!CanAccessVehicle(vehicleModule.Vehicle, basePlayer, provideFeedback: false)) return false;
            
            autoTurret.SetIsOnline(!electricSwitch.IsOn());
            if (autoTurret.IsOn())
                autoTurret.InitiateStartup();
            else
                autoTurret.InitiateShutdown();

            return null;
        }

        private object OnTurretTarget(AutoTurret turret, BasePlayer basePlayer)
        {
            if (turret == null || basePlayer == null || GetParentVehicleModule(turret) == null) return null;

            // Don't target human or NPC players in safe zones, unless they are hostile
            if (basePlayer.InSafeZone() && (basePlayer.IsNpc || !basePlayer.IsHostile()))
                return false;

            return null;
        }

        #endregion

        #region API

        private AutoTurret API_DeployAutoTurret(BaseVehicleModule vehicleModule, BasePlayer basePlayer)
        {
            var car = vehicleModule.Vehicle as ModularCar;
            if (car == null) return null;

            Vector3 position;
            if (!TryGetAutoTurretPositionForModule(vehicleModule, out position) ||
                GetModuleAutoTurret(vehicleModule) != null ||
                DeployWasBlocked(vehicleModule, basePlayer))
                return null;

            if (basePlayer == null)
                return DeployAutoTurret(car, vehicleModule, position);
            else
                return DeployAutoTurretForPlayer(car, vehicleModule, position, basePlayer);
        }

        #endregion

        #region Commands

        [Command("carturret")]
        private void CommandDeploy(IPlayer player, string cmd, string[] args)
        {
            if (player.IsServer || !VerifyPermissionAny(player, Permission_DeployCommand)) return;

            var basePlayer = player.Object as BasePlayer;
            ModularCar car;
            BaseVehicleModule vehicleModule;

            if (!VerifyCanBuild(player) ||
                !VerifyVehicleModuleFound(player, out car, out vehicleModule) ||
                !CanAccessVehicle(car, basePlayer) ||
                !VerifyCarHasAutoTurretCapacity(player, car) ||
                !VerifyPermissionToModule(player, vehicleModule))
                return;

            if (GetModuleAutoTurret(vehicleModule) != null)
            {
                ReplyToPlayer(player, "Deploy.Error.ModuleAlreadyHasTurret");
                return;
            }

            Vector3 position;
            if (!TryGetAutoTurretPositionForModule(vehicleModule, out position))
            {
                ReplyToPlayer(player, "Deploy.Error.UnsupportedModule");
                return;
            }

            Item autoTurretItem = null;
            var conditionFraction = 1.0f;

            var isFree = player.HasPermission(Permission_Free);
            if (!isFree)
            {
                autoTurretItem = FindPlayerAutoTurretItem(basePlayer);
                if (autoTurretItem == null)
                {
                    ReplyToPlayer(player, "Deploy.Error.NoTurret");
                    return;
                }
                conditionFraction = GetItemConditionFraction(autoTurretItem);
            }

            if (DeployWasBlocked(vehicleModule, basePlayer)) return;

            if (DeployAutoTurretForPlayer(car, vehicleModule, position, basePlayer, conditionFraction) != null && !isFree && autoTurretItem != null)
                UseItem(basePlayer, autoTurretItem);
        }

        #endregion

        #region Helper Methods

        private bool DeployWasBlocked(BaseVehicleModule vehicleModule, BasePlayer basePlayer)
        {
            object hookResult = Interface.CallHook("OnCarAutoTurretDeploy", vehicleModule, basePlayer);
            return hookResult is bool && (bool)hookResult == false;
        }

        private bool CanAccessVehicle(BaseVehicle vehicle, BasePlayer basePlayer, bool provideFeedback = true)
        {
            if (VehicleDeployedLocks == null) return true;
            var canAccess = VehicleDeployedLocks.Call("API_CanAccessVehicle", basePlayer, vehicle, provideFeedback);
            return !(canAccess is bool) || (bool)canAccess;
        }

        private void InitializeAutoTurrets()
        {
            foreach (var autoTurret in BaseNetworkable.serverEntities.OfType<AutoTurret>())
            {
                var vehicleModule = GetParentVehicleModule(autoTurret);
                if (vehicleModule == null) continue;

                RemoveColliderProtection(autoTurret);

                var turretSwitch = autoTurret.GetComponentInChildren<ElectricSwitch>();
                if (turretSwitch != null)
                    RemoveColliderProtection(turretSwitch);
            }
        }

        private bool VerifyPermissionAny(IPlayer player, params string[] permissionNames)
        {
            foreach (var perm in permissionNames)
                if (player.HasPermission(perm))
                    return true;

            ReplyToPlayer(player, "Generic.Error.NoPermission");
            return false;
        }

        private bool VerifyCanBuild(IPlayer player)
        {
            if ((player.Object as BasePlayer).CanBuild()) return true;
            ReplyToPlayer(player, "Generic.Error.BuildingBlocked");
            return false;
        }

        private bool VerifyVehicleModuleFound(IPlayer player, out ModularCar car, out BaseVehicleModule vehicleModule)
        {
            var basePlayer = player.Object as BasePlayer;
            var entity = GetLookEntity(basePlayer);

            vehicleModule = entity as BaseVehicleModule;
            if (vehicleModule != null)
            {
                car = vehicleModule.Vehicle as ModularCar;
                if (car != null) return true;

                ReplyToPlayer(player, "Deploy.Error.NoCarFound");
                return false;
            }

            car = entity as ModularCar;
            if (car == null)
            {
                var lift = entity as ModularCarGarage;
                car = lift?.carOccupant;
                if (car == null)
                {
                    ReplyToPlayer(player, "Deploy.Error.NoCarFound");
                    return false;
                }
            }

            BaseVehicleModule closestModule = FindAimModule(car, basePlayer);

            if (closestModule != null)
            {
                vehicleModule = closestModule;
                return true;
            }

            ReplyToPlayer(player, "Deploy.Error.NoModules");
            return false;
        }

        private bool VerifyCarHasAutoTurretCapacity(IPlayer player, ModularCar car, bool replyInChat = false)
        {
            var limit = GetCarAutoTurretLimit(car);
            var turretCount = car.GetComponentsInChildren<AutoTurret>().Length;
            if (turretCount < limit) return true;

            if (replyInChat)
                ChatMessage(player.Object as BasePlayer, "Deploy.Error.TurretLimit", limit);
            else
                ReplyToPlayer(player, "Deploy.Error.TurretLimit", limit);

            return false;
        }

        private bool VerifyPermissionToModule(IPlayer player, BaseVehicleModule vehicleModule)
        {
            if (HasPermissionToVehicleModule(player, vehicleModule)) return true;
            ReplyToPlayer(player, "Deploy.Error.NoPermissionToModule");
            return false;
        }

        private BaseVehicleModule FindAimModule(ModularCar car, BasePlayer basePlayer)
        {
            var headRay = basePlayer.eyes.HeadRay();

            BaseVehicleModule closestModule = null;
            float closestDistance = 0;

            for (var socketIndex = 0; socketIndex < car.TotalSockets; socketIndex++)
            {
                BaseVehicleModule currentModule;
                if (car.TryGetModuleAt(socketIndex, out currentModule) && currentModule.FirstSocketIndex == socketIndex)
                {
                    var currentDistance = Vector3.Cross(headRay.direction, currentModule.CenterPoint() - headRay.origin).magnitude;
                    if (ReferenceEquals(closestModule, null))
                    {
                        closestModule = currentModule;
                        closestDistance = currentDistance;
                    }
                    else if (currentDistance < closestDistance)
                    {
                        closestModule = currentModule;
                        closestDistance = currentDistance;
                    }
                }
            }

            return closestModule;
        }

        private int FindFirstSuitableSocketIndex(ModularCar car, BasePlayer basePlayer)
        {
            for (var socketIndex = 0; socketIndex < car.TotalSockets; socketIndex++)
            {
                BaseVehicleModule currentModule;
                if (car.TryGetModuleAt(socketIndex, out currentModule)
                    && currentModule.FirstSocketIndex == socketIndex
                    && HasPermissionToVehicleModule(basePlayer.IPlayer, currentModule)
                    && GetModuleAutoTurret(currentModule) == null)
                {
                    return socketIndex;
                }
            }

            return -1;
        }

        private void UseItem(BasePlayer basePlayer, Item item, int amountToConsume = 1)
        {
            item.amount -= amountToConsume;
            if (item.amount <= 0)
            {
                item.RemoveFromContainer();
                item.Remove();
            }
            else
                item.MarkDirty();

            basePlayer.Command("note.inv", item.info.itemid, -amountToConsume);
        }

        private int GetCarAutoTurretLimit(ModularCar car)
        {
            var defaultLimit = pluginConfig.DefaultLimitPerCar;

            if (car.OwnerID == 0)
                return defaultLimit;

            var ownerIdString = car.OwnerID.ToString();

            if (defaultLimit < 4 && permission.UserHasPermission(ownerIdString, Permission_Limit_4))
                return 4;
            else if (defaultLimit < 3 && permission.UserHasPermission(ownerIdString, Permission_Limit_3))
                return 3;
            else if (defaultLimit < 2 && permission.UserHasPermission(ownerIdString, Permission_Limit_2))
                return 2;

            return defaultLimit;
        }

        private float GetItemConditionFraction(Item item) =>
            item.hasCondition ? item.condition / item.info.condition.max : 1.0f;

        private Item FindPlayerAutoTurretItem(BasePlayer basePlayer) =>
            basePlayer.inventory.FindItemID(ItemId_AutoTurret);

        private Item CreateItemFromAutoTurret(AutoTurret autoTurret)
        {
            var turretItem = ItemManager.CreateByItemID(ItemId_AutoTurret);
            if (turretItem == null) return null;

            if (turretItem.info.condition.enabled)
                turretItem.condition = autoTurret.healthFraction * 100;

            return turretItem;
        }

        private string GetAutoTurretPermissionForModule(BaseVehicleModule vehicleModule) =>
            GetAutoTurretPermission(vehicleModule.AssociatedItemDef.shortname);

        private string GetAutoTurretPermission(string moduleItemShrotName) =>
            string.Format(Permission_ModuleFormat, moduleItemShrotName);

        private bool HasPermissionToVehicleModule(IPlayer player, BaseVehicleModule vehicleModule) =>
            player.HasPermission(Permission_AllModules) ||
            player.HasPermission(GetAutoTurretPermissionForModule(vehicleModule));

        private AutoTurret GetModuleAutoTurret(BaseVehicleModule vehicleModule) =>
            vehicleModule.GetComponentInChildren<AutoTurret>();

        private BaseVehicleModule GetParentVehicleModule(BaseEntity autoTurret) =>
            autoTurret.GetParentEntity() as BaseVehicleModule;

        private AutoTurret GetSwitchTurret(ElectricSwitch electricSwitch) =>
            electricSwitch.GetParentEntity() as AutoTurret;

        private AutoTurret DeployAutoTurretForPlayer(ModularCar car, BaseVehicleModule vehicleModule, Vector3 position, BasePlayer basePlayer, float conditionFraction = 1)
        {
            var autoTurret = DeployAutoTurret(car, vehicleModule, position, conditionFraction, basePlayer.userID);
            if (autoTurret == null) return null;

            autoTurret.authorizedPlayers.Add(new ProtoBuf.PlayerNameID
            {
                userid = basePlayer.userID,
                username = basePlayer.displayName
            });
            autoTurret.SendNetworkUpdate();

            // Allow other plugins to detect the auto turret being deployed (e.g., to add a weapon automatically)
            var turretItem = FindPlayerAutoTurretItem(basePlayer);
            if (turretItem != null)
            {
                RunOnEntityBuilt(turretItem, autoTurret);
            }
            else
            {
                // Temporarily increase the player inventory capacity to ensure there is enough space
                basePlayer.inventory.containerMain.capacity++;
                var temporaryTurretItem = ItemManager.CreateByItemID(ItemId_AutoTurret);
                if (basePlayer.inventory.GiveItem(temporaryTurretItem))
                {
                    RunOnEntityBuilt(temporaryTurretItem, autoTurret);
                    temporaryTurretItem.RemoveFromContainer();
                }
                temporaryTurretItem.Remove();
                basePlayer.inventory.containerMain.capacity--;
            }

            return autoTurret;
        }

        private void RunOnEntityBuilt(Item turretItem, AutoTurret autoTurret) =>
            Interface.CallHook("OnEntityBuilt", turretItem.GetHeldEntity(), autoTurret.gameObject);

        private bool TryGetAutoTurretPositionForModule(BaseVehicleModule vehicleModule, out Vector3 position) =>
            pluginConfig.ModulePositions.TryGetValue(vehicleModule.AssociatedItemDef.shortname, out position);

        private AutoTurret DeployAutoTurret(ModularCar car, BaseVehicleModule vehicleModule, Vector3 position, float conditionFraction = 1, ulong ownerId = 0)
        {
            var autoTurret = GameManager.server.CreateEntity(Prefab_Entity_AutoTurret, position, GetIdealTurretRotation(car, vehicleModule)) as AutoTurret;
            if (autoTurret == null) return null;

            autoTurret.SetFlag(BaseEntity.Flags.Reserved8, true);
            autoTurret.SetParent(vehicleModule);
            autoTurret.OwnerID = ownerId;
            RemoveColliderProtection(autoTurret);
            autoTurret.Spawn();
            autoTurret.SetHealth(autoTurret.MaxHealth() * conditionFraction);
            AttachTurretSwitch(autoTurret);

            Effect.server.Run(Prefab_Effect_DeployAutoTurret, autoTurret.transform.position);

            return autoTurret;
        }

        private ElectricSwitch AttachTurretSwitch(AutoTurret autoTurret)
        {
            var turretSwitch = GameManager.server.CreateEntity(Prefab_Entity_ElectricSwitch, TurretSwitchPosition, TurretSwitchRotation) as ElectricSwitch;
            if (turretSwitch == null) return null;

            turretSwitch.pickup.enabled = false;
            turretSwitch.SetParent(autoTurret);
            RemoveColliderProtection(turretSwitch);
            turretSwitch.Spawn();

            return turretSwitch;
        }

        private Quaternion GetIdealTurretRotation(ModularCar car, BaseVehicleModule vehicleModule) =>
            vehicleModule.FirstSocketIndex + 1 > (car.TotalSockets + 1) / 2 ? TurretBackwardRotation : Quaternion.identity;

        private void RemoveColliderProtection(BaseEntity ent)
        {
            foreach (var meshCollider in ent.GetComponentsInChildren<MeshCollider>())
                UnityEngine.Object.DestroyImmediate(meshCollider);

            UnityEngine.Object.DestroyImmediate(ent.GetComponent<GroundWatch>());
        }

        private BaseEntity GetLookEntity(BasePlayer basePlayer)
        {
            RaycastHit hit;
            if (!Physics.Raycast(basePlayer.eyes.HeadRay(), out hit, 3)) return null;
            return hit.GetEntity();
        }

        #endregion

        #region Configuration

        internal class Configuration : SerializableConfiguration
        {
            [JsonProperty("DefaultLimitPerCar")]
            public int DefaultLimitPerCar = 4;

            [JsonProperty("AutoTurretPositionByModule")]
            public Dictionary<string, Vector3> ModulePositions = new Dictionary<string, Vector3>()
            {
                ["vehicle.1mod.cockpit"] = new Vector3(0, 1.39f, -0.3f),
                ["vehicle.1mod.cockpit.armored"] = new Vector3(0, 1.39f, -0.3f),
                ["vehicle.1mod.cockpit.with.engine"] = new Vector3(0, 1.39f, -0.85f),
                ["vehicle.1mod.engine"] = new Vector3(0, 0.4f, 0),
                ["vehicle.1mod.flatbed"] = new Vector3(0, 0.06f, 0),
                ["vehicle.1mod.passengers.armored"] = new Vector3(0, 1.38f, -0.31f),
                ["vehicle.1mod.rear.seats"] = new Vector3(0, 1.4f, -0.12f),
                ["vehicle.1mod.storage"] = new Vector3(0, 0.61f, 0),
                ["vehicle.1mod.taxi"] = new Vector3(0, 1.38f, -0.13f),
                ["vehicle.2mod.flatbed"] = new Vector3(0, 0.06f, -0.7f),
                ["vehicle.2mod.fuel.tank"] = new Vector3(0, 1.28f, -0.85f),
                ["vehicle.2mod.passengers"] = new Vector3(0, 1.4f, -0.9f)
            };
        }

        private Configuration GetDefaultConfig() => new Configuration();

        #endregion

        #region Configuration Boilerplate

        internal class SerializableConfiguration
        {
            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonHelper.Deserialize(ToJson()) as Dictionary<string, object>;
        }

        internal static class JsonHelper
        {
            public static object Deserialize(string json) => ToObject(JToken.Parse(json));

            private static object ToObject(JToken token)
            {
                switch (token.Type)
                {
                    case JTokenType.Object:
                        return token.Children<JProperty>()
                                    .ToDictionary(prop => prop.Name,
                                                  prop => ToObject(prop.Value));

                    case JTokenType.Array:
                        return token.Select(ToObject).ToList();

                    default:
                        return ((JValue)token).Value;
                }
            }
        }

        private bool MaybeUpdateConfig(SerializableConfiguration config)
        {
            var currentWithDefaults = config.ToDictionary();
            var currentRaw = Config.ToDictionary(x => x.Key, x => x.Value);
            return MaybeUpdateConfigDict(currentWithDefaults, currentRaw);
        }

        private bool MaybeUpdateConfigDict(Dictionary<string, object> currentWithDefaults, Dictionary<string, object> currentRaw)
        {
            bool changed = false;

            foreach (var key in currentWithDefaults.Keys)
            {
                object currentRawValue;
                if (currentRaw.TryGetValue(key, out currentRawValue))
                {
                    var defaultDictValue = currentWithDefaults[key] as Dictionary<string, object>;
                    var currentDictValue = currentRawValue as Dictionary<string, object>;

                    if (defaultDictValue != null)
                    {
                        if (currentDictValue == null)
                        {
                            currentRaw[key] = currentWithDefaults[key];
                            changed = true;
                        }
                        else if (MaybeUpdateConfigDict(defaultDictValue, currentDictValue))
                            changed = true;
                    }
                }
                else
                {
                    currentRaw[key] = currentWithDefaults[key];
                    changed = true;
                }
            }

            return changed;
        }

        protected override void LoadDefaultConfig() => pluginConfig = GetDefaultConfig();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                pluginConfig = Config.ReadObject<Configuration>();
                if (pluginConfig == null)
                {
                    throw new JsonException();
                }

                if (MaybeUpdateConfig(pluginConfig))
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
        }

        protected override void SaveConfig()
        {
            Log($"Configuration changes saved to {Name}.json");
            Config.WriteObject(pluginConfig, true);
        }

        #endregion

        #region Localization

        private void ReplyToPlayer(IPlayer player, string messageName, params object[] args) =>
            player.Reply(string.Format(GetMessage(player, messageName), args));

        private void ChatMessage(BasePlayer basePlayer, string messageName, params object[] args) =>
            basePlayer.ChatMessage(string.Format(GetMessage(basePlayer.IPlayer, messageName), args));

        private string GetMessage(IPlayer player, string messageName, params object[] args)
        {
            var message = lang.GetMessage(messageName, this, player.Id);
            return args.Length > 0 ? string.Format(message, args) : message;
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Generic.Error.NoPermission"] = "You don't have permission to do that.",
                ["Generic.Error.BuildingBlocked"] = "Error: Cannot do that while building blocked.",
                ["Deploy.Error.NoCarFound"] = "Error: No car found.",
                ["Deploy.Error.NoModules"] = "Error: That car has no modules.",
                ["Deploy.Error.NoPermissionToModule"] = "You don't have permission to do that to that module type.",
                ["Deploy.Error.ModuleAlreadyHasTurret"] = "Error: That module already has a turret.",
                ["Deploy.Error.UnsupportedModule"] = "Error: That module is not supported.",
                ["Deploy.Error.TurretLimit"] = "Error: That car may only have {0} turret(s).",
                ["Deploy.Error.NoSuitableModule"] = "Error: No suitable module found.",
                ["Deploy.Error.NoTurret"] = "Error: You need an auto turret to do that.",
            }, this, "en");
        }

        #endregion
    }
}
