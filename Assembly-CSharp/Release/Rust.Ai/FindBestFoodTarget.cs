namespace Rust.Ai
{
	public class FindBestFoodTarget : BaseActionWithOptions<BaseEntity>
	{
		public override void DoExecute(BaseContext c)
		{
			BaseEntity baseEntity = GetBest(c, c.Memory.Visible);
			if (baseEntity == null || !c.AIAgent.WantsToEat(baseEntity))
			{
				baseEntity = null;
			}
			c.AIAgent.FoodTarget = baseEntity;
		}
	}
}
