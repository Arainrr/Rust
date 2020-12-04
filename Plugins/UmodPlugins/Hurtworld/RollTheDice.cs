﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Oxide.Core;
using System.Linq;

namespace Oxide.Plugins 
{
    // Original author: clang (Most of code from Kits plugin by Reheb)
    [Info("RollTheDice", "klauz24", "1.0.7")]
    [Description("Randomized item giving with possible negative outcomes")]
    class RollTheDice : HurtworldPlugin
    {
        string Msg(string msg, string SteamId) => lang.GetMessage(msg, this, SteamId);
        void ShowMsg(PlayerSession session, string msg) => Player.Message(session, msg);
        void BroadcastMsg(string msg) => Server.Broadcast(msg);
        private string GetSteamId(PlayerSession session) => session.SteamId.ToString();

        private ItemGeneratorAsset GetItemGeneratorAsset(string guid)
        {
            var generator = RuntimeHurtDB.Instance.GetObjectByGuid<ItemGeneratorAsset>(guid);
            return generator == null ? null : generator;
        }

        private NetworkInstantiateConfig FindNIC(string name)
        {
            NetworkInstantiateConfig[] allNICs = Resources.FindObjectsOfTypeAll<NetworkInstantiateConfig>();
            for (int i = 0; i < allNICs.Length; i++)
            {
                NetworkInstantiateConfig NIC = allNICs[i];
                if (NIC.name == name)
                {
                    return NIC;
                }
            }
            return null;
        }

        private void SpawnObject(Vector3 pos, string name) => HNetworkManager.Instance.NetInstantiate(FindNIC(name), RandomPosition(pos), Quaternion.identity, GameManager.GetSceneTime());

        private float RP(float min, float max) => UnityEngine.Random.Range(min, max);

        private Vector3 RandomPosition(Vector3 pos)
        {
            float x = pos.x,
                y = pos.y,
                z = pos.z;
            Vector3 newPos = new Vector3(RP(-10, 10) + x, RP(0, 10) + y, RP(-10, 10) + z);
            return newPos;
        }

        void Init()
        {
            LoadData();
            LoadPerms();
            try
            {
                rollsData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Dictionary<string, RollData>>>("RollTheDice_data");
            }
            catch
            {
                rollsData = new Dictionary<ulong, Dictionary<string, RollData>>();
            }
        }

