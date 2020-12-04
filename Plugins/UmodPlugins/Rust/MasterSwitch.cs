using Facepunch;
using System;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;

namespace Oxide.Plugins
{
    [Info("Master Switch", "Lincoln", "1.0.2")]
    [Description("Toggle things on or off with a command.")]
    public class MasterSwitch : RustPlugin
    {
        private readonly float maxValue = 500;
        private readonly float minValue = 0f;
        private float radius;
        private string myArgs;
        private const string permUse = "MasterSwitch.use";
        #region Permissions

        private void Init()
        {
            permission.RegisterPermission(permUse, this);
        }
        #endregion

        #region Checks
        private bool HasPermission(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, permUse))
            {
                player.ChatMessage(lang.GetMessage("NoPerm", this, player.UserIDString));
                return false;
            }

            else
            {
                return true;
            }
        }
        private bool HasArgs(BasePlayer player, string[] args)
        {
            Int64 numArg;
            try
            {
                Int64.TryParse(args[1], out numArg);
                Puts(numArg.ToString());
                if (args == null || args.Length == 0)
                {
                    player.ChatMessage(lang.GetMessage("Syntax", this, player.UserIDString));
                    return false;
                }
                return true;
            }
            catch
            {
                player.ChatMessage(lang.GetMessage("Syntax", this, player.UserIDString));
                return false;
            }
        }
        private bool HasRadius(BasePlayer player, float radius)
        {
            if (radius <= minValue || radius > maxValue || radius == 0)
            {
                player.ChatMessage(lang.GetMessage("Radius", this, player.UserIDString));
                return false;
            }
            else
            {
                return true;
            }
        }
        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["LightTurnOn"] = "<color=#ffc34d>MasterSwitch</color>: {0} Lights turned on within a {1}f radius.",
                ["LightTurnOff"] = "<color=#ffc34d>MasterSwitch</color>: {0} Lights turned off within a {1}f radius.",
                ["DoorOpen"] = "<color=#ffc34d>MasterSwitch</color>: {0} Doors opened within a {1}f radius.",
                ["DoorClose"] = "<color=#ffc34d>MasterSwitch</color>: {0} Doors closed within a {1}f radius.",
                ["TurretStart"] = "<color=#ffc34d>MasterSwitch</color>: {0} Turrets started within a {1}f radius.",
                ["TurretStop"] = "<color=#ffc34d>MasterSwitch</color>: {0} Turrets stopped within a {1}f radius.",
                ["BearTrapArm"] = "<color=#ffc34d>MasterSwitch</color>: {0} Bear traps have been armed within a {1}f radius.",
                ["BearTrapDisArm"] = "<color=#ffc34d>MasterSwitch</color>: {0} Bear traps have been disarmed within a {1}f radius.",
                ["MineExplode"] = "<color=#ffc34d>MasterSwitch</color>: {0} Mines have exploded within a {1}f radius.",
                ["IgnitableLit"] = "<color=#ffc34d>MasterSwitch</color>: {0} Ignitables have been lit within a {1}f radius.",
                ["MachinesOn"] = "<color=#ffc34d>MasterSwitch</color>: {0} fog/snow machines activated within a {1}f radius.",
                ["MachinesOff"] = "<color=#ffc34d>MasterSwitch</color>: {0} fog/snow machines de-activated within a {1}f radius.",
                ["NoPerm"] = "<color=#ffc34d>MasterSwitch</color>: You do not have permissions to use this.",
                ["Syntax"] = "<color=#ffc34d>MasterSwitch</color>: Incorrect syntax. Example /ms <command> <radius>",
                ["Radius"] = "<color=#ffc34d>MasterSwitch</color>: Radius out of bounds, choose 1 - 1000",

            }, this);
        }
        #endregion

        #region Unity

        //Creates a base entity list
        private List<BaseEntity> FindBaseEntity(Vector3 pos, float radius)
        {
            var hits = Physics.SphereCastAll(pos, radius, Vector3.up);
            var x = new List<BaseEntity>();
            foreach (var hit in hits)
            {
                var entity = hit.GetEntity()?.GetComponent<BaseEntity>();
                if (entity && !x.Contains(entity))
                    x.Add(entity);
            }

            return x;
        }

        #endregion

        #region Commands
        [ChatCommand("ms")]
        private void ToggleCommand(BasePlayer player, string command, string[] args)
        {

            if (player == null || args.IsNullOrEmpty() || !HasPermission(player) || (!HasArgs(player, args))) return;

            else
            {
                try
                {
                    radius = Convert.ToSingle(args[1]);
                }
                catch
                {
                    return;
                }


                if (!HasRadius(player, radius))
                {
                    return;
                }
                var baseEntityList = FindBaseEntity(player.transform.position, radius);
                List<string> entityCount = new List<string>();

                switch (args[0])
                {
                    case "open":
                        foreach (var entity in baseEntityList)
                        {
                            var x = entity as Door;

                            if (x is Door)
                            {
                                if (x.IsOpen()) continue;
                                entityCount.Add(x.ToString());
                                x.SetOpen(true);
                            }
                        }
                        var message = string.Format(lang.GetMessage("DoorOpen", this, player.UserIDString), entityCount.Count, args[1]);
                        player.ChatMessage(message);
                        break;

                    case "close":
                        foreach (var entity in baseEntityList)
                        {
                            var x = entity as Door;

                            if (x is Door)
                            {
                                if (!x.IsOpen()) continue;
                                entityCount.Add(x.ToString());
                                x.SetOpen(false);
                            }
                        }
                        message = string.Format(lang.GetMessage("DoorClose", this, player.UserIDString), entityCount.Count, args[1]);
                        player.ChatMessage(message);
                        break;

                    case "on":
                        foreach (var entity in baseEntityList)
                        {
                            var a = entity as BaseOven;
                            var b = entity as SirenLight;
                            var c = entity as CeilingLight;
                            var d = entity as SearchLight;
                            var e = entity as Candle;
                            var f = entity as AdvancedChristmasLights;
                            var g = entity as FlasherLight;
                            var h = entity as SimpleLight;
                            var i = entity as ElectricalHeater;
                            var j = entity as FuelGenerator;

                            if (a is BaseOven)
                            {
                                if (a.IsOn()) continue;
                                entityCount.Add(a.ToString());
                                a.StartCooking();
                            }
                            if (b is SirenLight)
                            {
                                if (b.IsPowered()) continue;
                                entityCount.Add(b.ToString());
                                b.SetFlag(BaseEntity.Flags.Reserved8, true, true, true);
                            }
                            if (c is CeilingLight)
                            {
                                if (c.IsOn()) continue;
                                entityCount.Add(c.ToString());
                                c.SetFlag(BaseEntity.Flags.On, true);
                            }
                            if (d is SearchLight)
                            {
                                if (d.IsPowered()) continue;
                                entityCount.Add(d.ToString());
                                d.SetFlag(BaseEntity.Flags.Reserved8, true, false, true);
                                d.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                            }
                            if (e is Candle)
                            {
                                if (e.IsOn()) continue;
                                entityCount.Add(e.ToString());
                                e.SetFlag(BaseEntity.Flags.On, true);
                            }
                            if (f is AdvancedChristmasLights)
                            {
                                if (f.IsPowered()) continue;
                                f.SetFlag(AdvancedChristmasLights.Flags.Reserved8, true, false, true);
                                entityCount.Add(f.ToString());
                                f.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                            }
                            if (g is FlasherLight)
                            {
                                if (g.IsPowered() || g.IsOn()) continue;
                                g.SetFlag(FlasherLight.Flags.Reserved8, true, false, true);
                                g.SetFlag(FlasherLight.Flags.On, true, false, true);
                                entityCount.Add(g.ToString());
                                g.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                            }
                            if (h is SimpleLight)
                            {
                                if (h.IsOn()) continue;
                                h.SetFlag(BaseEntity.Flags.On, true, true);
                                entityCount.Add(h.ToString());
                                h.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                            }
                            if (i is ElectricalHeater)
                            {
                                if (i.IsPowered()) continue;
                                entityCount.Add(i.ToString());
                                i.SetFlag(BaseEntity.Flags.Reserved8, true, false, true);
                                i.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                            }
                            if (j is FuelGenerator)
                            {
                                if (j.IsOn()) continue;
                                j.currentEnergy = 40;
                                j.SetFlag(BaseEntity.Flags.On, true, true);
                                entityCount.Add(j.ToString());
                                j.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                            }
                        }
                        message = string.Format(lang.GetMessage("LightTurnOn", this, player.UserIDString), entityCount.Count, args[1]);
                        player.ChatMessage(message);
                        break;

                    case "off":
                        foreach (var entity in baseEntityList)
                        {
                            var a = entity as BaseOven;
                            var b = entity as SirenLight;
                            var c = entity as CeilingLight;
                            var d = entity as SearchLight;
                            var e = entity as Candle;
                            var f = entity as AdvancedChristmasLights;
                            var g = entity as FlasherLight;
                            var h = entity as SimpleLight;
                            var i = entity as ElectricalHeater;
                            var j = entity as FuelGenerator;

                            if (a is BaseOven)
                            {
                                if (!a.IsOn()) continue;
                                entityCount.Add(a.ToString());
                                a.StopCooking();
                            }
                            if (b is SirenLight)
                            {
                                if (!b.IsPowered()) continue;
                                entityCount.Add(b.ToString());
                                b.SetFlag(BaseEntity.Flags.Reserved8, false, true, true);
                            }
                            if (c is CeilingLight)
                            {
                                if (!c.IsOn()) continue;
                                entityCount.Add(c.ToString());
                                c.SetFlag(BaseEntity.Flags.On, false);
                            }
                            if (d is SearchLight)
                            {
                                if (!d.IsPowered()) continue;
                                entityCount.Add(d.ToString());
                                d.SetFlag(BaseEntity.Flags.Reserved8, false, false, true);
                                d.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                            }
                            if (e is Candle)
                            {
                                if (!e.IsOn()) continue;
                                entityCount.Add(e.ToString());
                                e.SetFlag(BaseEntity.Flags.On, false);
                            }
                            if (f is AdvancedChristmasLights)
                            {
                                if (!f.IsPowered()) continue;
                                f.SetFlag(AdvancedChristmasLights.Flags.Reserved8, false, false, true);
                                entityCount.Add(f.ToString());
                                f.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                            }
                            if (g is FlasherLight)
                            {
                                if (!g.IsPowered() || !g.IsOn()) continue;
                                g.SetFlag(FlasherLight.Flags.Reserved8, false, false, true);
                                g.SetFlag(FlasherLight.Flags.On, false, false, true);
                                entityCount.Add(g.ToString());
                                g.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                            }
                            if (h is SimpleLight)
                            {
                                if (!h.IsOn()) continue;
                                h.SetFlag(BaseEntity.Flags.On, false, true);
                                entityCount.Add(h.ToString());
                                h.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                            }
                            if (i is ElectricalHeater)
                            {
                                if (!i.IsPowered()) continue;
                                entityCount.Add(i.ToString());
                                i.SetFlag(BaseEntity.Flags.Reserved8, false, false, false);
                                i.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                            }
                            if (j is FuelGenerator)
                            {
                                if (!j.IsOn()) continue;
                                j.currentEnergy = -1;
                                j.SetFlag(BaseEntity.Flags.On, false, true);
                                entityCount.Add(j.ToString());
                                j.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);

                            }
                        }

                        message = string.Format(lang.GetMessage("LightTurnOff", this, player.UserIDString), entityCount.Count, args[1]);
                        player.ChatMessage(message);
                        break;
                    case "start":
                        foreach (var entity in baseEntityList)
                        {
                            var x = entity as AutoTurret;

                            if (x is AutoTurret)
                            {
                                if (x.IsOn()) continue;
                                entityCount.Add(x.ToString());
                                x.InitiateStartup();
                            }
                        }
                        message = string.Format(lang.GetMessage("TurretStart", this, player.UserIDString), entityCount.Count, args[1]);
                        player.ChatMessage(message);
                        break;

                    case "stop":
                        foreach (var entity in baseEntityList)
                        {
                            var x = entity as AutoTurret;

                            if (x is AutoTurret)
                            {
                                if (!x.IsOn()) continue;
                                entityCount.Add(x.ToString());
                                x.InitiateShutdown();
                            }
                        }
                        message = string.Format(lang.GetMessage("TurretStop", this, player.UserIDString), entityCount.Count, args[1]);
                        player.ChatMessage(message);
                        break;

                    case "arm":
                        foreach (var entity in baseEntityList)
                        {
                            var x = entity as BearTrap;

                            if (x is BearTrap)
                            {
                                if (x.IsOn()) continue;
                                entityCount.Add(x.ToString());
                                x.Arm();
                            }
                        }
                        message = string.Format(lang.GetMessage("BearTrapArm", this, player.UserIDString), entityCount.Count, args[1]);
                        player.ChatMessage(message);
                        break;

                    case "disarm":
                        foreach (var entity in baseEntityList)
                        {
                            var x = entity as BearTrap;

                            if (x is BearTrap)
                            {
                                if (!x.IsOn()) continue;
                                entityCount.Add(x.ToString());
                                x.Fire();
                            }
                        }
                        message = string.Format(lang.GetMessage("BearTrapDisArm", this, player.UserIDString), entityCount.Count, args[1]);
                        player.ChatMessage(message);
                        break;

                    case "ignite":
                        foreach (var entity in baseEntityList)
                        {
                            var x = entity as BaseFirework;
                            var y = entity as Igniter;

                            if (x is BaseFirework)
                            {
                                if (x.IsLit() || x.IsOn()) continue;
                                entityCount.Add(x.ToString());
                                x.SetFlag(BaseEntity.Flags.OnFire, true, false, true);
                                x.Invoke(new Action(x.Begin), x.fuseLength);
                            }
                            if (y is Igniter)
                            {
                                entityCount.Add(y.ToString());
                                y.IgniteRange = 5f;
                                y.IgniteStartDelay = 0;
                                y.UpdateHasPower(1, 1);
                                y.SetFlag(BaseEntity.Flags.Reserved8, true, true, true);
                            }
                        }
                        message = string.Format(lang.GetMessage("IgnitableLit", this, player.UserIDString), entityCount.Count, args[1]);
                        player.ChatMessage(message);
                        break;

                    case "explode":
                        foreach (var entity in baseEntityList)
                        {
                            var x = entity as Landmine;

                            if (x is Landmine)
                            {
                                entityCount.Add(x.ToString());
                                x.Explode();
                            }
                        }
                        message = string.Format(lang.GetMessage("MineExplode", this, player.UserIDString), entityCount.Count, args[1]);
                        player.ChatMessage(message);
                        break;

                    case "activate":
                        foreach (var entity in baseEntityList)
                        {
                            var x = entity as FogMachine;
                            var y = entity as SnowMachine;

                            if (x is FogMachine)
                            {
                                if (x.IsOn()) continue;
                                x.EnableFogField();
                                x.StartFogging();
                                x.SetFlag(BaseEntity.Flags.On, true);
                                entityCount.Add(x.ToString());
                            }
                            if (y is SnowMachine)
                            {
                                if (x.IsOn()) continue;
                                y.EnableFogField();
                                y.StartFogging();
                                y.SetFlag(BaseEntity.Flags.On, true);
                                entityCount.Add(y.ToString());
                            }
                        }
                        message = string.Format(lang.GetMessage("MachinesOn", this, player.UserIDString), entityCount.Count, args[1]);
                        player.ChatMessage(message);
                        break;

                    case "deactivate":
                        foreach (var entity in baseEntityList)
                        {
                            var x = entity as FogMachine;
                            var y = entity as SnowMachine;

                            if (x is FogMachine)
                            {
                                if (!x.IsOn()) continue;
                                x.FinishFogging();
                                x.DisableNozzle();
                                x.SetFlag(BaseEntity.Flags.On, false);
                                entityCount.Add(x.ToString());
                            }
                            if (y is SnowMachine)
                            {
                                if (!x.IsOn()) continue;
                                y.FinishFogging();
                                y.DisableNozzle();
                                y.SetFlag(BaseEntity.Flags.On, false);
                                entityCount.Add(y.ToString());
                            }
                        }
                        message = string.Format(lang.GetMessage("MachinesOff", this, player.UserIDString), entityCount.Count, args[1]);
                        player.ChatMessage(message);
                        break;

                    default:

                        player.ChatMessage("Not a valid command type");

                        break;
                }
            }
            return;
        }
        #endregion
    }
}