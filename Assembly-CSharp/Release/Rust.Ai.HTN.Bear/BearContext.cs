using Apex.Ai.HTN;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rust.Ai.HTN.Bear
{
	public class BearContext : BaseNpcContext, IDisposable
	{
		public delegate void WorldStateChangedEvent(BearContext context, Facts fact, byte oldValue, byte newValue);

		[SerializeField]
		[ReadOnly]
		public bool _isWorldStateDirty;

		[SerializeField]
		private byte[] _worldState;

		[ReadOnly]
		[SerializeField]
		private byte[] _previousWorldState;

		[SerializeField]
		[ReadOnly]
		private int _decompositionScore;

		[SerializeField]
		[ReadOnly]
		private List<PrimitiveTaskSelector> _debugPlan;

		private static Stack<WorldStateInfo>[] _worldStateChanges;

		public WorldStateChangedEvent OnWorldStateChangedEvent;

		[ReadOnly]
		public bool HasVisitedLastKnownEnemyPlayerLocation;

		[ReadOnly]
		public HTNAnimal Body;

		[ReadOnly]
		public BearDomain Domain;

		[ReadOnly]
		public BearMemory Memory;

		public override PlanResultType PlanResult
		{
			get;
			set;
		}

		public override PlanStateType PlanState
		{
			get;
			set;
		}

		public override Stack<PrimitiveTaskSelector> HtnPlan
		{
			get;
			set;
		} = new Stack<PrimitiveTaskSelector>();


		public override Dictionary<Guid, Stack<Apex.Ai.HTN.IEffect>> AppliedEffects
		{
			get;
			set;
		} = new Dictionary<Guid, Stack<Apex.Ai.HTN.IEffect>>();


		public override Dictionary<Guid, Stack<Apex.Ai.HTN.IEffect>> AppliedExpectedEffects
		{
			get;
			set;
		} = new Dictionary<Guid, Stack<Apex.Ai.HTN.IEffect>>();


		public override bool IsWorldStateDirty
		{
			get
			{
				return _isWorldStateDirty;
			}
			set
			{
				_isWorldStateDirty = value;
			}
		}

		public override byte[] WorldState => _worldState;

		public override byte[] PreviousWorldState => _previousWorldState;

		public override Stack<WorldStateInfo>[] WorldStateChanges => _worldStateChanges;

		public override int DecompositionScore
		{
			get
			{
				return _decompositionScore;
			}
			set
			{
				_decompositionScore = value;
			}
		}

		public override PrimitiveTaskSelector CurrentTask
		{
			get;
			set;
		}

		public override List<PrimitiveTaskSelector> DebugPlan => _debugPlan;

		public override NpcOrientation OrientationType
		{
			get;
			set;
		}

		public override List<NpcPlayerInfo> PlayersInRange
		{
			get;
		} = new List<NpcPlayerInfo>();


		public override List<NpcPlayerInfo> EnemyPlayersInRange
		{
			get;
		} = new List<NpcPlayerInfo>();


		public override List<NpcPlayerInfo> EnemyPlayersInLineOfSight
		{
			get;
		} = new List<NpcPlayerInfo>();


		public override List<NpcPlayerInfo> EnemyPlayersAudible
		{
			get;
		} = new List<NpcPlayerInfo>();


		public override List<NpcPlayerInfo> PlayersOutsideDetectionRange
		{
			get;
		} = new List<NpcPlayerInfo>();


		public override NpcPlayerInfo PrimaryEnemyPlayerInLineOfSight
		{
			get;
			set;
		}

		public override NpcPlayerInfo PrimaryEnemyPlayerAudible
		{
			get;
			set;
		}

		public override List<AnimalInfo> AnimalsInRange
		{
			get;
		} = new List<AnimalInfo>();


		public override Vector3 BodyPosition => Body.transform.position;

		public override BaseNpcMemory BaseMemory => Memory;

		public override NpcPlayerInfo GetPrimaryEnemyPlayerTarget()
		{
			if (PrimaryEnemyPlayerInLineOfSight.Player != null)
			{
				return PrimaryEnemyPlayerInLineOfSight;
			}
			return default(NpcPlayerInfo);
		}

		public override bool HasPrimaryEnemyPlayerTarget()
		{
			return GetPrimaryEnemyPlayerTarget().Player != null;
		}

		public override Vector3 GetDirectionToPrimaryEnemyPlayerTargetBody()
		{
			NpcPlayerInfo primaryEnemyPlayerTarget = GetPrimaryEnemyPlayerTarget();
			if (primaryEnemyPlayerTarget.Player != null)
			{
				Vector3 b = Vector3.zero;
				if (primaryEnemyPlayerTarget.Player.IsDucked())
				{
					b = PlayerEyes.DuckOffset;
				}
				if (primaryEnemyPlayerTarget.Player.IsSleeping())
				{
					b = Vector3.down;
				}
				Vector3 a = primaryEnemyPlayerTarget.Player.CenterPoint() + b;
				Vector3 b2 = Body.CenterPoint();
				return (a - b2).normalized;
			}
			return Body.transform.forward;
		}

		public override Vector3 GetDirectionToAnimal()
		{
			AnimalInfo primaryKnownAnimal = Memory.PrimaryKnownAnimal;
			if (primaryKnownAnimal.Animal != null)
			{
				Vector3 zero = Vector3.zero;
				Vector3 a = primaryKnownAnimal.Animal.CenterPoint() + zero;
				Vector3 b = Body.CenterPoint();
				return (a - b).normalized;
			}
			return Body.transform.forward;
		}

		public override Vector3 GetDirectionToPrimaryEnemyPlayerTargetHead()
		{
			NpcPlayerInfo primaryEnemyPlayerTarget = GetPrimaryEnemyPlayerTarget();
			if (primaryEnemyPlayerTarget.Player != null)
			{
				Vector3 position = primaryEnemyPlayerTarget.Player.eyes.position;
				Vector3 b = Body.CenterPoint();
				return (position - b).normalized;
			}
			return Body.transform.forward;
		}

		public override Vector3 GetDirectionToMemoryOfPrimaryEnemyPlayerTarget()
		{
			BaseNpcMemory.EnemyPlayerInfo primaryKnownEnemyPlayer = Memory.PrimaryKnownEnemyPlayer;
			if (primaryKnownEnemyPlayer.PlayerInfo.Player != null)
			{
				Vector3 b = Body.CenterPoint();
				float d = b.y - Body.transform.position.y;
				Vector3 vector = primaryKnownEnemyPlayer.LastKnownPosition + primaryKnownEnemyPlayer.PlayerInfo.Player.transform.up * d - b;
				if (vector.sqrMagnitude < 2f)
				{
					return primaryKnownEnemyPlayer.LastKnownHeading;
				}
				return vector.normalized;
			}
			return Body.transform.forward;
		}

		public override Vector3 GetDirectionLookAround()
		{
			BaseNpcMemory.EnemyPlayerInfo primaryKnownEnemyPlayer = Memory.PrimaryKnownEnemyPlayer;
			if (primaryKnownEnemyPlayer.PlayerInfo.Player != null)
			{
				return (primaryKnownEnemyPlayer.PlayerInfo.Player.CenterPoint() - Body.CenterPoint()).normalized;
			}
			return Body.transform.forward;
		}

		public override Vector3 GetDirectionLastAttackedDir()
		{
			if (Body.lastAttacker != null)
			{
				return Body.LastAttackedDir;
			}
			return Body.transform.forward;
		}

		public override Vector3 GetDirectionAudibleTarget()
		{
			NpcPlayerInfo npcPlayerInfo = default(NpcPlayerInfo);
			foreach (NpcPlayerInfo item in EnemyPlayersAudible)
			{
				if (item.AudibleScore > npcPlayerInfo.AudibleScore)
				{
					npcPlayerInfo = item;
				}
			}
			if (npcPlayerInfo.Player != null)
			{
				Vector3 a = npcPlayerInfo.Player.CenterPoint();
				Vector3 b = Body.CenterPoint();
				return (a - b).normalized;
			}
			return Body.transform.forward;
		}

		public override void StartDomainDecomposition()
		{
		}

		public override void ResetState()
		{
			base.ResetState();
			Memory.ResetState();
			IsWorldStateDirty = false;
			PlanState = PlanStateType.NoPlan;
			PlanResult = PlanResultType.NoPlan;
			HtnPlan.Clear();
			AppliedEffects.Clear();
			AppliedExpectedEffects.Clear();
			DecompositionScore = int.MaxValue;
			CurrentTask = null;
			HasVisitedLastKnownEnemyPlayerLocation = false;
			OrientationType = NpcOrientation.Heading;
			PlayersInRange.Clear();
			EnemyPlayersInRange.Clear();
			EnemyPlayersAudible.Clear();
			EnemyPlayersInLineOfSight.Clear();
			PrimaryEnemyPlayerAudible = default(NpcPlayerInfo);
			PrimaryEnemyPlayerInLineOfSight = default(NpcPlayerInfo);
			for (int i = 0; i < _worldState.Length; i++)
			{
				_worldState[i] = 0;
				_previousWorldState[i] = 0;
			}
		}

		public BearContext(HTNAnimal body, BearDomain domain)
		{
			int length = Enum.GetValues(typeof(Facts)).Length;
			if (_worldState == null || _worldState.Length != length)
			{
				_worldState = new byte[length];
				_previousWorldState = new byte[length];
				if (_worldStateChanges == null)
				{
					_worldStateChanges = new Stack<WorldStateInfo>[length];
					for (int i = 0; i < length; i++)
					{
						_worldStateChanges[i] = new Stack<WorldStateInfo>(5);
					}
				}
			}
			_decompositionScore = int.MaxValue;
			Body = body;
			Domain = domain;
			PlanState = PlanStateType.NoPlan;
			if (Memory == null || Memory.BearContext != this)
			{
				Memory = new BearMemory(this);
			}
		}

		public void Dispose()
		{
		}

		public bool IsBodyAlive()
		{
			if (Body != null && Body.transform != null && !Body.IsDestroyed)
			{
				return !Body.IsDead();
			}
			return false;
		}

		public void IncrementFact(Facts fact, int value, bool invokeChangedEvent = true, bool setAsDirty = true, bool checkValueDiff = true)
		{
			SetFact(fact, GetFact(fact) + value, invokeChangedEvent, setAsDirty, checkValueDiff);
		}

		public void IncrementFact(Facts fact, byte value, bool invokeChangedEvent = true, bool setAsDirty = true, bool checkValueDiff = true)
		{
			SetFact(fact, GetFact(fact) + value, invokeChangedEvent, setAsDirty, checkValueDiff);
		}

		public void SetFact(Facts fact, EnemyRange value, bool invokeChangedEvent = true, bool setAsDirty = true, bool checkValueDiff = true)
		{
			SetFact(fact, (byte)value, invokeChangedEvent, setAsDirty, checkValueDiff);
		}

		public void SetFact(Facts fact, HealthState value, bool invokeChangedEvent = true, bool setAsDirty = true, bool checkValueDiff = true)
		{
			SetFact(fact, (byte)value, invokeChangedEvent, setAsDirty, checkValueDiff);
		}

		public void SetFact(Facts fact, bool value, bool invokeChangedEvent = true, bool setAsDirty = true, bool checkValueDiff = true)
		{
			SetFact(fact, (byte)(value ? 1 : 0), invokeChangedEvent, setAsDirty, checkValueDiff);
		}

		public void SetFact(Facts fact, int value, bool invokeChangedEvent = true, bool setAsDirty = true, bool checkValueDiff = true)
		{
			SetFact(fact, (byte)value, invokeChangedEvent, setAsDirty, checkValueDiff);
		}

		public void SetFact(Facts fact, byte value, bool invokeChangedEvent = true, bool setAsDirty = true, bool checkValueDiff = true)
		{
			if (!checkValueDiff || _worldState[(uint)fact] != value)
			{
				if (setAsDirty)
				{
					IsWorldStateDirty = true;
				}
				_previousWorldState[(uint)fact] = _worldState[(uint)fact];
				_worldState[(uint)fact] = value;
				if (invokeChangedEvent)
				{
					OnWorldStateChangedEvent?.Invoke(this, fact, _previousWorldState[(uint)fact], value);
				}
			}
		}

		public byte GetFact(Facts fact)
		{
			return _worldState[(uint)fact];
		}

		public byte GetPreviousFact(Facts fact)
		{
			return _previousWorldState[(uint)fact];
		}

		public override void SetFact(byte fact, byte value, bool invokeChangedEvent = true, bool setAsDirty = true, bool checkValueDiff = true)
		{
			SetFact((Facts)fact, value, invokeChangedEvent, setAsDirty, checkValueDiff);
		}

		public override byte GetFact(byte fact)
		{
			return GetFact((Facts)fact);
		}

		public bool IsFact(Facts fact)
		{
			return GetFact(fact) > 0;
		}

		public void PushFactChangeDuringPlanning(Facts fact, HealthState value, bool temporary)
		{
			PushFactChangeDuringPlanning((byte)fact, (byte)value, temporary);
		}

		public void PushFactChangeDuringPlanning(Facts fact, bool value, bool temporary)
		{
			PushFactChangeDuringPlanning((byte)fact, (byte)(value ? 1 : 0), temporary);
		}

		public void PushFactChangeDuringPlanning(Facts fact, int value, bool temporary)
		{
			PushFactChangeDuringPlanning((byte)fact, (byte)value, temporary);
		}

		public void PushFactChangeDuringPlanning(Facts fact, byte value, bool temporary)
		{
			PushFactChangeDuringPlanning((byte)fact, value, temporary);
		}

		public void PushFactChangeDuringPlanning(byte fact, byte value, bool temporary)
		{
			_worldStateChanges[fact].Push(new WorldStateInfo
			{
				Value = value,
				Temporary = temporary
			});
		}

		public void PopFactChangeDuringPlanning(Facts fact)
		{
			PopFactChangeDuringPlanning((byte)fact);
		}

		public void PopFactChangeDuringPlanning(byte fact)
		{
			if (_worldStateChanges[fact].Count > 0)
			{
				_worldStateChanges[fact].Pop();
			}
		}

		public byte PeekFactChangeDuringPlanning(Facts fact)
		{
			return PeekFactChangeDuringPlanning((byte)fact);
		}

		public byte PeekFactChangeDuringPlanning(byte fact)
		{
			if (_worldStateChanges[fact].Count > 0)
			{
				return _worldStateChanges[fact].Peek().Value;
			}
			return 0;
		}

		public byte GetWorldState(Facts fact)
		{
			return GetWorldState((byte)fact);
		}
	}
}
