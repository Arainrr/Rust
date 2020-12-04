using Oxide.Core;
using UnityEngine;

public class BaseTrapTrigger : TriggerBase
{
	public BaseTrap _trap;

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

	public override void OnObjectAdded(GameObject obj)
	{
		Interface.CallHook("OnTrapSnapped", this, obj);
		base.OnObjectAdded(obj);
		_trap.ObjectEntered(obj);
	}

	public override void OnEmpty()
	{
		base.OnEmpty();
		_trap.OnEmpty();
	}
}
