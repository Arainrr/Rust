using UnityEngine;
using UnityEngine.AI;

public class HumanBrain : BaseAIBrain<HumanNPC>
{
	public class ExfilState : BasicAIState
	{
		public override float GetWeight()
		{
			if (GetEntity().RecentlyDismounted() && GetEntity().SecondsSinceAttacked > 1f)
			{
				return 100f;
			}
			return 0f;
		}

		public override void StateEnter()
		{
			base.StateEnter();
			GetEntity().SetDesiredSpeed(HumanNPC.SpeedType.Sprint);
			AIInformationZone informationZone = GetEntity().GetInformationZone();
			if (informationZone != null)
			{
				AICoverPoint bestCoverPoint = informationZone.GetBestCoverPoint(GetEntity().transform.position, GetEntity().transform.position, 25f, 50f, GetEntity());
				if ((bool)bestCoverPoint)
				{
					bestCoverPoint.SetUsedBy(GetEntity(), 10f);
				}
				Vector3 vector = (bestCoverPoint == null) ? GetEntity().transform.position : bestCoverPoint.transform.position;
				GetEntity().SetDestination(vector);
				brain.mainInterestPoint = vector;
			}
		}

		public override void StateThink(float delta)
		{
			base.StateThink(delta);
			if (GetEntity().CanSeeTarget() && TimeInState() > 2f && GetEntity().DistanceToTarget() < 10f)
			{
				GetEntity().SetDesiredSpeed(HumanNPC.SpeedType.Walk);
			}
			else
			{
				GetEntity().SetDesiredSpeed(HumanNPC.SpeedType.Sprint);
			}
		}
	}

	public class MountedState : BasicAIState
	{
		public override float GetWeight()
		{
			if (GetEntity().isMounted)
			{
				return 100f;
			}
			return 0f;
		}

		public override void StateEnter()
		{
			GetEntity().SetNavMeshEnabled(false);
			GetEntity().SetDesiredSpeed(HumanNPC.SpeedType.Walk);
			base.StateEnter();
		}

		public override void StateLeave()
		{
			GetEntity().SetNavMeshEnabled(true);
			base.StateLeave();
		}
	}

	public class TraversalState : BasicAIState
	{
		private Vector3 desiredDestination;

		public bool finished;

		private AITraversalArea area;

		private bool isTraversing;

		private bool waiting;

		public override float GetWeight()
		{
			if (finished)
			{
				return 0f;
			}
			AITraversalArea traversalArea = GetEntity().GetTraversalArea();
			if (isTraversing || waiting)
			{
				return 10000f;
			}
			if (GetEntity().IsInTraversalArea())
			{
				NavMeshPath path = GetEntity().NavAgent.path;
				bool flag = false;
				bool flag2 = false;
				Vector3[] corners = path.corners;
				foreach (Vector3 vector in corners)
				{
					if (Vector3.Distance(vector, traversalArea.entryPoint1.position) <= 2f)
					{
						flag = true;
					}
					else if (Vector3.Distance(vector, traversalArea.entryPoint2.position) <= 2f)
					{
						flag2 = true;
					}
					if (traversalArea.movementArea.Contains(vector))
					{
						return 10000f;
					}
					if (flag && flag2)
					{
						return 10000f;
					}
				}
			}
			return 0f;
		}

		public override void StateEnter()
		{
			base.StateEnter();
			GetEntity().SetDesiredSpeed(HumanNPC.SpeedType.Walk);
			finished = false;
			isTraversing = false;
			waiting = false;
			desiredDestination = GetEntity().finalDestination;
			area = GetEntity().GetTraversalArea();
			if ((bool)area && area.CanTraverse(GetEntity()))
			{
				area.SetBusyFor(2f);
			}
		}

		public override void StateThink(float delta)
		{
			base.StateThink(delta);
			if ((bool)area)
			{
				if (isTraversing)
				{
					area.SetBusyFor(delta * 2f);
				}
				else if (area.CanTraverse(GetEntity()))
				{
					waiting = false;
					isTraversing = true;
					AITraversalWaitPoint entryPointNear = area.GetEntryPointNear(area.GetFarthestEntry(GetEntity().transform.position).position);
					if ((bool)entryPointNear)
					{
						entryPointNear.Occupy(delta * 2f);
					}
					Vector3 destination = (entryPointNear == null) ? desiredDestination : entryPointNear.transform.position;
					GetEntity().SetDestination(destination);
					area.SetBusyFor(delta * 2f);
				}
				else
				{
					AITraversalWaitPoint entryPointNear2 = area.GetEntryPointNear(GetEntity().transform.position);
					if ((bool)entryPointNear2)
					{
						entryPointNear2.Occupy();
						GetEntity().SetStationaryAimPoint(area.GetClosestEntry(GetEntity().transform.position).position);
					}
					Vector3 destination2 = (entryPointNear2 == null) ? GetEntity().transform.position : entryPointNear2.transform.position;
					GetEntity().SetDestination(destination2);
					waiting = true;
					isTraversing = false;
				}
			}
			if (isTraversing && Vector3.Distance(GetEntity().transform.position, GetEntity().finalDestination) < 0.25f)
			{
				finished = true;
				isTraversing = false;
				waiting = false;
			}
		}

