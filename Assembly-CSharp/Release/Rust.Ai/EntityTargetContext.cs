using Apex.AI;

namespace Rust.Ai
{
	public class EntityTargetContext : IAIContext
	{
		public IAIAgent Self;

		public BaseEntity[] Entities;

		public int EntityCount;

		public BaseNpc AnimalTarget;

		public float AnimalScore;

		public TimedExplosive ExplosiveTarget;

		public float ExplosiveScore;

		public void Refresh(IAIAgent self, BaseEntity[] entities, int entityCount)
		{
			Self = self;
			Entities = entities;
			EntityCount = entityCount;
			AnimalTarget = null;
			AnimalScore = 0f;
			ExplosiveTarget = null;
			ExplosiveScore = 0f;
		}
	}
}
