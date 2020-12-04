using Apex.Serialization;

namespace Rust.Ai
{
	public class HasHumanFactHealth : BaseScorer
	{
		[ApexSerialization(defaultValue = NPCPlayerApex.HealthEnum.Fine)]
		public NPCPlayerApex.HealthEnum value;

		public override float GetScore(BaseContext c)
		{
			if ((uint)c.GetFact(NPCPlayerApex.Facts.Health) != (uint)value)
			{
				return 0f;
			}
			return 1f;
		}
	}
}
