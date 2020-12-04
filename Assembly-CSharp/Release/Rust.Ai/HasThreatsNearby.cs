using Apex.Serialization;

namespace Rust.Ai
{
	public sealed class HasThreatsNearby : BaseScorer
	{
		[ApexSerialization]
		public float range = 20f;

		public override float GetScore(BaseContext c)
		{
			float num = 0f;
			for (int i = 0; i < c.Memory.All.Count; i++)
			{
				Memory.SeenInfo seenInfo = c.Memory.All[i];
				if (!(seenInfo.Entity == null) && !(c.Entity.Distance(seenInfo.Entity) > range))
				{
					num += c.AIAgent.FearLevel(seenInfo.Entity);
				}
			}
			return num;
		}
	}
}
