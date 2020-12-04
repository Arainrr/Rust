using Rust;
using System.Linq;
using UnityEngine;

public class TriggerHurt : TriggerBase
{
	public float DamagePerSecond = 1f;

	public float DamageTickRate = 4f;

	public DamageType damageType;

	public override GameObject InterestedInObject(GameObject obj)
	{
		obj = base.InterestedInObject(obj);
		if (obj == null)
		{
			return null;
		}
		BaseEntity baseEntity = GameObjectEx.ToBaseEntity(obj);
		if (baseEntity == null)
		{
			return null;
		}
		if (baseEntity.isClient)
		{
			return null;
		}
		return baseEntity.gameObject;
	}

	public override void OnObjects()
	{
		InvokeRepeating(OnTick, 0f, 1f / DamageTickRate);
	}

	public override void OnEmpty()
	{
		CancelInvoke(OnTick);
	}

	private void OnTick()
	{
		BaseEntity attacker = GameObjectEx.ToBaseEntity(base.gameObject);
		if (entityContents == null)
		{
			return;
		}
		BaseEntity[] array = entityContents.ToArray();
		foreach (BaseEntity baseEntity in array)
		{
			if (BaseEntityEx.IsValid(baseEntity))
			{
				BaseCombatEntity baseCombatEntity = baseEntity as BaseCombatEntity;
				if (!(baseCombatEntity == null))
				{
					baseCombatEntity.Hurt(DamagePerSecond * (1f / DamageTickRate), damageType, attacker);
				}
			}
		}
	}
}
