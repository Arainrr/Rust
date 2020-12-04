using Apex.Serialization;
using ConVar;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Rust.Ai
{
	public class HumanNavigateToOperator : BaseAction
	{
		public enum OperatorType
		{
			EnemyLoc,
			RandomLoc,
			SpawnLoc,
			FleeEnemy,
			FleeHurtDir,
			RetreatCover,
			FlankCover,
			AdvanceCover,
			FleeExplosive,
			Sidestep,
			ClosestCover,
			PatrolLoc,
			MountableChair,
			WaypointLoc,
			LastEnemyLoc,
			HideoutLoc
		}

		public enum TakeCoverIntention
		{
			Advance,
			Flank,
			Retreat,
			Closest
		}

		[ApexSerialization]
		public OperatorType Operator;

		public override void DoExecute(BaseContext c)
		{
			NPCHumanContext nPCHumanContext = c as NPCHumanContext;
			if (c.GetFact(NPCPlayerApex.Facts.CanNotMove) == 1 || (nPCHumanContext != null && nPCHumanContext.Human.NeverMove))
			{
				c.AIAgent.StopMoving();
				nPCHumanContext?.Human.SetFact(NPCPlayerApex.Facts.PathToTargetStatus, 2);
				return;
			}
			c.AIAgent.SetFact(NPCPlayerApex.Facts.IsRetreatingToCover, 0);
			c.AIAgent.SetFact(NPCPlayerApex.Facts.SidesteppedOutOfCover, 0);
			if (UnityEngine.Time.time - nPCHumanContext.LastNavigationTime < 1f)
			{
				return;
			}
			nPCHumanContext.LastNavigationTime = UnityEngine.Time.time;
			if (!nPCHumanContext.Human.NavAgent.pathPending)
			{
				switch (Operator)
				{
				case OperatorType.EnemyLoc:
					NavigateToEnemy(nPCHumanContext);
					break;
				case OperatorType.LastEnemyLoc:
					NavigateToLastEnemy(nPCHumanContext);
					break;
				case OperatorType.RandomLoc:
					NavigateToRandomLoc(nPCHumanContext);
					break;
				case OperatorType.SpawnLoc:
					NavigateToSpawnLoc(nPCHumanContext);
					break;
				case OperatorType.FleeEnemy:
					FleeEnemy(nPCHumanContext);
					break;
				case OperatorType.FleeHurtDir:
					FleeHurtDir(nPCHumanContext);
					break;
				case OperatorType.RetreatCover:
					NavigateToCover(nPCHumanContext, TakeCoverIntention.Retreat);
					break;
				case OperatorType.FlankCover:
					NavigateToCover(nPCHumanContext, TakeCoverIntention.Flank);
					break;
				case OperatorType.AdvanceCover:
					NavigateToCover(nPCHumanContext, TakeCoverIntention.Advance);
					break;
				case OperatorType.FleeExplosive:
					FleeExplosive(nPCHumanContext);
					break;
				case OperatorType.Sidestep:
					Sidestep(nPCHumanContext);
					break;
				case OperatorType.ClosestCover:
					NavigateToCover(nPCHumanContext, TakeCoverIntention.Closest);
					break;
				case OperatorType.PatrolLoc:
					NavigateToPatrolLoc(nPCHumanContext);
					break;
				case OperatorType.MountableChair:
					NavigateToMountableLoc(nPCHumanContext, Operator);
					break;
				case OperatorType.WaypointLoc:
					NavigateToWaypointLoc(nPCHumanContext);
					break;
				case OperatorType.HideoutLoc:
					NavigateToHideout(nPCHumanContext);
					break;
				}
			}
		}

		public static void MakeUnstuck(NPCHumanContext c)
		{
			c.Human.stuckDuration = 0f;
			c.Human.IsStuck = false;
		}

		public static void NavigateToEnemy(NPCHumanContext c)
		{
			if (c.GetFact(NPCPlayerApex.Facts.HasEnemy) <= 0 || !c.AIAgent.IsNavRunning())
			{
				return;
			}
			if (c.GetFact(NPCPlayerApex.Facts.HasLineOfSight) > 0 && c.EnemyPosition.sqrMagnitude > 0f)
			{
				MakeUnstuck(c);
				c.Human.StoppingDistance = 1.5f;
				c.Human.Destination = c.EnemyPosition;
			}
			else
			{
				Memory.SeenInfo info = c.Memory.GetInfo(c.AIAgent.AttackTarget);
				if (info.Entity != null && info.Position.sqrMagnitude > 0f)
				{
					MakeUnstuck(c);
					c.Human.StoppingDistance = 1.5f;
					c.Human.Destination = info.Position;
				}
			}
			c.Human.SetTargetPathStatus();
		}

		public static void NavigateToLastEnemy(NPCHumanContext c)
		{
			if (c.AIAgent.AttackTarget != null && c.AIAgent.IsNavRunning())
			{
				Memory.SeenInfo info = c.Memory.GetInfo(c.AIAgent.AttackTarget);
				if (info.Entity != null && info.Position.sqrMagnitude > 0f)
				{
					BasePlayer basePlayer = c.AIAgent.AttackTarget.ToPlayer();
					if (basePlayer != null && (basePlayer.IsAdmin || basePlayer.IsDeveloper) && basePlayer.IsFlying)
					{
						SetHumanSpeed.Set(c, NPCPlayerApex.SpeedEnum.StandStill);
						return;
					}
					NavMeshHit hit;
					if (!NavMesh.SamplePosition(info.Position, out hit, 1f, c.AIAgent.GetNavAgent.areaMask))
					{
						SetHumanSpeed.Set(c, NPCPlayerApex.SpeedEnum.StandStill);
						return;
					}
					MakeUnstuck(c);
					c.Human.StoppingDistance = 1f;
					c.Human.Destination = hit.position;
					c.Human.SetTargetPathStatus();
				}
			}
			UpdateRoamTime(c);
		}

		public static void NavigateToHideout(NPCHumanContext c)
		{
			if (c.EnemyHideoutGuess != null && c.AIAgent.IsNavRunning() && c.EnemyHideoutGuess.Position.sqrMagnitude > 0f)
			{
				MakeUnstuck(c);
				c.Human.StoppingDistance = 1f;
				c.Human.Destination = c.EnemyHideoutGuess.Position;
				c.Human.SetTargetPathStatus();
			}
			UpdateRoamTime(c);
		}

		public static void NavigateToRandomLoc(NPCHumanContext c)
		{
			if (IsHumanRoamReady.Evaluate(c) && c.AIAgent.IsNavRunning() && NavigateInDirOfBestSample(c, NavPointSampler.SampleCount.Eight, 4f, NavPointSampler.SampleFeatures.DiscourageSharpTurns, c.AIAgent.GetStats.MinRoamRange, c.AIAgent.GetStats.MaxRoamRange))
			{
				UpdateRoamTime(c);
				if (c.Human.OnChatter != null)
				{
					c.Human.OnChatter();
				}
			}
		}

		public static void NavigateToPatrolLoc(NPCHumanContext c)
		{
			if (!(c.AiLocationManager == null) && IsHumanRoamReady.Evaluate(c) && c.AIAgent.IsNavRunning())
			{
				PathInterestNode randomPatrolPointInRange = c.AiLocationManager.GetRandomPatrolPointInRange(c.Position, c.AIAgent.GetStats.MinRoamRange, c.AIAgent.GetStats.MaxRoamRange, c.CurrentPatrolPoint);
				if (randomPatrolPointInRange != null && randomPatrolPointInRange.transform.position.sqrMagnitude > 0f)
				{
					MakeUnstuck(c);
					c.Human.Destination = randomPatrolPointInRange.transform.position;
					c.Human.SetTargetPathStatus();
					c.CurrentPatrolPoint = randomPatrolPointInRange;
				}
				UpdateRoamTime(c);
				if (c.Human.OnChatter != null)
				{
					c.Human.OnChatter();
				}
			}
		}

		public static void NavigateToSpawnLoc(NPCHumanContext c)
		{
			if (IsHumanRoamReady.Evaluate(c) && c.AIAgent.IsNavRunning() && c.Human.SpawnPosition.sqrMagnitude > 0f)
			{
				MakeUnstuck(c);
				c.Human.StoppingDistance = 0.1f;
				c.Human.Destination = c.Human.SpawnPosition;
				c.Human.SetTargetPathStatus();
				UpdateRoamTime(c);
			}
		}

		public static void NavigateToMountableLoc(NPCHumanContext c, OperatorType mountableType)
		{
			if (mountableType == OperatorType.MountableChair && ConVar.AI.npc_ignore_chairs)
			{
				return;
			}
			BaseMountable chairTarget = c.ChairTarget;
			if (!(chairTarget == null))
			{
				Vector3 position = chairTarget.transform.position;
				NavMeshHit hit;
				if (NavMesh.SamplePosition(position, out hit, 10f, c.Human.NavAgent.areaMask))
				{
					position = hit.position;
				}
				if (!Mathf.Approximately(position.sqrMagnitude, 0f))
				{
					MakeUnstuck(c);
					c.Human.StoppingDistance = 0.05f;
					c.Human.Destination = position;
					c.Human.SetTargetPathStatus();
				}
			}
		}

		private static void UpdateRoamTime(NPCHumanContext c)
		{
			float num = c.AIAgent.GetStats.MaxRoamDelay - c.AIAgent.GetStats.MinRoamDelay;
			float num2 = c.AIAgent.GetStats.RoamDelayDistribution.Evaluate(Random.value) * num;
			c.NextRoamTime = UnityEngine.Time.realtimeSinceStartup + c.AIAgent.GetStats.MinRoamDelay + num2;
		}

		private static void NavigateToWaypointLoc(NPCHumanContext c)
		{
			if (c.GetFact(NPCPlayerApex.Facts.HasWaypoints) <= 0 || !c.Human.IsNavRunning())
			{
				return;
			}
			c.Human.StoppingDistance = 0.3f;
			WaypointSet.Waypoint waypoint = c.Human.WaypointSet.Points[c.Human.CurrentWaypointIndex];
			bool flag = false;
			Vector3 position = waypoint.Transform.position;
			if ((c.Human.Destination - position).sqrMagnitude > 0.01f)
			{
				MakeUnstuck(c);
				c.Human.Destination = position;
				c.Human.SetTargetPathStatus();
				flag = true;
			}
			float num = 0f;
			int num2 = c.Human.PeekNextWaypointIndex();
			if (c.Human.WaypointSet.Points.Count > num2 && Mathf.Approximately(c.Human.WaypointSet.Points[num2].WaitTime, 0f))
			{
				num = 1f;
			}
			if ((c.Position - c.Human.Destination).sqrMagnitude > c.Human.SqrStoppingDistance + num)
			{
				c.Human.LookAtPoint = null;
				c.Human.LookAtEyes = null;
				if (c.GetFact(NPCPlayerApex.Facts.IsMoving) == 0 && !flag)
				{
					c.Human.CurrentWaypointIndex = c.Human.GetNextWaypointIndex();
					c.SetFact(NPCPlayerApex.Facts.IsMovingTowardWaypoint, 0);
				}
				else
				{
					c.SetFact(NPCPlayerApex.Facts.IsMovingTowardWaypoint, 1);
				}
			}
			else if (IsWaitingAtWaypoint(c, ref waypoint))
			{
				if (IsClosestPlayerWithinDistance.Test(c, 4f))
				{
					LookAtClosestPlayer.Do(c);
				}
				else
				{
					c.Human.LookAtEyes = null;
					c.Human.LookAtRandomPoint();
				}
				c.SetFact(NPCPlayerApex.Facts.IsMovingTowardWaypoint, 0);
			}
			else
			{
				c.Human.CurrentWaypointIndex = c.Human.GetNextWaypointIndex();
				c.Human.LookAtPoint = null;
			}
		}

		private static bool IsWaitingAtWaypoint(NPCHumanContext c, ref WaypointSet.Waypoint waypoint)
		{
			if (!c.Human.IsWaitingAtWaypoint && waypoint.WaitTime > 0f)
			{
				c.Human.WaypointDelayTime = UnityEngine.Time.time + waypoint.WaitTime;
				c.Human.IsWaitingAtWaypoint = true;
				c.SetFact(NPCPlayerApex.Facts.Speed, 0);
			}
			else
			{
				if (c.Human.IsWaitingAtWaypoint && UnityEngine.Time.time >= c.Human.WaypointDelayTime)
				{
					c.Human.IsWaitingAtWaypoint = false;
				}
				if (!c.Human.IsWaitingAtWaypoint)
				{
					return false;
				}
			}
			return true;
		}

		public static void NavigateToCover(NPCHumanContext c, TakeCoverIntention intention)
		{
			if (!c.AIAgent.IsNavRunning())
			{
				return;
			}
			c.Human.TimeLastMovedToCover = UnityEngine.Time.realtimeSinceStartup;
			switch (intention)
			{
			case TakeCoverIntention.Retreat:
				if (c.CoverSet.Retreat.ReservedCoverPoint != null)
				{
					PathToCover(c, c.CoverSet.Retreat.ReservedCoverPoint.Position);
					c.SetFact(NPCPlayerApex.Facts.IsRetreatingToCover, 1);
				}
				else if (c.CoverSet.Closest.ReservedCoverPoint != null)
				{
					PathToCover(c, c.CoverSet.Closest.ReservedCoverPoint.Position);
					c.SetFact(NPCPlayerApex.Facts.IsRetreatingToCover, 1);
				}
				break;
			case TakeCoverIntention.Flank:
				if (c.CoverSet.Flank.ReservedCoverPoint != null)
				{
					PathToCover(c, c.CoverSet.Flank.ReservedCoverPoint.Position);
					c.SetFact(NPCPlayerApex.Facts.IsRetreatingToCover, 1);
				}
				else if (c.CoverSet.Closest.ReservedCoverPoint != null)
				{
					PathToCover(c, c.CoverSet.Closest.ReservedCoverPoint.Position);
					c.SetFact(NPCPlayerApex.Facts.IsRetreatingToCover, 1);
				}
				break;
			case TakeCoverIntention.Advance:
				if (c.CoverSet.Advance.ReservedCoverPoint != null)
				{
					PathToCover(c, c.CoverSet.Advance.ReservedCoverPoint.Position);
				}
				else if (c.CoverSet.Closest.ReservedCoverPoint != null)
				{
					PathToCover(c, c.CoverSet.Closest.ReservedCoverPoint.Position);
				}
				break;
			default:
				if (c.CoverSet.Closest.ReservedCoverPoint != null)
				{
					PathToCover(c, c.CoverSet.Closest.ReservedCoverPoint.Position);
				}
				break;
			}
		}

		public static void PathToCover(NPCHumanContext c, Vector3 coverPosition)
		{
			if (coverPosition.sqrMagnitude > 0f)
			{
				MakeUnstuck(c);
				c.AIAgent.GetNavAgent.destination = coverPosition;
				c.Human.SetTargetPathStatus();
				c.SetFact(NPCPlayerApex.Facts.IsMovingToCover, 1);
				if (c.Human.OnTakeCover != null)
				{
					c.Human.OnTakeCover();
				}
			}
		}

		public static void FleeEnemy(NPCHumanContext c)
		{
			if (c.AIAgent.IsNavRunning() && NavigateInDirOfBestSample(c, NavPointSampler.SampleCount.Eight, 4f, NavPointSampler.SampleFeatures.RetreatFromTarget, c.AIAgent.GetStats.MinFleeRange, c.AIAgent.GetStats.MaxFleeRange))
			{
				c.SetFact(NPCPlayerApex.Facts.IsFleeing, 1);
			}
		}

		public static void FleeExplosive(NPCHumanContext c)
		{
			if (c.AIAgent.IsNavRunning() && NavigateInDirOfBestSample(c, NavPointSampler.SampleCount.Eight, 4f, NavPointSampler.SampleFeatures.RetreatFromExplosive, c.AIAgent.GetStats.MinFleeRange, c.AIAgent.GetStats.MaxFleeRange))
			{
				c.SetFact(NPCPlayerApex.Facts.IsFleeing, 1);
				if (c.Human.OnFleeExplosive != null)
				{
					c.Human.OnFleeExplosive();
				}
			}
		}

		public static void FleeHurtDir(NPCHumanContext c)
		{
			if (c.AIAgent.IsNavRunning() && NavigateInDirOfBestSample(c, NavPointSampler.SampleCount.Eight, 4f, NavPointSampler.SampleFeatures.RetreatFromDirection, c.AIAgent.GetStats.MinFleeRange, c.AIAgent.GetStats.MaxFleeRange))
			{
				c.SetFact(NPCPlayerApex.Facts.IsFleeing, 1);
			}
		}

		public static void Sidestep(NPCHumanContext c)
		{
			if (c.AIAgent.IsNavRunning())
			{
				c.Human.StoppingDistance = 0.1f;
				if (NavigateInDirOfBestSample(c, NavPointSampler.SampleCount.Eight, 4f, NavPointSampler.SampleFeatures.FlankTarget, 2f, 5f) && c.AIAgent.GetFact(NPCPlayerApex.Facts.IsInCover) == 1)
				{
					c.AIAgent.SetFact(NPCPlayerApex.Facts.SidesteppedOutOfCover, 1);
				}
			}
		}

		private static bool NavigateInDirOfBestSample(NPCHumanContext c, NavPointSampler.SampleCount sampleCount, float radius, NavPointSampler.SampleFeatures features, float minRange, float maxRange)
		{
			List<NavPointSample> list = c.AIAgent.RequestNavPointSamplesInCircle(sampleCount, radius, features);
			if (list == null)
			{
				return false;
			}
			foreach (NavPointSample item in list)
			{
				Vector3 normalized = (item.Position - c.Position).normalized;
				Vector3 vector = c.Position + (normalized * minRange + normalized * ((maxRange - minRange) * Random.value));
				if (!(c.AIAgent.AttackTarget != null) || NavPointSampler.IsValidPointDirectness(vector, c.Position, c.EnemyPosition))
				{
					NavPointSample navPointSample = NavPointSampler.SamplePoint(vector, new NavPointSampler.SampleScoreParams
					{
						WaterMaxDepth = c.AIAgent.GetStats.MaxWaterDepth,
						Agent = c.AIAgent,
						Features = features
					});
					if (!Mathf.Approximately(navPointSample.Score, 0f) && !Mathf.Approximately(navPointSample.Position.sqrMagnitude, 0f))
					{
						MakeUnstuck(c);
						vector = navPointSample.Position;
						c.AIAgent.GetNavAgent.destination = vector;
						c.Human.SetTargetPathStatus();
						c.AIAgent.SetFact(NPCPlayerApex.Facts.IsMoving, 1, true, false);
						return true;
					}
				}
			}
			return false;
		}
	}
}
