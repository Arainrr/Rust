using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using Rust.Ai.HTN.Bear.Reasoners;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Network;
using ProtoBuf;
using System.Globalization;
using Rust;
using Oxide.Core.Configuration;
using Newtonsoft.Json.Serialization;
using Oxide.Core.Libraries.Covalence;

/********************************************************
 *  Follow the status on https://trello.com/b/BCm6PUwK/guishop   __φ(．．)

 *  Credits to Nogrod and Reneb for the original plugin. <Versions up to 1.4.6
 *  Thanks! to Default for maintaining and adding in feature updates over the years.  <versions 1.4.65 to 1.5.9

 *  Current Maintainer: 8/14/2020 Khan#8615 discord ID.  1.6.0  to Present.
 *  This plugin was rewritten section by section by Khan with the help of Bazzel in the months of August and September 2020.
 *  Massive Thanks to Bazz3l! Excellent teacher ◝(⁰▿⁰)◜
 *  Specialist hockeygel23 is working with me to finish developement of all the upcoming UI customizations!
 *  Special shoutout to whispers88! Without him we all wouldn't have the generator function idea!
 *  Thank you wishpers for the folder/data files help!
 *  Auto Config Updater by WhiteThunder! ◝(⁰▿⁰)◜
 * -----------------------------------------------------------

 *  TODO:
 *  Fix screen fullpaint flicker.
 *  Finish implementing limiter funciton | Add in new GUI layout + True/false for classic style.| Add ImageLibrary
 *  Code Cleanup/reformatting completed.
 *  Limiter Function thing
 *  Custom Points system with hooks or add Server Rewards support.
 *  Make VIP Permissions for UI stuff
 *
 *******************************************************/
// * Fix RaidableBases issue (Need to patch raidable bases hooks then patch guishop or create workaround.)
// * Create Error msgs for limits TODO:

/*
This Update 1.8.8
Enable GUIShop NPC Msg responses true/false setting
Seperated Code Relating to UI Color Changer and Made own Helper method Section at bottom for them
Added Disable Options for Personal UI stuff
Added Global Bypass setting for image or text
Added VIP Permission
*/

namespace Oxide.Plugins
{
    [Info("GUIShop", "Khan", "1.8.8")]
    [Description("GUI Shop based on Economics, with NPC support Re-Write ◝(⁰▿⁰)◜")]
    public class GUIShop : RustPlugin
    {
        #region References
        [PluginReference] Plugin Economics, Kits, ImageLibrary, ServerRewards;
        #endregion

        #region Fields
        private const string ShopOverlayName = "ShopOverlay";
        private const string ShopContentName = "ShopContent";
        private const string ShopDescOverlay = "ShopDescOverlay";
        private const string BlockAllow = "guishop.BlockByPass";         //Bypasses being raid blocked.
        private const string Use = "guishop.use";                 //needed to use /shop as a default player
        private const string Admin = "guishop.admin";            //adding for new admin commands coming
        public const string Vip = "guishop.vip";               //adding for color customizations
        readonly Hash<ulong, int> shopPage = new Hash<ulong, int>();
        private readonly int[] steps = { 1, 10, 100, 1000 };
        private Dictionary<ulong, Dictionary<string, double>> sellCooldowns;
        private Dictionary<ulong, Dictionary<string, double>> buyCooldowns;
        private Dictionary<string, ulong> buyed;
        private Dictionary<string, ulong> selled;
        private Dictionary<ulong, ItemLimit> limits;     //added limiter
        private List<MonumentInfo> _monuments; //updated ?
        //private bool Balance;
        private bool configChanged;
        int playersMask = LayerMask.GetMask("Player (Server)");
        private bool isShopReady;

        //adding image library support for config links 1.8.7
        private const string BackgroundImage = "Background"; //added 1.8.7
        /*private const string WelcomeImage = "WelcomeIcon";
        private const string IconImage = "IconImagee";
        private const string BuyIcon = "Buyicon";
        private const string SellIcon = "Sellicon";
        private const string AmountIcon1 = "Amounticon1";
        private const string AmountIcon2 = "Amounticon2";
        private const string CloseIcon = "Closeicon";*/

        //private static Economics Economics; //added

        //Auto Close
        private BasePlayer ShopPlayer;
        private List<string> PlayerUIOpen = new List<string>();

        //This is for all the new Color Systems
        private static GUIShop _instance;
        private static List<PlayerUISetting> playerdata = new List<PlayerUISetting>();
        private string UISettingChange = "Text";
        private bool ImageChanger;
        private double Transparency = 0.95;
        private PluginConfig config;
        #endregion

        #region Config

        internal class PluginConfig : SerializableConfiguration
        {
            [JsonProperty("Set Default Global Shop to open")]
            public string DefaultShop = "Commands";

            [JsonProperty("Switches to ServerRewards as default curency")]
            public bool CurrencySwitcher = false;

            [JsonProperty("Was Saved Don't Touch!")]
            public bool WasSaved = false;

            [JsonProperty("Sets shop command")]
            public string shopcommand = "shop";

            [JsonProperty("Player UI display")] //1.8.8
            public bool PersonalUI = false;

            [JsonProperty("Block Monuments")]
            public bool BlockMonuments = false;

            [JsonProperty("If true = Images, If False = Text Labels")]
            public bool UIImageOption = false;

            [JsonProperty("Enable GUIShop NPC Msg's")] //1.8.7
            public bool NPCLeaveResponse = false;

            [JsonProperty("GUI Shop - Welcome MSG")] // Shop Welcome Label
            public string WelcomeMsg = "WELCOME TO GUISHOP ◝(⁰▿⁰)◜";

            [JsonProperty("Shop - Buy Price Label")] // Buy Price Label  //NewUI + Classic
            public string BuyLabel = "Buy Price";

            [JsonProperty("Shop - Amount1 Label1")] // Amount1 label for both //NewUI + Classic
            public string AmountLabel = "Amount";

            [JsonProperty("Shop - Sell $ Label")] // Sell $ Label //NewUI + Classic
            public string SellLabel = "Sell $";

            [JsonProperty("Shop - Amount2 Label2")] // Amount2 label for both //NewUI + Classic
            public string AmountLabel2 = "Amount";

            [JsonProperty("Shop - Close Label")]  // Close button Label.
            public string CloseButtonlabel = "CLOSE";

            [JsonProperty("Shop - GUIShop Welcome Url")] // Welcome URL Image
            public string GuiShopWelcomeUrl = "https://i.imgur.com/RcLdEly.png";

            [JsonProperty("Shop - GUIShop Background Image Url")] //setting this results in all shop items having the same Icon. 1.8.8 fixed mistake edit
            public string BackgroundUrl = "https://i.imgur.com/i8h0RPa.png";

            [JsonProperty("Shop - Sets any shop items to this image if image link does not exist.")] //Sets any shop items to this image if image link does not exist.
            public string IconUrl = "https://imgur.com/BPM9UR4.png";

            [JsonProperty("Shop - Shop Buy Icon Url")] // Buy Image URL  //NewUI + Classic
            public string BuyIconUrl = "https://imgur.com/oeVUwCy.png";

            [JsonProperty("Shop - Shop Amount Image1")] // Ammount label for both //NewUI + Classic
            public string AmountUrl = "https://imgur.com/EKtvylU.png";

            [JsonProperty("Shop - Shop Amount Image2")] // Ammount label for both //NewUI + Classic
            public string AmountUrl2 = "https://imgur.com/EKtvylU.png";

            [JsonProperty("Shop - Shop Sell Icon Url")] // Sell Image URL //NewUI + Classic
            public string SellIconUrl = "https://imgur.com/jV3hEHy.png";

            [JsonProperty("Shop - Close Image Url")]  // Close button image URL.
            public string CloseButton = "https://imgur.com/IK5yVrW.png";

            /*[JsonProperty("GUIShop background transparency (min = 0.9, max = 1)")]  // Added for Close button image.
            public double Transparency = 0.95;*/

            [JsonProperty("GUIShop Configurable UI colors (First 8 Colors!)")]  // Added for Close button image.
            public List<string> ColorsUI = new List<string>();

            [JsonProperty("Shop - Shop Categories")]
            public Dictionary<string, ShopCategory> ShopCategories = new Dictionary<string, ShopCategory>();

            [JsonProperty("Shop - Shop List")]
            public Dictionary<string, ShopItem> ShopItems = new Dictionary<string, ShopItem>();

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }

        public class ShopItem
        {
            public string DisplayName;
            public string Shortname;
            public bool EnableBuy;
            public bool EnableSell;
            public string Image;
            public double SellPrice;
            public double BuyPrice;
            public int BuyCooldown;
            public int SellCooldown;
            //public int BuyLimit;
            //public int SellLimit;
            public string KitName;
            public List<string> Command; //updated
            public ulong SkinId;
        }

        public class ShopCategory
        {
            public string DisplayName;
            public string Description;
            public bool EnabledCategory;
            public bool EnableNPC;
            public string NPCId;
            public List<string> Items = new List<string>();
        }

        class ItemLimit //limiter function TODO:
        {
            public Dictionary<string, int> buy = new Dictionary<string, int>();
            public Dictionary<string, int> sell = new Dictionary<string, int>();

            public bool HasSellLimit(string item, int amount)
            {
                if (!sell.ContainsKey(item))
                {
                    sell[item] = 1;
                }

                return sell[item] >= amount;
            }

            public bool HasBuyLimit(string item, int amount)
            {
                if (!buy.ContainsKey(item))
                {
                    buy[item] = 1;
                }

                return buy[item] >= amount;
            }

            public void IncrementBuy(string item)
            {
                if (!buy.ContainsKey(item))
                    buy[item] = 1;
                else
                    buy[item]++;
            }

            public void IncrementSell(string item)
            {
                if (!sell.ContainsKey(item))
                    sell[item] = 1;
                else
                    sell[item]++;
            }
        }

        private class PlayerUISetting
        {
            public string playerID;
            public double Transparency;
            public string SellBoxColors;
            public string BuyBoxColors;
            public string UITextColor;
            public double rangeValue;
            public bool ImageOrText;
        }

        #region Lang File Messages?

