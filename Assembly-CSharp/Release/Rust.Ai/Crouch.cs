using Apex.Serialization;

namespace Rust.Ai
{
	public sealed class Crouch : BaseAction
	{
		[ApexSerialization]
		public bool crouch;

		public override void DoExecute(BaseContext ctx)
		{
			if (crouch)
			{
				NPCPlayerApex nPCPlayerApex = ctx.AIAgent as NPCPlayerApex;
				if (nPCPlayerApex != null)
				{
					nPCPlayerApex.modelState.SetFlag(ModelState.Flag.Ducked, crouch);
				}
			}
		}
	}
}
