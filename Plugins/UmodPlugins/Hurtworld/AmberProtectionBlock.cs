using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Amber Protection Block", "Mr. Blue", "0.0.2")]
    [Description("Block amber protection of certain items.")]

    class AmberProtectionBlock : HurtworldPlugin
    {
        private List<string> blocked = new List<string>();

        void Init()
        {
            blocked = Config.Get<List<string>>("BlockedGuids");
        }

        protected override void LoadDefaultConfig()
        {
            if (Config["BlockedGuids"] == null) Config.Set("BlockedGuids", new List<string>(new string[] { "c564c6a30cc064fcd87467bdbe47513a" }));
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(
                new Dictionary<string, string> {
                    { "ItemProtectionBlocked", "<color=orange>Not Protectable!</color>" }
                }, this);
        }
        string Msg(string msg, string SteamId = null) => lang.GetMessage(msg, this, SteamId);

        object OnAmberProtect(ItemComponentBase itemComponent, ItemObject item)
        {
            string guid = RuntimeHurtDB.Instance.GetGuid(item.Generator);
            if (guid == null) return null;

            if (blocked.Contains(guid))
            {
                if (item?.GetContainer()?.GetComponent<BaseUsableDevice>()?.GetCurrentlyInteracting() != null)
                {
                    HashSet<WorldItemInteractServer> users = item.GetContainer().GetComponent<BaseUsableDevice>().GetCurrentlyInteracting();
                    foreach (WorldItemInteractServer user in users)
                    {
                        PlayerSession session = user.OwnerIdentity.ConnectedSession;
                        AlertManager.Instance.GenericTextNotificationServer(Msg("ItemProtectionBlocked", session.SteamId.ToString()), session.Player);
                    }
                }
                else if (item?.GetContainer()?.networkView?.Owner != null)
                {
                    PlayerSession session = Singleton<GameManager>.Instance.GetSession(item.GetContainer().networkView.Owner);
                    if (session != null)
                        AlertManager.Instance.GenericTextNotificationServer(Msg("ItemProtectionBlocked", session.SteamId.ToString()), session.Player);
                }
                return true;
            }
            return null;
        }
    }
}