using Facepunch;
using System.Collections.Generic;
using UnityEngine;

public static class EnvironmentVolumeEx
{
	public static bool CheckEnvironmentVolumes(this Transform transform, Vector3 pos, Quaternion rot, Vector3 scale, EnvironmentType type)
	{
		List<EnvironmentVolume> obj = Pool.GetList<EnvironmentVolume>();
		transform.GetComponentsInChildren(true, obj);
		for (int i = 0; i < obj.Count; i++)
		{
			EnvironmentVolume environmentVolume = obj[i];
			OBB obb = new OBB(environmentVolume.transform, new Bounds(environmentVolume.Center, environmentVolume.Size));
			obb.Transform(pos, scale, rot);
			if (EnvironmentManager.Check(obb, type))
			{
				Pool.FreeList(ref obj);
				return true;
			}
		}
		Pool.FreeList(ref obj);
		return false;
	}

	public static bool CheckEnvironmentVolumes(this Transform transform, EnvironmentType type)
	{
		return CheckEnvironmentVolumes(transform, transform.position, transform.rotation, transform.lossyScale, type);
	}
}
