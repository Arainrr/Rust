﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Linq;
using System.Reflection;
using Rust;

namespace Oxide.Plugins
{
    [Info("BoobyTraps", "k1lly0u", "0.2.17")]
    [Description("Booby trap boxes and doors with a variety of traps")]
    class BoobyTraps : RustPlugin
    {
        #region Fields
        [PluginReference] Plugin Clans, Friends;

        private StoredData storedData;
        private DynamicConfigFile data;

        private bool initialized;
                
        private List<ZoneList> radiationZones;
        private List<Timer> trapTimers;

        private Dictionary<string, ItemDefinition> itemDefs;
        private Dictionary<uint, TrapInfo> currentTraps;

        private const string grenadeFX = "assets/prefabs/weapons/f1 grenade/effects/bounce.prefab";
        private const string explosiveFX = "assets/prefabs/locks/keypad/effects/lock.code.denied.prefab";
        private const string beancanFX = "assets/prefabs/weapons/beancan grenade/effects/bounce.prefab";
        private const string radiationFX = "assets/prefabs/weapons/beancan grenade/effects/beancan_grenade_explosion.prefab";
        private const string landmineFX = "assets/bundled/prefabs/fx/weapons/landmine/landmine_trigger.prefab";
        private const string beartrapFX = "assets/bundled/prefabs/fx/beartrap/arm.prefab";
        private const string shockFX = "assets/prefabs/locks/keypad/effects/lock.code.shock.prefab";

        private const string landminePrefab = "assets/prefabs/deployable/landmine/landmine.prefab";
        private const string beartrapPrefab = "assets/prefabs/deployable/bear trap/beartrap.prefab";
        private const string explosivePrefab = "assets/prefabs/tools/c4/explosive.timed.deployed.prefab";
        private const string beancanPrefab = "assets/prefabs/weapons/beancan grenade/grenade.beancan.deployed.prefab";
        private const string grenadePrefab = "assets/prefabs/weapons/f1 grenade/grenade.f1.deployed.prefab";
        private const string firePrefab = "assets/bundled/prefabs/oilfireballsmall.prefab";

        private const string explosivePerm = "boobytraps.explosives";
        private const string deployPerm = "boobytraps.deployables";
        private const string elementPerm = "boobytraps.elements";
        private const string adminPerm = "boobytraps.admin";

        private const int PLAYER_MASK = 131072;
        #endregion

        #region Oxide Hooks
        private void Loaded()
        {
            data = Interface.Oxide.DataFileSystem.GetFile("boobytrap_data");
            data.Settings.Converters = new JsonConverter[] { new StringEnumConverter(), new UnityVector3Converter() };
                       
            radiationZones = new List<ZoneList>();
            trapTimers = new List<Timer>();
            currentTraps = new Dictionary<uint, TrapInfo>();
        }

        private void OnServerInitialized()
        {
            itemDefs = ItemManager.itemList.ToDictionary(i => i.shortname);
            LoadData();
            InitializePlugin();            
        }

        private void Unload()
        {
            for (int i = 0; i < radiationZones.Count; i++)
            {
                radiationZones[i].time.Destroy();
                UnityEngine.Object.Destroy(radiationZones[i].zone.gameObject);
            }

            radiationZones.Clear();
            
            foreach (Timer time in trapTimers)
                time.Destroy();

            SaveData();
        }

        private void OnEntitySpawned(BaseEntity entity)
        {
            if (!initialized || entity == null || entity.net == null)
                return;

            if (entity is SupplyDrop)
            {
                if (configData.AutotrapSettings.UseAirdrops)
                {
                    ProcessEntity(entity, configData.AutotrapSettings.AirdropChance);                    
                }
                return;
            }
            if (entity is LootContainer)
            {
                if (configData.AutotrapSettings.UseLootContainers)
                {
                    ProcessEntity(entity, configData.AutotrapSettings.LootContainerChance);                    
                }
                return;
            }
        }

        private void OnLootEntity(BasePlayer inventory, BaseEntity target)
        {
            if (target == null || target.net == null)
                return;

            TryActivateTrap(target.net.ID, inventory);
        }

        private void OnEntityTakeDamage(BaseCombatEntity target, HitInfo info)
        {
            if (target == null || target.net == null || info == null)
                return;

            TryActivateTrap(target.net.ID, info?.InitiatorPlayer ?? null);
        }

        private void OnEntityDeath(BaseCombatEntity target, HitInfo info)
        {
            if (target == null || target.net == null || info == null)
                return;

            TryActivateTrap(target.net.ID, info?.InitiatorPlayer ?? null);
        }

        private void CanUseDoor(BasePlayer player, BaseLock locks)
        {
            BaseEntity target = locks.GetParentEntity();
            if (target == null || target.net == null)
                return;

            TryActivateTrap(target.net.ID, player);
        }

        private void OnDoorOpened(Door target, BasePlayer player)
        {
            if (target == null || target.net == null)
                return;

            TryActivateTrap(target.net.ID, player);
        }

        private void OnDoorClosed(Door target, BasePlayer player)
        {
            if (target == null || target.net == null)
                return;

            TryActivateTrap(target.net.ID, player);
        }

        private void OnEntityKill(BaseNetworkable entity)
        {
            if (entity == null || entity.net == null)
                return;

            if (currentTraps.ContainsKey(entity.net.ID))
                currentTraps.Remove(entity.net.ID);
        }
        #endregion

