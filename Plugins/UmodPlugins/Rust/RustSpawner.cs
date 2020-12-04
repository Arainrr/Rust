﻿using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Rust Spawner", "Zoin", "2.0.2")]
    [Description("Rust Spawner is a plugin that you can spawn cars/helis/animals/scarecrow")]
    class RustSpawner : CovalencePlugin
    {
        #region Variables
        Dictionary<string, Timer> CoolDowns = new Dictionary<string, Timer>();

        const string Horse_Perm = "rustspawner.horse";
        const string Wolf_Perm = "rustspawner.wolf";
        const string Bear_Perm = "rustspawner.bear";
        const string Sedan_Perm = "rustspawner.sedan";
        const string Scarecrow_Perm = "rustspawner.scarecrow";
        const string Minicopter_Perm = "rustspawner.minicopter";
        const string ScrapHeli_Perm = "rustspawner.scrapheli";
        const string NoCooldown_Perm = "rustspawner.nocooldown";
        #endregion

        #region Configuaration
        protected override void LoadDefaultConfig()
        {
            LogWarning("Creating a new configuration file");
            Config["Cooldown"] = 300;
            Config["BuildingSpawn"] = false;
        }
        #endregion

        #region LanguageAPI
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You do not have access to that command!",
                ["InvalidInput"] = "Please enter a valid spawnable name!",
                ["IndoorsBlocked"] = "You cannot spawn indoors only outside!",
                ["Info"] = "You can spawn the following entitys\nhorse ,wolf ,bear ,minicopter ,sedan ,scarecrow",
                ["Cooldown"] = "You are still on cooldown!",
                ["Spawned"] = "Your {0} has been spawned!",
                ["Prefix"] = "[Rust Spawner] "
            }, this);
        }
        #endregion

        #region Hooks
        private void Init()
        {
            permission.RegisterPermission(NoCooldown_Perm, this);
            permission.RegisterPermission(Horse_Perm, this);
            permission.RegisterPermission(Wolf_Perm, this);
            permission.RegisterPermission(Bear_Perm, this);
            permission.RegisterPermission(Minicopter_Perm, this);
            permission.RegisterPermission(Sedan_Perm, this);
            permission.RegisterPermission(ScrapHeli_Perm, this);
            permission.RegisterPermission(Scarecrow_Perm, this);
        }
        #endregion

        #region Commands
        [Command("rspawn")]
        private void Spawn(IPlayer player, string command, string[] args)
        {
            if (player.IsServer)
            {
                return;
            }

            string prefix = lang.GetMessage("Prefix", this, player.Id);

            if (args.Length < 1)
            {
                player.Reply(prefix + lang.GetMessage("InvalidInput", this, player.Id));
                return;
            }

            switch (args[0])
            {
                case "horse":
                    {
                        if (player.HasPermission(Horse_Perm))
                            SpawnEntity(player, "assets/rust.ai/nextai/testridablehorse.prefab");
                        else
                            player.Reply(prefix + lang.GetMessage("NoPermission", this, player.Id));
                        break;
                    }
                case "wolf":
                    {
                        if (player.HasPermission(Wolf_Perm))
                            SpawnEntity(player, "assets/rust.ai/agents/wolf/wolf.prefab");
                        else
                            player.Reply(prefix + lang.GetMessage("NoPermission", this, player.Id));
                        break;
                    }
                case "bear":
                    {
                        if (player.HasPermission(Bear_Perm))
                            SpawnEntity(player, "assets/rust.ai/agents/bear/bear.prefab");
                        else
                            player.Reply(prefix + lang.GetMessage("NoPermission", this, player.Id));
                        break;
                    }
                case "mini":
                    {
                        if (player.HasPermission(Minicopter_Perm))
                            SpawnEntity(player, "assets/content/vehicles/minicopter/minicopter.entity.prefab");
                        else
                            player.Reply(prefix + lang.GetMessage("NoPermission", this, player.Id));
                        break;
                    }
                case "scrapheli":
                    {
                        if (player.HasPermission(ScrapHeli_Perm))
                            SpawnEntity(player, "assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab");
                        else
                            player.Reply(prefix + lang.GetMessage("NoPermission", this, player.Id));
                        break;
                    }
                case "car":
                    {
                        if (player.HasPermission(Sedan_Perm))
                            SpawnEntity(player, "assets/content/vehicles/sedan_a/sedantest.entity.prefab");
                        else
                            player.Reply(prefix + lang.GetMessage("NoPermission", this, player.Id));
                        break;
                    }
                case "scarecrow":
                    {
                        if (player.HasPermission(Scarecrow_Perm))
                            SpawnEntity(player, "assets/prefabs/npc/scarecrow/scarecrow.prefab");
                        else
                            player.Reply(prefix + lang.GetMessage("NoPermission", this, player.Id));
                        break;
                    }
                case "info":
                    {
                            player.Reply(prefix + lang.GetMessage("Info", this, player.Id));
                        break;
                    }
                default:
                    player.Reply(prefix + lang.GetMessage("InvalidInput", this, player.Id));
                    break;
            }
        }
        #endregion

        #region Functions
        void SpawnEntity(IPlayer player, string entity_name)
        {
            BasePlayer PlayerObject = player.Object as BasePlayer;
            string prefix = lang.GetMessage("Prefix", this, player.Id);

            #region Cooldown
            if (!player.HasPermission(NoCooldown_Perm))
            {
                if (CoolDowns.ContainsKey(player.Id))
                {
                    player.Reply(prefix + lang.GetMessage("Cooldown", this, player.Id));
                    return;
                }
                else
                {
                    string id = player.Id;
                    Timer Cooldown = timer.Once((float)Config["Cooldown"], () =>
                    {
                        CoolDowns.Remove(id);
                    });

                    CoolDowns.Add(id, Cooldown);
                }
            }
            #endregion

            #region Building Check
            if (!(bool)Config["BuildingSpawn"] & !PlayerObject.IsOutside())
            {
                player.Reply(prefix + lang.GetMessage("IndoorsBlocked", this, player.Id));
                return;
            }
            #endregion

            #region Raycast
            Vector3 ViewAdjust = new Vector3(0f, 1.5f, 0f);
            Vector3 position = PlayerObject.transform.position + ViewAdjust;
            Vector3 rotation = Quaternion.Euler(PlayerObject.serverInput.current.aimAngles) * Vector3.forward;
            int range = 10;

            RaycastHit hit;
            if (!Physics.Raycast(position, rotation, out hit, range))
            {
                return;
            }
            #endregion

            #region Actual Spawning
            BaseEntity Entity = GameManager.server.CreateEntity(entity_name, hit.point);

            if (Entity)
            {
                Entity.Spawn();
                player.Reply(prefix + string.Format(lang.GetMessage("Spawned", this, player.Id), Entity.ShortPrefabName));
            }
            #endregion
        }
        #endregion
    }
}