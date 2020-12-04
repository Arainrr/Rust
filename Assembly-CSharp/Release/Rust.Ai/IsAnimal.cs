using Apex.AI;

namespace Rust.Ai
{
	public class IsAnimal : OptionScorerBase<BaseEntity>
	{
		public override float Score(IAIContext context, BaseEntity option)
		{
			if (!(option is BaseNpc))
			{
				return 0f;
			}
			return 1f;
		}
	}
}
