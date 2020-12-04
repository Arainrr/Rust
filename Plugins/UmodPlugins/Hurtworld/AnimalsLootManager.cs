using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Oxide.Core;

namespace Oxide.Plugins
{
	[Info("Animals Loot Manager", "Obito", "1.0.1")]
	[Description("Change the loot of all creatures easily.")]

	class AnimalsLootManager : HurtworldPlugin
	{
		#region Vars

		private System.Random rand = new System.Random();

		private List<ItemConfig> itemsList = new List<ItemConfig>();
		private class ItemConfig
		{
			public string name;
			public string guid;
			public int id;

			public ItemConfig(ItemGeneratorAsset item)
			{
				name = item.name;
				guid = RuntimeHurtDB.Instance.GetGuid(item);
				id = item.GeneratorId;
			}
		}


		// Cached method
		private Dictionary<PlayerSession, string> targetAnimals = new Dictionary<PlayerSession, string>();

		#endregion


		#region uMod Hooks

		void OnServerInitialized()
		{
			LoadItems();
			CheckInvalidItems();
		}

		object OnEntityTakeDamage(AIEntity entity, EntityEffectSourceData source)
		{
			var sourceDesc = GameManager.Instance.GetDescriptionKey(source.EntitySource);
			if (!sourceDesc.EndsWith("(P)")) return null;
		    var target = GameManager.Instance.GetDescriptionKey(entity.gameObject);
		    var initiator = GetPlayer(sourceDesc.Replace("(P)", ""));
		    if (target != null && initiator != null)
		    {
		    	if (targetAnimals.ContainsKey(initiator))
		    	{
		    		if (targetAnimals[initiator] != target)
		    			targetAnimals[initiator] = target;
		    	}
		    	else
		    		{
		    			targetAnimals.Add(initiator, target);
		    		}
		    }
		    return null;
		}

		void OnEntityDropLoot(GameObject entity, List<ItemObject> items)
		{
		   	var entDesc = GameManager.Instance.GetDescriptionKey(entity);
		   	if (entDesc.EndsWith("(P)"))
		   	{
		   		var session = GetPlayer(entDesc.Replace("(P)", ""));
		   		if (session != null)
		   		{
		   			if (targetAnimals.ContainsKey(session))
		   			{
		   				var creature = config.animals.FirstOrDefault(x => x.Key == targetAnimals[session]);
					   	if (!creature.Equals(default(KeyValuePair<string, AnimalConfig>)))
					   	{
					   		items = GetNewLoot(creature.Key, items);
					   		targetAnimals.Remove(session);
					   	}
		   			}
		   		}
		   	}
		}

		#endregion


		#region Functions

		private void LoadItems()
		{
			var generators = Singleton<GlobalItemManager>.Instance.GetGenerators().Values;
			foreach (var item in generators)
				itemsList.Add(new ItemConfig(item));
		}

		#endregion


		#region Helpers

		private List<ItemObject> GetNewLoot(string creature, List<ItemObject> oldLoot)
		{
			var newLoot = oldLoot;
			var creatureConfig = config.animals[creature];
			if (creatureConfig.replaceLoot) newLoot.Clear();
			foreach (var item in creatureConfig.lootConfig)
			{
				var itemGenerator = GetItem(item.name);
				if (itemGenerator != null)
				{
					var randNum = Core.Random.Range(0f, 100f);
					if (item.chance >= randNum)
					{
						var randAmount = rand.Next(item.min, item.max);
						var itemObj = Singleton<GlobalItemManager>.Instance.CreateItem(itemGenerator, randAmount);
						if (itemObj != null)
						{
							newLoot.Add(itemObj);
						}
					}
				}
			}
			return newLoot;
		}

		private ItemGeneratorAsset GetItem(string name)
		{
			foreach (var item in itemsList)
				if (item.name == name)
					return Singleton<GlobalItemManager>.Instance.GetGenerators()[item.id];
			return null;
		}

		private void CheckInvalidItems()
		{
			foreach (var pair in config.animals)
			{
				var animalCfg = pair.Value;
				foreach (var item in animalCfg.lootConfig)
				{
					if (GetItem(item.name) == null)
					{
						PrintError($"The item {item.name} is invalid. Please correct it in your configuration file");
					}
				}
			}
		}

		private PlayerSession GetPlayer(string name)
		{
			var session = (PlayerSession) null;
			foreach (var player in GameManager.Instance.GetSessions().Values)
				if (player.Identity.Name.ToLower() == name.ToLower()) session = player;
			return session;
		}

		#endregion


		#region Configuration

		private class AnimalConfig
		{
			public bool replaceLoot;
			public List<LootConfig> lootConfig;
		}

		private class LootConfig
		{
			public string name;
			public int max;
			public int min;
			public float chance;
		}

		private static Configuration config;
		private class Configuration
		{
			[JsonProperty(PropertyName = "Creatures")]
			public Dictionary<string, AnimalConfig> animals;
		}
		private Configuration GetDefaultConfig()
		{
			Configuration newConfig = new Configuration();
			var getCreatures = new Dictionary<string, AnimalConfig>();
			foreach (var creature in RuntimeHurtDB.Instance.GetAll<GameObject>())
			{
				var description = GameManager.Instance.GetDescriptionKey(creature);
				if (description.StartsWith("Creatures/"))
				{
					if (!getCreatures.ContainsKey(description))
					{
						var animalCfg = new AnimalConfig()
						{
							replaceLoot = false,
							lootConfig = new List<LootConfig>()
							{
								new LootConfig()
								{
									name = "Shaped Iron",
									max = 20,
									min = 10,
									chance = 70f
								},
								new LootConfig()
								{
									name = "Shaped Titranium",
									max = 5,
									min = 1,
									chance = 40f
								}
							}
						};
						getCreatures.Add(description, animalCfg);
					}
				}
			}

			newConfig.animals = getCreatures;
			return newConfig;
		}
		protected override void LoadConfig()
		{
			base.LoadConfig();
			try
			{
				config = Config.ReadObject<Configuration>();
				if (config == null) LoadDefaultConfig();
			}
			catch
			{
				PrintError("Your configuration file is corrupt. Trying to load the default configuration file...");
				LoadDefaultConfig();
			}
			SaveConfig();
		}
		protected override void LoadDefaultConfig() => config = GetDefaultConfig();
		protected override void SaveConfig() => Config.WriteObject(config);

		#endregion
	}
}