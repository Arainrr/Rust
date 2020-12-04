using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CompanionServer.Handlers;
using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using UnityEngine;

/*
    Fixed noclip not always being set when connecting to server
*/

namespace Oxide.Plugins
{
    [Info("Underworld", "nivex", "1.0.7")]
    [Description("Teleports admins/developer under the world when they disconnect.")]
    class Underworld : RustPlugin
    {
        [PluginReference] private Plugin Vanish;

        private const string permBlocked = "underworld.blocked";
        private const string permName = "underworld.use";
        private StoredData storedData = new StoredData();
        private DynamicConfigFile dataFile;
        private bool newSave;
        private readonly List<BasePlayer> saved = new List<BasePlayer>();
        private readonly List<BasePlayer> drowning = new List<BasePlayer>();

        public class StoredData
        {
            public Dictionary<string, UserInfo> Users = new Dictionary<string, UserInfo>();
        }

        public class UserInfo
        {
            public string Home { get; set; } = Vector3.zero.ToString();
            public bool WakeOnLand { get; set; } = true;
            public bool SaveInventory { get; set; } = true;
            public bool AutoNoClip { get; set; } = true;
            public List<UnderworldItem> Items { get; set; } = new List<UnderworldItem>();
        }

        public class UnderworldItem
        {
            public string container { get; set; } = "main";
            public string shortname { get; set; }
            public int itemid { get; set; }
            public ulong skinID { get; set; }
            public int amount { get; set; }
            public float condition { get; set; }
            public float maxCondition { get; set; }
            public int position { get; set; } = -1;
            public float fuel { get; set; }
            public int keyCode { get; set; }
            public int ammo { get; set; }
            public string ammoTypeShortname { get; set; }
            public string fogImages { get; set; }
            public string paintImages { get; set; }
            public List<UnderworldMod> contents { get; set; }

            public UnderworldItem() { }

            public UnderworldItem(string container, Item item)
            {
                if (item == null)
                    return;

                this.container = container;
                shortname = ItemManager.FindItemDefinition(item.info.shortname).shortname;
                itemid = item.info.itemid;
                skinID = item.skin;
                amount = item.amount;
                condition = item.condition;
                maxCondition = item.maxCondition;
                position = item.position;
                fuel = item.fuel;
                keyCode = item.instanceData?.dataInt ?? 0;
                ammo = item?.GetHeldEntity()?.GetComponent<BaseProjectile>()?.primaryMagazine?.contents ?? 0;
                ammoTypeShortname = item.GetHeldEntity()?.GetComponent<BaseProjectile>()?.primaryMagazine?.ammoType?.shortname ?? null;

                /*var mapEntity = item.GetHeldEntity() as MapEntity;

                if (mapEntity != null)
                {
                    fogImages = JsonConvert.SerializeObject(mapEntity.fogImages);
                    paintImages = JsonConvert.SerializeObject(mapEntity.paintImages);
                }*/

                if (item.contents?.itemList?.Count > 0)
                {
                    contents = new List<UnderworldMod>();

                    foreach (var mod in item.contents.itemList)
                    {
                        contents.Add(new UnderworldMod
                        {
                            shortname = mod.info.shortname,
                            amount = mod.amount,
                            condition = mod.condition,
                            maxCondition = mod.maxCondition,
                            itemid = mod.info.itemid
                        });
                    }
                }
            }
        }

        public class UnderworldMod
        {
            public string shortname { get; set; }
            public int amount { get; set; }
            public float condition { get; set; }
            public float maxCondition { get; set; }
            public int itemid { get; set; }
        }

        private void OnNewSave()
        {
            newSave = true;
        }

        private void Init()
        {
            Unsubscribe(nameof(OnPlayerDisconnected));
            Unsubscribe(nameof(OnPlayerConnected));
            Unsubscribe(nameof(OnPlayerSleepEnded));
            Unsubscribe(nameof(OnPlayerRespawned));
        }

        private void Loaded()
        {
            permission.RegisterPermission(permBlocked, this);
            permission.RegisterPermission(permName, this);
            dataFile = Interface.Oxide.DataFileSystem.GetFile(Name);

            try
            {
                storedData = dataFile.ReadObject<StoredData>();
            }
            catch { }

            if (storedData == null)
                storedData = new StoredData();

            LoadVariables();
        }

