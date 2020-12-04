using Apex.Serialization;
using UnityEngine;

namespace Rust.Ai
{
	public sealed class ReloadWeaponNeed : BaseScorer
	{
		[ApexSerialization]
		private AnimationCurve ResponseCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

		[ApexSerialization]
		private bool UseResponseCurve = true;

		public override float GetScore(BaseContext c)
		{
			BasePlayer basePlayer = c.AIAgent as BasePlayer;
			if (basePlayer != null)
			{
				AttackEntity attackEntity = basePlayer.GetHeldEntity() as AttackEntity;
				if (attackEntity != null)
				{
					BaseProjectile baseProjectile = attackEntity as BaseProjectile;
					if ((bool)baseProjectile)
					{
						float num = (float)baseProjectile.primaryMagazine.contents / (float)baseProjectile.primaryMagazine.capacity;
						if (!UseResponseCurve)
						{
							return num;
						}
						return ResponseCurve.Evaluate(num);
					}
				}
			}
			return 0f;
		}
	}
}
