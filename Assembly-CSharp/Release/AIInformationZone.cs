using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIInformationZone : MonoBehaviour
{
	public static List<AIInformationZone> zones = new List<AIInformationZone>();

	public List<AICoverPoint> coverPoints = new List<AICoverPoint>();

	public List<AIMovePoint> movePoints = new List<AIMovePoint>();

	public List<NavMeshLink> navMeshLinks = new List<NavMeshLink>();

	public Bounds bounds;

	private OBB areaBox;

	public void OnValidate()
	{
	}

	public void Start()
	{
		Process();
		areaBox = new OBB(base.transform.position, base.transform.lossyScale, base.transform.rotation, bounds);
		zones.Add(this);
	}

	public void OnDrawGizmos()
	{
		Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
		Gizmos.DrawCube(base.transform.position + bounds.center, bounds.size);
	}

	public void Process()
	{
		AICoverPoint[] componentsInChildren = base.transform.GetComponentsInChildren<AICoverPoint>();
		coverPoints.AddRange(componentsInChildren);
		AIMovePoint[] componentsInChildren2 = base.transform.GetComponentsInChildren<AIMovePoint>(true);
		movePoints.AddRange(componentsInChildren2);
		NavMeshLink[] componentsInChildren3 = base.transform.GetComponentsInChildren<NavMeshLink>(true);
		navMeshLinks.AddRange(componentsInChildren3);
	}

	public static AIInformationZone GetForPoint(Vector3 point, BaseEntity from = null)
	{
		if (zones == null || zones.Count == 0)
		{
			return null;
		}
		foreach (AIInformationZone zone in zones)
		{
			if (zone.areaBox.Contains(point))
			{
				return zone;
			}
		}
		return zones[0];
	}

	public AIMovePoint GetBestMovePointNear(Vector3 targetPosition, Vector3 fromPosition, float minRange, float maxRange, bool checkLOS = false, BaseEntity forObject = null)
	{
		AIMovePoint result = null;
		float num = -1f;
		foreach (AIMovePoint movePoint in movePoints)
		{
			float num2 = 0f;
			Vector3 position = movePoint.transform.position;
			float num3 = Vector3.Distance(targetPosition, position);
			if (!(num3 > maxRange) && movePoint.transform.parent.gameObject.activeSelf && !(movePoint.transform.position.y < WaterSystem.OceanLevel))
			{
				num2 += (movePoint.CanBeUsedBy(forObject) ? 100f : 0f);
				num2 += (1f - Mathf.InverseLerp(minRange, maxRange, num3)) * 100f;
				if (!(num2 < num) && (!checkLOS || !Physics.Linecast(targetPosition + Vector3.up * 1f, position + Vector3.up * 1f, 1218519297, QueryTriggerInteraction.Ignore)) && num2 > num)
				{
					result = movePoint;
					num = num2;
				}
			}
		}
		return result;
	}

	public Vector3 GetBestPositionNear(Vector3 targetPosition, Vector3 fromPosition, float minRange, float maxRange, bool checkLOS = false)
	{
		AIMovePoint aIMovePoint = null;
		float num = -1f;
		foreach (AIMovePoint movePoint in movePoints)
		{
			float num2 = 0f;
			Vector3 position = movePoint.transform.position;
			float num3 = Vector3.Distance(targetPosition, position);
			if (!(num3 > maxRange) && movePoint.transform.parent.gameObject.activeSelf)
			{
				num2 += (1f - Mathf.InverseLerp(minRange, maxRange, num3)) * 100f;
				if ((!checkLOS || !Physics.Linecast(targetPosition + Vector3.up * 1f, position + Vector3.up * 1f, 1218650369, QueryTriggerInteraction.Ignore)) && num2 > num)
				{
					aIMovePoint = movePoint;
					num = num2;
				}
			}
		}
		if (aIMovePoint != null)
		{
			return aIMovePoint.transform.position;
		}
		return targetPosition;
	}

	public AICoverPoint GetBestCoverPoint(Vector3 currentPosition, Vector3 hideFromPosition, float minRange = 0f, float maxRange = 20f, BaseEntity forObject = null)
	{
		AICoverPoint aICoverPoint = null;
		float num = 0f;
		foreach (AICoverPoint coverPoint in coverPoints)
		{
			if (!coverPoint.InUse() || coverPoint.IsUsedBy(forObject))
			{
				Vector3 position = coverPoint.transform.position;
				Vector3 normalized = (hideFromPosition - position).normalized;
				float num2 = Vector3.Dot(coverPoint.transform.forward, normalized);
				if (!(num2 < 1f - coverPoint.coverDot))
				{
					float value = Vector3.Distance(currentPosition, position);
					float num3 = 0f;
					if (minRange > 0f)
					{
						num3 -= (1f - Mathf.InverseLerp(0f, minRange, value)) * 100f;
					}
					float value2 = Mathf.Abs(position.y - currentPosition.y);
					num3 += (1f - Mathf.InverseLerp(1f, 5f, value2)) * 500f;
					num3 += Mathf.InverseLerp(1f - coverPoint.coverDot, 1f, num2) * 50f;
					num3 += (1f - Mathf.InverseLerp(2f, maxRange, value)) * 100f;
					float num4 = 1f - Mathf.InverseLerp(4f, 10f, Vector3.Distance(currentPosition, hideFromPosition));
					float value3 = Vector3.Dot((coverPoint.transform.position - currentPosition).normalized, normalized);
					num3 -= Mathf.InverseLerp(-1f, 0.25f, value3) * 50f * num4;
					if (num3 > num)
					{
						aICoverPoint = coverPoint;
						num = num3;
					}
				}
			}
		}
		if ((bool)aICoverPoint)
		{
			return aICoverPoint;
		}
		return null;
	}

	public NavMeshLink GetClosestNavMeshLink(Vector3 pos)
	{
		NavMeshLink result = null;
		float num = float.PositiveInfinity;
		foreach (NavMeshLink navMeshLink in navMeshLinks)
		{
			float num2 = Vector3.Distance(navMeshLink.gameObject.transform.position, pos);
			if (num2 < num)
			{
				result = navMeshLink;
				num = num2;
				if (num2 < 0.25f)
				{
					return result;
				}
			}
		}
		return result;
	}
}
