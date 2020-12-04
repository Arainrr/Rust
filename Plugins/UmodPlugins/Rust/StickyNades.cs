using UnityEngine;
using Rust;

namespace Oxide.Plugins
{
    [Info("Sticky Nades", "Bazz3l", "1.0.5")]
    [Description("Ability to throw nades at people and stick to them.")]
    class StickyNades : RustPlugin
    {
        #region Fields
        const string _permUse = "stickynades.use";
        #endregion

        #region Oxide
        void Init()
        {
            permission.RegisterPermission(_permUse, this);
        }

        void OnExplosiveThrown(BasePlayer player, BaseEntity entity, ThrownWeapon item)
        {
            if (!permission.UserHasPermission(player.UserIDString, _permUse))
            {
                return;
            }

            if (!(entity.ShortPrefabName.Contains("f1.deployed") || entity.ShortPrefabName.Contains("beancan.deployed")))
            {
                return;
            }

            entity.gameObject.AddComponent<StickyComponent>().player = player;
        }
        #endregion

        #region Components
        class StickyComponent : MonoBehaviour
        {
            BaseEntity _entity;
            public BasePlayer player;

            void Awake()
            {
                _entity = GetComponent<BaseEntity>();

                if (_entity == null)
                {
                    Destroy(this);
                    return;
                }

                gameObject.layer = (int) Layer.Reserved1;

                SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
                sphere.radius = 0.1f;
            }

            void OnCollisionEnter(Collision collision)
            {
                BasePlayer target = collision.gameObject.GetComponent<BasePlayer>();
                if (target != null && target != player)
                {
                    _entity.SetParent(target);
                    _entity.transform.localPosition = new Vector3(0f, 1f, 0f);

                    Destroy(_entity.GetComponent<Rigidbody>());
                }
            }
        }
        #endregion
    }
}