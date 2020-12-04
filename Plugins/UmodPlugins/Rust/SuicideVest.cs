using Newtonsoft.Json;
using Rust;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Suicide Vest", "birthdates", "1.0.5")]
    [Description("Allows players to have a suicide vest that blows up")]
    public class SuicideVest : RustPlugin
    {
        #region Variables
        private List<BasePlayer> armed = new List<BasePlayer>();
        private Dictionary<BasePlayer, long> cooldowns = new Dictionary<BasePlayer, long>();
        private Dictionary<BasePlayer, Timer> timers = new Dictionary<BasePlayer, Timer>();
        public Data data;

        private string permission_give = "suicidevest.give";
        private string permission_use = "suicidevest.use";
        #endregion

        #region Hooks
        void Init()
        {
            LoadConfig();
            permission.RegisterPermission(permission_use, this);
            permission.RegisterPermission(permission_give, this);
            cmd.AddChatCommand("givevest", this, ChatCMD);
            data = Interface.Oxide.DataFileSystem.ReadObject<Data>("Suicide Vest");
            if(!_config.explodeWhenShot)
            {
                Unsubscribe("OnEntityTakeDamage");
            }
        }

        void OnServerInitialized()
        {
            foreach(var pref in _config.explosionPrefab)
            {
                if(!StringPool.toString.Values.Contains(pref))
                {
                    PrintError(pref + " is not a valid prefab!");
                }
            }
            if(!StringPool.toString.Values.Contains(_config.armedSoundPrefab))
            {
                PrintError(_config.armedSoundPrefab + " is not a valid prefab!");
            }
            if(!StringPool.toString.Values.Contains(_config.unarmedSoundPrefab))
            {
                PrintError(_config.unarmedSoundPrefab + " is not a valid prefab!");
            }
            setupItems();
            cleanup();
        }

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("Suicide Vest", data);
            if(data.vests.Count < 1)
            {
                Unsubscribe("OnPlayerInput");
                Unsubscribe("OnPlayerDie");
                if(_config.explodeWhenShot) Unsubscribe("OnEntityTakeDamage");
            }
            else if(data.vests.Count == 1)
            {
                Subscribe("OnPlayerInput");
                Subscribe("OnPlayerDie");
                if(_config.explodeWhenShot) Subscribe("OnEntityTakeDamage");
            }
        }

        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if(!permission.UserHasPermission(player.UserIDString, permission_use) || input.WasJustReleased(BUTTON.FIRE_THIRD) || !input.WasDown(BUTTON.SPRINT) || !hasVest(player))
            {
                return;
            }

            if(armed.Contains(player))
            {
                if(!_config.unarm)
                {
                    return;
                }

                if(!cooldowns.ContainsKey(player))
                {
                    cooldowns.Add(player, DateTime.Now.Ticks + TimeSpan.FromSeconds(_config.unarmCooldown).Ticks);
                }
                else
                {
                    if(cooldowns [player] > DateTime.Now.Ticks)
                    {
                        return;
                    }
                    else
                    {
                        cooldowns.Remove(player);
                        cooldowns.Add(player, DateTime.Now.Ticks + TimeSpan.FromSeconds(_config.unarmCooldown).Ticks);
                    }
                }
                unarm(player);
            }
            else
            {
                arm(player);
            }
        }

        Vector3 getBody(BasePlayer player)
        {
            var pos = player.eyes.position;
            pos.y -= 1;
            return pos;
        }

        void OnPlayerDie(BasePlayer player, HitInfo info)
        {
            if(!armed.Contains(player))
            {
                return;
            }

            if(_config.explodeOnDeath)
            {
                explode(player);
                armed.Remove(player);
            }
            else
            {
                unarm(player);
            }
        }

        object CanMoveItem(Item item, PlayerInventory playerLoot, uint targetContainer, int targetSlot, int amount)
        {
            var player = item.GetOwnerPlayer();
            if(player == null)
            {
                return null;
            }

            if(armed.Contains(player) && data.vests.Contains(item.uid))
            {
                SendReply(player, lang.GetMessage("CannotMoveWhilstArmed", this, player.UserIDString));
                return false;
            }

            return null;
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            var player = entity as BasePlayer;

            if(player == null || !armed.Contains(player)) return;
            var p = info.Initiator as BasePlayer;
            if(p == null) return;
            if(info.boneName != "chest" || info.damageTypes.types [9] < 1f)
            {
                return;
            }
            explode(player);
        }

        private HashSet<Item> GetBoxItems()
        {
            var returnList = new HashSet<Item>();

            var boxes = Resources.FindObjectsOfTypeAll<StorageContainer>();
            foreach(var box in boxes)
            {
                if(box?.inventory?.itemList == null)
                {
                    continue;
                }

                foreach(var item in box.inventory.itemList)
                {
                    returnList.Add(item);
                }
            }

            return returnList;
        }

        private HashSet<Item> GetContainerItems(ItemContainer container)
        {
            var returnList = new HashSet<Item>();

            foreach(var item in container.itemList)
            {
                returnList.Add(item);
            }

            return returnList;
        }

        private HashSet<Item> GetEntityItems()
        {
            var returnList = new HashSet<Item>();
            var entities = BaseNetworkable.serverEntities.ToList();
            foreach(var a in entities.Cast<BaseEntity>().Where(x => x != null && x.GetItem() != null && data.vests.Contains(x.GetItem().uid)))
            {
                returnList.Add(a?.GetItem());
            }
            return returnList;
        }

        private HashSet<Item> GetPlayerItems()
        {
            var returnList = new HashSet<Item>();

            var players = new HashSet<BasePlayer>(BasePlayer.activePlayerList);
            players.UnionWith(BasePlayer.sleepingPlayerList);

            foreach(var player in players)
            {
                returnList.UnionWith(GetContainerItems(player.inventory.containerMain));
                returnList.UnionWith(GetContainerItems(player.inventory.containerBelt));
                returnList.UnionWith(GetContainerItems(player.inventory.containerWear));
            }

            return returnList;
        }

        void arm(BasePlayer player)
        {
            SendReply(player, string.Format(lang.GetMessage("Armed", this, player.UserIDString), _config.delay));
            armed.Add(player);

            Effect.server.Run(_config.armedSoundPrefab, player.transform.position);
            if(timers.ContainsKey(player))
            {
                timers [player].Destroy();
                timers.Remove(player);
            }
            timers.Add(player, timer.In(_config.delay, delegate
            {
                if(!armed.Contains(player))
                {
                    return;
                }
                armed.Remove(player);
                explode(player);

            }));
        }

        void explode(BasePlayer player)
        {
            var vest = getVest(player);
            if(vest != null)
            {
                vest.RemoveFromContainer();
                vest.RemoveFromWorld();
            }

            var pos = getBody(player);
            foreach(var pref in _config.explosionPrefab)
            {
                Effect.server.Run(pref, pos);
            }

            if(_config.explosionDamage > 0)
            {
                var all = new List<BaseCombatEntity>();

                Vis.Entities(pos, _config.explosionRadius, all);

                foreach(var entity in all.ToList())
                {
                    if(entity != null && entity.health > 0)
                    {
                        var a = entity.health;
                        entity.Hurt(new HitInfo(player, entity, DamageType.Explosion, _config.explosionDamage));
                        var p = entity as BasePlayer;
                        if(p != null && p.health != a)
                        {
                            p.metabolism.bleeding.value += _config.bleedingAfterDamage;
                            Interface.CallHook("OnRunPlayerMetabolism", p.metabolism);
                        }
                    }
                }
            }
            data.vests.Remove(vest.uid);
            SaveData();
        }

        void unarm(BasePlayer player)
        {
            SendReply(player, lang.GetMessage("UnArmed", this, player.UserIDString));
            armed.Remove(player);
            Effect.server.Run(_config.unarmedSoundPrefab, player.transform.position);
            if(timers.ContainsKey(player))
            {
                timers [player].Destroy();
                timers.Remove(player);
            }
        }

        bool hasVest(BasePlayer player) => player.inventory.containerWear.itemList.Find(x => data.vests.Contains(x.uid)) != null;

        Item getVest(BasePlayer player) => player.inventory.containerWear.itemList.Find(x => data.vests.Contains(x.uid));

        [ConsoleCommand("givevest")]
        void ConsoleCMD(ConsoleSystem.Arg arg)
        {
            if(!arg.IsAdmin)
            {
                return;
            }

            if(arg.Args.Length < 1)
            {
                arg.ReplyWith(lang.GetMessage("InvalidPlayer", this));
                return;
            }
            var player = BasePlayer.Find(arg.GetString(0));
            if(player == null)
            {
                arg.ReplyWith(lang.GetMessage("InvalidPlayer", this));
                return;
            }
            var vestItem = ItemManager.CreateByName(_config.item, 1, _config.skinID);
            if(vestItem == null)
            {
                PrintError("Vest item is NULL! Please fix your configuration.");
                return;
            }
            vestItem.name = _config.name;
            player.GiveItem(vestItem);

            data.vests.Add(vestItem.uid);
            SaveData();

        }

        void setupItems()
        {
            var list = GetBoxItems();
            list.UnionWith(GetPlayerItems());
            list.UnionWith(GetEntityItems());
            foreach(var item in list.Where(x => data.vests.Contains(x.uid)))
            {
                item.name = _config.name;
                item.skin = _config.skinID;
                item.MarkDirty();
            }
        }

        void Unload()
        {
            var list = GetBoxItems();
            list.UnionWith(GetPlayerItems());
            list.UnionWith(GetEntityItems());
            foreach(var item in list.Where(x => data.vests.Contains(x.uid)))
            {

                item.name = string.Empty;
                item.skin = 0;
                item.MarkDirty();
            }
        }

        void OnServerSave() => cleanup();

        void cleanup()
        {
            var list = GetBoxItems();
            list.UnionWith(GetPlayerItems());
            list.UnionWith(GetEntityItems());
            for(var z = 0; z < data.vests.Count; z++)
            {
                var vest = data.vests [z];
                if(list.FirstOrDefault(x => x.uid == vest) == null)
                {
                    data.vests.Remove(vest);
                }
            }
            SaveData();

        }

        object OnItemAction(Item item, string action, BasePlayer player)
        {
            if(action == "drop" && data.vests.Contains(item.uid) && armed.Contains(player))
            {
                SendReply(player, lang.GetMessage("CannotMoveWhilstArmed", this, player.UserIDString));
                return false;
            }
            return null;
        }

        void ChatCMD(BasePlayer player, string command, string [] args)
        {
            if(!permission.UserHasPermission(player.UserIDString, permission_give))
            {
                SendReply(player, lang.GetMessage("NoPermission", this, player.UserIDString));
            }
            else
            {
                BasePlayer target;
                if(args.Length < 1)
                {
                    target = player;
                }
                else target = BasePlayer.Find(args [0]);

                if(player == null)
                {
                    SendReply(player, lang.GetMessage("InvalidPlayer", this, player.UserIDString));
                    return;
                }
                var vestItem = ItemManager.CreateByName(_config.item, 1, _config.skinID);
                if(vestItem == null)
                {
                    PrintError("Vest item is NULL! Please fix your configuration.");
                    return;
                }
                vestItem.name = _config.name;
                target.GiveItem(vestItem);

                data.vests.Add(vestItem.uid);
                SaveData();

                SendReply(player, string.Format(lang.GetMessage("VestGiveSuccess", this, player.UserIDString), target.displayName));
            }
        }
        #endregion

        #region Configuration & Language
        public ConfigFile _config;

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Armed", "You have armed your vest and it will explode in {0} seconds."},
                {"UnArmed", "You have unarmed your vest."},
                {"CannotMoveWhilstArmed","You cannot move your vest whilst it's armed!"},
                {"NoPermission", "No permission!"},
                {"InvalidPlayer", "Invalid player!"},
                {"VestGiveSuccess", "You have given {0} a suicide vest"}
            }, this);
        }

        public class Data
        {
            public List<uint> vests = new List<uint>();
        }

        public class ConfigFile
        {
            [JsonProperty("Vest Item (Shortname)")]
            public string item;
            [JsonProperty("Vest Skin ID")]
            public ulong skinID;
            [JsonProperty("Count down after armed (seconds)")]
            public int delay;
            [JsonProperty("Vest Name")]
            public string name;
            [JsonProperty("Ability to unarm")]
            public bool unarm;
            [JsonProperty("Effect prefabs")]
            public List<string> explosionPrefab;
            [JsonProperty("Vest armed sound (prefab)")]
            public string armedSoundPrefab;
            [JsonProperty("Vest unarmed sound (prefab)")]
            public string unarmedSoundPrefab;
            [JsonProperty("Explode when a user dies with an armed vest?")]
            public bool explodeOnDeath;
            [JsonProperty("Unarm Cooldown (seconds)")]
            public long unarmCooldown;
            [JsonProperty("Explosion Radius")]
            public float explosionRadius;
            [JsonProperty("Explosion Damage")]
            public float explosionDamage;
            [JsonProperty("Bleeding Damage")]
            public float bleedingAfterDamage;
            [JsonProperty("Explode when a user shoots the vest?")]
            public bool explodeWhenShot;

            public static ConfigFile DefaultConfig()
            {
                return new ConfigFile()
                {
                    item = "metal.plate.torso",
                    skinID = 0,
                    name = "Suicide Vest",
                    delay = 10,
                    unarm = false,
                    explosionPrefab = new List<string>
                    {
                        "assets/bundled/prefabs/fx/gas_explosion_small.prefab",
                        "assets/bundled/prefabs/fx/explosions/explosion_03.prefab"
                    },
                    armedSoundPrefab = "assets/prefabs/locks/keypad/effects/lock.code.updated.prefab",
                    unarmedSoundPrefab = "assets/prefabs/locks/keypad/effects/lock.code.denied.prefab",
                    explodeOnDeath = true,
                    unarmCooldown = 3,
                    explosionRadius = 3f,
                    explosionDamage = 75f,
                    bleedingAfterDamage = 10f,
                    explodeWhenShot = true
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<ConfigFile>();
            if(_config == null)
            {
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig()
        {
            _config = ConfigFile.DefaultConfig();
            PrintWarning("Default configuration has been loaded.");
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }
        #endregion
    }
}
//Generated with birthdates' Plugin Maker
