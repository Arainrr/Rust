using Rust.Ai.HTN.Reasoning;

namespace Rust.Ai.HTN.Bear.Reasoners
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
			BearContext bearContext = npc.AiDomain.NpcContext as BearContext;
			if (bearContext == null)
			{
				return;
			}
			HTNAnimal hTNAnimal = npc as HTNAnimal;
			if (!(hTNAnimal == null))
			{
				NpcOrientation orientationType = NpcOrientation.Heading;
				if (npc.IsDestroyed || hTNAnimal.IsDead())
				{
					orientationType = NpcOrientation.None;
				}
				else if (bearContext.Memory.PrimaryKnownAnimal.Animal != null)
				{
					orientationType = ((!(bearContext.PrimaryEnemyPlayerInLineOfSight.Player != null)) ? NpcOrientation.LookAtAnimal : ((!(bearContext.Memory.PrimaryKnownAnimal.SqrDistance < bearContext.PrimaryEnemyPlayerInLineOfSight.SqrDistance)) ? (bearContext.PrimaryEnemyPlayerInLineOfSight.BodyVisible ? NpcOrientation.PrimaryTargetBody : (bearContext.PrimaryEnemyPlayerInLineOfSight.HeadVisible ? NpcOrientation.PrimaryTargetHead : NpcOrientation.LastKnownPrimaryTargetLocation)) : NpcOrientation.LookAtAnimal));
				}
				else if (bearContext.PrimaryEnemyPlayerInLineOfSight.Player != null)
				{
					orientationType = (bearContext.PrimaryEnemyPlayerInLineOfSight.BodyVisible ? NpcOrientation.PrimaryTargetBody : (bearContext.PrimaryEnemyPlayerInLineOfSight.HeadVisible ? NpcOrientation.PrimaryTargetHead : NpcOrientation.LastKnownPrimaryTargetLocation));
				}
				else if (hTNAnimal.lastAttacker != null && hTNAnimal.lastAttackedTime > 0f && time - hTNAnimal.lastAttackedTime < 2f)
				{
					orientationType = NpcOrientation.LastAttackedDirection;
				}
				else if (bearContext.Memory.PrimaryKnownEnemyPlayer.PlayerInfo.Player != null)
				{
					orientationType = ((bearContext.GetFact(Facts.IsSearching) > 0 && bearContext.GetFact(Facts.IsNavigating) == 0) ? NpcOrientation.LookAround : ((bearContext.GetFact(Facts.IsIdle) <= 0) ? NpcOrientation.LastKnownPrimaryTargetLocation : ((!bearContext.IsFact(Facts.CanHearEnemy)) ? NpcOrientation.Heading : NpcOrientation.AudibleTargetDirection)));
				}
				else if (bearContext.IsFact(Facts.CanHearEnemy))
				{
					orientationType = NpcOrientation.AudibleTargetDirection;
				}
				bearContext.OrientationType = orientationType;
			}
		}
	}
}
