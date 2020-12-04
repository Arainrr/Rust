using UnityEngine;
using UnityEngine.Events;

public class GenericSpawnPoint : BaseSpawnPoint
{
	public bool dropToGround = true;

	public bool randomRot;

	public GameObjectRef spawnEffect;

	public UnityEvent OnObjectSpawnedEvent = new UnityEvent();

	public UnityEvent OnObjectRetiredEvent = new UnityEvent();

	public override void GetLocation(out Vector3 pos, out Quaternion rot)
	{
		pos = base.transform.position;
		if (randomRot)
		{
			rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
		}
		else
		{
			rot = base.transform.rotation;
		}
		if (dropToGround)
		{
			DropToGround(ref pos, ref rot);
		}
	}

	public override void ObjectSpawned(SpawnPointInstance instance)
	{
		if (spawnEffect.isValid)
		{
			Effect.server.Run(spawnEffect.resourcePath, instance.GetComponent<BaseEntity>(), 0u, Vector3.zero, Vector3.up);
		}
		OnObjectSpawnedEvent.Invoke();
		base.gameObject.SetActive(false);
	}

	public override void ObjectRetired(SpawnPointInstance instance)
	{
		OnObjectRetiredEvent.Invoke();
		base.gameObject.SetActive(true);
	}
}
