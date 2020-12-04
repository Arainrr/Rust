using System;

namespace Rust.Ai.HTN.NPCTurret
{
	[Serializable]
	public class NPCTurretMemory : BaseNpcMemory
	{
		[NonSerialized]
		public NPCTurretContext NPCTurretContext;

		public override BaseNpcDefinition Definition => NPCTurretContext.Body.AiDefinition;

		public NPCTurretMemory(NPCTurretContext context)
			: base(context)
		{
			NPCTurretContext = context;
		}

		public override void ResetState()
		{
			base.ResetState();
		}
	}
}
