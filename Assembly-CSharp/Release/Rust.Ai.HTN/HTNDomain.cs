using Apex.AI;
using Apex.AI.Components;
using Apex.AI.Core.HTN;
using Apex.Ai.HTN;
using Rust.Ai.HTN.Reasoning;
using Rust.Ai.HTN.Sensors;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Rust.Ai.HTN
{
	public abstract class HTNDomain : MonoBehaviour, IHTNDomain, IContextProvider, IDisposable
	{
		public enum MovementRule
		{
			NeverMove,
			RestrainedMove,
			FreeMove
		}

		[ReadOnly]
		public MovementRule Movement = MovementRule.FreeMove;

		[ReadOnly]
		public float MovementRadius = -1f;

		private Vector3 _currentOffset = Vector3.zero;

		public float SqrMovementRadius => MovementRadius * MovementRadius;

		public abstract BaseNpcContext NpcContext
		{
			get;
		}

		public abstract IHTNContext PlannerContext
		{
			get;
		}

		public abstract IUtilityAI PlannerAi
		{
			get;
		}

		public abstract IUtilityAIClient PlannerAiClient
		{
			get;
		}

		public abstract NavMeshAgent NavAgent
		{
			get;
		}

		public abstract List<INpcSensor> Sensors
		{
			get;
		}

		public abstract List<INpcReasoner> Reasoners
		{
			get;
		}

		public byte[] WorldState => PlannerContext.WorldState;

		public byte[] PreviousWorldState => PlannerContext.PreviousWorldState;

		public Stack<PrimitiveTaskSelector> Plan => PlannerContext.HtnPlan;

		public void TickPlan()
		{
			if (PlannerContext.PlanState != PlanStateType.Running)
			{
				return;
			}
			if (PlannerContext.CurrentTask == null)
			{
				PlannerContext.CurrentTask = PlannerContext.HtnPlan.Pop();
			}
			else if (PlannerContext.CurrentTask.State == PrimitiveTaskStateType.Complete)
			{
				if (PlannerContext.HtnPlan.Count <= 0)
				{
					CompletePlan();
					Think();
					return;
				}
				PlannerContext.CurrentTask = PlannerContext.HtnPlan.Pop();
			}
			if (!PlannerContext.CurrentTask.ValidatePreconditions(PlannerContext))
			{
				AbortPlan();
				Think();
			}
			else
			{
				if (PlannerContext.CurrentTask == null)
				{
					return;
				}
				if (PlannerContext.CurrentTask.State == PrimitiveTaskStateType.NotStarted)
				{
					if (TaskQualifier.TestPreconditions(PlannerContext.CurrentTask, PlannerContext) <= 0f)
					{
						PlannerContext.CurrentTask.State = PrimitiveTaskStateType.Aborted;
						AbortPlan();
						Think();
						return;
					}
					PlannerContext.CurrentTask.State = PrimitiveTaskStateType.Running;
					foreach (IOperator @operator in PlannerContext.CurrentTask.Operators)
					{
						@operator.Execute(PlannerContext);
					}
				}
				int num = 0;
				using (List<IOperator>.Enumerator enumerator = PlannerContext.CurrentTask.Operators.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						switch (enumerator.Current.Tick(PlannerContext, PlannerContext.CurrentTask))
						{
						case OperatorStateType.Aborted:
							PlannerContext.CurrentTask.State = PrimitiveTaskStateType.Aborted;
							AbortPlan();
							Think();
							return;
						case OperatorStateType.Complete:
							num++;
							break;
						}
					}
				}
				if (num >= PlannerContext.CurrentTask.Operators.Count)
				{
					PlannerContext.CurrentTask.State = PrimitiveTaskStateType.Complete;
				}
			}
		}

		protected virtual void AbortPlan()
		{
			PlannerContext.HtnPlan.Clear();
			PlannerContext.PlanState = PlanStateType.Aborted;
			PlannerContext.DecompositionScore = int.MaxValue;
			PlannerContext.CurrentTask = null;
		}

		protected virtual void CompletePlan()
		{
			PlannerContext.PlanState = PlanStateType.Complete;
			PlannerContext.DecompositionScore = int.MaxValue;
			PlannerContext.CurrentTask = null;
		}

		public void TickReasoners(float time)
		{
			for (int i = 0; i < Reasoners.Count; i++)
			{
				INpcReasoner npcReasoner = Reasoners[i];
				float deltaTime = time - npcReasoner.LastTickTime;
				if (CanTickReasoner(deltaTime, npcReasoner))
				{
					TickReasoner(npcReasoner, deltaTime, time);
					npcReasoner.LastTickTime = time + UnityEngine.Random.value * 0.075f;
				}
			}
		}

		protected virtual bool CanTickReasoner(float deltaTime, INpcReasoner reasoner)
		{
			return deltaTime >= reasoner.TickFrequency;
		}

		protected abstract void TickReasoner(INpcReasoner reasoner, float deltaTime, float time);

		public void TickSensors(float time)
		{
			for (int i = 0; i < Sensors.Count; i++)
			{
				INpcSensor npcSensor = Sensors[i];
				float deltaTime = time - npcSensor.LastTickTime;
				if (CanTickSensor(deltaTime, npcSensor))
				{
					TickSensor(npcSensor, deltaTime, time);
					npcSensor.LastTickTime = time + UnityEngine.Random.value * 0.075f;
				}
			}
		}

		protected virtual bool CanTickSensor(float deltaTime, INpcSensor sensor)
		{
			return deltaTime >= sensor.TickFrequency;
		}

		protected abstract void TickSensor(INpcSensor sensor, float deltaTime, float time);

		public abstract IAIContext GetContext(Guid aiId);

		public abstract void Initialize(BaseEntity body);

		public abstract void Dispose();

		public abstract void TickDestinationTracker();

		public abstract void Resume();

		public abstract void Pause();

		public abstract Vector3 GetNextPosition(float delta);

		public abstract void ForceProjectileOrientation();

		public void Think()
		{
			PlannerContext.IsWorldStateDirty = false;
			PlannerAiClient.Execute();
			if ((PlannerContext.PlanResult == PlanResultType.FoundNewPlan || PlannerContext.PlanResult == PlanResultType.ReplacedPlan) && PlannerContext.CurrentTask != null)
			{
				foreach (IOperator @operator in PlannerContext.CurrentTask.Operators)
				{
					@operator?.Abort(PlannerContext, PlannerContext.CurrentTask);
				}
				PlannerContext.CurrentTask = null;
			}
		}

		public virtual void Tick(float time)
		{
			TickSensors(time);
			TickReasoners(time);
			TickPlan();
		}

		public virtual void ResetState()
		{
			NpcContext.ResetState();
		}

		public abstract Vector3 GetHeadingDirection();

		public abstract Vector3 GetHomeDirection();

		public Vector3 GetLookAroundDirection(float deltaTime)
		{
			return GetHeadingDirection() + _currentOffset;
		}

		public virtual void OnPreHurt(HitInfo info)
		{
		}

		public abstract void OnHurt(HitInfo info);

		public abstract void OnSensation(Sensation sensation);

		public abstract float SqrDistanceToSpawn();

		public abstract bool AllowedMovementDestination(Vector3 destination);
	}
}
