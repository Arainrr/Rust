using Facepunch;
using Network;
using Rust.Ai.HTN.ScientistJunkpile;
using System.Collections.Generic;
using UnityEngine;

public class JunkPile : BaseEntity
{
	public GameObjectRef sinkEffect;

	public SpawnGroup[] spawngroups;

	public ScientistJunkpileSpawner npcSpawnGroup;

	private const float lifetimeMinutes = 30f;

	private List<NPCPlayerApex> _npcs;

	private List<HTNPlayer> _htnPlayers;

	protected bool isSinking;

	public override bool OnRpcMessage(BasePlayer player, uint rpc, Message msg)
	{
		using (TimeWarning.New("JunkPile.OnRpcMessage"))
		{
		}
		return base.OnRpcMessage(player, rpc, msg);
	}

	public override void ServerInit()
	{
		base.ServerInit();
		Invoke(TimeOut, 1800f);
		InvokeRepeating(CheckEmpty, 10f, 30f);
		Invoke(SpawnInitial, 1f);
		isSinking = false;
	}

	private void SpawnInitial()
	{
		SpawnGroup[] array = spawngroups;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SpawnInitial();
		}
		npcSpawnGroup?.SpawnInitial();
	}

	public bool SpawnGroupsEmpty()
	{
		SpawnGroup[] array = spawngroups;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].currentPopulation > 0)
			{
				return false;
			}
		}
		ScientistJunkpileSpawner scientistJunkpileSpawner = npcSpawnGroup;
		if ((object)scientistJunkpileSpawner != null && scientistJunkpileSpawner.currentPopulation > 0)
		{
			return false;
		}
		return true;
	}

	public void CheckEmpty()
	{
		if (SpawnGroupsEmpty() && !PlayersNearby())
		{
			CancelInvoke(CheckEmpty);
			SinkAndDestroy();
		}
	}

	public bool PlayersNearby()
	{
		List<BasePlayer> obj = Pool.GetList<BasePlayer>();
		Vis.Entities(base.transform.position, TimeoutPlayerCheckRadius(), obj, 131072);
		bool result = false;
		foreach (BasePlayer item in obj)
		{
			if (!item.IsSleeping() && item.IsAlive())
			{
				result = true;
				break;
			}
		}
		Pool.FreeList(ref obj);
		return result;
	}

	public virtual float TimeoutPlayerCheckRadius()
	{
		return 15f;
	}

	public void TimeOut()
	{
		if (PlayersNearby())
		{
			Invoke(TimeOut, 30f);
		}
		if (SpawnGroupsEmpty())
		{
			SinkAndDestroy();
			return;
		}
		if (_npcs != null)
		{
			foreach (NPCPlayerApex npc in _npcs)
			{
				if (!(npc == null) && !(npc.transform == null) && !npc.IsDestroyed && !npc.IsDead())
				{
					npc.Kill();
				}
			}
			_npcs.Clear();
		}
		if (_htnPlayers != null)
		{
			foreach (HTNPlayer htnPlayer in _htnPlayers)
			{
				if (!(htnPlayer == null) && !(htnPlayer.transform == null) && !htnPlayer.IsDestroyed && !htnPlayer.IsDead())
				{
					htnPlayer.Kill();
				}
			}
			_htnPlayers.Clear();
		}
		SinkAndDestroy();
	}

	public void SinkAndDestroy()
	{
		CancelInvoke(SinkAndDestroy);
		SpawnGroup[] array = spawngroups;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Clear();
		}
		SetFlag(Flags.Reserved8, true, true);
		npcSpawnGroup?.Clear();
		ClientRPC(null, "CLIENT_StartSink");
		base.transform.position -= new Vector3(0f, 5f, 0f);
		isSinking = true;
		Invoke(KillMe, 22f);
	}

	public void KillMe()
	{
		Kill();
	}

	public void AddNpc(NPCPlayerApex npc)
	{
		if (_npcs == null)
		{
			_npcs = new List<NPCPlayerApex>(1);
		}
		_npcs.Add(npc);
	}

	public void AddNpc(HTNPlayer npc)
	{
		if (_htnPlayers == null)
		{
			_htnPlayers = new List<HTNPlayer>();
		}
		_htnPlayers.Add(npc);
	}
}
