using UnityEngine;

public class WaterVolume : TriggerBase
{
	public Bounds WaterBounds = new Bounds(Vector3.zero, Vector3.one);

	private OBB cachedBounds;

	private Transform cachedTransform;

	public Transform[] cutOffPlanes = new Transform[0];

	public bool waterEnabled = true;

	private void OnEnable()
	{
		cachedTransform = base.transform;
		cachedBounds = new OBB(cachedTransform, WaterBounds);
	}

	public bool Test(Vector3 pos, out WaterLevel.WaterInfo info)
	{
		if (!waterEnabled)
		{
			info = default(WaterLevel.WaterInfo);
			return false;
		}
		UpdateCachedTransform();
		if (cachedBounds.Contains(pos))
		{
			if (!CheckCutOffPlanes(pos))
			{
				info = default(WaterLevel.WaterInfo);
				return false;
			}
			Vector3 a = new Plane(cachedBounds.up, cachedBounds.position).ClosestPointOnPlane(pos);
			float y = (a + cachedBounds.up * cachedBounds.extents.y).y;
			float y2 = (a + -cachedBounds.up * cachedBounds.extents.y).y;
			info.isValid = true;
			info.currentDepth = Mathf.Max(0f, y - pos.y);
			info.overallDepth = Mathf.Max(0f, y - y2);
			info.surfaceLevel = y;
			return true;
		}
		info = default(WaterLevel.WaterInfo);
		return false;
	}

	public bool Test(Bounds bounds, out WaterLevel.WaterInfo info)
	{
		if (!waterEnabled)
		{
			info = default(WaterLevel.WaterInfo);
			return false;
		}
		UpdateCachedTransform();
		if (cachedBounds.Contains(bounds.ClosestPoint(cachedBounds.position)))
		{
			if (!CheckCutOffPlanes(bounds.center))
			{
				info = default(WaterLevel.WaterInfo);
				return false;
			}
			Vector3 a = new Plane(cachedBounds.up, cachedBounds.position).ClosestPointOnPlane(bounds.center);
			float y = (a + cachedBounds.up * cachedBounds.extents.y).y;
			float y2 = (a + -cachedBounds.up * cachedBounds.extents.y).y;
			info.isValid = true;
			info.currentDepth = Mathf.Max(0f, y - bounds.min.y);
			info.overallDepth = Mathf.Max(0f, y - y2);
			info.surfaceLevel = y;
			return true;
		}
		info = default(WaterLevel.WaterInfo);
		return false;
	}

	private bool CheckCutOffPlanes(Vector3 pos)
	{
		int num = cutOffPlanes.Length;
		bool flag = true;
		for (int i = 0; i < num; i++)
		{
			if (cutOffPlanes[i] != null && cutOffPlanes[i].InverseTransformPoint(pos).y > 0f)
			{
				flag = false;
				break;
			}
		}
		if (!flag)
		{
			return false;
		}
		return true;
	}

	private void UpdateCachedTransform()
	{
		if (cachedTransform != null && cachedTransform.hasChanged)
		{
			cachedBounds = new OBB(cachedTransform, WaterBounds);
			cachedTransform.hasChanged = false;
		}
	}

	internal override GameObject InterestedInObject(GameObject obj)
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
		return baseEntity.gameObject;
	}
}
