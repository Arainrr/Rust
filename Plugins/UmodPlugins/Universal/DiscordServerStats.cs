﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using WebSocketSharp;

namespace Oxide.Plugins
{
    [Info("Discord Server Stats", "MJSU", "1.0.6")]
    [Description("Displays stats about the server in discord")]
    public class DiscordServerStats : CovalencePlugin
    {
        #region Class Fields

        [PluginReference] private Plugin PlaceholderAPI;
        
        private StoredData _storedData; //Plugin Data
        private PluginConfig _pluginConfig; //Plugin Config
        private Dictionary<string, string> _headers;

        private const string DiscordBaseUrl = "https://discordapp.com/api/v6/";
        private const string CreateDiscordMessageUrl = DiscordBaseUrl + "channels/{0}/messages";
        private const string UpdateDiscordMessageUrl = DiscordBaseUrl + "channels/{0}/messages/{1}";
        private const string GatewayUrl = DiscordBaseUrl + "/gateway";

        private readonly Hash<string, DateTime> _joinedDate = new Hash<string, DateTime>();

        private Action<IPlayer, StringBuilder> _replacer;

        private DateTime _lastUpdate = DateTime.UtcNow;

        private Timer _updateTimer;
        #endregion

        #region Setup & Loading
        private void Init()
        {
            _storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Loading Default Config");
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            Config.Settings.DefaultValueHandling = DefaultValueHandling.Populate;
            _pluginConfig = AdditionalConfig(Config.ReadObject<PluginConfig>());
            Config.WriteObject(_pluginConfig);
        }

        private PluginConfig AdditionalConfig(PluginConfig config)
        { 
            config.StatsEmbed = new DiscordMessageConfig
            {
                Content = config.StatsEmbed?.Content ?? string.Empty,
                Embed = new EmbedConfig
                {
                    Title = config.StatsEmbed?.Embed?.Title ?? "{server.name}",
                    Description = config.StatsEmbed?.Embed?.Description ?? "Live Server Stats",
                    Url = config.StatsEmbed?.Embed?.Url ?? string.Empty,
                    Color = config.StatsEmbed?.Embed?.Color ?? "#de8732",
                    Image = config.StatsEmbed?.Embed?.Image ?? string.Empty,
                    Thumbnail = config.StatsEmbed?.Embed?.Thumbnail ?? string.Empty,
                    Fields = config.StatsEmbed?.Embed?.Fields ?? new List<FieldConfig>
                    {
                        new FieldConfig
                        {
                            Title = "Online / Max Players",
                            Value = "{server.players} / {server.players.max}",
                            Inline = true,
                            Enabled = true
                        },
#if RUST
                        new FieldConfig
                        {
                            Title = "Sleepers",
                            Value = "{server.players.sleepers}",
                            Inline = true,
                            Enabled = true
                        },
                        new FieldConfig
                        {
                            Title = "Players Loading",
                            Value = "{server.players.loading}",
                            Inline = true,
                            Enabled = true
                        },
                        new FieldConfig
                        {
                            Title = "Players In Queue",
                            Value = "{server.players.queued}",
                            Inline = true,
                            Enabled = true
                        },   
#endif
                        new FieldConfig
                        {
                            Title = "In Game Time",
                            Value = "{server.time:hh:mm:ss tt}",
                            Inline = true,
                            Enabled = true
                        },   
#if RUST
                        new FieldConfig
                        {
                            Title = "Map Entities",
                            Value = "{server.entities}",
                            Inline = true,
                            Enabled = true
                        },
                        new FieldConfig
                        {
                            Title = "Server Framerate",
                            Value = "{server.fps}",
                            Inline = true,
                            Enabled = true
                        },
                        new FieldConfig
                        {
                            Title = "Seed",
                            Value = "[{world.seed}](https://rustmaps.com/map/{world.size}_{world.seed})",
                            Inline = true,
                            Enabled = true
                        },
                        new FieldConfig
                        {
                            Title = "Size",
                            Value = "{world.size!km}km ({world.size!km^2}km^2)",
                            Inline = true,
                            Enabled = true
                        },
#endif
                        new FieldConfig
                        {
                            Title = "Protocol",
                            Value = "{server.protocol}",
                            Inline = true,
                            Enabled = true
                        },
#if RUST
                        new FieldConfig
                        {
                            Title = "Memory Usage",
                            Value = "{server.memory.used:0.00!gb} GB / {server.memory.total:0.00!gb} GB" ,
                            Inline = true,
                            Enabled = true
                        },
                        new FieldConfig
                        {
                            Title = "Network IO",
                            Value = "In: {server.network.in:0.00!kb} KB/s Out: {server.network.out:0.00!kb} KB/s " ,
                            Inline = true,
                            Enabled = true
                        },
                        new FieldConfig
                        {
                            Title = "Last Wiped Date Time",
                            Value = "{server.map.wipe.last:MM/dd/yy hh:mm:ss tt!local}" ,
                            Inline = true,
                            Enabled = true
                        },
                        
                        new FieldConfig
                        {
                            Title = "Last Blueprint Wipe Date Time",
                            Value = "{server.blueprints.wipe.last:MM/dd/yy hh:mm:ss tt!local}" ,
                            Inline = true,
                            Enabled = true
                        },
#endif
                        new FieldConfig
                        {
                            Title = "Last Joined",
                            Value = "{player.joined.last}" ,
                            Inline = true,
                            Enabled = true
                        },
                        new FieldConfig
                        {
                            Title = "Last Disconnected",
                            Value = "{player.disconnected.last} ({player.disconnected.last.duration:%h}H {player.disconnected.last.duration:%m}M {player.disconnected.last.duration:%s}S)" ,
                            Inline = true,
                            Enabled = true
                        },
                        new FieldConfig
                        {
                            Title = "Click & Connect",
                            Value = "steam://connect/{server.address}:{server.port}",
                            Inline = false,
                            Enabled = true
                        }
                    },
                    Footer = new FooterConfig
                    {
                        IconUrl = config.StatsEmbed?.Embed?.Footer?.IconUrl ?? string.Empty,
                        Text = config.StatsEmbed?.Embed?.Footer?.Text ?? string.Empty,
                        Enabled = config.StatsEmbed?.Embed?.Footer?.Enabled ?? true
                    },
                    Enabled = config.StatsEmbed?.Embed?.Enabled ?? true
                }
            };
            
            return config;
        }

