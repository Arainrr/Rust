using System.Collections.Generic;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Custom Death Messages", "Mr. Blue", "2.0.7")]
    [Description("Displays custom death messages")]
    class CustomDeathMessages : HurtworldPlugin
    {
        [PluginReference]
        private Plugin KillCounter;

        private void OnServerInitialized()
        {
            GameManager.Instance.ServerConfig.ChatDeathMessagesEnabled = false;
        }

        private void Unload()
        {
            GameManager.Instance.ServerConfig.ChatDeathMessagesEnabled = true;
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string> {
                { "Creatures/Antor", "{Name} got killed by a Antor" },
                { "Creatures/Bandrill", "{Name} got killed by a Bandrill" },
                { "Creatures/Bor", "{Name} got killed by a Bor" },
                { "Creatures/DartBug", "{Name} got killed by a Dart Bug" },
                { "Creatures/Radiation Bor", "{Name} got killed by a Radiation Bor" },
                { "Creatures/Rafaga", "{Name} got killed by a Rafaga" },
                { "Creatures/Sabra", "{Name} got killed by a Sabra" },
                { "Creatures/Sasquatch", "{Name} got killed by a Sasquatch" },
                { "Creatures/Skoogler", "{Name} got killed by a Skoogler" },
                { "Creatures/Shigi", "{Name} got killed by a Shigi" },
                { "Creatures/Tokar", "{Name} got killed by a Tokar" },
                { "Creatures/Thornling", "{Name} got killed by a Thornling" },
                { "Creatures/Yeti", "{Name} got killed by a Yeti" },
                { "EntityStats/BinaryEffects/Asphyxiation", "{Name} has died from suffocation" },
                { "EntityStats/BinaryEffects/Burning", "{Name} has burned to death" },
                { "EntityStats/BinaryEffects/Drowning", "{Name} has drowned" },
                { "EntityStats/BinaryEffects/Hyperthermia", "{Name} has died from overheating" },
                { "EntityStats/BinaryEffects/Hypothermia", "{Name} has frozen to death" },
                { "EntityStats/BinaryEffects/Radiation Poisoning", "{Name} has died from radiation poisoning" },
                { "EntityStats/BinaryEffects/Starvation", "{Name} has starved to death" },
                { "EntityStats/BinaryEffects/Starving", "{Name} has starved to death" },
                { "EntityStats/BinaryEffects/Territory Control Lockout Damage", "{Name} got killed by Territory Control Lockout Damage" },
                { "EntityStats/Sources/Damage Over Time", "{Name} just died" },
                { "EntityStats/Sources/Explosives", "{Name} got killed by an explosion" },
                { "EntityStats/Sources/Fall Damage", "{Name} has fallen to their death" },
                { "EntityStats/Sources/Poison", "{Name} has died from poisoning" },
                { "EntityStats/Sources/Radiation", "{Name} has died from radiation" },
                { "EntityStats/Sources/Suicide", "{Name} has committed suicide" },
                { "EntityStats/Sources/a Vehicle Impact", "{Name} got run over by a vehicle" },
                { "Machines/Landmine", "{Name} got killed by a Landmine" },
                { "Machines/Medusa Vine", "{Name} got killed by a Medusa Trap" },
                { "Too Cold", "{Name} has frozen to death" },
                { "Unknown", "{Name} just died on a mystic way" },
                { "killcounter_player", "{Name} got killed by {Killer}[{Kills}] with a {Weapon} over {Distance} meters" },
                { "unknown weapon", "unknown weapon" },
                { "player", "{Name} got killed by {Killer} with a {Weapon} over {Distance} meters" }
            }, this);
        }

        private void SendMessage(string key, string name, string killerName = "", string distance = "", string weapon = "", string killerKills = "")
        {
            foreach (PlayerSession s in GameManager.Instance.GetSessions().Values)
            {
                Player.Message(s, lang.GetMessage(key, this, s.SteamId.ToString())
                    .Replace("{Name}", name)
                    .Replace("{Killer}", killerName)
                    .Replace("{Distance}", distance)
                    .Replace("{Weapon}", weapon)
                    .Replace("{Kills}", killerKills));
            }
        }

        private PlayerSession GetPlayerSession(EntityEffectSourceData dataSource)
        {
            if (dataSource?.EntitySource?.GetComponent<EntityStats>()?.networkView == null) return null;
            HNetworkView networkView = dataSource.EntitySource.GetComponent<EntityStats>().networkView;
            return GameManager.Instance.GetSession(networkView.owner);
        }

        private void OnPlayerDeath(PlayerSession playerSession, EntityEffectSourceData dataSource)
        {
            string name = playerSession.Identity.Name;
            string SDKey = !string.IsNullOrEmpty(dataSource.SourceDescriptionKey) ? dataSource.SourceDescriptionKey : Singleton<GameManager>.Instance.GetDescriptionKey(dataSource.EntitySource);

            if (SDKey.EndsWith("(P)"))
            {
                PlayerSession killerSession = GetPlayerSession(dataSource);
                if (killerSession == null) return;
                string killerName = killerSession.Identity.Name;

                string weapon;
                if (killerSession?.WorldPlayerEntity?.GetComponent<EquippedHandlerBase>()?.EquipSession?.RootItem?.Generator?.name != null)
                {
                    weapon = killerSession.WorldPlayerEntity.GetComponent<EquippedHandlerBase>().EquipSession.RootItem.Generator.name.ToString();
                }
                else
                {
                    weapon = lang.GetMessage("unknown weapon", this);
                }

                string distance = Mathf.Round(Vector3.Distance(playerSession.WorldPlayerEntity.transform.position, killerSession.WorldPlayerEntity.transform.position)).ToString();

                if (KillCounter != null)
                {
                    var KillerKills = KillCounter.Call("AddKill", playerSession, dataSource);
                    SendMessage("killcounter_player", name, killerName, distance, weapon, (KillerKills ?? "?").ToString());
                }
                else
                {
                    SendMessage("player", name, killerName, distance, weapon);
                }
            }
            else
            {
                if (lang.GetMessage(SDKey, this) == SDKey)
                {
                    SendMessage("Unknown", name);
                    Puts("Found unknown SourceDescriptionKey: " + SDKey);
                }
                else
                {
                    SendMessage(SDKey, name);
                }
            }
        }
    }
}