        private void OnServerInitialized()
        {
            Subscribe(nameof(OnPlayerDisconnected));
            Subscribe(nameof(OnPlayerConnected));
            Subscribe(nameof(OnPlayerSleepEnded));
            Subscribe(nameof(OnPlayerRespawned));

            if (wipeSaves && GetSave())
            {
                foreach (var entry in storedData.Users)
                {
                    entry.Value.Items.Clear();
                }

                newSave = false;
                SaveData();
            }

            foreach (var p in BasePlayer.activePlayerList)
            {
                OnPlayerConnected(p);
            }
        }

        private bool GetSave()
        {
            if (newSave || BuildingManager.server.buildingDictionary.Count == 0)
            {
                return true;
            }

            return false;
        }

        private void Unload()
        {
            SaveData();
        }

        private void OnEntityTakeDamage(BasePlayer player, HitInfo hitInfo)
        {
            if (hitInfo != null && saved.Contains(player))
            {
                hitInfo.damageTypes = new Rust.DamageTypeList();
            }
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player.IsDead())
            {
                return;
            }

            var user = GetUser(player);

            if (user == null)
            {
                return;
            }

            var userHome = user.Home.ToVector3();
            var position = userHome == Vector3.zero ? defaultPos : userHome;

            if (position == Vector3.zero)
            {
                position = new Vector3(player.transform.position.x, TerrainMeta.HeightMap.GetHeight(player.transform.position) - 5f, player.transform.position.z);
            }

            player.Teleport(position);
            SaveInventory(player, user);
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            var user = GetUser(player);

            if (user == null)
            {
                return;
            }

            if (!saved.Contains(player))
            {
                saved.Add(player); // prevent antihack kick for clipping in terrain, also prevent damage from drowning

                if (saved.Count == 1)
                {
                    Subscribe(nameof(OnEntityTakeDamage));
                }
            }

            if (player.IsDead())
            {
                return;
            }

            if (player.IsSleeping())
            {
                StopDrowning(player);
                timer.Once(0.5f, () => OnPlayerConnected(player));
                return;
            }

            if (user.WakeOnLand)
            {
                float y = TerrainMeta.HeightMap.GetHeight(player.transform.position);
                player.Teleport(new Vector3(player.transform.position.x, y + 2f, player.transform.position.z));
                player.SendNetworkUpdateImmediate();
            }
            
            if (user.AutoNoClip)
            {
                player.Invoke(() =>
                {
                    if (!player.IsFlying)
                    {
                        player.SendConsoleCommand("noclip");
                    }
                }, 0.1f);
            }

            Disappear(player);

            timer.Once(5f, () =>
            {
                saved.Remove(player);
                drowning.Remove(player);
                if (saved.Count == 0) Unsubscribe(nameof(OnEntityTakeDamage));
            });
        }

        private void Disappear(BasePlayer player)
        {
            if (autoVanish && Vanish != null && Vanish.IsLoaded)
            {
                bool isInvisible = Vanish.Call<bool>("IsInvisible", player);
                if (!isInvisible) Vanish.Call("Disappear", player);
                else Vanish.Call("VanishGui", player);
            }
        }

        private void OnPlayerSleepEnded(BasePlayer player)
        {
            if (!player || !player.IsConnected)
            {
                return;
            }

            var user = GetUser(player);

            if (user == null)
            {
                return;
            }

            if (maxHHT)
            {
                player.health = 100f;
                player.metabolism.hydration.value = player.metabolism.hydration.max;
                player.metabolism.calories.value = player.metabolism.calories.max;
            }

            if (allowSaveInventory && user.SaveInventory && user.Items.Count > 0)
            {
                if (player.inventory.AllItems().Length == 2)
                {
                    if (player.inventory.GetAmount(ItemManager.FindItemDefinition("rock").itemid) == 1)
                    {
                        if (player.inventory.GetAmount(ItemManager.FindItemDefinition("torch").itemid) == 1)
                        {
                            player.inventory.Strip();
                        }
                    }
                }

                if (user.Items.Any(item => item.amount > 0))
                {
                    var list = new List<UnderworldItem>(user.Items);

                    foreach (var uwi in list)
                    {
                        RestoreItem(player, uwi);
                    }

                    list.Clear();
                }

                user.Items.Clear();
                SaveData();
            }

            Disappear(player);
        }

        private void OnPlayerRespawned(BasePlayer player)
        {
            if (!player || !player.IsConnected)
            {
                return;
            }

            var user = GetUser(player);

            if (user == null)
            {
                return;
            }

            Disappear(player);

            if (user.AutoNoClip)
            {
                player.Invoke(() =>
                {
                    if (!player.IsFlying)
                    {
                        player.SendConsoleCommand("noclip");
                    }
                }, 0.1f);
            }
        }

