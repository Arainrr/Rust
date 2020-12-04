using Apex.AI;
using Apex.Serialization;

namespace Rust.Ai
{
	public class HasPlayerTarget : ContextualScorerBase<PlayerTargetContext>
	{
		[ApexSerialization]
		private bool Not;

		public override float Score(PlayerTargetContext c)
		{
			if (Not)
			{
				return ((c.Target != null) ? 0f : 1f) * score;
			}
			return ((c.Target != null) ? 1f : 0f) * score;
		}
	}
}
