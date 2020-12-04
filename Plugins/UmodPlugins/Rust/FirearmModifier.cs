﻿using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Firearm Modifier", "Khan", "1.0.5")]
    [Description("Allows you to change Magazine Size + Weapon Condition levels")]
    public class FirearmModifier : RustPlugin
    {

        private string Use = "firearmmodifier.use";

        private PluginConfig config;

        private List<string> Exclude = new List<string>
        {
            "bow_hunting.entity",
            "compound_bow.entity",
            "crossbow.entity"
        };

        private class PluginConfig
        {
            [JsonProperty("Weapon Options")]
            public Dictionary<string, WeaponOption> WeaponOptions = new Dictionary<string, WeaponOption>();

            public string ToJson() => JsonConvert.SerializeObject(this);
            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }

        public class WeaponOption
        {
            public int MagazineSize;
            public float ItemCondition;
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"InvalidSelection", "You've selected an invalid weapon to modify please type a valid weapon shortname" },
                {"Syntax", "Invalid Params please do /modify shortname magazinesize amount"},
                {"NoPerm", "Unkown Command: modify"},
                {"Success", "You've successfully set {0} to {1}"}
            }, this);
        }
        string GetMessage(string key, string userId = null, params object[] args) => string.Format(lang.GetMessage(key, this, userId), args);

        private void CheckConfig()
        {
            foreach (var itemDefinition in ItemManager.itemList)
            {
                if (itemDefinition == null) continue;

                ItemModEntity itemModEntity = itemDefinition.GetComponent<ItemModEntity>();
                if (itemModEntity == null) continue;

                BaseProjectile baseProjectile = itemModEntity.entityPrefab?.Get()?.GetComponent<BaseProjectile>();
                if (baseProjectile == null) continue;

                if (Exclude.Contains(baseProjectile.ShortPrefabName)) continue;

                if (config.WeaponOptions.ContainsKey(baseProjectile.ShortPrefabName)) continue;

                config.WeaponOptions.Add(baseProjectile.ShortPrefabName, new WeaponOption
                {
                    MagazineSize = baseProjectile.primaryMagazine.definition.builtInSize,
                    ItemCondition = itemDefinition.condition.max
                });
            }
            SaveConfig();
        }

        private void Init()
        {
            permission.RegisterPermission(Use, this);
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<PluginConfig>();

                if (config == null)
                {
                    throw new JsonException();
                }

                CheckConfig();

                if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys))
                {
                    PrintWarning($"Configuration file {Name}.json was Updated");
                    SaveConfig();
                }

            }
            catch
            {
                PrintError("Configuration file is corrupt! Loading Default Config");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(config, true);

        protected override void LoadDefaultConfig() => config = new PluginConfig();

        void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            if (!permission.UserHasPermission(task.owner.UserIDString, Use))
            {
                return;
            }
            BaseProjectile projectile = item.GetHeldEntity() as BaseProjectile;

            if (projectile == null || !config.WeaponOptions.ContainsKey(projectile.ShortPrefabName))
            {
                return;
            }
            if (item.condition >= item._maxCondition)
            {
                WeaponOption weaponOptions = config.WeaponOptions[projectile.ShortPrefabName];
                item._maxCondition = weaponOptions.ItemCondition;
                item.condition = weaponOptions.ItemCondition;
                projectile.SendNetworkUpdate();
            }
        }

        void OnReloadWeapon(BasePlayer player, BaseProjectile projectile)
        {
            if (!(permission.UserHasPermission(player.UserIDString, Use) && config.WeaponOptions.ContainsKey(projectile.ShortPrefabName)))
            {
                return;
            }

            WeaponOption weaponOptions = config.WeaponOptions[projectile.ShortPrefabName];
            projectile.primaryMagazine.definition.builtInSize = weaponOptions.MagazineSize;
            projectile.primaryMagazine.capacity = weaponOptions.MagazineSize;
            projectile.SendNetworkUpdate();
        }

        [ChatCommand("modify")]
        private void cmdmodify(BasePlayer player, string command, string[] args)
        {

            if (!permission.UserHasPermission(player.UserIDString, Use))
            {
                SendReply(player, GetMessage("NoPerm", player.UserIDString));
                return;
            }

            if (args.Length < 3)
            {
                SendReply(player, GetMessage("Syntax", player.UserIDString));
                return;
            }
            WeaponOption weaponoption;
            if (!config.WeaponOptions.TryGetValue(args[0].ToLower(), out weaponoption))
            {
                SendReply(player, GetMessage("InvalidSelection", player.UserIDString));
                return;
            }
            switch (args[1].ToLower())
            {
                case "magazinesize":
                    weaponoption.MagazineSize = int.Parse(args[2]);
                    SendReply(player, GetMessage("Success", player.UserIDString, "magazinesize", weaponoption.MagazineSize));
                    SaveConfig();
                    break;
                case "itemcondition":
                    weaponoption.ItemCondition = int.Parse(args[2]);
                    SendReply(player, GetMessage("Success", player.UserIDString, "itemcondition", weaponoption.ItemCondition));
                    SaveConfig();
                    break;
                default:
                    SendReply(player, GetMessage("Syntax", player.UserIDString));
                    break;

            }

        }
    }
}