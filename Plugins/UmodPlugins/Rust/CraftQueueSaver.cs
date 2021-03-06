﻿using Facepunch;
using Oxide.Core;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Craft Queue Saver", "Jake_Rich", "1.1.4")]
    [Description("Saves player crafting queues on disconnect and on server shutdown")]
    class CraftQueueSaver : RustPlugin
    {
        #region Variables

        public ConfigurationAccessor<CraftQueueDatabase> CraftDB;

        #endregion Variables

        #region Classes

        public class CraftQueueDatabase
        {
            public Dictionary<ulong, QueueData> queueData = new Dictionary<ulong, QueueData>();

            public void SaveQueue(BasePlayer player)
            {
                queueData[player.userID] = new QueueData(player);
            }

            public bool LoadQueue(BasePlayer player)
            {
                QueueData data;
                if (!queueData.TryGetValue(player.userID, out data))
                {
                    return false;
                }

                foreach (SerializedItemCraftTask item in data.CraftQueue)
                {
                    item.Deserialize(player);
                }
                queueData.Remove(player.userID);
                return true;
            }
        }

        public class QueueData
        {
            public List<SerializedItemCraftTask> CraftQueue = new List<SerializedItemCraftTask>();

            public QueueData(BasePlayer player)
            {
                CraftQueue = player.inventory.crafting.queue.Select(x => new SerializedItemCraftTask(x)).ToList();
            }

            public QueueData()
            {
            }
        }

        public class SerializedItemCraftTask
        {
            private List<Item> TakenItems;

            public int amount { get; set; }
            public int skinDefinition { get; set; }
            public int itemID { get; set; }
            public bool fromTempBlueprint { get; set; }

            public List<string> itemBytes { get; set; }

            public SerializedItemCraftTask(ItemCraftTask craftTask)
            {
                amount = craftTask.amount;
                skinDefinition = craftTask.skinID;
                itemID = craftTask.blueprint.targetItem.itemid;

                TakenItems = craftTask.takenItems;
                SerializeItems();
            }

            public SerializedItemCraftTask()
            {
            }

            private void SerializeItems()
            {
                itemBytes = TakenItems.Select(x =>
                System.Convert.ToBase64String(x.Save().ToProtoBytes())
                ).ToList();
            }

            public void Deserialize(BasePlayer player)
            {
                player.inventory.crafting.taskUID++;
                ItemCraftTask craftTask = Pool.Get<ItemCraftTask>();
                craftTask.blueprint = ItemManager.bpList.FirstOrDefault(x => x.targetItem.itemid == itemID);

                if (craftTask.blueprint == null)
                {
                    return;
                }

                if (TakenItems == null)
                {
                    DeserializeItems();
                }

                craftTask.takenItems = TakenItems;
                craftTask.endTime = 0f;
                craftTask.taskUID = player.inventory.crafting.taskUID++;
                craftTask.owner = player;
                craftTask.amount = amount;
                craftTask.skinID = skinDefinition;

                if (fromTempBlueprint)
                {
                    Item bpItem = ItemManager.CreateByName("blueprint");
                    bpItem.blueprintTarget = itemID;
                    craftTask.takenItems.Add(bpItem);
                    craftTask.conditionScale = 0.5f;
                }

                object obj = Interface.CallHook("OnItemCraft", new object[]
                {
                    craftTask,
                    player,
                    fromTempBlueprint
                });

                if (obj is bool)
                {
                    return;
                }

                player.inventory.crafting.queue.AddFirst(craftTask);

                if (craftTask.owner != null)
                {
                    craftTask.owner.Command("note.craft_add", new object[]
                    {
                        craftTask.taskUID,
                        craftTask.blueprint.targetItem.itemid,
                        amount,
                        craftTask.skinID
                    });
                }
            }

            private void DeserializeItems()
            {
                TakenItems = itemBytes.Select(x =>
                ItemManager.Load(ProtoBuf.Item.Deserialize(System.Convert.FromBase64String(x)), null, true)).ToList();
            }
        }

        public class ConfigurationAccessor<Type> where Type : class
        {
            #region Typed Configuration Accessors

            private Type GetTypedConfigurationModel(string storageName)
            {
                return Interface.Oxide.DataFileSystem.ReadObject<Type>(storageName);
            }

            private void SaveTypedConfigurationModel(string storageName, Type storageModel)
            {
                Interface.Oxide.DataFileSystem.WriteObject(storageName, storageModel);
            }

            #endregion Typed Configuration Accessors

            private string name { get; set; }
            public Type Instance { get; set; }

            public ConfigurationAccessor(string name)
            {
                this.name = name;
                Init();
                Reload();
            }

            public virtual void Init()
            {
            }

            public void Load()
            {
                Instance = GetTypedConfigurationModel(name);
            }

            public void Save()
            {
                SaveTypedConfigurationModel(name, Instance);
            }

            public void Reload()
            {
                Load(); // Need to load and save to init list
                Save();
                Load();
            }
        }

        #endregion Classes

        #region Hooks

        private void Init()
        {
            CraftDB = new ConfigurationAccessor<CraftQueueDatabase>("SavedCraftQueues_Database.json");
        }

        private void OnServerInitialized()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                LoadQueue(player);
            }
        }

        private void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                OnPlayerDisconnected(player);
            }

            CraftDB.Save();
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            LoadQueue(player);
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            SaveQueue(player);
        }

        private void OnPlayerDeath(BasePlayer player)
        {
            if (player.IsNpc || player.HasPlayerFlag(BasePlayer.PlayerFlags.Connected)) // Only for disconnected sleepers
            {
                return;
            }

            LoadQueue(player);
        }

        private void OnNewSave() // Clear craft queues on wipe
        {
            CraftDB.Instance = new CraftQueueDatabase();
            CraftDB.Save();
        }

        #endregion Hooks

        private void SaveQueue(BasePlayer player)
        {
            foreach (ItemCraftTask current in player.inventory.crafting.queue.ToList())
            {
                player.Command("note.craft_done", new object[] { current.taskUID, 0 });
            }

            if (player.inventory.crafting.queue.Count == 0) // Do not overwrite when crafting queue has already been saved and cleared
            {
                return;
            }

            CraftDB.Instance.SaveQueue(player);
            CraftDB.Save();
            player.inventory.crafting.queue.Clear();
        }

        private void LoadQueue(BasePlayer player)
        {
            CraftDB.Instance.LoadQueue(player);
            CraftDB.Save();
        }
    }
}