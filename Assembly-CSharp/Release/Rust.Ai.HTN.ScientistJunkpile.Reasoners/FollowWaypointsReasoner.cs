using Rust.Ai.HTN.Reasoning;
using System;
using UnityEngine;
using UnityEngine.AI;

namespace Rust.Ai.HTN.ScientistJunkpile.Reasoners
{
	public class FollowWaypointsReasoner : INpcReasoner
	{
		private bool isFollowingWaypoints;

		private bool isFirstTick = true;

		private bool hasAlreadyPassedOnPrevCheck;

		public float TickFrequency
		{
			get;
			set;
		}

		public float LastTickTime
		{
			get;
			set;
		}

		private WaypointSet WaypointSet
		{
			get;
			set;
		}

		private int WaypointDirection
		{
			get;
			set;
		}

		private bool IsWaitingAtWaypoint
		{
			get;
			set;
		}

		private int CurrentWaypointIndex
		{
			get;
			set;
		}

		private float WaypointDelayTime
		{
			get;
			set;
		}

		public void Tick(IHTNAgent npc, float deltaTime, float time)
		{
			ScientistJunkpileContext scientistJunkpileContext = npc.AiDomain.NpcContext as ScientistJunkpileContext;
			if (scientistJunkpileContext == null || scientistJunkpileContext.Domain.NavAgent == null || scientistJunkpileContext.Location == null || scientistJunkpileContext.Location.PatrolPointGroup == null)
			{
				return;
			}
			if (WaypointSet == null)
			{
				WaypointSet = scientistJunkpileContext.Location.PatrolPointGroup.GetComponent<WaypointSet>();
			}
			if (WaypointSet == null || WaypointSet.Points.Count == 0)
			{
				return;
			}
			if (scientistJunkpileContext.IsFact(Facts.IsReturningHome) || scientistJunkpileContext.IsFact(Facts.HasEnemyTarget) || scientistJunkpileContext.IsFact(Facts.NearbyAnimal) || !scientistJunkpileContext.IsFact(Facts.IsUsingTool))
			{
				isFollowingWaypoints = false;
				hasAlreadyPassedOnPrevCheck = false;
				return;
			}
			if (!isFollowingWaypoints)
			{
				if ((!hasAlreadyPassedOnPrevCheck && (scientistJunkpileContext.GetPreviousFact(Facts.HasEnemyTarget) == 1 || scientistJunkpileContext.GetPreviousFact(Facts.NearbyAnimal) == 1)) || isFirstTick)
				{
					CurrentWaypointIndex = GetClosestWaypointIndex(scientistJunkpileContext.BodyPosition);
					if (isFirstTick)
					{
						isFirstTick = false;
					}
					else
					{
						hasAlreadyPassedOnPrevCheck = true;
					}
				}
				WaypointSet.Waypoint waypoint = WaypointSet.Points[CurrentWaypointIndex];
				if (waypoint.Transform == null)
				{
					CurrentWaypointIndex = GetNextWaypointIndex();
					isFollowingWaypoints = false;
					return;
				}
				Vector3 position = waypoint.Transform.position;
				NavMeshHit hit;
				if ((scientistJunkpileContext.Memory.TargetDestination - position).sqrMagnitude > 0.1f && NavMesh.SamplePosition(position + Vector3.up * 2f, out hit, 4f, scientistJunkpileContext.Domain.NavAgent.areaMask))
				{
					waypoint.Transform.position = hit.position;
					scientistJunkpileContext.Domain.SetDestination(hit.position);
					isFollowingWaypoints = true;
					scientistJunkpileContext.SetFact(Facts.IsNavigating, true);
					return;
				}
			}
			float num = 2f;
			float num2 = scientistJunkpileContext.Domain.NavAgent.stoppingDistance * scientistJunkpileContext.Domain.NavAgent.stoppingDistance;
			if ((scientistJunkpileContext.BodyPosition - scientistJunkpileContext.Memory.TargetDestination).sqrMagnitude <= num2 + num)
			{
				CurrentWaypointIndex = GetNextWaypointIndex();
				isFollowingWaypoints = false;
			}
		}

		public int PeekNextWaypointIndex()
		{
			if (WaypointSet == null || WaypointSet.Points.Count == 0)
			{
				return CurrentWaypointIndex;
			}
			int num = CurrentWaypointIndex;
			switch (WaypointSet.NavMode)
			{
			case WaypointSet.NavModes.Loop:
				num++;
				if (num >= WaypointSet.Points.Count)
				{
					num = 0;
				}
				else if (num < 0)
				{
					num = WaypointSet.Points.Count - 1;
				}
				break;
			case WaypointSet.NavModes.PingPong:
				num += WaypointDirection;
				if (num >= WaypointSet.Points.Count)
				{
					num = CurrentWaypointIndex - 1;
				}
				else if (num < 0)
				{
					num = 0;
				}
				break;
			}
			return num;
		}

		public int GetNextWaypointIndex()
		{
			if (WaypointSet == null || WaypointSet.Points.Count == 0 || WaypointSet.Points[PeekNextWaypointIndex()].IsOccupied)
			{
				return CurrentWaypointIndex;
			}
			int currentWaypointIndex = CurrentWaypointIndex;
			if (currentWaypointIndex >= 0 && currentWaypointIndex < WaypointSet.Points.Count)
			{
				WaypointSet.Waypoint value = WaypointSet.Points[currentWaypointIndex];
				value.IsOccupied = false;
				WaypointSet.Points[currentWaypointIndex] = value;
			}
			switch (WaypointSet.NavMode)
			{
			case WaypointSet.NavModes.Loop:
				currentWaypointIndex++;
				if (currentWaypointIndex >= WaypointSet.Points.Count)
				{
					currentWaypointIndex = 0;
				}
				else if (currentWaypointIndex < 0)
				{
					currentWaypointIndex = WaypointSet.Points.Count - 1;
				}
				break;
			case WaypointSet.NavModes.PingPong:
				currentWaypointIndex += WaypointDirection;
				if (currentWaypointIndex >= WaypointSet.Points.Count)
				{
					currentWaypointIndex = CurrentWaypointIndex - 1;
					WaypointDirection = -1;
				}
				else if (currentWaypointIndex < 0)
				{
					currentWaypointIndex = 0;
					WaypointDirection = 1;
				}
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
			if (currentWaypointIndex >= 0 && currentWaypointIndex < WaypointSet.Points.Count)
			{
				WaypointSet.Waypoint value2 = WaypointSet.Points[currentWaypointIndex];
				value2.IsOccupied = true;
				WaypointSet.Points[currentWaypointIndex] = value2;
			}
			return currentWaypointIndex;
		}

		public int GetClosestWaypointIndex(Vector3 position)
		{
			if (WaypointSet == null || WaypointSet.Points.Count == 0)
			{
				return CurrentWaypointIndex;
			}
			WaypointSet.Waypoint value = WaypointSet.Points[CurrentWaypointIndex];
			value.IsOccupied = false;
			WaypointSet.Points[CurrentWaypointIndex] = value;
			float num = float.MaxValue;
			int num2 = -1;
			for (int i = 0; i < WaypointSet.Points.Count; i++)
			{
				WaypointSet.Waypoint waypoint = WaypointSet.Points[i];
				if (!(waypoint.Transform == null))
				{
					float sqrMagnitude = (waypoint.Transform.position - position).sqrMagnitude;
					if (sqrMagnitude < num)
					{
						num = sqrMagnitude;
						num2 = i;
					}
				}
			}
			if (num2 >= 0)
			{
				return num2;
			}
			return CurrentWaypointIndex;
		}
	}
}
