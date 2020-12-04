using System.IO;
using Oxide.Core;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("HW Friendly Fire", "klauz24", "1.0.2"), Description("Disables damage between clan members.")]
    internal class HWFriendlyFire : HurtworldPlugin
    {
        private List<string> _clansDisabled = new List<string>();

        private const string _perm = "hwfriendlyfire.use";

        private void Init()
        {
            _clansDisabled = Interface.Oxide.DataFileSystem.ReadObject<List<string>>("HWFriendlyFire_Data");
            permission.RegisterPermission(_perm, this);
        }
/*
        private void Loaded()
        {
            try
            {
                _clansDisabled = Interface.Oxide.DataFileSystem.ReadObject<List<string>>("HWFriendlyFire_Data");
            }
            catch
            {
                _clansDisabled = new List<string>();
            }
        }
*/
        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("HWFriendlyFire_Data", _clansDisabled);
        }

        private Configuration _config;

        private class Configuration
        {
            [JsonProperty(PropertyName = "Enable alert")]
            public bool EnableAlert { get; set; } = true;

            [JsonProperty(PropertyName = "Only clan owner can toggle friendly fire")]
            public bool OnlyClanOwner { get; set; } = true;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null)
                {
                    throw new JsonException();
                }
            }
            catch
            {
                string configPath = $"{Interface.Oxide.ConfigDirectory}{Path.DirectorySeparatorChar}{Name}";
                PrintWarning($"Could not load a valid configuration file, creating a new configuration file at {configPath}.json");
                Config.WriteObject(_config, false, $"{configPath}_invalid.json");
                LoadDefaultConfig();
                SaveConfig();
            }
        }

        protected override void LoadDefaultConfig() => _config = new Configuration();

        protected override void SaveConfig() => Config.WriteObject(_config);

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"FF - Prefix", "<color=orange>[HW Friendly Fire]</color>"},
                {"FF - No perm", "You got no permission to use this command."},
                {"FF - No perm owner", "Only clan owner can toggle friendly fire."},
                {"FF - Info", "Clan tag: {0}\nStatus: {1}\nType '/ff on|off' to toogle friendly fire."},
                {"FF - Enabled", "<color=red>Friendly fire is enabled.</color>"},
                {"FF - Disabled", "<color=lime>Friendly fire is disabled.</color>"},
                {"FF - Already enabled", "Friendly fire is already enabled."},
                {"FF - Already disabled", "Friendly fire is already disabled."},
                {"FF - Syntax", "Syntax: /ff on|off"},
                {"FF - Alert", "Do not shoot your clan mates!"},
                {"FF - No clan", "You got no clan."},
                {"FF - Clan info", "Clan information:"}
            }, this);
        }

        private object OnPlayerTakeDamage(PlayerSession session, EntityEffectSourceData source)
        {
            if (session == null || source == null || source.EntitySource == null) return null;
            PlayerSession attackerSession = GetAttackerSession(source);
            if (attackerSession == null) return null;
            Clan clan = session.Identity.Clan;
            Clan clan2 = attackerSession.Identity.Clan;
            if (clan != null && clan2 != null && clan == clan2 && _clansDisabled.Contains(clan.ClanGuid))
            {
                if (_config.EnableAlert) Notification(attackerSession, Lang(attackerSession, "FF - Alert"));
                return 0f;
            }
            return null;
        }

        [ChatCommand("ff")]
        private void FriendlyFireCommand(PlayerSession session, string command, string[] args)
        {
            if (!permission.UserHasPermission(GetSessionId(session), _perm))
            {
                Msg(session, Lang(session, "FF - Prefix"), Lang(session, "FF - No perm"));
                return;
            }
            if (session.Identity.Clan == null)
            {
                Msg(session, Lang(session, "FF - Prefix"), Lang(session, "FF - No clan"));
                return;
            }
            if (_config.OnlyClanOwner && session.Identity.Clan.GetOwner() != session.SteamId.m_SteamID)
            {
                Msg(session, Lang(session, "FF - Prefix"), Lang(session, "FF - No perm owner"));
                return;
            }
            if (args.Length == 0)
            {
                Msg(session, Lang(session, "FF - Prefix"), Lang(session, "FF - Clan info"));
                Msg(session, null, string.Format(Lang(session, "FF - Info"), session.Identity.Clan.ClanTag, IsEnabled(session, session.Identity.Clan.ClanGuid)));
            }
            else
            {
                switch (args[0].ToLower())
                {
                    case "on":
                        if (_clansDisabled.Contains(GetClanGuid(session)))
                        {
                            _clansDisabled.Remove(GetClanGuid(session));
                            SaveData();
                            Msg(session, Lang(session, "FF - Prefix"), Lang(session, "FF - Enabled"));
                        }
                        else
                        {
                            Msg(session, Lang(session, "FF - Prefix"), Lang(session, "FF - Already enabled"));
                        }
                        break;
                    case "off":
                        if (!_clansDisabled.Contains(GetClanGuid(session)))
                        {
                            _clansDisabled.Add(GetClanGuid(session));
                            SaveData();
                            Msg(session, Lang(session, "FF - Prefix"), Lang(session, "FF - Disabled"));
                        }
                        else
                        {
                            Msg(session, Lang(session, "FF - Prefix"), Lang(session, "FF - Already disabled"));
                        }
                        break;
                    default:
                        Msg(session, Lang(session, "FF - Prefix"), Lang(session, "FF - Syntax"));
                        break;
                }
            }
        }

        private string IsEnabled(PlayerSession session, string guid)
        {
            if (_clansDisabled.Contains(guid))
            {
                return Lang(session, "FF - Disabled");
            }
            else
            {
                return Lang(session, "FF - Enabled");
            }
        }

        private string GetClanGuid(PlayerSession session)
        {
            return session.Identity.Clan.ClanGuid;
        }

        private string GetSessionId(PlayerSession session)
        {
            return session.SteamId.ToString();
        }

        private void Notification(PlayerSession session, string str)
        {
            AlertManager.Instance.GenericTextNotificationServer(str, session.Player);
        }

        // Credits Wulf
        private PlayerSession GetAttackerSession(EntityEffectSourceData source)
        {
            EntityStats entityStats = source.EntitySource.GetComponent<EntityStats>();
            HNetworkView networkView = entityStats.GetComponent<HNetworkView>();
            PlayerSession session = GameManager.Instance.GetSession(networkView.owner);
            return session;
        }

        private string Lang(PlayerSession session, string key)
        {
            return lang.GetMessage(key, this, GetSessionId(session));
        }

        private void Msg(PlayerSession session, string prefix, string message)
        {
            hurt.SendChatMessage(session, prefix, message);
        }
    }
}