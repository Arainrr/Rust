using UnityEngine;

public class EffectMount : EntityComponent<BaseEntity>, IClientComponent
{
	public GameObject effectPrefab;

	public GameObject spawnedEffect;

	public GameObject mountBone;

	public void SetOn(bool isOn)
	{
		if ((bool)spawnedEffect)
		{
			GameManager.Destroy(spawnedEffect);
		}
		spawnedEffect = null;
		if (isOn)
		{
			spawnedEffect = Object.Instantiate(effectPrefab);
			spawnedEffect.transform.SetPositionAndRotation(mountBone.transform.position, mountBone.transform.rotation);
			spawnedEffect.transform.parent = mountBone.transform;
			spawnedEffect.SetActive(true);
		}
	}
}
