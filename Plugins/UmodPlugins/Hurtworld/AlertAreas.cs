using Oxide.Core;
using Oxide.Core.Plugins;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Alert Areas", "klauz24", "0.1.3")]
    [Description("Sets areas with notification messages")]
    class AlertAreas : HurtworldPlugin
    {
        List<AlertArea> Areas = new List<AlertArea>();
        string editAreaName = null;

        class AlertArea
        {
            public string name { get; set; }
            public string alertText { get; set; }
            public float firstCornerX { get; set; }
            public float firstCornerZ { get; set; }
            public float secondCornerX { get; set; }
            public float secondCornerZ { get; set; }
            public bool constant { get; set; }

            private HashSet<string> received = new HashSet<string>();

            public void AddReceived(string steamId)
            {
                this.received.Add(steamId);
            }

            public void RemoveReceived(string steamId)
            {
                this.received.Remove(steamId);
            }

            public void ClearReceived()
            {
                this.received.Clear();
            }

            public bool IsReceived(string steamId)
            {
                return this.received.Contains(steamId);
            }

            public AlertArea() { }

            public AlertArea(string name, bool constant = false)
            {
                this.name = name;
                this.constant = constant;
            }

            public AlertArea(string name, string alertText, float firstCornerX, float firstCornerZ, float secondCornerX, float secondCornerZ, bool constant = false)
            {
                this.name = name;
                this.alertText = alertText;
                this.firstCornerX = firstCornerX;
                this.firstCornerZ = firstCornerZ;
                this.secondCornerX = secondCornerX;
                this.secondCornerZ = secondCornerZ;
                this.constant = constant;
            }
        }

        private void LoadAlertAreas()
        {
            var _Areas = Interface.Oxide.DataFileSystem.ReadObject<Collection<AlertArea>>("AlertAreas");
            foreach (var item in _Areas)
            {
                Areas.Add(new AlertArea(
                        item.name,
                        item.alertText,
                        item.firstCornerX,
                        item.firstCornerZ,
                        item.secondCornerX,
                        item.secondCornerZ,
                        item.constant
                    ));
            }
        }

        private void SaveAlertAreas()
        {
            Interface.Oxide.DataFileSystem.WriteObject("AlertAreas", Areas);
        }

        void Init()
        {
            LoadAlertAreas();
        }

        bool IsAlertArea(Vector3 position, AlertArea area)
        {

            float minX = area.firstCornerX < area.secondCornerX ? area.firstCornerX : area.secondCornerX;
            float maxX = area.secondCornerX > area.firstCornerX ? area.secondCornerX : area.firstCornerX;

            float minZ = area.firstCornerZ < area.secondCornerZ ? area.firstCornerZ : area.secondCornerZ;
            float maxZ = area.secondCornerZ > area.firstCornerZ ? area.secondCornerZ : area.firstCornerZ;

            if (minX <= position.x && position.x <= maxX
            && minZ <= position.z && position.z <= maxZ)
            {
                return true;
            }

            return false;
        }

        // For TradeZone plugin by SouZa.
        bool isInsideArea(Vector3 position, string areaName)
        {
            foreach (AlertArea area in Areas)
            {
                if (area.name.ToLower().Contains(areaName.ToLower()))
                {
                    if (IsAlertArea(position, area))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        void SendAreaAlerts(PlayerSession session)
        {
            //Puts("Checking area '" + area.name + "' for SteamId: ");
            string steamId = session.SteamId.ToString();

            if (!String.IsNullOrEmpty(steamId))
            {
                foreach (AlertArea area in Areas)
                {

                    Vector3 playerPosition = new Vector3(
                            session.WorldPlayerEntity.transform.position.x,
                            session.WorldPlayerEntity.transform.position.y,
                            session.WorldPlayerEntity.transform.position.z
                        );

                    if (IsAlertArea(playerPosition, area) && !string.IsNullOrEmpty(area.alertText) && (!area.IsReceived(steamId) || area.constant))
                    {
                        AlertManager.Instance.GenericTextNotificationServer(area.alertText, session.Player);
                        area.AddReceived(steamId);
                    }
                    else
                    {
                        area.RemoveReceived(steamId);
                    }
                }
            }
        }

        void OnPlayerInput(PlayerSession session, InputControls input)
        {
            if (input.Forward
            || input.Backward
            || input.StrafeLeft
            || input.StrafeRight
            || input.Sprint
            || input.Crouch)
            {
                SendAreaAlerts(session);
            }
        }

        bool HasAccess(PlayerSession session) => session.IsAdmin;

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"msg_prefix", "<color=yellow>[AlertAreas]</color> "},
                {"msg_noPermission", "You dont have permission to do this!"},
                {"msg_wrongSyntaxEditName", "Wrong syntax! Use: <color=aqua>/alertareas name <NewAreaName></color>"},
                {"msg_editNameSuccess", "Now area has the new name <color=green>{newName}</color>"},
                {"msg_wrongSyntaxMain", "Wrong syntax! Use: <color=aqua>/alertareas help</color>"},
                {"msg_help", "Available commands:"},
                {"msg_helpList", "<color=aqua>/alertareas list</color> - show all areas with alert."},
                {"msg_helpAdd", "<color=aqua>/alertareas add <AreaName> [true]</color> - add new area (with optional \"true\" alert text will be shown constant)."},
                {"msg_helpRemove", "<color=aqua>/alertareas remove <AreaName></color> - remove area (case insensitive)."},
                {"msg_helpEdit", "<color=aqua>/alertareas edit <AreaName></color> - edit mode where you can set alert text, corners coordinates and 'constant' attribute."},
                {"msg_noAlertAreas", "No areas with alert."},
                {"msg_areaListHeader", "Areas with alerts:"},
                {"msg_areaListItemTitle", "{i}. <color=green>{name}</color>: {alertText}"},
                {"msg_areaListItemCoords", "<color=grey>({firstCornerCoords}|{secondCornerCoords}) {constant}</color>"},
                {"msg_areaListItemConstant", "<color=red>(constant)</color>"},
                {"msg_addWrongSyntax", "Wrong syntax! Use: <color=aqua>/alertareas add <AreaName> [true]</color>"},
                {"msg_editWrongSyntax", "Wrong syntax! Use: <color=aqua>/alertareas edit <AreaName></color>"},
                {"msg_removeWrongSyntax", "Wrong syntax! Use: <color=aqua>/alertareas remove <AreaName></color>"},
                {"msg_constant", "constant"},
                {"msg_notConstant", "not constant"},
                {"msg_editCornerSuccess", "Corner {n} was successfully saved for area <color=green>{name}</color>."},
                {"msg_editTextSuccess", "Alert text was successfully saved for area <color=green>{name}</color>."},
                {"msg_editConstantSuccess", "Now the alert of area <color=green>{name}</color> is <color=green> {constantOrNot}</color>."},
                {"msg_wrongSyntaxEditCorner", "Wrong syntax! Use: <color=aqua>/alertareas corner 1</color> to set first corner and <color=aqua>/alertareas corner 2</color> to set second corner."},
                {"msg_wrongSyntaxEditConstant", "Wrong syntax! Use: <color=aqua>/alertareas constant <true|false></color>"},
                {"msg_wrongSyntaxEditText", "Wrong syntax! Use: <color=aqua>/alertareas text <Some alert text!></color>"},
                {"msg_areaNotFound", "Area <color=green>{name}</color> not found."},
                {"msg_editAreaNotFound", "Editing area <color=green>{name}</color> not found."},
                {"msg_areaAdded", "Area <color=green>{name}</color> has been added."},
                {"msg_areaRemoved", "Area <color=green>{name}</color> has been removed."},
                {"msg_areaAlreadyExists", "Area <color=green>{name}</color> already exists (case insensitive)."},
                {"msg_editMode", "Now you are editing area <color=green>{name}</color>"},
                {"msg_helpEditCorner1", "<color=aqua>/alertareas corner 1</color> - set first corner coordinates."},
                {"msg_helpEditCorner2", "<color=aqua>/alertareas corner 2</color> - set second corner coordinates."},
                {"msg_helpEditName", "<color=aqua>/alertareas name <NewAreaName></color> - change area name."},
                {"msg_helpEditText", "<color=aqua>/alertareas text <Some alert text!></color> - set area alert text."},
                {"msg_helpEditConstant", "<color=aqua>/alertareas constant <true|false></color> - show alert constant or not."},
                {"msg_notEditMode", "You are not in edit mode. Use: <color=aqua>/alertareas edit <AreaName></color> first."},
            }, this);
        }

        [ChatCommand("alertareas")]
        void cmdAlertAreas(PlayerSession session, string command, string[] args)
        {
            if (!HasAccess(session))
            {
                msgToPlayer(session, GetLang("msg_prefix", session.SteamId.ToString()) + GetLang("msg_noPermission", session.SteamId.ToString()));
                return;
            }

            if (args.Length == 0)
            {
                msgToPlayer(session, GetLang("msg_prefix", session.SteamId.ToString()) + GetLang("msg_wrongSyntaxMain", session.SteamId.ToString()));
                return;
            }

            string areaName;
            int areaIndex;

            switch (args[0])
            {
                case "help":
                    msgToPlayer(session, GetLang("msg_prefix", session.SteamId.ToString()) + GetLang("msg_help", session.SteamId.ToString()));
                    msgToPlayer(session, GetLang("msg_helpList", session.SteamId.ToString()));
                    msgToPlayer(session, GetLang("msg_helpAdd", session.SteamId.ToString()));
                    msgToPlayer(session, GetLang("msg_helpRemove", session.SteamId.ToString()));
                    msgToPlayer(session, GetLang("msg_helpEdit", session.SteamId.ToString()));
                    return;

                case "list":
                    if (Areas.Count == 0)
                    {
                        msgToPlayer(session, GetLang("msg_prefix", session.SteamId.ToString()) + GetLang("msg_noAlertAreas", session.SteamId.ToString()));
                        return;
                    }

                    msgToPlayer(session, GetLang("msg_prefix", session.SteamId.ToString()) + GetLang("msg_areaListHeader", session.SteamId.ToString()));

                    int rowNum = 0;
                    foreach (var area in Areas)
                    {
                        rowNum++;

                        msgToPlayer(session, GetLang("msg_areaListItemTitle", session.SteamId.ToString())
                            .Replace("{i}", rowNum.ToString())
                            .Replace("{name}", area.name)
                            .Replace("{alertText}", area.alertText)
                        );

                        msgToPlayer(session, GetLang("msg_areaListItemCoords", session.SteamId.ToString())
                            .Replace("{firstCornerCoords}", area.firstCornerX + "," + area.firstCornerZ)
                            .Replace("{secondCornerCoords}", area.secondCornerX + "," + area.secondCornerZ)
                            .Replace("{constant}", area.constant ? GetLang("msg_areaListItemConstant", session.SteamId.ToString()) : "")
                        );
                    }

                    break;


                case "add":
                case "edit":
                case "remove":

                    if (args.Length < 2)
                    {
                        msgToPlayer(session, GetLang("msg_prefix", session.SteamId.ToString()) + GetLang("msg_" + args[0] + "WrongSyntax", session));
                        return;
                    }

                    editAreaName = null;
                    areaName = args[1];
                    areaIndex = Areas.FindIndex(item => item.name.ToLower() == areaName.ToLower());

                    switch (args[0])
                    {

                        case "add":
                            if (areaIndex >= 0)
                            {
                                msgToPlayer(session, GetLang("msg_prefix", session.SteamId.ToString()) + GetLang("msg_areaAlreadyExists", session.SteamId.ToString())
                                    .Replace("{name}", Areas[areaIndex].name));
                                return;
                            }

                            if ((args.Length != 2 && args.Length != 3)
                            || (args.Length == 3 && args[2] != "true" && args[2] != "false"))
                            {
                                msgToPlayer(session, GetLang("msg_prefix", session.SteamId.ToString()) + GetLang("msg_addWrongSyntax", session.SteamId.ToString()));

                                return;
                            }

                            Areas.Add(new AlertArea(
                                areaName,
                                args.Length == 3 ? (args[2] == "true" ? true : false) : false
                            ));
                            msgToPlayer(session, GetLang("msg_prefix", session.SteamId.ToString()) + GetLang("msg_areaAdded", session.SteamId.ToString())
                                .Replace("{name}", areaName));

                            break;

                        case "edit":

                            if (areaIndex == -1)
                            {
                                msgToPlayer(session, GetLang("msg_prefix", session.SteamId.ToString()) + GetLang("msg_areaNotFound", session.SteamId.ToString())
                                    .Replace("{name}", areaName));
                                return;
                            }

                            msgToPlayer(session, GetLang("msg_prefix", session.SteamId.ToString()) + GetLang("msg_editMode", session.SteamId.ToString())
                                .Replace("{name}", Areas[areaIndex].name));

                            msgToPlayer(session, GetLang("msg_helpEditCorner1", session.SteamId.ToString()));
                            msgToPlayer(session, GetLang("msg_helpEditCorner2", session.SteamId.ToString()));
                            msgToPlayer(session, GetLang("msg_helpEditName", session.SteamId.ToString()));
                            msgToPlayer(session, GetLang("msg_helpEditText", session.SteamId.ToString()));
                            msgToPlayer(session, GetLang("msg_helpEditConstant", session.SteamId.ToString()));

                            editAreaName = areaName;

                            break;

                        case "remove":

                            if (areaIndex == -1)
                            {
                                msgToPlayer(session, GetLang("msg_prefix", session.SteamId.ToString()) + GetLang("msg_areaNotFound", session.SteamId.ToString())
                                    .Replace("{name}", areaName));
                                return;
                            }
                            msgToPlayer(session, GetLang("msg_prefix", session.SteamId.ToString()) + GetLang("msg_areaRemoved", session.SteamId.ToString())
                                .Replace("{name}", Areas[areaIndex].name));
                            Areas.RemoveAt(areaIndex);

                            break;
                    }

                    break;


                case "name":
                case "text":
                case "corner":
                case "constant":

                    if (editAreaName == null)
                    {
                        msgToPlayer(session, GetLang("msg_prefix", session.SteamId.ToString()) + GetLang("msg_notEditMode", session.SteamId.ToString()));
                        return;
                    }

                    areaIndex = Areas.FindIndex(item => item.name.ToLower() == editAreaName.ToLower());

                    if (areaIndex == -1)
                    {
                        msgToPlayer(session, GetLang("msg_prefix", session.SteamId.ToString()) + GetLang("msg_editAreaNotFound", session.SteamId.ToString())
                            .Replace("{name}", Areas[areaIndex].name));
                        editAreaName = null;
                        return;
                    }


                    switch (args[0])
                    {
                        case "name":

                            if (args.Length != 2)
                            {
                                msgToPlayer(session, GetLang("msg_prefix", session.SteamId.ToString()) + GetLang("msg_wrongSyntaxEditName", session.SteamId.ToString()));
                                return;
                            }

                            string newName = args[1];
                            int newNameIndex = Areas.FindIndex(item => item.name.ToLower() == newName.ToLower());

                            if (newNameIndex != -1)
                            {
                                msgToPlayer(session, GetLang("msg_prefix", session.SteamId.ToString()) + GetLang("msg_areaAlreadyExists", session.SteamId.ToString())
                                    .Replace("{name}", Areas[newNameIndex].name));
                                return;
                            }

                            Areas[areaIndex].name = newName;

                            msgToPlayer(session, GetLang("msg_prefix", session.SteamId.ToString()) + GetLang("msg_editNameSuccess", session.SteamId.ToString())
                                .Replace("{newName}", Areas[areaIndex].name));

                            break;


                        case "text":

                            if (args.Length < 2)
                            {
                                msgToPlayer(session, GetLang("msg_prefix", session.SteamId.ToString()) + GetLang("msg_wrongSyntaxEditText", session.SteamId.ToString()));
                                return;
                            }

                            string[] textArr = new string[args.Length - 1];
                            for (int i = 0, j = 0; i < textArr.Length; i++, j++)
                            {
                                if (i == 0) j++;
                                textArr[i] = args[j];
                            }
                            string text = String.Join(" ", textArr);

                            Areas[areaIndex].alertText = text;

                            msgToPlayer(session, GetLang("msg_prefix", session.SteamId.ToString()) + GetLang("msg_editTextSuccess", session.SteamId.ToString())
                                .Replace("{name}", Areas[areaIndex].name));

                            break;


                        case "corner":

                            if (args.Length != 2 || (args[1] != "1" && args[1] != "2"))
                            {
                                msgToPlayer(session, GetLang("msg_prefix", session.SteamId.ToString()) + GetLang("msg_wrongSyntaxEditCorner", session.SteamId.ToString()));
                                return;
                            }

                            switch (args[1])
                            {
                                case "1":
                                    Areas[areaIndex].firstCornerX = (float)Math.Round((decimal)session.WorldPlayerEntity.transform.position.x, 1);
                                    Areas[areaIndex].firstCornerZ = (float)Math.Round((decimal)session.WorldPlayerEntity.transform.position.z, 1);
                                    break;

                                case "2":
                                    Areas[areaIndex].secondCornerX = (float)Math.Round((decimal)session.WorldPlayerEntity.transform.position.x, 1);
                                    Areas[areaIndex].secondCornerZ = (float)Math.Round((decimal)session.WorldPlayerEntity.transform.position.z, 1);
                                    break;
                            }

                            msgToPlayer(session, GetLang("msg_prefix", session.SteamId.ToString()) + GetLang("msg_editCornerSuccess", session.SteamId.ToString())
                                .Replace("{name}", Areas[areaIndex].name)
                                .Replace("{n}", args[1]));

                            break;


                        case "constant":

                            if (args.Length != 2 || (args[1] != "true" && args[1] != "false"))
                            {
                                msgToPlayer(session, GetLang("msg_prefix", session.SteamId.ToString()) + GetLang("msg_wrongSyntaxEditConstant", session.SteamId.ToString()));
                                return;
                            }

                            Areas[areaIndex].constant = args[1] == "true" ? true : false;

                            msgToPlayer(session, GetLang("msg_prefix", session.SteamId.ToString()) + GetLang("msg_editConstantSuccess", session.SteamId.ToString())
                                .Replace("{name}", Areas[areaIndex].name)
                                .Replace("{constantOrNot}", Areas[areaIndex].constant ? GetLang("msg_constant", session.SteamId.ToString()) : GetLang("msg_notConstant", session.SteamId.ToString())));
                            break;
                    }

                    break;

                default:
                    msgToPlayer(session, GetLang("msg_prefix", session.SteamId.ToString()) + GetLang("msg_wrongSyntaxMain", session.SteamId.ToString()));
                    return;
            }

            SaveAlertAreas();
        }

        #region Chat Formatting
        string GetLang(string key, object SteamId = null) => lang.GetMessage(key, this, SteamId == null ? null : SteamId.ToString());

        void msgToPlayer(PlayerSession session, string msg) => hurt.SendChatMessage(session, null, msg);
        #endregion Chat Formatting
    }
}