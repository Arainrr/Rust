using UnityEngine;

public class EnvironmentVolume : MonoBehaviour
{
	public bool StickyGizmos;

	public EnvironmentType Type = EnvironmentType.Underground;

	public Vector3 Center = Vector3.zero;

	public Vector3 Size = Vector3.one;

	public BoxCollider trigger
	{
		get;
		private set;
	}

	protected void OnDrawGizmos()
	{
		if (StickyGizmos)
		{
			DrawGizmos();
		}
	}

	protected void OnDrawGizmosSelected()
	{
		if (!StickyGizmos)
		{
			DrawGizmos();
		}
	}

	private void DrawGizmos()
	{
		Vector3 lossyScale = base.transform.lossyScale;
		Quaternion rotation = base.transform.rotation;
		Vector3 pos = base.transform.position + rotation * Vector3.Scale(lossyScale, Center);
		Vector3 size = Vector3.Scale(lossyScale, Size);
		Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
		GizmosUtil.DrawCube(pos, size, rotation);
		GizmosUtil.DrawWireCube(pos, size, rotation);
	}

	protected virtual void Awake()
	{
		UpdateTrigger();
	}

	public void UpdateTrigger()
	{
		if (!trigger)
		{
			trigger = base.gameObject.GetComponent<BoxCollider>();
		}
		if (!trigger)
		{
			trigger = base.gameObject.AddComponent<BoxCollider>();
		}
		trigger.isTrigger = true;
		trigger.center = Center;
		trigger.size = Size;
	}
}
