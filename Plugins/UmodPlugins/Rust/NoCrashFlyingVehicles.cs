using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("No Crash Flying Vehicles", "MON@H", "0.2.1")]
    [Description("Prevents flying vehicles from crashing.")]
    public class NoCrashFlyingVehicles : CovalencePlugin
    {
        #region Class Fields

        private const string PermissionUse = "nocrashflyingvehicles.use";
        private bool _enabled;

        #endregion Class Fields

        #region Initialization

        private void Init()
        {
            permission.RegisterPermission(PermissionUse, this);
            foreach (var command in _configData.GlobalSettings.Commands)
            {
                AddCovalenceCommand(command, nameof(CmdNoCrash));
            }                
        }

        private void OnServerInitialized()
        {
            if (_configData.GlobalSettings.Commands.Length == 0)
            {
                _configData.GlobalSettings.Commands = new[] { "nocrash" };
                SaveConfig();
            }
            _enabled = _configData.GlobalSettings.DefaultEnabled;

            if (_enabled)
            {
                Subscribe(nameof(OnEntityTakeDamage));
            }
            else
            {
                Unsubscribe(nameof(OnEntityTakeDamage));
            }
        }

        #endregion Initialization

        #region Configuration

        private ConfigData _configData;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Global settings")]
            public GlobalSettings GlobalSettings = new GlobalSettings();

            [JsonProperty(PropertyName = "Chat settings")]
            public ChatSettings ChatSettings = new ChatSettings();

            [JsonProperty(PropertyName = "No Crash settings")]
            public NoCrashSettings NoCrashSettings = new NoCrashSettings();
        }

        private class GlobalSettings
        {
            [JsonProperty(PropertyName = "Use permissions")]
            public bool UsePermission = true;

            [JsonProperty(PropertyName = "Allow admins to use without permission")]
            public bool AdminsAllowed = true;

            [JsonProperty(PropertyName = "Enabled on start?")]
            public bool DefaultEnabled = true;

            [JsonProperty(PropertyName = "Commands list")]
            public string[] Commands = new[] { "nocrash", "ncfv" };
        }

        private class ChatSettings
        {
            [JsonProperty(PropertyName = "Chat steamID icon")]
            public ulong SteamIDIcon = 0;

            [JsonProperty(PropertyName = "Notify admins only")]
            public bool NotifyAdminsOnly = true;
        }

        private class NoCrashSettings
        {
            [JsonProperty(PropertyName = "MiniCopter enabled?")]
            public bool EnabledMiniCopter = true;

            [JsonProperty(PropertyName = "ScrapTransportHelicopter enabled?")]
            public bool EnabledScrapCopter = true;

            [JsonProperty(PropertyName = "CH47Helicopter enabled?")]
            public bool EnabledChinook = true;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _configData = Config.ReadObject<ConfigData>();
                if (_configData == null)
                {
                    LoadDefaultConfig();
                    SaveConfig();
                }
            }
            catch
            {
                PrintError("The configuration file is corrupted");
                LoadDefaultConfig();
                SaveConfig();
            }
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file");
            _configData = new ConfigData();
        }

        protected override void SaveConfig() => Config.WriteObject(_configData);

        #endregion Configuration

        #region Localization

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Disabled"] = "<color=#B22222>Disabled</color>",
                ["Enabled"] = "<color=#228B22>Enabled</color>",
                ["NotAllowed"] = "You do not have permission to use this command",
                ["Prefix"] = "<color=#00FFFF>[No Crash Flying Vehicles]</color>: ",
                ["State"] = "<color=#FFA500>{0}</color> {1} No Crash Flying Vehicles",
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Disabled"] = "<color=#B22222>Отключил</color>",
                ["Enabled"] = "<color=#228B22>Включил</color>",
                ["NotAllowed"] = "У вас нет разрешения на использование этой команды",
                ["Prefix"] = "<color=#00FFFF>[Летающий транспорт без аварий]</color>: ",
                ["State"] = "<color=#FFA500>{0}</color> {1} летающий транспорт без аварий",
            }, this, "ru");
        }

        #endregion Localization

        #region Commands

        private void CmdNoCrash(IPlayer player, string command, string[] args)
        {
            if (_configData.GlobalSettings.UsePermission && !permission.UserHasPermission(player.Id, PermissionUse))
            {
                if (!_configData.GlobalSettings.AdminsAllowed || !player.IsAdmin)
                {
                    Print(player, Lang("NotAllowed", player.Id));
                    return;
                }
            }

            _enabled = !_enabled;

            if (_enabled)
            {
                Subscribe(nameof(OnEntityTakeDamage));
            }
            else
            {
                Unsubscribe(nameof(OnEntityTakeDamage));
            }

            foreach (var p in BasePlayer.activePlayerList)
            {
                if (p.IsAlive())
                {
                    if (_configData.ChatSettings.NotifyAdminsOnly && !p.IsAdmin)
                    {
                        continue;
                    }
                    Print(player, Lang("State", player.Id, player.Name, _enabled ? Lang("Enabled", player.Id) : Lang("Disabled", player.Id)));       
                }
            }
        }

        #endregion Commands

        #region OxideHooks

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (!_enabled || info == null || entity == null || info.damageTypes.Has(Rust.DamageType.Decay))
            {
                return null;
            }

            var attacker = info.Initiator;
            if (attacker == null)
            {
                return null;
            }
            if (_configData.NoCrashSettings.EnabledMiniCopter && attacker.ShortPrefabName == "minicopter.entity")
            {
                return true;
            }
            if (_configData.NoCrashSettings.EnabledScrapCopter && attacker.ShortPrefabName == "scraptransporthelicopter")
            {
                return true;
            }
            if (_configData.NoCrashSettings.EnabledChinook && attacker.ShortPrefabName == "ch47.entity")
            {
                return true;
            }

            return null;
        }

        #endregion OxideHooks

        #region Helpers

        private void Print(IPlayer player, string message)
        {
            string text;
            if (string.IsNullOrEmpty(Lang("Prefix", player.Id)))
            {
                text = message;
            }
            else
            {
                text = Lang("Prefix", player.Id) + message;
            }
#if RUST
            (player.Object as BasePlayer).SendConsoleCommand ("chat.add", 2, _configData.ChatSettings.SteamIDIcon, text);
            return;
#endif
            player.Message(text);
        }

        #endregion Helpers
    }
}