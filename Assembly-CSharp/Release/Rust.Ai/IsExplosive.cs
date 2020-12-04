using Apex.AI;

namespace Rust.Ai
{
	public class IsExplosive : OptionScorerBase<BaseEntity>
	{
		public override float Score(IAIContext context, BaseEntity option)
		{
			TimedExplosive timedExplosive = option as TimedExplosive;
			if ((bool)timedExplosive)
			{
				float num = 0f;
				foreach (DamageTypeEntry damageType in timedExplosive.damageTypes)
				{
					num += damageType.amount;
				}
				if (!(num > 0f))
				{
					return 0f;
				}
				return 1f;
			}
			return 0f;
		}
	}
}
