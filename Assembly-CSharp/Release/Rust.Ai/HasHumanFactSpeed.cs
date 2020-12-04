using Apex.Serialization;

namespace Rust.Ai
{
	public class HasHumanFactSpeed : BaseScorer
	{
		[ApexSerialization(defaultValue = NPCPlayerApex.SpeedEnum.StandStill)]
		public NPCPlayerApex.SpeedEnum value;

		public override float GetScore(BaseContext c)
		{
			if ((uint)c.GetFact(NPCPlayerApex.Facts.Speed) != (uint)value)
			{
				return 0f;
			}
			return 1f;
		}
	}
}