        #region Functions
        private void InitializePlugin()
        {
            lang.RegisterMessages(Messages, this);
            permission.RegisterPermission(explosivePerm, this);
            permission.RegisterPermission(deployPerm, this);
            permission.RegisterPermission(elementPerm, this);
            permission.RegisterPermission(adminPerm, this);

            if (!ConVar.Server.radiation)
            {
                configData.TrapTypes[Traps.Radiation].Enabled = false;
                SaveConfig();
            }

            CheckCurrentTraps();           
            initialized = true;
        }

        private void CheckCurrentTraps()
        {
            IEnumerable<uint> entities = BaseEntity.serverEntities.Select(x => x.net.ID);
            uint[] trapIds = currentTraps.Keys.ToArray();

            for (int i = 0; i < trapIds.Length; i++)
            {
                uint entId = trapIds[i];
                if (!entities.Contains(entId))
                    currentTraps.Remove(entId);
            }
        }
        
        private void ProcessEntity(BaseEntity entity, int chance)
        {
            if (!SetRandom(chance))
                return;            

            Traps trap = configData.TrapTypes.Where(x => x.Value.Enabled).ToList().GetRandom().Key;

            SetTrap(entity, trap, string.Empty);

            if (configData.Options.NotifyRandomSetTraps)
                Puts($"Random trap has been set at {entity.transform.position} using trap {trap}");
        }

        private void SetTrap(BaseEntity entity, Traps trap, string owner)
        {
            TrapInfo info = new TrapInfo(trap, entity.transform.position, owner, string.IsNullOrEmpty(owner) ? false : true);            

            currentTraps[entity.net.ID] = info;
        }

        private bool TryPurchaseTrap(BasePlayer player, Traps trap)
        {
            if (configData.Options.OverrideCostsForAdmins && HasPermission(player.UserIDString, adminPerm))
                return true;

            List<TrapCostEntry> costs = configData.TrapTypes[trap].Costs;

            Dictionary<int, int> itemToTake = new Dictionary<int, int>();

            for (int i = 0; i < costs.Count; i++)
            {
                TrapCostEntry cost = costs[i];
                ItemDefinition itemDef;
                if (!itemDefs.TryGetValue(cost.Shortname, out itemDef))
                {
                    PrintError($"Error finding a item with the shortname \"{cost.Shortname}\". Please fix this mistake in your BoobyTrap config!");
                    continue;
                }

                if (!HasEnoughRes(player, itemDef.itemid, cost.Amount))
                {
                    SendReply(player, msg("insufficientResources", player.UserIDString));
                    return false;
                }

                itemToTake[itemDef.itemid] = cost.Amount;
            }

            foreach (KeyValuePair<int, int> item in itemToTake)
                TakeResources(player, item.Key, item.Value);

            return true;
        }

        private void TryActivateTrap(uint Id, BasePlayer player = null)
        {
            if (!IsBoobyTrapped(Id))
                return;

            TrapInfo info = currentTraps[Id];

            if (player != null)
            {
                if (configData.Options.IgnoreTriggerForTrapOwner)
                {
                    if (info.trapOwner == player.UserIDString)
                        return;
                }

                if (configData.Options.IgnoreTriggerForFriendsOfTrapOwner)
                {
                    if (AreFriends(info.trapOwner, player.UserIDString) || IsClanmate(info.trapOwner, player.UserIDString))
                        return;
                }
            }

            string warningFX = string.Empty;
            string prefab = string.Empty;

            Vector3 location = info.location;
            float fuse = configData.TrapTypes[info.trapType].FuseTimer;
            float amount = configData.TrapTypes[info.trapType].DamageAmount;
            float radius = configData.TrapTypes[info.trapType].Radius;

            bool spawnPrefab = false;
            bool radiusSpawn = false;
            bool isRadiation = false;
            bool isFire = false;

            switch (info.trapType)
            {
                case Traps.BeancanGrenade:
                    warningFX = beancanFX;
                    prefab = beancanPrefab;
                    spawnPrefab = true;
                    break;
                case Traps.Grenade:
                    warningFX = grenadeFX;
                    prefab = grenadePrefab;
                    spawnPrefab = true;
                    break;
                case Traps.Explosive:
                    warningFX = explosiveFX;
                    prefab = explosivePrefab;
                    spawnPrefab = true;
                    break;
                case Traps.Landmine:
                    warningFX = landmineFX;
                    prefab = landminePrefab;
                    amount = configData.TrapTypes[Traps.Landmine].Costs[0].Amount;
                    radiusSpawn = true;
                    break;
                case Traps.Beartrap:
                    warningFX = beancanFX;
                    prefab = beartrapPrefab;
                    amount = configData.TrapTypes[Traps.Beartrap].Costs[0].Amount;
                    radiusSpawn = true;
                    break;
                case Traps.Radiation:
                    warningFX = explosiveFX;
                    prefab = radiationFX;
                    isRadiation = true;
                    break;
                case Traps.Fire:
                    warningFX = beancanFX;
                    prefab = firePrefab;
                    isFire = true;
                    break;
                case Traps.Shock:
                    warningFX = explosiveFX;
                    prefab = shockFX;
                    break;
            }

            currentTraps.Remove(Id);

            if (configData.Options.PlayTrapWarningSoundFX)
                Effect.server.Run(warningFX, location);

            if (spawnPrefab)
            {
                BaseEntity entity = GameManager.server.CreateEntity(prefab, location, new Quaternion(), true);
                TimedExplosive timedExplosive = entity.GetComponent<TimedExplosive>();
                entity.Spawn();
                if (timedExplosive != null)
                {
                    timedExplosive.SetFuse(fuse);
                    timedExplosive.explosionRadius = radius;
                    timedExplosive.damageTypes = new List<DamageTypeEntry> { new DamageTypeEntry { amount = amount, type = DamageType.Explosion } };
                }
            }
            else
            {
                trapTimers.Add(timer.In(fuse, () =>
                {
                    if (radiusSpawn)
                    {
                        float angle = 360 / amount;
                        for (int i = 0; i < amount; i++)
                        {
                            float ang = i * angle;
                            Vector3 position = GetPositionOnCircle(location, ang, radius);
                            BaseEntity entity = GameManager.server.CreateEntity(prefab, position, new Quaternion(), true);
                            entity.Spawn();
                        }
                    }
                    else if (isFire)
                    {
                        BaseEntity entity = GameManager.server.CreateEntity(prefab, location, new Quaternion(), true);
                        entity.Spawn();                        
                    }
                    else if (isRadiation)
                    {
                        Effect.server.Run(prefab, location);
                        InitializeZone(location, configData.TrapTypes[Traps.Radiation].DamageAmount, configData.TrapTypes[Traps.Radiation].Duration, configData.TrapTypes[Traps.Radiation].Radius);
                    }
                    else
                    {
                        Effect.server.Run(prefab, location);
                        List<BasePlayer> nearbyPlayers = new List<BasePlayer>();
                        Vis.Entities(location, radius, nearbyPlayers);
                        foreach (BasePlayer nearPlayer in nearbyPlayers)
                            nearPlayer.Hurt(amount, DamageType.ElectricShock, null, true);
                    }
                }));                
            }

            if (configData.Options.NotifyPlayersWhenTrapTriggered && player != null)
                trapTimers.Add(timer.In(fuse, () => SendReply(player, string.Format(msg("triggered", player.UserIDString), info.trapType))));
        }