        [ChatCommand("uw")]
        private void cmdUnderworld(BasePlayer player, string command, string[] args)
        {
            if (!IsAllowed(player))
            {
                Player.Message(player, msg("NoPermission", player.UserIDString));
                return;
            }

            var user = GetUser(player);

            if (user == null)
            {
                Player.Message(player, msg("NoPermission", player.UserIDString));
                return;
            }

            if (args.Length >= 1)
            {
                switch (args[0].ToLower())
                {
                    case "tp":
                        {
                            player.Teleport(user.Home.ToVector3());
                            break;
                        }
                    case "save":
                        {
                            if (!allowSaveInventory)
                                return;

                            user.SaveInventory = !user.SaveInventory;
                            Player.Message(player, msg(user.SaveInventory ? "SavingInventory" : "NotSavingInventory", player.UserIDString));
                            SaveData();
                        }
                        return;
                    case "set":
                        {
                            var position = player.transform.position;

                            if (args.Length == 4)
                            {
                                if (args[1].All(char.IsDigit) && args[2].All(char.IsDigit) && args[3].All(char.IsDigit))
                                {
                                    var customPos = new Vector3(float.Parse(args[1]), 0f, float.Parse(args[3]));

                                    if (Vector3.Distance(customPos, Vector3.zero) <= TerrainMeta.Size.x / 1.5f)
                                    {
                                        customPos.y = float.Parse(args[2]);

                                        if (customPos.y > -100f && customPos.y < 4400f)
                                            position = customPos;
                                        else
                                            Player.Message(player, msg("OutOfBounds", player.UserIDString));
                                    }
                                    else
                                        Player.Message(player, msg("OutOfBounds", player.UserIDString));
                                }
                                else
                                    Player.Message(player, msg("Help1", player.UserIDString, FormatPosition(user.Home.ToVector3())));
                            }

                            user.Home = position.ToString();
                            Player.Message(player, msg("PositionAdded", player.UserIDString, FormatPosition(position)));
                            SaveData();
                        }
                        return;
                    case "reset":
                        {
                            user.Home = Vector3.zero.ToString();

                            if (defaultPos != Vector3.zero)
                            {
                                user.Home = defaultPos.ToString();
                                Player.Message(player, msg("PositionRemoved2", player.UserIDString, user.Home));
                            }
                            else
                                Player.Message(player, msg("PositionRemoved1", player.UserIDString));

                            SaveData();
                        }
                        return;
                    case "wakeup":
                        {
                            user.WakeOnLand = !user.WakeOnLand;
                            Player.Message(player, msg(user.WakeOnLand ? "PlayerWakeUp" : "PlayerWakeUpReset", player.UserIDString));
                            SaveData();
                        }
                        return;
                    case "noclip":
                        {
                            user.AutoNoClip = !user.AutoNoClip;
                            Player.Message(player, msg(user.AutoNoClip ? "PlayerNoClipEnabled" : "PlayerNoClipDisabled", player.UserIDString));
                            SaveData();
                        }
                        return;
                    case "g":
                    case "ground":
                        {
                            player.Teleport(new Vector3(player.transform.position.x, TerrainMeta.HeightMap.GetHeight(player.transform.position) + 1f, player.transform.position.z));
                        }
                        return;
                }
            }

            string homePos = FormatPosition(user.Home.ToVector3() == Vector3.zero ? defaultPos : user.Home.ToVector3());

            Player.Message(player, msg("Help0", player.UserIDString, user.SaveInventory && allowSaveInventory));
            Player.Message(player, msg("Help1", player.UserIDString, homePos));
            Player.Message(player, msg("Help2", player.UserIDString));
            Player.Message(player, msg("Help3", player.UserIDString, user.WakeOnLand));
            Player.Message(player, msg("Help4", player.UserIDString, user.AutoNoClip));
            Player.Message(player, msg("Help5", player.UserIDString));
        }

        public void StopDrowning(BasePlayer player)
        {
            if (!player.IsConnected || drowning.Contains(player) || (!player.IsAdmin && !DeveloperList.Contains(player.userID)))
            {
                return;
            }

            if (player.transform.position.y < TerrainMeta.HeightMap.GetHeight(player.transform.position) || player.IsHeadUnderwater())
            {
                if (!player.IsFlying)
                {
                    player.SendConsoleCommand("noclip");
                }

                player.metabolism.oxygen.min = 1;
                player.metabolism.oxygen.value = 1;
                player.metabolism.temperature.min = 32;
                player.metabolism.temperature.max = 32;
                player.metabolism.temperature.value = 32;
                drowning.Add(player);
            }
        }

