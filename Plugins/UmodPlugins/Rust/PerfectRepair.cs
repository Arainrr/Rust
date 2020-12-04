﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Perfect Repair", "Orange", "1.1.0")]
    [Description("Items will be fully repaired, removing the permanent penalty (red bar)")]
    public class PerfectRepair : RustPlugin
    {
        #region Oxide Hooks

        private void OnServerInitialized()
        {
            foreach (var def in ItemManager.itemList)
            {
                def.condition.maintainMaxCondition = true;
            }
            
            PrintWarning("All containers will be checked, it can cause small lag");
            ServerMgr.Instance.StartCoroutine(CheckContainers());
        }

        private IEnumerator CheckContainers()
        {
            foreach (var bEntity in BaseNetworkable.serverEntities)
            {
                var container = bEntity.GetComponent<StorageContainer>();
                if (container != null)
                {
                    foreach (var item in container.inventory.itemList ?? new List<Item>())
                    {
                        if (item.hasCondition)
                        {
                            item._maxCondition = item.info.condition.max;
                            item.MarkDirty();
                        }
                    }
                    
                    yield return new WaitForEndOfFrame();
                }

                var player = bEntity.GetComponent<BasePlayer>();
                if (player != null)
                {
                    foreach (var item in player.inventory.AllItems())
                    {
                        if (item.hasCondition)
                        {
                            item._maxCondition = item.info.condition.max;
                            item.MarkDirty();
                        }
                    }
                    
                    yield return new WaitForEndOfFrame();
                }

                var corpse = bEntity.GetComponent<LootableCorpse>();
                if (corpse != null)
                {
                    foreach (var item in corpse.containers.SelectMany(x => x.itemList))
                    {
                        if (item.hasCondition)
                        {
                            item._maxCondition = item.info.condition.max;
                            item.MarkDirty();
                        }
                    }
                    
                    yield return new WaitForEndOfFrame();
                }

                var droppedItem = bEntity.GetComponent<DroppedItem>();
                if (droppedItem != null)
                {
                    if (droppedItem.item.hasCondition)
                    {
                        droppedItem.item._maxCondition = droppedItem.item.info.condition.max;
                    }
                    
                    yield return new WaitForEndOfFrame();
                }

                var droppedContainer = bEntity.GetComponent<DroppedItemContainer>();
                if (droppedContainer != null)
                {
                    foreach (var item in droppedContainer.inventory.itemList)
                    {
                        if (item.hasCondition)
                        {
                            item._maxCondition = item.info.condition.max;
                            item.MarkDirty();
                        }
                    }
                    
                    yield return new WaitForEndOfFrame();
                }
                
                yield return new WaitForEndOfFrame();
            }
        }

        #endregion
    }
}
