using Apex.Serialization;

namespace Rust.Ai
{
	public class SetHumanSpeed : BaseAction
	{
		[ApexSerialization(defaultValue = NPCPlayerApex.SpeedEnum.StandStill)]
		public NPCPlayerApex.SpeedEnum value;

		public override void DoExecute(BaseContext c)
		{
			Set(c, value);
		}

		public static void Set(BaseContext c, NPCPlayerApex.SpeedEnum speed)
		{
			c.AIAgent.TargetSpeed = c.AIAgent.ToSpeed(speed);
			c.SetFact(NPCPlayerApex.Facts.Speed, (byte)speed);
		}
	}
}