        private Vector3 GetPositionOnCircle(Vector3 pos, float ang, float radius)
        {
            Vector3 randPos;
            randPos.x = pos.x + radius * Mathf.Sin(ang * Mathf.Deg2Rad);
            randPos.z = pos.z + radius * Mathf.Cos(ang * Mathf.Deg2Rad);
            randPos.y = pos.y;
            var targetPos = GetGroundPosition(randPos);
            return targetPos;
        }

        private Vector3 GetGroundPosition(Vector3 sourcePos)
        {
            RaycastHit hitInfo;

            if (Physics.Raycast(sourcePos, Vector3.down, out hitInfo, LayerMask.GetMask("Terrain", "World", "Construction")))            
                sourcePos.y = hitInfo.point.y;            
            sourcePos.y = Mathf.Max(sourcePos.y, TerrainMeta.HeightMap.GetHeight(sourcePos));
            return sourcePos;
        }

        private BaseEntity FindValidEntity(BasePlayer player, bool set)
        {
            BaseEntity entity = FindEntity(player);
            if (entity == null)
            {
                SendReply(player, msg("invalidEntity", player.UserIDString));
                return null;
            }
            if (configData.Options.RequireBuildingPrivToTrap)
            {
                if (player.GetBuildingPrivilege() == null || !player.CanBuild())
                {
                    SendReply(player, msg("noPrivilege", player.UserIDString));
                    return null;
                }
            }
            if (configData.Options.RequireOwnershipToTrap)
            {
                if (entity.OwnerID != player.userID)
                {
                    SendReply(player, msg("notOwner", player.UserIDString));
                    return null;
                }
            }
            if (set && currentTraps.ContainsKey(entity.net.ID))
            {
                SendReply(player, msg("hasTrap", player.UserIDString));
                return null;
            }
            return entity;
        }

        private BaseEntity FindEntity(BasePlayer player)
        {
            Vector3 currentRot = Quaternion.Euler(player.serverInput.current.aimAngles) * Vector3.forward;

            Vector3 eyesAdjust = new Vector3(0f, 1.5f, 0f);

            object rayResult = CastRay(player.transform.position + eyesAdjust, currentRot);

            if (rayResult is BaseEntity)
            {
                BaseEntity entity = rayResult as BaseEntity;

                if (entity.GetComponent<SupplyDrop>())
                {
                    if (!configData.Options.CanTrapSupplyDrops)
                        return null;
                }
                else if (entity.GetComponent<LootContainer>())
                {
                    if (!configData.Options.CanTrapLoot)
                        return null;
                }
                else if (entity.GetComponent<StorageContainer>())
                {
                    if (!configData.Options.CanTrapBoxes)
                        return null;
                }
                else if (entity.GetComponent<Door>())
                {
                    if (!configData.Options.CanTrapDoors)
                        return null;
                }
                
                return entity;
            }
            return null;
        }

        private object CastRay(Vector3 Pos, Vector3 Aim)
        {
            RaycastHit[] hits = Physics.RaycastAll(Pos, Aim);

            float distance = 100;
            object target = null;

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
           
