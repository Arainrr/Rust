using Rust.Ai.HTN.Reasoning;

namespace Rust.Ai.HTN.ScientistJunkpile.Reasoners
{
	public class OrientationReasoner : INpcReasoner
	{
		public float TickFrequency
		{
			get;
			set;
		}

		public float LastTickTime
		{
			get;
			set;
		}

		public void Tick(IHTNAgent npc, float deltaTime, float time)
		{
			ScientistJunkpileContext scientistJunkpileContext = npc.AiDomain.NpcContext as ScientistJunkpileContext;
			if (scientistJunkpileContext == null)
			{
				return;
			}
			HTNPlayer hTNPlayer = npc as HTNPlayer;
			if (!(hTNPlayer == null))
			{
				NpcOrientation orientationType = NpcOrientation.Heading;
				if (npc.IsDestroyed || hTNPlayer.IsDead() || hTNPlayer.IsWounded())
				{
					orientationType = NpcOrientation.None;
				}
				else if (scientistJunkpileContext.Memory.PrimaryKnownAnimal.Animal != null)
				{
					orientationType = ((!(scientistJunkpileContext.PrimaryEnemyPlayerInLineOfSight.Player != null)) ? NpcOrientation.LookAtAnimal : ((!(scientistJunkpileContext.Memory.PrimaryKnownAnimal.SqrDistance < scientistJunkpileContext.PrimaryEnemyPlayerInLineOfSight.SqrDistance)) ? (scientistJunkpileContext.PrimaryEnemyPlayerInLineOfSight.BodyVisible ? NpcOrientation.PrimaryTargetBody : (scientistJunkpileContext.PrimaryEnemyPlayerInLineOfSight.HeadVisible ? NpcOrientation.PrimaryTargetHead : NpcOrientation.LastKnownPrimaryTargetLocation)) : NpcOrientation.LookAtAnimal));
				}
				else if (scientistJunkpileContext.PrimaryEnemyPlayerInLineOfSight.Player != null)
				{
					orientationType = (scientistJunkpileContext.PrimaryEnemyPlayerInLineOfSight.BodyVisible ? NpcOrientation.PrimaryTargetBody : (scientistJunkpileContext.PrimaryEnemyPlayerInLineOfSight.HeadVisible ? NpcOrientation.PrimaryTargetHead : NpcOrientation.LastKnownPrimaryTargetLocation));
				}
				else if (hTNPlayer.lastAttacker != null && hTNPlayer.lastAttackedTime > 0f && time - hTNPlayer.lastAttackedTime < 2f)
				{
					orientationType = NpcOrientation.LastAttackedDirection;
				}
				else if (scientistJunkpileContext.Memory.PrimaryKnownEnemyPlayer.PlayerInfo.Player != null)
				{
					orientationType = ((scientistJunkpileContext.GetFact(Facts.IsSearching) > 0 && scientistJunkpileContext.GetFact(Facts.IsNavigating) == 0) ? NpcOrientation.LookAround : ((scientistJunkpileContext.GetFact(Facts.IsIdle) <= 0) ? NpcOrientation.LastKnownPrimaryTargetLocation : ((!scientistJunkpileContext.IsFact(Facts.CanHearEnemy)) ? NpcOrientation.Heading : NpcOrientation.AudibleTargetDirection)));
				}
				else if (scientistJunkpileContext.IsFact(Facts.CanHearEnemy))
				{
					orientationType = NpcOrientation.AudibleTargetDirection;
				}
				scientistJunkpileContext.OrientationType = orientationType;
			}
		}
	}
}
