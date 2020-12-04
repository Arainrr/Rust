using System;
using UnityEngine;

namespace Rust.Ai.HTN.Bear
{
	public class BearMemory : BaseNpcMemory
	{
		[NonSerialized]
		public BearContext BearContext;

		public Vector3 CachedPreferredDistanceDestination;

		public float CachedPreferredDistanceDestinationTime;

		public override BaseNpcDefinition Definition => BearContext.Body.AiDefinition;

		public BearMemory(BearContext context)
			: base(context)
		{
			BearContext = context;
		}

		public override void ResetState()
		{
			base.ResetState();
		}

		protected override void OnSetPrimaryKnownEnemyPlayer(ref EnemyPlayerInfo info)
		{
			base.OnSetPrimaryKnownEnemyPlayer(ref info);
			if ((info.LastKnownPosition - BearContext.Body.transform.position).sqrMagnitude > 1f)
			{
				BearContext.HasVisitedLastKnownEnemyPlayerLocation = false;
			}
		}
	}
}
