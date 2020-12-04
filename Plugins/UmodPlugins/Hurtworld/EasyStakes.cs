﻿//Reference: UnityEngine.UI
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("EasyStakes", "Mr. Blue", "2.0.3")]
    [Description("Fill up every authorized stake with amber.")]

    public class EasyStakes : HurtworldPlugin
    {

        #region General
        private string AmberGuid = "49a99af8b780d07489c5794c13fab84c";

        private void Loaded()
        {
            permission.RegisterPermission("easystakes.use", this);
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "EasyStake: You don't have Permission to do this!",
                ["Stakes"] = "EasyStakes: You're Authorized on {Count} stakes. You need {Amount} Amber to fill them all.",
                ["Filled"] = "EasyStakes: Successfully filled {count} Ownership Stakes with {amount} Amber",
                ["NotFilled"] = "EasyStakes: Not filled {count} Ownership Stakes, {amount} amber needed to fill stakes.",
                ["NoAmber"] = "EasyStakes: You've ran out of Amber!"
            }, this);
        }
        private string Msg(string msg, object SteamId = null) => lang.GetMessage(msg, this, SteamId.ToString());
        #endregion

        #region ChatCommands
        [ChatCommand("stakes")]
        private void StakeCommand(PlayerSession session, string command, string[] args)
        {
            string steamId = session.SteamId.ToString();
            if (!permission.UserHasPermission(steamId, "easystakes.use"))
            {
                Player.Message(session, Msg("NoPermission", steamId));
                return;
            }

            UserStakes userstakes = GetUserStakes(session);
            int totalNeeded = userstakes.TotalNeeded();
            int needFill = userstakes.NeedFill();

            if (needFill == 0)
            {
                Player.Message(session, Msg("Stakes", steamId)
                    .Replace("{count}", userstakes.Stakes.Count.ToString())
                    .Replace("{amount}", totalNeeded.ToString()));
                return;
            }

            if (args.Length == 1 && args[0].ToLower() == "fill")
            {
                int amber = GetAmber(session);
                if (amber == 0)
                {
                    Player.Message(session, Msg("NoAmber", steamId));
                }
                else if (amber >= totalNeeded)
                {
                    TakeAmber(session, totalNeeded);
                    needFill = userstakes.NeedFill();
                    userstakes.FillAll();
                    Player.Message(session, Msg("Filled", steamId)
                        .Replace("{count}", needFill.ToString())
                        .Replace("{amount}", totalNeeded.ToString()));
                }
                else
                {
                    TakeAmber(session, amber);
                    int filled = userstakes.FillStakes(amber);
                    needFill = userstakes.NeedFill();
                    totalNeeded = userstakes.TotalNeeded();

                    Player.Message(session, Msg("Filled", steamId)
                        .Replace("{count}", filled.ToString())
                        .Replace("{amount}", amber.ToString()));
                    if (needFill != 0)
                    {
                        Player.Message(session, Msg("NotFilled", steamId)
                            .Replace("{count}", needFill.ToString())
                            .Replace("{amount}", totalNeeded.ToString()));
                    }
                }
            }
            else
            {
                Player.Message(session, Msg("Stakes", steamId).Replace("{count}", userstakes.Stakes.Count.ToString()).Replace("{amount}", totalNeeded.ToString()));
            }
        }
        #endregion

        #region Classes
        private class UserStakes
        {
            public List<UserStake> Stakes = new List<UserStake>();

            public UserStakes(List<UserStake> userStakes)
            {
                Stakes = userStakes;
            }

            public int TotalNeeded()
            {
                int needed = 0;
                foreach (UserStake stake in Stakes)
                {
                    Inventory stakeStorage = stake.Stake.GetComponent<Inventory>();
                    if (stakeStorage == null) continue;
                    needed += stakeStorage.GetSlotConf(0).StackRestriction - stakeStorage.GetSlot(0).StackSize;
                }
                return needed;
            }

            public int NeedFill()
            {
                int stakes = 0;
                foreach (UserStake stake in Stakes)
                {
                    if (stake.AmberNeeded == 0) continue;
                    else stakes++;
                }
                return stakes;
            }

            public void FillAll()
            {
                foreach (UserStake stake in Stakes)
                {
                    Inventory stakeStorage = stake.Stake.GetComponent<Inventory>();
                    stakeStorage.GetSlot(0).StackSize = stakeStorage.GetSlotConf(0).StackRestriction;
                    stakeStorage.GetSlot(0).InvalidateStack();
                }
            }

            public int FillStakes(int a)
            {
                int amber = a;
                int filled = 0;
                foreach (UserStake stake in Stakes)
                {
                    Inventory stakeStorage = stake.Stake.GetComponent<Inventory>();
                    if (stakeStorage == null) continue;
                    filled++;
                    if (stake.AmberNeeded <= amber)
                    {
                        stakeStorage.GetSlot(0).StackSize = stakeStorage.GetSlotConf(0).StackRestriction;
                        stakeStorage.GetSlot(0).InvalidateStack();
                        amber -= stake.AmberNeeded;
                        continue;
                    }
                    else
                    {
                        stakeStorage.GetSlot(0).StackSize = stakeStorage.GetSlot(0).StackSize + amber;
                        stakeStorage.GetSlot(0).InvalidateStack();
                        return filled;
                    }
                }
                return filled;
            }
        }

        public class UserStake
        {
            public OwnershipStakeServer Stake;
            public int AmberNeeded;

            public UserStake(OwnershipStakeServer stake, int amberNeeded)
            {
                Stake = stake;
                AmberNeeded = amberNeeded;
            }

        }
        #endregion

        #region Helpers
        private UserStakes GetUserStakes(PlayerSession session)
        {
            List<UserStake> userStakes = new List<UserStake>();
            foreach (OwnershipStakeServer stake in Resources.FindObjectsOfTypeAll<OwnershipStakeServer>())
            {
                if (stake.AuthorizedPlayers.Contains(session.Identity) || (stake.IsClanTotem && stake.AuthorizedClans.Contains(session.Identity.Clan)) && !stake.IsTerritoryControlTotem)
                {
                    Inventory stakeStorage = stake.GetComponent<Inventory>();
                    if (stakeStorage == null || stakeStorage?.GetSlotConf(0)?.StackRestriction == null || stakeStorage?.GetSlot(0)?.StackSize == null) continue;
                    int needs = stakeStorage.GetSlotConf(0).StackRestriction - stakeStorage.GetSlot(0).StackSize;
                    userStakes.Add(new UserStake(stake, needs));
                }
            }
            return new UserStakes(userStakes);
        }

        private int GetAmber(PlayerSession session)
        {
            PlayerInventory pinv = session.WorldPlayerEntity.GetComponent<PlayerInventory>();
            if (pinv == null) return 0;
            int TotalAmber = 0;
            for (var i = 0; i < pinv.Capacity; i++)
            {
                ItemObject slot = pinv.GetSlot(i);
                if (slot == null) continue;
                var guid = RuntimeHurtDB.Instance.GetGuid(slot?.Generator);
                if (guid == null) continue;
                if (guid != AmberGuid) continue;
                TotalAmber += slot.StackSize;
            }
            return TotalAmber;
        }

        private void TakeAmber(PlayerSession session, int a)
        {
            var amount = a;
            PlayerInventory pinv = session.WorldPlayerEntity.GetComponent<PlayerInventory>();
            if (pinv == null) return;
            for (var i = 0; i < pinv.Capacity; i++)
            {
                ItemObject slot = pinv.GetSlot(i);
                if (slot == null) continue;
                var guid = RuntimeHurtDB.Instance.GetGuid(slot?.Generator);
                if (guid == null) continue;
                if (guid != AmberGuid) continue;

                if (slot.StackSize >= amount)
                {
                    slot.StackSize = slot.StackSize - amount;
                    slot.InvalidateStack();
                    pinv.Invalidate();
                    return;
                }
                else
                {
                    amount = amount - slot.StackSize;
                    slot.StackSize = slot.StackSize - slot.StackSize;
                    slot.InvalidateStack();
                    pinv.Invalidate();
                }
            }
        }
        #endregion
    }
}