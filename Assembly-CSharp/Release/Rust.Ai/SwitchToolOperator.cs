using Apex.Serialization;
using ConVar;
using UnityEngine;

namespace Rust.Ai
{
	public class SwitchToolOperator : BaseAction
	{
		public class HasCurrentToolType : BaseScorer
		{
			[ApexSerialization(defaultValue = NPCPlayerApex.ToolTypeEnum.None)]
			public NPCPlayerApex.ToolTypeEnum value;

			public override float GetScore(BaseContext c)
			{
				if ((uint)c.GetFact(NPCPlayerApex.Facts.CurrentToolType) != (uint)value)
				{
					return 0f;
				}
				return 1f;
			}
		}

		public class TargetVisibleFor : BaseScorer
		{
			[ApexSerialization]
			public float duration;

			public override float GetScore(BaseContext c)
			{
				return Test(c as NPCHumanContext, duration);
			}

			public static float Test(NPCHumanContext c, float duration)
			{
				if (c == null)
				{
					return 0f;
				}
				if (c.Human.AttackTarget != null && c.Human.lastAttacker == c.Human.AttackTarget && c.Human.SecondsSinceAttacked < 10f)
				{
					return 1f;
				}
				if (!(c.Human.AttackTargetVisibleFor >= duration))
				{
					return 0f;
				}
				return 1f;
			}
		}

		public class ReactiveAimsAtTarget : BaseScorer
		{
			public override float GetScore(BaseContext c)
			{
				return Test(c as NPCHumanContext);
			}

			public static float Test(NPCHumanContext c)
			{
				if (c.Human == null || c.Human.transform == null || c.Human.IsDestroyed || c.Human.AttackTarget == null || c.Human.AttackTarget.transform == null || c.Human.AttackTarget.IsDestroyed)
				{
					return 0f;
				}
				Vector3 normalized = (c.Human.AttackTarget.ServerPosition - c.Position).normalized;
				float num = Vector3.Dot(c.Human.eyes.BodyForward(), normalized);
				if (c.Human.isMounted)
				{
					if (!(num >= ConVar.AI.npc_valid_mounted_aim_cone))
					{
						return 0f;
					}
					return 1f;
				}
				if (!(num >= ConVar.AI.npc_valid_aim_cone))
				{
					return 0f;
				}
				return 1f;
			}
		}

		[ApexSerialization]
		private NPCPlayerApex.ToolTypeEnum ToolTypeDay;

		[ApexSerialization]
		private NPCPlayerApex.ToolTypeEnum ToolTypeNight;

		public override void DoExecute(BaseContext c)
		{
			TrySwitchToolTo(c as NPCHumanContext, ToolTypeDay, ToolTypeNight);
		}

		public static bool TrySwitchToolTo(NPCHumanContext c, NPCPlayerApex.ToolTypeEnum toolDay, NPCPlayerApex.ToolTypeEnum toolNight)
		{
			if (c != null)
			{
				Item item = null;
				uint svActiveItemID = c.Human.svActiveItemID;
				if (TOD_Sky.Instance != null)
				{
					item = ((!TOD_Sky.Instance.IsDay) ? FindTool(c, toolNight) : FindTool(c, toolDay));
				}
				if (item != null)
				{
					c.Human.UpdateActiveItem(item.uid);
					if (svActiveItemID != c.Human.svActiveItemID)
					{
						c.Human.NextToolSwitchTime = UnityEngine.Time.realtimeSinceStartup + c.Human.ToolSwitchFrequency;
						c.SetFact(NPCPlayerApex.Facts.CurrentWeaponType, 0);
						c.SetFact(NPCPlayerApex.Facts.CurrentToolType, (byte)c.Human.GetCurrentToolTypeEnum());
					}
					return true;
				}
			}
			return false;
		}

		public static Item FindTool(NPCHumanContext c, NPCPlayerApex.ToolTypeEnum tool)
		{
			Item[] array = c.Human.inventory.AllItems();
			foreach (Item item in array)
			{
				if (item.info.category == ItemCategory.Tool)
				{
					HeldEntity heldEntity = item.GetHeldEntity() as HeldEntity;
					if (heldEntity != null && heldEntity.toolType == tool)
					{
						return item;
					}
				}
			}
			return null;
		}
	}
}
