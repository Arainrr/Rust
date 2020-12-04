﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Vehicle Manager", "Mr. Blue", "2.0.2")]
    [Description("Vehicle customization! You can now install/remove/switch vehicle attachments.")]
    public class VehicleManager : HurtworldPlugin
    {
        #region Variables
        private bool showDistanceCommand = true;

        private static readonly string UsePermission = "vehiclemanager.use";
        private static readonly string InstallPermission = "vehiclemanager.install";
        private static readonly string RemovePermission = "vehiclemanager.remove";
        private static readonly string RemoveExtraPermission = "vehiclemanager.remove.extra";
        #endregion

        #region Loading
        private void Init()
        {
            showDistanceCommand = Config.Get<bool>("showDistanceCommand");

            permission.RegisterPermission(UsePermission, this);
            permission.RegisterPermission(InstallPermission, this);
            permission.RegisterPermission(RemovePermission, this);
            permission.RegisterPermission(RemoveExtraPermission, this);
        }

        protected override void LoadDefaultConfig()
        {
            Config.Set("showDistanceCommand", true);
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "Help_AvailableCommands", "Available vehicle commands:"},
                { "Help_CarDistance", "<color=orange>/car</color>: Show distance between you and your claimed vehicles."},
                { "Help_Install", "<color=orange>/car install <1-8></color>: Install new attachment from quick slot 1-8."},
                { "Help_Remove", "<color=orange>/car remove <attach></color>: Remove bumper|front|left|right|roof|rear."},
                { "Help_RemoveExtra", "<color=orange>/car remove <attach></color>: Remove gearbox|engine|tire|rotors."},
                { "NoPermission", "You don't have the permission to use this command. Required: {permission}" },
                { "VehicleDistanceDisabled", "This command is disabled." },
                { "VehicleDistance", "You have a {vehicle} {distance} away from you." },
                { "HelpCommand", "Type /car help for proper commands usage."},
                { "NoAttachmentFound", "There is no vehicle attachment in slot {slot}." },
                { "RemoveAttachmentError", "The vehicle doesn't have a {attachment} to remove." },
                { "RemoveSeatError", "The {seat} is occupied. Remove failed." },
                { "NoVehicleClaimed", "You don't have a claimed vehicle." },
                { "NotInsideVehicle", "You are not inside a vehicle." },
                { "NotVehicleOwner", "You are not the owner of this vehicle." },
                { "NotVehicleAttachment", "You are not installing a vehicle attachment." },
                { "VehicleAttachmentAlreadyInstalled", "{attachment} already installed on this vehicle!" },
                { "VehicleInstall", "You have installed {attachInstalled}."},
                { "VehicleSwitch", "You have switched {attachSwitched} to {attachInstalled}." },
                { "VehicleRemove", "You have removed {attachRemoved}." },
                { "VehicleRemoveAll", "You have removed everything from {vehicleType}." },
                { "vehicles/barra", "Barra" },
                { "vehicles/roach", "Roach" },
                { "vehicles/goat", "Goat" },
                { "vehicles/kanga", "Kanga" },
                { "vehicles/slug", "Slug" },
                { "vehicles/mozzy", "Mozzy" }
            }, this);
        }
        private string GetMsg(string key, string steamId = null) => lang.GetMessage(key, this, steamId);
        #endregion

        #region Chat Commands
        private void DoInstall(PlayerSession session, string[] args, string vehicleType, VehicleInventory vehicleInventory)
        {
            string steamId = session.SteamId.ToString();
            if (!permission.UserHasPermission(steamId, InstallPermission))
            {
                Player.Message(session, GetMsg("NoPermission", steamId).Replace("{permission}", InstallPermission));
                return;
            }

            //Check if player is installing a correct vehicle attachment
            int slot;
            if (!int.TryParse(args[1], out slot) || slot < 1 || slot > 8)
            {
                Player.Message(session, GetMsg("HelpCommand", steamId));
                return;
            }

            PlayerInventory playerInventory = session.WorldPlayerEntity.GetComponent<PlayerInventory>();
            ItemObject playerItemObject = playerInventory.GetSlot(slot - 1);
            if (playerItemObject != null)
            {
                ESlotType itemSlotType = GetSlotType(playerItemObject);

                string slotTypeString = itemSlotType.ToString().ToLower();
                Puts(slotTypeString);
                if (!slotTypeString.Contains(vehicleType))
                {
                    Player.Message(session, GetMsg("NotVehicleAttachment", steamId));
                    return;
                }

                //Correct item. Can install/switch.
                //Check if vehicle has the same attachment type installed. If not, install player's item. If yes, switch with player's item.
                bool updated = false;
                bool notSwitched = false;
                for (int slotNumber = 0; slotNumber < vehicleInventory.StorageConfig.Slots.Length; ++slotNumber)
                {
                    StorageSlotConfig storageSlotConfig = vehicleInventory.StorageConfig.Slots[slotNumber];
                    if (storageSlotConfig.SlotRestrictions.Contains(itemSlotType))
                    {
                        ItemObject switchslot = vehicleInventory.GetSlot(storageSlotConfig.SlotNumber);
                        if (switchslot == null)
                        {
                            //Vehicle doesn't have attachment on that slot. Can install.
                            VehicleInstall(session, playerItemObject, vehicleInventory, storageSlotConfig.SlotNumber);
                            Player.Message(session, GetMsg("VehicleInstall", steamId).Replace("{attachInstalled}", playerItemObject.Generator.name).Replace("{vehicleType}", GetMsg("vehicles/" + vehicleType)));
                            updated = true;
                        }
                        else
                        {
                            ItemObject vehicleAttach = vehicleInventory.GetSlot(storageSlotConfig.SlotNumber);
                            if (RuntimeHurtDB.Instance.GetGuid(vehicleAttach.Generator) == RuntimeHurtDB.Instance.GetGuid(playerItemObject.Generator))
                            {
                                notSwitched = true;
                                continue;
                            }
                            else
                            {
                                //Vehicle have attachment on that slot. Can switch.
                                string attachSwitched = VehicleSwitch(session, playerItemObject, vehicleInventory, storageSlotConfig.SlotNumber);
                                Player.Message(session, GetMsg("VehicleSwitch", steamId).Replace("{attachSwitched}", attachSwitched).Replace("{vehicleType}", GetMsg("vehicles/" + vehicleType)).Replace("{attachInstalled}", playerItemObject.Generator.name));
                                updated = true;
                            }
                        }
                        vehicleInventory.Invalidate(false);
                    }
                    if (updated)
                        break;
                }
                if (notSwitched && !updated)
                    Player.Message(session, GetMsg("VehicleAttachmentAlreadyInstalled", steamId).Replace("{attachment}",playerItemObject.Generator.name));
            }
            else
            {
                Player.Message(session, GetMsg("NoAttachmentFound", steamId).Replace("{slot}", slot.ToString()));
            }
        }

        private void DoRemove(PlayerSession session, string[] args, string vehicleType, VehicleInventory vehicleInventory, VehiclePassenger[] vehiclePassengers)
        {
            string steamId = session.SteamId.ToString();

            // /car remove bumper|front|left|right|roof|rear|tire|engine|gearbox
            string tmp = args[1].ToLower();

            //Check for permission to remove.extra
            if (tmp == "gear" || tmp == "gearbox" || tmp == "engine" || tmp == "tire" || tmp == "wheel" || tmp == "rotor" || tmp == "rotors")
                if (!permission.UserHasPermission(steamId, RemoveExtraPermission))
                {
                    Player.Message(session, GetMsg("NoPermission", steamId).Replace("{permission}", RemoveExtraPermission));
                    return;
                }
            //Check for permission to remove
            if (tmp == "bumper" || tmp == "front" || tmp == "left" || tmp == "right" || tmp == "roof" || tmp == "rear" || tmp == "all")
                if (!permission.UserHasPermission(steamId, RemovePermission))
                {
                    Player.Message(session, GetMsg("NoPermission", steamId).Replace("{permission}", RemovePermission));
                    return;
                }

            List<string> parts;
            if (tmp == "all")
                parts = new List<string> { "bumper", "front", "left", "right", "roof", "rear" };
            else
                parts = new List<string> { tmp };

            bool ignorePart = false;
            foreach (string attach in parts)
            {
                ignorePart = false;
                // Cannot remove rear if a player is on the vehicle rear
                if (attach == "rear" || attach == "front")
                {
                    foreach (VehiclePassenger vehiclePassenger in vehiclePassengers)
                    {
                        if (vehiclePassenger.HasPassenger())
                        {
                            string seat = GetSeatName(vehiclePassenger.SeatOffset);
                            if (attach == seat)
                            {
                                Player.Message(session, GetMsg("RemoveSeatError", steamId).Replace("{seat}", seat));
                                ignorePart = true;
                                break;
                            }
                        }
                    }
                }

                if (ignorePart)
                    continue;

                //Get the restrictedSlots relative to vehicle attachment player wants to remove
                List<int> restrictedSlots = GetRestrictedSlots(attach, vehicleType, vehicleInventory);
                GlobalItemManager globalItemManager = GlobalItemManager.Instance;
                foreach (int slot in restrictedSlots)
                {
                    //Give vehicle attach to player inventory
                    ItemObject vehicleAttach = vehicleInventory.GetSlot(slot);
                    globalItemManager.GiveItem(session.Player, vehicleAttach);
                    //Remove attachment from vehicle.
                    vehicleInventory.SetSlot(slot, null);
                    vehicleInventory.Invalidate(false);
                    if (tmp != "all")
                        Player.Message(session, GetMsg("VehicleRemove", steamId).Replace("{attachRemoved}", vehicleAttach.Generator.name).Replace("{vehicleType}", GetMsg("vehicles/" + vehicleType)));
                }
                if (restrictedSlots.Count == 0)
                    if (tmp != "all")
                        Player.Message(session, GetMsg("RemoveAttachmentError").Replace("{attachment}", attach));
            }
            if (tmp == "all")
                Player.Message(session, GetMsg("VehicleRemoveAll").Replace("{vehicleType}", GetMsg("vehicles/"+vehicleType)));
        }

        private void DoVehicleDistance(PlayerSession session)
        {
            string steamId = session.SteamId.ToString();
            List<VehicleOwnership> vehicleOwnerships = GetPlayerVehicles(session.Identity);
            if (vehicleOwnerships != null && vehicleOwnerships.Count > 0)
            {
                foreach (var vehicleOwnership in vehicleOwnerships)
                {
                    string distanceMsg = string.Format("{0:0}m", (int)Math.Ceiling(GetPlayerToCarDistance(session, vehicleOwnership)));
                    string vehicleName = GetMsg(vehicleOwnership.VehicleType.ToLower(), steamId);
                    Player.Message(session, GetMsg("VehicleDistance", steamId).Replace("{vehicle}", vehicleName).Replace("{distance}", distanceMsg));
                }
            }
            else
            {
                Player.Message(session, GetMsg("NoVehicleClaimed", steamId));
            }
        }

        private void DoHelpMessage(PlayerSession session)
        {
            string steamId = session.SteamId.ToString();

            Player.Message(session, GetMsg("Help_AvailableCommands", steamId));
            Player.Message(session, GetMsg("Help_CarDistance", steamId));
            Player.Message(session, GetMsg("Help_Install", steamId));
            Player.Message(session, GetMsg("Help_Remove", steamId));
            Player.Message(session, GetMsg("Help_RemoveExtra", steamId));
        }

        [ChatCommand("car")]
        void VehicleManagerCommand(PlayerSession session, string command, string[] args)
        {
            string steamId = session.SteamId.ToString();

            //Check for permission to use the plugin
            if (!permission.UserHasPermission(steamId, UsePermission))
            {
                Player.Message(session, GetMsg("NoPermission", steamId).Replace("{permission}", UsePermission));
                return;
            }

            //Get all claimed vehicles from player
            if (args.Length == 0)
            {
                if (!showDistanceCommand)
                    Player.Message(session, GetMsg("VehicleDistanceDisabled", steamId));
                else
                    DoVehicleDistance(session);
                return;
            }

            //Display help command
            string cmd = args[0].ToLower();
            if (args.Length == 1 && cmd == "help" || cmd == "h")
            {
                DoHelpMessage(session);
                return;
            }

            // /car install/remove commands
            if (args.Length == 2 && (cmd == "install" || cmd == "remove"))
            {
                VehicleOwnership vehicleOwnership = GetClosesVehicle(session);
                if (vehicleOwnership == null)
                {
                    Player.Message(session, "Something weird happend...");
                    return;
                }

                if (vehicleOwnership?.StatManager?.GetComponent<VehicleBase>() == null)
                {
                    Player.Message(session, GetMsg("NotInsideVehicle", steamId));
                    return;
                }

                VehicleBase vehicleBase = vehicleOwnership.StatManager.GetComponent<VehicleBase>();

                VehiclePassenger[] vehiclePassengers = vehicleBase.GetPassengers();
                if (vehiclePassengers.Count() < 0 || vehiclePassengers == null || !PlayerInsideVehicle(vehiclePassengers, session))
                {
                    Player.Message(session, GetMsg("NotInsideVehicle", steamId));
                    return;
                }

                VehicleInventory vehicleInventory = vehicleBase.GetComponent<VehicleInventory>();
                if (vehicleInventory == null)
                {
                    Player.Message(session, "Something weird happened, try again!");
                    return;
                }

                if (vehicleOwnership.GetOwner() != null && vehicleOwnership.GetOwner() != session.Identity)
                {
                    Player.Message(session, GetMsg("NotVehicleOwner", steamId));
                    return;
                }

                string vehicleType = vehicleOwnership.VehicleType;
                Puts(vehicleType);
                vehicleType = vehicleType?.Split('/')?[1]?.ToLower();
                if (vehicleType == null)
                    vehicleType = "unknown";

                if (cmd == "install")
                {
                    DoInstall(session, args, vehicleType, vehicleInventory);
                    return;
                }

                if (cmd == "remove")
                {
                    DoRemove(session, args, vehicleType, vehicleInventory, vehiclePassengers);
                    return;
                }
                return;
            }
            Player.Message(session, GetMsg("HelpCommand", steamId));
        }
        #endregion

        #region Helpers
        private bool PlayerInsideVehicle(VehiclePassenger[] vehiclePassengers, PlayerSession playerSession)
        {
            foreach (VehiclePassenger vehiclePassenger in vehiclePassengers)
            {
                GameObject passenger = vehiclePassenger.Passenger;
                if (passenger == null) continue;
                PlayerIdentity identity = Singleton<GameManager>.Instance.GetIdentity(passenger.HNetworkView().Owner);
                if (identity == null) continue;
                if (identity == playerSession.Identity) return true;
            }
            return false;
        }
        private List<VehicleOwnership> GetPlayerVehicles(PlayerIdentity identity)
        {
            List<VehicleOwnership> claims = Singleton<VehicleOwnershipManager>.Instance.GetClaims(identity);
            if (claims == null)
                return new List<VehicleOwnership>();
            claims = claims.Where(x => !x.StatManager.Dead).ToList();
            if (claims == null) 
                return new List<VehicleOwnership>();
            return claims.OrderBy(claim => GetPlayerToCarDistance(identity.ConnectedSession, claim)).ToList();
        }

        private VehicleOwnership GetClosesVehicle(PlayerSession playerSession)
        {
            VehicleOwnership[] vehicleOwnerships = Resources.FindObjectsOfTypeAll<VehicleOwnership>();
            if (vehicleOwnerships == null || vehicleOwnerships.Count() < 1)
                return null;

            IEnumerable<VehicleOwnership> unDeadOwnerships = vehicleOwnerships.Where(x => x.StatManager != null && !x.StatManager.Dead);
            if (unDeadOwnerships == null || unDeadOwnerships.Count() < 1)
                return null;

            IEnumerable<VehicleOwnership> sortedOwnerships = unDeadOwnerships.OrderBy(vehicleOwnership => GetPlayerToCarDistance(playerSession, vehicleOwnership));
            if (sortedOwnerships == null || sortedOwnerships.Count() < 1)
                return null;

            foreach (VehicleOwnership vehicleOwnership in sortedOwnerships)
            {
                if (vehicleOwnership?.StatManager?.GetComponent<VehicleBase>() == null) continue;
                VehicleBase vehicleBase = vehicleOwnership.StatManager.GetComponent<VehicleBase>();
                VehiclePassenger[] vehiclePassengers = vehicleBase.GetPassengers();
                if (vehiclePassengers == null || vehiclePassengers.Count() < 1) continue;
                if (PlayerInsideVehicle(vehiclePassengers, playerSession))
                    return vehicleOwnership;
            }

            return sortedOwnerships.First();
        }

        private float GetPlayerToCarDistance(PlayerSession owner, VehicleOwnership VO)
        {
            if (owner?.WorldPlayerEntity?.transform?.position == null) return float.MaxValue;
            Vector3 playerPos = owner.WorldPlayerEntity.transform.position;
            if (VO?.StatManager?.transform?.position == null) return float.MaxValue;
            Vector3 vehiclePos = VO.StatManager.transform.position;
            return Vector3.Distance(playerPos, vehiclePos);
        }

        private List<int> GetRestrictedSlots(string attach, string vehicleType, VehicleInventory vehicleInventory)
        {
            List<int> slots = new List<int>();
            List<ESlotType> results = new List<ESlotType>() { ESlotType.None };
            switch (attach)
            {
                case "rotors":
                case "rotor":
                    results.Add(ESlotType.MozzyRearRotors);
                    results.Add(ESlotType.MozzyMainRotors);
                    break;

                case "bumper":
                    results.Add(ESlotType.RoachBullBar);
                    break;

                case "left":
                    results.Add(vehicleType == "roach" ? ESlotType.RoachLeftPanel : ESlotType.MozzyLeftPanel);
                    break;

                case "right":
                    results.Add(vehicleType == "roach" ? ESlotType.RoachRightPanel : ESlotType.MozzyRightPanel);
                    break;

                case "roof":
                    results.Add(vehicleType == "roach" ? ESlotType.RoachRoofBay : ESlotType.MozzyRoofPanel);
                    break;

                case "rear":
                    switch (vehicleType)
                    {
                        case "roach": results.Add(ESlotType.RoachRearBay); break;
                        case "mozzy": results.Add(ESlotType.MozzyTail); break;
                        case "kanga": results.Add(ESlotType.KangaBackpanel); break;
                        case "goat": results.Add(ESlotType.GoatBackpanel); break;
                    }
                    break;

                case "front":
                    switch (vehicleType)
                    {
                        case "roach": results.Add(ESlotType.RoachFrontBay); break;
                        case "mozzy": results.Add(ESlotType.MozzyFrontPanel); break;
                        case "kanga": results.Add(ESlotType.KangaFrontpanel); break;
                        case "goat": results.Add(ESlotType.GoatFrontpanel); break;
                    }
                    break;

                case "tire":
                case "wheel":
                    switch (vehicleType)
                    {
                        case "roach": results.Add(ESlotType.RoachWheel); break;
                        case "kanga":
                            results.Add(ESlotType.KangaRearWheel);
                            results.Add(ESlotType.KangaFrontWheel);
                            break;
                        case "goat": results.Add(ESlotType.QuadWheel); break;
                        case "slug": results.Add(ESlotType.SlugWheel); break;
                    }
                    break;

                case "gearbox":
                case "gear":
                    switch (vehicleType)
                    {
                        case "roach": results.Add(ESlotType.RoachGearbox); break;
                        case "kanga": results.Add(ESlotType.KangaGearbox); break;
                        case "goat": results.Add(ESlotType.QuadGearbox); break;
                        case "slug": results.Add(ESlotType.SlugGearbox); break;
                    }
                    break;

                case "engine":
                    switch (vehicleType)
                    {
                        case "roach": results.Add(ESlotType.RoachEngine); break;
                        case "mozzy": results.Add(ESlotType.MozzyEngine); break;
                        case "kanga": results.Add(ESlotType.KangaEngine); break;
                        case "goat": results.Add(ESlotType.QuadEngine); break;
                        case "slug": results.Add(ESlotType.SlugEngine); break;
                        case "barra": results.Add(ESlotType.BarraEngine); break;
                    }
                    break;
                default: break;
            }

            for (int slotNumber = 0; slotNumber < vehicleInventory.StorageConfig.Slots.Length; ++slotNumber)
            {
                StorageSlotConfig storageSlotConfig = vehicleInventory.StorageConfig.Slots[slotNumber];
                if (vehicleInventory.GetSlot(storageSlotConfig.SlotNumber) == null) continue;
                List<ESlotType> slotRestrictions = storageSlotConfig.SlotRestrictions;

                IEnumerable<ESlotType> slot = slotRestrictions.Where(x => results.Contains(x));

                if (slot != null && slot.Count() > 0)
                    slots.Add(slotNumber);
            }
            return slots;
        }

        private void VehicleInstall(PlayerSession session, ItemObject playerItemObject, VehicleInventory vehicleInventory, int slotNumber)
        {
            //Add attachment to vehicle.
            vehicleInventory.GiveItemServer(playerItemObject, slotNumber, slotNumber);
            vehicleInventory.Invalidate(false);
            //Remove attachment from player inventory
            playerItemObject.InvalidateStorage();
        }

        private string VehicleSwitch(PlayerSession session, ItemObject playerItemObject, VehicleInventory vehicleInventory, int slotNumber)
        {
            //Give vehicle attachment to player
            PlayerInventory playerInventory = session.WorldPlayerEntity.GetComponent<PlayerInventory>();
            ItemObject vehicleAttach = vehicleInventory.GetSlot(slotNumber);
            playerInventory.GiveItemServer(vehicleAttach);
            playerItemObject.InvalidateStorage();

            //Add attachment to vehicle
            vehicleInventory.GiveItemServer(playerItemObject, slotNumber, slotNumber);
            vehicleInventory.Invalidate(false);

            return vehicleAttach.Generator.name;
        }

        private ESlotType GetSlotType(ItemObject item)
        {
            foreach (ESlotType eSlotType in Enum.GetValues(typeof(ESlotType)))
            {
                if (item.Generator.DataProvider.IsSlotType(eSlotType) && eSlotType != ESlotType.None)
                    return eSlotType;
            }
            return ESlotType.None;
        }

        private string GetSeatName(Vector3 offset)
        {
            string seat;
            switch (offset.ToString())
            {
                case "(0.0, -0.2, 1.1)":
                    seat = "front";
                    break;
                case "(-0.4, 0.1, 0.0)":
                    seat = "left";
                    break;
                case "(0.4, 0.1, 0.0)":
                    seat = "right";
                    break;
                case "(0.0, 0.9, -1.1)":
                    seat = "rear";
                    break;
                case "(0.0, 0.0, 0.0)":
                    seat = "goat/kanga";
                    break;
                default:
                    seat = "unknown";
                    break;
            }
            return seat;
        }
        #endregion
    }
}