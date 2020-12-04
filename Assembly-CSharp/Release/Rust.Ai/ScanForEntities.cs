using Apex.AI;
using Apex.Serialization;
using ConVar;
using UnityEngine;

namespace Rust.Ai
{
	[FriendlyName("Scan for Entities", "Update Context.Entities")]
	public sealed class ScanForEntities : BaseAction
	{
		public BaseEntity[] Results = new BaseEntity[64];

		[ApexSerialization]
		public int forgetTime = 10;

		public override void DoExecute(BaseContext c)
		{
			if (BaseEntity.Query.Server == null)
			{
				return;
			}
			int inSphere = BaseEntity.Query.Server.GetInSphere(c.Position, c.AIAgent.GetStats.VisionRange, Results, AiCaresAbout);
			if (inSphere == 0)
			{
				return;
			}
			for (int i = 0; i < inSphere; i++)
			{
				BaseEntity baseEntity = Results[i];
				if (baseEntity == null || baseEntity == c.Entity || !baseEntity.isServer || !WithinVisionCone(c.AIAgent, baseEntity))
				{
					continue;
				}
				BasePlayer basePlayer = baseEntity as BasePlayer;
				if (basePlayer != null && !baseEntity.IsNpc)
				{
					if (ConVar.AI.ignoreplayers)
					{
						continue;
					}
					Vector3 attackPosition = c.AIAgent.AttackPosition;
					if (!basePlayer.IsVisible(attackPosition, basePlayer.CenterPoint()) && !basePlayer.IsVisible(attackPosition, basePlayer.eyes.position) && !basePlayer.IsVisible(attackPosition, basePlayer.transform.position))
					{
						continue;
					}
				}
				c.Memory.Update(baseEntity);
			}
			c.Memory.Forget(forgetTime);
		}

		private static bool WithinVisionCone(IAIAgent agent, BaseEntity other)
		{
			if (agent.GetStats.VisionCone == -1f)
			{
				return true;
			}
			BaseCombatEntity entity = agent.Entity;
			Vector3 forward = entity.transform.forward;
			BasePlayer basePlayer = entity as BasePlayer;
			if (basePlayer != null)
			{
				basePlayer.eyes.BodyForward();
			}
			Vector3 normalized = (other.transform.position - entity.transform.position).normalized;
			if (Vector3.Dot(entity.transform.forward, normalized) < agent.GetStats.VisionCone)
			{
				return false;
			}
			return true;
		}

		private static bool AiCaresAbout(BaseEntity ent)
		{
			if (ent is BasePlayer)
			{
				return true;
			}
			if (ent is BaseNpc)
			{
				return true;
			}
			if (ent is WorldItem)
			{
				return true;
			}
			if (ent is BaseCorpse)
			{
				return true;
			}
			return false;
		}
	}
}
