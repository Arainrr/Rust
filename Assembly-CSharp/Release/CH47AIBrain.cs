using System.Collections.Generic;
using UnityEngine;

public class CH47AIBrain : BaseAIBrain<CH47HelicopterAIController>
{
	public class IdleState : BasicAIState
	{
		public override float GetWeight()
		{
			return 0.1f;
		}

		public override void StateEnter()
		{
			brain.GetEntity().SetMoveTarget(brain.GetEntity().GetPosition() + brain.GetEntity().rigidBody.velocity.normalized * 10f);
			base.StateEnter();
		}
	}

	public class PatrolState : BasicAIState
	{
		public List<Vector3> visitedPoints = new List<Vector3>();

		public static float patrolApproachDist = 75f;

		public bool AtPatrolDestination()
		{
			return Vector3Ex.Distance2D(brain.mainInterestPoint, brain.GetEntity().GetPosition()) < patrolApproachDist;
		}

		public override bool CanInterrupt()
		{
			if (base.CanInterrupt())
			{
				return AtPatrolDestination();
			}
			return false;
		}

		public override float GetWeight()
		{
			if (IsInState())
			{
				if (AtPatrolDestination() && TimeInState() > 2f)
				{
					return 0f;
				}
				return 3f;
			}
			float num = Mathf.InverseLerp(70f, 120f, TimeSinceState()) * 5f;
			return 1f + num;
		}

		public MonumentInfo GetRandomValidMonumentInfo()
		{
			int count = TerrainMeta.Path.Monuments.Count;
			int num = Random.Range(0, count);
			for (int i = 0; i < count; i++)
			{
				int num2 = i + num;
				if (num2 >= count)
				{
					num2 -= count;
				}
				MonumentInfo monumentInfo = TerrainMeta.Path.Monuments[num2];
				if (monumentInfo.Type != 0 && monumentInfo.Type != MonumentType.WaterWell && monumentInfo.Tier != MonumentTier.Tier0)
				{
					return monumentInfo;
				}
			}
			return null;
		}

		public Vector3 GetRandomPatrolPoint()
		{
			Vector3 result = Vector3.zero;
			MonumentInfo monumentInfo = null;
			if (TerrainMeta.Path != null && TerrainMeta.Path.Monuments != null && TerrainMeta.Path.Monuments.Count > 0)
			{
				int count = TerrainMeta.Path.Monuments.Count;
				int num = Random.Range(0, count);
				for (int i = 0; i < count; i++)
				{
					int num2 = i + num;
					if (num2 >= count)
					{
						num2 -= count;
					}
					MonumentInfo monumentInfo2 = TerrainMeta.Path.Monuments[num2];
					if (monumentInfo2.Type != 0 && monumentInfo2.Type != MonumentType.WaterWell && monumentInfo2.Tier != MonumentTier.Tier0 && (monumentInfo2.Tier & MonumentTier.Tier0) <= (MonumentTier)0)
					{
						bool flag = false;
						foreach (Vector3 visitedPoint in visitedPoints)
						{
							if (Vector3Ex.Distance2D(monumentInfo2.transform.position, visitedPoint) < 100f)
							{
								flag = true;
								break;
							}
						}
						if (!flag)
						{
							monumentInfo = monumentInfo2;
							break;
						}
					}
				}
				if (monumentInfo == null)
				{
					visitedPoints.Clear();
					monumentInfo = GetRandomValidMonumentInfo();
				}
			}
			if (monumentInfo != null)
			{
				visitedPoints.Add(monumentInfo.transform.position);
				result = monumentInfo.transform.position;
			}
			else
			{
				float x = TerrainMeta.Size.x;
				float y = 30f;
				result = Vector3Ex.Range(-1f, 1f);
				result.y = 0f;
				result.Normalize();
				result *= x * Random.Range(0f, 0.75f);
				result.y = y;
			}
			return result;
		}

