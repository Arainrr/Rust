﻿using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Auto Takeoff", "0x89A", "1.0.8")]
    [Description("Allows smooth takeoff with helicopters")]
    class AutoTakeoff : RustPlugin
    {
        #region -Fields-

        const string canUse = "autotakeoff.use";

        private Dictionary<int, bool> isTakeoff = new Dictionary<int, bool>();

        #endregion

        void Init()
        {
            permission.RegisterPermission(canUse, this);
        }

        #region -Localization-

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You do not have permission to use this command",
                ["NotMounted"] = "You are not in a helicopter",
                ["NotOnGround"] = "You are too far from the ground",
                ["NotFlying"] = "The helicopter is not flying",
                ["ErrorFound"] = "Error with plugin, please try again",
                ["DefaultConfig"] = "Generating new config"
            }
            , this);
        }

        #endregion

        #region -Configuration-

        private Configuration _config;
        class Configuration
        {
            [JsonProperty(PropertyName = "Take off method type")]
            public bool takeOffMethodType = true;

            [JsonProperty(PropertyName = "Helicopter move distance")]
            public float distanceMoved = 10f;

            [JsonProperty(PropertyName = "Minicopter can auto takeoff")]
            public bool minicopterCanTakeoff = true;

            [JsonProperty(PropertyName = "Minicopter move speed")]
            public float minicopterSpeed = 0.025f;

            [JsonProperty(PropertyName = "Minicopter push force")]
            public float minicopterPushForce = 50;

            [JsonProperty(PropertyName = "Scrap Helicopter can auto takeoff")]
            public bool scrapheliCanTakeoff = true;

            [JsonProperty(PropertyName = "Scrap helicopter move speed")]
            public float scrapHelicopterSpeed = 0.0075f;

            [JsonProperty(PropertyName = "Scrap helicopter push force")]
            public float scrapHelicopterPushForce = 100;
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
                PrintWarning("Error with config, using default values");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        protected override void LoadDefaultConfig() => _config = new Configuration();

        #endregion

        #region -Chat Command-

        [ChatCommand("takeoff")]
        void ChatCommand(BasePlayer player)
        {
            if (player != null && player.GetMountedVehicle() != null)
            {
                if (!permission.UserHasPermission(player.UserIDString, canUse) || !_config.scrapheliCanTakeoff && player.GetMountedVehicle().ShortPrefabName == "scraptransporthelicopter" 
                || !_config.minicopterCanTakeoff && player.GetMountedVehicle().ShortPrefabName == "minicopter.entity")
                {
                    PrintToChat(player, lang.GetMessage("NoPermission", this, player.UserIDString));
                    return;
                }

                TakeOff(player);
            }
        }

        void TakeOff(BasePlayer player)
        {
            BaseVehicle playerVehicle = player.GetMountedVehicle();

            MiniCopter helicopter = playerVehicle as MiniCopter;

            if (helicopter != null && helicopter.IsEngineOn() && helicopter.isMobile)
            {
                if (!isTakeoff.ContainsKey(helicopter.GetInstanceID()))
                    isTakeoff.Add(helicopter.GetInstanceID(), true);

                else isTakeoff[helicopter.GetInstanceID()] = true;

                //raycast to check if on ground
                Ray ray = new Ray(helicopter.transform.position, -Vector2.up);

                if (Physics.Raycast(ray, 0.5f))
                {
                    if (_config.takeOffMethodType)
                    {
                        helicopter.StartCoroutine(LerpMethod(player, helicopter));
                    }
                    else if (!_config.takeOffMethodType)
                    {
                        PushMethod(player, helicopter);
                    }
                }
                else PrintToChat(player, lang.GetMessage("NotOnGround", this, player.UserIDString));
            }
            else if (helicopter == null) PrintToChat(player, lang.GetMessage("NotMounted", this, player.UserIDString));
            else if (!helicopter.IsEngineOn() || !helicopter.isMobile) PrintToChat(player, lang.GetMessage("NotFlying", this, player.UserIDString));
        }

        #endregion

        #region -Methods-

        IEnumerator LerpMethod(BasePlayer player, MiniCopter helicopter)
        {
            if (helicopter != null)
            {
                Vector3 endPos = helicopter.transform.position + (Vector3.up * _config.distanceMoved);

                float distance = Vector3.Distance(helicopter.transform.position, endPos);
				
				 float speed;

                    if (helicopter.ShortPrefabName == "minicopter.entity") speed = _config.minicopterSpeed;
                    else speed = _config.scrapHelicopterSpeed;

                float startTime = Time.time;

                while (isTakeoff.ContainsKey(helicopter.GetInstanceID()) && isTakeoff[helicopter.GetInstanceID()] && helicopter.HasAnyPassengers() && helicopter.IsEngineOn())
                {
                    float distCovered = (Time.time - startTime) * speed;

                    float fractionOfJourney = distCovered / distance;

                    helicopter.transform.position = Vector3.Lerp(helicopter.transform.position, endPos, fractionOfJourney);

                    if (helicopter.CenterPoint().y + 1 >= endPos.y - 2)
                    {
                        isTakeoff[helicopter.GetInstanceID()] = false;

                        yield break;
                    }

                    yield return null;
                }
            }
        }

        void PushMethod(BasePlayer player, MiniCopter helicopter)
        {
            if (helicopter != null)
            {
                Rigidbody rb = helicopter.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    float force;

                    if (helicopter.ShortPrefabName == "minicopter.entity") force = _config.minicopterPushForce;
                    else force = _config.scrapHelicopterPushForce;

                    rb.AddForce(Vector3.up * force, ForceMode.Acceleration);
                }

                isTakeoff[helicopter.GetInstanceID()] = false;
            }
            else
            {
                PrintToChat(player, lang.GetMessage("ErrorFound", this, player.UserIDString));
            }            
        }
    }

    #endregion
}
