namespace Rust.Ai
{
	public class MoveToTargetEntity : BaseAction
	{
		public override void DoExecute(BaseContext c)
		{
			if (!(c.AIAgent.AttackTarget == null))
			{
				c.AIAgent.UpdateDestination(c.AIAgent.AttackTargetMemory.Position);
			}
		}
	}
}