        void LoadPerms()
        {
            foreach (var roll in storedData.Rolls.Values)
            {
                if (string.IsNullOrEmpty(roll.permission)) continue;
                permission.RegisterPermission(roll.permission, this);
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        ///// Configuration
        //////////////////////////////////////////////////////////////////////////////////////////

        void OnPlayerRespawn(PlayerSession session)
        {
            if (!storedData.Rolls.ContainsKey("autoroll")) return;
            var thereturn = Interface.Oxide.CallHook("CanRedeemRoll", session);
            if (thereturn == null)
            {
                var playerinv = session.WorldPlayerEntity.GetComponent<PlayerInventory>();
                for (var j = 0; j < playerinv.Capacity; j++)
                {
                    ItemObject item = playerinv.GetSlot(j);
                    if (item == null) continue;
                    Singleton<ClassInstancePool>.Instance.ReleaseInstanceExplicit(item);
                    playerinv.SetSlot(j, null);
                }
                GiveRoll(session, "autoroll");
            }
        }


        //////////////////////////////////////////////////////////////////////////////////////////
        ///// Roll Redeemer
        //////////////////////////////////////////////////////////////////////////////////////////

        private void TryGiveRoll(PlayerSession session, string rollname)
        {
            var success = CanRedeemARoll(session, rollname);
            if (!success) return;

            success = GiveRoll(session, rollname);
            if (!success) return;

            ShowMsg(session, Msg("TryGiveRoll_Completed", GetSteamId(session)));

            ProccessRollGiven(session, rollname);
        }

        void ProccessRollGiven(PlayerSession session, string rollname)
        {
            Roll roll;
            if (string.IsNullOrEmpty(rollname) || !storedData.Rolls.TryGetValue(rollname, out roll)) return;

            var rollData = GetRollData(session.SteamId.m_SteamID, rollname);
            if (roll.max > 0) rollData.max += 1;

            if (roll.cooldown > 0) rollData.cooldown = CurrentTime() + roll.cooldown;
        }

        bool GiveRoll(PlayerSession session, string rollname)
        {
            Roll roll;
            if (string.IsNullOrEmpty(rollname) || !storedData.Rolls.TryGetValue(rollname, out roll))
            {
                ShowMsg(session, Msg("GiveRoll_NotExist", GetSteamId(session)).Replace("{rollname}", rollname));
                return false;
            }

            var playerinv = session.WorldPlayerEntity.GetComponent<PlayerInventory>();
            var amanager = Singleton<AlertManager>.Instance;
            var itemmanager = Singleton<GlobalItemManager>.Instance;
            var playerEntity = session.WorldPlayerEntity;
            var rolledItem = new RollItem();
            var rolledReplacement = new RollItem();
            var rolledSpawn = new RollSpawn();
            var ManagerInstance = GameManager.Instance;
            var sumItemChance = 0;
            var sumSpawnChance = 0;
            var sumReplaceChance = 0;
            var totalChance = 0;
            var count = 0;

            foreach (var rollem in roll.items)
                sumItemChance += rollem.chance;


            foreach (var rollspawn in roll.spawns)
                sumSpawnChance += rollspawn.chance;

            foreach (var rollreplace in roll.replacements)
                sumReplaceChance += rollreplace.chance;

            totalChance = sumItemChance + sumSpawnChance + sumReplaceChance;

            if (totalChance == 0) return false;

            var rnd = Core.Random.Range(1, totalChance);

            if (rnd <= sumItemChance)
            {
                for (int i = 0; i < roll.items.Count && rolledItem.GUID == (null); i++)
                {
                    count += roll.items[i].chance;

                    if (count >= rnd)
                        rolledItem = roll.items[i];
                }
                var IGA = GetItemGeneratorAsset(rolledItem.GUID);
                if (IGA == null)
                {
                    Puts("Error, guid {0} doesn't exist.", rolledItem.GUID);
                    return false;
                }
                ItemObject item = itemmanager.CreateItem(IGA, rolledItem.amount);
                if (playerinv.GetSlot(0) == null)
                {
                    playerinv.SetSlot(0, item);
                    amanager.ItemReceivedServer(item, item.StackSize, session.Player);
                    playerinv.Invalidate(false);
                }
                else
                {
                    itemmanager.GiveItem(session.Player, IGA, rolledItem.amount);
                }
                BroadcastMsg(Msg("GiveRoll_ItemGive", GetSteamId(session))
                    .Replace("{player}", session.Identity.Name)
                    .Replace("{amount}", rolledItem.amount.ToString())
                    .Replace("{itemname}", item.Generator.ToString().Split('/').Last()));
            }
            else if (rnd > sumItemChance && rnd <= sumItemChance + sumSpawnChance)
            {
                count = sumItemChance;
                GameObject Obj = new GameObject();

                for (int i = 0; i < roll.spawns.Count && rolledSpawn.spawnName == null; i++)
                {
                    count += roll.spawns[i].chance;

                    if (count >= rnd)
                        rolledSpawn = roll.spawns[i];
                }

                var iterations = 0;
                var numberSpawned = 0;

                for (numberSpawned = 0; numberSpawned < rolledSpawn.amount && iterations < rolledSpawn.amount * 100;)
                {
                    Vector3 pos = session.WorldPlayerEntity.transform.position;
                    SpawnObject(pos, rolledSpawn.spawnName);
                    numberSpawned++;
                    iterations++;
                }

                BroadcastMsg(Msg("GiveRoll_SpawnGive", GetSteamId(session))
                    .Replace("{player}", session.Identity.Name)
                    .Replace("{amount}", numberSpawned.ToString())
                    .Replace("{creature}", creatures[rolledSpawn.spawnName.ToLower()]));
            }
            else
            {
                var replacementFound = false;
                count = sumItemChance + sumSpawnChance;

                for (int i = 0; i < roll.replacements.Count && rolledReplacement.GUID == (null); i++)
                {
                    count += roll.replacements[i].chance;

                    if (count >= rnd)
                        rolledReplacement = roll.replacements[i];
                }

                int invSlots = playerinv.StorageConfig.Slots.Length;
                if (invSlots > 0)
                {
                    var noOfIterations = 0;
                    for (int i = Core.Random.Range(0, invSlots - 1); !replacementFound && noOfIterations < 1000; i = Core.Random.Range(0, invSlots - 1))
                    {
                        if (playerinv.GetSlot(i) != null)
                        {
                            var IGA = GetItemGeneratorAsset(rolledItem.GUID);
                            if (IGA == null)
                            {
                                Puts("Error, guid {0} doesn't exist.", rolledItem.GUID);
                                return false;
                            }
                            ItemObject item = itemmanager.CreateItem(IGA, rolledReplacement.amount);

                            var replaceedItem = playerinv.GetSlot(i);

                            playerinv.SetSlot(i, item);
                            amanager.ItemReceivedServer(item, item.StackSize, session.Player);
                            playerinv.Invalidate(false);
                            replacementFound = true;

                            BroadcastMsg(Msg("GiveRoll_ItemReplace", GetSteamId(session))
                                .Replace("{player}", session.Identity.Name)
                                .Replace("{amount1}", replaceedItem.StackSize.ToString())
                                .Replace("{itemname1}", replaceedItem.Generator.GetNameKey().ToString().Split('/').Last())
                                .Replace("{amount2}", playerinv.GetSlot(i).StackSize.ToString())
                                .Replace("{itemname2}", playerinv.GetSlot(i).GetNameKey().ToString().Split('/').Last()));
                        }

                        noOfIterations++;
                    }
                }
            }
            return true;
        }
        //////////////////////////////////////////////////////////////////////////////////////////
        ///// Check Rolls
        //////////////////////////////////////////////////////////////////////////////////////////

        bool isRoll(string rollname) => !string.IsNullOrEmpty(rollname) && storedData.Rolls.ContainsKey(rollname);

        static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        static double CurrentTime() { return DateTime.UtcNow.Subtract(epoch).TotalSeconds; }

        bool CanSeeRoll(PlayerSession session, string rollname, out string reason)
        {
            reason = string.Empty;
            Roll roll;
            if (string.IsNullOrEmpty(rollname) || !storedData.Rolls.TryGetValue(rollname, out roll)) return false;
            if (roll.hide) return false;
            if (roll.authlevel > 0)
                if (!session.IsAdmin) return false;
            if (!string.IsNullOrEmpty(roll.permission))
                if (!permission.UserHasPermission(session.SteamId.ToString(), roll.permission)) return false;
            if (roll.max > 0)
            {
                var left = GetRollData(session.SteamId.m_SteamID, rollname).max;
                if (left >= roll.max)
                {
                    reason += "- 0 left";
                    return false;
                }
                reason += $"- {(roll.max - left)} left";
            }
            if (roll.cooldown > 0)
            {
                var cd = GetRollData(session.SteamId.m_SteamID, rollname).cooldown;
                var ct = CurrentTime();
                if (cd > ct && cd != 0.0)
                {
                    reason += $"- {Math.Abs(Math.Ceiling(cd - ct))} seconds";
                    return false;
                }
            }
            return true;
        }

        bool CanRedeemARoll(PlayerSession session, string rollname)
        {
            Roll roll;
            if (string.IsNullOrEmpty(rollname) || !storedData.Rolls.TryGetValue(rollname, out roll))
            {
                ShowMsg(session, Msg("CanRedeemARoll_NotExist", GetSteamId(session)).Replace("{rollname}", rollname));
                return false;
            }

            var thereturn = Interface.Oxide.CallHook("CanRedeemRoll", session);
            if (thereturn != null)
            {
                ShowMsg(session, Msg("CanRedeemARoll_NotAllowed", GetSteamId(session)));
                return false;
            }

            if (roll.authlevel > 0)
                if (!session.IsAdmin)
                {
                    ShowMsg(session, Msg("CanRedeemARoll_NoLevel", GetSteamId(session)));
                    return false;
                }

            if (!string.IsNullOrEmpty(roll.permission))
                if (!permission.UserHasPermission(session.SteamId.ToString(), roll.permission))
                {
                    ShowMsg(session, Msg("CanRedeemARoll_NoPerm", GetSteamId(session)));
                    return false;
                }

            var rollData = GetRollData(session.SteamId.m_SteamID, rollname);
            if (roll.max > 0)
                if (rollData.max >= roll.max)
                {
                    ShowMsg(session, Msg("CanRedeemARoll_Out", GetSteamId(session)));
                    return false;
                }

            if (roll.cooldown > 0)
            {
                var ct = CurrentTime();
                if (rollData.cooldown > ct && rollData.cooldown != 0.0)
                {
                    ShowMsg(session, Msg("CanRedeemARoll_Wait", GetSteamId(session))
                        .Replace("{seconds}", Math.Abs(Math.Ceiling(rollData.cooldown - ct)).ToString()));
                    return false;
                }
            }
            return true;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Roll Class
        //////////////////////////////////////////////////////////////////////////////////////

        class RollItem : RollSelection
        {
            public string GUID;
        }

        class RollSpawn : RollSelection
        {
            public string spawnName;
        }

        class RollSelection
        {
            public int amount;
            public int chance;
        }

        class Roll
        {
            public string name;
            public string description;
            public int max;
            public double cooldown;
            public int authlevel;
            public bool hide;
            public string permission;
            public List<RollItem> items = new List<RollItem>();
            public List<RollSpawn> spawns = new List<RollSpawn>();
            public List<RollItem> replacements = new List<RollItem>();
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Data Manager
        //////////////////////////////////////////////////////////////////////////////////////

        void SaveRollsData() => Interface.Oxide.DataFileSystem.WriteObject("RollTheDice_data", rollsData);

        StoredData storedData;
        Dictionary<ulong, Dictionary<string, RollData>> rollsData;

        Dictionary<string, string> creatures = new Dictionary<string, string>
            {
                {"aiborserver","bor"},
                {"aiyetiserver","yeti"},
                {"aitokarserver","tokar"}
            };

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"GiveRoll_ItemGive","<color=green>[ROLL]</color> {player} has rolled the dice and recieved {amount} {itemname}"},
                {"GiveRoll_SpawnGive","<color=green>[ROLL]</color> {player} has rolled the dice and spawned {amount} {creature}"},
                {"GiveRoll_ItemReplace","<color=green>[ROLL]</color> {player} has rolled the dice and replaced {amount1} {itemname1} with {amount2} {itemname2}"},
                {"SendListRollEdition_1","permission \"permission name\" => set the permission needed to get this roll"},
                {"SendListRollEdition_2","description \"description text here\" => set a description for this roll"},
                {"SendListRollEdition_3","authlevel XXX"},
                {"SendListRollEdition_4","cooldown XXX"},
                {"SendListRollEdition_5","max XXX"},
                {"SendListRollEdition_6","additem <itemId> <amount> <chance> => set new items for your roll"},
                {"SendListRollEdition_8","addspawn <creature> <amount> <chance> => spawn creatures on player"},
                {"SendListRollEdition_9","addreplace <itemId> <amount> <chance> => replace random inventory item"},
                {"SendListRollEdition_10","hide TRUE/FALSE => dont show this roll in lists (EVER)"},
                {"CmdRoll_CantSeeRoll", "{rollname} - {desc} {reason}"},
                {"CmdRoll_Player1","====== Player Commands ======"},
                {"CmdRoll_Player2","/roll => to get the list of rolls"},
                {"CmdRoll_Player3","/roll ROLLNAME => to redeem the roll"},
                {"CmdRoll_Admin1", "====== Admin Commands ======"},
                {"CmdRoll_Admin2","/roll add ROLLNAME => add a roll"},
                {"CmdRoll_Admin3","/roll remove ROLLNAME => remove a roll"},
                {"CmdRoll_Admin4","/roll edit ROLLNAME => edit a roll"},
                {"CmdRoll_Admin5","/roll list => get a raw list of rolls (the real full list)"},
                {"CmdRoll_Admin6","/roll give PLAYER/STEAMID ROLLNAME => make a player roll the dice"},
                {"CmdRoll_Admin7","/roll resetrolls => deletes all rolls"},
                {"CmdRoll_Admin8","/roll resetdata => reset player data"},
                {"CmdRoll_NoAccess","You don't have access to this command"},
                {"CmdRoll_RollList","{rollname} - {description}"},
                {"CmdRoll_ResetAll","All player and roll data has been reset"},
                {"CmdRoll_ResetPlayer", "All player data has been reset"},
                {"CmdRoll_NewRollExists", "This roll already exists"},
                {"CmdRoll_NewRollNotExists", "This roll doesn't seem to exist"},
                {"CmdRoll_NewRoll", "You've created a new roll: {rollname}"},
                {"CmdRoll_Give","/roll give PLAYER/STEAMID ROLLNAME"},
                {"CmdRoll_GiveNoPlayer", "No player found"},
                {"CmdRoll_GiveMultiple", "Multiple players found"},
                {"CmdRoll_Given","You gave {player} the roll: {rollname}"},
                {"CmdRoll_GivenReceive", "You've received the roll {rollname} from {player} enjoy!"},
                {"CmdRoll_Edit", "You are now editing roll: {rollname}"},
                {"CmdRoll_Remove","{rollname} was removed"},
                {"CmdRoll_NotInEdit", "You are not creating or editing a roll"},
                {"CmdRoll_Dirty", "There was an error while getting this roll, was it changed while you were editing it?"},
                {"CmdRoll_AddItem", "Item added successfully"},
                {"CmdRoll_AddStat", "Stat added successfully"},
                {"CmdRoll_AddSpawn", "Spawn added successfully"},
                {"CmdRoll_AddReplace", "Item replacement added successfully"},
                {"CmdRoll_InvalidArgs", "{arg} Invalid agrument"},
                {"GiveRoll_NotExist", "The roll '{rollname}' doesn't exist"},
                {"TryGiveRoll_Completed", "Roll Completed" },
                {"CanRedeemARoll_NotExist", "The roll '{rollname}' doesn't exist"},
                {"CanRedeemARoll_NotAllowed", "You are not allowed to roll the dice at this moment"},
                {"CanRedeemARoll_NoLevel", "You don't have the level to use this roll"},
                {"CanRedeemARoll_NoPerm", "You don't have the permissions to use this roll"},
                {"CanRedeemARoll_Out", "You already took all your chances with this roll"},
                {"CanRedeemARoll_Wait", "You need to wait {seconds} seconds to use this roll"},
                {"Rolls_ListOfRolls", "List of rolls:"}
            }, this);
        }

