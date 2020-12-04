using System.IO;
using Oxide.Core;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Crafting Block", "klauz24", "1.0.0"), Description("Does not allow to craft certain items.")]
    internal class CraftingBlock : HurtworldPlugin
    {
        private const string _perm = "craftingblock.bypass";

        private void Init() => permission.RegisterPermission(_perm, this);

        private Configuration _config;

        private class Configuration
        {
            [JsonProperty(PropertyName = "Blocked items (GUID)")]
            public List<string> BlockedItems { get; set; } = new List<string>()
            {
                "Some GUID here"
            };
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
                {"CB - Prefix", "<color=red>[Crafting Block]</color>"},
                {"CB - Can not craft", "{0} can not be crafted."}
            }, this);
        }

        private object CanCraft(Crafter crafter, PlayerSession session, ICraftable recipe, int count)
        {
            if (permission.UserHasPermission(GetSessionId(session), _perm)) return null;
            if (_config.BlockedItems.Contains(GetGuid(recipe.GenerateItem())))
            {
                Msg(session, Lang(session, "CB - Prefix"), string.Format(Lang(session, "CB - Can not craft"), recipe.GenerateItem().Generator.name));
                return false;
            }
            return null;
        }

        private string GetGuid(ItemObject obj)
        {
            return RuntimeHurtDB.Instance.GetGuid(obj.Generator);
        }

        private string GetSessionId(PlayerSession session)
        {
            return session.SteamId.ToString();
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