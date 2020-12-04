using Apex.Serialization;

namespace Rust.Ai
{
	public class HasHumanFactAmmo : BaseScorer
	{
		public enum EqualityEnum
		{
			Greater,
			Gequal,
			Equal,
			Lequal,
			Lesser
		}

		[ApexSerialization(defaultValue = NPCPlayerApex.AmmoStateEnum.Full)]
		public NPCPlayerApex.AmmoStateEnum value;

		[ApexSerialization]
		public bool requireRanged;

		[ApexSerialization(defaultValue = EqualityEnum.Equal)]
		public EqualityEnum Equality;

		public override float GetScore(BaseContext c)
		{
			if (requireRanged && c.GetFact(NPCPlayerApex.Facts.CurrentWeaponType) == 1)
			{
				if (Equality <= EqualityEnum.Equal)
				{
					return 0f;
				}
				return 1f;
			}
			byte fact = c.GetFact(NPCPlayerApex.Facts.CurrentAmmoState);
			switch (Equality)
			{
			default:
				if ((uint)fact != (uint)value)
				{
					return 0f;
				}
				return 1f;
			case EqualityEnum.Greater:
				if ((int)fact >= (int)value)
				{
					return 0f;
				}
				return 1f;
			case EqualityEnum.Gequal:
				if ((int)fact > (int)value)
				{
					return 0f;
				}
				return 1f;
			case EqualityEnum.Lequal:
				if ((int)fact < (int)value)
				{
					return 0f;
				}
				return 1f;
			case EqualityEnum.Lesser:
				if ((int)fact <= (int)value)
				{
					return 0f;
				}
				return 1f;
			}
		}
	}
}
