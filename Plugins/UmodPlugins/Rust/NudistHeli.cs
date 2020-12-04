﻿using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Nudist Heli", "Panduck", "0.0.9")]
    [Description("Configurable helicopter engagement behaviour.")]
    public class NudistHeli : RustPlugin
    {
        private NudistHeliSettings settings;
        private Dictionary<BasePlayer, float> hostilePlayers;

        private void Init()
        {
            settings = Config.ReadObject<NudistHeliSettings>();
            hostilePlayers = new Dictionary<BasePlayer, float>();
        }

        private NudistHeliSettings GetDefaultConfig()
        {
            return new NudistHeliSettings()
            {
                MaxClothingCount = 3,
                HostileTime = 60,
                OnlyEngageOnWeaponHeld = false,
                RestrictedWeapons = new List<string>()
                {
                    "rifle.ak",
                    "rifle.bolt",
                    "smg.2",
                    "shotgun.double",
                    "rifle.l96",
                    "rifle.lr300",
                    "lmg.m249",
                    "rifle.m39",
                    "pistol.m92",
                    "smg.mp5",
                    "shotgun.pump",
                    "pistol.python",
                    "pistol.revolver",
                    "rocket.launcher",
                    "pistol.semiauto",
                    "rifle.semiauto",
                    "shotgun.spas12",
                    "smg.thompson"
                },
                DebugMessages = false
            };
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(GetDefaultConfig(), true);
        }

        private void SetHostility(BasePlayer player, bool hostility)
        {
            if (hostility)
            {
                if (!hostilePlayers.ContainsKey(player))
                {
                    if (settings.DebugMessages)
                    {
                        Puts($"Setting '{player.displayName}' to hostile.");
                    }

                    hostilePlayers.Add(player, Time.time);
                }
                else
                {
                    if (settings.DebugMessages)
                    {
                        Puts($"Resetting hostility for '{player.displayName}'.");
                    }

                    hostilePlayers[player] = Time.time;
                }
            }
            else
            {
                if (GetHostility(player))
                {
                    if (settings.DebugMessages)
                    {
                        Puts($"Removing hostility from '{player.displayName}'.");
                    }

                    hostilePlayers.Remove(player);
                }
            }
        }

        private bool GetHostility(BasePlayer player)
        {
            return hostilePlayers.ContainsKey(player);
        }

        private void UpdateHostility(BasePlayer player, PatrolHelicopterAI heli)
        {
            if (player.IsVisible(heli.transform.position, player.transform.position))
            {
                var clothing = player.inventory.containerWear.itemList;
                var belt = player.inventory.containerBelt.itemList;

                bool skipStep = false;

                if (clothing.Count > settings.MaxClothingCount)
                {
                    if (settings.DebugMessages)
                    {
                        Puts($"Detected restricted clothing count for '{player.displayName}'.");
                    }

                    SetHostility(player, true);
                    skipStep = true;
                }

                if (!skipStep)
                {
                    if (settings.OnlyEngageOnWeaponHeld)
                    {
                        var activeItem = player.GetActiveItem();

                        if (settings.RestrictedWeapons.Contains(activeItem.info.shortname))
                        {
                            if (settings.DebugMessages)
                            {
                                Puts($"Detected restricted held weapon for '{player.displayName}'.");
                            }

                            SetHostility(player, true);
                        }
                    }
                    else
                    {
                        foreach (var beltItem in belt)
                        {
                            if (settings.RestrictedWeapons.Contains(beltItem.info.shortname))
                            {
                                if (settings.DebugMessages)
                                {
                                    Puts($"Detected restricted belt weapon for '{player.displayName}'.");
                                }

                                SetHostility(player, true);
                            }
                        }
                    }
                }
            }

            if (GetHostility(player))
            {
                if ((Time.time - hostilePlayers[player]) >= settings.HostileTime)
                {
                    if (settings.DebugMessages)
                    {
                        Puts($"Forgetting about '{player.displayName}'.");
                    }

                    SetHostility(player, false);
                }
            }
        }

        private void ClearHostilities()
        {
            hostilePlayers.Clear();
        }

        private object CanHelicopterTarget(PatrolHelicopterAI heli, BasePlayer player)
        {
            UpdateHostility(player, heli);

            if(heli._targetList.Count > 0)
            {
                var group = heli._targetList.GroupBy(x => x.ply);
                var select = group.Select(x => x.First());

                heli._targetList = select.ToList();
            }

            return GetHostility(player);
        }

        void OnEntityKill(BaseNetworkable entity)
        {
            if(entity is BaseHelicopter)
            {
                if (settings.DebugMessages)
                {
                    Puts($"Helicopter destroyed, clearing hostilities.");
                }

                ClearHostilities();
            }
        }

        public class NudistHeliSettings
        {
            public int MaxClothingCount { get; set; }
            public float HostileTime { get; set; }
            public List<string> RestrictedWeapons { get; set; }
            public bool OnlyEngageOnWeaponHeld { get; set; }
            public bool DebugMessages { get; set; }
        }
    }
}
