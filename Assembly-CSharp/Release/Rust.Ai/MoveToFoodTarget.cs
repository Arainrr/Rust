namespace Rust.Ai
{
	public class MoveToFoodTarget : BaseAction
	{
		public override void DoExecute(BaseContext c)
		{
			if (!(c.AIAgent.FoodTarget == null))
			{
				c.AIAgent.UpdateDestination(c.AIAgent.FoodTarget.transform);
			}
		}
	}
}