		public override bool CanInterrupt()
		{
			return true;
		}

		public override void StateLeave()
		{
			base.StateLeave();
			finished = false;
			area = null;
			isTraversing = false;
			waiting = false;
			GetEntity().SetDestination(desiredDestination);
		}
	}

	public class IdleState : BasicAIState
	{
		public override float GetWeight()
		{
			return 0.1f;
		}

		public override void StateEnter()
		{
			GetEntity().SetDesiredSpeed(HumanNPC.SpeedType.SlowWalk);
			base.StateEnter();
		}

		public override void StateThink(float delta)
		{
			base.StateThink(delta);
		}
	}

	public class RoamState : BasicAIState
	{
		private float nextRoamPositionTime = -1f;

		private float lastDestinationTime;

		public override float GetWeight()
		{
			if (!GetEntity().HasTarget() && GetEntity().SecondsSinceAttacked > 10f)
			{
				return 5f;
			}
			return 0f;
		}

		public override void StateEnter()
		{
			GetEntity().SetDesiredSpeed(HumanNPC.SpeedType.SlowWalk);
			GetEntity().SetPlayerFlag(BasePlayer.PlayerFlags.Relaxed, true);
			nextRoamPositionTime = -1f;
			lastDestinationTime = Time.time;
			base.StateEnter();
		}

		public override void StateLeave()
		{
			GetEntity().SetPlayerFlag(BasePlayer.PlayerFlags.Relaxed, false);
			base.StateLeave();
		}

		public override void StateThink(float delta)
		{
			base.StateThink(delta);
			bool flag = Time.time - lastDestinationTime > 25f;
			if (((Vector3.Distance(GetEntity().finalDestination, GetEntity().transform.position) < 2f) | flag) && nextRoamPositionTime == -1f)
			{
				nextRoamPositionTime = Time.time + Random.Range(5f, 10f);
			}
			if (nextRoamPositionTime != -1f && Time.time > nextRoamPositionTime)
			{
				AIMovePoint bestRoamPosition = GetEntity().GetBestRoamPosition(GetEntity().transform.position);
				if ((bool)bestRoamPosition)
				{
					float num = Vector3.Distance(bestRoamPosition.transform.position, GetEntity().transform.position) / 1.5f;
					bestRoamPosition.MarkUsedForRoam(num + 11f);
				}
				lastDestinationTime = Time.time;
				Vector3 destination = (bestRoamPosition == null) ? GetEntity().transform.position : bestRoamPosition.transform.position;
				GetEntity().SetDestination(destination);
				nextRoamPositionTime = -1f;
			}
		}
	}

	public class CoverState : BasicAIState
	{
		private float lastCoverTime;

		private bool isFleeing;

		private bool inCover;

		private float timeInCover;

		public override float GetWeight()
		{
			float num = 0f;
			if (!GetEntity().currentTarget && GetEntity().SecondsSinceAttacked < 2f)
			{
				return 4f;
			}
			if (GetEntity().DistanceToTarget() > GetEntity().EngagementRange() * 3f)
			{
				return 6f;
			}
			if (!IsInState() && TimeSinceState() < 2f)
			{
				return 0f;
			}
			if (GetEntity().SecondsSinceAttacked < 5f || GetEntity().healthFraction < 0.4f || GetEntity().DistanceToTarget() < 15f)
			{
				if (GetEntity().IsReloading())
				{
					num += 2f;
				}
				num += (1f - Mathf.Lerp(0.1f, 0.35f, GetEntity().AmmoFractionRemaining())) * 1.5f;
			}
			if (isFleeing)
			{
				num += 1f;
			}
			if (GetEntity().healthFraction < 1f)
			{
				float num2 = 1f - Mathf.InverseLerp(0.8f, 1f, GetEntity().healthFraction);
				num += (1f - Mathf.InverseLerp(1f, 2f, GetEntity().SecondsSinceAttacked)) * num2 * 2f;
			}
			return num;
		}

		public override bool CanInterrupt()
		{
			float num = GetEntity().currentTarget ? 2f : 8f;
			if (TimeInState() > 5f)
			{
				if (inCover)
				{
					return timeInCover > num;
				}
				return true;
			}
			return false;
		}