        class StoredData
        {
            public Dictionary<string, Roll> Rolls = new Dictionary<string, Roll>();
        }
        class RollData
        {
            public int max;
            public double cooldown;
        }
        void ResetData()
        {
            rollsData.Clear();
            SaveRollsData();
        }

        void Unload() => SaveRollsData();
        void OnServerSave() => SaveRollsData();

        void SaveRolls() => Interface.Oxide.DataFileSystem.WriteObject("Rolls", storedData);

        void LoadData()
        {
            var rolls = Interface.Oxide.DataFileSystem.GetFile("Rolls");
            try
            {
                rolls.Settings.NullValueHandling = NullValueHandling.Ignore;
                storedData = rolls.ReadObject<StoredData>();
            }
            catch
            {
                storedData = new StoredData();
            }
            rolls.Settings.NullValueHandling = NullValueHandling.Include;
        }

        RollData GetRollData(ulong userID, string rollname)
        {
            Dictionary<string, RollData> rollDatas;
            if (!rollsData.TryGetValue(userID, out rollDatas)) rollsData[userID] = rollDatas = new Dictionary<string, RollData>();
            RollData rollData;
            if (!rollDatas.TryGetValue(rollname, out rollData)) rollDatas[rollname] = rollData = new RollData();
            return rollData;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Roll Editor
        //////////////////////////////////////////////////////////////////////////////////////

        readonly Dictionary<ulong, string> rollEditor = new Dictionary<ulong, string>();

        //////////////////////////////////////////////////////////////////////////////////////
        // Console Command
        //////////////////////////////////////////////////////////////////////////////////////

        List<PlayerSession> FindPlayer(string arg)
        {
            var listPlayers = new List<PlayerSession>();

            ulong steamid;
            ulong.TryParse(arg, out steamid);
            var lowerarg = arg.ToLower();

            foreach (var pair in GameManager.Instance.GetSessions())
            {
                var session = pair.Value;
                if (!session.IsLoaded) continue;
                if (steamid != 0L)
                    if (session.SteamId.m_SteamID == steamid)
                    {
                        listPlayers.Clear();
                        listPlayers.Add(session);
                        return listPlayers;
                    }
                var lowername = session.Identity.Name.ToLower();
                if (lowername.Contains(lowerarg)) listPlayers.Add(session);
            }
            return listPlayers;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Chat Command
        //////////////////////////////////////////////////////////////////////////////////////

        bool HasAccess(PlayerSession session) => session.IsAdmin;

        void SendListRollEdition(PlayerSession session)
        {
            ShowMsg(session, Msg("SendListRollEdition_1", GetSteamId(session)));
            ShowMsg(session, Msg("SendListRollEdition_2", GetSteamId(session)));
            ShowMsg(session, Msg("SendListRollEdition_3", GetSteamId(session)));
            ShowMsg(session, Msg("SendListRollEdition_4", GetSteamId(session)));
            ShowMsg(session, Msg("SendListRollEdition_5", GetSteamId(session)));
            ShowMsg(session, Msg("SendListRollEdition_6", GetSteamId(session)));
            ShowMsg(session, Msg("SendListRollEdition_7", GetSteamId(session)));
            ShowMsg(session, Msg("SendListRollEdition_8", GetSteamId(session)));
            ShowMsg(session, Msg("SendListRollEdition_9", GetSteamId(session)));
            ShowMsg(session, Msg("SendListRollEdition_10", GetSteamId(session)));
        }

        [ChatCommand("roll")]
        void cmdRoll(PlayerSession session, string command, string[] args)
        {
            if (args.Length == 0)
            {
                ShowMsg(session, Msg("Rolls_ListOfRolls", GetSteamId(session)));
                var reason = string.Empty;
                foreach (var pair in storedData.Rolls)
                {
                    var cansee = CanSeeRoll(session, pair.Key, out reason);
                    if (!cansee && string.IsNullOrEmpty(reason)) continue;
                    ShowMsg(session, Msg("CmdRoll_CantSeeRoll", GetSteamId(session))
                        .Replace("{rollname}", pair.Value.name)
                        .Replace("{desc}", pair.Value.description)
                        .Replace("{reason}", reason));
                }
                return;
            }
            if (args.Length == 1)
            {
                switch (args[0].ToLower())
                {
                    case "help":
                        ShowMsg(session, Msg("CmdRoll_Player1", GetSteamId(session)));
                        ShowMsg(session, Msg("CmdRoll_Player2", GetSteamId(session)));
                        ShowMsg(session, Msg("CmdRoll_Player3", GetSteamId(session)));
                        if (!HasAccess(session)) return;
                        ShowMsg(session, Msg("CmdRoll_Admin1", GetSteamId(session)));
                        ShowMsg(session, Msg("CmdRoll_Admin2", GetSteamId(session)));
                        ShowMsg(session, Msg("CmdRoll_Admin3", GetSteamId(session)));
                        ShowMsg(session, Msg("CmdRoll_Admin4", GetSteamId(session)));
                        ShowMsg(session, Msg("CmdRoll_Admin5", GetSteamId(session)));
                        ShowMsg(session, Msg("CmdRoll_Admin6", GetSteamId(session)));
                        ShowMsg(session, Msg("CmdRoll_Admin7", GetSteamId(session)));
                        ShowMsg(session, Msg("CmdRoll_Admin8", GetSteamId(session)));
                        break;
                    case "add":
                    case "remove":
                    case "edit":
                        if (!HasAccess(session)) { ShowMsg(session, Msg("CmdRoll_NoAccess", GetSteamId(session))); return; }
                        ShowMsg(session, Msg("CmdRoll_Admin4", GetSteamId(session)));
                        break;
                    case "give":
                        if (!HasAccess(session)) { ShowMsg(session, Msg("CmdRoll_NoAccess", GetSteamId(session))); return; }
                        ShowMsg(session, Msg("CmdRoll_Admin6", GetSteamId(session)));
                        break;
                    case "list":

                        if (!HasAccess(session)) { ShowMsg(session, Msg("CmdRoll_NoAccess", GetSteamId(session))); return; }
                        foreach (var roll in storedData.Rolls.Values)
                            ShowMsg(session, Msg("CmdRoll_RollList", GetSteamId(session)).Replace("{rollname}", roll.name).Replace("{description}", roll.description));
                        break;
                    case "additem":
                        break;
                    case "addstat":
                        break;
                    case "addspawn":
                        break;
                    case "addreplace":
                        break;
                    case "resetrolls":
                        if (!HasAccess(session)) { ShowMsg(session, Msg("CmdRoll_NoAccess", GetSteamId(session))); return; }
                        storedData.Rolls.Clear();
                        rollEditor.Clear();
                        ResetData();
                        SaveRolls();
                        ShowMsg(session, Msg("CmdRoll_ResetAll", GetSteamId(session)));
                        break;
                    case "resetdata":
                        if (!HasAccess(session)) { ShowMsg(session, Msg("CmdRoll_NoAccess", GetSteamId(session))); return; }
                        ResetData();
                        ShowMsg(session, Msg("CmdRoll_ResetPlayer", GetSteamId(session)));
                        break;
                    default:
                        TryGiveRoll(session, args[0].ToLower());
                        break;
                }
                if (args[0].ToLower() != "additem" && args[0].ToLower() != "addstat" && args[0].ToLower() != "addspawn" && args[0].ToLower() != "addreplace") return;

            }
            if (!HasAccess(session)) { ShowMsg(session, Msg("CmdRoll_NoAccess", GetSteamId(session))); return; }

            string rollname;
            switch (args[0].ToLower())
            {
                case "add":
                    rollname = args[1].ToLower();
                    if (storedData.Rolls.ContainsKey(rollname))
                    {
                        ShowMsg(session, Msg("CmdRoll_NewRollExists", GetSteamId(session)));
                        return;
                    }
                    storedData.Rolls[rollname] = new Roll { name = args[1] };
                    rollEditor[session.SteamId.m_SteamID] = rollname;
                    ShowMsg(session, Msg("CmdRoll_NewRoll", GetSteamId(session)).Replace("{rollname}", args[1]));
                    SendListRollEdition(session);
                    break;
                case "give":
                    if (args.Length < 3)
                    {
                        ShowMsg(session, Msg("CmdRoll_Admin6", GetSteamId(session)));
                        return;
                    }
                    rollname = args[2].ToLower();
                    if (!storedData.Rolls.ContainsKey(rollname))
                    {
                        ShowMsg(session, Msg("CmdRoll_NewRollNotExists", GetSteamId(session)));
                        return;
                    }
                    var findPlayers = FindPlayer(args[1]);
                    if (findPlayers.Count == 0)
                    {
                        ShowMsg(session, Msg("CmdRoll_GiveNoPlayer", GetSteamId(session)));
                        return;
                    }
                    if (findPlayers.Count > 1)
                    {
                        ShowMsg(session, Msg("CmdRoll_GiveMultiple", GetSteamId(session)));
                        return;
                    }
                    GiveRoll(findPlayers[0], rollname);
                    ShowMsg(session, Msg("CmdRoll_Given", GetSteamId(session)).Replace("{player}", findPlayers[0].Identity.Name).Replace("{rollname}", storedData.Rolls[rollname].name));
                    ShowMsg(findPlayers[0], Msg("CmdRoll_GivenReceive", GetSteamId(session)).Replace("{player}", session.Identity.Name).Replace("{rollname}", storedData.Rolls[rollname].name));
                    break;
                case "edit":
                    rollname = args[1].ToLower();
                    if (!storedData.Rolls.ContainsKey(rollname))
                    {
                        ShowMsg(session, Msg("CmdRoll_NewRollNotExists", GetSteamId(session)));
                        return;
                    }
                    rollEditor[session.SteamId.m_SteamID] = rollname;

                    ShowMsg(session, Msg("CmdRoll_Edit", GetSteamId(session)).Replace("{rollname}", rollname));
                    SendListRollEdition(session);
                    break;
                case "remove":
                    rollname = args[1].ToLower();
                    if (!storedData.Rolls.Remove(rollname))
                    {
                        ShowMsg(session, Msg("CmdRoll_NewRollNotExists", GetSteamId(session)));
                        return;
                    }

                    ShowMsg(session, Msg("CmdRoll_Remove", GetSteamId(session)).Replace("{rollname}", rollname));
                    if (rollEditor[session.SteamId.m_SteamID] == rollname) rollEditor.Remove(session.SteamId.m_SteamID);
                    break;
                default:
                    if (!rollEditor.TryGetValue(session.SteamId.m_SteamID, out rollname))
                    {
                        ShowMsg(session, Msg("CmdRoll_NotInEdit", GetSteamId(session)));
                        return;
                    }
                    Roll roll;
                    if (!storedData.Rolls.TryGetValue(rollname, out roll))
                    {
                        ShowMsg(session, Msg("CmdRoll_Dirty", GetSteamId(session)));
                        return;
                    }
                    for (var i = 0; i < args.Length; i++)
                    {
                        object editvalue;
                        var key = args[i].ToLower();
                        switch (key)
                        {
                            case "additem":
                                RollItem item = new RollItem();
                                item.GUID = args[++i];
                                item.amount = Math.Abs(int.Parse(args[++i]));
                                item.chance = Math.Abs(int.Parse(args[++i]));

                                roll.items.Add(item);
                                ShowMsg(session, Msg("CmdRoll_AddItem", GetSteamId(session)));
                                continue;
                            case "addspawn":
                                RollSpawn spawn = new RollSpawn();

                                var spawnType = args[++i].ToLower();
                                var spawnAmount = args[++i].ToLower();
                                var spawnChance = args[++i].ToLower();
                                bool spawnFound = true;

                                switch (spawnType)
                                {
                                    case "bor":
                                        spawn.spawnName = "AIBorServer";
                                        break;
                                    case "yeti":
                                        spawn.spawnName = "AIYetiServer";
                                        break;
                                    case "tokar":
                                        spawn.spawnName = "AITokarServer";
                                        break;
                                    default:
                                        spawnFound = false;
                                        break;
                                }

                                if (spawnFound)
                                {
                                    spawn.amount = Math.Abs(int.Parse(spawnAmount));
                                    spawn.chance = Math.Abs(int.Parse(spawnChance));
                                    roll.spawns.Add(spawn);

                                    ShowMsg(session, Msg("CmdRoll_AddSpawn", GetSteamId(session)));
                                }
                                continue;
                            case "addreplace":
                                RollItem itemReplace = new RollItem();
                                itemReplace.GUID = args[++i];
                                itemReplace.amount = Math.Abs(int.Parse(args[++i]));
                                itemReplace.chance = Math.Abs(int.Parse(args[++i]));

                                roll.replacements.Add(itemReplace);
                                ShowMsg(session, Msg("CmdRoll_AddReplace", GetSteamId(session)));
                                continue;
                            case "name":
                                continue;
                            case "description":
                                editvalue = roll.description = args[++i];
                                break;
                            case "max":
                                editvalue = roll.max = int.Parse(args[++i]);
                                break;
                            case "cooldown":
                                editvalue = roll.cooldown = double.Parse(args[++i]);
                                break;
                            case "authlevel":
                                editvalue = roll.authlevel = int.Parse(args[++i]);
                                break;
                            case "hide":
                                editvalue = roll.hide = bool.Parse(args[++i]);
                                break;
                            case "permission":
                                editvalue = roll.permission = args[++i];
                                LoadPerms();
                                break;
                            default:
                                ShowMsg(session, Msg("CmdRoll_InvalidArgs", GetSteamId(session)).Replace("{arg}", args[i]));
                                continue;
                        }
                    }
                    break;
            }
            SaveRolls();
        }
    }
}