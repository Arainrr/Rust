﻿﻿//#define DEBUG

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Heal", "Wulf", "3.0.1")]
    [Description("Allows players with permission to heal themselves or others")]
    internal class Heal : CovalencePlugin
    {
        #region Configuration

        private Configuration config;

        private class Configuration
        {
            [JsonProperty("Command cooldown in seconds (0 to disable)")]
            public int CommandCooldown = 30;

            [JsonProperty("Maximum heal amount")]
            public int MaxHealAmount = 100;

            [JsonProperty("Notify target when healed")]
            public bool NotifyTarget = true;

            [JsonProperty("Use permission system")]
            public bool UsePermissions = true;

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }

        protected override void LoadDefaultConfig() => config = new Configuration();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    throw new JsonException();
                }

                if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys))
                {
                    LogWarning("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch
            {
                LogWarning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            LogWarning($"Configuration changes saved to {Name}.json");
            Config.WriteObject(config, true);
        }

        #endregion Configuration

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CommandCooldown"] = "Wait a bit before attempting to use '{0}' again",
                ["CommandHeal"] = "heal",
                ["CommandHealAll"] = "healall",
                ["CommandHealPlayer"] = "healplayer",
                ["NotAllowed"] = "You are not allowed to use the '{0}' command",
                ["NoPlayersFound"] = "No players found with name or ID '{0}'",
                ["NoPlayersOnline"] = "There are no players online to heal",
                ["PlayerNotFound"] = "Player '{0}' was not found",
                ["PlayerWasHealed"] = "{0} was healed {1}",
                ["PlayerWasNotHealed"] = "{0} was unable to be healed",
                ["PlayersFound"] = "Multiple players were found, please specify: {0}",
                ["PlayersOnly"] = "Command '{0}' can only be used by players",
                ["PlayersHealed"] = "All players have been healed {0}!",
                ["YouWereHealed"] = "You were healed {0}",
                ["YouWereNotHealed"] = "You were unable to be healed",
                ["UsageHeal"] = "Usage: {0} [amount] -- Heal self by specfied or default amount",
                ["UsageHealAll"] = "Usage: {0} [amount] -- Heal all players by specfied or default amount",
                ["UsageHealPlayer"] = "Usage: {0} <player name or id> [amount] -- Heal player by specfied or default amount"
            }, this);
        }

        #endregion Localization

        #region Initialization

        private readonly Hash<string, float> cooldowns = new Hash<string, float>();

        private const string permSelf = "heal.self";
        private const string permAll = "heal.all";
        private const string permPlayer = "heal.player";

        private void Init()
        {
            AddLocalizedCommand(nameof(CommandHeal));
            AddLocalizedCommand(nameof(CommandHealAll));
            AddLocalizedCommand(nameof(CommandHealPlayer));

            permission.RegisterPermission(permSelf, this);
            permission.RegisterPermission(permAll, this);
            permission.RegisterPermission(permPlayer, this);
            MigratePermission("healer.self", permSelf);
            MigratePermission("healer.all", permAll);
            MigratePermission("healer.others", permPlayer);
        }

        #endregion Initialization

        private bool HasCooldown(IPlayer player, string command)
        {
            if (config.CommandCooldown > 0 && !player.IsServer)
            {
                if (!cooldowns.ContainsKey(player.Id))
                {
                    cooldowns.Add(player.Id, 0f);
                }

                if (cooldowns[player.Id] + config.CommandCooldown > Interface.Oxide.Now)
                {
                    Message(player, "CommandCooldown", command);
                    return true;
                }
            }

            return false;
        }

        private bool HealPlayer(IPlayer target, float amount)
        {
            float health = target.Health;
            float healthDiff = target.MaxHealth - health;
            amount = amount < healthDiff ? amount : healthDiff;
            if (target.Health < target.MaxHealth)
            {
                target.Heal(amount);
            }
#if DEBUG
            Puts($"Pre health: {health}");
            Puts($"Post health: {target.Health}");
            Puts($"Pre health + amount: {health + amount}");
            Puts($"Healed? {target.Health >= health + amount}");
#endif
            return target.Health >= health + amount;
        }

        #region Heal Command

        private void CommandHeal(IPlayer player, string command, string[] args)
        {
            if (player.IsServer)
            {
                Message(player, "PlayersOnly", command);
                return;
            }

            if (config.UsePermissions && !player.HasPermission(permSelf))
            {
                Message(player, "NotAllowed", command);
                return;
            }

            if (HasCooldown(player, command))
            {
                return;
            }

            float amount = args.Length >= 1 && float.TryParse(args[0], out amount) && amount > 0f ? amount : config.MaxHealAmount;
            if (HealPlayer(player, amount))
            {
                if (config.NotifyTarget)
                {
                    Message(player, "YouWereHealed", amount);
                }
                if (config.CommandCooldown > 0)
                {
                    cooldowns[player.Id] = Interface.Oxide.Now;
                }
            }
            else
            {
                Message(player, "YouWereNotHealed", player.Name.Sanitize());
            }
        }

        #endregion Heal Command

        #region Heal Player Command

        private void CommandHealPlayer(IPlayer player, string command, string[] args)
        {
            if (!config.UsePermissions && !player.IsAdmin || !player.HasPermission(permPlayer))
            {
                Message(player, "NotAllowed", command);
                return;
            }

            if (HasCooldown(player, command))
            {
                return;
            }

            if (args.Length < 1)
            {
                Message(player, "UsageHealPlayer", command);
                return;
            }

            IPlayer target = FindPlayer(args[0], player);
            if (target == null)
            {
                return;
            }

            float amount = args.Length > 1 && float.TryParse(args.Last(), out amount) && amount > 0f ? amount : config.MaxHealAmount;
            if (HealPlayer(player, amount))
            {
                if (config.CommandCooldown > 0)
                {
                    cooldowns[player.Id] = Interface.Oxide.Now;
                }
                if (!Equals(target.Id, player.Id))
                {
                    Message(player, "PlayerWasHealed", target.Name.Sanitize(), amount);
                }
            }
            else
            {
                Message(player, "PlayerWasNotHealed", target.Name.Sanitize());
            }
        }

        #endregion Heal Player Command

        #region Heal All Command

        private void CommandHealAll(IPlayer player, string command, string[] args)
        {
            if (!config.UsePermissions && !player.IsAdmin || !player.HasPermission(permAll))
            {
                Message(player, "NotAllowed", command);
                return;
            }

            if (HasCooldown(player, command))
            {
                return;
            }

            if (!players.Connected.Any())
            {
                Message(player, "NoPlayersOnline");
                return;
            }

            float amount = args.Length > 1 && float.TryParse(args.Last(), out amount) && amount > 0f ? amount : config.MaxHealAmount;
            foreach (IPlayer target in players.Connected)
            {
                HealPlayer(target, amount);
            }
            if (config.CommandCooldown > 0)
            {
                cooldowns[player.Id] = Interface.Oxide.Now;
            }
            Message(player, "PlayersHealed", amount);
        }

        #endregion Heal All Command

        #region Helpers

        private IPlayer FindPlayer(string playerNameOrId, IPlayer player)
        {
            IPlayer[] foundPlayers = players.FindPlayers(playerNameOrId).ToArray();
            if (foundPlayers.Length > 1)
            {
                Message(player, "PlayersFound", string.Join(", ", foundPlayers.Select(p => p.Name).Take(10).ToArray()).Truncate(60));
                return null;
            }

            IPlayer target = foundPlayers.Length == 1 ? foundPlayers[0] : null;
            if (target == null)
            {
                Message(player, "NoPlayersFound", playerNameOrId);
                return null;
            }

            return target;
        }

        private void AddLocalizedCommand(string command)
        {
            foreach (string language in lang.GetLanguages(this))
            {
                Dictionary<string, string> messages = lang.GetMessages(language, this);
                foreach (KeyValuePair<string, string> message in messages)
                {
                    if (message.Key.Equals(command))
                    {
                        if (!string.IsNullOrEmpty(message.Value))
                        {
                            AddCovalenceCommand(message.Value, command);
                        }
                    }
                }
            }
        }

        private string GetLang(string langKey, string playerId = null, params object[] args)
        {
            return string.Format(lang.GetMessage(langKey, this, playerId), args);
        }

        private void Message(IPlayer player, string textOrLang, params object[] args)
        {
            if (player.IsConnected)
            {
                string message = GetLang(textOrLang, player.Id, args);
                player.Reply(message != textOrLang ? message : textOrLang);
            }
        }

        private void MigratePermission(string oldPerm, string newPerm)
        {
            foreach (string groupName in permission.GetPermissionGroups(oldPerm))
            {
                permission.GrantGroupPermission(groupName, newPerm, null);
                permission.RevokeGroupPermission(groupName, oldPerm);
            }

            foreach (string playerId in permission.GetPermissionUsers(oldPerm))
            {
                permission.GrantUserPermission(Regex.Replace(playerId, "[^0-9]", ""), newPerm, null);
                permission.RevokeUserPermission(Regex.Replace(playerId, "[^0-9]", ""), oldPerm);
            }
        }

        #endregion Helpers
    }
}
