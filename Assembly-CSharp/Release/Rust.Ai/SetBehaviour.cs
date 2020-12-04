using Apex.Serialization;

namespace Rust.Ai
{
	public sealed class SetBehaviour : BaseAction
	{
		[ApexSerialization]
		public BaseNpc.Behaviour Behaviour;

		[ApexSerialization]
		public float BusyTime;

		public override void DoExecute(BaseContext c)
		{
			if (c.AIAgent.CurrentBehaviour != Behaviour)
			{
				c.AIAgent.CurrentBehaviour = Behaviour;
				if (BusyTime > 0f)
				{
					c.AIAgent.SetBusyFor(BusyTime);
				}
			}
		}
	}
}
