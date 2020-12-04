namespace Rust.Ai
{
	public sealed class StartAttack : BaseAction
	{
		public override void DoExecute(BaseContext c)
		{
			c.AIAgent.StartAttack();
		}
	}
}
