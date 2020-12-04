//#define DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Keywords", "Wulf", "1.2.1")]
    [Description("Get notified when a keyword is triggered in chat")]
    public class Keywords : CovalencePlugin
    {
        #region Configuration

        private Configuration config;

        public class Configuration
        {
            [JsonProperty("Permission required to trigger keywords")]
            public bool UsePermissions = false;

            [JsonProperty("Include original message with notification")]
            public bool IncludeOriginal = true;

            [JsonProperty("Match only exact keywords")]
            public bool MatchExact = true;

            [JsonProperty("Auto-reply for triggered keywords")]
            public bool AutoReply = false;

            [JsonProperty("Notify configured players in chat")]
            public bool NotifyInChat = false;

            [JsonProperty("IDs to notify in chat", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> IDsToNotify = new List<string> { "PLAYER_ID", "PLAYER_ID_2" };

            [JsonProperty("Notify configured group in chat")]
            public bool NotifyGroupInChat = true;

            [JsonProperty("Groups to notify in chat", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> GroupsToNotify = new List<string> { "admin", "moderator" };

#if RUST
            [JsonProperty("Notify using GUI Announcements")]
            public bool NotifyUsingGUI = false;

            [JsonProperty("Banner color to use for GUI (RGBA or color name)")]
            public string BannerColorGUI = "0.1 0.1 0.1 0.7";

            [JsonProperty("Text color to use for GUI (RGB or color name)")]
            public string TextColorGUI = "1 1 1";
#endif

            [JsonProperty("Notify in Discord channel")]
            public bool NotifyInDiscord = false;

            [JsonProperty("Roles to mention on Discord", ObjectCreationHandling = ObjectCreationHandling.Replace)] // <@&ROLEID> for roles
            public List<string> RolesToMention = new List<string> { "305751989176762388" };

            [JsonProperty("Users to mention on Discord", ObjectCreationHandling = ObjectCreationHandling.Replace)] // <@USERID> for users
            public List<string> UsersToMention = new List<string> { "97031326011506688" };

            [JsonProperty("Discord webhook URL")]
            public string DiscordWebhook = "https://support.discordapp.com/hc/en-us/articles/228383668-Intro-to-Webhooks";

            [JsonProperty("Keywords to listen for in chat", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Keywords = new List<string> { "admin", "crash", "bug" };

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
                ["AutoReply"] = "Your message has triggered a notification to admin",
                ["KeywordsChat"] = "{0} ({1}) has used the keyword(s): {2}",
                ["KeywordsDiscord"] = "Keyword(s) ({0}) have been used by {1} ({2})"
            }, this);
        }

        #endregion Localization

        #region Initialization

        [PluginReference]
        private Plugin BetterChat, GUIAnnouncements;

        private const string permUse = "keywords.use";

        private List<string> keywords = new List<string>();

        private void OnServerInitialized()
        {
            permission.RegisterPermission(permUse, this);

            if (BetterChat != null && BetterChat.IsLoaded)
            {
                Unsubscribe(nameof(OnUserChat));
            }

            keywords = config.Keywords.Select(x => x.ToLower()) as List<string>;
        }

        #endregion Initialization

        #region Chat Handling

        private List<string> keywordMatches;

        private void HandleChat(IPlayer player, string message)
        {
            if (player == null || string.IsNullOrEmpty(message) || config.UsePermissions && !player.HasPermission(permUse))
            {
                return;
            }

            if (config.MatchExact)
            {
                // These are exact matches
                keywordMatches = message.ToLower().Split(' ').Intersect(config.Keywords, StringComparer.OrdinalIgnoreCase).ToList(); // TODO: Find alternative to Linq
            }
            else
            {
                // These are close matches
                string[] split = message.ToLower().Split(' ');
                for (int i = 0; i < split.Length; i++)
                {
                    if (keywords.Contains(split[i]))
                    {
                        keywordMatches.Add(split[i]);
                    }
                }
            }

            if (keywordMatches.Count > 0)
            {
#if DEBUG
                LogWarning($"Keyword(s) triggered by {player.Name} ({player.Id})! {string.Join(", ", keywordMatches.ToArray())}");
#endif
                if (config.NotifyInChat)
                {
                    IPlayer target = null;
                    foreach (string targetId in config.IDsToNotify)
                    {
                        target = players.FindPlayerById(targetId);
                        if (target != null && target.IsConnected)
                        {
                            string notification = GetLang("KeywordsChat", target.Id, player.Name, player.Id, string.Join(", ", keywordMatches.ToArray()));
                            target.Reply(config.IncludeOriginal ? notification += $" | {message}" : notification);
#if RUST
                            if (config.NotifyUsingGUI)
                            {
                                GUIAnnouncements?.Call("CreateAnnouncement", notification, config.BannerColorGUI, config.TextColorGUI, target);
                            }
#endif
                        }
                    }
                }

                if (config.NotifyGroupInChat)
                {
                    foreach (IPlayer target in players.Connected)
                    {
                        foreach (string group in config.GroupsToNotify)
                        {
                            if (target.BelongsToGroup(group.ToLower()))
                            {
                                string notification = GetLang("KeywordsChat", target.Id, player.Name, player.Id, string.Join(", ", keywordMatches.ToArray()));
                                target.Reply(config.IncludeOriginal ? notification += $" | {message}" : notification);
#if RUST
                                if (config.NotifyUsingGUI)
                                {
                                    GUIAnnouncements?.Call("CreateAnnouncement", notification, config.BannerColorGUI, config.TextColorGUI, target);
                                }
#endif
                            }
                        }
                    }
                }

                if (config.NotifyInDiscord)
                {
                    string notification = GetLang("KeywordsDiscord", null, string.Join(", ", keywordMatches.ToArray()), player.Name, player.Id); // TODO: Use StringBuilder
                    if (config.IncludeOriginal)
                    {
                        notification += $" | {message}";
                    }

                    if (config.RolesToMention?.Count > 0)
                    {
                        foreach (string role in config.RolesToMention)
                        {
                            if (!string.IsNullOrEmpty(role))
                            {
                                notification += $" <@&{role}>";
                            }
                        }
                    }
                    if (config.UsersToMention?.Count > 0)
                    {
                        foreach (string user in config.UsersToMention)
                        {
                            if (!string.IsNullOrEmpty(user))
                            {
                                notification += $" <@{user}>";
                            }
                        }
                    }
                    notification = $"{{\"content\": \"{notification}\"}}";
#if DEBUG
                    LogWarning($"DEBUG: {notification}");
#endif

                    webrequest.Enqueue(config.DiscordWebhook, notification, (code, response) =>
                    {
#if DEBUG
                        LogWarning($"DEBUG: {config.DiscordWebhook}");
                        if (!string.IsNullOrEmpty(response))
                        {
                            LogWarning($"DEBUG: {response}");
                        }
#endif
                        if (code != 204)
                        {
                            LogWarning($"Discord API responded with code {code}");
                        }
                    }, this, RequestMethod.POST, new Dictionary<string, string> { ["Content-Type"] = "application/json" });
                }

                if (config.AutoReply)
                {
                    player.Reply(GetLang("AutoReply", player.Id));
                }
            }
        }

        private void OnBetterChat(Dictionary<string, object> data)
        {
            HandleChat(data["Player"] as IPlayer, data["Message"] as string);
        }

        private void OnUserChat(IPlayer player, string message) => HandleChat(player, message);

        #endregion Chat Handling

        #region Helpers

        private string GetLang(string langKey, string playerId = null, params object[] args)
        {
            return string.Format(lang.GetMessage(langKey, this, playerId), args);
        }

        #endregion Helpers
    }
}