		public override void StateEnter()
		{
			base.StateEnter();
			Vector3 randomPatrolPoint = GetRandomPatrolPoint();
			brain.mainInterestPoint = randomPatrolPoint;
			float num = Mathf.Max(TerrainMeta.WaterMap.GetHeight(randomPatrolPoint), TerrainMeta.HeightMap.GetHeight(randomPatrolPoint));
			float num2 = num;
			RaycastHit hitInfo;
			if (Physics.SphereCast(randomPatrolPoint + new Vector3(0f, 200f, 0f), 20f, Vector3.down, out hitInfo, 300f, 1218511105))
			{
				num2 = Mathf.Max(hitInfo.point.y, num);
			}
			brain.mainInterestPoint.y = num2 + 30f;
		}

		public override void StateThink(float delta)
		{
			base.StateThink(delta);
			brain.GetEntity().SetMoveTarget(brain.mainInterestPoint);
		}
	}

	public class LandState : BasicAIState
	{
		private float landedForSeconds;

		private float lastLandtime;

		private float landingHeight = 20f;

		private float nextDismountTime;

		public override float GetWeight()
		{
			if (!GetEntity().ShouldLand())
			{
				return 0f;
			}
			float num = Time.time - lastLandtime;
			if (IsInState() && landedForSeconds < 12f)
			{
				return 1000f;
			}
			if (!IsInState() && num > 10f)
			{
				return 9000f;
			}
			return 0f;
		}

		public override void StateThink(float delta)
		{
			Vector3 position = brain.GetEntity().transform.position;
			Vector3 forward = brain.GetEntity().transform.forward;
			CH47LandingZone closest = CH47LandingZone.GetClosest(brain.GetEntity().landingTarget);
			if (!closest)
			{
				return;
			}
			float magnitude = brain.GetEntity().rigidBody.velocity.magnitude;
			Vector3.Distance(closest.transform.position, position);
			float num = Vector3Ex.Distance2D(closest.transform.position, position);
			Mathf.InverseLerp(1f, 20f, num);
			bool enabled = num < 40f;
			bool altitudeProtection = num > 15f && position.y < closest.transform.position.y + 10f;
			brain.GetEntity().EnableFacingOverride(enabled);
			brain.GetEntity().SetAltitudeProtection(altitudeProtection);
			bool num2 = Mathf.Abs(closest.transform.position.y - position.y) < 3f && num <= 5f && magnitude < 1f;
			if (num2)
			{
				landedForSeconds += delta;
				if (lastLandtime == 0f)
				{
					lastLandtime = Time.time;
				}
			}
			float num3 = 1f - Mathf.InverseLerp(0f, 7f, num);
			landingHeight -= 4f * num3 * Time.deltaTime;
			if (landingHeight < -5f)
			{
				landingHeight = -5f;
			}
			brain.GetEntity().SetAimDirection(closest.transform.forward);
			Vector3 moveTarget = brain.mainInterestPoint + new Vector3(0f, landingHeight, 0f);
			if (num < 100f && num > 15f)
			{
				Vector3 vector = Vector3Ex.Direction2D(closest.transform.position, position);
				RaycastHit hitInfo;
				if (Physics.SphereCast(position, 15f, vector, out hitInfo, num, 1218511105))
				{
					Vector3 a = Vector3.Cross(vector, Vector3.up);
					moveTarget = hitInfo.point + a * 50f;
				}
			}
			brain.GetEntity().SetMoveTarget(moveTarget);
			if (num2)
			{
				if (landedForSeconds > 1f && Time.time > nextDismountTime)
				{
					foreach (BaseVehicle.MountPointInfo mountPoint in brain.GetEntity().mountPoints)
					{
						if ((bool)mountPoint.mountable && mountPoint.mountable.IsMounted())
						{
							nextDismountTime = Time.time + 0.5f;
							mountPoint.mountable.DismountAllPlayers();
							break;
						}
					}
				}
				if (landedForSeconds > 8f)
				{
					brain.GetComponent<CH47AIBrain>().age = float.PositiveInfinity;
				}
			}
		}

		public override void StateEnter()
		{
			brain.mainInterestPoint = GetEntity().landingTarget;
			landingHeight = 15f;
			base.StateEnter();
		}

