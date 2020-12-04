using System.Collections;
using UnityEngine;

namespace Rust.Ai.HTN.Bear
{
	[CreateAssetMenu(menuName = "Rust/AI/Animals/Bear Definition")]
	public class BearDefinition : BaseNpcDefinition
	{
		[Header("Sensory Extensions")]
		public float StandingAggroRange = 40f;

		[Header("Corpse")]
		public GameObjectRef CorpsePrefab;

		[Header("Equipment")]
		public LootContainer.LootSpawnSlot[] Loot;

		[Header("Audio")]
		public Vector2 IdleEffectRepeatRange = new Vector2(10f, 15f);

		public GameObjectRef IdleEffect;

		public GameObjectRef DeathEffect;

		private bool _isEffectRunning;

		public float SqrStandingAggroRange => StandingAggroRange * StandingAggroRange;

		public float AggroRange(bool isStanding)
		{
			if (!isStanding)
			{
				return Engagement.AggroRange;
			}
			return StandingAggroRange;
		}

		public float SqrAggroRange(bool isStanding)
		{
			if (!isStanding)
			{
				return Engagement.SqrAggroRange;
			}
			return SqrStandingAggroRange;
		}

		public override void StartVoices(HTNAnimal target)
		{
			if (!_isEffectRunning)
			{
				_isEffectRunning = true;
				target.StartCoroutine(PlayEffects(target));
			}
		}

		public override void StopVoices(HTNAnimal target)
		{
			if (_isEffectRunning)
			{
				_isEffectRunning = false;
			}
		}

		private IEnumerator PlayEffects(HTNAnimal target)
		{
			while (_isEffectRunning && target != null && target.transform != null && !target.IsDestroyed && !target.IsDead())
			{
				if (IdleEffect.isValid)
				{
					Effect.server.Run(IdleEffect.resourcePath, target, StringPool.Get("head"), Vector3.zero, Vector3.zero);
				}
				float seconds = Random.Range(IdleEffectRepeatRange.x, IdleEffectRepeatRange.y + 1f);
				yield return CoroutineEx.waitForSeconds(seconds);
			}
		}

		public override BaseCorpse OnCreateCorpse(HTNAnimal target)
		{
			if (DeathEffect.isValid)
			{
				Effect.server.Run(DeathEffect.resourcePath, target, 0u, Vector3.zero, Vector3.zero);
			}
			using (TimeWarning.New("Create corpse"))
			{
				BaseCorpse baseCorpse = target.DropCorpse(CorpsePrefab.resourcePath);
				if ((bool)baseCorpse)
				{
					if (target.AiDomain != null && target.AiDomain.NavAgent != null && target.AiDomain.NavAgent.isOnNavMesh)
					{
						baseCorpse.transform.position = baseCorpse.transform.position + Vector3.down * target.AiDomain.NavAgent.baseOffset;
					}
					baseCorpse.InitCorpse(target);
					baseCorpse.Spawn();
					baseCorpse.TakeChildren(target);
				}
				return baseCorpse;
			}
		}
	}
}