        private UserInfo GetUser(BasePlayer player)
        {
            if (!player || !player.IsConnected || !IsAllowed(player) || permission.UserHasPermission(player.UserIDString, permBlocked))
                return null;

            if (!storedData.Users.ContainsKey(player.UserIDString))
                storedData.Users.Add(player.UserIDString, new UserInfo());

            return storedData.Users[player.UserIDString];
        }

        public string FormatPosition(Vector3 position)
        {
            string x = position.x.ToString("N2");
            string y = position.y.ToString("N2");
            string z = position.z.ToString("N2");

            return $"{x} {y} {z}";
        }

        private void SaveData()
        {
            if (dataFile != null && storedData != null)
            {
                dataFile.WriteObject(storedData);
            }
        }

        private void SaveInventory(BasePlayer player, UserInfo user)
        {
            if (!allowSaveInventory || !user.SaveInventory)
                return;

            if (player.inventory.AllItems().Length == 0)
            {
                user.Items.Clear();
                SaveData();
                return;
            }

            var items = new List<UnderworldItem>();
            var list = new List<Item>(player.inventory.containerWear.itemList);
            list.RemoveAll(item => Blacklisted(item));

            foreach (Item item in list)
            {
                items.Add(new UnderworldItem("wear", item));
                item.Remove();
            }

            list = new List<Item>(player.inventory.containerMain.itemList);
            list.RemoveAll(item => Blacklisted(item));

            foreach (Item item in list)
            {
                items.Add(new UnderworldItem("main", item));
                item.Remove();
            }

            list = new List<Item>(player.inventory.containerBelt.itemList);
            list.RemoveAll(item => Blacklisted(item));

            foreach (Item item in list)
            {
                items.Add(new UnderworldItem("belt", item));
                item.Remove();
            }

            list.Clear();

            if (items.Count == 0)
            {
                return;
            }

            ItemManager.DoRemoves();
            user.Items.Clear();
            user.Items.AddRange(items);
            SaveData();
        }

        private void RestoreItem(BasePlayer player, UnderworldItem uwi)
        {
            if (uwi.itemid == 0 || uwi.amount < 1 || string.IsNullOrEmpty(uwi.container))
                return;

            Item item = ItemManager.CreateByItemID(uwi.itemid, uwi.amount, uwi.skinID);

            if (item == null)
                return;

            if (item.hasCondition)
            {
                item.maxCondition = uwi.maxCondition; // restore max condition after repairs
                item.condition = uwi.condition; // repair last known condition
            }

            item.fuel = uwi.fuel;

            var heldEntity = item.GetHeldEntity();

            if (heldEntity != null)
            {
                if (item.skin != 0)
                    heldEntity.skinID = item.skin;

                var weapon = heldEntity as BaseProjectile;

                if (weapon != null)
                {
                    if (!string.IsNullOrEmpty(uwi.ammoTypeShortname))
                    {
                        weapon.primaryMagazine.ammoType = ItemManager.FindItemDefinition(uwi.ammoTypeShortname);
                    }

                    weapon.primaryMagazine.contents = 0; // unload the old ammo // TODO: I dont think this is bugged anymore :p
                    weapon.SendNetworkUpdateImmediate(false); // update
                    weapon.primaryMagazine.contents = uwi.ammo; // load new ammo
                    weapon.SendNetworkUpdateImmediate(false); // update
                }
            }

            if (uwi.contents != null)
            {
                foreach (var uwm in uwi.contents)
                {
                    Item mod = ItemManager.CreateByItemID(uwm.itemid, 1);

                    if (mod == null)
                        continue;

                    if (mod.hasCondition)
                    {
                        mod.maxCondition = uwm.maxCondition; // restore max condition after repairs
                        mod.condition = uwm.condition; // repair last known condition
                    }

                    item.contents.AddItem(mod.info, Math.Max(uwm.amount, 1)); // restore attachments / water amount
                }
            }

            if (uwi.keyCode != 0) // restore key data
            {
                item.instanceData = Pool.Get<ProtoBuf.Item.InstanceData>();
                item.instanceData.ShouldPool = false;
                item.instanceData.dataInt = uwi.keyCode;
            }

            /*if (!string.IsNullOrEmpty(uwi.fogImages) && !string.IsNullOrEmpty(uwi.paintImages))
            {
                var mapEntity = item.GetHeldEntity() as MapEntity;

                if (mapEntity != null)
                {
                    mapEntity.SetOwnerPlayer(player);
                    mapEntity.fogImages = JsonConvert.DeserializeObject<uint[]>(uwi.fogImages);
                    mapEntity.paintImages = JsonConvert.DeserializeObject<uint[]>(uwi.paintImages);
                }
            }*/

            item.MarkDirty();

            var container = uwi.container == "belt" ? player.inventory.containerBelt : uwi.container == "wear" ? player.inventory.containerWear : player.inventory.containerMain;

            if (!item.MoveToContainer(container, uwi.position, true))
            {
                if (!item.MoveToContainer(player.inventory.containerMain, -1, true))
                {
                    item.Remove();
                }
            }
        }