		public override void StateLeave()
		{
			brain.GetEntity().EnableFacingOverride(false);
			brain.GetEntity().SetAltitudeProtection(true);
			brain.GetEntity().SetMinHoverHeight(30f);
			landedForSeconds = 0f;
			base.StateLeave();
		}

		public override bool CanInterrupt()
		{
			return true;
		}
	}

	public class OrbitState : BasicAIState
	{
		public Vector3 GetOrbitCenter()
		{
			return brain.mainInterestPoint;
		}

		public override bool CanInterrupt()
		{
			return base.CanInterrupt();
		}

		public override float GetWeight()
		{
			if (IsInState())
			{
				float num = 1f - Mathf.InverseLerp(120f, 180f, TimeInState());
				return 5f * num;
			}
			if (brain._currentState == 2 && Vector3Ex.Distance2D(brain.mainInterestPoint, brain.GetEntity().GetPosition()) <= PatrolState.patrolApproachDist * 1.1f)
			{
				return 5f;
			}
			return 0f;
		}

		public override void StateEnter()
		{
			brain.GetEntity().EnableFacingOverride(true);
			brain.GetEntity().InitiateAnger();
			base.StateEnter();
		}

		public override void StateThink(float delta)
		{
			Vector3 orbitCenter = GetOrbitCenter();
			CH47HelicopterAIController entity = brain.GetEntity();
			Vector3 position = entity.GetPosition();
			Vector3 vector = Vector3Ex.Direction2D(orbitCenter, position);
			Vector3 vector2 = Vector3.Cross(Vector3.up, vector);
			float d = (Vector3.Dot(Vector3.Cross(entity.transform.right, Vector3.up), vector2) < 0f) ? (-1f) : 1f;
			float d2 = 75f;
			Vector3 normalized = (-vector + vector2 * d * 0.6f).normalized;
			Vector3 vector3 = orbitCenter + normalized * d2;
			entity.SetMoveTarget(vector3);
			entity.SetAimDirection(Vector3Ex.Direction2D(vector3, position));
			base.StateThink(delta);
		}

		public override void StateLeave()
		{
			brain.GetEntity().EnableFacingOverride(false);
			brain.GetEntity().CancelAnger();
			base.StateLeave();
		}
	}

	public class EgressState : BasicAIState
	{
		private bool killing;

		private bool egressAltitueAchieved;

		public override bool CanInterrupt()
		{
			return false;
		}

		public override float GetWeight()
		{
			if (brain.GetEntity().OutOfCrates() && !brain.GetEntity().ShouldLand())
			{
				return 10000f;
			}
			CH47AIBrain component = brain.GetComponent<CH47AIBrain>();
			if (component != null)
			{
				if (!(component.age > 1800f))
				{
					return 0f;
				}
				return 10000f;
			}
			return 0f;
		}

		public override void StateEnter()
		{
			brain.GetEntity().EnableFacingOverride(false);
			Transform transform = brain.GetEntity().transform;
			Rigidbody rigidBody = brain.GetEntity().rigidBody;
			Vector3 rhs = (rigidBody.velocity.magnitude < 0.1f) ? transform.forward : rigidBody.velocity.normalized;
			Vector3 a = Vector3.Cross(Vector3.Cross(transform.up, rhs), Vector3.up);
			brain.mainInterestPoint = transform.position + a * 8000f;
			brain.mainInterestPoint.y = 100f;
			brain.GetEntity().SetMoveTarget(brain.mainInterestPoint);
			base.StateEnter();
		}

