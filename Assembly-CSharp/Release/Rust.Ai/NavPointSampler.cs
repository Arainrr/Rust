using ConVar;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Rust.Ai
{
	public static class NavPointSampler
	{
		public enum SampleCount
		{
			Four,
			Eight,
			Sixteen
		}

		[Flags]
		public enum SampleFeatures
		{
			None = 0x0,
			DiscourageSharpTurns = 0x1,
			RetreatFromTarget = 0x2,
			ApproachTarget = 0x4,
			FlankTarget = 0x8,
			RetreatFromDirection = 0x10,
			RetreatFromExplosive = 0x20,
			TopologyPreference = 0x40,
			RangeFromSpawn = 0x80
		}

		public struct SampleScoreParams
		{
			public float WaterMaxDepth;

			public IAIAgent Agent;

			public SampleFeatures Features;
		}

		private const float HalfPI = (float)Math.PI / 180f;

		private static readonly NavPointSampleComparer NavPointSampleComparer = new NavPointSampleComparer();

		public static bool SampleCircleWaterDepthOnly(SampleCount sampleCount, Vector3 center, float radius, SampleScoreParams scoreParams, ref List<NavPointSample> samples)
		{
			if (scoreParams.Agent == null || scoreParams.Agent.GetNavAgent == null)
			{
				return false;
			}
			float num = 90f;
			switch (sampleCount)
			{
			case SampleCount.Eight:
				num = 45f;
				break;
			case SampleCount.Sixteen:
				num = 22.5f;
				break;
			}
			for (float num2 = 0f; num2 < 360f; num2 += num)
			{
				NavPointSample item = SamplePointWaterDepthOnly(GetPointOnCircle(center, radius, num2), 2f);
				if (item.Score > 0f)
				{
					samples.Add(item);
				}
			}
			if (samples.Count > 0)
			{
				samples.Sort(NavPointSampleComparer);
			}
			return samples.Count > 0;
		}

		public static bool SampleCircle(SampleCount sampleCount, Vector3 center, float radius, SampleScoreParams scoreParams, ref List<NavPointSample> samples)
		{
			if (scoreParams.Agent == null || scoreParams.Agent.GetNavAgent == null)
			{
				return false;
			}
			float num = 90f;
			switch (sampleCount)
			{
			case SampleCount.Eight:
				num = 45f;
				break;
			case SampleCount.Sixteen:
				num = 22.5f;
				break;
			}
			float num2 = 2f + (float)GetFeatureCount((int)scoreParams.Features);
			for (float num3 = 0f; num3 < 360f; num3 += num)
			{
				NavPointSample item = SamplePoint(GetPointOnCircle(center, radius, num3), scoreParams);
				if (item.Score > 0f)
				{
					samples.Add(item);
					if (item.Score >= num2)
					{
						break;
					}
				}
			}
			if (samples.Count == 0)
			{
				for (float num4 = 0f; num4 < 360f; num4 += num)
				{
					NavPointSample item2 = SamplePointWaterDepthOnly(GetPointOnCircle(center, radius, num4), 2f);
					if (item2.Score > 0f)
					{
						samples.Add(item2);
					}
				}
			}
			if (samples.Count > 0)
			{
				samples.Sort(NavPointSampleComparer);
			}
			return samples.Count > 0;
		}

		public static int GetFeatureCount(int features)
		{
			int num = 0;
			while (features != 0)
			{
				features &= features - 1;
				num++;
			}
			return num;
		}

		public static Vector3 GetPointOnCircle(Vector3 center, float radius, float degrees)
		{
			return new Vector3(center.x + radius * Mathf.Cos(degrees * ((float)Math.PI / 180f)), z: center.z + radius * Mathf.Sin(degrees * ((float)Math.PI / 180f)), y: center.y);
		}

		public static NavPointSample SamplePointWaterDepthOnly(Vector3 pos, float depth)
		{
			if (TerrainMeta.HeightMap != null)
			{
				pos.y = TerrainMeta.HeightMap.GetHeight(pos);
			}
			float score = _WaterDepth(pos, 2f) * 2f;
			NavPointSample result = default(NavPointSample);
			result.Position = pos;
			result.Score = score;
			return result;
		}

		public static NavPointSample SamplePoint(Vector3 pos, SampleScoreParams scoreParams)
		{
			if (TerrainMeta.HeightMap != null)
			{
				pos.y = TerrainMeta.HeightMap.GetHeight(pos);
			}
			float num = _WaterDepth(pos, scoreParams.WaterMaxDepth) * 2f;
			if (num > 0f && _SampleNavMesh(ref pos, scoreParams.Agent))
			{
				if ((scoreParams.Features & SampleFeatures.DiscourageSharpTurns) > SampleFeatures.None)
				{
					num += _DiscourageSharpTurns(pos, scoreParams.Agent);
				}
				if ((scoreParams.Features & SampleFeatures.RetreatFromTarget) > SampleFeatures.None)
				{
					num += RetreatPointValue(pos, scoreParams.Agent);
				}
				if ((scoreParams.Features & SampleFeatures.ApproachTarget) > SampleFeatures.None)
				{
					num += ApproachPointValue(pos, scoreParams.Agent);
				}
				if ((scoreParams.Features & SampleFeatures.FlankTarget) > SampleFeatures.None)
				{
					num += FlankPointValue(pos, scoreParams.Agent);
				}
				if ((scoreParams.Features & SampleFeatures.RetreatFromDirection) > SampleFeatures.None)
				{
					num += RetreatFromDirection(pos, scoreParams.Agent);
				}
				if ((scoreParams.Features & SampleFeatures.RetreatFromExplosive) > SampleFeatures.None)
				{
					num += RetreatPointValue(pos, scoreParams.Agent);
				}
				if ((scoreParams.Features & SampleFeatures.TopologyPreference) > SampleFeatures.None)
				{
					num += TopologyPreference(pos, scoreParams.Agent);
				}
				if ((scoreParams.Features & SampleFeatures.RangeFromSpawn) > SampleFeatures.None)
				{
					num *= RangeFromHome(pos, scoreParams.Agent);
				}
			}
			NavPointSample result = default(NavPointSample);
			result.Position = pos;
			result.Score = num;
			return result;
		}

		private static bool _SampleNavMesh(ref Vector3 pos, IAIAgent agent)
		{
			NavMeshHit hit;
			if (NavMesh.SamplePosition(pos, out hit, agent.GetNavAgent.height * 2f, agent.GetNavAgent.areaMask))
			{
				pos = hit.position;
				return true;
			}
			return false;
		}

		private static float _WaterDepth(Vector3 pos, float maxDepth)
		{
			float waterDepth = WaterLevel.GetWaterDepth(pos);
			if (Mathf.Approximately(waterDepth, 0f))
			{
				return 1f;
			}
			waterDepth = Mathf.Min(waterDepth, maxDepth);
			return 1f - waterDepth / maxDepth;
		}

		private static float _DiscourageSharpTurns(Vector3 pos, IAIAgent agent)
		{
			Vector3 normalized = (pos - agent.Entity.ServerPosition).normalized;
			float num = Vector3.Dot(agent.Entity.transform.forward, normalized);
			if (num > 0.45f)
			{
				return 1f;
			}
			if (num > 0f)
			{
				return num;
			}
			return 0f;
		}

		public static bool IsValidPointDirectness(Vector3 point, Vector3 pos, Vector3 targetPos)
		{
			Vector3 vector = pos - targetPos;
			Vector3 vector2 = pos - point;
			if (Vector3.Dot(vector.normalized, vector2.normalized) > 0.5f && vector2.sqrMagnitude > vector.sqrMagnitude)
			{
				return false;
			}
			return true;
		}

		public static bool PointDirectnessToTarget(Vector3 point, Vector3 pos, Vector3 targetPos, out float value)
		{
			Vector3 vector = point - pos;
			Vector3 vector2 = targetPos - pos;
			value = Vector3.Dot(vector.normalized, vector2.normalized);
			if (value > 0.5f && vector.sqrMagnitude > vector2.sqrMagnitude)
			{
				value = 0f;
				return false;
			}
			return true;
		}

		public static float RetreatPointValue(Vector3 point, IAIAgent agent)
		{
			if (agent.AttackTarget == null)
			{
				return 0f;
			}
			float value = 0f;
			if (!PointDirectnessToTarget(point, agent.Entity.ServerPosition, agent.AttackTarget.ServerPosition, out value))
			{
				return 0f;
			}
			if (value <= -0.5f)
			{
				return value * -1f;
			}
			return 0f;
		}

		public static float RetreatPointValueExplosive(Vector3 point, IAIAgent agent)
		{
			BaseContext baseContext = agent.GetContext(Guid.Empty) as BaseContext;
			if (baseContext == null || baseContext.DeployedExplosives.Count == 0 || baseContext.DeployedExplosives[0] == null || baseContext.DeployedExplosives[0].IsDestroyed)
			{
				return 0f;
			}
			float value = 0f;
			if (!PointDirectnessToTarget(point, agent.Entity.ServerPosition, baseContext.DeployedExplosives[0].ServerPosition, out value))
			{
				return 0f;
			}
			if (value <= -0.5f)
			{
				return value * -1f;
			}
			return 0f;
		}

		public static float ApproachPointValue(Vector3 point, IAIAgent agent)
		{
			if (agent.AttackTarget == null)
			{
				return 0f;
			}
			float value = 0f;
			if (!PointDirectnessToTarget(point, agent.Entity.ServerPosition, agent.AttackTarget.ServerPosition, out value))
			{
				return 0f;
			}
			if (value >= 0.5f)
			{
				return value;
			}
			return 0f;
		}

		public static float FlankPointValue(Vector3 point, IAIAgent agent)
		{
			if (agent.AttackTarget == null)
			{
				return 0f;
			}
			float value = 0f;
			if (!PointDirectnessToTarget(point, agent.Entity.ServerPosition, agent.AttackTarget.ServerPosition, out value))
			{
				return 0f;
			}
			if (value >= -0.1f && value <= 0.1f)
			{
				return 1f;
			}
			return 0f;
		}

		public static float RetreatFromDirection(Vector3 point, IAIAgent agent)
		{
			if (agent.Entity.LastAttackedDir == Vector3.zero)
			{
				return 0f;
			}
			Vector3 normalized = (point - agent.Entity.ServerPosition).normalized;
			if (Vector3.Dot(agent.Entity.LastAttackedDir, normalized) > -0.5f)
			{
				return 0f;
			}
			return 1f;
		}

		public static float TopologyPreference(Vector3 point, IAIAgent agent)
		{
			if (TerrainMeta.TopologyMap != null)
			{
				int topology = TerrainMeta.TopologyMap.GetTopology(point);
				if ((agent.TopologyPreference() & topology) > 0)
				{
					return 1f;
				}
			}
			return 0f;
		}

		public static float RangeFromHome(Vector3 point, IAIAgent agent)
		{
			float sqrMagnitude = (point - agent.SpawnPosition).sqrMagnitude;
			float num = agent.GetStats.MaxRoamRange * ConVar.AI.npc_max_roam_multiplier;
			if (sqrMagnitude > num)
			{
				return 0f;
			}
			return 1f;
		}
	}
}
