using Apex.AI;
using System;
using UnityEngine;

namespace Rust.Ai
{
	public class MemorisedPlayerTarget : ActionBase<PlayerTargetContext>
	{
		public override void Execute(PlayerTargetContext context)
		{
			BaseContext baseContext = context.Self.GetContext(Guid.Empty) as BaseContext;
			if (baseContext == null)
			{
				return;
			}
			float num = 0f;
			BasePlayer basePlayer = null;
			Vector3 lastKnownPosition = Vector3.zero;
			float num2 = baseContext.AIAgent.GetStats.AggressionRange * baseContext.AIAgent.GetStats.AggressionRange;
			float num3 = baseContext.AIAgent.GetStats.DeaggroRange * baseContext.AIAgent.GetStats.DeaggroRange;
			for (int i = 0; i < baseContext.Memory.All.Count; i++)
			{
				Memory.SeenInfo seenInfo = baseContext.Memory.All[i];
				BasePlayer basePlayer2 = seenInfo.Entity as BasePlayer;
				if (basePlayer2 != null)
				{
					float sqrMagnitude = (seenInfo.Position - baseContext.Position).sqrMagnitude;
					if (seenInfo.Danger > num && (sqrMagnitude <= num2 || (baseContext.Entity.lastAttacker == basePlayer2 && sqrMagnitude <= num3)))
					{
						num = seenInfo.Danger;
						basePlayer = basePlayer2;
						lastKnownPosition = seenInfo.Position;
					}
				}
			}
			if (basePlayer != null)
			{
				context.Target = basePlayer;
				context.Score = num;
				context.LastKnownPosition = lastKnownPosition;
			}
		}
	}
}