		public override void StateEnter()
		{
			GetEntity().SetDesiredSpeed(HumanNPC.SpeedType.Walk);
			lastCoverTime = -10f;
			isFleeing = false;
			inCover = false;
			timeInCover = -1f;
			GetEntity().ClearStationaryAimPoint();
			base.StateEnter();
		}

		public override void StateLeave()
		{
			base.StateLeave();
			GetEntity().SetDucked(false);
			GetEntity().ClearStationaryAimPoint();
		}

		public override void StateThink(float delta)
		{
			base.StateThink(delta);
			float num = 2f;
			float num2 = 0f;
			if (Time.time > lastCoverTime + num && !isFleeing)
			{
				Vector3 hideFromPosition = GetEntity().currentTarget ? GetEntity().currentTarget.transform.position : (GetEntity().transform.position + GetEntity().LastAttackedDir * 30f);
				float num3 = (GetEntity().currentTarget != null) ? GetEntity().DistanceToTarget() : 30f;
				AIInformationZone informationZone = GetEntity().GetInformationZone();
				if (informationZone != null)
				{
					float secondsSinceAttacked = GetEntity().SecondsSinceAttacked;
					float minRange = (secondsSinceAttacked < 2f) ? 2f : 0f;
					float maxRange = 20f;
					AICoverPoint bestCoverPoint = informationZone.GetBestCoverPoint(GetEntity().transform.position, hideFromPosition, minRange, maxRange, GetEntity());
					if ((bool)bestCoverPoint)
					{
						bestCoverPoint.SetUsedBy(GetEntity());
					}
					Vector3 vector = (bestCoverPoint == null) ? GetEntity().transform.position : bestCoverPoint.transform.position;
					GetEntity().SetDestination(vector);
					float num4 = Vector3.Distance(vector, GetEntity().transform.position);
					GetEntity().DistanceToTarget();
					bool flag2;
					int num5;
					if (secondsSinceAttacked < 4f)
					{
						flag2 = (GetEntity().AmmoFractionRemaining() <= 0.25f);
					}
					else
						num5 = 0;
					bool flag3;
					int num6;
					if (GetEntity().healthFraction < 0.5f && secondsSinceAttacked < 1f)
					{
						flag3 = (Time.time > num2);
					}
					else
						num6 = 0;
					if ((num3 > 6f && num4 > 6f) || GetEntity().currentTarget == null)
					{
						isFleeing = true;
						num2 = Time.time + Random.Range(4f, 7f);
					}
					if (num4 > 1f)
					{
						GetEntity().ClearStationaryAimPoint();
					}
				}
				lastCoverTime = Time.time;
			}
			bool flag = Vector3.Distance(GetEntity().transform.position, GetEntity().finalDestination) <= 0.25f;
			if (!inCover && flag)
			{
				if (isFleeing)
				{
					GetEntity().SetStationaryAimPoint(GetEntity().finalDestination + -GetEntity().eyes.BodyForward() * 5f);
				}
				else if ((bool)GetEntity().currentTarget)
				{
					GetEntity().SetStationaryAimPoint(GetEntity().transform.position + Vector3Ex.Direction2D(GetEntity().currentTarget.transform.position, GetEntity().transform.position) * 5f);
				}
			}
			inCover = flag;
			if (inCover)
			{
				timeInCover += delta;
			}
			else
			{
				timeInCover = 0f;
			}
			GetEntity().SetDucked(inCover);
			if (inCover)
			{
				isFleeing = false;
			}
			if (GetEntity().AmmoFractionRemaining() == 0f || isFleeing || (!GetEntity().CanSeeTarget() && inCover && GetEntity().SecondsSinceDealtDamage > 2f && GetEntity().AmmoFractionRemaining() < 0.25f))
			{
				GetEntity().AttemptReload();
			}
			if (!inCover)
			{
				if (TimeInState() > 1f && isFleeing)
				{
					GetEntity().SetDesiredSpeed(HumanNPC.SpeedType.Sprint);
				}
				else
				{
					GetEntity().SetDesiredSpeed(HumanNPC.SpeedType.Walk);
				}
			}
		}
	}

	public class CombatState : BasicAIState
	{
		private float nextStrafeTime;

		public override void StateEnter()
		{
			base.StateEnter();
			brain.mainInterestPoint = GetEntity().transform.position;
			GetEntity().SetDesiredSpeed(HumanNPC.SpeedType.Walk);
		}

		public override float GetWeight()
		{
			if (!GetEntity().HasTarget())
			{
				return 0f;
			}
			if (!GetEntity().TargetInRange())
			{
				return 0f;
			}
			float num = 1f - Mathf.InverseLerp(GetEntity().GetIdealDistanceFromTarget(), GetEntity().EngagementRange(), GetEntity().DistanceToTarget());
			float num2 = 0.5f * num;
			if (GetEntity().CanSeeTarget())
			{
				num2 += 1f;
			}
			return num2;
		}