                if (hit.collider.GetComponentInParent<BaseEntity>() != null)
                {
                    if (hit.distance < distance)
                    {
                        distance = hit.distance;
                        target = hit.collider.GetComponentInParent<BaseEntity>();
                    }
                }               
            }
            return target;
        }

        private void SendEchoConsole(Network.Connection cn, string msg)
        {
            if (Network.Net.sv.IsConnected())
            {
                Network.Net.sv.write.Start();
                Network.Net.sv.write.PacketID(Network.Message.Type.ConsoleMessage);
                Network.Net.sv.write.String(msg);
                Network.Net.sv.write.Send(new Network.SendInfo(cn));
            }
        }
        #endregion

        #region Helpers
        private bool HasPermission(string userId, string perm) => permission.UserHasPermission(userId, perm);

        private bool HasAnyPerm(string userId) => (HasPermission(userId, explosivePerm) || HasPermission(userId, deployPerm) || HasPermission(userId, elementPerm) || HasPermission(userId, adminPerm));

        private bool IsBoobyTrapped(uint Id) => currentTraps.ContainsKey(Id);

        private void RemoveTrap(uint Id) => currentTraps.Remove(Id);

        private bool HasEnoughRes(BasePlayer player, int itemid, int amount) => player.inventory.GetAmount(itemid) >= amount;

        private void TakeResources(BasePlayer player, int itemid, int amount) => player.inventory.Take(null, itemid, amount);

        private double GrabCurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;

        private int GetRandom(int chance) => UnityEngine.Random.Range(1, chance);

        private bool SetRandom(int chance) => GetRandom(chance) == 1;
        #endregion

        #region Radiation
        private void InitializeZone(Vector3 Location, float intensity, float duration, float radius)
        {
            RadiationZone radiationZone = new GameObject().AddComponent<RadiationZone>();
            radiationZone.Activate(Location, radius, intensity);

            ZoneList listEntry = new ZoneList { zone = radiationZone };

            listEntry.time = timer.Once(duration, () => DestroyZone(listEntry));

            radiationZones.Add(listEntry);
        }

        private void DestroyZone(ZoneList zone)
        {
            if (radiationZones.Contains(zone))
            {
                int index = radiationZones.FindIndex(a => a.zone == zone.zone);
                radiationZones[index].time.Destroy();

                UnityEngine.Object.Destroy(radiationZones[index].zone.gameObject);
                radiationZones.Remove(zone);
            }
        }

        public class ZoneList
        {
            public RadiationZone zone;
            public Timer time;
        }

        public class RadiationZone : MonoBehaviour
        {
            private void Awake()
            {
                gameObject.layer = (int)Layer.Reserved1;
                gameObject.name = "Radiation Zone";
            }
            public void Activate(Vector3 pos, float radius, float amount)
            {                
                transform.position = pos;

                SphereCollider sphereCollider = gameObject.GetComponent<SphereCollider>() ?? gameObject.AddComponent<SphereCollider>();
                sphereCollider.isTrigger = true;
                sphereCollider.radius = radius;

                TriggerRadiation triggerRadiation = gameObject.GetComponent<TriggerRadiation>() ?? gameObject.AddComponent<TriggerRadiation>();
                triggerRadiation.RadiationAmountOverride = amount;
                triggerRadiation.interestLayers = PLAYER_MASK;
                triggerRadiation.enabled = true;

                gameObject.SetActive(true);
                enabled = true;
            }                
        }
        #endregion

