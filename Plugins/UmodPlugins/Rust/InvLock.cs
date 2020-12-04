using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Inv Lock", "Dana", "1.2.4")]
    [Description("Locks the inventory and hotbar of players")]
    class InvLock : RustPlugin
    {
        private const string belt = "invlock.belt";
        private const string wear = "invlock.wear";
        private const string main = "invlock.main";
        private const string update = "invlock.update";

        #region Oxide Hooks

        private void Init()
        {
            permission.RegisterPermission(belt, this);
            permission.RegisterPermission(wear, this);
            permission.RegisterPermission(main, this);
            permission.RegisterPermission(update, this);
        }


        private void Unload()
        {
            foreach (var player in BasePlayer.allPlayerList)
            {
                ToggleInvLock(player, "belt", false);
                ToggleInvLock(player, "main", false);
                ToggleInvLock(player, "wear", false);
            }
        }

        #endregion

        #region Chat
        [ChatCommand("invlock")]
        private void lockCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, update))
            {
                SendReply(player, Lang("NoPermission", player.UserIDString));
                return;
            }

            if (args.Length == 0)
            {
                SendReply(player, Lang("InvalidArgument", player.UserIDString));
                return;
            }
            
            var type = args[0].ToLower();
            if (type != "all" && type != "belt" && type != "main" && type != "wear")
            {
                SendReply(player, Lang("InvalidArgument", player.UserIDString));
                return;
            }

            var count = 0;
            if (type == "all")
            {
                foreach (var basePlayer in BasePlayer.allPlayerList)
                {
                    var result = ToggleInvLock(basePlayer, "belt", true);
                    result = ToggleInvLock(basePlayer, "main", true) || result;
                    result = ToggleInvLock(basePlayer, "wear", true) || result;
                    if (result)
                        count++;
                }
                SendReply(player, Lang("All Locked", player.UserIDString), count);
            }
            else
            {
                foreach (var basePlayer in BasePlayer.allPlayerList)
                {
                    var result = ToggleInvLock(basePlayer, type, true);
                    if (result)
                        count++;
                }
                SendReply(player, Lang("Type Locked", player.UserIDString, type, count));
            }
        }

        [ChatCommand("invunlock")]
        private void unlockCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, update))
            {
                SendReply(player, Lang("NoPermission", player.UserIDString));
                return;
            }

            if (args.Length == 0)
            {
                SendReply(player, Lang("InvalidArgument", player.UserIDString));
                return;
            }

            var type = args[0].ToLower();
            if (type != "all" && type != "belt" && type != "main" && type != "wear")
            {
                SendReply(player, Lang("InvalidArgument", player.UserIDString));
                return;
            }

            if (type == "all")
            {
                foreach (var basePlayer in BasePlayer.allPlayerList)
                {
                    ToggleInvLock(basePlayer, "belt", false);
                    ToggleInvLock(basePlayer, "main", false);
                    ToggleInvLock(basePlayer, "wear", false);
                }
                SendReply(player, Lang("All UnLocked", player.UserIDString));
            }
            else
            {
                foreach (var basePlayer in BasePlayer.allPlayerList)
                {
                    ToggleInvLock(basePlayer, type, false);
                }
                SendReply(player, Lang("Type UnLocked", player.UserIDString, type));
            }
        }
        #endregion

        #region Toggling
        private bool ToggleInvLock(BasePlayer player, string container, bool status)
        {
            var locking = status && permission.UserHasPermission(player.UserIDString, $"invlock.{container}");
            ItemContainer inventory = null;
            switch (container.ToLower())
            {
                case "belt":
                    inventory = player.inventory.containerBelt;
                    break;
                case "main":
                    inventory = player.inventory.containerMain;
                    break;
                case "wear":
                    inventory = player.inventory.containerWear;
                    break;
            }
            inventory?.SetLocked(locking);
            player.SendNetworkUpdateImmediate();
            return locking;
        }

        #endregion

        #region Localization
        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "<size=13><color=#ffc300>Inventory Lock</color>\n<size=12>you do not have permission to use this command</size>",
                ["Type Locked"] = "<size=13><color=#ffc300>Inventory Lock</color>\n<size=12>inventory type <color=#ffc300>{0}</color> has been locked for <color=#ffc300>{1}</color> players</size>",
                ["Type UnLocked"] = "<size=13><color=#ffc300>Inventory Lock</color>\n<size=12>inventory type <color=#ffc300>{0}</color> has been unlocked</size>",
                ["All Locked"] = "<size=13><color=#ffc300>Inventory Lock</color>\n<size=12>all inventories have been locked for <color=#ffc300>{0}</color> players</size>",
                ["All UnLocked"] = "<size=13><color=#ffc300>Inventory Lock</color>\n<size=12>all inventories have been unlocked</size>",
                ["InvalidArgument"] = "<size=13><color=#ffc300>Inventory Lock</color>\n<size=12>invalid argument, acceptable arguments are\n\n<color=#ffc300>•</color> all<color=#ffc300>\n•</color> wear<color=#ffc300>\n•</color> main<color=#ffc300>\n•</color> belt</size>"
            }, this);
        }
        private string Lang(string key, string userId = null, params object[] args) => string.Format(lang.GetMessage(key, this, userId), args);
        #endregion

    }
}