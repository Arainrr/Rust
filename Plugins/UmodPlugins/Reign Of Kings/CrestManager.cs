﻿using CodeHatch;
using CodeHatch.Common;
using CodeHatch.Engine.Core.Cache;
using CodeHatch.Engine.Entities.Definitions;
using CodeHatch.Engine.Modules.SocialSystem;
using CodeHatch.Engine.Networking;
using CodeHatch.Thrones.SocialSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Crest Manager", "D-Kay", "1.0.1")]
    [Description("Modify and manage crests around the world.")]
    public class CrestManager : ReignOfKingsPlugin
    {
        #region Variables

        #region Properties

        private readonly List<string> _permissions = new List<string>
        {
            "Use",
        };

        #endregion

        #endregion

        #region Save and Load Data

        private void Init()
        {
            RegisterPermissions();
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "No permission", "You do not have permission to use this command." },
                { "Invalid args", "It looks like you made a mistake somewhere. Use /Plugin help for a list of available commands" },

                { "Crest none", "Could not find a crest within distance." },

                { "Player none", "{0} is currently not online." },
                { "Player crest exists", "{0} already has a crest placed somewhere on the map." },

                { "Help title", "[0000FF]Crest Manager Commands[-]" },
                { "Help place", "[00FF00]/crest place <optional: distance>[-] - Place a steel crest where you're looking at. If no surface is found within distance, crest is places at your current position." },
                { "Help finish", "[00FF00]/crest finish[-] - Instantly finish the crest you're looking at." },
                { "Help remove", "[00FF00]/crest remove[-] - Remove the crest that belongs to the area you're in." },
                { "Help modify", "[00FF00]/crest modify <playername>[-] - Change the ownership of the crest you're looking at to the provided player." },
            }, this);
        }

        #endregion

        #region Commands

        [ChatCommand("Crest")]
        private void CmdPlugin(Player player, string cmd, string[] args)
        {
            if (!CheckPermission(player, "use")) return;
            if (!CheckArgs(player, args)) return;

            switch (args.First().ToLower())
            {
                case "place":
                    CmdPlace(player, args.Skip(1));
                    break;
                case "finish":
                    CmdFinish(player);
                    break;
                case "remove":
                    CmdRemove(player);
                    break;
                case "modify":
                    CmdModify(player, args.Skip(1));
                    break;
                case "help":
                    SendHelpText(player);
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region Command Functions

        private void CmdPlace(Player player, IEnumerable<string> args)
        {
            var distance = 5f;
            var position = player.Entity.Position;
            var rotation = player.Entity.Rotation;
            if (args.Any())
            {
                if (!float.TryParse(args.JoinToString(""), out distance)) distance = 5f;
            }
            RaycastHit hit;
            if (Physics.Raycast(player.Entity.Position, player.Entity.GetOrCreate<LookBridge>().Forward, out hit, distance))
            {
                position = hit.point;
                rotation = Quaternion.LookRotation(hit.normal);
            }
            PlaceCrest(position, rotation);
        }

        private void CmdFinish(Player player)
        {
            var crest = GetCrest(player, 20f);
            if (crest == null)
            {
                SendError(player, "Crest none");
                return;
            }

            FinishConquering(crest.TheCrest);
        }

        private void CmdRemove(Player player)
        {
            RemoveCrest(player.Entity.Position);
        }

        private void CmdModify(Player player, IEnumerable<string> args)
        {
            var target = Server.GetPlayerByName(args.JoinToString(" "));
            if (target == null)
            {
                SendError(player, "Player none", args.JoinToString(" "));
                return;
            }
            
            if (HasCrest(target.Id))
            {
                SendError(player, "Player crest exists", target.Name);
                return;
            }

            var crest = GetCrest(player, 20f);
            if (crest == null)
            {
                SendError(player, "Crest none");
                return;
            }

            ChangeOwnership(crest.TheCrest, target);
        }

        #endregion

        #region System Functions

        private void FinishConquering(Crest crest)
        {
            crest.TransitionTime = Server.Time.TotalTimeOnline;
        }

        private void PlaceCrest(Vector3 position, Quaternion rotation)
        {
            var blueprint = InvBlueprints.GetBlueprint("Steel Crest", false, true);
            if (blueprint == null) return;
            CustomNetworkInstantiate.ServerInstantiate(blueprint, position, rotation);
        }

        private void RemoveCrest(Vector3 position)
        {
            var crestScheme = SocialAPI.Get<CrestScheme>();
            var crest = crestScheme.GetCrestAt(position);
            if (crest == null) return;

            var entity = Entity.GetAll().FirstOrDefault(c => c.Position == crest.Position);
            if (entity)
            {
                var health = entity.GetComponentInChildren<EntityHealth>();
                health.Kill();
                return;
            }

            crestScheme.RemoveObject(crest.SocialId, crest.ObjectGUID);
            crestScheme.UnregisterObject(crest.ObjectGUID, crest.Position);
        }

        private void RemoveCrest(CrestBehaviour crest)
        {
            RemoveCrest(crest.TheCrest);
            UnityEngine.Object.Destroy(crest.gameObject);
        }

        private void RemoveCrest(Crest crest)
        {
            var crestScheme = SocialAPI.Get<CrestScheme>();
            crestScheme.RemoveObject(crest.SocialId, crest.ObjectGUID);
            crestScheme.UnregisterObject(crest.ObjectGUID, crest.Position);
        }

        private void RemoveCrest(Player player)
        {
            var crestScheme = SocialAPI.Get<CrestScheme>();
            var crest = crestScheme.GetCrest(player.Id);
            if (crest == null) return;
            crestScheme.RemoveObject(crest.SocialId, crest.ObjectGUID);
            crestScheme.UnregisterObject(crest.ObjectGUID, crest.Position);
        }

        private void ChangeOwnership(Crest crest, Player target)
        {
            var finished = crest.Completed;
            var crestScheme = SocialAPI.Get<CrestScheme>();
            var crests = crestScheme.Storage.GetStatic<Crests>();
            crestScheme.RemoveObject(crest.SocialId, crest.ObjectGUID);
            crests.UnRegisterObject(crest.ObjectGUID, crest.Position);
            crests.RegisterObject(crest.ObjectGUID, crest.Position);
            crestScheme.AddObject(target.GetGuild().BaseID, crest.ObjectGUID, target.Id);
            if (finished) FinishConquering(crest);
        }

        private bool HasCrest(ulong playerId)
        {
            var crestScheme = SocialAPI.Get<CrestScheme>();
            return crestScheme.GetCrest(playerId) != null;
        }

        private CrestBehaviour GetCrest(Player player, float distance)
        {
            GameObject crest;
            if (!TryGetGameObject(player, distance, out crest)) return null;

            var crestBehaviour = crest.GetComponent<CrestBehaviour>();
            if (crestBehaviour != null) return crestBehaviour;
            crestBehaviour = crest.GetComponentInChildren<CrestBehaviour>();
            return crestBehaviour != null ? crestBehaviour : null;
        }

        private bool TryGetGameObject(Player player, float distance, out GameObject gameObject)
        {
            gameObject = null;
            RaycastHit hit;
            if (!Physics.Raycast(player.Entity.Position, player.Entity.GetOrCreate<LookBridge>().Forward, out hit, distance)) return false;
            gameObject = hit.transform.gameObject;
            return true;
        }

        #endregion

        #region Hooks

        private void SendHelpText(Player player)
        {
            if (!HasPermission(player, "use")) return;
            SendMessage(player, "Help title");
            SendMessage(player, "Help place");
            SendMessage(player, "Help finish");
            SendMessage(player, "Help remove");
            SendMessage(player, "Help modify");
        }

        #endregion

        #region Utility

        private void RegisterPermissions()
        {
            _permissions.Foreach(p => permission.RegisterPermission($"{Name}.{p}", this));
        }
        private bool CheckPermission(Player player, string permission)
        {
            if (HasPermission(player, permission)) return true;
            SendError(player, "No permission");
            return false;
        }
        private bool HasPermission(Player player, string permission)
        {
            return player.HasPermission($"{Name}.{permission}");
        }
        private bool CheckArgs(Player player, IEnumerable<string> args, int count = 1)
        {
            if (!args.Any())
            {
                SendError(player, "Invalid args");
                return false;
            }
            if (count <= 1) return true;

            if (args.Count() >= count) return true;
            SendError(player, "Invalid args");
            return false;
        }

        private void SendMessage(Player player, string key, params object[] obj) => player?.SendMessage(GetMessage(key, player, obj));
        private void SendError(Player player, string key, params object[] obj) => player?.SendError(GetMessage(key, player, obj));

        public string GetMessage(string key, Player player, params object[] obj)
        {
            return GetMessage(key, player, false, obj);
        }
        public string GetMessage(string key, Player player, bool hasTitle, params object[] args)
        {
            var title = hasTitle ? lang.GetMessage("Message prefix", this, player?.Id.ToString()) : "";
            return string.Format(title + lang.GetMessage(key, this, player?.Id.ToString()), args);
        }

        #endregion
    }
}
