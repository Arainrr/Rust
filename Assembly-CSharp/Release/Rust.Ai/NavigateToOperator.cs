using Apex.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace Rust.Ai
{
	public class NavigateToOperator : BaseAction
	{
		public enum OperatorType
		{
			EnemyLoc,
			RandomLoc,
			Spawn,
			FoodLoc,
			FleeEnemy,
			FleeHurtDir,
			TopologyPreference
		}

		[ApexSerialization]
		public OperatorType Operator;

		public override void DoExecute(BaseContext c)
		{
			if (c.GetFact(BaseNpc.Facts.CanNotMove) == 1)
			{
				c.AIAgent.StopMoving();
				c.SetFact(BaseNpc.Facts.PathToTargetStatus, 2);
			}
			else if (c.AIAgent.IsNavRunning() && !c.AIAgent.GetNavAgent.pathPending)
			{
				switch (Operator)
				{
				case OperatorType.EnemyLoc:
					NavigateToEnemy(c);
					break;
				case OperatorType.RandomLoc:
					NavigateToRandomLoc(c);
					break;
				case OperatorType.Spawn:
					NavigateToSpawn(c);
					break;
				case OperatorType.FoodLoc:
					NavigateToFood(c);
					break;
				case OperatorType.FleeEnemy:
					FleeEnemy(c);
					break;
				case OperatorType.FleeHurtDir:
					FleeHurtDir(c);
					break;
				case OperatorType.TopologyPreference:
					NavigateToTopologyPreference(c);
					break;
				}
			}
		}

		public static void MakeUnstuck(BaseContext c)
		{
			BaseNpc baseNpc = c.Entity as BaseNpc;
			if ((bool)baseNpc)
			{
				baseNpc.stuckDuration = 0f;
				baseNpc.IsStuck = false;
			}
		}

		public static void NavigateToEnemy(BaseContext c)
		{
			if (c.GetFact(BaseNpc.Facts.HasEnemy) > 0 && c.AIAgent.IsNavRunning())
			{
				MakeUnstuck(c);
				c.AIAgent.Destination = c.EnemyPosition;
				c.AIAgent.SetTargetPathStatus();
			}
		}

		public static void NavigateToRandomLoc(BaseContext c)
		{
			if (IsRoamReady.Evaluate(c) && c.AIAgent.IsNavRunning())
			{
				if (NavigateInDirOfBestSample(c, NavPointSampler.SampleCount.Eight, 4f, NavPointSampler.SampleFeatures.DiscourageSharpTurns | NavPointSampler.SampleFeatures.RangeFromSpawn, c.AIAgent.GetStats.MinRoamRange, c.AIAgent.GetStats.MaxRoamRange))
				{
					float num = c.AIAgent.GetStats.MaxRoamDelay - c.AIAgent.GetStats.MinRoamDelay;
					float time = Random.value * num / num;
					float num2 = c.AIAgent.GetStats.RoamDelayDistribution.Evaluate(time) * num;
					c.NextRoamTime = Time.realtimeSinceStartup + c.AIAgent.GetStats.MinRoamDelay + num2;
				}
				else
				{
					NavigateToSpawn(c);
				}
			}
		}

		public static void NavigateToTopologyPreference(BaseContext c)
		{
			if (IsRoamReady.Evaluate(c) && c.AIAgent.IsNavRunning())
			{
				if (NavigateInDirOfBestSample(c, NavPointSampler.SampleCount.Eight, 4f, NavPointSampler.SampleFeatures.DiscourageSharpTurns | NavPointSampler.SampleFeatures.TopologyPreference | NavPointSampler.SampleFeatures.RangeFromSpawn, c.AIAgent.GetStats.MinRoamRange, c.AIAgent.GetStats.MaxRoamRange))
				{
					float num = c.AIAgent.GetStats.MaxRoamDelay - c.AIAgent.GetStats.MinRoamDelay;
					float time = Random.value * num / num;
					float num2 = c.AIAgent.GetStats.RoamDelayDistribution.Evaluate(time) * num;
					c.NextRoamTime = Time.realtimeSinceStartup + c.AIAgent.GetStats.MinRoamDelay + num2;
				}
				else
				{
					NavigateToRandomLoc(c);
				}
			}
		}

		public static void NavigateToSpawn(BaseContext c)
		{
			if (c.AIAgent.IsNavRunning())
			{
				MakeUnstuck(c);
				c.AIAgent.Destination = c.AIAgent.SpawnPosition;
				c.AIAgent.SetTargetPathStatus();
			}
		}

		public static void NavigateToFood(BaseContext c)
		{
			if (c.AIAgent.FoodTarget != null && !c.AIAgent.FoodTarget.IsDestroyed && c.AIAgent.FoodTarget.transform != null && c.GetFact(BaseNpc.Facts.FoodRange) < 2 && c.AIAgent.IsNavRunning())
			{
				MakeUnstuck(c);
				c.AIAgent.Destination = c.AIAgent.FoodTarget.ServerPosition;
				c.AIAgent.SetTargetPathStatus();
			}
		}

		public static void FleeEnemy(BaseContext c)
		{
			if (c.AIAgent.IsNavRunning() && NavigateInDirOfBestSample(c, NavPointSampler.SampleCount.Eight, 4f, NavPointSampler.SampleFeatures.RetreatFromTarget, c.AIAgent.GetStats.MinFleeRange, c.AIAgent.GetStats.MaxFleeRange))
			{
				c.SetFact(BaseNpc.Facts.IsFleeing, 1);
			}
		}

		public static void FleeHurtDir(BaseContext c)
		{
			if (c.AIAgent.IsNavRunning() && NavigateInDirOfBestSample(c, NavPointSampler.SampleCount.Eight, 4f, NavPointSampler.SampleFeatures.RetreatFromDirection, c.AIAgent.GetStats.MinFleeRange, c.AIAgent.GetStats.MaxFleeRange))
			{
				c.SetFact(BaseNpc.Facts.IsFleeing, 1);
			}
		}

		private static bool NavigateInDirOfBestSample(BaseContext c, NavPointSampler.SampleCount sampleCount, float radius, NavPointSampler.SampleFeatures features, float minRange, float maxRange)
		{
			List<NavPointSample> list = c.AIAgent.RequestNavPointSamplesInCircle(sampleCount, radius, features);
			if (list == null)
			{
				return false;
			}
			foreach (NavPointSample item in list)
			{
				Vector3 normalized = (item.Position - c.Position).normalized;
				Vector3 pos = c.Position + (normalized * minRange + normalized * ((maxRange - minRange) * Random.value));
				NavPointSample navPointSample = NavPointSampler.SamplePoint(pos, new NavPointSampler.SampleScoreParams
				{
					WaterMaxDepth = c.AIAgent.GetStats.MaxWaterDepth,
					Agent = c.AIAgent,
					Features = features
				});
				if (!Mathf.Approximately(navPointSample.Score, 0f))
				{
					MakeUnstuck(c);
					pos = navPointSample.Position;
					c.AIAgent.Destination = pos;
					c.AIAgent.SetTargetPathStatus();
					return true;
				}
			}
			float num = 2f;
			list = c.AIAgent.RequestNavPointSamplesInCircleWaterDepthOnly(sampleCount, radius, num);
			if (list == null)
			{
				return false;
			}
			foreach (NavPointSample item2 in list)
			{
				Vector3 normalized2 = (item2.Position - c.Position).normalized;
				Vector3 pos2 = c.Position + (normalized2 * minRange + normalized2 * ((maxRange - minRange) * Random.value));
				NavPointSample navPointSample2 = NavPointSampler.SamplePointWaterDepthOnly(pos2, num);
				if (!Mathf.Approximately(navPointSample2.Score, 0f))
				{
					MakeUnstuck(c);
					pos2 = navPointSample2.Position;
					c.AIAgent.Destination = pos2;
					c.AIAgent.SetTargetPathStatus();
					return true;
				}
			}
			return false;
		}
	}
}
