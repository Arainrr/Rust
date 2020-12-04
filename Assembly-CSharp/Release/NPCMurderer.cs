using Oxide.Core;
using UnityEngine;

public class NPCMurderer : NPCPlayerApex
{
	public LootContainer.LootSpawnSlot[] LootSpawnSlots;

	public override BaseNpc.AiStatistics.FamilyEnum Family => BaseNpc.AiStatistics.FamilyEnum.Murderer;

	public override string Categorize()
	{
		return "murderer";
	}

	public override float StartHealth()
	{
		return UnityEngine.Random.Range(100f, 100f);
	}

	public override float StartMaxHealth()
	{
		return startHealth;
	}

	public override float MaxHealth()
	{
		return _maxHealth;
	}

	public override bool ShouldDropActiveItem()
	{
		return false;
	}

	public override BaseCorpse CreateCorpse()
	{
		using (TimeWarning.New("Create corpse"))
		{
			NPCPlayerCorpse nPCPlayerCorpse = DropCorpse("assets/prefabs/npc/murderer/murderer_corpse.prefab") as NPCPlayerCorpse;
			if ((bool)nPCPlayerCorpse)
			{
				nPCPlayerCorpse.SetLootableIn(2f);
				nPCPlayerCorpse.SetFlag(Flags.Reserved5, HasPlayerFlag(PlayerFlags.DisplaySash));
				nPCPlayerCorpse.SetFlag(Flags.Reserved2, true);
				for (int i = 0; i < inventory.containerWear.itemList.Count; i++)
				{
					Item item = inventory.containerWear.itemList[i];
					if (item != null && item.info.shortname == "gloweyes")
					{
						inventory.containerWear.Remove(item);
						break;
					}
				}
				nPCPlayerCorpse.TakeFrom(inventory.containerMain, inventory.containerWear, inventory.containerBelt);
				nPCPlayerCorpse.playerName = base.displayName;
				nPCPlayerCorpse.playerSteamID = userID;
				nPCPlayerCorpse.Spawn();
				nPCPlayerCorpse.TakeChildren(this);
				ItemContainer[] containers = nPCPlayerCorpse.containers;
				for (int j = 0; j < containers.Length; j++)
				{
					containers[j].Clear();
				}
				if (LootSpawnSlots.Length != 0)
				{
					object obj = Interface.CallHook("OnCorpsePopulate", this, nPCPlayerCorpse);
					if (obj is BaseCorpse)
					{
						return (BaseCorpse)obj;
					}
					LootContainer.LootSpawnSlot[] lootSpawnSlots = LootSpawnSlots;
					for (int j = 0; j < lootSpawnSlots.Length; j++)
					{
						LootContainer.LootSpawnSlot lootSpawnSlot = lootSpawnSlots[j];
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
