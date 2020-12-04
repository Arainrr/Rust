using Apex.Serialization;

namespace Rust.Ai
{
	public class HasCurrentWeaponType : BaseScorer
	{
		[ApexSerialization(defaultValue = NPCPlayerApex.WeaponTypeEnum.None)]
		public NPCPlayerApex.WeaponTypeEnum value;

		public override float GetScore(BaseContext c)
		{
			if ((uint)c.GetFact(NPCPlayerApex.Facts.CurrentWeaponType) != (uint)value)
			{
				return 0f;
			}
			return 1f;
		}
	}
}
