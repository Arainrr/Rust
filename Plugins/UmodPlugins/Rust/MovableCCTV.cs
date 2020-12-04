using System.Collections.Generic;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Movable CCTV", "Bazz3l", "1.0.7")]
    [Description("Player controllable cctv cameras using WASD")]
    class MovableCCTV : RustPlugin
    {
        #region Fields
        const string permUse = "movablecctv.use";
        const string panelName = "cctv_panel";

        public static MovableCCTV plugin;

        CuiElementContainer uiElements;
        CuiTextComponent textLabel;
        #endregion

        #region Config
        PluginConfig config;

        protected override void LoadDefaultConfig() => Config.WriteObject(GetDefaultConfig(), true);

        PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                RotateSpeed = 0.2f
            };
        }

        class PluginConfig
        {
            public float RotateSpeed;
        }
        #endregion

        #region Local
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string> {
                {"mounted", "Use WASD to move the camera."}
            }, this);
        }
        #endregion

        #region Oxide
        void OnServerInitialized()
        {
            permission.RegisterPermission(permUse, this);
            CheckMoveble();
            CreateUI();
        }

        void Init()
        {
            plugin = this;
            config = Config.ReadObject<PluginConfig>();
        }

        void Unload()
        {
            foreach (CameraMover obj in UnityEngine.Object.FindObjectsOfType<CameraMover>())
            {
                GameObject.Destroy(obj);
            }

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, panelName);
            }
        }

        void OnEntitySpawned(CCTV_RC cctv)
        {
            if (!cctv.IsStatic())
            {
                cctv.hasPTZ = true;
            }
        }

        void OnEntityMounted(ComputerStation station, BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, permUse))
            {
                return;
            }

            if (player.GetComponent<CameraMover>() == null)
            {
                player.gameObject.AddComponent<CameraMover>();
            }

            CuiHelper.AddUi(player, PlayerUI(player));
        }

        void OnEntityDismounted(ComputerStation station, BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, permUse))
            {
                return;
            }

            CameraMover cameraMover = player.GetComponent<CameraMover>();
            if (cameraMover == null)
            {
                return;
            }

            cameraMover.Destroy();

            CuiHelper.DestroyUi(player, panelName);
        }
        #endregion

        #region Core
        void CheckMoveble()
        {
            foreach(BaseEntity entity in BaseNetworkable.serverEntities)
            {
                CCTV_RC cctv = entity.GetComponent<CCTV_RC>();
                if (cctv != null && !cctv.IsStatic())
                {
                    cctv.hasPTZ = true;
                }
            }
        }

        class CameraMover : MonoBehaviour
        {
            ComputerStation station;
            BasePlayer player;

            void Awake()
            {
                player = GetComponent<BasePlayer>();
                if (player == null)
                {
                    Destroy();
                    return;
                }

                station = player?.GetMounted() as ComputerStation;
            }

            void FixedUpdate()
            {
                if (station == null || !station.currentlyControllingEnt.IsValid(true))
                {
                    return;
                }

                CCTV_RC cctv = station.currentlyControllingEnt.Get(true).GetComponent<CCTV_RC>();
                if (cctv == null || cctv.IsStatic())
                {
                    return;
                }

                float y = player.serverInput.IsDown(BUTTON.FORWARD) ? 1f : (player.serverInput.IsDown(BUTTON.BACKWARD) ? -1f : 0f);
                float x = player.serverInput.IsDown(BUTTON.LEFT) ? -1f : (player.serverInput.IsDown(BUTTON.RIGHT) ? 1f : 0f);

                InputState inputState = new InputState();
                inputState.current.mouseDelta.y = y * plugin.config.RotateSpeed;
                inputState.current.mouseDelta.x = x * plugin.config.RotateSpeed;

                cctv.UserInput(inputState, player);
            }

            public void Destroy() => Destroy(this);
        }
        #endregion

        #region UI
        void CreateUI()
        {
            uiElements = new CuiElementContainer();

            string panel = uiElements.Add(new CuiPanel {
                CursorEnabled = true,
                Image = {
                    Color = "0 0 0 0"
                },
                RectTransform = {
                    AnchorMin = "0.293 0.903",
                    AnchorMax = "0.684 0.951"
                }
            }, "Overlay", panelName);

            CuiLabel label = new CuiLabel
            {
                Text = {
                    Text  = "",
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.5",
                    FontSize = 14
                },
                RectTransform = {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            };

            textLabel = label.Text;

            uiElements.Add(label, panel);
        }

        CuiElementContainer PlayerUI(BasePlayer player)
        {
            textLabel.Text = Lang("mounted", player.UserIDString);

            return uiElements;
        }
        #endregion

        #region Helpers
        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        #endregion
    }
}