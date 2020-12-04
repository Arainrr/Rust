namespace Rust.Ai
{
	public class EnergyLevel : BaseScorer
	{
		public override float GetScore(BaseContext c)
		{
			return c.AIAgent.GetEnergy;
		}
	}
}
