﻿using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace Oxide.Plugins
{
    [Info("HW Fly", "klauz24", "1.2.2"), Description("Allows players with permissions to fly around.")]
    internal class HWFly : HurtworldPlugin
    {
        private Dictionary<ulong, float> _flying = new Dictionary<ulong, float>();

        private const float _defaultFlySpeed = 75.0f;

        private const string _perm = "hwfly.allowed";

        private void Init() => permission.RegisterPermission(_perm, this);

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["HW Fly - Prefix"] = "<color=lightblue>[HW Fly]</color>",
                ["HW Fly - No perm"] = "You got no permission to use this command.",
                ["HW Fly - Enabled"] = "Fly mode enabled.",
                ["HW Fly - Disabled"] = "Fly mode disabled.",
                ["HW Fly - Custom speed"] = "Fly speed set to {0}.",
            }, this);
        }

        [ChatCommand("fly")]
        private void FlyCommand(PlayerSession session, string command, string[] args)
        {
            if (!permission.UserHasPermission(GetSessionIdString(session), _perm) || !session.IsAdmin)
            {
                Msg(session, Lang(session, "HW Fly - Prefix"), Lang(session, "HW Fly - No perm"));
                return;
            }
            if (args.Length == 0)
            {
                if (_flying.ContainsKey(GetSessionIdUlong(session)))
                {
                    _flying.Remove(GetSessionIdUlong(session));
                    Msg(session, Lang(session, "HW Fly - Prefix"), Lang(session, "HW Fly - Disabled"));
                }
                else
                {
                    _flying.Add(GetSessionIdUlong(session), _defaultFlySpeed);
                    ResetStats(session);
                    Msg(session, Lang(session, "HW Fly - Prefix"), Lang(session, "HW Fly - Enabled"));
                }
            }
            if (args.Length >= 1)
            {
                float speed;
                if (float.TryParse(args[0], out speed))
                {
                    if (_flying.ContainsKey(GetSessionIdUlong(session)))
                    {
                        _flying[GetSessionIdUlong(session)] = speed;
                        Msg(session, Lang(session, "HW Fly - Prefix"), string.Format(Lang(session, "HW Fly - Custom speed"), speed));
                    }
                    else
                    {
                        _flying.Add(GetSessionIdUlong(session), speed);
                        Msg(session, Lang(session, "HW Fly - Prefix"), Lang(session, "HW Fly - Enabled"));
                        Msg(session, Lang(session, "HW Fly - Prefix"), string.Format(Lang(session, "HW Fly - Custom speed"), speed));
                    }
                }
            }
        }

        private void ResetStats(PlayerSession session)
        {
            EntityStats stats = session.WorldPlayerEntity.GetComponent<EntityStats>();
            foreach (KeyValuePair<EntityFluidEffectKey, IEntityFluidEffect> effect in stats.GetFluidEffects())
            {
                effect.Value.Reset(true);
            }
        }

        private void OnPlayerInput(PlayerSession session, InputControls input)
        {
            if (_flying.ContainsKey(GetSessionIdUlong(session)))
            {
                CharacterMotorSimple motor = session.WorldPlayerEntity.GetComponent<CharacterMotorSimple>();
                Vector3 direction = new Vector3(0f, 0f, 0f);
                float speed = _flying[GetSessionIdUlong(session)];
                if (input.Forward)
                {
                    direction = input.DirectionVector * speed;
                }
                if (input.Backward)
                {
                    direction = input.DirectionVector * -speed;
                }
                motor.IsGrounded = true;
                motor.Set_currentVelocity(direction.normalized * _flying[GetSessionIdUlong(session)]);
                ResetStats(session);
            }
        }

        private ulong GetSessionIdUlong(PlayerSession session)
        {
            return session.SteamId.m_SteamID;
        }

        private string GetSessionIdString(PlayerSession session)
        {
            return session.SteamId.ToString();
        }

        private string Lang(PlayerSession session, string key)
        {
            return lang.GetMessage(key, this, session.SteamId.ToString());
        }

        private void Msg(PlayerSession session, string prefix, string message)
        {
            hurt.SendChatMessage(session, prefix, message);
        }
    }
}