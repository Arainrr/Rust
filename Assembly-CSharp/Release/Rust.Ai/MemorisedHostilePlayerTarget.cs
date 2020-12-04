using Apex.AI;
using Apex.Serialization;
using System;
using UnityEngine;

namespace Rust.Ai
{
	public class MemorisedHostilePlayerTarget : ActionBase<PlayerTargetContext>
	{
		[ApexSerialization]
		public float HostilityTimeout = 10f;

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
			for (int i = 0; i < baseContext.Memory.All.Count; i++)
			{
				Memory.SeenInfo seenInfo = baseContext.Memory.All[i];
				BasePlayer basePlayer2 = seenInfo.Entity as BasePlayer;
				if (basePlayer2 != null)
				{
					Memory.ExtendedInfo extendedInfo = baseContext.Memory.GetExtendedInfo(seenInfo.Entity);
					if (Time.time < extendedInfo.LastHurtUsTime + HostilityTimeout && seenInfo.Danger > num && (seenInfo.Position - baseContext.Position).sqrMagnitude <= num2)
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
