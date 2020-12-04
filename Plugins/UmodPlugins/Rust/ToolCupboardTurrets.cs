﻿using UnityEngine;

using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Tool Cupboard Turrets", "0x89A", "1.2.0")]
    [Description("Turrets only attack building blocked players")]

    class ToolCupboardTurrets : RustPlugin
    {
        #region -Fields

        private const string turretsIgnore = "toolcupboardturrets.ignore";
        private const string turretsNeverIgnore = "toolcupboardturrets.neverIgnore";

        #endregion

        #region -Init-

        void Init()
        {
            if (!_config.samSitesAffected && !_config.staticSamSitesAffected)
                Unsubscribe(nameof(OnSamSiteTarget));

            if (!_config.autoturretsAffected && !_config.shotgunTrapsAffected && !_config.flameTrapsAffected && !_config.NPCTurretsAffected)
                Unsubscribe(nameof(CanBeTargeted));

            permission.RegisterPermission(turretsIgnore, this);

            permission.RegisterPermission(turretsNeverIgnore, this);
        }

        #endregion

        #region -Configuration-

        private Configuration _config;

        private class Configuration
        {
            [JsonProperty(PropertyName = "Auto-turrets affected")]
            public bool autoturretsAffected = true;

            [JsonProperty(PropertyName = "Auto-turrets shoot authed players")]
            public bool autoturretsShootAuthed = true;

            [JsonProperty(PropertyName = "shotgun traps affected")]
            public bool shotgunTrapsAffected = true;

            [JsonProperty(PropertyName = "flame traps affected")]
            public bool flameTrapsAffected = true;

            [JsonProperty(PropertyName = "Sam sites affected")]
            public bool samSitesAffected = true;

            [JsonProperty(PropertyName = "Launch site sams affected")]
            public bool staticSamSitesAffected = false;

            [JsonProperty(PropertyName = "Outpost turrets affected")]
            public bool NPCTurretsAffected = false;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new System.Exception();
                SaveConfig();
            }
            catch
            {
                PrintWarning("Error loading config (either corrupt or does not exist), using default values");

                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        protected override void LoadDefaultConfig() => _config = new Configuration();

        #endregion

        #region -Hooks-

        object CanBeTargeted(BasePlayer player, BaseCombatEntity entity)
        {
            if (permission.UserHasPermission(player.UserIDString, turretsIgnore))
                return false;

            else if (permission.UserHasPermission(player.UserIDString, turretsNeverIgnore))
                return null;

            if (entity is NPCAutoTurret && _config.NPCTurretsAffected)
            {
                BuildingPrivlidge priviledge = entity.GetBuildingPrivilege();

                if (priviledge != null && !priviledge.IsAuthed(player) && player.IsBuildingBlocked() && player.IsVisible(new Vector3(entity.transform.position.x, entity.transform.position.y + 0.8f, entity.transform.position.z), new Vector3(player.transform.position.x, player.transform.position.y + 1.5f, player.transform.position.z))) return null;
                else return false;
            }

            if ((entity is AutoTurret && !(entity is NPCAutoTurret) && _config.autoturretsAffected))
            {
                AutoTurret turret = entity as AutoTurret;

                BuildingPrivlidge priviledge = turret.GetBuildingPrivilege();

                if (priviledge != null && !priviledge.IsAuthed(player) && player.IsBuildingBlocked() && player.IsVisible(new Vector3(turret.transform.position.x, turret.transform.position.y + 0.8f, turret.transform.position.z), new Vector3(player.transform.position.x, player.transform.position.y + 1.5f, player.transform.position.z)))
                {
                    if (_config.autoturretsShootAuthed && turret.IsAuthed(player))
                    {
                        turret.SetTarget(player);

                        return null;
                    }
                    else if (!turret.IsAuthed(player))
                        return null;
                }

                return false;
            }

            if ((entity is FlameTurret && _config.flameTrapsAffected) || (entity is GunTrap && _config.shotgunTrapsAffected) && !player.IsBuildingBlocked() || !player.IsVisible(entity.transform.position, player.transform.position, Mathf.Infinity))
                return false;

            return null;
        }

        object OnSamSiteTarget(SamSite samsite, MiniCopter target)
        {
            BasePlayer player = target.GetDriver();

            BuildingPrivlidge priviledge = samsite.GetBuildingPrivilege();

            if (player != null && permission.UserHasPermission(player.UserIDString, turretsIgnore) || (samsite.ShortPrefabName == "sam_site_turret_deployed" && _config.samSitesAffected || samsite.ShortPrefabName == "sam_static" && _config.staticSamSitesAffected) && (priviledge == null || priviledge != null && ((priviledge.IsAuthed(player) || (!priviledge.IsAuthed(player.userID) && !player.IsBuildingBlocked()))))) return false;

            return null;
        }

        #endregion
    }
}

