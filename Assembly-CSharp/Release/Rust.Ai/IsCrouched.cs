namespace Rust.Ai
{
	public sealed class IsCrouched : BaseScorer
	{
		public override float GetScore(BaseContext c)
		{
			NPCPlayerApex nPCPlayerApex = c.AIAgent as NPCPlayerApex;
			if (nPCPlayerApex != null)
			{
				if (!nPCPlayerApex.modelState.ducked)
				{
					return 0f;
				}
				return 1f;
			}
			return 0f;
		}
	}
}
