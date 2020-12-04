using Apex.Serialization;

namespace Rust.Ai
{
	public class HasHumanFactBodyState : BaseScorer
	{
		[ApexSerialization(defaultValue = NPCPlayerApex.BodyState.StandingTall)]
		public NPCPlayerApex.BodyState value;

		public override float GetScore(BaseContext c)
		{
			if ((uint)c.GetFact(NPCPlayerApex.Facts.BodyState) != (uint)value)
			{
				return 0f;
			}
			return 1f;
		}
	}
}
