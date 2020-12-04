using System;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Rad Town Loot", "Krungh Crow", "1.1.3")]
    [Description("Return of Radanimals with animal settings")]

    #region Changelogs and ToDo
    /**********************************************************************
    * 
    * v1.1.2 : Changed spawnchance from 0-1 to 0-100 (to make it more clear)
    * v1.1.3 : Added check if initiator is player or npc
    * 
    **********************************************************************/
    #endregion

    class RadTownLoot : RustPlugin
    {
        private Dictionary<string, List<ulong>> Skins { get; set; } = new Dictionary<string, List<ulong>>();

        const string prefix = "[<color=green>RadTownLoot</color>] ";
        
        #region Configuration

        private static Configuration config;
        private bool configLoaded;

        private class Configuration
        {
            [JsonProperty(PropertyName = "Bear settings")]
            public SettingsBearSpawns BearSpawns = new SettingsBearSpawns();

            [JsonProperty(PropertyName = "Wolf settings")]
            public SettingsWolfSpawns WolfSpawns = new SettingsWolfSpawns();

            [JsonProperty(PropertyName = "Chat Steam64ID")]
            public ulong ChatIcon { get; set; }

            [JsonProperty(PropertyName = "Use Random Skins")]
            public bool RandomSkins { get; set; } = true;

            [JsonProperty(PropertyName = "Spawn Min Amount Items")]
            public int MinAmount { get; set; } = 2;

            [JsonProperty(PropertyName = "Spawn Max Amount Items")]
            public int MaxAmount { get; set; } = 6;

            [JsonProperty(PropertyName = "Loot Table", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<TreasureItem> Loot { get; set; } = DefaultLoot;
        }

        class SettingsBearSpawns
        {
            [JsonProperty(PropertyName = "Change Bear stats on spawns")]
            public bool BearUse = false;
            [JsonProperty(PropertyName = "Show Bear spawns in Console")]
            public bool ConBear = false;
            [JsonProperty(PropertyName = "Bear Droprate 0-100")]
            public float ChanceOfCrate = 10.0f;
            [JsonProperty(PropertyName = "Minimum Health")]
            public int BearHealthmin = 400;
            [JsonProperty(PropertyName = "Maximum Health")]
            public int BearHealthmax = 400;
            [JsonProperty(PropertyName = "Minimum Strength (Att dmg)")]
            public int BearDamage = 40;
            [JsonProperty(PropertyName = "Maximum Strength (Att dmg")]
            public int BearDamageMax = 40;
            [JsonProperty(PropertyName = "Running Speed")]
            public float BearSpeed = 6f;
        }

        class SettingsWolfSpawns
        {
            [JsonProperty(PropertyName = "Change Wolf stats on spawns")]
            public bool WolfUse = false;
            [JsonProperty(PropertyName = "Show Wolf spawns in Console")]
            public bool ConWolf = false;
            [JsonProperty(PropertyName = "Wolf Droprate 0-100")]
            public float ChanceOfCrate2 = 10.0f;
            [JsonProperty(PropertyName = "Minimum Health")]
            public int WolfHealthmin = 150;
            [JsonProperty(PropertyName = "Maximum Health")]
            public int WolfHealthmax = 150;
            [JsonProperty(PropertyName = "Minimum Strength (Att dmg)")]
            public int WolfDamage = 23;
            [JsonProperty(PropertyName = "Maximum Strength (Att dmg)")]
            public int WolfDamageMax = 23;
            [JsonProperty(PropertyName = "Running Speed")]
            public float WolfSpeed = 6f;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<Configuration>();
            }
            catch (JsonException ex)
            {
                Puts(ex.Message);
                PrintError("Your configuration file contains a json error, shown above. Please fix this.");
                return;
            }
            catch (Exception ex)
            {
                Puts(ex.Message);
                LoadDefaultConfig();
            }

            if (config == null)
            {
                Puts("Config is null");
                LoadDefaultConfig();
            }

            configLoaded = true;

            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(config);

        protected override void LoadDefaultConfig() => config = new Configuration();

        private static List<TreasureItem> DefaultLoot
        {
            get
            {
                return new List<TreasureItem>
                {
                    new TreasureItem { shortname = "ammo.pistol", amount = 5, skin = 0, amountMin = 5 },
                    new TreasureItem { shortname = "ammo.pistol.fire", amount = 5, skin = 0, amountMin = 5 },
                    new TreasureItem { shortname = "ammo.pistol.hv", amount = 5, skin = 0, amountMin = 5 },
                    new TreasureItem { shortname = "ammo.rifle", amount = 5, skin = 0, amountMin = 5 },
                    new TreasureItem { shortname = "ammo.rifle.explosive", amount = 5, skin = 0, amountMin = 5 },
                    new TreasureItem { shortname = "ammo.rifle.hv", amount = 5, skin = 0, amountMin = 5 },
                    new TreasureItem { shortname = "ammo.rifle.incendiary", amount = 5, skin = 0, amountMin = 5 },
                    new TreasureItem { shortname = "ammo.rocket.basic.bp", amount = 1, skin = 0, amountMin = 1 },
                    new TreasureItem { shortname = "ammo.rocket.fire.bp", amount = 1, skin = 0, amountMin = 1 },
                    new TreasureItem { shortname = "ammo.rocket.hv.bp", amount = 1, skin = 0, amountMin = 1 },
                    new TreasureItem { shortname = "ammo.shotgun", amount = 12, skin = 0, amountMin = 8 },
                    new TreasureItem { shortname = "explosive.timed", amount = 1, skin = 0, amountMin = 1 },
                    new TreasureItem { shortname = "explosives", amount = 1, skin = 0, amountMin = 1 },
                    new TreasureItem { shortname = "pistol.m92", amount = 1, skin = 0, amountMin = 1 },
                    new TreasureItem { shortname = "rifle.ak.bp", amount = 1, skin = 0, amountMin = 1 },
                    new TreasureItem { shortname = "rifle.bolt.bp", amount = 1, skin = 0, amountMin = 1 },
                    new TreasureItem { shortname = "shotgun.spas12", amount = 1, skin = 0, amountMin = 1 },
                    new TreasureItem { shortname = "smg.2.bp", amount = 1, skin = 0, amountMin = 1 },
                    new TreasureItem { shortname = "smg.thompson.bp", amount = 1, skin = 0, amountMin = 1 },
                    new TreasureItem { shortname = "weapon.mod.8x.scope.bp", amount = 1, skin = 0, amountMin = 1 },
                    new TreasureItem { shortname = "weapon.mod.flashlight.bp", amount = 1, skin = 0, amountMin = 1 },
                    new TreasureItem { shortname = "weapon.mod.holosight.bp", amount = 1, skin = 0, amountMin = 1 },
                    new TreasureItem { shortname = "weapon.mod.lasersight.bp", amount = 1, skin = 0, amountMin = 1 },
                    new TreasureItem { shortname = "weapon.mod.silencer.bp", amount = 1, skin = 0, amountMin = 1 },
                    new TreasureItem { shortname = "weapon.mod.small.scope.bp", amount = 1, skin = 0, amountMin = 1 },
                    new TreasureItem { shortname = "grenade.f1.bp", amount = 1, skin = 0, amountMin = 1 },
                    new TreasureItem { shortname = "pickaxe", amount = 1, skin = 0, amountMin = 1 },
                    new TreasureItem { shortname = "hatchet", amount = 1, skin = 0, amountMin = 1 },
                    new TreasureItem { shortname = "can.beans", amount = 3, skin = 0, amountMin = 1 },
                    new TreasureItem { shortname = "can.tuna", amount = 3, skin = 0, amountMin = 1 },
                    new TreasureItem { shortname = "black.raspberries", amount = 5, skin = 0, amountMin = 3 },
                };
            }
        }

        public class TreasureItem
        {
            public string shortname { get; set; }
            public int amount { get; set; }
            public ulong skin { get; set; }
            public int amountMin { get; set; }
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["RadTownLoot"] = "A {0} dropped something!",
            }, this);
        }

        private string msg(string key, string id = null) => lang.GetMessage(key, this, id);

        #endregion

        #region Hooks

        private void Init()
        {
            Unsubscribe(nameof(OnEntityDeath));
            permission.RegisterPermission("radtownloot.use", this);
            permission.RegisterPermission("radtownloot.admin", this);
        }

        private void OnServerInitialized(bool isStartup)
        {
            if (!configLoaded || config.Loot.Count == 0)
            {
                return;
            }

            Subscribe(nameof(OnEntityDeath));
        }

        private void OnEntityDeath(Bear bear, HitInfo hitInfo)
        {
            if (hitInfo == null || bear == null || bear.transform == null || config.BearSpawns.ChanceOfCrate <= 0f || UnityEngine.Random.value > config.BearSpawns.ChanceOfCrate / 100)
            {
                return;
            }

            var player = hitInfo.Initiator as BasePlayer;

            if (!player.userID.IsSteamId() || !permission.UserHasPermission(player.UserIDString, "radtownloot.use"))
                {
                    return;
            }

            SpawnRadLoot(bear.transform.position + new Vector3(0f, 0.5f, 0f), bear.transform.rotation);
            {
                Player.Message(player, prefix + string.Format(msg("RadTownLoot", player.UserIDString), bear.ShortPrefabName.ToLower()), config.ChatIcon);
                LogToFile("RadTownKills" ,$"{DateTime.Now:h:mm:ss tt}] {player} killed a Radtown Bear and got some loot", this);
            }
        }

        private void OnEntityDeath(Wolf wolf, HitInfo hitInfo)
        {
            if (hitInfo == null || wolf == null || wolf.transform == null || config.WolfSpawns.ChanceOfCrate2 <= 0f || UnityEngine.Random.value > config.WolfSpawns.ChanceOfCrate2 / 100)
            {
                return;
            }

            var player = hitInfo.Initiator as BasePlayer;

            if (!player.userID.IsSteamId() || !permission.UserHasPermission(player.UserIDString, "radtownloot.use"))
            {
                return;
            }

            SpawnRadLoot(wolf.transform.position + new Vector3(0f, 0.5f, 0f), wolf.transform.rotation);
            {
                Player.Message(player, prefix + string.Format(msg("RadTownLoot", player.UserIDString), wolf.ShortPrefabName.ToLower()), config.ChatIcon);
                LogToFile("RadTownKills" ,$"{DateTime.Now:h:mm:ss tt}] {player} killed a Radtown Wolf and got some loot", this);
            }
        }

        private void OnEntitySpawned(Bear bear) //Check and executes if its a Bear entity on each spawn (server or player spawned)
        {
            if (bear == null)
            {
                return;
            }

            //Get random values
            int RandomHealth = UnityEngine.Random.Range(config.BearSpawns.BearHealthmin, config.BearSpawns.BearHealthmax);
            int RandomDamage = UnityEngine.Random.Range(config.BearSpawns.BearDamage, config.BearSpawns.BearDamageMax);

            if (config.BearSpawns.BearUse)//If set to true in CFG will Alter the Animal statistics
            {
                bear.InitializeHealth(RandomHealth, RandomHealth);
                bear.lifestate = BaseCombatEntity.LifeState.Alive;
                bear.AttackDamage = RandomDamage;
                bear.Stats.Speed = config.BearSpawns.BearSpeed;
                bear.Stats.TurnSpeed = config.BearSpawns.BearSpeed;

                if (config.BearSpawns.ConBear)//If set to True in CFG wil print to console
                {
                    Puts($"A Bear spawned with {RandomHealth} HP and {RandomDamage} Strength");
                }
            }
        }

        private void OnEntitySpawned(Wolf wolf)
        {
            if (wolf == null)
            {
                return;
            }

            int RandomHealth = UnityEngine.Random.Range(config.WolfSpawns.WolfHealthmin, config.WolfSpawns.WolfHealthmax);
            int RandomDamage = UnityEngine.Random.Range(config.WolfSpawns.WolfDamage, config.WolfSpawns.WolfDamageMax);

            if (config.WolfSpawns.WolfUse)
            {
                wolf.InitializeHealth(RandomHealth, RandomHealth);
                wolf.lifestate = BaseCombatEntity.LifeState.Alive;
                wolf.AttackDamage = RandomDamage;
                wolf.Stats.Speed = config.WolfSpawns.WolfSpeed;
                wolf.Stats.TurnSpeed = config.WolfSpawns.WolfSpeed;

                if (config.WolfSpawns.ConWolf)
                {
                    Puts($"A Wolf spawned with {RandomHealth} HP and {RandomDamage} Strength");
                }
            }
        }
        #endregion
        #region Commands

        [ChatCommand("rad")]
        void cmdrad(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "radtownloot.admin"))
            {
                Player.Message(player,  string.Format(msg("NoPermission", player.UserIDString)), config.ChatIcon);
                return;
            }

            if (args.Length == 0)
            {
                Player.Message(player,  string.Format(msg("InvalidInput", player.UserIDString)), config.ChatIcon);
                return;
            }

            {
                if (args[0].ToLower() == "animals")
                {
                    Player.Message(player, prefix +  string.Format(msg("\nVersion : <color=orange>v", player.UserIDString)) + this.Version.ToString() + "</color> By : <color=orange>" + this.Author.ToString()
                    + msg("</color>\nBear and wolf info")
                    + msg("\n")
                    //Bear Info
                    + msg("\n<color=orange>Bear</color> : Pop <color=purple>") + Bear.Population.ToString() + msg("</color> ")
                    + msg("Health <color=purple>") + config.BearSpawns.BearHealthmin.ToString() + msg("</color>/<color=purple>") + config.BearSpawns.BearHealthmax.ToString() + msg("</color>")
                    + msg(" Alive <color=purple>") + BaseNetworkable.serverEntities.OfType<Bear>().Count().ToString() + msg("</color> ")
                    //Wolf Info
                    + msg("\n<color=orange>Wolf</color> : Pop <color=purple>") + Wolf.Population.ToString() + msg("</color> ")
                    + msg("Health <color=purple>") + config.WolfSpawns.WolfHealthmin.ToString() + msg("</color>/<color=purple>") + config.WolfSpawns.WolfHealthmax.ToString() + msg("</color>")
                    + msg(" Alive <color=purple>") + BaseNetworkable.serverEntities.OfType<Wolf>().Count().ToString() + msg("</color> ")
                    , config.ChatIcon);
                }
            }
        }
        #endregion


        #region Methods

        private void SpawnRadLoot(Vector3 pos, Quaternion rot)
        {
            var backpack = GameManager.server.CreateEntity(StringPool.Get(1519640547), pos, rot, true) as DroppedItemContainer;

            if (backpack == null) return;

            backpack.inventory = new ItemContainer();
            backpack.inventory.ServerInitialize(null, 36);
            backpack.inventory.GiveUID();
            backpack.inventory.entityOwner = backpack;
            backpack.inventory.SetFlag(ItemContainer.Flag.NoItemInput, true);
            backpack.Spawn();

            SpawnLoot(backpack.inventory, config.Loot.ToList());
        }

        private void SpawnLoot(ItemContainer container, List<TreasureItem> loot)
        {
            int total = UnityEngine.Random.Range(Math.Min(loot.Count, config.MinAmount), Math.Min(loot.Count, config.MaxAmount));

            if (total == 0 || loot.Count == 0)
            {
                return;
            }

            container.capacity = total;
            ItemDefinition def;
            List<ulong> skins;
            TreasureItem lootItem;

            for (int j = 0; j < total; j++)
            {
                if (loot.Count == 0)
                {
                    break;
                }

                lootItem = loot.GetRandom();

                loot.Remove(lootItem);

                if (lootItem.amount <= 0)
                {
                    continue;
                }

                string shortname = lootItem.shortname;
                bool isBlueprint = shortname.EndsWith(".bp");

                if (isBlueprint)
                {
                    shortname = shortname.Replace(".bp", string.Empty);
                }

                def = ItemManager.FindItemDefinition(shortname);

                if (def == null)
                {
                    Puts("Invalid shortname: {0}", lootItem.shortname);
                    continue;
                }

                ulong skin = lootItem.skin;

                if (config.RandomSkins && skin == 0)
                {
                    skins = GetItemSkins(def);

                    if (skins.Count > 0)
                    {
                        skin = skins.GetRandom();
                    }
                }

                int amount = lootItem.amount;

                if (amount <= 0)
                {
                    continue;
                }

                if (lootItem.amountMin > 0 && lootItem.amountMin < lootItem.amount)
                {
                    amount = UnityEngine.Random.Range(lootItem.amountMin, lootItem.amount);
                }

                Item item;

                if (isBlueprint)
                {
                    item = ItemManager.CreateByItemID(-996920608, 1, 0);

                    if (item == null) continue;

                    item.blueprintTarget = def.itemid;
                    item.amount = amount;
                }
                else item = ItemManager.Create(def, amount, skin);

                if (!item.MoveToContainer(container, -1, false))
                {
                    item.Remove();
                }
            }
        }

        private List<ulong> GetItemSkins(ItemDefinition def)
        {
            List<ulong> skins;
            if (!Skins.TryGetValue(def.shortname, out skins))
            {
                Skins[def.shortname] = skins = ExtractItemSkins(def, skins);
            }

            return skins;
        }

        private List<ulong> ExtractItemSkins(ItemDefinition def, List<ulong> skins)
        {
            skins = new List<ulong>();

            foreach (var skin in def.skins)
            {
                skins.Add(Convert.ToUInt64(skin.id));
            }
            foreach (var asi in Rust.Workshop.Approved.All.Values)
            {
                if (!string.IsNullOrEmpty(asi.Skinnable.ItemName) && asi.Skinnable.ItemName == def.shortname)
                {
                    skins.Add(Convert.ToUInt64(asi.WorkshopdId));
                }
            }

            return skins;
        }

        #endregion Methods
    }
}