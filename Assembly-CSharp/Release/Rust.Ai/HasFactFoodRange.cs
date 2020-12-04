using Apex.Serialization;

namespace Rust.Ai
{
	public class HasFactFoodRange : BaseScorer
	{
		[ApexSerialization(defaultValue = BaseNpc.FoodRangeEnum.EatRange)]
		public BaseNpc.FoodRangeEnum value;

		public override float GetScore(BaseContext c)
		{
			if ((uint)c.GetFact(BaseNpc.Facts.FoodRange) != (uint)value)
			{
				return 0f;
			}
			return 1f;
		}
	}
}