        #region Commands
        [ChatCommand("trap")]
        private void cmdTrap(BasePlayer player, string command, string[] args)
        {
            if (!HasAnyPerm(player.UserIDString))
                return;

            if (args.Length == 0)
            {
                SendReply(player, string.Format(msg("help1", player.UserIDString), Title, Version, configData.Options.CanTrapDoors, configData.Options.CanTrapBoxes, configData.Options.CanTrapLoot, configData.Options.CanTrapSupplyDrops));
                SendReply(player, msg("help2", player.UserIDString));

                Dictionary<Traps, TrapEntry> types = configData.TrapTypes;

                if (HasPermission(player.UserIDString, adminPerm))
                {
                    SendReply(player, msg("help3", player.UserIDString));
                    SendReply(player, msg("help4", player.UserIDString));
                }
                else
                {
                    List<string> trapTypes = new List<string>();
                    if (HasPermission(player.UserIDString, explosivePerm))
                    {
                        if (types[Traps.BeancanGrenade].Enabled && !types[Traps.BeancanGrenade].AdminOnly)
                            trapTypes.Add("Beancan");
                        if (types[Traps.Grenade].Enabled && !types[Traps.Grenade].AdminOnly)
                            trapTypes.Add("Grenade");
                        if (types[Traps.Explosive].Enabled && !types[Traps.BeancanGrenade].AdminOnly)
                            trapTypes.Add("Explosive");
                    }
                    if (HasPermission(player.UserIDString, deployPerm))
                    {
                        if (types[Traps.Landmine].Enabled && !types[Traps.Landmine].AdminOnly)
                            trapTypes.Add("Landmine");
                        if (types[Traps.Beartrap].Enabled && !types[Traps.Beartrap].AdminOnly)
                            trapTypes.Add("Beartrap");                        
                    }
                    if (HasPermission(player.UserIDString, elementPerm))
                    {
                        if (types[Traps.Radiation].Enabled && !types[Traps.Radiation].AdminOnly && ConVar.Server.radiation)
                            trapTypes.Add("Radiation");
                        if (types[Traps.Fire].Enabled && !types[Traps.Fire].AdminOnly)
                            trapTypes.Add("Fire");
                        if (types[Traps.Shock].Enabled && !types[Traps.Shock].AdminOnly)
                            trapTypes.Add("Shock");
                    }
                    SendReply(player, $"{msg("help5", player.UserIDString)} <color=#939393>{trapTypes.ToSentence()}</color>");
                }
                return;         
            }
            switch (args[0].ToLower())
            {
                case "cost":
                    if (args.Length > 1)
                    {                        
                        Traps trap;
                        switch (args[1].ToLower())
                        {
                            case "beancan":
                                {
                                    if (!HasPermission(player.UserIDString, explosivePerm) && !HasPermission(player.UserIDString, adminPerm))
                                        return;
                                    trap = Traps.BeancanGrenade;
                                    break;
                                }
                            case "grenade":
                                {
                                    if (!HasPermission(player.UserIDString, explosivePerm) && !HasPermission(player.UserIDString, adminPerm))
                                        return;
                                    trap = Traps.Grenade;
                                    break;
                                }
                            case "explosive":
                                {
                                    if (!HasPermission(player.UserIDString, explosivePerm) && !HasPermission(player.UserIDString, adminPerm))
                                        return;
                                    trap = Traps.Explosive;
                                    break;
                                }
                            case "landmine":
                                {
                                    if (!HasPermission(player.UserIDString, deployPerm) && !HasPermission(player.UserIDString, adminPerm))
                                        return;
                                    trap = Traps.Landmine;
                                    break;
                                }
                            case "beartrap":
                                {
                                    if (!HasPermission(player.UserIDString, deployPerm) && !HasPermission(player.UserIDString, adminPerm))
                                        return;
                                    trap = Traps.Beartrap;
                                    break;
                                }
                            case "radiation":
                                {
                                    if (!ConVar.Server.radiation || (!HasPermission(player.UserIDString, elementPerm) && !HasPermission(player.UserIDString, adminPerm)))
                                        return;
                                    trap = Traps.Radiation;
                                    break;
                                }
                            case "fire":
                                {
                                    if (!HasPermission(player.UserIDString, elementPerm) && !HasPermission(player.UserIDString, adminPerm))
                                        return;
                                    trap = Traps.Fire;
                                    break;
                                }
                            case "shock":
                                {
                                    if (!HasPermission(player.UserIDString, elementPerm) && !HasPermission(player.UserIDString, adminPerm))
                                        return;
                                    trap = Traps.Shock;
                                    break;
                                }
                            default:
                                SendReply(player, msg("invalidTrap", player.UserIDString));
                                return;
                        }
                        if (!configData.TrapTypes[trap].Enabled || (configData.TrapTypes[trap].AdminOnly && !HasPermission(player.UserIDString, adminPerm)))
                        {
                            SendReply(player, msg("notEnabled", player.UserIDString));
                            return;
                        }

                        string costs = string.Format(msg("getCosts", player.UserIDString), trap);

                        for (int i = 0; i < configData.TrapTypes[trap].Costs.Count; i++)
                        {
                            TrapCostEntry cost = configData.TrapTypes[trap].Costs[i];
                            ItemDefinition itemDef;
                            if (!itemDefs.TryGetValue(cost.Shortname, out itemDef))
                            {
                                PrintError($"Error finding a item with the shortname \"{cost.Shortname}\". Please fix this mistake in your BoobyTrap config!");
                                continue;
                            }
                            costs += $"\n<color=#00CC00>{cost.Amount}</color> <color=#939393>x</color> <color=#00CC00>{itemDef.displayName.translated}</color>";
                        }

                        SendReply(player, costs);
                    }
                    return;
                case "set":
                    if (args.Length > 1)
                    {
                        BaseEntity entity = FindValidEntity(player, true);
                        if (entity == null)                                                    
                            return;

                        Traps trap;                    
                        switch (args[1].ToLower())
                        {
                            case "beancan":
                                {
                                    if (!HasPermission(player.UserIDString, explosivePerm) && !HasPermission(player.UserIDString, adminPerm))
                                        return;
                                    trap = Traps.BeancanGrenade;                                    
                                    break;
                                }
                            case "grenade":
                                {
                                    if (!HasPermission(player.UserIDString, explosivePerm) && !HasPermission(player.UserIDString, adminPerm))
                                        return;
                                    trap = Traps.Grenade;
                                    break;
                                }
                            case "explosive":
                                {
                                    if (!HasPermission(player.UserIDString, explosivePerm) && !HasPermission(player.UserIDString, adminPerm))
                                        return;
                                    trap = Traps.Explosive;
                                    break;
                                }
                            case "landmine":
                                {
                                    if (!HasPermission(player.UserIDString, deployPerm) && !HasPermission(player.UserIDString, adminPerm))
                                        return;
                                    trap = Traps.Landmine;
                                    break;
                                }
                            case "beartrap":
                                {
                                    if (!HasPermission(player.UserIDString, deployPerm) && !HasPermission(player.UserIDString, adminPerm))
                                        return;
                                    trap = Traps.Beartrap;
                                    break;
                                }
                            case "radiation":
                                {
                                    if (!HasPermission(player.UserIDString, elementPerm) && !HasPermission(player.UserIDString, adminPerm))
                                        return;
                                    trap = Traps.Radiation;
                                    break;
                                }
                            case "fire":
                                {
                                    if (!HasPermission(player.UserIDString, elementPerm) && !HasPermission(player.UserIDString, adminPerm))
                                        return;
                                    trap = Traps.Fire;
                                    break;
                                }
                            case "shock":
                                {
                                    if (!HasPermission(player.UserIDString, elementPerm) && !HasPermission(player.UserIDString, adminPerm))
                                        return;
                                    trap = Traps.Shock;
                                    break;
                                }
                            default:
                                SendReply(player, msg("invalidTrap", player.UserIDString));
                                return;
                        }
                        if (!configData.TrapTypes[trap].Enabled || (configData.TrapTypes[trap].AdminOnly && !HasPermission(player.UserIDString, adminPerm)))
                        {
                            SendReply(player, msg("notEnabled", player.UserIDString));
                            return;
                        }
                        if (TryPurchaseTrap(player, trap))
                        {
                            SetTrap(entity, trap, player.UserIDString);
                            SendReply(player, string.Format(msg("trapSet", player.UserIDString), trap));
                        }
                    }
                    return;
                case "remove":
                    {
                        BaseEntity entity = FindValidEntity(player, false);
                        if (entity == null)
                            return;
                        if (configData.Options.RequireOwnershipToTrap && (entity.OwnerID != 0U && entity.OwnerID != player.userID))
                        {
                            SendReply(player, msg("notOwner", player.UserIDString));
                            return;
                        }
                        if (!currentTraps.ContainsKey(entity.net.ID))
                        {
                            SendReply(player, msg("noTrap", player.UserIDString));
                            return;
                        }
                        else
                        {
                            currentTraps.Remove(entity.net.ID);
                            SendReply(player, msg("removeSuccess", player.UserIDString));
                            return;
                        }
                    }
                case "check":
                    {
                        BaseEntity entity = FindValidEntity(player, false);
                        if (entity == null)
                            return;
                        if (configData.Options.RequireOwnershipToTrap && (entity.OwnerID != 0U && entity.OwnerID != player.userID))
                        {
                            SendReply(player, msg("notOwner", player.UserIDString));
                            return;
                        }
                        if (!currentTraps.ContainsKey(entity.net.ID))
                        {
                            SendReply(player, msg("noTrap", player.UserIDString));
                            return;
                        }
                        else
                        {
                            TrapInfo info = currentTraps[entity.net.ID];                            
                            SendReply(player, string.Format(msg("trapInfo", player.UserIDString), info.trapType));
                            return;
                        }
                    }
                case "removeall":
                    {
                        if (!player.IsAdmin && !HasPermission(player.UserIDString, adminPerm))
                        {
                            SendReply(player, msg("noPerm", player.UserIDString));
                            return;
                        }
                        currentTraps.Clear();
                        SendReply(player, msg("removedAll", player.UserIDString));
                        return;
                    }
                case "list":
                    {
                        if (!player.IsAdmin && !HasPermission(player.UserIDString, adminPerm))
                        {
                            SendReply(player, msg("noPerm", player.UserIDString));
                            return;
                        }
                        SendEchoConsole(player.net.connection, string.Format(msg("currentTraps", player.UserIDString), currentTraps.Count));
                        Puts(string.Format(msg("currentTraps", player.UserIDString), currentTraps.Count));
                        foreach(var trap in currentTraps)
                        {
                            string trapInfo = string.Format("{0} - {1} - {2}", trap.Key, trap.Value.trapType, trap.Value.location);
                            SendEchoConsole(player.net.connection, trapInfo);
                            Puts(trapInfo);
                        }
                        return;
                    }    
            }
        }

