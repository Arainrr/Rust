using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Switch Gear", "Mr. Blue", "0.0.6")]
    [Description("Switch visual override gear into normal gear slots.")]

    class SwitchGear : HurtworldPlugin
    {
        private static Dictionary<int, int> pairs = new Dictionary<int, int>() { { 8, 28 }, { 9, 29 }, { 10, 30 }, { 11, 31 }, { 12, 32 }, { 13, 33 }, { 14, 34 }, { 15, 35 } };
        Dictionary<PlayerSession, DateTime> cooldowns = new Dictionary<PlayerSession, DateTime>();
        const string perm = "switchgear.use";
        private static float CoolDown = 20f;
        private static bool SwitchOnEmpty = false;
        private static bool SwitchBackpack = false;

        void Init()
        {
            CoolDown = Convert.ToSingle(Config["Cooldown"]);
            SwitchOnEmpty = (bool)Config["SwitchOnEmpty"];
            SwitchBackpack = (bool)Config["SwitchBackpack"];

            permission.RegisterPermission(perm, this);
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating a new configuration file");
            Config["Cooldown"] = 20f;
            Config["SwitchBackpack"] = true;
            Config["SwitchOnEmpty"] = false;
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(
                new Dictionary<string, string> {
                    { "GearSwitched", "Switched gear!" },
                    { "SwitchWait", "You have to wait {time} seconds before using this command!" },
                    { "NoPermission", "You do not have the permissions to use this command. (Perm: {perm})" },
                    { "SwitchError", "Something went wrong switching your gear, please try again" }
                }, this);
        }

        string Msg(string msg, string SteamId = null) => lang.GetMessage(msg, this, SteamId);

        [ChatCommand("switch")]
        void SwitchGearCommand(PlayerSession session, string command, string[] args)
        {
            string steamid = session.SteamId.ToString();
            if (!permission.UserHasPermission(steamid, perm))
            {
                Player.Message(session, Msg("NoPermission", steamid).Replace("{perm}", perm));
                return;
            }
            if (cooldowns.ContainsKey(session))
            {
                if (DateTime.Compare(cooldowns[session], DateTime.Now) > 0)
                {
                    Player.Message(session, Msg("SwitchWait", steamid).Replace("{time}", (Math.Ceiling((cooldowns[session] - DateTime.Now).TotalSeconds)).ToString()));
                    return;
                }
                cooldowns.Remove(session);
            }
            cooldowns.Add(session, DateTime.Now.AddSeconds(CoolDown));

            var pInv = session.WorldPlayerEntity.GetComponent<Inventory>();

            //This should never happen...
            if (pInv == null)
            {
                Player.Message(session, Msg("SwitchError", steamid));
                return;
            }

            foreach (KeyValuePair<int, int> pair in pairs)
            {
                //Store the gear and override slot
                ItemObject gslot = pInv.GetSlot(pair.Key);
                ItemObject oslot = pInv.GetSlot(pair.Value);

                if (pair.Key == 14 && !SwitchBackpack && gslot != null)
                    continue;

                if (!SwitchOnEmpty && oslot == null)
                    continue;

                //Empty out the slots first
                pInv.SetSlot(pair.Value, null);
                pInv.SetSlot(pair.Key, null);

                //Swap items
                if (gslot != null)
                    pInv.SetSlot(pair.Value, gslot);
                if (oslot != null)
                    pInv.SetSlot(pair.Key, oslot);
            }
            pInv.Invalidate();

            Player.Message(session, Msg("GearSwitched", steamid));
        }
    }
}