namespace Rust.Ai
{
	public sealed class Eat : BaseAction
	{
		public override void DoExecute(BaseContext c)
		{
			c.AIAgent.Eat();
		}
	}
}
