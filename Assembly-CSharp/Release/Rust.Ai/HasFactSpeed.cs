using Apex.Serialization;

namespace Rust.Ai
{
	public class HasFactSpeed : BaseScorer
	{
		[ApexSerialization(defaultValue = BaseNpc.SpeedEnum.StandStill)]
		public BaseNpc.SpeedEnum value;

		public override float GetScore(BaseContext c)
		{
			if ((uint)c.GetFact(BaseNpc.Facts.Speed) != (uint)value)
			{
				return 0f;
			}
			return 1f;
		}
	}
}
