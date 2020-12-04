using Oxide.Core;
using Rust.Ai;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Scientist : NPCPlayerApex
{
	public static readonly HashSet<Scientist> AllScientists = new HashSet<Scientist>();

	private static readonly List<Scientist> CommQueryCache = new List<Scientist>();

	private static readonly List<AiAnswer_ShareEnemyTarget> CommTargetCache = new List<AiAnswer_ShareEnemyTarget>(10);

	[Header("Loot")]
	public LootContainer.LootSpawnSlot[] LootSpawnSlots;

	public string LootPanelName;

	public override BaseNpc.AiStatistics.FamilyEnum Family => BaseNpc.AiStatistics.FamilyEnum.Scientist;

	private void InitComm()
	{
		base.OnAggro = (ActionCallback)Delegate.Combine(base.OnAggro, new ActionCallback(OnAggroComm));
	}

	private void OnDestroyComm()
	{
		base.OnAggro = (ActionCallback)Delegate.Remove(base.OnAggro, new ActionCallback(OnAggroComm));
	}

	public override int GetAlliesInRange(out List<Scientist> allies)
	{
		CommQueryCache.Clear();
		foreach (Scientist allScientist in AllScientists)
		{
			if (!(allScientist == this) && IsInCommunicationRange(allScientist))
			{
				CommQueryCache.Add(allScientist);
			}
		}
		allies = CommQueryCache;
		return CommQueryCache.Count;
	}

	public override void SendStatement(AiStatement_EnemyEngaged statement)
	{
		foreach (Scientist allScientist in AllScientists)
		{
			if (!(allScientist == this) && IsInCommunicationRange(allScientist))
			{
				allScientist.OnAiStatement(this, statement);
			}
		}
	}

	public override void SendStatement(AiStatement_EnemySeen statement)
	{
		foreach (Scientist allScientist in AllScientists)
		{
			if (!(allScientist == this) && IsInCommunicationRange(allScientist))
			{
				allScientist.OnAiStatement(this, statement);
			}
		}
	}

	public override void OnAiStatement(NPCPlayerApex source, AiStatement_EnemyEngaged statement)
	{
		if (statement.Enemy != null && statement.LastKnownPosition.HasValue && HostilityConsideration(statement.Enemy) && (base.AiContext.EnemyPlayer == null || base.AiContext.EnemyPlayer == statement.Enemy))
		{
			if (source.GetFact(Facts.AttackedRecently) > 0)
			{
				SetFact(Facts.AllyAttackedRecently, 1);
				AllyAttackedRecentlyTimeout = Time.realtimeSinceStartup + 7f;
			}
			if (GetFact(Facts.IsBandit) > 0)
			{
				base.AiContext.LastAttacker = statement.Enemy;
				lastAttackedTime = source.lastAttackedTime;
			}
			Memory.ExtendedInfo extendedInfo;
			UpdateTargetMemory(statement.Enemy, 0.1f, statement.LastKnownPosition.Value, out extendedInfo);
		}
	}

	public override void OnAiStatement(NPCPlayerApex source, AiStatement_EnemySeen statement)
	{
	}

	public override int AskQuestion(AiQuestion_ShareEnemyTarget question, out List<AiAnswer_ShareEnemyTarget> answers)
	{
		CommTargetCache.Clear();
		List<Scientist> allies;
		if (GetAlliesInRange(out allies) > 0)
		{
			foreach (Scientist item2 in allies)
			{
				AiAnswer_ShareEnemyTarget item = item2.OnAiQuestion(this, question);
				if (item.PlayerTarget != null)
				{
					CommTargetCache.Add(item);
				}
			}
		}
		answers = CommTargetCache;
		return CommTargetCache.Count;
	}

	private void OnAggroComm()
	{
		AiStatement_EnemyEngaged aiStatement_EnemyEngaged = default(AiStatement_EnemyEngaged);
		aiStatement_EnemyEngaged.Enemy = base.AiContext.EnemyPlayer;
		aiStatement_EnemyEngaged.Score = base.AiContext.LastTargetScore;
		AiStatement_EnemyEngaged statement = aiStatement_EnemyEngaged;
		if (base.AiContext.EnemyPlayer != null)
		{
			Memory.SeenInfo info = base.AiContext.Memory.GetInfo(base.AiContext.EnemyPlayer);
			if (info.Entity != null && !info.Entity.IsDestroyed && !base.AiContext.EnemyPlayer.IsDead())
			{
				statement.LastKnownPosition = info.Position;
			}
			else
			{
				statement.Enemy = null;
			}
		}
		SendStatement(statement);
	}

	public override string Categorize()
	{
		return "scientist";
	}

	public override float StartHealth()
	{
		return UnityEngine.Random.Range(startHealth, startHealth);
	}

	public override float StartMaxHealth()
	{
		return startHealth;
	}

	public override float MaxHealth()
	{
		return startHealth;
	}

	public override void ServerInit()
	{
		if (!base.isClient)
		{
			base.ServerInit();
			AllScientists.Add(this);
			InitComm();
		}
	}

	internal override void DoServerDestroy()
	{
		base.DoServerDestroy();
		AllScientists.Remove(this);
		OnDestroyComm();
	}

	public override bool ShouldDropActiveItem()
	{
		return false;
	}

	public override BaseCorpse CreateCorpse()
	{
		using (TimeWarning.New("Create corpse"))
		{
			NPCPlayerCorpse nPCPlayerCorpse = DropCorpse("assets/prefabs/npc/scientist/scientist_corpse.prefab") as NPCPlayerCorpse;
			if ((bool)nPCPlayerCorpse)
			{
				nPCPlayerCorpse.transform.position = nPCPlayerCorpse.transform.position + Vector3.down * NavAgent.baseOffset;
				nPCPlayerCorpse.SetLootableIn(2f);
				nPCPlayerCorpse.SetFlag(Flags.Reserved5, HasPlayerFlag(PlayerFlags.DisplaySash));
				nPCPlayerCorpse.SetFlag(Flags.Reserved2, true);
				nPCPlayerCorpse.TakeFrom(inventory.containerMain, inventory.containerWear, inventory.containerBelt);
				nPCPlayerCorpse.playerName = LootPanelName;
				nPCPlayerCorpse.playerSteamID = userID;
				nPCPlayerCorpse.Spawn();
				nPCPlayerCorpse.TakeChildren(this);
				ItemContainer[] containers = nPCPlayerCorpse.containers;
				for (int i = 0; i < containers.Length; i++)
				{
					containers[i].Clear();
				}
				if (LootSpawnSlots.Length != 0)
				{
					object obj = Interface.CallHook("OnCorpsePopulate", this, nPCPlayerCorpse);
					if (obj is BaseCorpse)
					{
						return (BaseCorpse)obj;
					}
					LootContainer.LootSpawnSlot[] lootSpawnSlots = LootSpawnSlots;
					for (int i = 0; i < lootSpawnSlots.Length; i++)
					{
						LootContainer.LootSpawnSlot lootSpawnSlot = lootSpawnSlots[i];
						for (int j = 0; j < lootSpawnSlot.numberToSpawn; j++)
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

	public override void Load(LoadInfo info)
	{
		base.Load(info);
		_displayName = string.Format("Scientist {0}", (net != null) ? ((int)net.ID) : "scientist".GetHashCode());
	}
}
