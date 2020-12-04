namespace Rust.Ai
{
	public class ChaseTarget : BaseAction
	{
		public override void DoExecute(BaseContext c)
		{
			if (!(c.AIAgent.AttackTarget == null) && c.AIAgent is BaseNpc)
			{
				c.AIAgent.UpdateDestination(c.AIAgent.AttackTarget.transform);
			}
		}
	}
}
