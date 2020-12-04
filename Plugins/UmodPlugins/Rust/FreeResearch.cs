﻿using Network;						// Connection (Effects)
using Oxide.Core.Plugins;			// Plugin
using System.Collections.Generic;	// List
using System.Linq;					// Where
using UnityEngine;					// Vector3

namespace Oxide.Plugins
{
    [Info("Free Research", "Zugzwang", "1.0.10")]
    [Description("Free research and workbench experimentation.")]		
	
    class FreeResearch : RustPlugin
    {
        #region Startup and Unload

		const int ScrapID = -932201673;
		const int MaxResearchScrap = 501;
		
		void OnServerInitialized()
        {
			foreach (ResearchTable z in BaseNetworkable.serverEntities.Where(x => x is ResearchTable))
				LoadItem(z.inventory, ScrapID, MaxResearchScrap, false);
		
			foreach (Workbench z in BaseNetworkable.serverEntities.Where(x => x is Workbench))
				LoadItem(z.inventory, ScrapID, z.GetScrapForExperiment(), true);
		}
		
		void Unload()
		{
			foreach (ResearchTable z in BaseNetworkable.serverEntities.Where(x => x is ResearchTable))
				z.inventory.Remove(z.inventory.GetSlot(1));
		
			foreach (Workbench z in BaseNetworkable.serverEntities.Where(x => x is Workbench))
			{
				z.inventory.Clear();
				z.inventory.SetFlag(ItemContainer.Flag.IsLocked, false);
			}
		}
		
        #endregion Startup and Unload		
		
		#region LoadItem
		
		void LoadItem(ItemContainer ic, int itemid, int quantity, bool lockOption)
		{
			Item i = ic.FindItemByItemID(itemid);
			
			if (i != null)
			{
				if (i.amount < quantity) i.amount = quantity;
			}
			
			else if (!ic.IsLocked())
			{
				i = ItemManager.CreateByItemID(itemid, quantity);
				if (i != null && !i.MoveToContainer(ic, 1))
				{
					i.Remove();
					return;
				}
			}
			
			if (lockOption && !ic.IsLocked())
				ic.SetFlag(ItemContainer.Flag.IsLocked, true);
		}
		
		#endregion LoadItem
		
		#region Instant Research and Experimentation

		void OnItemResearch(ResearchTable table, Item item, BasePlayer player)
		{
			table.researchDuration = 0;
        }
		
		object OnExperimentStart(Workbench workbench, BasePlayer player)
		{
			var playerInfo = SingletonComponent<ServerMgr>.Instance.persistance.GetPlayerInfo(player.userID);
			
			int unlocked = 0;

			foreach (LootSpawn.Entry lse in workbench.experimentalItems.subSpawn)
			{
				ItemDefinition def = lse.category.items[0].itemDef;
				
				if (def == null) continue;
				if (def.Blueprint.defaultBlueprint) continue;
				if (!def.Blueprint.userCraftable) continue;
				if (!def.Blueprint.isResearchable) continue;
				if (def.Blueprint.NeedsSteamItem) continue;
				if (def.Blueprint.NeedsSteamDLC) continue;
				if (playerInfo.unlockedItems.Contains(def.itemid)) continue;
			
				playerInfo.unlockedItems.Add(def.itemid);
				unlocked++;
			}
			
			SingletonComponent<ServerMgr>.Instance.persistance.SetPlayerInfo(player.userID, playerInfo);
			player.SendNetworkUpdateImmediate();

			player.ClientRPCPlayer(null, player, "UnlockedBlueprint", 0);
			Effect.server.Run(workbench.experimentSuccessEffect.resourcePath, (BaseEntity) workbench, 0U, Vector3.zero, Vector3.zero, (Connection) null, false);
			
			//Puts($"{player.displayName} unlocked {unlocked} Level {workbench.Workbenchlevel} blueprints.");
			player.ChatMessage($"Unlocked {unlocked} level {workbench.Workbenchlevel} blueprints.");
			
			return true;
		}

		#endregion Instant Research and Experimentation

		#region Deploy and Pickup

		void OnEntitySpawned(ResearchTable entity)
        {
			LoadItem(entity.inventory, ScrapID, MaxResearchScrap, false);
			entity.pickup.requireEmptyInv = false;
		}

		void OnEntitySpawned(Workbench entity)
        {
			LoadItem(entity.inventory, ScrapID, entity.GetScrapForExperiment(), true);
			entity.pickup.requireEmptyInv = false;
		}	
		
		object CanPickupEntity(BasePlayer player, ResearchTable entity)
		{
			Item i = entity.inventory.GetSlot(0);
			if (i != null)
			{
				player.ChatMessage($"Can't Pickup: Remove the '{i.info.shortname}'.");
				return false;
			}
			
			if (player.CanBuild() && player.IsHoldingEntity<Hammer>())
			{
				entity.inventory.Clear();
				return true;
			}
			return null;
		}	
		
		object CanPickupEntity(BasePlayer player, Workbench entity)
		{
			if (player.CanBuild() && player.IsHoldingEntity<Hammer>())
			{
				entity.inventory.Clear();
				return true;
			}
			return null;
		}		
		
		#endregion Deploy and Pickup 		
		
		#region Free Tuition and Scrap Control
		
		void OnItemUse(Item item, int amountToUse)
		{
			if (item.info.itemid == ScrapID && item.parent?.entityOwner is ResearchTable || item.parent?.entityOwner is Workbench)
				item.amount += amountToUse;
		}		

        object CanMoveItem(Item item, PlayerInventory playerLoot, uint targetContainerId, int targetSlot)
        {
			if (item?.info?.itemid != ScrapID)
				return null;
		
			if (item?.parent?.entityOwner is Workbench || item?.parent?.entityOwner is ResearchTable)
				return false;

			ItemContainer targetContainer = playerLoot?.FindContainer(targetContainerId);
			
			if (targetContainer?.entityOwner is Workbench || targetContainer?.entityOwner is ResearchTable)
				return false;

			return null;
		}		
		
		void OnItemRemovedFromContainer(ItemContainer container, Item item)
		{
			if (item?.info?.itemid == ScrapID && container?.entityOwner is Workbench || container?.entityOwner is ResearchTable)
				NextFrame(() => { item.MoveToContainer(container, 1, true); });
		}		
		
		Item OnItemSplit(Item item, int amount)
		{
			if ((item?.parent?.entityOwner is Workbench || item?.parent?.entityOwner is ResearchTable) && item.position == 1)
				return item;
			else
				return null;
		}		

		#endregion Free Tuition and Scrap Control
	}
}