using System;
using UnityEngine;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Convert = System.Convert;
using System.Linq;
using Oxide.Game.Rust.Cui;
using Rust;

namespace Oxide.Plugins
{
    [Info("Lock Master", "FastBurst", "1.0.4")]
    [Description("Lock all your storages and deployables")]
    class LockMaster : RustPlugin
    {
        private static bool isPlayer(ulong id) => id > 76560000000000000L;
        private const string COFFIN_PREFAB = "assets/prefabs/misc/halloween/coffin/coffinstorage.prefab";
        private const string COMPOSTER_PREFAB = "assets/prefabs/deployable/composter/composter.prefab";
        private const string DROPBOX_PREFAB = "assets/prefabs/deployable/dropbox/dropbox.deployed.prefab";
        private const string VENDING_PREFAB = "assets/prefabs/deployable/vendingmachine/vendingmachine.deployed.prefab";
        private const string FURNACE_PREFAB = "assets/prefabs/deployable/furnace/furnace.prefab";
        private const string LARGE_FURNACE_PREFAB = "assets/prefabs/deployable/furnace.large/furnace.large.prefab";
        private const string REFINERY_PREFAB = "assets/prefabs/deployable/oil refinery/refinery_small_deployed.prefab";
        private const string BBQ_PREFAB = "assets/prefabs/deployable/bbq/bbq.deployed.prefab";
        public static LockMaster Instance;
        ConfigData configData;

        class ConfigData
        {
            public string Command = "refreshall";
            public string PermAdmin = "lockmaster.admin";
        }

        protected override void LoadDefaultConfig()
        {
            configData = new ConfigData() { };

            SaveConfig(configData);
            PrintWarning("New configuration file created.");
        }

        private void Init()
        {
            Instance = this;
            configData = Config.ReadObject<ConfigData>();

            SaveConfig(configData);

            permission.RegisterPermission(configData.PermAdmin, this);
            cmd.AddChatCommand(configData.Command, this, "CmdRefresh");
        }

        private void SaveConfig(ConfigData config) => Config.WriteObject(config, true);

