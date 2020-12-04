using Oxide.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rust.Ai.HTN
{
	[Serializable]
	public class BaseNpcMemory
	{
		[Serializable]
		private struct FailedDestinationInfo
		{
			public float Time;

			public Vector3 Destination;
		}

		[Serializable]
		public struct EnemyPlayerInfo
		{
			public float Time;

			public NpcPlayerInfo PlayerInfo;

			public Vector3 LastKnownLocalPosition;

			public Vector3 LastKnownLocalHeading;

			public Vector3 OurLastLocalPositionWhenLastSeen;

			public bool BodyVisibleWhenLastNoticed;

			public bool HeadVisibleWhenLastNoticed;

			public Vector3 LastKnownPosition
			{
				get
				{
					if (PlayerInfo.Player != null)
					{
						BaseEntity parentEntity = PlayerInfo.Player.GetParentEntity();
						if (parentEntity != null)
						{
							return parentEntity.transform.TransformPoint(LastKnownLocalPosition);
						}
					}
					return LastKnownLocalPosition;
				}
			}

			public Vector3 LastKnownHeading
			{
				get
				{
					if (PlayerInfo.Player != null)
					{
						BaseEntity parentEntity = PlayerInfo.Player.GetParentEntity();
						if (parentEntity != null)
						{
							return parentEntity.transform.TransformDirection(LastKnownLocalHeading);
						}
					}
					return LastKnownLocalHeading;
				}
			}

			public Vector3 OurLastPositionWhenLastSeen
			{
				get
				{
					if (PlayerInfo.Player != null)
					{
						BaseEntity parentEntity = PlayerInfo.Player.GetParentEntity();
						if (parentEntity != null)
						{
							return parentEntity.transform.TransformPoint(OurLastLocalPositionWhenLastSeen);
						}
					}
					return OurLastLocalPositionWhenLastSeen;
				}
			}
		}

		[Serializable]
		public struct EntityOfInterestInfo
		{
			public float Time;

			public BaseEntity Entity;
		}

		[ReadOnly]
		public bool HasTargetDestination;

		[ReadOnly]
		public Vector3 TargetDestination;

		[ReadOnly]
		private readonly List<FailedDestinationInfo> _failedDestinationMemory = new List<FailedDestinationInfo>(10);

		[ReadOnly]
		public EnemyPlayerInfo PrimaryKnownEnemyPlayer;

		[ReadOnly]
		public List<EnemyPlayerInfo> KnownEnemyPlayers = new List<EnemyPlayerInfo>(10);

		[ReadOnly]
		public List<EntityOfInterestInfo> KnownEntitiesOfInterest = new List<EntityOfInterestInfo>(10);

		[ReadOnly]
		public List<EntityOfInterestInfo> KnownTimedExplosives = new List<EntityOfInterestInfo>(10);

		[ReadOnly]
		public AnimalInfo PrimaryKnownAnimal;

		[ReadOnly]
		public Vector3 LastClosestEdgeNormal;

		[NonSerialized]
		public BaseNpcContext NpcContext;

		public virtual BaseNpcDefinition Definition => null;

		public BaseNpcMemory(BaseNpcContext context)
		{
			NpcContext = context;
		}

		public virtual void ResetState()
		{
			HasTargetDestination = false;
			_failedDestinationMemory?.Clear();
			PrimaryKnownEnemyPlayer.PlayerInfo.Player = null;
			KnownEnemyPlayers?.Clear();
			KnownEntitiesOfInterest?.Clear();
			PrimaryKnownAnimal.Animal = null;
			LastClosestEdgeNormal = Vector3.zero;
		}

		public bool IsValid(Vector3 destination)
		{
			foreach (FailedDestinationInfo item in _failedDestinationMemory)
			{
				if ((item.Destination - destination).sqrMagnitude <= 0.1f)
				{
					return false;
				}
			}
			return true;
		}

		public void AddFailedDestination(Vector3 destination)
		{
			for (int i = 0; i < _failedDestinationMemory.Count; i++)
			{
				FailedDestinationInfo value = _failedDestinationMemory[i];
				if ((value.Destination - destination).sqrMagnitude <= 0.1f)
				{
					value.Time = Time.time;
					_failedDestinationMemory[i] = value;
					return;
				}
			}
			_failedDestinationMemory.Add(new FailedDestinationInfo
			{
				Time = Time.time,
				Destination = destination
			});
		}

		public void ForgetPrimiaryEnemyPlayer()
		{
			PrimaryKnownEnemyPlayer.PlayerInfo.Player = null;
		}

		public void ForgetPrimiaryAnimal()
		{
			PrimaryKnownAnimal.Animal = null;
		}

		public void RememberPrimaryAnimal(BaseNpc animal)
		{
			if (Interface.CallHook("OnNpcTarget", this, animal) != null)
			{
				return;
			}
			int num = 0;
			AnimalInfo primaryKnownAnimal;
			while (true)
			{
				if (num < NpcContext.AnimalsInRange.Count)
				{
					primaryKnownAnimal = NpcContext.AnimalsInRange[num];
					if (primaryKnownAnimal.Animal == animal)
					{
						break;
					}
					num++;
					continue;
				}
				return;
			}
			PrimaryKnownAnimal = primaryKnownAnimal;
		}

		public void RememberPrimaryEnemyPlayer(BasePlayer primaryTarget)
		{
			int num = 0;
			EnemyPlayerInfo info;
			while (true)
			{
				if (num < KnownEnemyPlayers.Count)
				{
					info = KnownEnemyPlayers[num];
					if (info.PlayerInfo.Player == primaryTarget)
					{
						break;
					}
					num++;
					continue;
				}
				return;
			}
			OnSetPrimaryKnownEnemyPlayer(ref info);
		}

		protected virtual void OnSetPrimaryKnownEnemyPlayer(ref EnemyPlayerInfo info)
		{
			PrimaryKnownEnemyPlayer = info;
		}

		public void RememberEnemyPlayer(IHTNAgent npc, ref NpcPlayerInfo info, float time, float uncertainty = 0f, string debugStr = "ENEMY!")
		{
			if (info.Player == null || info.Player.transform == null || info.Player.IsDestroyed || info.Player.IsDead() || info.Player.IsWounded() || Interface.CallHook("OnNpcTarget", npc.Body, info.Player) != null)
			{
				return;
			}
			if (Mathf.Approximately(info.SqrDistance, 0f))
			{
				info.SqrDistance = (npc.BodyPosition - info.Player.transform.position).sqrMagnitude;
			}
			for (int i = 0; i < KnownEnemyPlayers.Count; i++)
			{
				EnemyPlayerInfo enemyPlayerInfo = KnownEnemyPlayers[i];
				if (enemyPlayerInfo.PlayerInfo.Player == info.Player)
				{
					enemyPlayerInfo.PlayerInfo = info;
					if (uncertainty < 0.05f)
					{
						enemyPlayerInfo.LastKnownLocalPosition = info.Player.transform.localPosition;
						enemyPlayerInfo.LastKnownLocalHeading = info.Player.GetLocalVelocity().normalized;
						enemyPlayerInfo.OurLastLocalPositionWhenLastSeen = npc.transform.localPosition;
						enemyPlayerInfo.BodyVisibleWhenLastNoticed = info.BodyVisible;
						enemyPlayerInfo.HeadVisibleWhenLastNoticed = info.HeadVisible;
					}
					else
					{
						Vector2 vector = UnityEngine.Random.insideUnitCircle * uncertainty;
						enemyPlayerInfo.LastKnownLocalPosition = info.Player.transform.localPosition + new Vector3(vector.x, 0f, vector.y);
						enemyPlayerInfo.LastKnownLocalHeading = (enemyPlayerInfo.LastKnownPosition - NpcContext.BodyPosition).normalized;
						enemyPlayerInfo.BodyVisibleWhenLastNoticed = info.BodyVisible;
						enemyPlayerInfo.HeadVisibleWhenLastNoticed = info.HeadVisible;
					}
					enemyPlayerInfo.Time = time;
					KnownEnemyPlayers[i] = enemyPlayerInfo;
					if (PrimaryKnownEnemyPlayer.PlayerInfo.Player == info.Player)
					{
						PrimaryKnownEnemyPlayer = enemyPlayerInfo;
					}
					return;
				}
			}
			KnownEnemyPlayers.Add(new EnemyPlayerInfo
			{
				PlayerInfo = info,
				LastKnownLocalPosition = info.Player.transform.localPosition,
				Time = time
			});
		}

		public void RememberEntityOfInterest(IHTNAgent npc, BaseEntity entityOfInterest, float time, string debugStr)
		{
			TimedExplosive timedExplosive = entityOfInterest as TimedExplosive;
			if (timedExplosive != null)
			{
				RememberTimedExplosives(npc, timedExplosive, time, "EXPLOSIVE!");
			}
			bool flag = false;
			for (int i = 0; i < KnownEntitiesOfInterest.Count; i++)
			{
				EntityOfInterestInfo value = KnownEntitiesOfInterest[i];
				if (value.Entity == null)
				{
					KnownEntitiesOfInterest.RemoveAt(i);
					i--;
				}
				else if (value.Entity == entityOfInterest)
				{
					value.Time = time;
					KnownEntitiesOfInterest[i] = value;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				KnownEntitiesOfInterest.Add(new EntityOfInterestInfo
				{
					Entity = entityOfInterest,
					Time = time
				});
			}
		}

		public void RememberTimedExplosives(IHTNAgent npc, TimedExplosive explosive, float time, string debugStr)
		{
			bool flag = false;
			for (int i = 0; i < KnownTimedExplosives.Count; i++)
			{
				EntityOfInterestInfo value = KnownTimedExplosives[i];
				if (value.Entity == null)
				{
					KnownTimedExplosives.RemoveAt(i);
					i--;
				}
				else if (value.Entity == explosive)
				{
					value.Time = time;
					KnownTimedExplosives[i] = value;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				KnownTimedExplosives.Add(new EntityOfInterestInfo
				{
					Entity = explosive,
					Time = time
				});
			}
		}

		protected virtual void OnForget(BasePlayer player)
		{
		}

		public void Forget(float memoryTimeout)
		{
			float time = Time.time;
			for (int i = 0; i < _failedDestinationMemory.Count; i++)
			{
				FailedDestinationInfo failedDestinationInfo = _failedDestinationMemory[i];
				if (time - failedDestinationInfo.Time > memoryTimeout)
				{
					_failedDestinationMemory.RemoveAt(i);
					i--;
				}
			}
			for (int j = 0; j < KnownEnemyPlayers.Count; j++)
			{
				EnemyPlayerInfo enemyPlayerInfo = KnownEnemyPlayers[j];
				float num = time - enemyPlayerInfo.Time;
				if (num > memoryTimeout)
				{
					KnownEnemyPlayers.RemoveAt(j);
					j--;
					if (enemyPlayerInfo.PlayerInfo.Player != null)
					{
						OnForget(enemyPlayerInfo.PlayerInfo.Player);
						if (PrimaryKnownEnemyPlayer.PlayerInfo.Player == enemyPlayerInfo.PlayerInfo.Player)
						{
							ForgetPrimiaryEnemyPlayer();
						}
					}
				}
				else if (PrimaryKnownEnemyPlayer.PlayerInfo.Player == enemyPlayerInfo.PlayerInfo.Player)
				{
					PrimaryKnownEnemyPlayer.PlayerInfo.AudibleScore *= 1f - num / memoryTimeout;
					PrimaryKnownEnemyPlayer.PlayerInfo.VisibilityScore *= 1f - num / memoryTimeout;
				}
			}
			for (int k = 0; k < KnownEntitiesOfInterest.Count; k++)
			{
				EntityOfInterestInfo entityOfInterestInfo = KnownEntitiesOfInterest[k];
				if (time - entityOfInterestInfo.Time > memoryTimeout)
				{
					KnownEntitiesOfInterest.RemoveAt(k);
					k--;
				}
			}
			if (PrimaryKnownAnimal.Animal != null && time - PrimaryKnownAnimal.Time > memoryTimeout)
			{
				PrimaryKnownAnimal.Animal = null;
			}
		}

		public virtual bool ShouldRemoveOnPlayerForgetTimeout(float time, NpcPlayerInfo player)
		{
			if (player.Player == null || player.Player.transform == null || player.Player.IsDestroyed || player.Player.IsDead() || player.Player.IsWounded())
			{
				return true;
			}
			if (time > player.Time + (Definition?.Memory.ForgetInRangeTime ?? 0f))
			{
				return true;
			}
			return false;
		}
	}
}
