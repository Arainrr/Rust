using ConVar;
using Rust.Ai.HTN.Reasoning;
using UnityEngine;

namespace Rust.Ai.HTN.ScientistJunkpile.Reasoners
{
	public class CoverPointsReasoner : INpcReasoner
	{
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

		public void Tick(IHTNAgent npc, float deltaTime, float time)
		{
			ScientistJunkpileContext scientistJunkpileContext = npc.AiDomain.NpcContext as ScientistJunkpileContext;
			if (scientistJunkpileContext == null)
			{
				return;
			}
			scientistJunkpileContext.SetFact(Facts.HasNearbyCover, (scientistJunkpileContext.CoverPoints.Count > 0) ? 1 : 0);
			if (!scientistJunkpileContext.IsFact(Facts.HasEnemyTarget))
			{
				scientistJunkpileContext.ReserveCoverPoint(null);
				return;
			}
			HTNPlayer hTNPlayer = npc as HTNPlayer;
			if (!(hTNPlayer == null))
			{
				float bestScore = 0f;
				float bestScore2 = 0f;
				float bestScore3 = 0f;
				foreach (CoverPoint coverPoint in scientistJunkpileContext.CoverPoints)
				{
					if (!coverPoint.IsCompromised && (!coverPoint.IsReserved || coverPoint.ReservedFor.EqualNetID(hTNPlayer)))
					{
						float arcThreshold = -0.8f;
						BaseNpcMemory.EnemyPlayerInfo enemyInfo = scientistJunkpileContext.Memory.PrimaryKnownEnemyPlayer;
						if (coverPoint.ProvidesCoverFromPoint(enemyInfo.LastKnownPosition, arcThreshold))
						{
							Vector3 dirCover = coverPoint.Position - npc.transform.position;
							Vector3 dirDanger = enemyInfo.LastKnownPosition - npc.transform.position;
							float directness = Vector3.Dot(dirCover.normalized, dirDanger.normalized);
							if (bestScore < 1f)
							{
								EvaluateAdvancement(npc, scientistJunkpileContext, ref bestScore, ref enemyInfo, coverPoint, dirCover, dirDanger, directness);
							}
							if (bestScore3 < 1f)
							{
								EvaluateRetreat(npc, scientistJunkpileContext, ref bestScore3, ref enemyInfo, coverPoint, dirCover, dirDanger, ref directness);
							}
							if (bestScore2 < 1f)
							{
								EvaluateFlanking(npc, scientistJunkpileContext, ref bestScore2, ref enemyInfo, coverPoint, dirCover, dirDanger, directness);
							}
						}
					}
				}
			}
		}

		private static bool EvaluateAdvancement(IHTNAgent npc, ScientistJunkpileContext c, ref float bestScore, ref BaseNpcMemory.EnemyPlayerInfo enemyInfo, CoverPoint option, Vector3 dirCover, Vector3 dirDanger, float directness)
		{
			if (directness >= 0.2f)
			{
				float sqrMagnitude = dirCover.sqrMagnitude;
				if (sqrMagnitude > dirDanger.sqrMagnitude || sqrMagnitude < 0.5f)
				{
					return false;
				}
				float sqrMagnitude2 = (option.Position - enemyInfo.LastKnownPosition).sqrMagnitude;
				float allowedCoverRangeSqr = c.Domain.GetAllowedCoverRangeSqr();
				float num = directness + (allowedCoverRangeSqr - sqrMagnitude) / allowedCoverRangeSqr + option.Score + ((sqrMagnitude2 < sqrMagnitude) ? 1f : 0f);
				if (num > bestScore)
				{
					if (ConVar.AI.npc_cover_use_path_distance && npc != null && !c.Domain.PathDistanceIsValid(enemyInfo.LastKnownPosition, option.Position))
					{
						return false;
					}
					if ((option.Position - enemyInfo.LastKnownPosition).sqrMagnitude < sqrMagnitude)
					{
						num *= 0.9f;
					}
					bestScore = num;
					c.BestAdvanceCover = option;
					return true;
				}
			}
			return false;
		}

		private static bool EvaluateRetreat(IHTNAgent npc, ScientistJunkpileContext c, ref float bestScore, ref BaseNpcMemory.EnemyPlayerInfo enemyInfo, CoverPoint option, Vector3 dirCover, Vector3 dirDanger, ref float directness)
		{
			float sqrMagnitude = dirCover.sqrMagnitude;
			if (directness <= -0.2f)
			{
				float allowedCoverRangeSqr = c.Domain.GetAllowedCoverRangeSqr();
				float num = directness * -1f + (allowedCoverRangeSqr - sqrMagnitude) / allowedCoverRangeSqr + option.Score;
				if (num > bestScore)
				{
					bestScore = num;
					c.BestRetreatCover = option;
					return true;
				}
			}
			return false;
		}

		private static bool EvaluateFlanking(IHTNAgent npc, ScientistJunkpileContext c, ref float bestScore, ref BaseNpcMemory.EnemyPlayerInfo enemyInfo, CoverPoint option, Vector3 dirCover, Vector3 dirDanger, float directness)
		{
			if (directness > -0.2f && directness < 0.2f)
			{
				float sqrMagnitude = dirCover.sqrMagnitude;
				float allowedCoverRangeSqr = c.Domain.GetAllowedCoverRangeSqr();
				float num = (0.2f - Mathf.Abs(directness)) / 0.2f + (allowedCoverRangeSqr - sqrMagnitude) / allowedCoverRangeSqr + option.Score;
				if (num > bestScore)
				{
					if (ConVar.AI.npc_cover_use_path_distance && npc != null && !c.Domain.PathDistanceIsValid(enemyInfo.LastKnownPosition, option.Position))
					{
						return false;
					}
					bestScore = 0.1f - Mathf.Abs(num);
					c.BestFlankCover = option;
					return true;
				}
			}
			return false;
		}
	}
}