        [ConsoleCommand("trap")]
        private void ccmdTrap(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
                return;

            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, $"-- {Title}  v{Version} --");
                SendReply(arg, msg("conHelp"));
                return;
            }

            switch (arg.Args[0].ToLower())
            {
                case "removeall":
                    currentTraps.Clear();
                    SendReply(arg, msg("removedAll"));
                    return;
                case "list":
                    Puts(string.Format(msg("currentTraps"), currentTraps.Count));

                    foreach (KeyValuePair<uint, TrapInfo> trap in currentTraps)
                    {
                        string trapInfo = string.Format("{0} - {1} - {2}", trap.Key, trap.Value.trapType, trap.Value.location);
                        Puts(trapInfo);
                    }
                    return;
                default:
                    return;
            }
        }
        #endregion

        #region Friends
        private bool AreFriends(string playerId, string friendId)
        {
            if (Friends)
                return (bool)Friends?.Call("AreFriends", playerId, friendId);
            return true;
        }

        private bool IsClanmate(string playerId, string friendId)
        {
            if (Clans)
            {
                object playerTag = Clans?.Call("GetClanOf", playerId);
                object friendTag = Clans?.Call("GetClanOf", friendId);
                if (playerTag is string && friendTag is string)
                {
                    if (!string.IsNullOrEmpty((string)playerTag) && !string.IsNullOrEmpty((string)friendTag) && (playerTag == friendTag))
                        return true;
                }
                return false;
            }
            return true;
        }
        #endregion

        #region Config 
        private ConfigData configData;

        private class TrapCostEntry
        {
            public string Shortname { get; set; }
            public int Amount { get; set; }
        }

        private class TrapEntry
        {
            public bool Enabled { get; set; }
            public bool AdminOnly { get; set; }
            public float DamageAmount { get; set; }
            public float Radius { get; set; }
            public float FuseTimer { get; set; }
            public float Duration { get; set; }
            public List<TrapCostEntry> Costs { get; set; }

        }

        private class Autotraps
        {
            public bool UseAirdrops { get; set; }
            public bool UseLootContainers { get; set; }
            public int AirdropChance { get; set; }
            public int LootContainerChance { get; set; }
        }

        private class Options
        {
            public bool NotifyRandomSetTraps { get; set; }
            public bool NotifyPlayersWhenTrapTriggered { get; set; }
            public bool PlayTrapWarningSoundFX { get; set; }
            public bool CanTrapBoxes { get; set; }
            public bool CanTrapLoot { get; set; }
            public bool CanTrapSupplyDrops { get; set; }
            public bool CanTrapDoors { get; set; }
            public bool RequireOwnershipToTrap { get; set; }
            public bool RequireBuildingPrivToTrap { get; set; }
            public bool IgnoreTriggerForTrapOwner { get; set; }
            public bool IgnoreTriggerForFriendsOfTrapOwner { get; set; }
            public bool OverrideCostsForAdmins { get; set; }
        }

        private class ConfigData
        {
            public Autotraps AutotrapSettings { get; set; }
            public Dictionary<Traps, TrapEntry> TrapTypes { get; set; }
            public Options Options { get; set; }
            public Oxide.Core.VersionNumber Version { get; set; }
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
                AutotrapSettings = new Autotraps
                {
                    AirdropChance = 40,
                    LootContainerChance = 40,
                    UseAirdrops = true,
                    UseLootContainers = true
                },
                Options = new Options
                {
                    NotifyRandomSetTraps = true,
                    NotifyPlayersWhenTrapTriggered = true,
                    PlayTrapWarningSoundFX = true,
                    CanTrapBoxes = true,
                    CanTrapDoors = true,
                    CanTrapLoot = false,
                    CanTrapSupplyDrops = false,
                    OverrideCostsForAdmins = true,
                    RequireBuildingPrivToTrap = true,
                    RequireOwnershipToTrap = true,
                    IgnoreTriggerForTrapOwner = false,
                    IgnoreTriggerForFriendsOfTrapOwner = false,
                },
                TrapTypes = new Dictionary<Traps, TrapEntry>
                {
                    {Traps.BeancanGrenade, new TrapEntry
                    {
                        AdminOnly = false,
                        Costs = new List<TrapCostEntry>
                        {
                            new TrapCostEntry
                            {
                                Shortname = "grenade.beancan",
                                Amount = 2
                            }
                        },
                        DamageAmount = 30,
                        Radius = 4,
                        FuseTimer = 2,
                        Enabled = true
                    }
                    },
                    {Traps.Beartrap, new TrapEntry
                    {
                        AdminOnly = false,
                        Costs = new List<TrapCostEntry>
                        {
                            new TrapCostEntry
                            {
                                Shortname = "trap.bear",
                                Amount = 10
                            }
                        },
                        DamageAmount = 0,
                        Radius = 2,
                        FuseTimer = 2,
                        Enabled = true
                    }
                    },
                    {Traps.Explosive, new TrapEntry
                    {
                        AdminOnly = false,
                        Costs = new List<TrapCostEntry>
                        {
                            new TrapCostEntry
                            {
                                Shortname = "explosive.timed",
                                Amount = 2
                            }
                        },
                        DamageAmount = 110,
                        Radius = 10,
                        FuseTimer = 3,
                        Enabled = true
                    }
                    },
                    {Traps.Fire, new TrapEntry
                    {
                        AdminOnly = false,
                        Costs = new List<TrapCostEntry>
                        {
                            new TrapCostEntry
                            {
                                Shortname = "lowgradefuel",
                                Amount = 50
                            }
                        },
                        DamageAmount = 1,
                        Radius = 2,
                        FuseTimer = 3,
                        Enabled = true
                    }
                    },
                    {Traps.Grenade, new TrapEntry
                    {
                        AdminOnly = false,
                        Costs = new List<TrapCostEntry>
                        {
                            new TrapCostEntry
                            {
                                Shortname = "grenade.f1",
                                Amount = 2
                            }
                        },
                        DamageAmount = 75,
                        Radius = 5,
                        FuseTimer = 3,
                        Enabled = true
                    }
                    },
                    {Traps.Landmine, new TrapEntry
                    {
                        AdminOnly = false,
                        Costs = new List<TrapCostEntry>
                        {
                            new TrapCostEntry
                            {
                                Shortname = "trap.landmine",
                                Amount = 10
                            }
                        },
                        DamageAmount = 0,
                        Radius = 2,
                        FuseTimer = 2,
                        Enabled = true
                    }
                    },
                    {Traps.Radiation, new TrapEntry
                    {
                        AdminOnly = true,
                        Costs = new List<TrapCostEntry>(),
                        DamageAmount = 20,
                        Radius = 10,
                        FuseTimer = 3,
                        Duration = 20,
                        Enabled = true
                    }
                    },
                    {Traps.Shock, new TrapEntry
                    {
                        AdminOnly = true,
                        Costs = new List<TrapCostEntry>(),
                        DamageAmount = 95,
                        Radius = 2,
                        FuseTimer = 2,
                        Enabled = true
                    }
                    }
                },
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }
        #endregion

        #region Data Management
        enum Traps
        {
            BeancanGrenade,
            Grenade,
            Explosive,
            Landmine,
            Beartrap,
            Radiation,
            Fire,
            Shock
        }

        private class TrapInfo
        {            
            public Traps trapType;
            public Vector3 location;
            public string trapOwner;
            public bool saveTrap; 

            public TrapInfo() { }

            public TrapInfo(Traps trapType, Vector3 location, string trapOwner, bool saveTrap)
            {
                this.trapType = trapType;
                this.location = location;
                this.trapOwner = trapOwner;
                this.saveTrap = saveTrap;
            }
        }

        private void SaveData()
        {
            storedData.trapData = currentTraps.Where(x => x.Value.saveTrap == true).ToDictionary(y => y.Key, y => y.Value);
            data.WriteObject(storedData);
        }

        private void LoadData()
        {
            try
            {
                storedData = data.ReadObject<StoredData>();
                currentTraps = storedData.trapData;
            }
            catch
            {
                storedData = new StoredData();
            }
        }

        private class StoredData
        {
            public Dictionary<uint, TrapInfo> trapData = new Dictionary<uint, TrapInfo>();
        }

        private class UnityVector3Converter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                Vector3 vector = (Vector3)value;
                writer.WriteValue($"{vector.x} {vector.y} {vector.z}");
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.String)
                {
                    string[] values = reader.Value.ToString().Trim().Split(' ');
                    return new Vector3(Convert.ToSingle(values[0]), Convert.ToSingle(values[1]), Convert.ToSingle(values[2]));
                }
                JObject o = JObject.Load(reader);
                return new Vector3(Convert.ToSingle(o["x"]), Convert.ToSingle(o["y"]), Convert.ToSingle(o["z"]));
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Vector3);
            }
        }
        #endregion

        #region Messaging
        private string msg(string key, string playerid = null) => lang.GetMessage(key, this, playerid);

        private Dictionary<string, string> Messages = new Dictionary<string, string>()
        {
            {"insufficientResources", "<color=#939393>You have insufficient resources to purchase this trap!</color>" },
            {"triggered","<color=#939393>You just triggered a </color><color=#00CC00>{0}</color> <color=#939393>trap!</color>" },
            {"invalidEntity", "<color=#939393>You are not looking at a valid trap-able entity!</color>"},
            {"noPrivilege","<color=#939393>You must have building privilege to place/remove a trap!</color>" },
            {"notOwner", "<color=#939393>You must own the entity you wish to place/remove a trap on!</color>"},
            {"hasTrap", "<color=#939393>This entity already has a trap placed on it!</color>"},
            {"help1", "<color=#00CC00>-- {0}  v{1} --</color>\n<color=#939393>With this plugin you can set traps on a variety of objects.\nDoors : {2}\nStorage Containers : {3}\nLoot Containers : {4}\nSupply Drops : {5}</color>"},
            {"help2", "<color=#00CC00>/trap cost <traptype></color><color=#939393> - Displays the cost to place this trap</color>\n<color=#00CC00>/trap set <traptype></color><color=#939393> - Sets a trap on the object you are looking at</color><color=#00CC00>\n/trap remove</color><color=#939393> - Removes a trap set by yourself on the object you are looking at</color><color=#00CC00>\n/trap check</color><color=#939393> - Check the object your are looking at for traps set by yourself</color>"},
            {"help3", "<color=#00CC00>-- Available Types --</color><color=#939393>\nBeancan, Grenade, Explosive, Landmine, Beartrap, Radiation, Fire, Shock</color>"},
            {"help4", "<color=#00CC00>/trap removeall</color><color=#939393>> - Removes all active traps on the map</color><color=#00CC00>\n/trap list</color><color=#939393> - Lists all traps in console</color>"},
            {"help5", "<color=#00CC00>-- Available Types -- </color>\n"},
            {"invalidTrap", "<color=#939393>Invalid trap type selected</color>"},
            {"noTrap", "<color=#939393>The object you are looking at does not have a trap on it!</color>"},
            {"removeSuccess", "<color=#939393>You have successfully removed the trap from this object!</color>"},
            {"trapInfo", "<color=#939393>This object is trapped with a </color><color=#00CC00>{0}</color><color=#939393> trap!</color>"},
            {"noPerm", "<color=#939393>You do not have permission to use this command!</color>"},
            {"removedAll", "<color=#939393>You have successfully removed all traps!</color>"},
            {"currentTraps", "-- There are currently {0} active traps --\n[Entity ID] - [Trap Type] - [Location]"},
            {"conHelp", "trap removeall - Removes all active traps on the map\ntrap list - Lists all traps"},
            {"trapSet", "<color=#939393>You have successfully set a </color><color=#00CC00>{0} </color><color=#939393>trap on this object!</color>" },
            {"getCosts", "<color=#939393>Costs to set a </color><color=#00CC00>{0}</color> <color=#939393>trap:</color>" },
            {"notEnabled", "<color=#939393>This trap is not enabled!</color>" }
        };
        #endregion
    }
}