        private void CmdRefresh(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, configData.PermAdmin))
            {
                Message(player, "Usage");
                return;
            }
            NextTick(() =>
            {
                foreach (var entity in UnityEngine.Object.FindObjectsOfType<BaseOven>())
                {
                    if (entity == null) continue;
                    if (isPlayer(entity.OwnerID))
                    {
                        var oven = entity as BaseOven;
                        oven.isLockable = true;
                        oven.SendNetworkUpdate();
                        oven.SendNetworkUpdateImmediate(true);
                    }
                }

                foreach (var entity in UnityEngine.Object.FindObjectsOfType<Composter>())
                {
                    if (entity == null) continue;
                    if (isPlayer(entity.OwnerID))
                    {
                        var composters = entity as Composter;
                        composters.isLockable = true;
                        composters.SendNetworkUpdate();
                        composters.SendNetworkUpdateImmediate(true);
                    }
                }

                foreach (var entity in UnityEngine.Object.FindObjectsOfType<DropBox>())
                {
                    if (entity == null) continue;
                    if (isPlayer(entity.OwnerID))
                    {
                        var dropboxes = entity as DropBox;
                        dropboxes.isLockable = true;
                        dropboxes.SendNetworkUpdate();
                        dropboxes.SendNetworkUpdateImmediate(true);
                    }
                }

                foreach (var entity in UnityEngine.Object.FindObjectsOfType<VendingMachine>())
                {
                    if (entity == null) continue;
                    if (isPlayer(entity.OwnerID))
                    {
                        var vendingBoxes = entity as VendingMachine;
                        vendingBoxes.isLockable = true;
                        vendingBoxes.SendNetworkUpdate();
                        vendingBoxes.SendNetworkUpdateImmediate(true);
                    }
                }
                Message(player, "Success");
            });
        }

        private void OnServerInitialized()
        {
            foreach (var entity in UnityEngine.Object.FindObjectsOfType<Composter>())
            {
                if (entity == null) continue;
                if (isPlayer(entity.OwnerID))
                {
                    var composters = entity as Composter;
                    composters.isLockable = true;
                    composters.SendNetworkUpdate();
                    composters.SendNetworkUpdateImmediate(true);
                }
            }

            foreach (var entity in UnityEngine.Object.FindObjectsOfType<DropBox>())
            {
                if (entity == null) continue;
                if (isPlayer(entity.OwnerID))
                {
                    var dropboxes = entity as DropBox;
                    dropboxes.isLockable = true;
                    dropboxes.SendNetworkUpdate();
                    dropboxes.SendNetworkUpdateImmediate(true);
                }
            }

            foreach (var entity in UnityEngine.Object.FindObjectsOfType<VendingMachine>())
            {
                if (entity == null) continue;
                if (isPlayer(entity.OwnerID))
                {
                    var vendingBoxes = entity as VendingMachine;
                    vendingBoxes.isLockable = true;
                    vendingBoxes.SendNetworkUpdate();
                    vendingBoxes.SendNetworkUpdateImmediate(true);
                }
            }

            foreach (var entity in UnityEngine.Object.FindObjectsOfType<BaseOven>())
            {
                if (entity == null) continue;
                if (isPlayer(entity.OwnerID))
                {
                    var ovens = entity as BaseOven;
                    ovens.isLockable = true;
                    ovens.SendNetworkUpdate();
                    ovens.SendNetworkUpdateImmediate(true);
                }
            }

            foreach (var entity in UnityEngine.Object.FindObjectsOfType<StorageContainer>())
            {
                OnEntitySpawned(entity);
            }
        }


        private void OnEntitySpawned(BaseNetworkable entity)
        {
            // Lock Composters
            if (entity is Composter)
            {
                var composterLock = entity as Composter;
                composterLock.isLockable = true;
                composterLock.SendNetworkUpdateImmediate(true);
                return;
            }
            var lockcompster = (entity as BaseLock)?.GetParentEntity() as Composter;
            if (lockcompster != null)
            {
                switch (lockcompster.PrefabName)
                {
                    case COMPOSTER_PREFAB:
                        entity.transform.localPosition = new Vector3(0f, 1.3f, 0.6f);
                        entity.transform.localRotation = Quaternion.Euler(0, 90, 0);
                        break;
                }
            }

            // Lock DropbBoxes
            if (entity is DropBox)
            {
                var dropboxLock = entity as DropBox;
                dropboxLock.isLockable = true;
                dropboxLock.SendNetworkUpdateImmediate(true);
                return;
            }
            var lockboxes = (entity as BaseLock)?.GetParentEntity() as DropBox;
            if (lockboxes != null)
            {
                switch (lockboxes.PrefabName)
                {
                    case DROPBOX_PREFAB:
                        entity.transform.localPosition = new Vector3(0, 0, 0);
                        entity.transform.localRotation = Quaternion.Euler(0, 0, 0);
                        break;
                }
            }

            // Lock VendingMachines
            if (entity is VendingMachine)
            {
                var vendingLock = entity as VendingMachine;
                vendingLock.isLockable = true;
                vendingLock.SendNetworkUpdateImmediate(true);
                return;
            }
            var vendingBoxes = (entity as BaseLock)?.GetParentEntity() as VendingMachine;
            if (vendingBoxes != null)
            {
                switch (vendingBoxes.PrefabName)
                {
                    case VENDING_PREFAB:
                        entity.transform.localPosition = new Vector3(0, 0, 0);
                        entity.transform.localRotation = Quaternion.Euler(0, 0, 0);
                        break;
                }
            }

            // Lock Coffins
            //if (entity.ShortPrefabName.Contains("coffin") == true)
            //{
            //    var coffinLock = entity as StorageContainer;
            //    coffinLock.isLockable = true;
            //    coffinLock.SendNetworkUpdate();
            //    coffinLock.SendNetworkUpdateImmediate(true);
            //    return;
            //}
            //var lockcoffins = (entity as BaseLock)?.GetParentEntity() as StorageContainer;
            //if (lockcoffins != null)
            //{
            //    if (lockcoffins.PrefabName == COFFIN_PREFAB)
            //    {
            //        entity.transform.localEulerAngles = new Vector3(0, 90, 90);
            //        entity.transform.localPosition = new Vector3(0.49f, -0.015f, -0.75f);
            //    }
            //}

            // Lock All Ovens
            if (entity is BaseOven)
            {
                var ovenLock = entity as BaseOven;
                ovenLock.isLockable = true;
                ovenLock.SendNetworkUpdate();
                ovenLock.SendNetworkUpdateImmediate(true);
                return;
            }
            var lockovens = (entity as BaseLock)?.GetParentEntity() as BaseOven;
            if (lockovens != null)
            {
                switch (lockovens.PrefabName)
                {
                    case FURNACE_PREFAB:
                        entity.transform.localPosition = new Vector3(-0.02f, 0.3f, 0.5f);
                        entity.transform.localRotation = Quaternion.Euler(0, 90, 0);
                        break;
                    case LARGE_FURNACE_PREFAB:
                        entity.transform.localPosition = new Vector3(0.65f, 1.25f, -0.65f);
                        entity.transform.localRotation = Quaternion.Euler(0, 45, 0);
                        break;
                    case REFINERY_PREFAB:
                        entity.transform.localPosition = new Vector3(-0.01f, 1.25f, -0.6f);
                        entity.transform.localRotation = Quaternion.Euler(0, 90, 0);
                        break;
                    case BBQ_PREFAB:
                        entity.transform.localPosition = new Vector3(0.3f, 0.75f, 0f);
                        entity.transform.localRotation = Quaternion.Euler(0, 0, 0);
                        break;
                }
            }
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Usage", "You do not have permission to use this command"},
                {"Success", "Refreshing is done"}
            }, this);
        }

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        private void Message(BasePlayer player, string key, params object[] args)
        {
            SendReply(player, Lang(key, player.UserIDString, args));
        }
    }
}