		public override void StateLeave()
		{
			GetEntity().SetDucked(false);
			base.StateLeave();
		}

		public override void StateThink(float delta)
		{
			base.StateThink(delta);
			if (Time.time > nextStrafeTime)
			{
				if (Random.Range(0, 3) == 1)
				{
					nextStrafeTime = Time.time + Random.Range(2f, 3f);
					GetEntity().SetDucked(true);
					GetEntity().Stop();
				}
				else
				{
					nextStrafeTime = Time.time + Random.Range(3f, 4f);
					GetEntity().SetDucked(false);
					GetEntity().SetDesiredSpeed(HumanNPC.SpeedType.Walk);
					GetEntity().SetDestination(GetEntity().GetRandomPositionAround(brain.mainInterestPoint, 1f));
				}
			}
		}
	}

	public class ChaseState : BasicAIState
	{
		private float nextPositionUpdateTime;

		public override float GetWeight()
		{
			float num = 0f;
			if (!GetEntity().HasTarget())
			{
				return 0f;
			}
			if (GetEntity().AmmoFractionRemaining() < 0.3f || GetEntity().IsReloading())
			{
				num -= 1f;
			}
			if (GetEntity().HasTarget())
			{
				num += 0.5f;
			}
			num = (GetEntity().CanSeeTarget() ? (num - 0.5f) : (num + 1f));
			if (GetEntity().DistanceToTarget() > GetEntity().GetIdealDistanceFromTarget())
			{
				num += 1f;
			}
			return num;
		}

		public override void StateEnter()
		{
			base.StateEnter();
			GetEntity().SetDesiredSpeed(HumanNPC.SpeedType.Walk);
		}

		public override void StateLeave()
		{
			base.StateLeave();
		}

		public override void StateThink(float delta)
		{
			base.StateThink(delta);
			if (GetEntity().currentTarget == null)
			{
				return;
			}
			float num = Vector3.Distance(GetEntity().currentTarget.transform.position, GetEntity().transform.position);
			if (num < 5f)
			{
				GetEntity().SetDesiredSpeed(HumanNPC.SpeedType.SlowWalk);
			}
			else if (num < 10f)
			{
				GetEntity().SetDesiredSpeed(HumanNPC.SpeedType.Walk);
			}
			else
			{
				GetEntity().SetDesiredSpeed(HumanNPC.SpeedType.Sprint);
			}
			if (!(Time.time > nextPositionUpdateTime))
			{
				return;
			}
			Random.Range(1f, 2f);
			Vector3 destination = GetEntity().transform.position;
			if (!(GetEntity().GetInformationZone() == null))
			{
				AIMovePoint bestMovePointNear = GetEntity().GetInformationZone().GetBestMovePointNear(GetEntity().currentTarget.transform.position, GetEntity().transform.position, 0f, 35f, true);
				if ((bool)bestMovePointNear)
				{
					bestMovePointNear.MarkUsedForEngagement(5f, GetEntity());
					destination = bestMovePointNear.transform.position;
					destination = GetEntity().GetRandomPositionAround(destination, 0f, bestMovePointNear.radius - 0.3f);
				}
				else
				{
					GetEntity().GetRandomPositionAround(GetEntity().currentTarget.transform.position, 1f);
				}
				GetEntity().SetDestination(destination);
			}
		}
	}

	public const int HumanState_Idle = 1;

	public const int HumanState_Flee = 2;

	public const int HumanState_Cover = 3;

	public const int HumanState_Patrol = 4;

	public const int HumanState_Roam = 5;

	public const int HumanState_Chase = 6;

	public const int HumanState_Exfil = 7;

	public const int HumanState_Mounted = 8;

	public const int HumanState_Combat = 9;

	public const int HumanState_Traverse = 10;

	public const int HumanState_Alert = 11;

	public const int HumanState_Investigate = 12;

	private float thinkRate = 0.25f;

	private float lastThinkTime = -0f;

	public override void InitializeAI()
	{
		base.InitializeAI();
		AIStates = new BasicAIState[11];
		AddState(new IdleState(), 1);
		AddState(new RoamState(), 5);
		AddState(new ChaseState(), 6);
		AddState(new CoverState(), 3);
		AddState(new CombatState(), 9);
		AddState(new MountedState(), 8);
		AddState(new ExfilState(), 7);
	}

	public override bool ShouldThink()
	{
		if (Time.time > lastThinkTime + thinkRate)
		{
			return true;
		}
		return false;
	}

	public override void DoThink()
	{
		float delta = Time.time - lastThinkTime;
		AIThink(delta);
		lastThinkTime = Time.time;
	}
}
