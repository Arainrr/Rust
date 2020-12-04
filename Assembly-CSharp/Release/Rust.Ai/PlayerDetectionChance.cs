using Apex.AI;
using Apex.Serialization;
using UnityEngine;

namespace Rust.Ai
{
	public class PlayerDetectionChance : OptionScorerBase<BasePlayer>
	{
		[ApexSerialization]
		private float score = 10f;

		public override float Score(IAIContext context, BasePlayer option)
		{
			PlayerTargetContext playerTargetContext = context as PlayerTargetContext;
			if (playerTargetContext != null)
			{
				if (!Evaluate(playerTargetContext.Self, playerTargetContext.Dot[playerTargetContext.CurrentOptionsIndex], option))
				{
					return 0f;
				}
				return score;
			}
			return 0f;
		}

		public static bool Evaluate(IAIAgent self, float dot, BasePlayer option)
		{
			NPCPlayerApex nPCPlayerApex = self as NPCPlayerApex;
			if (nPCPlayerApex != null)
			{
				if (Time.time > nPCPlayerApex.NextDetectionCheck)
				{
					nPCPlayerApex.NextDetectionCheck = Time.time + 2f;
					bool flag = Random.value < FovDetection(dot, option);
					bool flag2 = Random.value < NoiseLevel(option);
					bool flag3 = Random.value < LightDetection(option);
					nPCPlayerApex.LastDetectionCheckResult = (flag | flag2 | flag3);
				}
				return nPCPlayerApex.LastDetectionCheckResult;
			}
			return true;
		}

		private static float FovDetection(float dot, BasePlayer option)
		{
			return ((dot >= 0.75f) ? 1.5f : ((dot + 1f) * 0.5f)) * (option.IsRunning() ? 1.5f : 1f) * (option.IsDucked() ? 0.75f : 1f);
		}

		private static float NoiseLevel(BasePlayer option)
		{
			float num = option.IsDucked() ? 0.5f : 1f;
			num *= (option.IsRunning() ? 1.5f : 1f);
			num *= ((option.estimatedSpeed <= 0.01f) ? 0.1f : 1f);
			if (option.inventory.containerWear.itemList.Count == 0)
			{
				return num * 0.1f;
			}
			return num + (float)option.inventory.containerWear.itemList.Count * 0.025f;
		}

		private static float LightDetection(BasePlayer option)
		{
			bool flag = false;
			Item activeItem = option.GetActiveItem();
			if (activeItem != null)
			{
				HeldEntity heldEntity = activeItem.GetHeldEntity() as HeldEntity;
				if (heldEntity != null)
				{
					flag = heldEntity.LightsOn();
				}
			}
			if (!flag)
			{
				return 0f;
			}
			return 0.1f;
		}
	}
}
