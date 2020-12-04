using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("HW Magic Tools", "Obito/Tricky", "1.0.0")]
	[Description("Automatically smelts mined resources")]

	class HWMagicTools : HurtworldPlugin
	{
		#region Vars

		private readonly string permName = "hwmagictools.use";

		private List<Item_> itemsList = new List<Item_>();
		private class Item_
		{
			public string name;
			public string guid;
			public int id;

			public Item_(ItemGeneratorAsset item)
			{
				name = item.name;
				guid = RuntimeHurtDB.Instance.GetGuid(item);
				id = item.GeneratorId;
			}
		}

		private Dictionary<string, string> smeltItems = new Dictionary<string, string>
		{
			["Bark"] = "Ash",
			["Iron Ore"] = "Shaped Iron",
			["Titranium Ore"] = "Shaped Titranium",
			["Mondinium Ore"] = "Shaped Mondinium",
			["Ultranium Ore"] = "Shaped Ultranium",
			["Galvanite Shard"] = "Shaped Galvanite"
		};

		#endregion


		#region uMod Hooks

		void Init()
		{
			permission.RegisterPermission(permName, this);
			LoadItems();
		}

		void OnDispenserGather(GameObject resourceNode, HurtMonoBehavior player, List<ItemObject> items)
		{
		    var sourceDesc = GameManager.Instance.GetDescriptionKey(resourceNode);
		    if (sourceDesc == null || !sourceDesc.EndsWith("(P)")) return;

		    var session = Player.Find(sourceDesc.Replace("(P)", ""));
		    if (session == null) return;

		    if (!hasPerm(session)) return;
		    foreach (var item in items)
		    {
		    	if (!smeltItems.ContainsKey(item.Generator.name)) continue;

		    	var itemGen = GetItem(smeltItems[item.Generator.name]);
		    	if (itemGen == null) continue;

		    	var itemObj = Singleton<GlobalItemManager>.Instance.CreateItem(itemGen, item.StackSize);
                if (itemObj != null)
                {
                    items.Remove(item);
                    items.Add(itemObj);
                }
		    }
		}

		#endregion


		#region Tools

		private void LoadItems()
		{
			var generators = Singleton<GlobalItemManager>.Instance.GetGenerators();
			if (generators == null) return;
			foreach (var item in generators)
					itemsList.Add(new Item_(item.Value));
		}

		private ItemGeneratorAsset GetItem(string name)
		{
			foreach (var item in itemsList)
				if (item.name == name)
					return Singleton<GlobalItemManager>.Instance.GetGenerators()[item.id];
			return null;
		}

		private bool hasPerm(PlayerSession session)
			=> permission.UserHasPermission(session.IPlayer.Id, permName);

		#endregion
	}
}