        private bool IsAllowed(BasePlayer player)
        {
            return player != null && (player.IsAdmin || DeveloperList.Contains(player.userID) || HasPermission(player) || player.net?.connection?.authLevel > 0u);
        }

        private bool Blacklisted(Item item)
        {
            return Blacklist.Contains(item.info.shortname) || Blacklist.Contains(item.info.itemid.ToString()) || item.info.shortname == "map";
        }

        private bool HasPermission(BasePlayer player)
        {
            return player != null && player.IPlayer.HasPermission(permName);
        }

        #region Config

        private bool Changed;
        private Vector3 defaultPos;
        private bool allowSaveInventory;
        private bool maxHHT;
        private bool autoVanish;
        private List<string> Blacklist = new List<string>();
        private bool wipeSaves;

        private List<object> DefaultBlacklist
        {
            get
            {
                return new List<object>
                {
                    "2080339268",
                    "can.tuna.empty"
                };
            }
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["PositionAdded"] = "You will now teleport to <color=yellow>{0}</color> on disconnect.",
                ["PositionRemoved1"] = "You will now teleport under ground on disconnect.",
                ["PositionRemoved2"] = "You will now teleport to <color=yellow>{0}</color> on disconnect.",
                ["PlayerWakeUp"] = "You will now teleport above ground when you wake up.",
                ["PlayerWakeUpReset"] = "You will no longer teleport above ground when you wake up.",
                ["PlayerNoClipEnabled"] = "You will now automatically be noclipped on reconnect.",
                ["PlayerNoClipDisabled"] = "You will no longer be noclipped on reconnect.",
                ["SavingInventory"] = "Your inventory will be saved and stripped on disconnect, and restored when you wake up.",
                ["NotSavingInventory"] = "Your inventory will no longer be saved.",
                ["Help0"] = "/uw save - toggles saving inventory (enabled: {0})",
                ["Help1"] = "/uw set <x y z> - sets your log out position. can specify coordinates <color=yellow>{0}</color>",
                ["Help2"] = "/uw reset - resets your log out position to be underground unless a position is configured in the config file",
                ["Help3"] = "/uw wakeup - toggle waking up on land (enabled: {0})",
                ["Help4"] = "/uw noclip - toggle auto noclip on reconnect (enabled: {0})",
                ["Help5"] = "/uw g - teleport to the ground",
                ["OutOfBounds"] = "The specified coordinates are not within the allowed boundaries of the map.",
                ["NoPermission"] = "You do not have permission to use this command."
            }, this);
        }

        private void LoadVariables()
        {
            maxHHT = Convert.ToBoolean(GetConfig("Settings", "Set Health, Hunger and Thirst to Max", false));
            defaultPos = GetConfig("Settings", "Default Teleport To Position On Disconnect", "(0, 0, 0)").ToString().ToVector3();
            allowSaveInventory = Convert.ToBoolean(GetConfig("Settings", "Allow Save And Strip Admin Inventory On Disconnect", true));
            Blacklist = (GetConfig("Settings", "Blacklist", DefaultBlacklist) as List<object>).Where(o => o != null && o.ToString().Length > 0).Cast<string>().ToList();
            autoVanish = Convert.ToBoolean(GetConfig("Settings", "Auto Vanish On Connect", true));
            wipeSaves = Convert.ToBoolean(GetConfig("Settings", "Wipe Saved Inventories On Map Wipe", false));
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file");
            Config.Clear();
            LoadVariables();
        }

        private object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }

            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }

            return value;
        }

        private string msg(string key, string id = null, params object[] args)
        {
            string message = id == null ? RemoveFormatting(lang.GetMessage(key, this, id)) : lang.GetMessage(key, this, id);

            return args.Length > 0 ? string.Format(message, args) : message;
        }

        private string RemoveFormatting(string source)
        {
            return source.Contains(">") ? Regex.Replace(source, "<.*?>", string.Empty) : source;
        }

        #endregion
    }
}