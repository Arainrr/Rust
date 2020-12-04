using Apex.Serialization;

namespace Rust.Ai
{
	public class SetSpeed : BaseAction
	{
		[ApexSerialization(defaultValue = BaseNpc.SpeedEnum.StandStill)]
		public BaseNpc.SpeedEnum value;

		public override void DoExecute(BaseContext c)
		{
			c.AIAgent.TargetSpeed = c.AIAgent.ToSpeed(value);
			c.SetFact(BaseNpc.Facts.Speed, (byte)value);
		}
	}
}
