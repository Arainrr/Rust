using UnityEngine.AI;

namespace Rust.Ai
{
	public sealed class CanPathToEntity : WeightedScorerBase<BaseEntity>
	{
		private static readonly NavMeshPath pathToEntity = new NavMeshPath();

		public override float GetScore(BaseContext c, BaseEntity target)
		{
			if (c.AIAgent.IsNavRunning() && c.AIAgent.GetNavAgent.CalculatePath(target.ServerPosition, pathToEntity) && pathToEntity.status == NavMeshPathStatus.PathComplete)
			{
				return 1f;
			}
			return 0f;
		}
	}
}
