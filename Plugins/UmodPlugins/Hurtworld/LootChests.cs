using System;
using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Loot Chests", "Mr. Blue", "2.0.2")]
    [Description("Spawns Storage Chests with loot around the map")]

    class LootChests : HurtworldPlugin
    {
        #region Variables
        List<HNetworkView> chestList = new List<HNetworkView>();
        public Items items = new Items();
        private Timer spawnTimer = null;

        private bool showSpawnMessage, showDespawnMessage;
        private int chestSpawnCount, itemsPerChest;
        private float secondsTillDestroy;
        private List<string> locList;
        private NetworkInstantiateConfig prefab = null;

        private static string AdminPermission = "lootchests.admin";
        #endregion

        #region Classes
        public class Items
        {
            public List<LCItem> lcItems = new List<LCItem>();

            public static System.Random random = new System.Random();

            public static LCItem GetItem(List<LCItem> items)
            {
                int poolSize = 0;
                for (int i = 0; i < items.Count; i++)
                    poolSize += items[i].Chance;

                int randomNumber = random.Next(0, poolSize) + 1;

                int accumulatedProbability = 0;
                for (int i = 0; i < items.Count; i++)
                {
                    accumulatedProbability += items[i].Chance;
                    if (randomNumber <= accumulatedProbability)
                        return items[i];
                }
                return null;
            }
        }

        public class LCItem
        {
            public string Guid;
            public int Min, Max, Chance;
            public int Amount() => UnityEngine.Random.Range(Min, Max + 1);

            public LCItem(string guid, int min, int max, int chance)
            {
                Guid = guid;
                Min = min;
                Max = max;
                Chance = chance;
            }
        }
        #endregion

        #region Loading
        void Init()
        {
            permission.RegisterPermission(AdminPermission, this);
            items = Interface.Oxide.DataFileSystem.ReadObject<Items>("LootChests");

            showSpawnMessage = (bool)Config["ShowSpawnMessage"];
            showDespawnMessage = (bool)Config["ShowDespawnMessage"];
            locList = Config.Get<List<string>>("StartPoints");
            chestSpawnCount = Convert.ToInt32(Config["ChestSpawnCount"]);
            itemsPerChest = Convert.ToInt32(Config["ItemsPerChest"]);
            secondsTillDestroy = Convert.ToSingle(Config["SecondsTillDestroy"]);

            if (items.lcItems.Count == 0)
            {
                items.lcItems.Add(new LCItem("49a99af8b780d07489c5794c13fab84c", 5, 25, 100));
                items.lcItems.Add(new LCItem("2e718220fde28dd4d8ec5ef1c101a9e2", 1, 1, 500));
                SaveItemList();
            }
        }

        protected override void LoadDefaultConfig()
        {
            List<object> defaultLocations = new List<object>() { "-2800, 200, -1000, 20" };
            Config.Set("StartPoints", defaultLocations);
            Config.Set("ChestSpawnCount", 20);
            Config.Set("SecondsForSpawn", 7200);
            Config.Set("SecondsTillDestroy", 1800);
            Config.Set("ItemsPerChest", 1);
            Config.Set("ShowSpawnMessage", true);
            Config.Set("ShowDespawnMessage", true);
            Config.Set("PrefabName", "");
        }

        void OnServerInitialized()
        {
            timer.Once(15f, () => {
                GetPrefab();
                Puts("Started spawning");
                spawnTimer = timer.Every(Convert.ToSingle(Config["SecondsForSpawn"]), () =>
                {
                    if (showSpawnMessage)
                        Server.Broadcast(Msg("Spawned"));

                    SpawnsChest();

                    if (showDespawnMessage)
                        timer.Once(secondsTillDestroy, () => { Server.Broadcast(Msg("Despawned")); });
                });
             });
        }

        void Unload()
        {
            spawnTimer.Destroy();

            Puts("Cleaning up spawned objects");
            foreach (HNetworkView nwv in chestList)
                Singleton<HNetworkManager>.Instance.NetDestroy(nwv);
        }
        #endregion

        #region Localization
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string> {
                { "NoPermission", "<color=#ffa500>LootChests</color>: You dont have Permission to use this command! (lootchests.admin)" },
                { "Usage", "<color=#ffa500>LootChests</color>: Usage:\n<color=orange>/LootChests addspawn [radius]</color> - Add new chest spawn point\n<color=orange>/LootChests additem [minstack] [maxstack] [chance]</color> - Adds item from first hotbar slot" },
                { "AddNoItem", "<color=#ffa500>LootChests</color>: Could not find a item in your first hotbar slot!" },
                { "ItemAdded", "<color=#ffa500>LootChests</color>: <color=purple>{itemname}</color> has been added with min {minstack} and max {maxstack} items, with {chance} chance" },
                { "LocationAdded", "<color=#ffa500>LootChests</color>: Location added: {location} with radius: {radius}" },
                { "Spawned", "<color=#ffa500>LootChests</color>: Loot Chests have Spawned" },
                { "Despawned", "<color=#ffa500>LootChests</color>: Loot Chests have Despawned" },
                { "ChestSpawnError", "<color=#ffa500>LootChests</color>: Error: Unable to spawn the chest as it did not exist." }
            }, this);
        }

        string Msg(string msg, string SteamId = null) => lang.GetMessage(msg, this, SteamId);
        #endregion

        #region Data Handling
        private void SaveItemList()
        {
            Interface.Oxide.DataFileSystem.WriteObject("LootChests", items);
        }
        #endregion

        #region ChatCommands
        [ChatCommand("lootchests")]
        private void LootChestCommand(PlayerSession session, string command, string[] args)
        {
            string steamId = session.SteamId.ToString();
            if (!permission.UserHasPermission(steamId, AdminPermission))
            {
                Player.Message(session, Msg("NoPermission", steamId));
                return;
            }

            if (args.Length == 2 && args[0].ToLower() == "addspawn")
            {
                float radius;
                if (float.TryParse(args[1], out radius))
                {
                    var locList = Config.Get<List<string>>("StartPoints");
                    var ploc = session.WorldPlayerEntity.transform.position;
                    locList.Add(ploc.x + ", " + ploc.y + ", " + ploc.z + ", " + radius);
                    var location = ploc.x + ", " + ploc.y + ", " + ploc.z;
                    Puts(Msg("LocationAdded")
                        .Replace("{location}", location)
                        .Replace("{radius}", radius.ToString()));

                    Player.Message(session, Msg("LocationAdded", steamId)
                        .Replace("{location}", location)
                        .Replace("{radius}", radius.ToString()));

                    Config.Set("StartPoints", locList);
                    this.locList = locList;
                    SaveConfig();
                    return;
                }
            }

            if (args.Length == 4 && args[0].ToLower() == "additem")
            {
                int minStack = Convert.ToInt32(args[1]);
                int maxStack = Convert.ToInt32(args[2]);
                int chance = Convert.ToInt32(args[3]);

                var playerInventory = session.WorldPlayerEntity.GetComponent<Inventory>();
                ItemObject itemObject = playerInventory?.GetSlot(0);
                string guid = RuntimeHurtDB.Instance.GetGuid(itemObject?.Generator);
                if (guid != null)
                {
                    items.lcItems.Add(new LCItem(guid, minStack, maxStack, chance));
                    Player.Message(session, Msg("ItemAdded", steamId)
                        .Replace("{itemname}", itemObject.Generator.ToString())
                        .Replace("{minstack}", minStack.ToString())
                        .Replace("{maxstack}", maxStack.ToString())
                        .Replace("{chance}", chance.ToString()));

                }
                else
                    Player.Message(session, Msg("AddNoItem", steamId));

                SaveItemList();
                return;
            }
            Player.Message(session, Msg("Usage", steamId));
        }
        #endregion

        #region Spawning
        private void SpawnsChest()
        {
            foreach (string Loc in locList)
            {
                int fail = 0;
                int i = 0;
                string[] XYZ = Loc.ToString().Split(',');
                Vector3 position = new Vector3(Convert.ToSingle(XYZ[0]), Convert.ToSingle(XYZ[1]), Convert.ToSingle(XYZ[2]));
                float radius = Convert.ToSingle(XYZ[3]);
                while (i < chestSpawnCount)
                {
                    if (fail == 3)
                    {
                        Puts($"[FAIL] Loc: {XYZ[0]}, {XYZ[1]}, {XYZ[2]}, failed to spawn 3 times! Update this location or remove it.");
                        return;
                    }
                    Vector3 randposition = new Vector3((position.x + UnityEngine.Random.Range(-radius, radius)), (500f), (position.z + UnityEngine.Random.Range(-radius, radius)));
                    int layerMask = ~((1 << 10) | (1 << 11) | (1 << 12) | (1 << 29) | (1 << 24));

                    RaycastHit hitInfo;
                    if (Physics.Raycast(randposition, Vector3.down, out hitInfo, Mathf.Infinity, layerMask))
                    {
                        GameObject Obj = Singleton<HNetworkManager>.Instance.NetInstantiate(uLink.NetworkPlayer.server, (prefab ?? Singleton<RuntimeAssetManager>.Instance.RefList.LootCachePrefab), hitInfo.point, Quaternion.identity, GameManager.GetSceneTime());
                        if (Obj != null)
                        {
                            Inventory inv = Obj.GetComponent<Inventory>() as Inventory;
                            if (inv.Capacity < itemsPerChest)
                                inv.SetCapacity(itemsPerChest + 1);
                            HNetworkView nwv = Obj.HNetworkView();
                            chestList.Add(nwv);
                            GiveItems(inv);
                            Destroy(nwv);
                        }
                        else
                        {
                            Server.Broadcast(Msg("ChestSpawnError"));
                            return;
                        }
                        i++;
                    }
                    else
                    {
                        fail++;
                    }
                }
            }
        }

        private void GetPrefab()
        {
            string PrefabName = (string)Config["PrefabName"];
            if (!string.IsNullOrEmpty(PrefabName))
            {
                NetworkInstantiateConfig[] networkInstantiateConfigs = Resources.FindObjectsOfTypeAll<NetworkInstantiateConfig>();
                foreach (NetworkInstantiateConfig networkInstantiateConfig in networkInstantiateConfigs)
                {
                    if(networkInstantiateConfig != null && networkInstantiateConfig.name.ToLower() == PrefabName.ToLower())
                    {
                        prefab = networkInstantiateConfig;
                        return;
                    }
                }
            }
        }

        private void GiveItems(Inventory inv)
        {
            int num = 0;
            GlobalItemManager globalItemManager = Singleton<GlobalItemManager>.Instance;
            while (num < itemsPerChest)
            {
                LCItem item = Items.GetItem(items.lcItems);
                ItemGeneratorAsset generator = RuntimeHurtDB.Instance.GetObjectByGuid<ItemGeneratorAsset>((string)item.Guid);
                globalItemManager.GiveItem(generator, item.Amount(), inv);
                num++;
            }
        }

        private void Destroy(HNetworkView nwv)
        {
            timer.Once(secondsTillDestroy, () =>
            {
                if (nwv != null)
                {
                    Singleton<HNetworkManager>.Instance.NetDestroy(nwv);
                    chestList.Remove(nwv);
                }
            });
        }
        #endregion
    }
}