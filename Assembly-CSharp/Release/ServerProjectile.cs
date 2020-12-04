using UnityEngine;

public class ServerProjectile : EntityComponent<BaseEntity>, IServerComponent
{
	public Vector3 initialVelocity;

	public float drag;

	public float gravityModifier = 1f;

	public float speed = 15f;

	public float scanRange;

	public Vector3 swimScale;

	public Vector3 swimSpeed;

	public float radius;

	public bool impacted;

	public float swimRandom;

	public Vector3 _currentVelocity = Vector3.zero;

	private void FixedUpdate()
	{
		if (base.baseEntity.isServer)
		{
			DoMovement();
		}
	}

	public void DoMovement()
	{
		if (impacted)
		{
			return;
		}
		_currentVelocity += Physics.gravity * gravityModifier * Time.fixedDeltaTime * Time.timeScale;
		Vector3 currentVelocity = _currentVelocity;
		if (swimScale != Vector3.zero)
		{
			if (swimRandom == 0f)
			{
				swimRandom = Random.Range(0f, 20f);
			}
			float num = Time.time + swimRandom;
			Vector3 direction = new Vector3(Mathf.Sin(num * swimSpeed.x) * swimScale.x, Mathf.Cos(num * swimSpeed.y) * swimScale.y, Mathf.Sin(num * swimSpeed.z) * swimScale.z);
			direction = base.transform.InverseTransformDirection(direction);
			currentVelocity += direction;
		}
		float num2 = currentVelocity.magnitude * Time.fixedDeltaTime;
		RaycastHit hitInfo;
		if (GamePhysics.Trace(new Ray(base.transform.position, currentVelocity.normalized), radius, out hitInfo, num2 + scanRange, 1236478737))
		{
			BaseEntity entity = RaycastHitEx.GetEntity(hitInfo);
			if (!BaseEntityEx.IsValid(entity) || !BaseEntityEx.IsValid(base.baseEntity.creatorEntity) || entity.net.ID != base.baseEntity.creatorEntity.net.ID)
			{
				base.transform.position += base.transform.forward * Mathf.Max(0f, hitInfo.distance - 0.1f);
				SendMessage("ProjectileImpact", hitInfo, SendMessageOptions.DontRequireReceiver);
				impacted = true;
				return;
			}
		}
		base.transform.position += base.transform.forward * num2;
		base.transform.rotation = Quaternion.LookRotation(currentVelocity.normalized);
	}

	public void InitializeVelocity(Vector3 overrideVel)
	{
		base.transform.rotation = Quaternion.LookRotation(overrideVel.normalized);
		initialVelocity = overrideVel;
		_currentVelocity = overrideVel;
	}
}
