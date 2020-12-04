using Oxide.Core;
using System.Collections;
using UnityEngine;

namespace Rust.Ai.HTN.Murderer
{
	[CreateAssetMenu(menuName = "Rust/AI/Murderer Definition")]
	public class MurdererDefinition : BaseNpcDefinition
	{
		[Header("Aim")]
		public AnimationCurve MissFunction = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

		[Header("Equipment")]
		public PlayerInventoryProperties[] loadouts;

		public LootContainer.LootSpawnSlot[] Loot;

		[Header("Audio")]
		public GameObjectRef DeathEffect;

		public override void StartVoices(HTNPlayer target)
		{
		}

		public override void StopVoices(HTNPlayer target)
		{
		}

		public override void Loadout(HTNPlayer target)
		{
			if (target == null || target.IsDestroyed || target.IsDead() || target.IsWounded() || target.inventory == null || target.inventory.containerBelt == null || target.inventory.containerMain == null || target.inventory.containerWear == null)
			{
				return;
			}
			if (loadouts != null && loadouts.Length != 0)
			{
				PlayerInventoryProperties playerInventoryProperties = loadouts[UnityEngine.Random.Range(0, loadouts.Length)];
				if (playerInventoryProperties != null)
				{
					playerInventoryProperties.GiveToPlayer(target);
					target.StartCoroutine(EquipWeapon(target));
				}
			}
			else
			{
				Debug.LogWarning("Loadout for NPC " + base.name + " was empty.");
			}
		}

		public override void OnlyLoadoutWeapons(HTNPlayer target)
		{
			if (target == null || target.IsDestroyed || target.IsDead() || target.IsWounded() || target.inventory == null || target.inventory.containerBelt == null || target.inventory.containerMain == null || target.inventory.containerWear == null)
			{
				return;
			}
			if (loadouts != null && loadouts.Length != 0)
			{
				PlayerInventoryProperties playerInventoryProperties = loadouts[UnityEngine.Random.Range(0, loadouts.Length)];
				if (playerInventoryProperties != null)
				{
					foreach (PlayerInventoryProperties.ItemAmountSkinned item in playerInventoryProperties.belt)
					{
						if (item.itemDef.category == ItemCategory.Weapon)
						{
							target.inventory.GiveItem(ItemManager.Create(item.itemDef, (int)item.amount, 0uL), target.inventory.containerBelt);
						}
					}
					target.StartCoroutine(EquipWeapon(target));
				}
			}
			else
			{
				Debug.LogWarning("Loadout for NPC " + base.name + " was empty.");
			}
		}

		public IEnumerator EquipWeapon(HTNPlayer target)
		{
			yield return CoroutineEx.waitForSeconds(0.25f);
			if (target == null || target.IsDestroyed || target.IsDead() || target.IsWounded() || target.inventory == null || target.inventory.containerBelt == null)
			{
				yield break;
			}
			Item slot = target.inventory.containerBelt.GetSlot(0);
			if (slot == null)
			{
				yield break;
			}
			target.UpdateActiveItem(slot.uid);
			yield return CoroutineEx.waitForSeconds(0.25f);
			MurdererDomain murdererDomain = target.AiDomain as MurdererDomain;
			if (!murdererDomain)
			{
				yield break;
			}
			if (slot.info.category == ItemCategory.Weapon)
			{
				BaseEntity heldEntity = slot.GetHeldEntity();
				if (heldEntity is BaseProjectile)
				{
					murdererDomain.MurdererContext.SetFact(Facts.HeldItemType, ItemType.ProjectileWeapon);
					murdererDomain.ReloadFirearm();
				}
				else if (heldEntity is BaseMelee)
				{
					murdererDomain.MurdererContext.SetFact(Facts.HeldItemType, ItemType.MeleeWeapon);
					Chainsaw chainsaw = heldEntity as Chainsaw;
					if ((bool)chainsaw)
					{
						chainsaw.ServerNPCStart();
					}
				}
				else if (heldEntity is ThrownWeapon)
				{
					murdererDomain.MurdererContext.SetFact(Facts.HeldItemType, ItemType.ThrowableWeapon);
				}
			}
			else if (slot.info.category == ItemCategory.Medical)
			{
				murdererDomain.MurdererContext.SetFact(Facts.HeldItemType, ItemType.HealingItem);
			}
			else
			{
				if (slot.info.category != ItemCategory.Tool)
				{
					yield break;
				}
				BaseEntity heldEntity2 = slot.GetHeldEntity();
				if (heldEntity2 is BaseMelee)
				{
					murdererDomain.MurdererContext.SetFact(Facts.HeldItemType, ItemType.MeleeWeapon);
					Chainsaw chainsaw2 = heldEntity2 as Chainsaw;
					if ((bool)chainsaw2)
					{
						chainsaw2.ServerNPCStart();
					}
				}
				else
				{
					murdererDomain.MurdererContext.SetFact(Facts.HeldItemType, ItemType.LightSourceItem);
				}
			}
		}

