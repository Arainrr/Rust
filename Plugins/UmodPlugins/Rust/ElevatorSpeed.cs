﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Elevator Speed", "Lincoln", "1.0.4")]
    [Description("Adjust the speed of the elevator.")]
    public class ElevatorSpeed : RustPlugin
    {
        int minSpeed = 1;
        int currentSpeed = 1;
        string[] args = null;
        string command = "";
        string checkMessage = "";
        private const string permUse = "ElevatorSpeed.use";
        private const string permAdmin = "ElevatorSpeed.admin";

        #region config
        //Creating a config file
        private static PluginConfig config;
        private class PluginConfig
        {
            [JsonProperty(PropertyName = "Maximum Speed")] public int maximumSpeed { get; set; }

            public static PluginConfig DefaultConfig() => new PluginConfig()
            {
                maximumSpeed = 1
            };
        }
        protected override void LoadDefaultConfig()
        {
            PrintWarning("New configuration file created!!");
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
            SaveConfig();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
        #endregion

        private List<Elevator> FindElevator(Vector3 pos, float radius)
        {
            var hits = Physics.SphereCastAll(pos, radius, Vector3.up);
            var x = new List<Elevator>();
            foreach (var hit in hits)
            {
                var entity = hit.GetEntity()?.GetComponent<Elevator>();
                if (entity && !x.Contains(entity))
                    x.Add(entity);
            }

            return x;
        }

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoElevators"] = "<color=#ffc34d>Elevator Speed: </color> No owned elevators found. Please stand near by your elevator.",
                ["NotOwner"] = "<color=#ffc34d>Elevator Speed: </color> You do not own this elevator.",
                ["SpeedUpdate"] = "<color=#ffc34d>Elevator Speed: </color>Updating elevator speed to <color=#b0fa66>{0}</color>.",
                ["SpeedCheck"] = "<color=#ffc34d>Elevator Speed: </color>This elevator speed is set to <color=#b0fa66>{0}</color>.",
                ["NoPerm"] = "<color=#ffc34d>Elevator Speed</color>: You do not have permissions to use this.",
                ["SpeedInvalid"] = "<color=#ffc34d>Elevator Speed</color>: Please choose a speed between <color=#b0fa66>1</color> and <color=#b0fa66>{0}</color>. Default speed is <color=#b0fa66>1</color>.",
            }, this);
        }
        #endregion
        private void OnServerInitialized()
        {
            permission.RegisterPermission(permUse, this);
            permission.RegisterPermission(permAdmin, this);
        }
        private bool HasPermission(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, permUse)) return true;
            else return false;
        }
        private bool HasAdmin(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, permAdmin)) return true;
            else return false;
        }
        [ChatCommand("liftcheck")]
        private void LiftCheckCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player))
            {
                player.ChatMessage(lang.GetMessage("NoPerm", this, player.UserIDString)); return;
            }
            var elevatorList = FindElevator(player.transform.position, 3);
            if (HasAdmin(player))
            {
                CheckSpeedAdmin(player, currentSpeed);
                return;
            }
            if (elevatorList.Count == 0)
            {
                player.ChatMessage(lang.GetMessage("NoElevators", this, player.UserIDString)); return;
            }

            if (!CheckSpeed(player, currentSpeed))
            {
                var playerMessage = string.Format(lang.GetMessage("NotOwner", this, player.UserIDString));
                player.ChatMessage(playerMessage);
                return;
            }

            player.ChatMessage(string.Format(lang.GetMessage("SpeedCheck", this, player.UserIDString), checkMessage));
        }
        [ChatCommand("liftspeed")]
        private void LiftSpeedCommand(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;

            if (!HasPermission(player))
            {
                player.ChatMessage(lang.GetMessage("NoPerm", this, player.UserIDString)); return;
            }

            if (args.Length == 0)
            {
                player.ChatMessage(string.Format(lang.GetMessage("SpeedInvalid", this, player.UserIDString), config.maximumSpeed));
                return;
            }

            try
            {
                currentSpeed = Convert.ToInt32(args[0]);
            }
            catch
            {
                return;
            }
            var elevatorList = FindElevator(player.transform.position, 3);
            if (elevatorList.Count == 0)
            {
                player.ChatMessage(string.Format(lang.GetMessage("NoElevators", this, player.UserIDString)));
                return;
            }
            if (currentSpeed < minSpeed)
            {
                player.ChatMessage(string.Format(lang.GetMessage("SpeedInvalid", this, player.UserIDString), config.maximumSpeed));
                return;
            }
            if (HasAdmin(player))
            {
                ChangeSpeedAdmin(player, currentSpeed);
                player.ChatMessage(string.Format(lang.GetMessage("SpeedUpdate", this, player.UserIDString), currentSpeed)); return;
            }
            if (currentSpeed > config.maximumSpeed)
            {
                player.ChatMessage(string.Format(lang.GetMessage("SpeedInvalid", this, player.UserIDString), config.maximumSpeed));
                return;
            }
            if (!ChangeSpeed(player, currentSpeed))
            {
                player.ChatMessage(string.Format(lang.GetMessage("NotOwner", this, player.UserIDString)));
                return;
            }
            player.ChatMessage(string.Format(lang.GetMessage("SpeedUpdate", this, player.UserIDString), currentSpeed));
        }
        private bool ChangeSpeed(BasePlayer player, int currentSpeed)
        {
            var elevator = UnityEngine.Object.FindObjectOfType<Elevator>();
            var elevatorOwner = BasePlayer.FindByID(elevator.OwnerID);
            var elevatorList = FindElevator(player.transform.position, 3);


            foreach (var entity in elevatorList)
            {
                var x = entity;
                if (entity.OwnerID != player.userID)
                {
                    if (HasAdmin(player)) return true;

                    return false;
                }
                if (x is Elevator)
                {
                    entity.LiftSpeedPerMetre = currentSpeed;
                }
            }
            return true;
        }
        private void ChangeSpeedAdmin(BasePlayer player, int currentSpeed)
        {
            var elevatorList = FindElevator(player.transform.position, 3);

            foreach (var entity in elevatorList)
            {
                var x = entity;
                if (x is Elevator)
                {
                    entity.LiftSpeedPerMetre = currentSpeed;
                }
            }
        }
        private void CheckSpeedAdmin(BasePlayer player, int currentSpeed)
        {
            var elevatorList = FindElevator(player.transform.position, 3);

            foreach (var entity in elevatorList)
            {
                var x = entity as Elevator;

                if (x is Elevator)
                {
                    checkMessage = entity.LiftSpeedPerMetre.ToString();
                }
            }
            player.ChatMessage(string.Format(lang.GetMessage("SpeedCheck", this, player.UserIDString), checkMessage));
        }
        private bool CheckSpeed(BasePlayer player, int currentSpeed)
        {
            var elevatorList = FindElevator(player.transform.position, 3);

            foreach (var entity in elevatorList)
            {
                var x = entity as Elevator;
                if (entity.OwnerID != player.userID)
                {
                    if (HasAdmin(player))
                    {
                        return true;
                    }
                    return false;
                }
                if (x is Elevator)
                {
                    checkMessage = entity.LiftSpeedPerMetre.ToString();
                }
            }
            return true;
        }
        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            var entity = go.ToBaseEntity();
            if (entity == null) return;

            var elevator = entity as Elevator;
            if (elevator == null) return;

            var elevatorBelow = elevator.GetElevatorInDirection(Elevator.Direction.Down);
            if (elevatorBelow == null) return;

            elevator.OwnerID = elevatorBelow.OwnerID;
            Puts(elevator.OwnerID + " " + elevatorBelow.OwnerID);
        }
        private void OnEntitySpawned(Elevator elevator)
        {
            if (elevator == null || elevator.OwnerID == 0) return;
            var player = BasePlayer.FindByID(elevator.OwnerID);
            if (!HasPermission(player)) return;
            ChangeSpeed(player, currentSpeed);
        }
        private void Unload()
        {
            config = null;
        }

    }
}