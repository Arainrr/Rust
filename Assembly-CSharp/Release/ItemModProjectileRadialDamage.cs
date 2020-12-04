using Facepunch;
using Rust;
using System.Collections.Generic;
using UnityEngine;

public class ItemModProjectileRadialDamage : ItemModProjectileMod
{
	public float radius = 0.5f;

	public DamageTypeEntry damage;

	public GameObjectRef effect;

	public bool ignoreHitObject = true;

	public override void ServerProjectileHit(HitInfo info)
	{
		if (effect.isValid)
		{
			Effect.server.Run(effect.resourcePath, info.HitPositionWorld, info.HitNormalWorld);
		}
		List<BaseCombatEntity> obj = Pool.GetList<BaseCombatEntity>();
		List<BaseCombatEntity> obj2 = Pool.GetList<BaseCombatEntity>();
		Vis.Entities(info.HitPositionWorld, radius, obj2, 1236478737);
		foreach (BaseCombatEntity item in obj2)
		{
			if (item.isServer && !obj.Contains(item) && (!(item == info.HitEntity) || !ignoreHitObject))
			{
				Vector3 a = item.ClosestPoint(info.HitPositionWorld);
				float num = Vector3.Distance(a, info.HitPositionWorld) / radius;
				if (!(num > 1f))
				{
					float num2 = 1f - num;
					if (item.IsVisible(info.HitPositionWorld - info.ProjectileVelocity.normalized * 0.1f) && item.IsVisible(info.HitPositionWorld - (a - info.HitPositionWorld).normalized * 0.1f))
					{
						obj.Add(item);
						item.OnAttacked(new HitInfo(info.Initiator, item, damage.type, damage.amount * num2));
					}
				}
			}
		}
		Pool.FreeList(ref obj);
		Pool.FreeList(ref obj2);
	}
}