        private void OnServerInitialized()
        {
            _headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bot {_pluginConfig.DiscordBotKey}" ,
                ["Content-Type"] = "application/json" ,
                ["User-Agent"] = $"DiscordBot (${nameof(DiscordServerStats)}, {Version})" 
            };
            
            foreach (IPlayer player in players.Connected)
            {
                OnUserConnected(player);
            }
            
            if (PlaceholderAPI == null || !PlaceholderAPI.IsLoaded)
            {
                PrintError("Missing plugin dependency PlaceholderAPI: https://umod.org/plugins/placeholder-api");
                return;
            }
            
            if(PlaceholderAPI.Version < new VersionNumber(2, 0, 0))
            {
                PrintError("Placeholder API plugin must be version 2.0.0 or higher");
                return;
            }
        }

        private void OnServerSave()
        {
            NextTick(SaveData);
        }
        
        private void Unload()
        {
            SaveData();
            DisconnectWebsocket();
        }
        #endregion

        #region Hooks
        private void OnUserConnected(IPlayer player)
        {
            NextTick(() =>
            {
                _joinedDate[player.Id] = DateTime.Now;
                _storedData.LastConnected = player.Name;
            });
        }

        private void OnUserDisconnected(IPlayer player, string reason)
        {
            NextTick(() =>
            {
                _storedData.LastDisconnectedDuration = DateTime.Now - _joinedDate[player.Id];
                _storedData.LastDisconnected = player.Name;
            });
        }

        #endregion

        #region Message Handling
        private void SetupMessaging()
        {
            if (string.IsNullOrEmpty(_pluginConfig.DiscordBotKey))
            {
                PrintWarning("Discord Bot Key not set. Plugin will not work until set.");
                return;
            }

            if (string.IsNullOrEmpty(_pluginConfig.DiscordChannel))
            {
                PrintWarning("Discord Channel Id not set. Plugin will not work until set.");
                return;
            }

            if (string.IsNullOrEmpty(_storedData.PreviousBotKey))
            {
                PrintWarning("First time bot being used with plugin. Identifying bot with gateway.");
                ConnectToGateway();
                return;
            }
            
            if (_pluginConfig.DiscordBotKey != _storedData.PreviousBotKey)
            {
                PrintWarning("Bot key for plugin has changed. Identifying bot with gateway.");
                ConnectToGateway();
                return;
            }

            if (string.IsNullOrEmpty(_storedData.ExistingMessageId))
            {
                SendCreateMessage();
            }
            else
            {
                SendUpdateMessage();
            }

            _updateTimer?.Destroy();
            _updateTimer = timer.Every(_pluginConfig.UpdateInterval * 60, SendUpdateMessage);
        }
        
        private void SendCreateMessage()
        {
            _lastUpdate = DateTime.UtcNow;
            DiscordMessage create = ParseMessage(_pluginConfig.StatsEmbed);
            CreateDiscordMessage(create, new Action<int, DiscordMessage>((code, response) =>
            {
                if (code == 404)
                {
                    PrintWarning("Create message returned 404. Please confirm channel id in config is correct.");
                    return;
                }
                
                if (response == null)
                {
                    PrintWarning($"Created message returned null. Code: {code}");
                    return;
                }
                    
                _storedData.ExistingMessageId = response.Id;
                SaveData();
            }) );
        }

