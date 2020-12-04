using System;
using Oxide.Core;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("HW Kits", "klauz24", "1.0.9")]
    [Description("Create kits of items for players to use.")]
    class HWKits : HurtworldPlugin
    {

        void Loaded()
        {
            LoadData();
            try
            {
                kitsData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Dictionary<string, KitData>>>("HWKits_Data");
            }
            catch
            {
                kitsData = new Dictionary<ulong, Dictionary<string, KitData>>();
            }
        }

        void OnServerInitialized() => InitializePermissions();

        void InitializePermissions()
        {
            foreach (var kit in storedData.Kits.Values)
            {
                if (string.IsNullOrEmpty(kit.permission)) continue;
                permission.RegisterPermission(kit.permission, this);
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        ///// Localization
        //////////////////////////////////////////////////////////////////////////////////////////

        protected override void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"HWKits - Prefix", "<color=yellow>[HW Kits]</color>"},
                {"HWKits - List Text", "List of kits: "},
                {"HWKits - List Kits", "{kName} - {kDescription} {reason}"},
                {"HWKits - No perm to use", "You got no permissions to use this command."},
                {"HWKits - Redeem", "Kit redeemed."},
                {"HWKits - Does not exist", "The kit <color=orange>{kName}</color> does not exist."},
                {"HWKits - 0 left", "- <color=red>0</color> left."},
                {"HWKits - Amount left", "- <color=orange>{amount}</color> left."},
                {"HWKits - Cooldown", "- <color=red>{time}</color> seconds."},
                {"HWKits - Not allowed at this moment", "You are not allowed to redeem a kit at the moment."},
                {"HWKits - No level", "You don't have the level to use this kit."},
                {"HWKits - No perm to use the kit", "You don't have the permissions to use this kit."},
                {"HWKits - Redeemed all", "You already redeemed all of those kits."},
                {"HWKits - Cooldown message", "You need to wait <color=orange>{sec}</color> seconds to use this kit."},
                {"HWKits - Admin - Edit 1", "permission \"permission name\" => set the permission needed to get this kit"},
                {"HWKits - Admin - Edit 2", "description \"description text here\" => set a description for this kit"},
                {"HWKits - Admin - Edit 3", "authlevel XXX"},
                {"HWKits - Admin - Edit 4", "cooldown XXX"},
                {"HWKits - Admin - Edit 5", "max XXX"},
                {"HWKits - Admin - Edit 6", "items => set new items for your kit (will copy your inventory)"},
                {"HWKits - Admin - Edit 7", "hide TRUE/FALSE => dont show this kit in lists (EVER)"},
                {"HWKits - Admin - Help 1", "====== Player Commands ======"},
                {"HWKits - Admin - Help 2", "/kit => to get the list of kits"},
                {"HWKits - Admin - Help 3", "/kit KITNAME => to redeem the kit"},
                {"HWKits - Admin - Help 4", "====== Admin Commands ======"},
                {"HWKits - Admin - Help 5", "/kit add KITNAME => add a kit"},
                {"HWKits - Admin - Help 6", "/kit remove KITNAME => remove a kit"},
                {"HWKits - Admin - Help 7", "/kit edit KITNAME => edit a kit"},
                {"HWKits - Admin - Help 8", "/kit list => get a raw list of kits (the real full list)"},
                {"HWKits - Admin - Help 9", "/kit give PLAYER/STEAMID KITNAME => give a kit to a player"},
                {"HWKits - Admin - Help 10", "/kit resetkits => deletes all kits"},
                {"HWKits - Admin - Help 11", "/kit resetdata => reset player data"},
                {"HWKits - No access", "You don't have access to this command."},
                {"HWKits - Kit exists", "This kit already exists."},
                {"HWKits - Created", "You have created new kit: {kName}."},
                {"HWKits - Creating - Does not exist", "/kit give PLAYER/STEAMID KITNAME"},
                {"HWKits - Player not found", "No players found."},
                {"HWKits - Player multiple found", "Multiple players found."},
                {"HWKits - Kit sent", "You gave {pName} the kit: {kName}."},
                {"HWKits - Kit recieved", "You've received the kit {kName} from {pName}."},
                {"HWKits - Editing", "You are now editing the kit: {kName}."},
                {"HWKits - Removed", "{kName} was removed."},
                {"HWKits - Doing nothing", "You are not creating or editing a kit."},
                {"HWKits - Error", "There was an error while getting this kit, was it changed while you were editing it?"},
                {"HWKits - Items copied", "The items were copied from your inventory."},
                {"HWKits - Not valid argument", "<color=orange>{args}</color> is not a valid argument."}
            }, this);
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        ///// uMod Hooks
        //////////////////////////////////////////////////////////////////////////////////////////

        void OnPlayerRespawn(PlayerSession session)
        {
            if (!storedData.Kits.ContainsKey("autokit")) return;
            var thereturn = Interface.Oxide.CallHook("canRedeemKit", session);
            if (thereturn == null)
            {
                var playerinv = session.WorldPlayerEntity.Storage;
                for (var j = 0; j < playerinv.Capacity; j++)
                {
                    if (playerinv.GetSlot(j) == null) continue;
                    //if (playerinv.GetSlot(j).ItemId == null) continue;
                    ClassInstancePool.Instance.ReleaseInstanceExplicit(playerinv.GetSlot(j));
                    playerinv.SetSlot(j, null);
                }
                GiveKit(session, "autokit");
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        ///// Kit Creator
        //////////////////////////////////////////////////////////////////////////////////////////

        List<KitItem> GetPlayerItems(PlayerSession session)
        {
            var playerinv = session.WorldPlayerEntity.Storage;

            var kititems = new List<KitItem>();
            for (var i = 0; i < playerinv.Capacity; i++)
            {
                var item = playerinv.GetSlot(i);
                if (item?.Generator == null) continue;
                kititems.Add(new KitItem
                {
                    itemGuid = RuntimeHurtDB.Instance.GetGuid(item.Generator).ToString(),
                    amount = item.StackSize,
                    slot = i
                });
            }
            return kititems;
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        ///// Kit Redeemer
        //////////////////////////////////////////////////////////////////////////////////////////

        private void TryGiveKit(PlayerSession session, string kitname)
        {
            var success = CanRedeemKit(session, kitname) as string;
            if (success != null)
            {
                hurt.SendChatMessage(session, lang.GetMessage("HWKits - Prefix", this, session.SteamId.ToString()), success);
                return;
            }
            success = GiveKit(session, kitname) as string;
            if (success != null)
            {
                hurt.SendChatMessage(session, lang.GetMessage("HWKits - Prefix", this, session.SteamId.ToString()), success);
                return;
            }
            hurt.SendChatMessage(session, lang.GetMessage("HWKits - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWKits - Redeem", this, session.SteamId.ToString()));
            ProccessKitGiven(session, kitname);
        }

        void ProccessKitGiven(PlayerSession session, string kitname)
        {
            Kit kit;
            if (string.IsNullOrEmpty(kitname) || !storedData.Kits.TryGetValue(kitname, out kit)) return;

            var kitData = GetKitData(session.SteamId.m_SteamID, kitname);
            if (kit.max > 0) kitData.max += 1;

            if (kit.cooldown > 0) kitData.cooldown = CurrentTime() + kit.cooldown;
        }

        object GiveKit(PlayerSession session, string kitname)
        {
            Kit kit;
            if (string.IsNullOrEmpty(kitname) || !storedData.Kits.TryGetValue(kitname, out kit)) return lang.GetMessage("HWKits - Does not exist", this, session.SteamId.ToString()).Replace("{kName}", kitname);
            var playerinv = session.WorldPlayerEntity.Storage;
            var amanager = Singleton<AlertManager>.Instance;
            var itemmanager = Singleton<GlobalItemManager>.Instance;
            foreach (var kitem in kit.items)
            {
                if (playerinv.GetSlot(kitem.slot) == null)
                {
                    var getGuid = RuntimeHurtDB.Instance.GetObjectByGuid<ItemGeneratorAsset>(kitem.itemGuid);
                    var item = itemmanager.CreateItem(getGuid, kitem.amount);
                    playerinv.SetSlot(kitem.slot, item);
                    amanager.ItemReceivedServer(item, item.StackSize, session.Player);
                    playerinv.Invalidate(false);
                }
                else
                {
                    var getGuid = RuntimeHurtDB.Instance.GetObjectByGuid<ItemGeneratorAsset>(kitem.itemGuid);
                    itemmanager.GiveItem(session.Player, getGuid, kitem.amount);
                }
            }
            return true;
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        ///// Check Kits
        //////////////////////////////////////////////////////////////////////////////////////////

        bool isKit(string kitname) => !string.IsNullOrEmpty(kitname) && storedData.Kits.ContainsKey(kitname);

        static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        static double CurrentTime() { return DateTime.UtcNow.Subtract(epoch).TotalSeconds; }

        bool CanSeeKit(PlayerSession session, string kitname, out string reason)
        {
            reason = string.Empty;
            Kit kit;
            if (string.IsNullOrEmpty(kitname) || !storedData.Kits.TryGetValue(kitname, out kit)) return false;
            if (kit.hide) return false;
            if (kit.authlevel > 0)
                if (!session.IsAdmin) return false;
            if (!string.IsNullOrEmpty(kit.permission))
                if (!permission.UserHasPermission(session.SteamId.ToString(), kit.permission)) return false;
            if (kit.max > 0)
            {
                var left = GetKitData(session.SteamId.m_SteamID, kitname).max;
                if (left >= kit.max)
                {
                    reason += lang.GetMessage("HWKits - 0 left", this, session.SteamId.ToString());
                    return false;
                }
                reason += lang.GetMessage("HWKits - Amount left", this, session.SteamId.ToString()).Replace("{amount}", $"{(kit.max - left)}");
            }
            if (kit.cooldown > 0)
            {
                var cd = GetKitData(session.SteamId.m_SteamID, kitname).cooldown;
                var ct = CurrentTime();
                if (cd > ct && cd != 0.0)
                {
                    reason += lang.GetMessage("HWKits - Cooldown", this, session.SteamId.ToString()).Replace("{time}", $"{Math.Abs(Math.Ceiling(cd - ct))}");
                    return false;
                }
            }
            return true;
        }

        object CanRedeemKit(PlayerSession session, string kitname)
        {
            Kit kit;
            if (string.IsNullOrEmpty(kitname) || !storedData.Kits.TryGetValue(kitname, out kit)) return lang.GetMessage("HWKits - Does not exist", this, session.SteamId.ToString()).Replace("{kName}", kitname);

            var thereturn = Interface.Oxide.CallHook("canRedeemKit", session, kitname);
            if (thereturn != null)
            {
                if (thereturn is string) return thereturn;
                return lang.GetMessage("HWKits - Not allowed at this moment", this, session.SteamId.ToString());
            }

            if (kit.authlevel > 0)
                if (!session.IsAdmin) return lang.GetMessage("HWKits - No level", this, session.SteamId.ToString());

            if (!string.IsNullOrEmpty(kit.permission))
                if (!permission.UserHasPermission(session.SteamId.ToString(), kit.permission))
                    return lang.GetMessage("HWKits - No perm to use the kit", this, session.SteamId.ToString());

            var kitData = GetKitData(session.SteamId.m_SteamID, kitname);
            if (kit.max > 0)
                if (kitData.max >= kit.max) return lang.GetMessage("HWKits - Redeemed all", this, session.SteamId.ToString());

            if (kit.cooldown > 0)
            {
                var ct = CurrentTime();
                if (kitData.cooldown > ct && kitData.cooldown != 0.0)
                    return lang.GetMessage("HWKits - Cooldown message", this, session.SteamId.ToString()).Replace("{sec}", $"{Math.Abs(Math.Ceiling(kitData.cooldown - ct))}");
            }
            return true;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Kit Class
        //////////////////////////////////////////////////////////////////////////////////////

        class KitItem
        {
            public string itemGuid;
            public int amount;
            public int slot;
        }

        class Kit
        {
            public string name;
            public string description;
            public int max;
            public double cooldown;
            public int authlevel;
            public bool hide;
            public string permission;
            public List<KitItem> items = new List<KitItem>();
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Data Manager
        //////////////////////////////////////////////////////////////////////////////////////

        void SaveKitsData() => Interface.Oxide.DataFileSystem.WriteObject("HWKits_Data", kitsData);

        StoredData storedData;
        Dictionary<ulong, Dictionary<string, KitData>> kitsData;

        class StoredData
        {
            public Dictionary<string, Kit> Kits = new Dictionary<string, Kit>();
        }
        class KitData
        {
            public int max;
            public double cooldown;
        }
        void ResetData()
        {
            kitsData.Clear();
            SaveKitsData();
        }

        void Unload() => SaveKitsData();
        void OnServerSave() => SaveKitsData();

        void SaveKits() => Interface.Oxide.DataFileSystem.WriteObject("HWKits", storedData);

        void LoadData()
        {
            var kits = Interface.Oxide.DataFileSystem.GetFile("HWKits");
            try
            {
                kits.Settings.NullValueHandling = NullValueHandling.Ignore;
                storedData = kits.ReadObject<StoredData>();
            }
            catch
            {
                storedData = new StoredData();
            }
            kits.Settings.NullValueHandling = NullValueHandling.Include;
        }

        KitData GetKitData(ulong userID, string kitname)
        {
            Dictionary<string, KitData> kitDatas;
            if (!kitsData.TryGetValue(userID, out kitDatas)) kitsData[userID] = kitDatas = new Dictionary<string, KitData>();
            KitData kitData;
            if (!kitDatas.TryGetValue(kitname, out kitData)) kitDatas[kitname] = kitData = new KitData();
            return kitData;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Kit Editor
        //////////////////////////////////////////////////////////////////////////////////////

        readonly Dictionary<ulong, string> kitEditor = new Dictionary<ulong, string>();

        //////////////////////////////////////////////////////////////////////////////////////
        // Console Command
        //////////////////////////////////////////////////////////////////////////////////////

        List<PlayerSession> FindPlayer(string arg)
        {
            var listPlayers = new List<PlayerSession>();

            ulong steamid;
            ulong.TryParse(arg, out steamid);
            var lowerarg = arg.ToLower();

            foreach (var pair in GameManager.Instance.GetSessions())
            {
                var session = pair.Value;
                if (!session.IsLoaded) continue;
                if (steamid != 0L)
                    if (session.SteamId.m_SteamID == steamid)
                    {
                        listPlayers.Clear();
                        listPlayers.Add(session);
                        return listPlayers;
                    }
                var lowername = session.Identity.Name.ToLower();
                if (lowername.Contains(lowerarg)) listPlayers.Add(session);
            }
            return listPlayers;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Chat Command
        //////////////////////////////////////////////////////////////////////////////////////

        bool HasAccess(PlayerSession session) => session.IsAdmin;

        void SendListKitEdition(PlayerSession session)
        {
            hurt.SendChatMessage(session, null, lang.GetMessage("HWKits - Admin - Edit 1", this, session.SteamId.ToString()));
            hurt.SendChatMessage(session, null, lang.GetMessage("HWKits - Admin - Edit 2", this, session.SteamId.ToString()));
            hurt.SendChatMessage(session, null, lang.GetMessage("HWKits - Admin - Edit 3", this, session.SteamId.ToString()));
            hurt.SendChatMessage(session, null, lang.GetMessage("HWKits - Admin - Edit 4", this, session.SteamId.ToString()));
            hurt.SendChatMessage(session, null, lang.GetMessage("HWKits - Admin - Edit 5", this, session.SteamId.ToString()));
            hurt.SendChatMessage(session, null, lang.GetMessage("HWKits - Admin - Edit 6", this, session.SteamId.ToString()));
            hurt.SendChatMessage(session, null, lang.GetMessage("HWKits - Admin - Edit 7", this, session.SteamId.ToString()));
        }

        [ChatCommand("kit")]
        void cmdKit(PlayerSession session, string command, string[] args)
        {
            if (args.Length == 0)
            {
                var reason = string.Empty;
                hurt.SendChatMessage(session, lang.GetMessage("HWKits - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWKits - List Text", this, session.SteamId.ToString()));
                foreach (var pair in storedData.Kits)
                {
                    var cansee = CanSeeKit(session, pair.Key, out reason);
                    if (!cansee && string.IsNullOrEmpty(reason)) continue;
                    hurt.SendChatMessage(session, null, lang.GetMessage("HWKits - List Kits", this, session.SteamId.ToString())
                        .Replace("{kName}", pair.Value.name)
                        .Replace("{kDescription}", pair.Value.description)
                        .Replace("{reason}", reason));
                }
                return;
            }
            if (args.Length == 1)
            {
                switch (args[0])
                {
                    case "help":
                        hurt.SendChatMessage(session, null, lang.GetMessage("HWKits - Admin - Help 1", this, session.SteamId.ToString()));
                        hurt.SendChatMessage(session, null, lang.GetMessage("HWKits - Admin - Help 2", this, session.SteamId.ToString()));
                        hurt.SendChatMessage(session, null, lang.GetMessage("HWKits - Admin - Help 3", this, session.SteamId.ToString()));
                        if (!HasAccess(session)) return;
                        hurt.SendChatMessage(session, null, lang.GetMessage("HWKits - Admin - Help 4", this, session.SteamId.ToString()));
                        hurt.SendChatMessage(session, null, lang.GetMessage("HWKits - Admin - Help 5", this, session.SteamId.ToString()));
                        hurt.SendChatMessage(session, null, lang.GetMessage("HWKits - Admin - Help 6", this, session.SteamId.ToString()));
                        hurt.SendChatMessage(session, null, lang.GetMessage("HWKits - Admin - Help 7", this, session.SteamId.ToString()));
                        hurt.SendChatMessage(session, null, lang.GetMessage("HWKits - Admin - Help 8", this, session.SteamId.ToString()));
                        hurt.SendChatMessage(session, null, lang.GetMessage("HWKits - Admin - Help 9", this, session.SteamId.ToString()));
                        hurt.SendChatMessage(session, null, lang.GetMessage("HWKits - Admin - Help 10", this, session.SteamId.ToString()));
                        hurt.SendChatMessage(session, null, lang.GetMessage("HWKits - Admin - Help 11", this, session.SteamId.ToString()));
                        break;
                    case "add":
                    case "remove":
                    case "edit":
                        if (!HasAccess(session)) { hurt.SendChatMessage(session, lang.GetMessage("HWKits - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWKits - No access", this, session.SteamId.ToString())); return; }
                        hurt.SendChatMessage(session, null, $"/kit {args[0]} KITNAME");
                        break;
                    case "give":
                        if (!HasAccess(session)) { hurt.SendChatMessage(session, lang.GetMessage("HWKits - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWKits - No access", this, session.SteamId.ToString())); return; }
                        hurt.SendChatMessage(session, null, "/kit give PLAYER/STEAMID KITNAME");
                        break;
                    case "list":
                        if (!HasAccess(session)) { hurt.SendChatMessage(session, lang.GetMessage("HWKits - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWKits - No access", this, session.SteamId.ToString())); return; }
                        foreach (var kit in storedData.Kits.Values) hurt.SendChatMessage(session, null, $"{kit.name} - {kit.description}");
                        break;
                    case "items":
                        break;
                    case "resetkits":
                        if (!HasAccess(session)) { hurt.SendChatMessage(session, lang.GetMessage("HWKits - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWKits - No access", this, session.SteamId.ToString())); return; }
                        storedData.Kits.Clear();
                        kitEditor.Clear();
                        ResetData();
                        SaveKits();
                        hurt.SendChatMessage(session, lang.GetMessage("HWKits - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWKits - Data wiped", this, session.SteamId.ToString()));
                        break;
                    case "resetdata":
                        if (!HasAccess(session)) { hurt.SendChatMessage(session, lang.GetMessage("HWKits - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWKits - No access", this, session.SteamId.ToString())); return; }
                        ResetData();
                        hurt.SendChatMessage(session, lang.GetMessage("HWKits - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWKits - Data wiped", this, session.SteamId.ToString()));
                        break;
                    default:
                        TryGiveKit(session, args[0].ToLower());
                        break;
                }
                if (args[0] != "items") return;

            }
            if (!HasAccess(session)) { hurt.SendChatMessage(session, lang.GetMessage("HWKits - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWKits - No access", this, session.SteamId.ToString())); return; }

            string kitname;
            switch (args[0])
            {
                case "add":
                    kitname = args[1].ToLower();
                    if (storedData.Kits.ContainsKey(kitname))
                    {
                        hurt.SendChatMessage(session, lang.GetMessage("HWKits - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWKits - Kit exists", this, session.SteamId.ToString()));
                        return;
                    }
                    storedData.Kits[kitname] = new Kit { name = args[1] };
                    kitEditor[session.SteamId.m_SteamID] = kitname;
                    hurt.SendChatMessage(session, lang.GetMessage("HWKits - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWKits - Created", this, session.SteamId.ToString()).Replace("{kName}", args[1]));
                    SendListKitEdition(session);
                    break;
                case "give":
                    if (args.Length < 3)
                    {
                        hurt.SendChatMessage(session, lang.GetMessage("HWKits - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWKits - Give syntax", this, session.SteamId.ToString()));
                        return;
                    }
                    kitname = args[2].ToLower();
                    if (!storedData.Kits.ContainsKey(kitname))
                    {
                        hurt.SendChatMessage(session, lang.GetMessage("HWKits - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWKits - Creating - Does not exist", this, session.SteamId.ToString()));
                        return;
                    }
                    var findPlayers = FindPlayer(args[1]);
                    if (findPlayers.Count == 0)
                    {
                        hurt.SendChatMessage(session, lang.GetMessage("HWKits - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWKits - Player not found", this, session.SteamId.ToString()));
                        return;
                    }
                    if (findPlayers.Count > 1)
                    {
                        hurt.SendChatMessage(session, lang.GetMessage("HWKits - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWKits - Player multiple found", this, session.SteamId.ToString()));
                        return;
                    }
                    GiveKit(findPlayers[0], kitname);
                    hurt.SendChatMessage(session, lang.GetMessage("HWKits - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWKits - Kit sent", this, session.SteamId.ToString())
                        .Replace("{pName}", findPlayers[0].Identity.Name)
                        .Replace("{kitName}", storedData.Kits[kitname].name));
                    hurt.SendChatMessage(findPlayers[0], lang.GetMessage("HWKits - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWKits - Kit recieved", this, session.SteamId.ToString())
                            .Replace("{kName}", storedData.Kits[kitname].name)
                            .Replace("{pName}", session.Identity.Name));
                    break;
                case "edit":
                    kitname = args[1].ToLower();
                    if (!storedData.Kits.ContainsKey(kitname))
                    {
                        hurt.SendChatMessage(session, lang.GetMessage("HWKits - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWKits - Creating - Does not exist", this, session.SteamId.ToString()));
                        return;
                    }
                    kitEditor[session.SteamId.m_SteamID] = kitname;
                    hurt.SendChatMessage(session, lang.GetMessage("HWKits - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWKits - Editing", this, session.SteamId.ToString())
                        .Replace("{kName}", kitname));
                    SendListKitEdition(session);
                    break;
                case "remove":
                    kitname = args[1].ToLower();
                    if (!storedData.Kits.Remove(kitname))
                    {
                        hurt.SendChatMessage(session, lang.GetMessage("HWKits - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWKits - Creating - Does not exist", this, session.SteamId.ToString()));
                        return;
                    }
                    hurt.SendChatMessage(session, lang.GetMessage("HWKits - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWKits - Removed", this, session.SteamId.ToString())
                        .Replace("{kName}", kitname));
                    if (kitEditor[session.SteamId.m_SteamID] == kitname) kitEditor.Remove(session.SteamId.m_SteamID);
                    break;
                default:
                    if (!kitEditor.TryGetValue(session.SteamId.m_SteamID, out kitname))
                    {
                        hurt.SendChatMessage(session, lang.GetMessage("HWKits - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWKits - Doing nothing", this, session.SteamId.ToString()));
                        return;
                    }
                    Kit kit;
                    if (!storedData.Kits.TryGetValue(kitname, out kit))
                    {
                        hurt.SendChatMessage(session, lang.GetMessage("HWKits - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWKits - Error", this, session.SteamId.ToString()));
                        return;
                    }
                    for (var i = 0; i < args.Length; i++)
                    {
                        object editvalue;
                        var key = args[i].ToLower();
                        switch (key)
                        {
                            case "items":
                                kit.items = GetPlayerItems(session);
                                hurt.SendChatMessage(session, lang.GetMessage("HWKits - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWKits - Items copied", this, session.SteamId.ToString()));
                                continue;
                            case "name":
                                continue;
                            case "description":
                                editvalue = kit.description = args[++i];
                                break;
                            case "max":
                                editvalue = kit.max = int.Parse(args[++i]);
                                break;
                            case "cooldown":
                                editvalue = kit.cooldown = double.Parse(args[++i]);
                                break;
                            case "authlevel":
                                editvalue = kit.authlevel = int.Parse(args[++i]);
                                break;
                            case "hide":
                                editvalue = kit.hide = bool.Parse(args[++i]);
                                break;
                            case "permission":
                                editvalue = kit.permission = this.Name + "." + args[++i];
                                InitializePermissions();
                                break;
                            default:
                                hurt.SendChatMessage(session, lang.GetMessage("HWKits - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWKits - Not valid argument", this, session.SteamId.ToString())
                                    .Replace("{args}", args[i]));
                                continue;
                        }
                        hurt.SendChatMessage(session, null, $"{key} set to {editvalue ?? "null"}");
                    }
                    break;
            }
            SaveKits();
        }
    }
}
