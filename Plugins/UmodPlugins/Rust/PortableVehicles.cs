using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VLB;

namespace Oxide.Plugins
{
    [Info("Portable Vehicles", "Orange", "1.0.15")]
    [Description("Give vehicles as item to your players")]
    public class PortableVehicles : RustPlugin
    {
        #region Vars

        private Dictionary<ulong, string> skinToPrefab = new Dictionary<ulong, string>
        {
            {1742627792, "assets/content/vehicles/boats/rhib/rhib.prefab"},
            {1742651766, "assets/content/vehicles/boats/rowboat/rowboat.prefab"},
            {1742653197, "assets/content/vehicles/minicopter/minicopter.entity.prefab"},
            {1742652663, "assets/content/vehicles/sedan_a/sedantest.entity.prefab"},
            {1771792500, "assets/prefabs/npc/ch47/ch47.entity.prefab"},
            {1771792987, "assets/prefabs/deployable/hot air balloon/hotairballoon.prefab"},
            {1773898864, "assets/rust.ai/nextai/testridablehorse.prefab"},
            {1856165291, "assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab"},
            {2160249787, "assets/content/vehicles/modularcar/2module_car_spawned.entity.prefab"},
            {2160250208, "assets/content/vehicles/modularcar/3module_car_spawned.entity.prefab"},
            {2160251723, "assets/content/vehicles/modularcar/4module_car_spawned.entity.prefab"},
        };

        private const string itemShortName = "box.repair.bench";
        private const string commandGive = "portablevehicles.give";
        private const string permPickup = "portablevehicles.pickup";

        #endregion

        #region Oxide Hooks

        private void Init()
        {
            cmd.AddConsoleCommand(commandGive, this, nameof(cmdGiveConsole));
            permission.RegisterPermission(permPickup, this);
        }

        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            CheckPlacement(plan, go);
        }

        private object OnHammerHit(BasePlayer player, HitInfo info)
        {
            var entity = info?.HitEntity?.GetComponent<BaseVehicle>();
            if (CheckPickup(player, entity) == true)
            {
                return true;
            }

            return null;
        } 

        #endregion

        #region Commands

        private void cmdGiveConsole(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin == false)
            {
                Message(arg, "Permission");
                return;
            }

            var args = arg.Args;
            if (args == null || args.Length < 2)
            {
                Message(arg, "Usage");
                return;
            }

            var player = FindPlayer(arg, args[0]);
            if (player == null)
            {
                return;
            }

            var skin = GetSkin(args[1]);
            if (skin == 0)
            {
                Message(arg, "Usage");
                return;
            }

