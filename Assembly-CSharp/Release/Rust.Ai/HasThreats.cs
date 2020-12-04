namespace Rust.Ai
{
	public sealed class HasThreats : BaseScorer
	{
		public override float GetScore(BaseContext c)
		{
			float num = 0f;
			for (int i = 0; i < c.Memory.All.Count; i++)
			{
				Memory.SeenInfo seenInfo = c.Memory.All[i];
				if (!(seenInfo.Entity == null))
				{
					num += c.AIAgent.FearLevel(seenInfo.Entity);
				}
			}
			return num;
		}
	}
}
