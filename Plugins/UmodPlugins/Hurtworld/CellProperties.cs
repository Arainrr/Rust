﻿using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Cell Properties", "Mr. Blue", "2.0.1")]
    [Description("Command to check stake properties of construction cells.")]
    class CellProperties : CovalencePlugin
    {
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "No Stake", "<color=orange>[Cell Properties]</color> There is no stake in this cell!" },
                { "Header", "<size=16><color=orange>[Cell Properties]</color> Available Properties:</size>" },
                { "Stake Type", "Stake Type: <color=orange><b>{type}</b></color>" },
                { "Stake Name", "Stake Name: <color=orange><b>{name}</b></color>" },
                { "Player Header", "Authorized Players:" },
                { "Clan Header", "Clan: <color=orange><b>{name}</b> ({tag})</color> Members:" },
                { "No Authorized Clans", "- No clans authorized on this stake." },
                { "No Authorized Players", "- No players authorized on this stake." },
                { "Stake Player Online", "- Online <color=green>{name}</color> ({id})" },
                { "Stake Player Offline", "- Offline <color=red>{name}</color> ({id})" },
                { "Clan Player Online", "- Online <color=green>{name}</color> ({id}) {rank}" },
                { "Clan Player Offline", "- Offline <color=red>{name}</color> ({id}) {rank}" }
            }, this);
        }

        private string Msg(string key, IPlayer player) => lang.GetMessage(key, this, player.Id);

        public OwnershipStakeServer GetOwnerStake(Vector3 vector3) => ConstructionManager.Instance.GetOwnerStake(vector3);

        [Command("cell"), Permission("cellproperties.use")]
        private void cellCommand(IPlayer player, string command, string[] args) => player.Reply(GetMessage(player).Trim());

        private string GetMessage(IPlayer player)
        {
            GenericPosition pos = player.Position();
            OwnershipStakeServer stake = GetOwnerStake(new Vector3(pos.X, pos.Y, pos.Z));
            if (stake == null) return Msg("No Stake", player);
            
            StringBuilder output = new StringBuilder();

            output.AppendLine(Msg("Header", player));
            output.AppendLine(Msg("Stake Type", player).Replace("{type}", (stake.IsClanTotem ? (stake.IsTerritoryControlTotem ? "Territory" : "Clan") : "Normal")));
            output.AppendLine(Msg("Stake Name", player).Replace("{name}", (stake.TerritoryName == string.Empty ? "Not Set" : stake.TerritoryName)));

            if (stake.IsClanTotem)
            {
                if (stake.AuthorizedClans.Count == 0)
                {
                    output.AppendLine(Msg("No Authorized Clans", player));
                    return output.ToString();
                }

                foreach (Clan clan in stake.AuthorizedClans)
                {
                    output.AppendLine(Msg("Clan Header", player).Replace("{name}", clan.ClanName).Replace("{tag}", clan.ClanTag));
                    foreach (ulong playerId in clan.GetMemebers())
                    {
                        PlayerIdentity identity = GameManager.Instance.GetIdentity(playerId);
                        EClanRank rank = clan.GetClanRank(playerId);
                        string msg = identity?.ConnectedSession != null && identity.ConnectedSession.IsLoaded ? Msg("Clan Player Online", player) : Msg("Clan Player Offline", player);

                        output.AppendLine(msg.Replace("{name}", identity.Name).Replace("{id}", identity.SteamId.ToString()).Replace("{rank}", rank.ToString()));
                    }
                }
            }
            else
            {
                if (stake.AuthorizedPlayers.Count == 0)
                {
                    output.AppendLine(Msg("No Authorized Players", player));
                    return output.ToString();
                }

                output.AppendLine(Msg("Player Header", player));

                foreach (PlayerIdentity identity in stake.AuthorizedPlayers)
                {
                    string msg = identity?.ConnectedSession != null && identity.ConnectedSession.IsLoaded ? Msg("Stake Player Online", player) : Msg("Stake Player Offline", player);

                    output.AppendLine(msg.Replace("{name}", identity.Name).Replace("{id}", identity.SteamId.ToString()));
                }
            }

            return output.ToString();
        }
    }
}