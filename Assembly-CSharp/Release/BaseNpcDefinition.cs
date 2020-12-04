using Rust.Ai.HTN;
using System;
using UnityEngine;

public abstract class BaseNpcDefinition : Definition<BaseNpcDefinition>
{
	[Serializable]
	public class InfoStats
	{
		public Family Family;

		public Family[] Predators;

		public Family[] Prey;

		public BaseNpc.AiStatistics.FamilyEnum ToFamily(Family family)
		{
			switch (family)
			{
			default:
				return BaseNpc.AiStatistics.FamilyEnum.Player;
			case Family.Bear:
				return BaseNpc.AiStatistics.FamilyEnum.Bear;
			case Family.Boar:
				return BaseNpc.AiStatistics.FamilyEnum.Boar;
			case Family.Chicken:
				return BaseNpc.AiStatistics.FamilyEnum.Chicken;
			case Family.Deer:
				return BaseNpc.AiStatistics.FamilyEnum.Deer;
			case Family.Horse:
				return BaseNpc.AiStatistics.FamilyEnum.Horse;
			case Family.Wolf:
				return BaseNpc.AiStatistics.FamilyEnum.Wolf;
			case Family.Scientist:
				return BaseNpc.AiStatistics.FamilyEnum.Scientist;
			case Family.Murderer:
				return BaseNpc.AiStatistics.FamilyEnum.Murderer;
			case Family.Zombie:
				return BaseNpc.AiStatistics.FamilyEnum.Zombie;
			}
		}
	}

	[Serializable]
	public class VitalStats
	{
		public float HP = 100f;
	}

	[Serializable]
	public class MovementStats
	{
		public float DuckSpeed = 1.7f;

		public float WalkSpeed = 2.8f;

		public float RunSpeed = 5.5f;

		public float TurnSpeed = 120f;

		public float Acceleration = 12f;
	}

	[Serializable]
	public class SensoryStats
	{
		public float VisionRange = 40f;

		public float HearingRange = 20f;

		[Range(0f, 360f)]
		public float FieldOfView = 120f;

		private const float Inv180 = 0.00555555569f;

		public float SqrVisionRange => VisionRange * VisionRange;

		public float SqrHearingRange => HearingRange * HearingRange;

		public float FieldOfViewRadians => (FieldOfView - 180f) * -0.00555555569f - 0.1f;
	}

	[Serializable]
	public class MemoryStats
	{
		public float ForgetTime = 30f;

		public float ForgetInRangeTime = 5f;

		public float NoSeeReturnToSpawnTime = 10f;

		public float ForgetAnimalInRangeTime = 5f;
	}

	[Serializable]
	public class EngagementStats
	{
		public float CloseRange = 2f;

		public float MediumRange = 20f;

		public float LongRange = 100f;

		public float AggroRange = 100f;

		public float DeaggroRange = 150f;

		public float Hostility = 1f;

		public float Defensiveness = 1f;

		public float SqrCloseRange => CloseRange * CloseRange;

		public float SqrMediumRange => MediumRange * MediumRange;

		public float SqrLongRange => LongRange * LongRange;

		public float SqrAggroRange => AggroRange * AggroRange;

		public float SqrDeaggroRange => DeaggroRange * DeaggroRange;

		public float CloseRangeFirearm(AttackEntity ent)
		{
			if (!ent)
			{
				return CloseRange;
			}
			return CloseRange + ent.CloseRangeAddition;
		}

		public float MediumRangeFirearm(AttackEntity ent)
		{
			if (!ent)
			{
				return MediumRange;
			}
			return MediumRange + ent.MediumRangeAddition;
		}

		public float LongRangeFirearm(AttackEntity ent)
		{
			if (!ent)
			{
				return LongRange;
			}
			return LongRange + ent.LongRangeAddition;
		}

		public float SqrCloseRangeFirearm(AttackEntity ent)
		{
			float num = CloseRangeFirearm(ent);
			return num * num;
		}

		public float SqrMediumRangeFirearm(AttackEntity ent)
		{
			float num = MediumRangeFirearm(ent);
			return num * num;
		}

		public float SqrLongRangeFirearm(AttackEntity ent)
		{
			float num = LongRangeFirearm(ent);
			return num * num;
		}

		public float CenterOfCloseRange()
		{
			return CloseRange * 0.5f;
		}

		public float CenterOfCloseRangeFirearm(AttackEntity ent)
		{
			return CloseRangeFirearm(ent) * 0.5f;
		}

		public float SqrCenterOfCloseRange()
		{
			float num = CenterOfCloseRange();
			return num * num;
		}

		public float SqrCenterOfCloseRangeFirearm(AttackEntity ent)
		{
			float num = CenterOfCloseRangeFirearm(ent);
			return num * num;
		}

		public float CenterOfMediumRange()
		{
			float num = MediumRange - CloseRange;
			return MediumRange - num * 0.5f;
		}

		public float CenterOfMediumRangeFirearm(AttackEntity ent)
		{
			float num = MediumRangeFirearm(ent);
			float num2 = num - CloseRangeFirearm(ent);
			return num - num2 * 0.5f;
		}

		public float SqrCenterOfMediumRange()
		{
			float num = CenterOfMediumRange();
			return num * num;
		}

		public float SqrCenterOfMediumRangeFirearm(AttackEntity ent)
		{
			float num = CenterOfMediumRangeFirearm(ent);
			return num * num;
		}
	}

	[Serializable]
	public class RoamStats
	{
		public float MaxRoamRange = 20f;

		public float MinRoamDelay = 5f;

		public float MaxRoamDelay = 10f;

		public float SqrMaxRoamRange => MaxRoamRange * MaxRoamRange;
	}

	public enum Family
	{
		Player,
		Scientist,
		Murderer,
		Horse,
		Deer,
		Boar,
		Wolf,
		Bear,
		Chicken,
		Zombie
	}

	[Header("Domain")]
	public HTNDomain Domain;

	[Header("Base Stats")]
	public InfoStats Info;

	public VitalStats Vitals;

	public MovementStats Movement;

	public SensoryStats Sensory;

	public MemoryStats Memory;

	public EngagementStats Engagement;

	public virtual void Loadout(HTNPlayer target)
	{
	}

	public virtual void OnlyLoadoutWeapons(HTNPlayer target)
	{
	}

	public virtual void StartVoices(HTNPlayer target)
	{
	}

	public virtual void StopVoices(HTNPlayer target)
	{
	}

	public virtual BaseCorpse OnCreateCorpse(HTNPlayer target)
	{
		return null;
	}

	public virtual void Loadout(HTNAnimal target)
	{
	}

	public virtual void StartVoices(HTNAnimal target)
	{
	}

	public virtual void StopVoices(HTNAnimal target)
	{
	}

	public virtual BaseCorpse OnCreateCorpse(HTNAnimal target)
	{
		return null;
	}
}