            GiveItem(player, skin);
        }

        #endregion

        #region Core

        private void CheckPlacement(Planner plan, GameObject go)
        {
            var entity = go.ToBaseEntity();
            if (entity == null)
            {
                return;
            }

            var player = plan.GetOwnerPlayer();
            var prefab = (string) null;
            if (!skinToPrefab.TryGetValue(entity.skinID, out prefab))
            {
                return;
            }

            var transform = entity.transform;
            var position = transform.position;
            var rotation = transform.rotation;
            var owner = entity.OwnerID;
            var skin = entity.skinID;
            
            transform.position = new Vector3();
            entity.TransformChanged();
            timer.Once(1f, () =>
            {
                if (entity.IsValid() && entity.IsDestroyed == false)
                {
                    entity.Kill();
                }
            });

            var vehicle = GameManager.server.CreateEntity(prefab, position, rotation)?.GetComponent<BaseVehicle>();
            if (vehicle != null)
            {
                vehicle.skinID = skin;
                vehicle.OwnerID = owner;
                vehicle.Spawn();

                if (vehicle.mountPoints != null && vehicle.mountPoints.Count > 0)
                {
                    var driverSeat = vehicle.mountPoints.FirstOrDefault()?.mountable;
                    if (driverSeat != null)
                    { 
                        driverSeat.MountPlayer(player);
                        player.SendNetworkUpdate();
                    }
                }
            }
        }

        private bool CheckPickup(BasePlayer player, BaseVehicle entity)
        {
            if (entity == null)
            {
                return false;
            }

            // TODO: Bring me back later
            // if (entity.skinID == 0)
            // {
            //     return false;
            // }
            
            if (permission.UserHasPermission(player.UserIDString, permPickup) == false)
            {
                return false;
            }

            var time = entity.SecondsSinceAttacked;
            if (time < 30)
            {
                Message(player, "Recently Attacked", (30 - time).ToString("0.0"));
                return true; 
            }
            
            var diff = (Mathf.Abs(entity.MaxHealth() - entity.Health()));
            if (diff > 5f)
            { 
                Message(player, "Durability");
                return false;
            }

            if (entity.OwnerID != player.userID)
            {
                Message(player, "Pickup Ownership");
                return true;
            }

            if (player.CanBuild() == false)
            {
                Message(player, "Cupboard");
                return true;
            }

            var containers = entity.GetComponentsInChildren<StorageContainer>();
            if (containers.Any(x => x.inventory.itemList.Count > 0))
            {
                Message(player, "Not Empty");
                return true;
            }

            var fs = entity.GetFuelSystem();
            if (fs != null && fs.GetFuelContainer().IsLocked() == false && fs.HasFuel())
            {
                Message(player, "Fuel");
                return true;
            }

            var script = entity.GetOrAddComponent<PickupScript>();
            script.AddHit();
            var left = script.GetHitsLeft();
            if (left > 0)
            {
                Message(player, "Hits", script.GetHitsLeft());
                return true;
            }

            foreach (var value in skinToPrefab)
            {
                if (value.Value == entity.PrefabName)
                {
                    entity.Kill();
                    GiveItem(player, value.Key);
                    return true;
                }
            }

            return false;
        }

        private BasePlayer FindPlayer(ConsoleSystem.Arg arg, string nameOrID)
        {
            var targets = BasePlayer.activePlayerList.Where(x =>
                x.UserIDString == nameOrID || x.displayName.ToLower().Contains(nameOrID.ToLower())).ToList();

            if (targets.Count == 0)
            {
                Message(arg, "No Player");
                return null;
            }

            if (targets.Count > 1)
            {
                Message(arg, "Multiple Players");
                return null;
            }

            return targets[0];
        }

        private void GiveItem(BasePlayer player, ulong skinID)
        {
            var item = ItemManager.CreateByName(itemShortName, 1, skinID);
            if (item != null)
            {
                item.name = "Portable Vehicle";
                player.GiveItem(item);
                Message(player, "Received");
            }
        }

        private ulong GetSkin(string name)
        {
            switch (name.ToLower())
            {
                case "rhib":
                case "militaryboat":
                case "military":
                    return 1742627792;

                case "boat":
                case "rowboat":
                case "motorboat":
                    return 1742651766;

                case "copter":
                case "minicopter":
                    return 1742653197;

                case "balloon":
                case "hotairballoon":
                    return 1771792987;

                case "ch":
                case "ch47":
                case "chinook":
                    return 1771792500;

                case "horse":
                case "testridablehorse":
                    return 1773898864;

                case "scrap":
                case "scraphelicopter":
                case "helicopter":
                    return 1856165291;
                
                case "car":
                case "car1":
                case "sedan":
                    return 1742652663;
                
                case "car2":
                    return 2160249787;
                
                case "car3":
                    return 2160250208;
                
                case "car4":
                    return 2160251723;

                default:
                    return 0;
            }
        }

        #endregion

        #region Localization 1.1.1

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Usage", "Usage: portablevehicles.give [steamID / player name] [vehicle name]\n"},
                {"Permission", "You don't have permission to use that!"},
                {"Received", "You received portable vehicle!"},
                {"No Player", "There are no players with that Name or steamID!"},
                {"Multiple Players", "There are many players with that Name:\n{0}"},
                {"Pickup Ownership", "Only owner can pickup vehicles!"},
                {"Fuel", "You need to remove fuel from vehicle first!"},
                {"Recently Attacked", "Vehicle was recently attacked! {0}s left"},
                {"Durability", "You need to repair vehicles fully!"},
                {"Not Empty", "Vehicle is not empty! Check fuel or storages!"},
                {"Hits", "You need to do more {0} hits!"},
                {"Cupboard", "You need to have building privilege to do that!"}
            }, this);
        }

        private void Message(ConsoleSystem.Arg arg, string messageKey, params object[] args)
        {
            var message = GetMessage(messageKey, null, args);
            var player = arg.Player();
            if (player != null)
            {
                player.ChatMessage(message);
            }
            else
            {
                Puts(message);
            }
        }

        private void Message(BasePlayer player, string messageKey, params object[] args)
        {
            if (player == null)
            {
                return;
            }

            var message = GetMessage(messageKey, player.UserIDString, args);
            player.ChatMessage(message);
        }

        private string GetMessage(string messageKey, string playerID, params object[] args)
        {
            return string.Format(lang.GetMessage(messageKey, this, playerID), args);
        }

        #endregion

        #region Scripts

        private class PickupScript : MonoBehaviour
        {
            private BaseVehicle entity;
            private int hits;

            private void Awake()
            {
                entity = GetComponent<BaseVehicle>();
            }

            public void AddHit()
            {
                hits++;
                CancelInvoke(nameof(ResetHits));
                Invoke(nameof(ResetHits), 60);
            }

            private void ResetHits()
            {
                hits = 0;
            }

            public int GetHitsLeft()
            {
                return 5 - hits;
            }
        }

        #endregion
    }
}