using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("HWNoClip", "klauz24", 1.0), Description("Walk through the objects and walls like a hacker")]
    internal class HWNoClip : HurtworldPlugin
    {
        private const string _perm = "hwnoclip.use";

        private void Init() => permission.RegisterPermission(_perm, this);

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"HWNoClip - Prefix", "<color=green>[HW NoClip]</color>"},
                {"HWNoClip - No Perm", "You do not have permission to use this command."},
                {"HWNoClip - Enabled", "NoClip enabled!"},
                {"HWNoClip - Disabled", "NoClip disabled!"}
            }, this);
        }

        [ChatCommand("noclip")]
        private void HWNoClipCommand(PlayerSession session, string command, string[] args)
        {
            if (permission.UserHasPermission(session.SteamId.ToString(), _perm) || session.IsAdmin)
            {
                if (session.WorldPlayerEntity.gameObject.layer != 12)
                {
                    session.WorldPlayerEntity.gameObject.layer = 12;
                    hurt.SendChatMessage(session, lang.GetMessage("HWNoClip - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWNoClip - Enabled", this, session.SteamId.ToString()));
                }
                else
                {
                    session.WorldPlayerEntity.gameObject.layer = 17;
                    hurt.SendChatMessage(session, lang.GetMessage("HWNoClip - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWNoClip - Disabled", this, session.SteamId.ToString()));
                }
            }
            else
            {
                hurt.SendChatMessage(session, lang.GetMessage("HWNoClip - Prefix", this, session.SteamId.ToString()), lang.GetMessage("HWNoClip - No Perm", this, session.SteamId.ToString()));
            }
        }
    }
}