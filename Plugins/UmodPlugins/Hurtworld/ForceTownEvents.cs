using System.IO;
using Oxide.Core;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Force Town Events", "klauz24", "1.0.0"), Description("Allows you to force town events")]
    internal class ForceTownEvents : HurtworldPlugin
    {
        private const string _perm = "forcetownevents.use";

        private void Init() => permission.RegisterPermission(_perm, this);

        private void OnServerInitialized() => timer.Every(_config.BoostInterval, () => ForceEvent(_config.EventsAmount));

        private Configuration _config;

        private class Configuration
        {
            [JsonProperty(PropertyName = "Interval")]
            public float BoostInterval { get; set; } = 1800f;

            [JsonProperty(PropertyName = "Events amount")]
            public int EventsAmount { get; set; } = 1;

            [JsonProperty(PropertyName = "Min players to start")]
            public int MinPlayers { get; set; } = 10;
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
                {"FTE - Prefix", "<color=blue>[Force Town Events]</color>"},
                {"FTE - No perm", "You got no permission to use this command."},
                {"FTE - Starting", "Town events boost, starting {0} events!"}
            }, this);
        }

        [ChatCommand("fte")]
        private void ForceTownControlManual(PlayerSession session)
        {
            if (!permission.UserHasPermission(session.SteamId.ToString(), _perm))
            {
                Msg(session, Lang(session, "FTE - Prefix"), Lang(session, "FTE - No perm"));
                return;
            }
            ForceEvent(_config.EventsAmount);
        }

        private bool MinPlayers()
        {
            int playerCount = GameManager.Instance.GetPlayerCount();
            if (playerCount >= _config.MinPlayers)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void ForceEvent(int times)
        {
            if (MinPlayers())
            {
                for (int i = 0; i < times; i++)
                {
                    TownEventDirector.Instance.CycleEvent();
                }
                foreach (PlayerSession session in GameManager.Instance.GetSessions().Values)
                {
                    Msg(session, Lang(session, "FTE - Prefix"), string.Format(Lang(session, "FTE - Starting"), _config.EventsAmount));
                }
            }
            else
            {
                Puts("Not enough players to start.");
            }
        }

        private void Msg(PlayerSession session, string prefix, string message) => hurt.SendChatMessage(session, prefix, message);

        private string Lang(PlayerSession session, string key) => lang.GetMessage(key, this, session.SteamId.ToString());
    }
}