        private void SendUpdateMessage()
        {
            if (string.IsNullOrEmpty(_storedData.ExistingMessageId))
            {
                SendCreateMessage();
                return;
            }

            _lastUpdate = DateTime.UtcNow;
            DiscordMessage update = ParseMessage(_pluginConfig.StatsEmbed);
            update.Id = _storedData.ExistingMessageId;

            UpdateDiscordMessage(update, new Action<int, DiscordMessage>( (code,response) =>
            {
                if (code == 404 && response == null)
                {
                    SendCreateMessage();
                }
            }));
        }
        #endregion

        #region PlaceholderAPI
        private string ParseFields(string json)
        {
            StringBuilder sb = new StringBuilder(json);
            
            GetReplacer()?.Invoke(null, sb);
            
            sb.Replace("\\n", "\n");
            return sb.ToString();
        }

        private void OnPluginUnloaded(Plugin plugin)
        {
            if (plugin?.Name == "PlaceholderAPI")
            {
                _replacer = null;
            }
        }
        
        private void OnPlaceholderAPIReady()
        {
            timer.In(1f, () =>
            {
                RegisterPlaceholder("player.joined.last", (player, s) => _storedData.LastConnected, "Displays the name of the player who joined last");
                RegisterPlaceholder("player.disconnected.last", (player, s) => _storedData.LastDisconnected, "Displays the name of the player who disconnected last");
                RegisterPlaceholder("player.disconnected.last.duration", (player, s) => _storedData.LastDisconnectedDuration, "Displays duration of the last disconnected player");
                RegisterPlaceholder("discordserverstats.last.update", (player, s) => _lastUpdate, "Displays the datetime the last update was sent.");
                SetupMessaging();
            });
        }

        private void RegisterPlaceholder(string key, Func<IPlayer, string, object> action, string description = null)
        {
            if (IsPlaceholderApiLoaded())
            {
                PlaceholderAPI.Call("AddPlaceholder", this, key, action, description);
            }
        }

        private Action<IPlayer, StringBuilder> GetReplacer()
        {
            if (!IsPlaceholderApiLoaded())
            {
                return _replacer;
            }
            
            return _replacer ?? (_replacer = PlaceholderAPI.Call<Action<IPlayer, StringBuilder>>("GetProcessPlaceholders"));
        }

        private bool IsPlaceholderApiLoaded() => PlaceholderAPI != null && PlaceholderAPI.IsLoaded;
        #endregion

