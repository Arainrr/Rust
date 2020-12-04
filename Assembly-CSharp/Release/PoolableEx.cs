using UnityEngine;

public static class PoolableEx
{
	public static bool SupportsPooling(this GameObject gameObject)
	{
		Poolable component = gameObject.GetComponent<Poolable>();
		if (component != null)
		{
			return component.prefabID != 0;
		}
		return false;
	}

	public static void AwakeFromInstantiate(this GameObject gameObject)
	{
		if (gameObject.activeSelf)
		{
			gameObject.GetComponent<Poolable>().SetBehaviourEnabled(true);
		}
		else
		{
			gameObject.SetActive(true);
		}
	}
}
