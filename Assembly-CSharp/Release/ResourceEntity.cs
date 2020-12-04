using Facepunch;
using Oxide.Core;
using ProtoBuf;
using System;
using UnityEngine.Serialization;

public class ResourceEntity : BaseEntity
{
	[FormerlySerializedAs("health")]
	public float startHealth;

	[FormerlySerializedAs("protection")]
	public ProtectionProperties baseProtection;

	public float health;

	public ResourceDispenser resourceDispenser;

	[NonSerialized]
	protected bool isKilled;

	public override void Load(LoadInfo info)
	{
		base.Load(info);
		if (info.msg.resource != null)
		{
			health = info.msg.resource.health;
		}
	}

	public override void InitShared()
	{
		base.InitShared();
		if (base.isServer)
		{
			DecorComponent[] components = PrefabAttribute.server.FindAll<DecorComponent>(prefabID);
			DecorComponentEx.ApplyDecorComponentsScaleOnly(base.transform, components);
		}
	}

	public override void ServerInit()
	{
		base.ServerInit();
		resourceDispenser = GetComponent<ResourceDispenser>();
		if (health == 0f)
		{
			health = startHealth;
		}
	}

	public override void Save(SaveInfo info)
	{
		base.Save(info);
		if (info.forDisk)
		{
			info.msg.resource = Pool.Get<BaseResource>();
			info.msg.resource.health = Health();
		}
	}

	public override float MaxHealth()
	{
		return startHealth;
	}

	public override float Health()
	{
		return health;
	}

	protected virtual void OnHealthChanged()
	{
	}

	public override void OnAttacked(HitInfo info)
	{
		if (!base.isServer || isKilled || Interface.CallHook("OnEntityTakeDamage", this, info) != null)
		{
			return;
		}
		if (resourceDispenser != null)
		{
			resourceDispenser.OnAttacked(info);
		}
		if (!info.DidGather)
		{
			if ((bool)baseProtection)
			{
				baseProtection.Scale(info.damageTypes);
			}
			float num = info.damageTypes.Total();
			health -= num;
			if (health <= 0f)
			{
				OnKilled(info);
			}
			else
			{
				OnHealthChanged();
			}
		}
	}

	public virtual void OnKilled(HitInfo info)
	{
		isKilled = true;
		Interface.CallHook("OnEntityDeath", this, info);
		Kill();
	}

	public override float BoundsPadding()
	{
		return 1f;
	}
}
