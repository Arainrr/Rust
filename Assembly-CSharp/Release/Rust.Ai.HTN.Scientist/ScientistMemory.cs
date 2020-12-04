using System;
using UnityEngine;

namespace Rust.Ai.HTN.Scientist
{
	[Serializable]
	public class ScientistMemory : BaseNpcMemory
	{
		[NonSerialized]
		public ScientistContext ScientistContext;

		public Vector3 CachedPreferredDistanceDestination;

		public float CachedPreferredDistanceDestinationTime;

		public Vector3 CachedCoverDestination;

		public float CachedCoverDestinationTime;

		public override BaseNpcDefinition Definition => ScientistContext.Body.AiDefinition;

		public ScientistMemory(ScientistContext context)
			: base(context)
		{
			ScientistContext = context;
		}

		public override void ResetState()
		{
			base.ResetState();
		}

		protected override void OnSetPrimaryKnownEnemyPlayer(ref EnemyPlayerInfo info)
		{
			base.OnSetPrimaryKnownEnemyPlayer(ref info);
			if ((info.LastKnownPosition - ScientistContext.BodyPosition).sqrMagnitude > 1f)
			{
				ScientistContext.HasVisitedLastKnownEnemyPlayerLocation = false;
			}
		}
	}
}