		public override void StateThink(float delta)
		{
			base.StateThink(delta);
			if (killing)
			{
				return;
			}
			Vector3 position = brain.GetEntity().transform.position;
			if (position.y < 85f && !egressAltitueAchieved)
			{
				CH47LandingZone closest = CH47LandingZone.GetClosest(position);
				if (closest != null && Vector3Ex.Distance2D(closest.transform.position, position) < 20f)
				{
					float num = 0f;
					if (TerrainMeta.HeightMap != null && TerrainMeta.WaterMap != null)
					{
						num = Mathf.Max(TerrainMeta.WaterMap.GetHeight(position), TerrainMeta.HeightMap.GetHeight(position));
					}
					num += 100f;
					Vector3 moveTarget = position;
					moveTarget.y = num;
					brain.GetEntity().SetMoveTarget(moveTarget);
					return;
				}
			}
			egressAltitueAchieved = true;
			brain.GetEntity().SetMoveTarget(brain.mainInterestPoint);
			if (TimeInState() > 300f)
			{
				brain.GetEntity().Invoke("DelayedKill", 2f);
				killing = true;
			}
		}

		public override void StateLeave()
		{
			base.StateLeave();
		}
	}

	public class DropCrate : BasicAIState
	{
		private float nextDropTime;

		public override bool CanInterrupt()
		{
			if (base.CanInterrupt())
			{
				return !CanDrop();
			}
			return false;
		}

		public bool CanDrop()
		{
			if (Time.time > nextDropTime)
			{
				return brain.GetEntity().CanDropCrate();
			}
			return false;
		}

		public override float GetWeight()
		{
			if (!CanDrop())
			{
				return 0f;
			}
			if (IsInState())
			{
				return 10000f;
			}
			if (brain._currentState == 5 && brain.GetCurrentState().TimeInState() > 60f)
			{
				CH47DropZone closest = CH47DropZone.GetClosest(brain.mainInterestPoint);
				if ((bool)closest && Vector3Ex.Distance2D(closest.transform.position, brain.mainInterestPoint) < 200f)
				{
					CH47AIBrain component = brain.GetComponent<CH47AIBrain>();
					if (component != null)
					{
						float num = Mathf.InverseLerp(300f, 600f, component.age);
						return 1000f * num;
					}
				}
			}
			return 0f;
		}

		public override void StateEnter()
		{
			brain.GetEntity().SetDropDoorOpen(true);
			brain.GetEntity().EnableFacingOverride(false);
			CH47DropZone closest = CH47DropZone.GetClosest(brain.GetEntity().transform.position);
			if (closest == null)
			{
				nextDropTime = Time.time + 60f;
			}
			brain.mainInterestPoint = closest.transform.position;
			brain.GetEntity().SetMoveTarget(brain.mainInterestPoint);
			base.StateEnter();
		}

		public override void StateThink(float delta)
		{
			base.StateThink(delta);
			if (CanDrop() && Vector3Ex.Distance2D(brain.mainInterestPoint, brain.GetEntity().transform.position) < 5f && brain.GetEntity().rigidBody.velocity.magnitude < 5f)
			{
				brain.GetEntity().DropCrate();
				nextDropTime = Time.time + 120f;
			}
		}

		public override void StateLeave()
		{
			brain.GetEntity().SetDropDoorOpen(false);
			nextDropTime = Time.time + 60f;
			base.StateLeave();
		}
	}

	public const int CH47State_Idle = 1;

	public const int CH47State_Patrol = 2;

	public const int CH47State_Land = 3;

	public const int CH47State_Dropoff = 4;

	public const int CH47State_Orbit = 5;

	public const int CH47State_Retreat = 6;

	public const int CH47State_Egress = 7;

	private float age;

	public override void InitializeAI()
	{
		base.InitializeAI();
		AIStates = new BasicAIState[8];
		AddState(new IdleState(), 1);
		AddState(new PatrolState(), 2);
		AddState(new OrbitState(), 5);
		AddState(new EgressState(), 7);
		AddState(new DropCrate(), 4);
		AddState(new LandState(), 3);
	}

	public void FixedUpdate()
	{
		if (!(base.baseEntity == null) && !base.baseEntity.isClient)
		{
			AIThink(Time.fixedDeltaTime);
		}
	}

	public void OnDrawGizmos()
	{
		GetCurrentState()?.DrawGizmos();
	}

	public override void AIThink(float delta)
	{
		age += delta;
		base.AIThink(delta);
	}
}
