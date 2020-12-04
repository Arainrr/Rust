namespace Rust.Ai
{
	public class StopMoving : BaseAction
	{
		public override void DoExecute(BaseContext c)
		{
			c.AIAgent.StopMoving();
		}
	}
}
