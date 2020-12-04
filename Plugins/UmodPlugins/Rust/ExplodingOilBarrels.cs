using System.Collections.Generic; 
using Rust;
using UnityEngine;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Exploding Oil Barrel", "Bazz3l", "1.0.7")]
    [Description("Exploding oil barrels with explosion force, player damage and ground shake effect")]
    class ExplodingOilBarrels : RustPlugin
    {
        #region Fields
        const string _explosionEffect = "assets/bundled/prefabs/fx/explosions/explosion_03.prefab";
        const string _fireEffect = "assets/bundled/prefabs/fx/gas_explosion_small.prefab"; 
        const string _shakeEffect = "assets/prefabs/weapons/thompson/effects/attack_shake.prefab";

        PluginConfig _config;
        #endregion

        #region Config
        protected override void LoadDefaultConfig() => Config.WriteObject(GetDefaultConfig(), true);

        PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                EnableShakeScreen = true,
                EnableExplosionForce = true,
                EnablePlayerDamage = true,
                PlayerDamageDistance = 2f,
                PlayerDamage = 10f,
                ShakeDistance = 20f,
                ExplosionItemDistance = 20f,
                ExplosionRange = 50f,
                ExplosionForce = 50f
            };
        }

        class PluginConfig
        {
            [JsonProperty(PropertyName = "Screen shake effect for explosion")]
            public bool EnableShakeScreen;

            [JsonProperty(PropertyName = "Moves items in range of explosion")]
            public bool EnableExplosionForce;

            [JsonProperty(PropertyName = "Deal damage to players in distance of explosion")]
            public bool EnablePlayerDamage;

            [JsonProperty(PropertyName = "ditance to deal damage to players")]
            public float PlayerDamageDistance;

            [JsonProperty(PropertyName = "amount of damage delt to players in range")]
            public float PlayerDamage;

            [JsonProperty(PropertyName = "distance shake will effect players from explosion")]
            public float ShakeDistance;

            [JsonProperty(PropertyName = "amount of force delt to object in range")]
            public float ExplosionForce;

            [JsonProperty(PropertyName = "distance to find object near explosion")]
            public float ExplosionItemDistance;

            [JsonProperty(PropertyName = "distance to find targets near explosion")]
            public float ExplosionRange;
        }
        #endregion 

        #region Oxide
        void Init()
        {
            _config = Config.ReadObject<PluginConfig>();
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return;

            if (!(entity.ShortPrefabName == "oil_barrel" && info.damageTypes.GetMajorityDamageType() == DamageType.Bullet)) return;

            PlayExplosion(entity.transform.position);
            PlayerInRange(entity.transform.position);

            if (_config.EnableExplosionForce)
            {
                MoveItems(entity.transform.position);
            }
        }
        #endregion

        #region Core
        void PlayExplosion(Vector3 position)
        {
            Effect.server.Run(_explosionEffect, position, Vector3.zero, null, false);
            Effect.server.Run(_fireEffect, position, Vector3.zero, null, false);
        }

        void MoveItems(Vector3 position)
        {
            List<DroppedItem> droppedItems = new List<DroppedItem>();

            Vis.Entities<DroppedItem>(position, _config.ExplosionItemDistance, droppedItems);

            droppedItems.RemoveAll(item => item == null || item.IsDestroyed || !item.IsVisible(position));

            foreach(DroppedItem item in droppedItems)
            {
                item.GetComponent<Rigidbody>()?.AddExplosionForce(_config.ExplosionForce, position, _config.ExplosionItemDistance);
            }
        }

        void PlayerInRange(Vector3 position)
        {
            List<BasePlayer> players = new List<BasePlayer>();

            Vis.Entities<BasePlayer>(position, _config.ExplosionRange, players);

            foreach(BasePlayer player in players)
            {
                if (_config.EnablePlayerDamage && Vector3.Distance(player.transform.position, position) <= _config.PlayerDamageDistance)
                {
                    DamagePlayer(player, position);
                }

                if (_config.EnableShakeScreen && Vector3.Distance(player.transform.position, position) <= _config.ShakeDistance)
                {
                    PlayerShake(player);
                }
            }
        }

        void DamagePlayer(BasePlayer player, Vector3 position) => player.Hurt(_config.PlayerDamage);

        void PlayerShake(BasePlayer player)
        {
            for (int i = 0; i < 10; i++)
            {
                Effect.server.Run(_shakeEffect, player.transform.position);
            }
        }
        #endregion
    }
}
