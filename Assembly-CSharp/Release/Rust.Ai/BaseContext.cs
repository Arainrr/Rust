using Apex.AI;
using System.Collections.Generic;
using UnityEngine;

namespace Rust.Ai
{
	public class BaseContext : IAIContext
	{
		public Memory Memory;

		public BasePlayer ClosestPlayer;

		public List<BasePlayer> Players = new List<BasePlayer>();

		public List<BaseNpc> Npcs = new List<BaseNpc>();

		public List<BasePlayer> PlayersBehindUs = new List<BasePlayer>();

		public List<BaseNpc> NpcsBehindUs = new List<BaseNpc>();

		public List<TimedExplosive> DeployedExplosives = new List<TimedExplosive>(1);

		public BasePlayer EnemyPlayer;

		public BaseNpc EnemyNpc;

		public float LastTargetScore;

		public float LastEnemyPlayerScore;

		public float LastEnemyNpcScore;

		public float NextRoamTime;

		public Vector3 lastSampledPosition
		{
			get;
			set;
		}

		public List<Vector3> sampledPositions
		{
			get;
			private set;
		}

		public IAIAgent AIAgent
		{
			get;
			private set;
		}

		public BaseCombatEntity Entity
		{
			get;
			private set;
		}

		public Vector3 Position
		{
			get
			{
				if (Entity.IsDestroyed || Entity.transform == null)
				{
					return Vector3.zero;
				}
				return Entity.ServerPosition;
			}
		}

		public Vector3 EnemyPosition
		{
			get
			{
				if (!(EnemyPlayer != null))
				{
					if (!(EnemyNpc != null))
					{
						return Vector3.zero;
					}
					return EnemyNpc.ServerPosition;
				}
				return EnemyPlayer.ServerPosition;
			}
		}

		public BaseContext(IAIAgent agent)
		{
			AIAgent = agent;
			Entity = agent.Entity;
			sampledPositions = new List<Vector3>();
			Memory = new Memory();
		}

		public byte GetFact(BaseNpc.Facts fact)
		{
			return AIAgent.GetFact(fact);
		}

		public void SetFact(BaseNpc.Facts fact, byte value)
		{
			AIAgent.SetFact(fact, value);
		}

		public byte GetFact(NPCPlayerApex.Facts fact)
		{
			return AIAgent.GetFact(fact);
		}

		public void SetFact(NPCPlayerApex.Facts fact, byte value, bool triggerCallback = true, bool onlyTriggerCallbackOnDiffValue = true)
		{
			AIAgent.SetFact(fact, value, triggerCallback, onlyTriggerCallbackOnDiffValue);
		}
	}
}
