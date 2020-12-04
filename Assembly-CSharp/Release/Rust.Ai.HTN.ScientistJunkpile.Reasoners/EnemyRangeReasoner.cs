using Rust.Ai.HTN.Reasoning;

namespace Rust.Ai.HTN.ScientistJunkpile.Reasoners
{
	public class EnemyRangeReasoner : INpcReasoner
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
			if (scientistJunkpileContext.Memory.PrimaryKnownEnemyPlayer.PlayerInfo.Player == null)
			{
				scientistJunkpileContext.SetFact(Facts.EnemyRange, EnemyRange.OutOfRange);
			}
			float sqrMagnitude = (scientistJunkpileContext.Memory.PrimaryKnownEnemyPlayer.LastKnownPosition - scientistJunkpileContext.BodyPosition).sqrMagnitude;
			AttackEntity firearm = scientistJunkpileContext.Domain.GetFirearm();
			float num = scientistJunkpileContext.Body.AiDefinition.Engagement.SqrCloseRangeFirearm(firearm);
			if (sqrMagnitude <= num)
			{
				scientistJunkpileContext.SetFact(Facts.EnemyRange, EnemyRange.CloseRange);
				return;
			}
			float num2 = scientistJunkpileContext.Body.AiDefinition.Engagement.SqrMediumRangeFirearm(firearm);
			if (sqrMagnitude <= num2)
			{
				scientistJunkpileContext.SetFact(Facts.EnemyRange, EnemyRange.MediumRange);
				return;
			}
			float num3 = scientistJunkpileContext.Body.AiDefinition.Engagement.SqrLongRangeFirearm(firearm);
			if (sqrMagnitude <= num3)
			{
				scientistJunkpileContext.SetFact(Facts.EnemyRange, EnemyRange.LongRange);
			}
			else
			{
				scientistJunkpileContext.SetFact(Facts.EnemyRange, EnemyRange.OutOfRange);
			}
		}
	}
}
