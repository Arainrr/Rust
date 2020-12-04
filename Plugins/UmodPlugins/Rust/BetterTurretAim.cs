﻿﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Oxide.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Better Turret Aim", "WhiteThunder", "1.0.0")]
    [Description("Improves the speed at which auto turrets aim at their current target.")]
    internal class BetterTurretAim : CovalencePlugin
    {
        #region Fields

        private static BetterTurretAim PluginInstance;

        private Configuration PluginConfig;

        private const string PermissionUse = "betterturretaim.use";

        #endregion

        #region Hooks

        private void Init()
        {
            PluginInstance = this;

            permission.RegisterPermission(PermissionUse, this);

            if (!PluginConfig.RequirePermission)
            {
                Unsubscribe(nameof(OnGroupPermissionGranted));
                Unsubscribe(nameof(OnGroupPermissionRevoked));
                Unsubscribe(nameof(OnUserPermissionGranted));
                Unsubscribe(nameof(OnUserPermissionRevoked));
            }
        }

        private void OnServerInitialized(bool initialBoot)
        {
            if (!initialBoot)
                InitializeAutoTurrets();
        }

        private void Unload()
        {
            DestroyAimComponents();
            PluginInstance = null;
        }

        private void OnEntitySpawned(AutoTurret autoTurret) =>
            MaybeImproveAim(autoTurret);

        private void OnGroupPermissionGranted(string group, string perm)
        {
            if (perm != PermissionUse) return;
            OnUsagePermissionChanged();
        }

        private void OnGroupPermissionRevoked(string group, string perm)
        {
            if (perm != PermissionUse) return;
            OnUsagePermissionChanged();
        }

        private void OnUserPermissionGranted(string userId, string perm)
        {
            if (perm != PermissionUse) return;
            OnUsagePermissionChanged(userId);
        }

        private void OnUserPermissionRevoked(string userId, string perm)
        {
            if (perm != PermissionUse) return;
            OnUsagePermissionChanged(userId);
        }

        #endregion

        #region Helper Methods

        private void InitializeAutoTurrets()
        {
            foreach (var autoTurret in BaseNetworkable.serverEntities.OfType<AutoTurret>())
                MaybeImproveAim(autoTurret);
        }

        private bool ImproveAimWasBlocked(AutoTurret autoTurret)
        {
            object hookResult = Interface.CallHook("OnAutoTurretAimImprove", autoTurret);
            return hookResult is bool && (bool)hookResult == false;
        }

        private void OnUsagePermissionChanged(string userIdString = "")
        {
            foreach (var autoTurret in BaseNetworkable.serverEntities.OfType<AutoTurret>())
            {
                var ownerId = autoTurret.OwnerID;
                
                // If a userId was specified, skip updating any turrets that don't belong to that user
                if (userIdString != string.Empty && userIdString != ownerId.ToString())
                    continue;

                var aimComponent = autoTurret.GetComponent<TurretAimImprover>();
                if (aimComponent == null)
                    MaybeImproveAim(autoTurret);
                else if (ownerId == 0 || !permission.UserHasPermission(userIdString, PermissionUse))
                    UnityEngine.Object.Destroy(aimComponent);
            }
        }

        private void MaybeImproveAim(AutoTurret autoTurret)
        {
            if (autoTurret is NPCAutoTurret)
                return;

            if (PluginConfig.OnlyVehicles && GetParentVehicle(autoTurret) == null)
                return;

            if (!PluginConfig.RequirePermission)
            {
                ImproveAiming(autoTurret);
                return;
            }

            var ownerId = autoTurret.OwnerID;
            if (ownerId == 0 || !permission.UserHasPermission(ownerId.ToString(), PermissionUse))
                return;

            ImproveAiming(autoTurret);
        }

        private BaseVehicle GetParentVehicle(BaseEntity entity)
        {
            var parent = entity.GetParentEntity();
            return parent as BaseVehicle ?? (parent as BaseVehicleModule)?.Vehicle;
        }

        private void ImproveAiming(AutoTurret autoTurret)
        {
            if (ImproveAimWasBlocked(autoTurret))
                return;

            var aimComponent = autoTurret.gameObject.AddComponent<TurretAimImprover>();
        }

        private void DestroyAimComponents()
        {
            var aimComponents = UnityEngine.Object.FindObjectsOfType<TurretAimImprover>();
            if (aimComponents != null)
                foreach (var component in aimComponents)
                    UnityEngine.Object.Destroy(component);
        }

        #endregion

        #region Helper Classes

        internal class TurretAimImprover : FacepunchBehaviour
        {
            private AutoTurret Turret;

            private void Awake()
            {
                Turret = GetComponent<AutoTurret>();

                var tickInterval = PluginInstance.PluginConfig.UpdateIntervalSeconds;
                InvokeRandomized(MaybeUpdateAim, UnityEngine.Random.Range(0f, 1f), tickInterval, tickInterval * 0.25f);
            }

            private void MaybeUpdateAim()
            {
                if (Turret == null ||
                    Turret.GetAttachedWeapon() == null ||
                    !Turret.HasTarget() ||
                    Turret.aimDir == Vector3.zero)
                    return;

                var lookRotation = Quaternion.LookRotation(Turret.aimDir);
                var targetYaw = Quaternion.Euler(0, lookRotation.eulerAngles.y, 0);
                var targetPitch = Quaternion.Euler(lookRotation.eulerAngles.x, 0, 0);

                var gunYaw = Turret.gun_yaw.transform.rotation;
                var gunPitch = Turret.gun_pitch.transform.localRotation;

                var interpolation = PluginInstance.PluginConfig.Interpolation;

                if (gunYaw != targetYaw)
                    Turret.gun_yaw.transform.rotation = Quaternion.Lerp(gunYaw, targetYaw, interpolation);

                if (gunPitch != targetPitch)
                    Turret.gun_pitch.transform.localRotation = Quaternion.Lerp(gunPitch, targetPitch, interpolation);
            }
        }

        #endregion

        #region Configuration

        internal class Configuration : SerializableConfiguration
        {
            [JsonProperty("RequirePermission")]
            public bool RequirePermission = true;

            [JsonProperty("OnlyVehicles")]
            public bool OnlyVehicles = false;

            [JsonProperty("UpdateIntervalSeconds")]
            public float UpdateIntervalSeconds = 0.015f;

            [JsonProperty("Interpolation")]
            public float Interpolation = 0.5f;
        }

        internal class SerializableConfiguration
        {
            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonHelper.Deserialize(ToJson()) as Dictionary<string, object>;
        }

        internal static class JsonHelper
        {
            public static object Deserialize(string json) => ToObject(JToken.Parse(json));

            private static object ToObject(JToken token)
            {
                switch (token.Type)
                {
                    case JTokenType.Object:
                        return token.Children<JProperty>()
                                    .ToDictionary(prop => prop.Name,
                                                  prop => ToObject(prop.Value));

                    case JTokenType.Array:
                        return token.Select(ToObject).ToList();

                    default:
                        return ((JValue)token).Value;
                }
            }
        }

        private bool MaybeUpdateConfig(Configuration config)
        {
            var currentWithDefaults = config.ToDictionary();
            var currentRaw = Config.ToDictionary(x => x.Key, x => x.Value);
            return MaybeUpdateConfigDict(currentWithDefaults, currentRaw);
        }

        private bool MaybeUpdateConfigDict(Dictionary<string, object> currentWithDefaults, Dictionary<string, object> currentRaw)
        {
            bool changed = false;

            foreach (var key in currentWithDefaults.Keys)
            {
                object currentRawValue;
                if (currentRaw.TryGetValue(key, out currentRawValue))
                {
                    var defaultDictValue = currentWithDefaults[key] as Dictionary<string, object>;
                    var currentDictValue = currentRawValue as Dictionary<string, object>;

                    if (defaultDictValue != null)
                    {
                        if (currentDictValue == null)
                        {
                            currentRaw[key] = currentWithDefaults[key];
                            changed = true;
                        }
                        else if (MaybeUpdateConfigDict(defaultDictValue, currentDictValue))
                            changed = true;
                    }
                }
                else
                {
                    currentRaw[key] = currentWithDefaults[key];
                    changed = true;
                }
            }

            return changed;
        }

        protected override void LoadDefaultConfig() => PluginConfig = new Configuration();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                PluginConfig = Config.ReadObject<Configuration>();
                if (PluginConfig == null)
                {
                    throw new JsonException();
                }

                if (MaybeUpdateConfig(PluginConfig))
                {
                    LogWarning("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch
            {
                LogWarning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            Log($"Configuration changes saved to {Name}.json");
            Config.WriteObject(PluginConfig, true);
        }

        #endregion
    }
}
