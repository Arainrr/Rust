namespace Rust.Ai
{
	public class StaminaLevel : BaseScorer
	{
		public override float GetScore(BaseContext c)
		{
			return c.AIAgent.GetStamina;
		}
	}
}