		public override BaseCorpse OnCreateCorpse(HTNPlayer target)
		{
			if (DeathEffect.isValid)
			{
				Effect.server.Run(DeathEffect.resourcePath, target, 0u, Vector3.zero, Vector3.zero);
			}
			using (TimeWarning.New("Create corpse"))
			{
				NPCPlayerCorpse nPCPlayerCorpse = target.DropCorpse("assets/prefabs/npc/murderer/murderer_corpse.prefab") as NPCPlayerCorpse;
				if ((bool)nPCPlayerCorpse)
				{
					if (target.AiDomain != null && target.AiDomain.NavAgent != null && target.AiDomain.NavAgent.isOnNavMesh)
					{
						nPCPlayerCorpse.transform.position = nPCPlayerCorpse.transform.position + Vector3.down * target.AiDomain.NavAgent.baseOffset;
					}
					nPCPlayerCorpse.SetLootableIn(2f);
					nPCPlayerCorpse.SetFlag(BaseEntity.Flags.Reserved5, target.HasPlayerFlag(BasePlayer.PlayerFlags.DisplaySash));
					nPCPlayerCorpse.SetFlag(BaseEntity.Flags.Reserved2, true);
					for (int i = 0; i < target.inventory.containerWear.itemList.Count; i++)
					{
						Item item = target.inventory.containerWear.itemList[i];
						if (item != null && item.info.shortname == "gloweyes")
						{
							target.inventory.containerWear.Remove(item);
							break;
						}
					}
					nPCPlayerCorpse.TakeFrom(target.inventory.containerMain, target.inventory.containerWear, target.inventory.containerBelt);
					nPCPlayerCorpse.playerName = target.displayName;
					nPCPlayerCorpse.playerSteamID = target.userID;
					nPCPlayerCorpse.Spawn();
					nPCPlayerCorpse.TakeChildren(target);
					ItemContainer[] containers = nPCPlayerCorpse.containers;
					for (int j = 0; j < containers.Length; j++)
					{
						containers[j].Clear();
					}
					if (Loot.Length != 0)
					{
						object obj = Interface.CallHook("OnCorpsePopulate", target, nPCPlayerCorpse);
						if (obj is BaseCorpse)
						{
							return (BaseCorpse)obj;
						}
						LootContainer.LootSpawnSlot[] loot = Loot;
						for (int j = 0; j < loot.Length; j++)
						{
							LootContainer.LootSpawnSlot lootSpawnSlot = loot[j];
							for (int k = 0; k < lootSpawnSlot.numberToSpawn; k++)
							{
								if (UnityEngine.Random.Range(0f, 1f) <= lootSpawnSlot.probability)
								{
									lootSpawnSlot.definition.SpawnIntoContainer(nPCPlayerCorpse.containers[0]);
								}
							}
						}
					}
				}
				return nPCPlayerCorpse;
			}
		}
	}
}