        #region Helper Methods
        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _storedData);
        #endregion

        #region Classes
        private class PluginConfig
        {
            [DefaultValue("")]
            [JsonProperty(PropertyName = "Discord Bot Key")]
            public string DiscordBotKey { get; set; }
            
            [DefaultValue("")]
            [JsonProperty(PropertyName = "Discord Channel Id")]
            public string DiscordChannel { get; set; }
            
            [DefaultValue(1f)]
            [JsonProperty(PropertyName = "Message Update Interval (Minutes)")]
            public float UpdateInterval { get; set; }

            [JsonProperty(PropertyName = "Stats Embed Message")]
            public DiscordMessageConfig StatsEmbed { get; set; }
        }

        private class StoredData
        {
            public string ExistingMessageId { get; set; }
            public string PreviousBotKey { get; set; }
            public string LastConnected { get; set; } = "N/A";
            public string LastDisconnected { get; set; } = "N/A";
            public TimeSpan LastDisconnectedDuration = TimeSpan.Zero;
        }

        private class RestResponse
        {
            private string Data { get; }

            public RestResponse(string data)
            {
                Data = data;
            }

            public T ParseData<T>() => JsonConvert.DeserializeObject<T>(Data);
        }
        #endregion

        #region Discord Embed
        #region Send Embed Methods

        /// <summary>
        /// Sends the DiscordMessage
        /// </summary>
        /// <param name="message">Message being sent</param>
        /// <param name="callback"></param>
        private void CreateDiscordMessage<T>(DiscordMessage message, Action<int, T> callback)
        {
            string json = ParseFields( message.ToJson());
            webrequest.Enqueue(string.Format(CreateDiscordMessageUrl, _pluginConfig.DiscordChannel), json, (code, response) => SendDiscordMessageCallback(code, response, callback), this, RequestMethod.POST, _headers);
        }

        /// <summary>
        /// Sends the DiscordMessage
        /// </summary>
        /// <param name="message">Message being sent</param>
        /// <param name="callback"></param>
        private void UpdateDiscordMessage<T>(DiscordMessage message, Action<int, T> callback)
        {
            string json = ParseFields(message.ToJson());
            webrequest.Enqueue(string.Format(UpdateDiscordMessageUrl, _pluginConfig.DiscordChannel, message.Id), json, (code, response) => SendDiscordMessageCallback(code, response, callback), this, RequestMethod.PATCH, _headers);
        }

        /// <summary>
        /// Callback when sending the embed if any errors occured
        /// </summary>
        /// <param name="code">HTTP response code</param>
        /// <param name="message">Response message</param>
        /// <param name="callback"></param>
        private void SendDiscordMessageCallback<T>(int code, string message, Action<int, T> callback)
        {
            if (code == 404)
            {
                callback?.Invoke(code, default(T));
                return;
            }
            
            if (code != 204 && code != 200)
            {
                PrintError(message);
                callback?.Invoke(code, default(T));
                return;
            }
            
            RestResponse response = new RestResponse(message);
            callback?.Invoke(code, response.ParseData<T>());
        }
        #endregion
        
        #region Helper Methods

        private const string OwnerIcon = "https://steamcdn-a.akamaihd.net/steamcommunity/public/images/avatars/47/47db946f27bc76d930ac82f1656f7a10707bb67d_full.jpg";

        private void AddPluginInfoFooter(Embed embed)
        {
            embed.AddFooter($"{Title} V{Version} by {Author}", OwnerIcon);
        }
        #endregion
        
        #region Embed Classes

        private class DiscordMessage
        {
            /// <summary>
            /// The id of the message
            /// </summary>
            [JsonProperty("id")]
            public string Id { get; set; }
            
            /// <summary>
            /// The name of the user sending the message changing this will change the webhook bots name
            /// </summary>
            [JsonProperty("username")]
            private string Username { get; set; }

            /// <summary>
            /// The avatar url of the user sending the message changing this will change the webhook bots avatar
            /// </summary>
            [JsonProperty("avatar_url")]
            private string AvatarUrl { get; set; }

            /// <summary>
            /// String only content to be sent
            /// </summary>
            [JsonProperty("content")]
            private string Content { get; set; }

            /// <summary>
            /// Embeds to be sent
            /// </summary>
            [JsonProperty("embed")]
            private Embed Embed { get; set; }

            [JsonConstructor]
            public DiscordMessage()
            {

            }

            public DiscordMessage(string username = null, string avatarUrl = null)
            {
                Username = username;
                AvatarUrl = avatarUrl;
            }

            public DiscordMessage(string content, string username = null, string avatarUrl = null)
            {
                Content = content;
                Username = username;
                AvatarUrl = avatarUrl;
            }

            public DiscordMessage(Embed embed, string username = null, string avatarUrl = null)
            {
                Embed = embed;
                Username = username;
                AvatarUrl = avatarUrl;
            }

            /// <summary>
            /// Adds a new embed to the list of embed to send
            /// </summary>
            /// <param name="embed">Embed to add</param>
            /// <returns>This</returns>
            /// <exception cref="IndexOutOfRangeException">Thrown if more than 1 embeds are added in a send as that is the discord limit</exception>
            public DiscordMessage AddEmbed(Embed embed)
            {
                if (Embed != null)
                {
                    throw new IndexOutOfRangeException("Only 1 embed are allowed per message");
                }

                Embed = embed;
                return this;
            }

            /// <summary>
            /// Adds string content to the message
            /// </summary>
            /// <param name="content"></param>
            /// <returns></returns>
            public DiscordMessage AddContent(string content)
            {
                Content = content;
                return this;
            }

            /// <summary>
            /// Changes the username and avatar image for the bot sending the message
            /// </summary>
            /// <param name="username">username to change</param>
            /// <param name="avatarUrl">avatar img url to change</param>
            /// <returns>This</returns>
            public DiscordMessage AddSender(string username, string avatarUrl)
            {
                Username = username;
                AvatarUrl = avatarUrl;
                return this;
            }

            /// <summary>
            /// Returns message as JSON to be sent in the web request
            /// </summary>
            /// <returns></returns>
            public string ToJson() => JsonConvert.SerializeObject(this, Formatting.None,
                new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore});
        }

        private class Embed
        {
            /// <summary>
            /// Color of the left side bar of the embed message
            /// </summary>
            [JsonProperty("color")]
            private int Color { get; set; }

            /// <summary>
            /// Fields to be added to the embed message
            /// </summary>
            [JsonProperty("fields")]
            private List<Field> Fields { get; } = new List<Field>();

            /// <summary>
            /// Title of the embed message
            /// </summary>
            [JsonProperty("title")]
            private string Title { get; set; }

            /// <summary>
            /// Description of the embed message
            /// </summary>
            [JsonProperty("description")]
            private string Description { get; set; }
            
            /// <summary>
            /// Description of the embed message
            /// </summary>
            [JsonProperty("url")]
            private string Url { get; set; }

            /// <summary>
            /// Image to added to the embed message. Appears at the bottom of the message above the footer
            /// </summary>
            [JsonProperty("image")]
            private Image Image { get; set; }

            /// <summary>
            /// Thumbnail image added to the embed message. Appears in the top right corner
            /// </summary>
            [JsonProperty("thumbnail")]
            private Image Thumbnail { get; set; }

            /// <summary>
            /// Video to add to the embed message
            /// </summary>
            [JsonProperty("video")]
            private Video Video { get; set; }

            /// <summary>
            /// Author to add to the embed message. Appears above the title.
            /// </summary>
            [JsonProperty("author")]
            private AuthorInfo Author { get; set; }

            /// <summary>
            /// Footer to add to the embed message. Appears below all content.
            /// </summary>
            [JsonProperty("footer")]
            private Footer Footer { get; set; }

            /// <summary>
            /// Adds a title to the embed message
            /// </summary>
            /// <param name="title">Title to add</param>
            /// <returns>This</returns>
            public Embed AddTitle(string title)
            {
                Title = title;
                return this;
            }

            /// <summary>
            /// Adds a description to the embed message
            /// </summary>
            /// <param name="description">description to add</param>
            /// <returns>This</returns>
            public Embed AddDescription(string description)
            {
                Description = description;
                return this;
            }
            
            /// <summary>
            /// Adds a url to the embed message
            /// </summary>
            /// <param name="url"></param>
            /// <returns>This</returns>
            public Embed AddUrl(string url)
            {
                Url = url;
                return this;
            }

            /// <summary>
            /// Adds an author to the embed message. The author will appear above the title
            /// </summary>
            /// <param name="name">Name of the author</param>
            /// <param name="iconUrl">Icon Url to use for the author</param>
            /// <param name="url">Url to go to when the authors name is clicked on</param>
            /// <param name="proxyIconUrl">Backup icon url. Can be left null if you only have one icon url</param>
            /// <returns>This</returns>
            public Embed AddAuthor(string name, string iconUrl = null, string url = null, string proxyIconUrl = null)
            {
                Author = new AuthorInfo(name, iconUrl, url, proxyIconUrl);
                return this;
            }

            /// <summary>
            /// Adds a footer to the embed message
            /// </summary>
            /// <param name="text">Text to be added to the footer</param>
            /// <param name="iconUrl">Icon url to add in the footer. Appears to the left of the text</param>
            /// <param name="proxyIconUrl">Backup icon url. Can be left null if you only have one icon url</param>
            /// <returns>This</returns>
            public Embed AddFooter(string text, string iconUrl = null, string proxyIconUrl = null)
            {
                Footer = new Footer(text, iconUrl, proxyIconUrl);

                return this;
            }

            /// <summary>
            /// Adds an int based color to the embed. Color appears as a bar on the left side of the message
            /// </summary>
            /// <param name="color"></param>
            /// <returns></returns>
            public Embed AddColor(int color)
            {
                if (color < 0x0 || color > 0xFFFFFF)
                {
                    throw new Exception($"Color '{color}' is outside the valid color range");
                }
                
                Color = color;
                return this;
            }

            /// <summary>
            /// Adds a hex based color. Color appears as a bar on the left side of the message
            /// </summary>
            /// <param name="color">Color in string hex format</param>
            /// <returns>This</returns>
            /// <exception cref="Exception">Exception thrown if color is outside of range</exception>
            public Embed AddColor(string color)
            {
                int parsedColor = int.Parse(color.TrimStart('#'), NumberStyles.AllowHexSpecifier);
                if (parsedColor < 0x0 || parsedColor > 0xFFFFFF)
                {
                    throw new Exception($"Color '{color}' is outside the valid color range");
                }

                Color = parsedColor;
                return this;
            }

            /// <summary>
            /// Adds a RGB based color. Color appears as a bar on the left side of the message
            /// </summary>
            /// <param name="red">Red value between 0 - 255</param>
            /// <param name="green">Green value between 0 - 255</param>
            /// <param name="blue">Blue value between 0 - 255</param>
            /// <returns>This</returns>
            /// <exception cref="Exception">Thrown if red, green, or blue is outside of range</exception>
            public Embed AddColor(int red, int green, int blue)
            {
                if (red < 0 || red > 255 || green < 0 || green > 255 || blue < 0 || blue > 255)
                {
                    throw new Exception($"Color Red:{red} Green:{green} Blue:{blue} is outside the valid color range. Must be between 0 - 255");
                }

                Color = red * 65536 + green * 256 + blue;;
                return this;
            }

            /// <summary>
            /// Adds a blank field.
            /// If inline it will add a blank column.
            /// If not inline will add a blank row
            /// </summary>
            /// <param name="inline">If the field is inline</param>
            /// <returns>This</returns>
            public Embed AddBlankField(bool inline)
            {
                Fields.Add(new Field("\u200b", "\u200b", inline));
                return this;
            }

            /// <summary>
            /// Adds a new field with the name as the title and value as the value.
            /// If inline will add a new column. If row will add in a new row.
            /// </summary>
            /// <param name="name"></param>
            /// <param name="value"></param>
            /// <param name="inline"></param>
            /// <returns></returns>
            public Embed AddField(string name, string value, bool inline)
            {
                Fields.Add(new Field(name, value, inline));
                return this;
            }

            /// <summary>
            /// Adds an image to the embed. The url should point to the url of the image.
            /// If using attachment image you can make the url: "attachment://{image name}.{image extension}
            /// </summary>
            /// <param name="url">Url for the image</param>
            /// <param name="width">width of the image</param>
            /// <param name="height">height of the image</param>
            /// <param name="proxyUrl">Backup url for the image</param>
            /// <returns></returns>
            public Embed AddImage(string url, int? width = null, int? height = null, string proxyUrl = null)
            {
                Image = new Image(url, width, height, proxyUrl);
                return this;
            }

            /// <summary>
            /// Adds a thumbnail in the top right corner of the embed
            /// If using attachment image you can make the url: "attachment://{image name}.{image extension}
            /// </summary>
            /// <param name="url">Url for the image</param>
            /// <param name="width">width of the image</param>
            /// <param name="height">height of the image</param>
            /// <param name="proxyUrl">Backup url for the image</param>
            /// <returns></returns>
            public Embed AddThumbnail(string url, int? width = null, int? height = null, string proxyUrl = null)
            {
                Thumbnail = new Image(url, width, height, proxyUrl);
                return this;
            }

            /// <summary>
            /// Adds a video to the embed
            /// </summary>
            /// <param name="url">Url for the video</param>
            /// <param name="width">Width of the video</param>
            /// <param name="height">Height of the video</param>
            /// <returns></returns>
            public Embed AddVideo(string url, int? width = null, int? height = null)
            {
                Video = new Video(url, width, height);
                return this;
            }
        }

        /// <summary>
        /// Field for and embed message
        /// </summary>
        private class Field
        {
            /// <summary>
            /// Name of the field
            /// </summary>
            [JsonProperty("name")]
            private string Name { get; }

            /// <summary>
            /// Value for the field
            /// </summary>
            [JsonProperty("value")]
            private string Value { get; }

            /// <summary>
            /// If the field should be in the same row or a new row
            /// </summary>
            [JsonProperty("inline")]
            private bool Inline { get; }

            public Field(string name, string value, bool inline)
            {
                Name = name;
                Value = value;
                Inline = inline;
            }
        }

        /// <summary>
        /// Image for an embed message
        /// </summary>
        private class Image
        {
            /// <summary>
            /// Url for the image
            /// </summary>
            [JsonProperty("url")]
            private string Url { get; }

            /// <summary>
            /// Width for the image
            /// </summary>
            [JsonProperty("width")]
            private int? Width { get; }

            /// <summary>
            /// Height for the image
            /// </summary>
            [JsonProperty("height")]
            private int? Height { get; }

            /// <summary>
            /// Proxy url for the image
            /// </summary>
            [JsonProperty("proxyURL")]
            private string ProxyUrl { get; }

            public Image(string url, int? width, int? height, string proxyUrl)
            {
                Url = url;
                Width = width;
                Height = height;
                ProxyUrl = proxyUrl;
            }
        }

        /// <summary>
        /// Video for an embed message
        /// </summary>
        private class Video
        {
            /// <summary>
            /// Url to the video
            /// </summary>
            [JsonProperty("url")]
            private string Url { get; }

            /// <summary>
            /// Width of the video
            /// </summary>
            [JsonProperty("width")]
            private int? Width { get; }

            /// <summary>
            /// Height of the video
            /// </summary>
            [JsonProperty("height")]
            private int? Height { get; }

            public Video(string url, int? width, int? height)
            {
                Url = url;
                Width = width;
                Height = height;
            }
        }

        /// <summary>
        /// Author of an embed message
        /// </summary>
        private class AuthorInfo
        {
            /// <summary>
            /// Name of the author
            /// </summary>
            [JsonProperty("name")]
            private string Name { get; }

            /// <summary>
            /// Url to go to when clicking on the authors name
            /// </summary>
            [JsonProperty("url")]
            private string Url { get; }

            /// <summary>
            /// Icon url for the author
            /// </summary>
            [JsonProperty("icon_url")]
            private string IconUrl { get; }

            /// <summary>
            /// Proxy icon url for the author
            /// </summary>
            [JsonProperty("proxy_icon_url")]
            private string ProxyIconUrl { get; }

            public AuthorInfo(string name, string iconUrl, string url, string proxyIconUrl)
            {
                Name = name;
                Url = url;
                IconUrl = iconUrl;
                ProxyIconUrl = proxyIconUrl;
            }
        }

        /// <summary>
        /// Footer for an embed message
        /// </summary>
        private class Footer
        {
            /// <summary>
            /// Text for the footer
            /// </summary>
            [JsonProperty("text")]
            private string Text { get; }

            /// <summary>
            /// Icon url for the footer
            /// </summary>
            [JsonProperty("icon_url")]
            private string IconUrl { get; }

            /// <summary>
            /// Proxy icon url for the footer
            /// </summary>
            [JsonProperty("proxy_icon_url")]
            private string ProxyIconUrl { get; }

            public Footer(string text, string iconUrl, string proxyIconUrl)
            {
                Text = text;
                IconUrl = iconUrl;
                ProxyIconUrl = proxyIconUrl;
            }
        }

        #endregion

        #region Attachment Classes
        /// <summary>
        /// Enum for attachment content type
        /// </summary>
        private enum AttachmentContentType
        {
            Png,
            Jpg
        }

        private class Attachment
        {
            /// <summary>
            /// Attachment data
            /// </summary>
            public byte[] Data { get; }
            
            /// <summary>
            /// File name for the attachment.
            /// Used in the url field of an image
            /// </summary>
            public string Filename { get; }
            
            /// <summary>
            /// Content type for the attachment
            /// https://developer.mozilla.org/en-US/docs/Web/HTTP/Basics_of_HTTP/MIME_types
            /// </summary>
            public string ContentType { get; }

            public Attachment(byte[] data, string filename, AttachmentContentType contentType)
            {
                Data = data;
                Filename = filename;

                switch (contentType)
                {
                    case AttachmentContentType.Jpg:
                        ContentType = "image/jpeg";
                        break;
                    
                    case AttachmentContentType.Png:
                        ContentType = "image/png";
                        break;
                }
            }

            public Attachment(byte[] data, string filename, string contentType)
            {
                Data = data;
                Filename = filename;
                ContentType = contentType;
            }
        }
        
        #endregion

        #region Config Classes

        private class DiscordMessageConfig
        {
            public string Content { get; set; }
            public EmbedConfig Embed { get; set; }
        }
        
        private class EmbedConfig
        {
            [JsonProperty("Enabled")]
            public bool Enabled { get; set; }
            
            [JsonProperty("Title")]
            public string Title { get; set; }
            
            [JsonProperty("Description")]
            public string Description { get; set; }
            
            [JsonProperty("Url")]
            public string Url { get; set; }
            
            [JsonProperty("Embed Color")]
            public string Color { get; set; }
            
            [JsonProperty("Image Url")]
            public string Image { get; set; }
            
            [JsonProperty("Thumbnail Url")]
            public string Thumbnail { get; set; }
            
            [JsonProperty("Fields")]
            public List<FieldConfig> Fields { get; set; }
            
            [JsonProperty("Footer")]
            public FooterConfig Footer { get; set; }
        }
        
        private class FieldConfig
        {
            [JsonProperty("Title")]
            public string Title { get; set; }
            
            [JsonProperty("Value")]
            public string Value { get; set; }
            
            [JsonProperty("Inline")]
            public bool Inline { get; set; }

            [JsonProperty("Enabled")]
            public bool Enabled { get; set; }
        }

        private class FooterConfig
        {
            [JsonProperty("Icon Url")]
            public string IconUrl { get; set; }
            
            [JsonProperty("Text")]
            public string Text { get; set; }
            
            [JsonProperty("Enabled")]
            public bool Enabled { get; set; }
        }
        #endregion
        
        #region Config Methods
        private DiscordMessage ParseMessage(DiscordMessageConfig config)
        {
            DiscordMessage message = new DiscordMessage();

            if (!string.IsNullOrEmpty(config.Content))
            {
                message.AddContent(config.Content);
            }

            if (config.Embed != null && config.Embed.Enabled)
            {
                Embed embed = new Embed();
                if (!string.IsNullOrEmpty(config.Embed.Title))
                {
                    embed.AddTitle(config.Embed.Title);
                }

                if (!string.IsNullOrEmpty(config.Embed.Description))
                {
                    embed.AddDescription(config.Embed.Description);
                }
                
                if (!string.IsNullOrEmpty(config.Embed.Url))
                {
                    embed.AddUrl(config.Embed.Url);
                }

                if (!string.IsNullOrEmpty(config.Embed.Color))
                {
                    embed.AddColor(config.Embed.Color);
                }

                if (!string.IsNullOrEmpty(config.Embed.Image))
                {
                    embed.AddImage(config.Embed.Image);
                }

                if (!string.IsNullOrEmpty(config.Embed.Thumbnail))
                {
                    embed.AddThumbnail(config.Embed.Thumbnail);
                }

                foreach (FieldConfig field in config.Embed.Fields.Where(f => f.Enabled))
                {
                    string value = field.Value;
                    if (string.IsNullOrEmpty(value))
                    {
                        PrintWarning($"Field: {field.Title} was skipped because the value was null or empty.");
                        continue;
                    }
                    
                    embed.AddField(field.Title, value + "\u200b", field.Inline);
                }

                if (config.Embed.Footer != null && config.Embed.Footer.Enabled)
                {
                    if (string.IsNullOrEmpty(config.Embed.Footer.Text) &&
                        string.IsNullOrEmpty(config.Embed.Footer.IconUrl))
                    {
                        AddPluginInfoFooter(embed);
                    }
                    else
                    {
                        embed.AddFooter(config.Embed.Footer.Text, config.Embed.Footer.IconUrl);
                    }
                }
                
                message.AddEmbed(embed);
            }
            
            return message;
        }
        #endregion
        #endregion

        #region Discord Gateway
        private WebSocket _socket;
        private string _socketUrl;
        
        private void ConnectToGateway()
        {
            if (_socket != null)
            {
                PrintWarning("Socket already active. Skipping connection");
                return;
            }
            
            webrequest.Enqueue(GatewayUrl, null, HandleGatewayResponse, this);
        }

        private void HandleGatewayResponse(int code, string response)
        {
            if (code != 200)
            {
                PrintError($"An error occured trying to get the discord gateway: Code: {code} Response: {response}");
                return;
            }

            GatewayApiResponse gatewayApi = JsonConvert.DeserializeObject<GatewayApiResponse>(response);
            _socketUrl = gatewayApi.Url;
            ConnectWebSocket();
        }

        private void ConnectWebSocket()
        {
            _socket = new WebSocket($"{_socketUrl}/?v=6&encoding=json");
            _socket.OnMessage += SocketOnOnMessage;
            _socket.OnError += SocketOnOnError;
            _socket.ConnectAsync();
        }

        private void DisconnectWebsocket()
        {
            _socket?.CloseAsync();
            _socket = null;
        }
        
        private void SocketOnOnError(object sender, ErrorEventArgs e)
        {
            PrintError($"A socket error occured while trying to setup the discord bot. Trying again in 15 seconds\n{e.Message}\n{e.Exception}");
            DisconnectWebsocket();
            timer.In(15f, ConnectWebSocket);
        }

        private void SocketOnOnMessage(object sender, MessageEventArgs e)
        {
            ResponsePayload payload = JsonConvert.DeserializeObject<ResponsePayload>(e.Data);
            switch (payload.OpCode)
            {
                case OpCodes.Dispatch:
                
                    switch (payload.EventName)
                    {
                        case "READY":
                            _storedData.PreviousBotKey = _pluginConfig.DiscordBotKey;
                            SetupMessaging();
                            SaveData();
                            PrintWarning("Bot successfully identified with gateway.");
                            timer.In(1f, DisconnectWebsocket);
                            break;
                    }

                    break;
                    
                case OpCodes.Hello:
                    RequestPayload request = new RequestPayload
                    {
                        OP = OpCodes.Identify,
                        Payload =  new Identify
                        {
                            Token = _pluginConfig.DiscordBotKey,
                            Properties = new Properties
                            {
                                OS = nameof(DiscordServerStats),
                                Browser = nameof(DiscordServerStats),
                                Device = nameof(DiscordServerStats),
                            },
                            Compress = false,
                            Shard = new List<int> {0, 1},
                            LargeThreshold = 50
                        }
                    };
                    
                    _socket.Send(JsonConvert.SerializeObject(request));
                    break;
            }
        }

        private class GatewayApiResponse
        {
            [JsonProperty(PropertyName = "url")]
            public string Url { get; set; }
        }
        
        private class ResponsePayload
        {
            [JsonProperty("op")]
            public OpCodes OpCode { get; set; }
        
            [JsonProperty("t")]
            public string EventName { get; set; }

            [JsonProperty("d")]
            public object Data { get; set; }

            [JsonProperty("s")]
            public int? Sequence { get; set; }

            public JObject EventData => Data as JObject;
        }
        
        private class RequestPayload
        {
            [JsonProperty("op")]
            public OpCodes OP;

            [JsonProperty("d")]
            public object Payload;
        }

        private class Identify
        {
            [JsonProperty("token")]
            public string Token;

            [JsonProperty("properties")]
            public Properties Properties;

            [JsonProperty("compress")]
            public bool Compress;

            [JsonProperty("large_threshold")]
            public int LargeThreshold;

            [JsonProperty("shard")]
            public List<int> Shard;
        }

        private class Properties
        {
            [JsonProperty("$os")]
            public string OS;

            [JsonProperty("$browser")]
            public string Browser;

            [JsonProperty("$device")]
            public string Device;
        }

        private enum OpCodes
        {
            Dispatch = 0,
            Heartbeat = 1,
            Identify = 2,
            StatusUpdate = 3,
            VoiceStateUpdate = 4,
            VoiceServerPing = 5,
            Resume = 6,
            Reconnect = 7,
            RequestGuildMembers = 8,
            InvalidSession = 9,
            Hello = 10,
            HeartbeatACK = 11
        }
        #endregion
    }
}
