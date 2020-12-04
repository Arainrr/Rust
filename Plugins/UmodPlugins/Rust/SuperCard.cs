﻿using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using System.Linq;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Super Card", "Mevent#4546", "1.0.4")]
    [Description("Open all doors")]
    public class SuperCard : CovalencePlugin
    {
        #region Config
        private ConfigData config;

        private class ConfigData
        {
            [JsonProperty("Command")]
            public string cmd;

            [JsonProperty("Item settings")]
            public ItemConfig item;

            [JsonProperty("Enable spawn?")]
            public bool enableSpawn;

            [JsonProperty("Drop Settings")]
            public List<DropInfo> drop;
        }

        public class ItemConfig
        {
            public string DisplayName;

            public string ShortName;

            public ulong SkinID;

            [JsonProperty("Breaking the item (1 - standard)")]
            public float LoseCondition;

            public Item ToItem()
            {
                var newItem = ItemManager.CreateByName(ShortName, 1, SkinID);
                newItem.name = DisplayName;

                return newItem;
            }
        }

        public class DropInfo
        {
            [JsonProperty("Object prefab name")]
            public string PrefabName;

            [JsonProperty("Minimum item to drop")]
            public int MinAmount;

            [JsonProperty("Maximum item to drop")]
            public int MaxAmount;

            [JsonProperty("Item Drop Chance")]
            public int DropChance;
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                cmd = "supercard.give",
                item = new ItemConfig
                {
                    DisplayName = "Super Card",
                    ShortName = "keycard_red",
                    SkinID = 1988408422,
                    LoseCondition = 1f
                },
                enableSpawn = true,
                drop = new List<DropInfo>
                {
                    new DropInfo
                    {
                        PrefabName = "assets/bundled/prefabs/radtown/crate_normal.prefab",
                        MinAmount = 1,
                        MaxAmount = 1,
                        DropChance = 50
                    },
                    new DropInfo
                    {
                        PrefabName = "assets/bundled/prefabs/radtown/loot_barrel_2.prefab",
                        MinAmount = 1,
                        MaxAmount = 1,
                        DropChance = 5
                    },
                    new DropInfo
                    {
                        PrefabName = "assets/bundled/prefabs/radtown/loot_barrel_1.prefab",
                        MinAmount = 1,
                        MaxAmount = 1,
                        DropChance = 5
                    }
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigData>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintError("Configuration file is corrupt(or not exists), creating new one!");
            config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
        #endregion

        #region Hooks
        private void Init()
        {
            if (!config.enableSpawn) Unsubscribe(nameof(OnLootSpawn));

            AddCovalenceCommand(config.cmd, nameof(Cmd));
        }

        private object OnCardSwipe(CardReader cardReader, Keycard card, BasePlayer player)
        {
            if (cardReader == null || card == null || player == null || card.skinID != config.item.SkinID) return null;

            var obj = card.GetItem();
            if (obj == null || (double)obj.conditionNormalized <= 0.0) return null;

            cardReader.Invoke(new Action(cardReader.GrantCard), 0.5f);
            obj.LoseCondition(config.item.LoseCondition);

            return true;
        }

        private void OnLootSpawn(LootContainer container)
        {
            if (container == null || !config.drop.Exists(x => x.PrefabName.Contains(container.PrefabName))) return;

            var customItem = config.drop.First(x => x.PrefabName.Contains(container.PrefabName));

            if (UnityEngine.Random.Range(0, 100) <= customItem.DropChance)
            {
                InvokeHandler.Instance.Invoke(() =>
                {
                    var count = UnityEngine.Random.Range(customItem.MinAmount, customItem.MaxAmount + 1);

                    if (container.inventory.capacity <= container.inventory.itemList.Count)
                    {
                        container.inventory.capacity = container.inventory.itemList.Count + count;
                    }

                    for (int i = 0; i < count; i++)
                    {
                        var item = config?.item?.ToItem();
                        if (item == null)
                        {
                            PrintError(lang.GetMessage("ErrorCreating", this));
                            break;
                        }

                        item.MoveToContainer(container.inventory);
                    }

                }, 0.21f);
            }
        }
        #endregion

        #region Commands
        private void Cmd(IPlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
            {
                player.Reply(lang.GetMessage("NoPermission", this, player.Id));
                return;
            }

            if (args.Length == 0)
            {
                player.Reply(string.Format(lang.GetMessage("Syntax", this, player.Id), config.cmd));
                return;
            }

            var target = players.FindPlayer(args[0])?.Object as BasePlayer;
            if (target == null)
            {
                player.Reply(string.Format(lang.GetMessage("NotFound", this, player.Id), args[0]));
                return;
            }

            var item = config?.item?.ToItem();
            if (item == null)
            {
                player.Reply(string.Format(lang.GetMessage("ErrorCreating", this, player.Id), args[0]));
                return;
            }

            target.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
        }
        #endregion

        #region Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NotFound"] = "We can't find player with that name/ID! {0}",
                ["ErrorCreating"] = "We couldn't create an item",
                ["Syntax"] = "Syntax: /{0} name/steamid",
                ["NoPermission"] = "You don't have permission to use this command!"
            }, this);
        }
        #endregion
    }
}