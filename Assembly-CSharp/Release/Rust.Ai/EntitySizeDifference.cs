namespace Rust.Ai
{
	public sealed class EntitySizeDifference : WeightedScorerBase<BaseEntity>
	{
		public override float GetScore(BaseContext c, BaseEntity target)
		{
			float num = 1f;
			BaseNpc baseNpc = c.AIAgent as BaseNpc;
			if (baseNpc != null)
			{
				num = baseNpc.Stats.Size;
			}
			if (target as BasePlayer != null)
			{
				return 1f / num;
			}
			BaseNpc baseNpc2 = target as BaseNpc;
			if (baseNpc2 != null)
			{
				return baseNpc2.Stats.Size / num;
			}
			return 0f;
		}
	}
}
