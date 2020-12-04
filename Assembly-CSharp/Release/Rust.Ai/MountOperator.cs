using Apex.Serialization;
using ConVar;

namespace Rust.Ai
{
	public class MountOperator : BaseAction
	{
		public enum MountOperationType
		{
			Mount,
			Dismount
		}

		[ApexSerialization]
		public MountOperationType Type;

		public override void DoExecute(BaseContext c)
		{
			MountOperation(c as NPCHumanContext, Type);
		}

		public static void MountOperation(NPCHumanContext c, MountOperationType type)
		{
			switch (type)
			{
			case MountOperationType.Mount:
				if (c.GetFact(NPCPlayerApex.Facts.IsMounted) == 0 && !ConVar.AI.npc_ignore_chairs)
				{
					BaseChair chairTarget = c.ChairTarget;
					if (chairTarget != null)
					{
						c.Human.Mount(chairTarget);
					}
				}
				break;
			case MountOperationType.Dismount:
				if (c.GetFact(NPCPlayerApex.Facts.IsMounted) == 1)
				{
					c.Human.Dismount();
				}
				break;
			}
		}
	}
}
