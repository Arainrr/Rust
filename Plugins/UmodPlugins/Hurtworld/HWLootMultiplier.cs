using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
	[Info("HW Loot Multiplier", "klauz24", "1.2.0"), Description("Simple loot multiplier for your Hurtworld server")]
	internal class HWLootMultiplier : HurtworldPlugin
	{
		private PlayerIdentity _owner;

		private Configuration _config;

		private class Configuration
		{
			[JsonProperty(PropertyName = "Global multiplier")]
			public int GlobalMultiplier { get; set; } = 1;

			[JsonProperty(PropertyName = "Enable global multiplier")]
			public bool EnableGlobalMultiplier { get; set; } = false;

			[JsonProperty(PropertyName = "Plants")]
			public int Plants { get; set; } = 1;

			[JsonProperty(PropertyName = "Resources")]
			public int Gather { get; set; } = 1;

			[JsonProperty(PropertyName = "Animals")]
			public int Animals { get; set; } = 1;

			[JsonProperty(PropertyName = "Airdrop")]
			public int Airdrop { get; set; } = 1;

			[JsonProperty(PropertyName = "Loot frenzy")]
			public int LootFrenzy { get; set; } = 1;

			[JsonProperty(PropertyName = "Mining drills")]
			public int MiningDrills { get; set; } = 1;

			[JsonProperty(PropertyName = "Pick up resources")]
			public int PickUp { get; set; } = 1;

			[JsonProperty(PropertyName = "Explodable mining rocks")]
			public int ExplodableMiningRock { get; set; } = 1;

			[JsonProperty(PropertyName = "Town event (Amount of cases)")]
			public int TownEvent { get; set; } = 1;

			[JsonProperty(PropertyName = "Town case T1")]
			public int TownHardCaseT1 { get; set; } = 1;

			[JsonProperty(PropertyName = "Town case T2")]
			public int TownHardCaseT2 { get; set; } = 1;

			[JsonProperty(PropertyName = "Fragments case T1")]
			public int FragmentsT1 { get; set; } = 1;

			[JsonProperty(PropertyName = "Fragments case T2")]
			public int FragmentsT2 { get; set; } = 1;

			[JsonProperty(PropertyName = "Fragments case T3")]
			public int FragmentsT3 { get; set; } = 1;

			[JsonProperty(PropertyName = "Town boxes")]
			public int TownBoxes { get; set; } = 1;

			[JsonProperty(PropertyName = "Vehicles")]
			public int Vehicles { get; set; } = 1;

			public string ToJson() => JsonConvert.SerializeObject(this);

			public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
		}

		protected override void LoadDefaultConfig() => _config = new Configuration();

		protected override void LoadConfig()
		{
			base.LoadConfig();
			try
			{
				_config = Config.ReadObject<Configuration>();
				if (_config == null)
				{
					throw new JsonException();
				}

				if (!_config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys))
				{
					Puts("Configuration appears to be outdated; updating and saving");
					SaveConfig();
				}
			}
			catch
			{
				Puts($"Configuration file {Name}.json is invalid; using defaults");
				LoadDefaultConfig();
			}
		}

		protected override void SaveConfig()
		{
			Puts($"Configuration changes saved to {Name}.json");
			Config.WriteObject(_config, true);
		}

		private void OnPlantGather(GrowingPlantUsable plant, WorldItemInteractServer player, List<ItemObject> items)
		{
			this._owner = GameManager.Instance.GetIdentity(player.networkView.owner);
			HandleLoot(items, _config.Plants);
		}

		private void OnDispenserGather(GameObject obj, HurtMonoBehavior player, List<ItemObject> items)
		{
			this._owner = GameManager.Instance.GetIdentity(player.networkView.owner);
			HandleLoot(items, _config.Gather);
		}

		private void OnDrillDispenserGather(GameObject obj, DrillMachine machine, List<ItemObject> items)
		{
			this._owner = GameManager.Instance.GetIdentity(machine.networkView.owner);
			HandleLoot(items, _config.MiningDrills);
		}

		private void OnAirdrop(GameObject obj, AirDropEvent airdrop, List<ItemObject> items)
		{
			this._owner = GameManager.Instance.GetIdentity(airdrop.networkView.owner);
			HandleLoot(items, _config.Airdrop);
		}

		private void OnCollectiblePickup(LootOnPickup node, WorldItemInteractServer player, List<ItemObject> items)
		{
			this._owner = GameManager.Instance.GetIdentity(player.networkView.owner);
			HandleLoot(items, _config.PickUp);
		}

		private void OnControlTownDrop(GameObject obj, ControlTownEvent townEvent, List<ItemObject> items)
		{
			this._owner = GameManager.Instance.GetIdentity(townEvent.networkView.owner);
			HandleLoot(items, _config.TownEvent);
		}

		private void OnLootFrenzySpawn(GameObject obj, LootFrenzyTownEvent frenzyEvent, List<ItemObject> items)
		{
			this._owner = GameManager.Instance.GetIdentity(frenzyEvent.networkView.owner);
			HandleLoot(items, _config.LootFrenzy);
		}

		private void OnMiningRockExplode(GameObject obj, ExplodableMiningRock rock, List<ItemObject> items)
		{
			this._owner = GameManager.Instance.GetIdentity(rock.networkView.owner);
			HandleLoot(items, _config.ExplodableMiningRock);
		}

		private void OnDisassembleVehicle(GameObject vehicle, VehicleStatManager vehicleStatManager, List<ItemObject> items)
		{
			this._owner = GameManager.Instance.GetIdentity(vehicleStatManager.networkView.owner);
			HandleLoot(items, _config.Vehicles);
		}

		private void OnEntityDropLoot(GameObject obj, List<ItemObject> items) => HandleLoot(items, _config.Animals);

		private void OnLootCaseOpen(ItemComponentLootCase lootCase, ItemObject obj, Inventory inv, List<ItemObject> items)
		{
			this._owner = GameManager.Instance.GetIdentity(inv.networkView.owner);
			var ltn = lootCase.LootTree.name;
			for (var i = 0; i < items.Count; i++)
			{
				var defaultStack = items[i].StackSize;
				if (_config.EnableGlobalMultiplier)
				{
					items[i].StackSize = defaultStack * _config.GlobalMultiplier;
				}
				else
				{
					if (ltn == "TownEventHardcaseLoot T1") items[i].StackSize = defaultStack * _config.TownHardCaseT1;
					if (ltn == "TownEventHardcaseLoot") items[i].StackSize = defaultStack * _config.TownHardCaseT2;
					if (ltn == "Fragments Tier 1") items[i].StackSize = defaultStack * _config.FragmentsT1;
					if (ltn == "Fragments Tier 2") items[i].StackSize = defaultStack * _config.FragmentsT2;
					if (ltn == "Fragments Tier 3") items[i].StackSize = defaultStack * _config.FragmentsT3;
				}
				items[i].InvalidateStack();
			}
		}

		private void OnEntitySpawned(HNetworkView data)
		{
			this._owner = GameManager.Instance.GetIdentity(data.owner);
			var name = data.gameObject.name;
			if (name == "GenericTownLootCacheServer(Clone)")
			{
				var inv = data.gameObject.GetComponent<Inventory>();
				if (inv != null)
				{
					for (var i = 0; i < inv.Capacity; i++)
					{
						var item = inv.GetSlot(i);
						var defaultStack = item.StackSize;
						if (_config.EnableGlobalMultiplier)
						{
							item.StackSize = defaultStack * _config.GlobalMultiplier;
						}
						else
						{
							item.StackSize = defaultStack * _config.TownBoxes;
						}
						item.InvalidateStack();
					}
				}
			}
		}

		private void HandleLoot(List<ItemObject> list, int multiplier)
		{
			for (var i = 0; i < list.Count; i++)
			{
				var defaultStack = list[i].StackSize;
				if (_config.EnableGlobalMultiplier)
				{
					list[i].StackSize = defaultStack * _config.GlobalMultiplier;
				}
				else
				{
					list[i].StackSize = defaultStack * multiplier;
				}
				list[i].InvalidateStack();
			}
		}
	}
}
