using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rust.Ai.HTN.ScientistJunkpile
{
	[Serializable]
	public class ScientistJunkpileMemory : BaseNpcMemory
	{
		[NonSerialized]
		public ScientistJunkpileContext ScientistJunkpileContext;

		public Vector3 CachedPreferredDistanceDestination;

		public float CachedPreferredDistanceDestinationTime;

		public Vector3 CachedCoverDestination;

		public float CachedCoverDestinationTime;

		public List<BasePlayer> MarkedEnemies = new List<BasePlayer>();

		public override BaseNpcDefinition Definition => ScientistJunkpileContext.Body.AiDefinition;

		public ScientistJunkpileMemory(ScientistJunkpileContext context)
			: base(context)
		{
			ScientistJunkpileContext = context;
		}

		public override void ResetState()
		{
			base.ResetState();
			MarkedEnemies.Clear();
		}

		protected override void OnSetPrimaryKnownEnemyPlayer(ref EnemyPlayerInfo info)
		{
			if (MarkedEnemies.Contains(info.PlayerInfo.Player))
			{
				base.OnSetPrimaryKnownEnemyPlayer(ref info);
				if ((info.LastKnownPosition - ScientistJunkpileContext.BodyPosition).sqrMagnitude > 1f)
				{
					ScientistJunkpileContext.HasVisitedLastKnownEnemyPlayerLocation = false;
				}
			}
		}

		public void MarkEnemy(BasePlayer player)
		{
			if (player != null && !MarkedEnemies.Contains(player))
			{
				MarkedEnemies.Add(player);
			}
		}

		protected override void OnForget(BasePlayer player)
		{
			MarkedEnemies.Remove(player);
		}

		public override bool ShouldRemoveOnPlayerForgetTimeout(float time, NpcPlayerInfo player)
		{
			if (player.Player == null || player.Player.transform == null || player.Player.IsDestroyed || player.Player.IsDead() || player.Player.IsWounded())
			{
				return true;
			}
			if (time <= player.Time + Definition.Memory.ForgetInRangeTime)
			{
				return false;
			}
			if (MarkedEnemies.Contains(player.Player) && (player.SqrDistance <= 0f || player.SqrDistance > Definition.Sensory.SqrVisionRange))
			{
				return false;
			}
			return true;
		}
	}
}