        // load default messages to Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"MessageShowNoEconomics", "Couldn't get informations out of Economics. Is it installed? __φ(．．)"},
                {"MessageShowNoServerRewards", "Couldn't get informations out of ServerRewards. Is it installed? __φ(．．)"},
                {"MessageBought", "You've successfully bought {0}x {1}"},
                {"MessageSold", "You've successfully sold {0}x {1} "},
                {"MessageErrorCooldown", "This item has a cooldown of {0} seconds."},
                {"MessageErrorCooldownAmount", "This item has a cooldown and amount is limited to 1"}, //Was limited to 1. Update to {0}
                {"MessageErrorLimit", "This item has a limit of {0} uses"}, //Error limit msgs (•ิ_•ิ)?
                {"MessageErrorInventoryFull", "Your inventory is full."},
                {"MessageErrorInventorySlots", "Your inventory needs {0} free slots."},
                {"MessageErrorNoShop", "This shop doesn't seem to exist."},
                {"MessageErrorGlobalDisabled", "Global Shops are disabled please visit NPC vendors!"},
                {"MessageErrorNoActionShop", "You are not allowed to {0} in this shop"},
                {"MessageErrorNoActionItem", "You are not allowed to {0} this item here"},
                {"MessageErrorItemItem", "WARNING: It seems like it's not a valid item"},
                {"MessageErrorItemNoValid", "WARNING: It seems like it's not a valid item"},
                {"MessageErrorRedeemKit", "WARNING: There was an error while giving you this kit"},
                {"MessageErrorBuyCmd", "Can't buy multiple (・・ ) ?"},
                {"MessageErrorBuyPrice", "WARNING: No buy price was given by the admin, you can't buy this item"},
                {"MessageErrorSellPrice", "WARNING: No sell price was given by the admin, you can't sell this item"},
                {"MessageErrorNotEnoughMoney", "You need {0} coins to buy {1} of {2} (¬‿¬ )"},
                {"MessageErrorNotEnoughSell", "You don't have enough of this item. (o_O) !"},
                {"MessageErrorNotNothing", "You cannot buy nothing of this item. (o_O) !"},
                {"MessageErrorItemNoExist", "WARNING: The item you are trying to buy doesn't seem to exist (o_O) !"},
                {"MessageErrorItemNoExistTake", "WARNING: The item you are trying to sell is not sellable (っ•﹏•)っ"},
                {"MessageErrorBuildingBlocked", "You cannot shop while in building blocked area. (っ•﹏•)っ"},
                {"MessageErrorAdmin", "You do not have the admin permission for GUIShop to use this command (•ิ_•ิ)?"},
                {"MessageErrorWaitingOnDownloads", "GUIShop is waiting on downloads to finish first __φ(．．)"}, //new error
                {"BlockedMonuments", "Aye! You may not shop while near a Monument!"},
                {"MessageErrorItemNotEnabled", "The shop keeper has disabled this item; pleb"},
                {"MessageErrorItemNotFound", "Item was not found"},
                {"CantSellCommands", "Commands cannot be sold"},
                {"CantSellKits", "Kits cannot be sold"},
                {"MessageErrorCannotSellWhileEquiped", "Cannot Sell while wearing this item."},
                {"MessageShopResponse", "GUIShop is waiting for ImageLibrary downloads to finish please wait."},
                {"MessageNPCResponseclose", "Thanks for shopping at {0} come again soon!"}
            }, this);
        }

        // get message from Lang
        string GetMessage(string key, string userId = null) => lang.GetMessage(key, this, userId);
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        #endregion

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

                if (MaybeUpdateConfig(config))
                {
                    PrintWarning("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }

            }
            catch
            {
                LoadDefaultConfig();

                PrintToConsole($"Please verify your {Name}.json config at <http://pro.jsonlint.com/>.");
            }
        }

        protected override void SaveConfig()
        {
            PrintToConsole($"Configuration changes saved to {Name}.json");

            Config.WriteObject(config);
        }

        /*private void CustomList()
        {

        }*/

        private void CheckConfig()
        {

            if (!config.ShopCategories.ContainsKey("Commands"))
            {
                config.ShopCategories.Add("Commands", new ShopCategory
                {
                    DisplayName = "Commands",
                    Description = "You currently have {0} coins to spend in the commands shop",
                    EnabledCategory = true
                });
                configChanged = true;
            }

            // Example of command adding
            if (config.ShopCategories.ContainsKey("Commands") && !config.ShopItems.ContainsKey("Minicopter") && !config.ShopItems.ContainsKey("Sedan") && !config.ShopItems.ContainsKey("Airdrop Call"))
            {
                config.ShopItems.Add("Minicopter", new ShopItem
                {
                    DisplayName = "Minicopter",
                    Shortname = "minicopter",
                    EnableBuy = true,
                    EnableSell = false,
                    Image = "https://i.imgur.com/vI6LwCZ.png",
                    BuyPrice = 1.0,
                    SellPrice = 1.0,
                    BuyCooldown = 0,
                    SellCooldown = 0,
                    KitName = null,
                    Command = new List<string> { "spawn minicopter \"$player.x $player.y $player.z\"" },
                });

                config.ShopItems.Add("Sedan", new ShopItem
                {
                    DisplayName = "Sedan",
                    Shortname = "sedan",
                    EnableBuy = true,
                    EnableSell = false,
                    Image = "",
                    BuyPrice = 1.0,
                    SellPrice = 1.0,
                    BuyCooldown = 0,
                    SellCooldown = 0,
                    KitName = null,
                    Command = new List<string> { "spawn sedan \"$player.x $player.y $player.z\"" },
                });

                config.ShopItems.Add("Airdrop Call", new ShopItem
                {
                    DisplayName = "Airdrop Call",
                    Shortname = "airdrop.call",
                    EnableBuy = true,
                    EnableSell = true,
                    Image = "",
                    BuyPrice = 1.0,
                    SellPrice = 1.0,
                    BuyCooldown = 0,
                    SellCooldown = 0,
                    KitName = null,
                    Command = new List<string> { "inventory.giveto $player.id supply.signal" },
                });

                config.ShopCategories["Commands"].Items.Add("Minicopter");
                config.ShopCategories["Commands"].Items.Add("Sedan");
                config.ShopCategories["Commands"].Items.Add("Airdrop Call");

                configChanged = true;
            }

            //Auto Generator Function
            foreach (ItemDefinition item in ItemManager.itemList)
            {
                string categoryName = item.category.ToString();

                ShopCategory shopCategory;

                if (!config.ShopCategories.TryGetValue(categoryName, out shopCategory))
                {
                    config.ShopCategories[categoryName] = shopCategory = new ShopCategory
                    {
                        DisplayName = item.category.ToString(),
                        Description = "You currently have {0} coins to spend in the " + item.category + " shop",
                        EnabledCategory = true
                    };

                    configChanged = true;
                }

                if (!shopCategory.Items.Contains(item.displayName.english) && !config.WasSaved) //updated 1.8.3
                {
                    shopCategory.Items.Add(item.displayName.english);

                    configChanged = true;
                }

                if (!config.ShopItems.ContainsKey(item.displayName.english))
                {
                    config.ShopItems.Add(item.displayName.english, new ShopItem
                    {
                        DisplayName = item.displayName.english,
                        Shortname = item.shortname,
                        EnableBuy = true,
                        EnableSell = true,
                        BuyPrice = 1.0,
                        SellPrice = 1.0,
                        Image = "https://rustlabs.com/img/items180/" + item.shortname + ".png"
                    });

                    configChanged = true;
                }
                CallHook("Add", string.Format(config.IconUrl, item.shortname)); //re-added + Updated
            }

            if (config.ColorsUI.Count <= 0)
            {
                config.ColorsUI = new List<string> { "#A569BD", "#2ECC71", "#E67E22", "#3498DB", "#E74C3C", "#F1C40F", "#F4F6F7", "#00FFFF" };
                configChanged = true;
            }

            if (configChanged)
            {
                config.WasSaved = true; //updated 1.8.3
                SaveConfig();
            }
        }

        internal class SerializableConfiguration
        {
            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonHelper.Deserialize(ToJson()) as Dictionary<string, object>;
        }

        internal static class JsonHelper
        {
            public static object Deserialize(string json) => ToObject(JToken.Parse(json));

            private static object ToObject(JToken token)
            {
                switch (token.Type)
                {
                    case JTokenType.Object:
                        return token.Children<JProperty>().ToDictionary(prop => prop.Name, prop => ToObject(prop.Value));
                    case JTokenType.Array:
                        return token.Select(ToObject).ToList();

                    default:
                        return ((JValue)token).Value;
                }
            }
        }

        private bool MaybeUpdateConfig(SerializableConfiguration config)
        {
            var currentWithDefaults = config.ToDictionary();
            var currentRaw = Config.ToDictionary(x => x.Key, x => x.Value);
            return MaybeUpdateConfigDict(currentWithDefaults, currentRaw);
        }

        private bool MaybeUpdateConfigDict(Dictionary<string, object> currentWithDefaults, Dictionary<string, object> currentRaw)
        {
            bool changed = false;

            foreach (var key in currentWithDefaults.Keys)
            {
                object currentRawValue;
                if (currentRaw.TryGetValue(key, out currentRawValue))
                {
                    var defaultDictValue = currentWithDefaults[key] as Dictionary<string, object>;
                    var currentDictValue = currentRawValue as Dictionary<string, object>;

                    if (defaultDictValue != null)
                    {
                        if (currentDictValue == null)
                        {
                            currentRaw[key] = currentWithDefaults[key];
                            changed = true;
                        }
                        else if (MaybeUpdateConfigDict(defaultDictValue, currentDictValue))
                            changed = true;
                    }
                }
                else
                {
                    currentRaw[key] = currentWithDefaults[key];
                    changed = true;
                }
            }

            return changed;
        }

        private void LoadImages()
        {
            Dictionary<string, string> images = new Dictionary<string, string>();

            foreach (ShopItem shopitem in config.ShopItems.Values)
            {
                if (images.ContainsKey(shopitem.Shortname)) continue;

                if (!shopitem.Command.IsNullOrEmpty())
                {
                    images.Add(shopitem.Shortname, shopitem.Image); //added
                }
                else
                {
                    images.Add(shopitem.Shortname, shopitem.Image); //added
                }
            }

            if (images.Count > 0)
            {
                ImageLibrary?.Call("ImportImageList", Title, images);
            }
            ImageLibrary?.Call("LoadImageList", Title, config.ShopItems.Select(x => new KeyValuePair<string, ulong>(x.Value.Shortname, x.Value.SkinId)).ToList(), new Action(ShopReady));

            ImageLibrary?.Call("AddImage", config.BackgroundUrl, BackgroundImage); // 1.8.7
            /*ImageLibrary?.Call("AddImage", config.GuiShopWelcomeUrl, WelcomeImage); // 1.8.7
            ImageLibrary?.Call("AddImage", config.IconUrl, IconImage); // 1.8.7
            ImageLibrary?.Call("AddImage", config.BuyIconUrl, BuyIcon); // 1.8.7
            ImageLibrary?.Call("AddImage", config.SellIconUrl, SellIcon); // 1.8.7
            ImageLibrary?.Call("AddImage", config.AmountUrl, AmountIcon1); // 1.8.7
            ImageLibrary?.Call("AddImage", config.AmountUrl2, AmountIcon2); // 1.8.7
            ImageLibrary?.Call("AddImage", config.CloseButton, CloseIcon); // 1.8.7*/

            if (!ImageLibrary)
            {
                isShopReady = true;
            }
        }



        private void ShopReady()
        {
            isShopReady = true;
        }

        #endregion

        #region Storage
        private void LoadData()
        {
            try
            {
                buyCooldowns = _buyCooldowns.ReadObject<Dictionary<ulong, Dictionary<string, double>>>();
            }
            catch
            {
                buyCooldowns = new Dictionary<ulong, Dictionary<string, double>>();
            }
            try
            {
                sellCooldowns = _sellCooldowns.ReadObject<Dictionary<ulong, Dictionary<string, double>>>();
            }
            catch
            {
                sellCooldowns = new Dictionary<ulong, Dictionary<string, double>>();
            }
            try
            {
                buyed = _buyed.ReadObject<Dictionary<string, ulong>>();
            }
            catch
            {
                buyed = new Dictionary<string, ulong>();
            }
            try
            {
                selled = _selled.ReadObject<Dictionary<string, ulong>>();
            }
            catch
            {
                selled = new Dictionary<string, ulong>();
            }
            try
            {
                limits = _limits.ReadObject<Dictionary<ulong, ItemLimit>>();
            }
            catch
            {
                limits = new Dictionary<ulong, ItemLimit>();
            }
            try
            {
                playerdata = _playerdata.ReadObject<List<PlayerUISetting>>();
            }
            catch
            {
                playerdata = new List<PlayerUISetting>();
            }
        }

        private void SaveData()
        {
            _buyCooldowns.WriteObject(buyCooldowns);
            _sellCooldowns.WriteObject(sellCooldowns);
            _buyed.WriteObject(buyed);
            _selled.WriteObject(selled);
            _limits.WriteObject(limits);
            _playerdata.WriteObject(playerdata);
        }
        #endregion

        #region Oxide

        Dictionary<ulong, string> CustomSpawnables = new Dictionary<ulong, string>
        {
            {
                2255658925, "assets/prefabs/deployable/woodenbox/woodbox_deployed.prefab"
            }
        };

        void OnUserConnected(IPlayer player)
        {
            BasePlayer new_player = player.Object as BasePlayer;
            //Puts($"{player.Name} ({player.Id}) connected from {player.Address}");
            NewConfigInDataFile(new_player);
        }

        void OnEntityBuilt(Planner plan, GameObject go)
        {
            BaseEntity entity = go.ToBaseEntity();
            if (entity == null || entity.skinID == 0UL)
            {
                return;
            }

            if (CustomSpawnables.ContainsKey(entity.skinID))
            {
                SpawnReplacementItem(entity, CustomSpawnables[entity.skinID]);

                NextTick(() => entity.Kill());
            }
        }

        void SpawnReplacementItem(BaseEntity entity, string prefabpath)
        {
            BaseEntity newentity = GameManager.server.CreateEntity(prefabpath, entity.ServerPosition, entity.ServerRotation);
            newentity?.Spawn();
        }

        void OnEntityTakeDamage(BasePlayer player, HitInfo info) //added auto close feature
        {
            if (info == null)
            {
                //Puts("Info is null");
            }
            if (PlayerUIOpen.Contains(player.UserIDString) == true && (info.IsProjectile() == true || info.damageTypes.Has(DamageType.Bite) || info.damageTypes.Has(DamageType.Blunt) || info.damageTypes.Has(DamageType.Drowned) || info.damageTypes.Has(DamageType.Explosion) || info.damageTypes.Has(DamageType.Stab) || info.damageTypes.Has(DamageType.Slash) || info.damageTypes.Has(DamageType.Fun_Water)))
            {
                DestroyUi(player, true);
            }
        }

        protected override void LoadDefaultConfig() => config = new PluginConfig();

        private void OnServerInitialized()
        {
            CheckConfig(); //added here from loadconfig area. 1.8.5 fix
            permission.RegisterPermission(BlockAllow, this);
            permission.RegisterPermission(Use, this);
            permission.RegisterPermission(Admin, this); //Added
            permission.RegisterPermission(Vip, this); //Adding VIP perms
            _monuments = TerrainMeta.Path.Monuments;

            //Subscribe("OnUseNPC");
            //Subscribe("OnLeaveNPC");

            LoadImages();
        }

        private void Init()
        {
            _instance = this;
        }

        private DynamicConfigFile _buyCooldowns;
        private DynamicConfigFile _sellCooldowns;
        private DynamicConfigFile _buyed;
        private DynamicConfigFile _selled;
        private DynamicConfigFile _limits;
        private DynamicConfigFile _playerdata;

        private void Loaded()
        {
            cmd.AddChatCommand(config.shopcommand, this, cmdShop);


            _buyCooldowns = Interface.Oxide.DataFileSystem.GetFile(nameof(GUIShop) + "/BuyCooldowns");
            _sellCooldowns = Interface.Oxide.DataFileSystem.GetFile(nameof(GUIShop) + "/SellCooldowns");
            _buyed = Interface.Oxide.DataFileSystem.GetFile(nameof(GUIShop) + "/Purchases");
            _selled = Interface.Oxide.DataFileSystem.GetFile(nameof(GUIShop) + "/Sales");
            _limits = Interface.Oxide.DataFileSystem.GetFile(nameof(GUIShop) + "/Limits"); //adding Buy Limiter Function (Limit) TODO:
            _playerdata = Interface.Oxide.DataFileSystem.GetFile(nameof(GUIShop) + "/GUIShopPlayerConfigs"); //added color customizations

            LoadData();
        }

        private void Unload()
        {
            SaveData();
            //Unsubscribe("OnUseNPC");
            //Unsubscribe("OnLeaveNPC");
            _instance = null;
        }

        private void OnServerSave() => SaveData();

        #endregion

        #region UI
        private static CuiElementContainer CreateShopOverlay(string shopname, BasePlayer player)
        {
            return new CuiElementContainer
            {
                {
                    new CuiPanel  //This is the background transparency slider!
                    {
                        Image = {
                            Color = $"0 0 0 {_instance.GetUITransparency(player)}",   // 0.1 0.1 0.1 0.98 //0.8 to 0.7 //Make darker or lighter. TODO: Make slider bar option.
                            //Material = "assets/content/ui/uibackgroundblur.mat" // new
                            //Material = "Assets/Icons/Iconmaterial.mat"
                            //Material = ""
                            //Url = "https://i.imgur.com/3LE40y0.png"
                        },
                        RectTransform = {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        },
                        CursorEnabled = true
                    },
                    "Overlay",
                    ShopOverlayName
                },
                {
                    new CuiElement // Background Image FLame border!
                    {
                        Parent = ShopOverlayName,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = _instance.ImageLibrary?.Call<string>("GetImage", BackgroundImage) //updated 1.8.7
                                //Url = _instance.config.BackgroundUrl
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "1 1"
                            }
                        }
                    }
                },
                {
                    new CuiElement // GUIShop Welcome MSG
                    {
                        Parent = ShopOverlayName,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                //Png = _instance.ImageLibrary?.Call<string>("GetImage", WelcomeImage) //updated 1.8.7
                                Url = _instance.GetText(_instance.config.GuiShopWelcomeUrl, "image", player)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.3 0.85",
                                AnchorMax = "0.7 0.95"
                            }
                        }
                    }
                },
                {
                    new CuiLabel //Welcome Msg
                    {
                        Text = {
                            Text = _instance.GetText(_instance.config.WelcomeMsg, "label", player),  //Updated to config output. https://i.imgur.com/Y9n5KgO.png
                            FontSize = 30,
                            Color = _instance.GetUITextColor(player),
                            Align = TextAnchor.MiddleCenter
                        },
                        RectTransform = {
                            AnchorMin = "0.3 0.85",
                            AnchorMax = "0.7 0.95"
                        }
                    },
                    ShopOverlayName
                },
                /*{
                    new CuiElement // Limit Icon
                    {
                        Parent = ShopOverlayName,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Url = _instance.GetText(config.LimitUrl, "image", player)  // // Adjust position/size
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.28 0.6",
                                AnchorMax = "0.33 0.65"
                            }
                        }
                    }
                },
                {
                    new CuiLabel
                    {
                        Text = {
                            Text = _instance.GetText(config.Limit, "label", player), //added Config output
                            FontSize = 20,
                            Color = _instance.GetUITextColor(player),
                            Align = TextAnchor.MiddleCenter
                        },
                        RectTransform = {
                            AnchorMin = "0.2 0.6",
                            AnchorMax = "0.5 0.65" //"0.23 0.65" old was Item rebranded to Limit
                        }
                    },
                    ShopOverlayName
                },*/
                /*{
                    new CuiLabel  //Adding missing Lable for limit function
                    {
                        Text = {
                            Text = "Limit",
                            FontSize = 20,
                            Align = TextAnchor.MiddleCenter
                        },
                        RectTransform = {
                            AnchorMin = "0.2 0.6", //"0.2 0.6", Buy
                            AnchorMax = "0.5 0.65" //"0.7 0.65"  Buy
                        }
                    },
                    ShopOverlayName
                },*/
                {
                    new CuiElement // Amount Icon
                    {
                        Parent = ShopOverlayName,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                //Png = _instance.ImageLibrary?.Call<string>("GetImage", AmountIcon1) //1.8.7
                                Url = _instance.GetText(_instance.config.AmountUrl, "image", player)  // // Adjust position/size
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.53 0.6",
                                AnchorMax = "0.58 0.65"
                            }
                        }
                    }
                },
                {
                    new CuiLabel // Amount Label
                    {
                        Text = {
                            Text = _instance.GetText(_instance.config.AmountLabel, "label", player),
                            FontSize = 20,
                            Color = _instance.GetUITextColor(player),
                            Align = TextAnchor.MiddleLeft
                        },
                        RectTransform = {
                            AnchorMin = "0.535 0.6",
                            AnchorMax = "0.7 0.65"
                        }
                    },
                    ShopOverlayName
                },
                {
                    new CuiElement // Buy Icon
                    {
                        Parent = ShopOverlayName,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                //Png = _instance.ImageLibrary?.Call<string>("GetImage", BuyIcon) //1.8.7
                                Url = _instance.GetText(_instance.config.BuyIconUrl, "image", player), //"https://i.imgur.com/3ucgFVg.png"  // Adjust position/size
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.435 0.6",
                                AnchorMax = "0.465 0.65"
                            }
                        }
                    }
                },
                {
                    new CuiLabel // Buy Price Label,
                    {
                        Text = {
                            //Color = "0 0 0 0.40",
                            Text = _instance.GetText(_instance.config.BuyLabel, "label", player),  //Updated
                            FontSize = 20,
                            Color = _instance.GetUITextColor(player),
                            Align = TextAnchor.MiddleCenter
                        },
                        RectTransform = {
                            AnchorMin = "0.4 0.6",
                            AnchorMax = "0.5 0.65"
                        }
                    },
                    ShopOverlayName
                },
                {
                    new CuiLabel // Old Sell Label, color added, added config output
                    {
                        Text = {
                            Text = _instance.GetText(_instance.config.SellLabel, "label", player),  //Sell $
                            FontSize = 20,
                            Color = _instance.GetUITextColor(player),
                            Align = TextAnchor.MiddleCenter
                        },
                        RectTransform = {
                            AnchorMin = "0.55 0.6",  //Second digit = Hight Done.
                            AnchorMax = "0.9 0.65"  //Left to right size for msg
                        }
                    },
                    ShopOverlayName
                },
                {
                    new CuiElement // Sell Icon
                    {
                        Parent = ShopOverlayName,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                //Png = _instance.ImageLibrary?.Call<string>("GetImage", SellIcon) //1.8.7
                                Url = _instance.GetText(_instance.config.SellIconUrl, "image", player)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.7 0.6",  //Second digit = Hight Done. First Digit = Position on screen from left to right.
                                AnchorMax = "0.76 0.65"  //Left to right size for msg
                            }
                        }
                    }
                },
                {
                    new CuiElement // Amount Icon
                    {
                        Parent = ShopOverlayName,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                //Png = _instance.ImageLibrary?.Call<string>("GetImage", AmountIcon2) //1.8.7
                                Url = _instance.GetText(_instance.config.AmountUrl2, "image", player)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.8 0.6",
                                AnchorMax = "0.85 0.65"
                            }
                        }
                    }
                },
                {
                    new CuiLabel //Amount Label
                    {
                        Text = {
                            Text = _instance.GetText(_instance.config.AmountLabel2, "label", player),
                            FontSize = 20,
                            Color = _instance.GetUITextColor(player),
                            Align = TextAnchor.MiddleCenter
                        },
                        RectTransform = {
                            AnchorMin = "0.75 0.6",
                            AnchorMax = "0.9 0.65"
                        }
                    },
                    ShopOverlayName
                },
                {
                    new CuiElement //close button image
                    {
                        Parent = ShopOverlayName,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                //Png = _instance.ImageLibrary?.Call<string>("GetImage", CloseIcon) //1.8.7
                                Url = _instance.GetText(_instance.config.CloseButton, "image", player),  // Adjust position/size
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.45 0.14",
                                AnchorMax = "0.55 0.19"
                            }
                        }
                    }
                },
                {
                    new CuiButton //close button Label
                    {
                        Button = {
                            Close = ShopOverlayName,
                            Color = "0 0 0 0.40" //"1.4 1.4 1.4 0.14"  new
                        },
                        RectTransform = {
                            AnchorMin = "0.45 0.14",  // 0.05 0.1  0.15 0.2
                            AnchorMax = "0.55 0.19"  //second is highit of the box.  12 17
                        },
                        Text = {
                            Text = _instance.GetText(_instance.config.CloseButtonlabel, "label", player), //Added config option Close
                            FontSize = 20,
                            Color = _instance.GetUITextColor(player),
                            Align = TextAnchor.MiddleCenter
                        }
                    },
                    ShopOverlayName
                }
            };
        }

        private readonly CuiLabel shopDescription = new CuiLabel
        {
            Text = {
                Text = "{shopdescription}",
                FontSize = 15,
                //Color = _instance.GetUITextColor(player),  //TODO: Work around this to merge into custom color settings
                Align = TextAnchor.MiddleCenter
            },
            RectTransform = {
                AnchorMin = "0.2 0.7",
                AnchorMax = "0.8 0.75"
            }
        };

        private CuiElementContainer CreateShopItemEntry(ShopItem shopItem, float ymax, float ymin, string shop, string color, bool sell, bool cooldown, BasePlayer player) //add limits, Semi finished
        {
            var container = new CuiElementContainer
            {
                /*{
                    new CuiLabel //Test added for Limits display set amount positioning (Its in the perfect position now!)
                    {
                        Text = {
                            Text = limits,  //rename for limiter config setting
                            FontSize = 15,
                            Color = _instance.GetUITextColor(player),
                            Align = TextAnchor.MiddleCenter
                        },
                        RectTransform = {
                            AnchorMin = $"{(sell ? 0.725 : 0.2)} {ymin}", //Keep location setting
                            AnchorMax = $"{(sell ? 0.5 : 0.5)} {ymax}" //Keep location setting
                        }
                    },
                    ShopContentName
                }, */
                {
                    new CuiLabel  //Buy Price Display's Cost set amount in config
                    {
                        Text = {
                            Text = (sell ? shopItem.SellPrice : shopItem.BuyPrice).ToString(),
                            FontSize = 15,
                            Color = _instance.GetUITextColor(player),
                            Align = TextAnchor.MiddleLeft
                        },
                        RectTransform = {
                            AnchorMin = $"{(sell ? 0.725 : 0.45)} {ymin}",
                            AnchorMax = $"{(sell ? 0.755 : 0.5)} {ymax}"
                        }
                    },
                    ShopContentName
                }
            };

            bool isKitOrCommand = !shopItem.Command.IsNullOrEmpty() || !string.IsNullOrEmpty(shopItem.KitName);

            int[] maxSteps = steps;

            if (isKitOrCommand)
            {
                maxSteps = new[] { 1 };
            }

            if (cooldown)
            {
                return container;
            }

            for (var i = 0; i < maxSteps.Length; i++)
            {
                container.Add(new CuiButton
                {
                    Button = {
                        Command = $"shop.{(sell ? "sell" : "buy")} {shop} {shopItem.DisplayName} {maxSteps[i]}",
                        Color = color
                    },
                    RectTransform = {
                        AnchorMin = $"{(sell ? 0.775 : 0.5) + i * 0.03 + 0.001} {ymin}",
                        AnchorMax = $"{(sell ? 0.805 : 0.53) + i * 0.03 - 0.001} {ymax}"
                    },
                    Text = {
                        Text = maxSteps[i].ToString(),
                        FontSize = 15,
                        Color = _instance.GetUITextColor(player),
                        Align = TextAnchor.MiddleCenter
                    }
                }, ShopContentName);
            }

            if (!isKitOrCommand && !(!sell && shopItem.BuyCooldown > 0 || sell && shopItem.SellCooldown > 0))  //Disables buy buttons currently for Classic UI.
            {
                container.Add(new CuiButton
                {
                    Button = {
                        Command = $"shop.{(sell ? "sell" : "buy")} {shop} {shopItem.DisplayName} all",
                        Color = color
                    },
                    RectTransform = {
                        AnchorMin = $"{(sell ? 0.775 : 0.5) + maxSteps.Length * 0.03 + 0.001} {ymin}",
                        AnchorMax = $"{(sell ? 0.805 : 0.53) + maxSteps.Length * 0.03 - 0.001} {ymax}"
                    },
                    Text = {
                        Text = "All",  //All button
                        FontSize = 15,
                        Color = _instance.GetUITextColor(player),
                        Align = TextAnchor.MiddleCenter
                    }
                }, ShopContentName);
            }

            return container;
        }

        private CuiElementContainer CreateShopItemIcon(string name, float ymax, float ymin, ShopItem data, BasePlayer player)
        {
            string url = (string)ImageLibrary?.Call("GetImage", data.Shortname);
            //Puts("{0} {1} Image is null", url == null, data.Shortname);

            var label = new CuiLabel
            {
                Text = {
                    Text = name,
                    FontSize = 15,
                    Color = _instance.GetUITextColor(player),
                    Align = TextAnchor.MiddleLeft
                },
                RectTransform = {
                    AnchorMin = $"0.1 {ymin}",
                    AnchorMax = $"0.3 {ymax}"
                }
            };

            var rawImage = new CuiRawImageComponent(); //Updated

            if (!string.IsNullOrEmpty(url))
            {
                rawImage.Png = url;
            }
            else
            {
                if (string.IsNullOrEmpty(data.Image))
                {
                    url = string.Format(config.IconUrl, data.Image);
                }
                else
                {
                    url = data.Image;
                }
                rawImage.Url = url;
            }

            var container = new CuiElementContainer
            {
                {
                    label,
                    ShopContentName
                },
                new CuiElement
                {
                    Parent = ShopContentName,
                    Components =
                    {
                        rawImage,
                        new CuiRectTransformComponent {AnchorMin = $"0.05 {ymin}", AnchorMax = $"0.08 {ymax}"}
                    }
                }
            };
            return container;
        }

        private static CuiElementContainer CreateShopColorChanger(string currentshop, BasePlayer player) //1.8.8 updating UI
        {
            CuiElementContainer container = new CuiElementContainer
            {
                {
                    new CuiLabel
                    {
                        Text = {
                            Text = "Personal UI Settings",  //Updated maade config outputs
                            FontSize = 15,
                            Color = _instance.GetUITextColor(player),
                            Align = TextAnchor.MiddleCenter
                        },
                        RectTransform = {
                            AnchorMin = "0.18 0.11",
                            AnchorMax = "0.33 0.15"
                        }
                    },
                    ShopOverlayName
                },
                {
                    new CuiButton //set button 1 + color
                    {
                        Button =
                        {
                            Command = $"shop.colorsetting Text {currentshop}",
                            Close = ShopOverlayName,
                            Color = _instance.GetSettingTypeToChange("Text")
                        },
                        RectTransform = {AnchorMin = "0.10 0.09", AnchorMax = "0.17 0.12"},
                        Text =
                        {
                            Text = "Set Text Color",
                            FontSize = 15,
                            Color = _instance.GetUITextColor(player),
                            Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-regular.ttf"
                        }
                    },
                    ShopOverlayName,
                    "Set Text Color"
                },
                {
                    new CuiButton //New Toggle Botton  TODO: Make enable/disable option in config
                    {
                        Button =
                        {
                            Command = $"shop.imageortext {currentshop}",
                            Close = ShopOverlayName,
                            Color = "0 0 0 0"
                        },
                        RectTransform = {AnchorMin = "0.06 0.09", AnchorMax = "0.10 0.12"},
                        Text =
                        {
                            Text = "Toggle",
                            FontSize = 15,
                            Color = _instance.GetUITextColor(player),
                            Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-regular.ttf"
                        }
                    },
                    ShopOverlayName,
                    "Toggle"
                },
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"shop.uicolor {HexToColor(_instance.config.ColorsUI[0])} {currentshop}",
                            Close = ShopOverlayName,
                            Color = $"{HexToColor(_instance.config.ColorsUI[0])} 0.9"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.18 0.07",
                            AnchorMax = "0.21 0.11"
                        },
                        Text =
                        {
                            Text = "",// FontSize = 20, Align = TextAnchor.MiddleCenter,
                            //Font = "robotocondensed-regular.ttf"
                        }
                    },
                    ShopOverlayName,
                    "Purple"
                },
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"shop.uicolor {HexToColor(_instance.config.ColorsUI[1])} {currentshop}",
                            Close = ShopOverlayName,
                            Color = $"{HexToColor(_instance.config.ColorsUI[1])} 0.9"
                        },
                        RectTransform = {AnchorMin = "0.22 0.07", AnchorMax = "0.25 0.11"},
                        Text =
                        {
                            Text = "",// FontSize = 20, Align = TextAnchor.MiddleCenter,
                            //Font = "robotocondensed-regular.ttf"
                        }
                    },
                    ShopOverlayName,
                    "Green"
                },
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"shop.uicolor {HexToColor(_instance.config.ColorsUI[2])} {currentshop}",
                            Close = ShopOverlayName,
                            Color = $"{HexToColor(_instance.config.ColorsUI[2])} 0.9"
                        },
                        RectTransform = {
                            AnchorMin = "0.26 0.07",
                            AnchorMax = "0.29 0.11"
                            },
                        Text =
                        {
                            Text = "",// FontSize = 20, Align = TextAnchor.MiddleCenter,
                            //Font = "robotocondensed-regular.ttf"
                        }
                    },
                    ShopOverlayName,
                    "Orange"
                },
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"shop.uicolor {HexToColor(_instance.config.ColorsUI[3])} {currentshop}",
                            Close = ShopOverlayName,
                            Color = $"{HexToColor(_instance.config.ColorsUI[3])} 0.9"
                        },
                        RectTransform = {AnchorMin = "0.30 0.07", AnchorMax = "0.33 0.11"},
                        Text =
                        {
                            Text = "",// FontSize = 20, Align = TextAnchor.MiddleCenter,
                            //Font = "robotocondensed-regular.ttf"
                        }
                    },
                    ShopOverlayName,
                    "DarkBlue"
                },
                {
                    new CuiButton //set button 3
                    {
                        Button =
                        {
                            Command = $"shop.colorsetting Sell {currentshop}",
                            Close = ShopOverlayName,
                            Color = _instance.GetSettingTypeToChange("Sell")
                        },
                        RectTransform = {AnchorMin = "0.10 0.05", AnchorMax = "0.17 0.08"},
                        Text =
                        {
                            Text = "Sell Color",
                            FontSize = 15,
                            Align = TextAnchor.MiddleCenter,
                            Color = _instance.GetUITextColor(player),
                            Font = "robotocondensed-regular.ttf"
                        }
                    },
                    ShopOverlayName,
                    "Sell Changer"
                },
                {
                    new CuiButton //set button 2
                    {
                        Button =
                        {
                            Command = $"shop.colorsetting Buy {currentshop}",
                            Close = ShopOverlayName,
                            Color = _instance.GetSettingTypeToChange("Buy")
                        },
                        RectTransform = {AnchorMin = "0.10 0.02", AnchorMax = "0.17 0.05"},
                        Text =
                        {
                            Text = "Buy Color",
                            FontSize = 15,
                            Align = TextAnchor.MiddleCenter,
                            Color = _instance.GetUITextColor(player),
                            Font = "robotocondensed-regular.ttf"
                        }
                    },
                    ShopOverlayName,
                    "Buy Changer"
                },
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"shop.uicolor {HexToColor(_instance.config.ColorsUI[4])} {currentshop}",
                            Close = ShopOverlayName,
                            Color = $"{HexToColor(_instance.config.ColorsUI[4])} 0.9"
                        },
                        RectTransform = {AnchorMin = "0.18 0.02", AnchorMax = "0.21 0.06"},
                        Text =
                        {
                            Text = "",// FontSize = 20, Align = TextAnchor.MiddleCenter,
                            //Font = "robotocondensed-regular.ttf"
                        }
                    },
                    ShopOverlayName,
                    "Red"
                },
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"shop.uicolor {HexToColor(_instance.config.ColorsUI[5])} {currentshop}",
                            Close = ShopOverlayName,
                            Color = $"{HexToColor(_instance.config.ColorsUI[5])} 0.9"
                        },
                        RectTransform = {AnchorMin = "0.22 0.02", AnchorMax = "0.25 0.06"},
                        Text =
                        {
                            Text = "",// FontSize = 20, Align = TextAnchor.MiddleCenter,
                            //Font = "robotocondensed-regular.ttf"
                        }
                    },
                    ShopOverlayName,
                    "Yellow"
                },
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"shop.uicolor {HexToColor(_instance.config.ColorsUI[6])} {currentshop}",
                            Close = ShopOverlayName,
                            Color = $"{HexToColor(_instance.config.ColorsUI[6])} 0.9"
                        },
                        RectTransform = {AnchorMin = "0.26 0.02", AnchorMax = "0.29 0.06"},
                        Text =
                        {
                            Text = "",// FontSize = 20, Align = TextAnchor.MiddleCenter,
                            //Font = "robotocondensed-regular.ttf"
                        }
                    },
                    ShopOverlayName,
                    "White"
                },
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"shop.uicolor {HexToColor(_instance.config.ColorsUI[7])} {currentshop}",
                            Close = ShopOverlayName,
                            Color = $"{HexToColor(_instance.config.ColorsUI[7])} 0.9"
                            //Color = "1 1 1 0.98"
                        },
                        RectTransform = {AnchorMin = "0.30 0.02", AnchorMax = "0.33 0.06"},
                        Text =
                        {
                            Text = "",// FontSize = 20, Align = TextAnchor.MiddleCenter,
                            //Font = "robotocondensed-regular.ttf"
                        }
                    },
                    ShopOverlayName,
                    "LightBlue"
                },
                {
                    new CuiLabel //Display Bar
                    {
                        Text = {
                            Text = "ⅢⅢⅢⅢⅢⅢⅢⅢ",
                            Color = _instance.GetUITextColor(player),
                            FontSize = 20,
                            Align = TextAnchor.MiddleCenter
                        },
                        RectTransform = {
                            AnchorMin = "0.80 0.19",
                            AnchorMax = $"{0.80 + _instance.AnchorBarMath(player)} 0.24"
                        }
                    },
                    ShopOverlayName
                },
                {
                    new CuiElement
                    {
                        Parent = ShopOverlayName,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Url = _instance.GetText("https://imgur.com/qx9syT5.png", "image", player)  // More transparency Arrow
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.85 0.14",
                                AnchorMax = "0.90 0.19"
                            }
                        }
                    }
                },
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"shop.transparency increase  {currentshop}",
                            Close = ShopOverlayName,
                            Color = "0 0 0 0.40"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.85 0.14",
                            AnchorMax = "0.90 0.19"
                        },
                        Text =
                        {
                            Text = _instance.GetText(">>", "label", player),
                            Color = _instance.GetUITextColor(player),
                            FontSize = 30, Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-regular.ttf"
                        }
                    },
                    ShopOverlayName,
                    "ButtonMore"
                },
                {
                    new CuiElement
                    {
                        Parent = ShopOverlayName,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Url = _instance.GetText("https://imgur.com/zNKprM1.png", "image", player) // Less transparency Arrow
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.80 0.14",
                                AnchorMax = "0.85 0.19"
                            }
                        }
                    }
                },
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"shop.transparency decrease {currentshop}",
                            Close = ShopOverlayName,
                            Color = "0 0 0 0.40"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.80 0.14",
                            AnchorMax = "0.85 0.19"
                        },
                        Text =
                        {
                            Text = _instance.GetText("<<", "label", player),
                            Color = _instance.GetUITextColor(player),
                            FontSize = 30, Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-regular.ttf"
                        }
                    },
                    ShopOverlayName,
                    "ButtonLess"
                }
            };

            return container;
        }

        private static CuiElementContainer CreateShopChangePage(string currentshop, int shoppageminus, int shoppageplus, bool npcVendor, string npcID, BasePlayer player) //OG Latest
        {
            CuiElementContainer container = new CuiElementContainer
            {
                {
                    new CuiElement
                    {
                        Parent = ShopOverlayName,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Url = _instance.GetText("https://imgur.com/zNKprM1.png", "image", player)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.345 0.14",
                                AnchorMax = "0.445 0.19"
                            }
                        }
                    }
                },
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"shop.show {currentshop} {shoppageminus}",
                            Color = "0 0 0 0.40"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.345 0.14",
                            AnchorMax = "0.445 0.19"
                        },
                        Text =
                        {
                            Text = _instance.GetText("<<", "label", player),
                            Color = _instance.GetUITextColor(player),
                            FontSize = 30,
                            Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-regular.ttf"
                        }
                    },
                    ShopOverlayName,
                    "ButtonBack"
                },
                {
                    new CuiElement
                    {
                        Parent = ShopOverlayName,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Url = _instance.GetText("https://imgur.com/qx9syT5.png", "image", player)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.555 0.14",
                                AnchorMax = "0.655 0.19"
                            }
                        }
                    }
                },
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"shop.show {currentshop} {shoppageplus}",
                            Color = "0 0 0 0.40"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.555 0.14",
                            AnchorMax = "0.655 0.19"
                        },
                        Text =
                        {
                            Text = _instance.GetText(">>", "label", player),
                            Color = _instance.GetUITextColor(player),
                            FontSize = 30,
                            Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-regular.ttf"
                        }
                    },
                    ShopOverlayName,
                    "ButtonForward"
                }
            };

            int rowPos = 0;

            if (npcVendor)
            {
                foreach (ShopCategory cat in _instance.config.ShopCategories.Values.Where(i => i.EnableNPC && i.NPCId == npcID))
                {
                    CreateTab(ref container, cat, shoppageminus, rowPos, player);

                    rowPos++;
                }
            }
            else
            {
                foreach (ShopCategory cat in _instance.config.ShopCategories.Values.Where(i => i.EnabledCategory && !i.EnableNPC)) //Updated 1.8.2
                {
                    CreateTab(ref container, cat, shoppageminus, rowPos, player);

                    rowPos++;
                }
            }

            return container;
        }

        private static void CreateTab(ref CuiElementContainer container, ShopCategory cat, int shoppageminus, int rowPos, BasePlayer player) //Button-Shop Tab generator
        {
            container.Add(new CuiButton
            {
                Button =
                {
                    Command = $"shop.show {cat.DisplayName} {shoppageminus}",
                    Color = "0.5 0.5 0.5 0.5"  //"1.2 1.2 1.2 0.24" new
                },
                RectTransform =
                {
                    AnchorMin = $"{(0.09 + (rowPos * 0.056))} 0.78", // * 0.056 = Margin for more buttons... less is better
                    AnchorMax = $"{(0.14 + (rowPos * 0.056))} 0.82"
                },
                Text =
                {
                    Text = cat.DisplayName,
                    Align = TextAnchor.MiddleCenter,
                    Color = _instance.GetUITextColor(player),
                    Font = "robotocondensed-regular.ttf",
                    FontSize = 12
                }
            }, ShopOverlayName, cat.DisplayName);
        }

        private void DestroyUi(BasePlayer player, bool full = false)
        {
            CuiHelper.DestroyUi(player, ShopContentName);
            CuiHelper.DestroyUi(player, "ButtonForward");
            CuiHelper.DestroyUi(player, "ButtonBack");
            if (!full) return;
            CuiHelper.DestroyUi(player, ShopDescOverlay);
            CuiHelper.DestroyUi(player, ShopOverlayName);
        }
        #endregion

        #region Shop
        private void ShowShop(BasePlayer player, string shopid, int from = 0, bool fullPaint = true, bool refreshMoney = false)
        {
            shopPage[player.userID] = from;

            ShopCategory shop;

            if (config.ShopCategories.Where(x => !x.Value.EnableNPC).Count() <= 0) //added for when all shops are disabled for global
            {
                SendReply(player, GetMessage("MessageErrorGlobalDisabled"));
                return;
            }

            if (!config.ShopCategories.TryGetValue(shopid, out shop))
            {
                SendReply(player, GetMessage("MessageErrorNoShop", player.UserIDString));

                return;
            }

            if (config.CurrencySwitcher == true && ServerRewards == null)
            {
                SendReply(player, GetMessage("MessageShowNoServerRewards", player.UserIDString));
                return;
            }

            if (config.CurrencySwitcher == false && Economics == null)
            {
                SendReply(player, GetMessage("MessageShowNoEconomics", player.UserIDString));
                return;
            }

            if (config.BlockMonuments && !shop.EnableNPC && IsNearMonument(player))
            {
                SendReply(player, GetMessage("BlockedMonuments", player.UserIDString));
                return;
            }


            double playerCoins = 0;

            if (config.CurrencySwitcher == true)
            {
                playerCoins = (double)ServerRewards?.Call<int>("CheckPoints", player.UserIDString);
            }
            else
            {
                playerCoins = (double)Economics.CallHook("Balance", player.UserIDString);
            }

            shopDescription.Text.Text = string.Format(shop.Description, playerCoins);

            if (refreshMoney)
            {
                CuiHelper.DestroyUi(player, ShopDescOverlay);

                CuiHelper.AddUi(player, new CuiElementContainer { { shopDescription, ShopOverlayName, ShopDescOverlay } });
            }

            DestroyUi(player, fullPaint);

            CuiElementContainer container;

            if (fullPaint)
            {
                container = CreateShopOverlay(shop.DisplayName, player);

                container.Add(shopDescription, ShopOverlayName, ShopDescOverlay);
            }
            else
                container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = "0 0.2",
                    AnchorMax = "1 0.6"
                }
            }, ShopOverlayName, ShopContentName);

            if (from < 0)
            {
                CuiHelper.AddUi(player, container);
                return;
            }

            int current = 0;

            List<ShopItem> shopItems = new List<ShopItem>(); //This section allows the user to re-order the items in shop based on index location.

            foreach (var shortname in shop.Items)
            {
                if (!config.ShopItems.ContainsKey(shortname)) continue;

                ShopItem shopItem = config.ShopItems[shortname];

                shopItems.Add(shopItem);
            }

            foreach (ShopItem data in shopItems)
            {
                if (current >= from && current < from + 7)
                {
                    float pos = 0.85f - 0.125f * (current - from);

                    string name = data.DisplayName;

                    string cooldowndescription = string.Empty;

                    double sellCooldown;
                    double buyCooldown;

                    bool hasSellCooldown = data.SellCooldown > 0 && HasSellCooldown(player.userID, data.DisplayName, out sellCooldown);
                    bool hasBuyCooldown = data.BuyCooldown > 0 && HasBuyCooldown(player.userID, data.DisplayName, out buyCooldown);

                    bool cooldown = data.BuyCooldown > 0 || data.SellCooldown > 0;

                    if (data.BuyCooldown > 0)
                    {
                        cooldowndescription += $" (Buy CoolDown: {FormatTime((long)data.BuyCooldown)})";
                    }

                    if (data.SellCooldown > 0)
                    {
                        cooldowndescription += $" (Sell CoolDown: {FormatTime((long)data.SellCooldown)})";
                    }

                    name = string.Format("{0}<size=10>{1}</size>", name, (cooldown ? "\n" + cooldowndescription : "")); //added Updated,  Creates new line for cooldowns under the Displayed Item Names.

                    container.AddRange(CreateShopItemIcon(name, pos + 0.125f, pos, data, player));

                    bool buyed = false;

                    if (hasBuyCooldown)
                    {
                        buyed = true;

                        container.Add(new CuiLabel
                        {
                            Text =
                            {
                                Text = data.BuyPrice.ToString(),
                                FontSize = 15,
                                Color = _instance.GetUITextColor(player),
                                Align = TextAnchor.MiddleLeft
                            },
                            RectTransform = {
                                AnchorMin = $"0.45 {pos}",
                                AnchorMax = $"0.5 {pos + 0.125f}"
                            }
                        }, ShopContentName);
                    }

                    if (!buyed && data.EnableBuy)
                        container.AddRange(CreateShopItemEntry(data, pos + 0.125f, pos, shopid, GetUIBuyBoxColor(player), false, hasBuyCooldown, player));

                    if (data.EnableSell)
                        container.AddRange(CreateShopItemEntry(data, pos + 0.125f, pos, shopid, GetUISellBoxColor(player), true, hasSellCooldown, player));
                }

                current++;
            }

            int minfrom = from <= 7 ? 0 : from - 7;

            int maxfrom = from + 7 >= current ? from : from + 7;

            container.AddRange(CreateShopChangePage(shopid, minfrom, maxfrom, shop.EnableNPC, shop.NPCId, player));

            if(permission.UserHasPermission(player.UserIDString, Vip) || config.PersonalUI)
            {
                container.AddRange(CreateShopColorChanger(shopid, player)); //1.8.8 updating UI
            }
            if (!config.PersonalUI)
            {
                foreach(var DataEntry in playerdata)
                {
                    switch (DataEntry.ImageOrText)
                    {
                        case true:
                            DataEntry.ImageOrText = false;
                            break;
                    }
                }
            }
            if(!permission.UserHasPermission(player.UserIDString, Vip) && !config.PersonalUI)
            {
                foreach (var DataEntry in playerdata)
                {
                    if (DataEntry.playerID != player.UserIDString) continue;
                    switch (DataEntry.ImageOrText)
                    {
                        case true:
                            DataEntry.ImageOrText = false;
                            break;
                    }
                }
            }

            CuiHelper.AddUi(player, container);
        }

        double GetFactor(ShopItem data)
        {
            if (data.DisplayName == null)
            {
                return 1;
            }

            string itemname = data.DisplayName;
            ulong buy;
            if (!buyed.TryGetValue(itemname, out buy))
            {
                buy = 1;
            }

            ulong sell;
            if (!selled.TryGetValue(itemname, out sell))
            {
                sell = 1;
            }

            return Math.Min(Math.Max(buy / (double)sell, .25), 4);
        }

        object CanDoAction(BasePlayer player, string shop, string item, string ttype)
        {
            if (!config.ShopCategories.ContainsKey(shop))
            {
                return Lang("MessageErrorNoActionShop", player.UserIDString, ttype);
            }

            if (!config.ShopItems.ContainsKey(item))
            {
                return Lang("MessageErrorItemNotFound", player.UserIDString);
            }

            if (!config.ShopItems[item].EnableBuy && ttype == "buy")
            {
                return Lang("MessageErrorItemNotEnabled", player.UserIDString, ttype);
            }

            if (!config.ShopItems[item].EnableSell && ttype == "sell")
            {
                return Lang("MessageErrorItemNotEnabled", player.UserIDString, ttype);
            }

            return true;
        }

        object CanShop(BasePlayer player, string shopname)
        {
            if (!config.ShopCategories.ContainsKey(shopname))
            {
                return GetMessage("MessageErrorNoShop", player.UserIDString);
            }

            return true;
        }

        #region Buy
        object TryShopBuy(BasePlayer player, string shop, string item, int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            object success = CanShop(player, shop);

            if (success is string)
            {
                return success;
            }

            success = CanDoAction(player, shop, item, "buy");

            if (success is string)
            {
                return success;
            }

            success = CanBuy(player, item, amount);

            if (success is string)
            {
                return success;
            }
            //Puts(item);

            success = TryGive(player, item, amount);

            if (success is string)
            {
                return success;
            }

            ShopItem data = config.ShopItems[item];

            object tryShopBuy = false;

            if (config.CurrencySwitcher == true)
            {
                tryShopBuy = ServerRewards?.Call("TakePoints", player.UserIDString, (int)(data.BuyPrice * amount));
                if (tryShopBuy == null || tryShopBuy is bool && !(bool)tryShopBuy)
                {
                    return GetMessage("MessageShowNoServerRewards", player.UserIDString);
                }
            }

            if (config.CurrencySwitcher == false)
            {
                tryShopBuy = Economics?.CallHook("Withdraw", player.UserIDString, data.BuyPrice * amount);
                if (tryShopBuy == null || tryShopBuy is bool && !(bool)tryShopBuy)
                {
                    return GetMessage("MessageShowNoEconomics", player.UserIDString);
                }
            }

            if (data.BuyCooldown > 0)
            {
                Dictionary<string, double> itemCooldowns;

                if (!buyCooldowns.TryGetValue(player.userID, out itemCooldowns))
                {
                    buyCooldowns[player.userID] = itemCooldowns = new Dictionary<string, double>();
                }

                itemCooldowns[item] = CurrentTime() + data.BuyCooldown /* *amount */;
            }

            if (!string.IsNullOrEmpty(data.DisplayName)) //updated
            {
                ulong count;

                buyed.TryGetValue(data.DisplayName, out count);

                buyed[data.DisplayName] = count + (ulong)amount;
            }

            return tryShopBuy is bool && (bool)tryShopBuy;
        }

        object TryGive(BasePlayer player, string item, int amount)
        {
            ShopItem data = config.ShopItems[item];

            if (!data.Command.IsNullOrEmpty())  //updated 1.8.5
            {
                Vector3 pos = player.ServerPosition + player.eyes.HeadForward() * 3.5f;
                pos.y = TerrainMeta.HeightMap.GetHeight(pos);

                foreach (var command in data.Command)
                {
                    var c = command
                        .Replace("$player.id", player.UserIDString)
                        .Replace("$player.name", player.displayName)
                        .Replace("$player.x", pos.x.ToString())
                        .Replace("$player.y", pos.y.ToString())
                        .Replace("$player.z", pos.z.ToString());

                    if (c.StartsWith("shop.show close", StringComparison.OrdinalIgnoreCase))
                        NextTick(() => ConsoleSystem.Run(ConsoleSystem.Option.Server, c));
                    else
                        ConsoleSystem.Run(ConsoleSystem.Option.Server, c);

                }
                Puts("Player: {0} Bought command: {1}", player.displayName, item);
            }

            else if (!string.IsNullOrEmpty(data.KitName))
            {
                object isKit = Kits?.CallHook("isKit", data.KitName);

                if (isKit is bool && (bool)isKit)
                {
                    object successkit = Kits.CallHook("GiveKit", player, data.KitName);

                    if (successkit is bool && !(bool)successkit)
                    {
                        return GetMessage("MessageErrorRedeemKit", player.UserIDString);
                    }

                    Puts("Player: {0} Bought Kit: {1}", player.displayName, data.Shortname);

                    return true;
                }
            }

            else if (!string.IsNullOrEmpty(data.Shortname))
            {
                if (player.inventory.containerMain.IsFull() && player.inventory.containerBelt.IsFull())
                {
                    return GetMessage("MessageErrorInventoryFull", player.UserIDString);
                }

                object success = GiveItem(player, data, amount);

                if (success is string)
                {
                    return success;
                }

                Puts("Player: {0} Bought Item: {1} x{2}", player.displayName, data.Shortname, amount);
            }

            return true;
        }

        private int FreeSlots(BasePlayer player)
        {
            var slots = player.inventory.containerMain.capacity + player.inventory.containerBelt.capacity;
            var taken = player.inventory.containerMain.itemList.Count + player.inventory.containerBelt.itemList.Count;
            return slots - taken;
        }

        private List<int> GetStacks(ItemDefinition item, int amount)
        {
            var list = new List<int>();
            var maxStack = item.stackable;

            while (amount > maxStack)
            {
                amount -= maxStack;
                list.Add(maxStack);
            }

            list.Add(amount);

            return list;
        }

        private int GetAmountBuy(BasePlayer player, string item)
        {
            if (player.inventory.containerMain.IsFull() && player.inventory.containerBelt.IsFull())
            {
                return 0;
            }

            ShopItem data = config.ShopItems[item];
            ItemDefinition definition = ItemManager.FindItemDefinition(data.Shortname);
            if (definition == null)
            {
                return 0;
            }

            var freeSlots = FreeSlots(player);

            return freeSlots * definition.stackable;
        }

        private object GiveItem(BasePlayer player, ShopItem data, int amount)
        {
            if (amount <= 0)
            {
                return GetMessage("MessageErrorNotNothing", player.UserIDString);
            }

            ItemDefinition definition = ItemManager.FindItemDefinition(data.Shortname);
            if (definition == null)
            {
                return GetMessage("MessageErrorItemNoExist", player.UserIDString);
            }

            var stack = GetStacks(definition, amount);
            var stacks = stack.Count;

            var slots = FreeSlots(player);
            if (slots < stacks)
            {
                return Lang("MessageErrorInventorySlots", player.UserIDString, stacks);
            }

            var quantity = (int)Math.Ceiling(amount / (float)stacks);
            //Puts(data.SkinId.ToString());
            for (var i = 0; i < stacks; i++)
            {
                var item = ItemManager.CreateByItemID(definition.itemid, quantity, data.SkinId);
                if (!player.inventory.GiveItem(item))
                {
                    item.Remove(0);
                }
            }

            return true;
        }

        object CanBuy(BasePlayer player, string item, int amount)
        {

            if (config.CurrencySwitcher == true && ServerRewards == null)
            {
                return GetMessage("MessageShowNoServerRewards", player.UserIDString);
            }

            if (config.CurrencySwitcher == false && Economics == null)
            {
                return GetMessage("MessageShowNoEconomics", player.UserIDString);
            }

            if (!config.ShopItems.ContainsKey(item))
            {
                return GetMessage("MessageErrorItemNoValid", player.UserIDString);
            }

            var data = config.ShopItems[item];
            if (data.BuyPrice < 0)
            {
                return GetMessage("MessageErrorBuyPrice", player.UserIDString);
            }

            if (data.Command != null && amount > 1)
            {
                return GetMessage("MessageErrorBuyCmd", player.UserIDString);
            }

            double buyprice = data.BuyPrice;
            double playerCoins = 0;

            if (config.CurrencySwitcher == true)
            {
                playerCoins = (double)ServerRewards?.Call<int>("CheckPoints", player.UserIDString);
                if (playerCoins < buyprice * amount)
                {
                    return Lang("MessageErrorNotEnoughMoney", player.UserIDString, buyprice * amount, amount, item);
                }
            }

            if (config.CurrencySwitcher == false)
            {
                playerCoins = (double)Economics.CallHook("Balance", player.UserIDString);
                if (playerCoins < buyprice * amount)
                {
                    return Lang("MessageErrorNotEnoughMoney", player.UserIDString, buyprice * amount, amount, item);
                }
            }

            if (data.BuyCooldown > 0)
            {
                Dictionary<string, double> itemCooldowns;
                double itemCooldown;

                if (buyCooldowns.TryGetValue(player.userID, out itemCooldowns) && itemCooldowns.TryGetValue(item, out itemCooldown) && itemCooldown > CurrentTime())
                {
                    return Lang("MessageErrorCooldown", player.UserIDString, FormatTime((long)(itemCooldown - CurrentTime())));
                }
            }

            return true;
        }
        #endregion
        #endregion

        #region Sell
        object TryShopSell(BasePlayer player, string shop, string item, int amount)
        {
            object success = CanShop(player, shop);
            if (success is string)
            {
                return success;
            }

            success = CanDoAction(player, shop, item, "sell");
            if (success is string)
            {
                return success;
            }

            success = CanSell(player, item, amount);
            if (success is string)
            {
                return success;
            }

            success = TrySell(player, item, amount);
            if (success is string)
            {
                return success;
            }

            ShopItem data = config.ShopItems[item];
            ShopItem itemdata = config.ShopItems[item];
            double cooldown = Convert.ToDouble(itemdata.SellCooldown);

            if (cooldown > 0)
            {
                Dictionary<string, double> itemCooldowns;

                if (!sellCooldowns.TryGetValue(player.userID, out itemCooldowns))
                {
                    sellCooldowns[player.userID] = itemCooldowns = new Dictionary<string, double>();
                }

                itemCooldowns[item] = CurrentTime() + cooldown;
            }

            if (config.CurrencySwitcher == true)
            {
                ServerRewards?.Call("AddPoints", player.UserIDString, (int)(data.SellPrice * amount));
            }

            if (config.CurrencySwitcher == false)
            {
                Economics?.CallHook("Deposit", player.UserIDString, data.SellPrice * amount);
            }

            if (!string.IsNullOrEmpty(data.DisplayName))
            {
                ulong count;

                selled.TryGetValue(data.DisplayName, out count);

                selled[data.DisplayName] = count + (ulong)amount;
            }

            return true;
        }

        object TrySell(BasePlayer player, string item, int amount)
        {
            ShopItem data = config.ShopItems[item];

            if (string.IsNullOrEmpty(data.DisplayName))
            {
                return GetMessage("MessageErrorItemItem", player.UserIDString);
            }

            if (!data.Command.IsNullOrEmpty())
            {
                return GetMessage("CantSellCommands", player.UserIDString);
            }

            object iskit = Kits?.CallHook("isKit", data.Shortname);

            if (iskit is bool && (bool)iskit)
            {
                return GetMessage("CantSellKits", player.UserIDString);
            }

            object success = TakeItem(player, data, amount);
            if (success is string)
            {
                return success;
            }

            Puts("Player: {0} Sold Item: {1} x{2}", player.displayName, data.Shortname, amount);

            return true;
        }

        private int GetAmountSell(BasePlayer player, string item)
        {
            ShopItem data = config.ShopItems[item];

            ItemDefinition definition = ItemManager.FindItemDefinition(data.Shortname);

            if (definition == null)
            {
                return 0;
            }

            return player.inventory.GetAmount(definition.itemid);
        }

        private object TakeItem(BasePlayer player, ShopItem data, int amount)
        {
            if (amount <= 0)
            {
                return GetMessage("MessageErrorNotEnoughSell", player.UserIDString);
            }

            ItemDefinition definition = ItemManager.FindItemDefinition(data.Shortname);

            if (definition == null)
            {
                return GetMessage("MessageErrorItemNoExistTake", player.UserIDString);
            }

            int pamount = player.inventory.GetAmount(definition.itemid);

            if (pamount < amount)
            {
                return GetMessage("MessageErrorNotEnoughSell", player.UserIDString);
            }

            player.inventory.Take(null, definition.itemid, amount);

            return true;
        }

        object CanSell(BasePlayer player, string item, int amount)
        {

            if (!config.ShopItems.ContainsKey(item))
            {
                return GetMessage("MessageErrorItemNoValid", player.UserIDString);
            }

            ShopItem itemdata = config.ShopItems[item];

            if (player.inventory.containerMain.FindItemsByItemName(itemdata.Shortname) == null && player.inventory.containerBelt.FindItemsByItemName(itemdata.Shortname) == null) //fixed..
            {
                return GetMessage("MessageErrorNotEnoughSell", player.UserIDString);
            }

            if (itemdata.SellPrice < 0)
            {
                return GetMessage("MessageErrorSellPrice", player.UserIDString);
            }

            if (itemdata.SellCooldown > 0)
            {
                Dictionary<string, double> itemCooldowns;

                double itemCooldown;

                if (sellCooldowns.TryGetValue(player.userID, out itemCooldowns) && itemCooldowns.TryGetValue(item, out itemCooldown) && itemCooldown > CurrentTime())
                {
                    return Lang("MessageErrorCooldown", player.UserIDString, FormatTime((long)(itemCooldown - CurrentTime())));
                }
            }

            return true;
        }
        #endregion

        #region Commands

        #region Chat

        private void cmdShop(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, Use))
            {
                return;
            }

            if (!isShopReady)
            {
                SendReply(player, GetMessage("MessageShopResponse", player.UserIDString));
                return;
            }

            ShopCategory category;

            string shopKey;

            if (GetNearestVendor(player, out category))
                shopKey = category.DisplayName;
            else
                shopKey = config.DefaultShop;

            if (!player.CanBuild())
            {
                if (permission.UserHasPermission(player.UserIDString, BlockAllow)) //Overrides being blocked.
                    ShowShop(player, config.DefaultShop);
                else
                    SendReply(player, GetMessage("MessageErrorBuildingBlocked", player.UserIDString));

                return;
            }
            ImageChanger = config.UIImageOption;
            ShopPlayer = player;
            PlayerUIOpen.Add(player.UserIDString);
            ShowShop(player, shopKey);
        }
        [ChatCommand("cleardata")]
        private void cmdClearData(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, Admin))
            {
                playerdata.Clear();

                Puts($"{player.userID} has cleared the data in the GUI Shop file");
            }
        }

        #endregion

        #region Console
        [ConsoleCommand("shop.show")] //updated to fix spacing issues in name.
        private void ccmdShopShow(ConsoleSystem.Arg arg)
        {
            if (!arg.HasArgs(2))
            {
                return;
            }

            string shopid = "";
            BasePlayer player = arg.Player();

            for (int i = 0; i <= arg.Args.Length - 1; i++)
            {
                if (i <= arg.Args.Length - 3)
                {
                    var Names = arg.Args[i].Replace("'", "") + " ";
                    shopid = shopid + Names;
                }
                if (i == arg.Args.Length - 2)
                {
                    var LastNames = arg.Args[i].Replace("'", "");
                    shopid = shopid + LastNames;
                }
            }

            if (shopid.Equals("close", StringComparison.OrdinalIgnoreCase))
            {
                BasePlayer targetPlayer = arg.GetPlayerOrSleeper(1);

                DestroyUi(targetPlayer, true);

                return;
            }

            if (player == null || !permission.UserHasPermission(player.UserIDString, Use))
            {
                return;
            }

            ShowShop(player, shopid, arg.GetInt(1), false, true);
        }

        [ConsoleCommand("shop.buy")]
        private void ccmdShopBuy(ConsoleSystem.Arg arg)
        {
            if (!arg.HasArgs(3))
            {
                return;
            }

            BasePlayer player = arg.Player();
            string item = "";
            int amount = 0;

            if (player == null || !permission.UserHasPermission(player.UserIDString, Use))
            {
                return;
            }

            object success = Interface.Oxide.CallHook("canShop", player);

            if (success != null)
            {
                SendReply(player, success as string ?? "You are not allowed to shop at the moment"); //OG Leave...
                return;
            }

            //Puts($"{arg.Args.Length}, works?");
            var ShopNameWords = 1;
            var ShopName = arg.Args[0].Replace("'", "");
            var TempShop = "";
            for (int j = 0; j <= arg.Args.Length - 1; j++)
            {
                var ShopAmountInCat = 0;
                if (j > 0)
                {
                    ShopName = ShopName + arg.Args[j].Replace("'", "");
                    ShopNameWords++;
                }
                foreach (var category in config.ShopCategories)
                {
                    if (category.Value.DisplayName == ShopName)
                    {
                        ShopAmountInCat++;
                        TempShop = ShopName;
                    }

                }

                if (ShopAmountInCat == 1)
                {
                    ShopName = TempShop;
                    break;
                }
                else
                {
                    ShopName = ShopName + " ";
                }
            }
            //Puts(ShopName + " when shop.buy triggered");
            //Puts($"{ShopNameWords}");

            for (int i = 0; i <= arg.Args.Length - 1; i++)
            {
                if (i == ShopNameWords - 1) continue;
                if (i > ShopNameWords - 1 && i <= arg.Args.Length - 3)
                {
                    var Names = arg.Args[i].Replace("'", "") + " ";
                    item = item + Names;
                }
                if (i == arg.Args.Length - 2)
                {
                    var LastNames = arg.Args[i].Replace("'", "");
                    item = item + LastNames;
                }

                if (i == arg.Args.Length - 1)
                {
                    amount = arg.Args[i].Equals("all") ? GetAmountBuy(player, item) : Convert.ToInt32(arg.Args[i]);
                    break;
                }
            }
            //Puts(item + " when shop.buy");

            success = TryShopBuy(player, ShopName, item, amount);

            if (success is string)
            {
                SendReply(player, (string)success);
                return;
            }

            ShopItem shopitem = config.ShopItems.Values.FirstOrDefault(x => x.DisplayName == item);

            SendReply(player, GetMessage("MessageBought", player.UserIDString), amount, shopitem.DisplayName);
            ShowShop(player, ShopName, shopPage[player.userID], false, true);
        }

        [ConsoleCommand("shop.sell")]
        private void ccmdShopSell(ConsoleSystem.Arg arg)
        {
            if (!arg.HasArgs(3))
            {
                return;
            }

            BasePlayer player = arg.Player();

            if (player == null || !permission.UserHasPermission(player.UserIDString, Use))
            {
                return;
            }

            object success = Interface.Oxide.CallHook("canShop", player);

            if (success != null)
            {
                string message = "You are not allowed to shop at the moment";
                if (success is string)
                {
                    message = (string)success;
                }

                SendReply(player, message);
                return;
            }

            string item = "";
            int amount = 0;
            var ShopNameWords = 1;
            var ShopName = arg.Args[0].Replace("'", "");
            var TempShop = "";
            for (int j = 0; j <= arg.Args.Length - 1; j++)
            {
                var ShopAmountInCat = 0;
                if (j > 0)
                {
                    ShopName = ShopName + arg.Args[j].Replace("'", "");
                    ShopNameWords++;
                }
                foreach (var category in config.ShopCategories)
                {
                    if (category.Value.DisplayName == ShopName)
                    {
                        ShopAmountInCat++;
                        TempShop = ShopName;
                    }

                }

                if (ShopAmountInCat == 1)
                {
                    ShopName = TempShop;
                    break;
                }
                else
                {
                    ShopName = ShopName + " ";
                }
            }

            for (int i = 0; i <= arg.Args.Length - 1; i++)
            {
                if (i == ShopNameWords - 1) continue;
                if (i > ShopNameWords - 1 && i <= arg.Args.Length - 3)
                {
                    var Names = arg.Args[i].Replace("'", "") + " ";
                    item = item + Names;
                }
                if (i == arg.Args.Length - 2)
                {
                    var LastNames = arg.Args[i].Replace("'", "");
                    item = item + LastNames;
                }

                if (i == arg.Args.Length - 1)
                {
                    amount = arg.Args[i].Equals("all") ? GetAmountSell(player, item) : Convert.ToInt32(arg.Args[i]);
                    break;
                }
            }

            success = TryShopSell(player, ShopName, item, amount);

            if (success is string)
            {
                SendReply(player, (string)success);
                return;
            }

            ShopItem shopitem = config.ShopItems.Values.FirstOrDefault(x => x.DisplayName == item);

            SendReply(player, GetMessage("MessageSold", player.UserIDString), amount, shopitem.DisplayName);
            ShowShop(player, ShopName, shopPage[player.userID], false, true);
        }

        [ConsoleCommand("shop.transparency")]
        private void ccmdShopTransparency(ConsoleSystem.Arg arg)
        {
            PlayerTransparencyChange(arg.Player(), arg.Args[0]);

            if (!permission.UserHasPermission(arg.Player().UserIDString, Use))
            {
                return;
            }

            if (!arg.Player().CanBuild())
            {
                if (permission.UserHasPermission(arg.Player().UserIDString, BlockAllow)) //Overrides being blocked.
                    ShowShop(arg.Player(), config.DefaultShop);
                else
                    SendReply(arg.Player(), GetMessage("MessageErrorBuildingBlocked", arg.Player().UserIDString));
                return;
            }
            NewConfigInDataFile(arg.Player());
            ShowShop(arg.Player(), arg.Args[1]);

            //Puts($"Console Command has been triggered with arguments {arg.Player()}, {arg.Args[0]}");
        }

        [ConsoleCommand("shop.uicolor")]
        private void ccmdUIColor(ConsoleSystem.Arg arg)
        {
            //Puts($"shop.Color console function gave argument RGB = {arg.Args[0]}, {arg.Args[1]}, {arg.Args[2]}, argument 2 = {arg.Args[3]}");
            if (arg.Args[0] == null || arg.Args[1] == null)
            {
                return;
            }

            PlayerColorTextChange(arg.Player(), arg.Args[0], arg.Args[1], arg.Args[2], UISettingChange);
            if (!permission.UserHasPermission(arg.Player().UserIDString, Use)) //added vip option.
            {
                return;
            }

            if (!arg.Player().CanBuild())
            {
                if (permission.UserHasPermission(arg.Player().UserIDString, BlockAllow)) //Overrides being blocked.
                    ShowShop(arg.Player(), config.DefaultShop);
                else
                    SendReply(arg.Player(), GetMessage("MessageErrorBuildingBlocked", arg.Player().UserIDString));
                return;
            }

            NewConfigInDataFile(arg.Player());
            ShowShop(arg.Player(), arg.Args[3]);
        }

        [ConsoleCommand("shop.colorsetting")]
        private void ccmdUIColorSetting(ConsoleSystem.Arg arg)
        {
            if (arg.Args[0] == null || arg.Args[1] == null)
            {
                return;
            }
            UISettingChange = arg.Args[0];
            if (!permission.UserHasPermission(arg.Player().UserIDString, Use)) //added vip
            {
                return;
            }

            if (!arg.Player().CanBuild())
            {
                if (permission.UserHasPermission(arg.Player().UserIDString, BlockAllow)) //Overrides being blocked.
                    ShowShop(arg.Player(), config.DefaultShop);
                else
                    SendReply(arg.Player(), GetMessage("MessageErrorBuildingBlocked", arg.Player().UserIDString));
                return;
            }
            ShowShop(arg.Player(), arg.Args[1]);
            GetSettingTypeToChange(UISettingChange);
        }

        [ConsoleCommand("shop.imageortext")]
        private void ccmdUIImageOrText(ConsoleSystem.Arg arg)
        {
            if (arg.Args[0] == null)
            {
                return;
            }
            if (!permission.UserHasPermission(arg.Player().UserIDString, Use)) //vip
            {
                return;
            }

            if (!arg.Player().CanBuild())
            {
                if (permission.UserHasPermission(arg.Player().UserIDString, BlockAllow)) //Overrides being blocked.
                    ShowShop(arg.Player(), config.DefaultShop);
                else
                    SendReply(arg.Player(), GetMessage("MessageErrorBuildingBlocked", arg.Player().UserIDString));
                return;
            }

            NewConfigInDataFile(arg.Player());
            SetImageOrText(arg.Player());
            ShowShop(arg.Player(), arg.Args[0]);
        }
        #endregion

        #endregion

        #region CoolDowns
        private static int CurrentTime() => Facepunch.Math.Epoch.Current;

        private bool HasBuyCooldown(ulong userID, string item, out double itemCooldown)
        {
            Dictionary<string, double> itemCooldowns;

            itemCooldown = 0.0;

            return buyCooldowns.TryGetValue(userID, out itemCooldowns) && itemCooldowns.TryGetValue(item, out itemCooldown) && itemCooldown > CurrentTime();
        }

        private bool HasSellCooldown(ulong userID, string item, out double itemCooldown)
        {
            Dictionary<string, double> itemCooldowns;

            itemCooldown = 0.0;

            return sellCooldowns.TryGetValue(userID, out itemCooldowns) && itemCooldowns.TryGetValue(item, out itemCooldown) && itemCooldown > CurrentTime();
        }

        private static string FormatTime(long seconds)
        {
            TimeSpan timespan = TimeSpan.FromSeconds(seconds);

            return string.Format(timespan.TotalHours >= 1 ? "{2:00}:{0:00}:{1:00}" : "{0:00}:{1:00}", timespan.Minutes, timespan.Seconds, Math.Floor(timespan.TotalHours));
        }

        #endregion

        #region UI Colors

        private string GetSettingTypeToChange(string type)
        {
            if (type == UISettingChange)
            {
                return $"{HexToColor("#FFFFFF")} 0.2";
            }
            return $"{HexToColor("#CCD1D1")} 0";
        }

        private void SetImageOrText(BasePlayer player)
        {

            foreach (var DataEntry in playerdata)
            {
                if (DataEntry.playerID != player.UserIDString) continue;
                if (config.PersonalUI)
                {
                    switch (config.UIImageOption)
                    {
                        case true:
                            DataEntry.ImageOrText = true;
                            break;
                        case false:
                            DataEntry.ImageOrText = false;
                            break;
                    }
                    return;
                }
                switch (DataEntry.ImageOrText)
                {
                    case true:
                        DataEntry.ImageOrText = false;
                        break;
                    case false:
                        DataEntry.ImageOrText = true;
                        break;
                }
            }

        }

        private bool GetImageOrText(BasePlayer player)
        {
            foreach (var DataEntry in playerdata)
            {
                if (DataEntry.playerID != player.UserIDString) continue;
                ImageChanger = DataEntry.ImageOrText;
                return ImageChanger;
            }
            return ImageChanger;
        }

        private string GetText(string text, string type, BasePlayer player)
        {

            if (GetImageOrText(player) == true)
            {
                switch (type)
                {
                    case "label":
                        return "";
                    case "image":
                        return text;
                }
            }
            else
            {
                switch (type)
                {
                    case "label":
                        return text;
                    case "image":
                        return "https://i.imgur.com/fL7N8Zf.png"; //Never ever remove or change... if you do GL..
                }
            }
            return "";
        }

        private void SavaPlayerConfigData(PlayerUISetting settings)
        {
            playerdata.Add(settings);
            Puts($"Player has been added to the list {settings.playerID}");
        }

        private double AnchorBarMath(BasePlayer UIPlayer)
        {
            foreach (var PlayerInList in playerdata)
            {
                if (PlayerInList.playerID != UIPlayer.UserIDString) continue;
                return ((GetUITransparency(UIPlayer) / 10) - ((GetUITransparency(UIPlayer) / 10) - (PlayerInList.rangeValue / 1000))) * 10;
            }
            return 0;
        }

        private void NewConfigInDataFile(BasePlayer UIPlayer)
        {
            var PlayerConfigTSave = new PlayerUISetting
            {
                playerID = UIPlayer.UserIDString,
                Transparency = Transparency,
                UITextColor = $"{HexToColor("#FFFFFF")} 1",
                SellBoxColors = $"{HexToColor("#FFFFFF")} 0.15",
                BuyBoxColors = $"{HexToColor("#FFFFFF")} 0.15",
                rangeValue = (Transparency - 0.9) * 100,
                ImageOrText = config.UIImageOption
            };
            if (!CheckIfPlayerInDataFile(UIPlayer))
            {
                SavaPlayerConfigData(PlayerConfigTSave);
                Puts("New config entry created");
            }
        }

        private bool CheckIfPlayerInDataFile(BasePlayer UIPlayer)
        {
            bool check = false;
            foreach (var PlayerInList in playerdata)
            {
                if (PlayerInList.playerID != UIPlayer.UserIDString)
                {
                    check = false;
                }
                else
                {
                    check = true;
                }
            }
            return check;
        }

        private double PlayerTransparencyChange(BasePlayer UIPlayer, string action)
        {
            if (CheckIfPlayerInDataFile(UIPlayer) == false)
            {
                NewConfigInDataFile(UIPlayer);
                Puts("There's an existing player in the  file!");
            }
            foreach (var PlayerInList in playerdata)
            {
                if (PlayerInList.playerID != UIPlayer.UserIDString) continue;
                switch (action)
                {
                    case "increase":
                        if (PlayerInList.Transparency == 1)
                        {
                            break;
                        }
                        PlayerInList.Transparency = PlayerInList.Transparency + 0.01;
                        PlayerInList.rangeValue = PlayerInList.rangeValue + 1;
                        break;
                    case "decrease":
                        if (PlayerInList.Transparency == 0.9)
                        {
                            break;
                        }
                        PlayerInList.Transparency = PlayerInList.Transparency - 0.01;
                        PlayerInList.rangeValue = PlayerInList.rangeValue - 1;
                        break;

                }
                return PlayerInList.Transparency;
            }
            return Transparency;
        }
        private double GetUITransparency(BasePlayer UIplayer)
        {
            foreach (var PlayerInList in playerdata)
            {
                if (PlayerInList.playerID != UIplayer.UserIDString) continue;
                return (PlayerInList.Transparency);
            }
            return Transparency;
        }

        private void PlayerColorTextChange(BasePlayer UIPlayer, string TextColorRed, string TextColorGreen, string TextColorBlue, string UISettingToChange)
        {
            if (CheckIfPlayerInDataFile(UIPlayer) == false)
            {
                NewConfigInDataFile(UIPlayer);
                Puts("There's an existing player in the  file!");
            }
            foreach (var PlayerInList in playerdata)
            {
                if (PlayerInList.playerID != UIPlayer.UserIDString) continue;

                switch (UISettingToChange)
                {
                    case "Text":
                        PlayerInList.UITextColor = $"{TextColorRed} {TextColorGreen} {TextColorBlue} 1";
                        break;
                    case "Buy":
                        PlayerInList.BuyBoxColors = $"{TextColorRed} {TextColorGreen} {TextColorBlue} {GetUITransparency(UIPlayer) - 0.75}";
                        break;
                    case "Sell":
                        PlayerInList.SellBoxColors = $"{TextColorRed} {TextColorGreen} {TextColorBlue} {GetUITransparency(UIPlayer) - 0.75}";
                        break;
                }
            }
        }

        private string GetUITextColor(BasePlayer UIplayer)
        {
            foreach (var PlayerInList in playerdata)
            {
                if (PlayerInList.playerID != UIplayer.UserIDString) continue;
                return (PlayerInList.UITextColor);
            }
            return $"{HexToColor("#FFFFFF")} 1";
        }

        private string GetUISellBoxColor(BasePlayer UIplayer)
        {
            foreach (var PlayerInList in playerdata)
            {
                if (PlayerInList.playerID != UIplayer.UserIDString) continue;
                return (PlayerInList.SellBoxColors);
            }
            return $"{HexToColor("#FF0000")} {0.3 - GetUITransparency(UIplayer) / 10}";
        }

        private string GetUIBuyBoxColor(BasePlayer UIplayer)
        {
            foreach (var PlayerInList in playerdata)
            {
                if (PlayerInList.playerID != UIplayer.UserIDString) continue;
                return (PlayerInList.BuyBoxColors);
            }
            return $"{HexToColor("#00FF00")} {0.3 - GetUITransparency(UIplayer) / 10}";
        }

        public static string HexToColor(string hexString)
        {
            if (hexString.IndexOf('#') != -1) hexString = hexString.Replace("#", "");

            int b = 0;
            int r = 0;
            int g = 0;

            if (hexString.Length == 6)
            {
                r = int.Parse(hexString.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                g = int.Parse(hexString.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                b = int.Parse(hexString.Substring(4, 2), NumberStyles.AllowHexSpecifier);
            }
            return $"{(double)r / 255} {(double)g / 255} {(double)b / 255}";
        }

        #endregion

        #region Limits
        private bool LimitReached(ulong userId, string item, int amount, string ttype) //Limiter function TODO: current disabled
        {
            ItemLimit itemLimit;

            if (!limits.TryGetValue(userId, out itemLimit))
            {
                itemLimit = new ItemLimit();

                limits.Add(userId, itemLimit);
            }

            if (ttype == "buy" && !itemLimit.HasBuyLimit(item, amount))
            {
                itemLimit.IncrementBuy(item);

                return false;
            }

            if (ttype == "sell" && !itemLimit.HasSellLimit(item, amount))
            {
                itemLimit.IncrementSell(item);

                return false;
            }

            return true;
        }
        #endregion

        #region NPC

        bool GetNearestVendor(BasePlayer player, out ShopCategory category) //NPC helper finished.
        {
            category = null;

            Collider[] colliders = Physics.OverlapSphere(player.ServerPosition, 2.5f, playersMask);

            if (!colliders.Any())
            {
                return false;
            }

            BasePlayer npc = colliders.Select(col => col.GetComponent<BasePlayer>())
                .FirstOrDefault(x => !IsPlayer(x.userID));

            if (npc == null)
            {
                return false;
            }

            category = config.ShopCategories.Select(x => x.Value).FirstOrDefault(i => i.EnableNPC && i.NPCId == npc.UserIDString); // TODO: Add this to above DONE 1.8.0 updated 1.8.1

            if (category == null)
            {
                return false;
            }

            return true;
        }

        bool IsPlayer(ulong userID)
        {
            return userID >= 76560000000000000L;
        }

        private void OnUseNPC(BasePlayer npc, BasePlayer player) //added 1.8.7
        {
            ShopCategory category = config.ShopCategories.Select(x => x.Value).FirstOrDefault(i => i.EnableNPC && i.NPCId == npc.UserIDString);

            if (category == null)
            {
                return;
            }
            ShowShop(player, category.DisplayName);
        }

        private void OnLeaveNPC(BasePlayer npc, BasePlayer player) //added 1.8.7
        {
            ShopCategory category = config.ShopCategories.Select(x => x.Value).FirstOrDefault(i => i.EnableNPC && i.NPCId == npc.UserIDString);

            if (category == null)
            {
                return;
            }
            CloseShop(player);

            if (config.NPCLeaveResponse) //added 1.8.8
            {
                SendReply(player, GetMessage("MessageNPCResponseclose", player.UserIDString), category.DisplayName);
            }
        }

        #endregion

        #region Helpers

        private bool IsNearMonument(BasePlayer player)
        {
            foreach (var monumentInfo in _monuments)
            {
                float distance = Vector3Ex.Distance2D(monumentInfo.transform.position, player.ServerPosition);

                if (monumentInfo.name.Contains("sphere") && distance < 30f)
                {
                    return true;
                }

                if (monumentInfo.name.Contains("launch") && distance < 30f)
                {
                    return true;
                }

                if (!monumentInfo.IsInBounds(player.ServerPosition)) continue;

                return true;
            }

            return false;
        }

        #endregion

        // NPC Marker Method.  8:19AM 9/26/2020 Pacific Standard Time. TODO:

        #region Markers
        /*class VendingMapMarker : MapMarkerGenericRadius
        {
            public override bool CanUseNetworkCache(Connection connection)
            {
                if (connection != null)
                {
                    float single = color1.a;
                    Vector3 vector3 = new Vector3(color1.r, color1.g, color1.b);
                    Vector3 vector31 = new Vector3(color2.r, color2.g, color2.b);
                    Invoke(() => ClientRPCEx(new SendInfo(connection), null, "MarkerUpdate", vector3, single, vector31, alpha, radius), 0f);
                }

                return base.CanUseNetworkCache(connection);
            }
        }

        [ChatCommand("vnmark")]
        void CreateMarker(BasePlayer basePlayer, string command, string[] args)
        {
            var marker = new GameObject().AddComponent<VendingMapMarker>();
            marker.prefabID = 2849728229;
            marker.appType = AppMarkerType.GenericRadius;
            marker.transform.position = basePlayer.transform.position;
            marker.color1 = Color.white;
            marker.color2 = Color.black;
            marker.radius = 0.1f;
            marker.alpha = 1f;
            marker._limitedNetworking = false;
            marker.enableSaving = false;
            marker.globalBroadcast = true;
            marker.Spawn();
        }*/

        /*[ChatCommand("removeall")]
        private void cmdRemoveAll(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, Admin))
            {
                RemoveMarkers();
                SendUpdate();
                SendNetworkUpdate();
            }
            else
            SendReply(player, GetMessage("MessageErrorAdmin", player.UserIDString));
        }*/

        // NPC Markers __φ(．．)

        #endregion

        #region UI Re-write
        #endregion

        #region API Hooks

        private void OpenShop(BasePlayer player, string shopName, string npcID)
        {
            if (player == null || string.IsNullOrEmpty(shopName) || string.IsNullOrEmpty(npcID))
            {
                return;
            }

            ShopCategory shopCategory;

            config.ShopCategories.TryGetValue(shopName, out shopCategory);

            if (shopCategory == null || !shopCategory.EnableNPC || shopCategory.NPCId != npcID)
            {
                return;
            }

            ShowShop(player, shopName);
        }

        private void CloseShop(BasePlayer player)
        {
            if (player == null)
            {
                return;
            }

            CuiHelper.DestroyUi(player, ShopOverlayName);
        }

        #endregion

    }
}