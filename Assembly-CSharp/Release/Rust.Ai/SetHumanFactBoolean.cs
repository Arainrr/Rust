using Apex.Serialization;

namespace Rust.Ai
{
	public class SetHumanFactBoolean : BaseAction
	{
		[ApexSerialization]
		public NPCPlayerApex.Facts fact;

		[ApexSerialization(defaultValue = false)]
		public bool value;

		public override void DoExecute(BaseContext c)
		{
			c.SetFact(fact, (byte)(value ? 1 : 0));
		}
	}
}
