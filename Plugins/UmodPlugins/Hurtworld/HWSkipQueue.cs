namespace Oxide.Plugins
{
    [Info("HW Skip Queue", "klauz24", "1.2.0"), Description("Allows players with permission to jump the queue")]
    internal class HWSkipQueue : HurtworldPlugin
    {
        private const string _perm = "hwskipqueue.use";

        private void Init() => permission.RegisterPermission(_perm, this);

        private object CanJoinQueue(PlayerSession session)
        {
            if (session.IsAdmin || permission.UserHasPermission(GetSteamId(session), _perm)) return false;
            return true;
        }

        private string GetSteamId(PlayerSession session) => session.SteamId.ToString();
    }
}