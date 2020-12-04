using CompanionServer;
using Facepunch;
using Network;
using Oxide.Core;
using ProtoBuf;
using System.Collections.Generic;
using UnityEngine;

public class RelationshipManager : BaseEntity
{
	public class PlayerTeam
	{
		public ulong teamID;

		public string teamName;

		public ulong teamLeader;

		public List<ulong> members = new List<ulong>();

		public List<ulong> invites = new List<ulong>();

		public float teamStartTime;

		public List<Network.Connection> onlineMemberConnections = new List<Network.Connection>();

		public float teamLifetime => Time.realtimeSinceStartup - teamStartTime;

		public BasePlayer GetLeader()
		{
			return FindByID(teamLeader);
		}

		public void SendInvite(BasePlayer player)
		{
			if (invites.Count > 8)
			{
				invites.RemoveRange(0, 1);
			}
			BasePlayer basePlayer = FindByID(teamLeader);
			if (!(basePlayer == null))
			{
				invites.Add(player.userID);
				player.ClientRPCPlayer(null, player, "CLIENT_PendingInvite", basePlayer.displayName, teamLeader, teamID);
			}
		}

		public void AcceptInvite(BasePlayer player)
		{
			if (invites.Contains(player.userID))
			{
				invites.Remove(player.userID);
				AddPlayer(player);
				player.ClearPendingInvite();
			}
		}

		public void RejectInvite(BasePlayer player)
		{
			player.ClearPendingInvite();
			invites.Remove(player.userID);
		}

		public bool AddPlayer(BasePlayer player)
		{
			ulong userID = player.userID;
			if (members.Contains(userID))
			{
				return false;
			}
			if (player.currentTeam != 0L)
			{
				return false;
			}
			if (members.Count >= maxTeamSize)
			{
				return false;
			}
			player.currentTeam = teamID;
			members.Add(userID);
			Instance.playerToTeam.Add(userID, this);
			MarkDirty();
			player.SendNetworkUpdate();
			return true;
		}

		public bool RemovePlayer(ulong playerID)
		{
			if (members.Contains(playerID))
			{
				members.Remove(playerID);
				Instance.playerToTeam.Remove(playerID);
				BasePlayer basePlayer = FindByID(playerID);
				if (basePlayer != null)
				{
					basePlayer.ClearTeam();
					CompanionServer.Util.BroadcastAppTeamRemoval(basePlayer);
				}
				if (teamLeader == playerID)
				{
					if (members.Count > 0)
					{
						SetTeamLeader(members[0]);
					}
					else
					{
						Disband();
					}
				}
				MarkDirty();
				return true;
			}
			return false;
		}

		public void SetTeamLeader(ulong newTeamLeader)
		{
			teamLeader = newTeamLeader;
			MarkDirty();
		}

		public void Disband()
		{
			Instance.DisbandTeam(this);
			CompanionServer.Server.TeamChat.Remove(teamID);
		}

		public void MarkDirty()
		{
			foreach (ulong member in members)
			{
				BasePlayer basePlayer = FindByID(member);
				if (basePlayer != null)
				{
					basePlayer.UpdateTeam(teamID);
				}
			}
			CompanionServer.Util.BroadcastAppTeamUpdate(this);
		}

		public List<Network.Connection> GetOnlineMemberConnections()
		{
			if (members.Count == 0)
			{
				return null;
			}
			onlineMemberConnections.Clear();
			foreach (ulong member in members)
			{
				BasePlayer basePlayer = FindByID(member);
				if (!(basePlayer == null) && basePlayer.Connection != null)
				{
					onlineMemberConnections.Add(basePlayer.Connection);
				}
			}
			return onlineMemberConnections;
		}
	}

	[ServerVar]
	public static int maxTeamSize = 8;

	public static RelationshipManager _instance;

	public Dictionary<ulong, BasePlayer> cachedPlayers = new Dictionary<ulong, BasePlayer>();

	public Dictionary<ulong, PlayerTeam> playerToTeam = new Dictionary<ulong, PlayerTeam>();

	public Dictionary<ulong, PlayerTeam> teams = new Dictionary<ulong, PlayerTeam>();

	public ulong lastTeamIndex = 1uL;

	public static RelationshipManager Instance => _instance;

	public void OnEnable()
	{
		if (!base.isClient)
		{
			if (_instance != null)
			{
				Debug.LogError("Major fuckup! RelationshipManager spawned twice, Contact Developers!");
				Object.Destroy(base.gameObject);
			}
			else
			{
				_instance = this;
			}
		}
	}

	public void OnDestroy()
	{
		_instance = null;
	}

	public override void Save(SaveInfo info)
	{
		base.Save(info);
		info.msg.relationshipManager = Pool.Get<ProtoBuf.RelationshipManager>();
		info.msg.relationshipManager.maxTeamSize = maxTeamSize;
		if (info.forDisk)
		{
			info.msg.relationshipManager.lastTeamIndex = lastTeamIndex;
			info.msg.relationshipManager.teamList = Pool.GetList<ProtoBuf.PlayerTeam>();
			foreach (KeyValuePair<ulong, PlayerTeam> team in teams)
			{
				PlayerTeam value = team.Value;
				if (value != null)
				{
					ProtoBuf.PlayerTeam playerTeam = Pool.Get<ProtoBuf.PlayerTeam>();
					playerTeam.teamLeader = value.teamLeader;
					playerTeam.teamID = value.teamID;
					playerTeam.teamName = value.teamName;
					playerTeam.members = Pool.GetList<ProtoBuf.PlayerTeam.TeamMember>();
					foreach (ulong member in value.members)
					{
						ProtoBuf.PlayerTeam.TeamMember teamMember = Pool.Get<ProtoBuf.PlayerTeam.TeamMember>();
						BasePlayer basePlayer = FindByID(member);
						teamMember.displayName = ((basePlayer != null) ? basePlayer.displayName : (SingletonComponent<ServerMgr>.Instance.persistance.GetPlayerName(member) ?? "DEAD"));
						teamMember.userID = member;
						playerTeam.members.Add(teamMember);
					}
					info.msg.relationshipManager.teamList.Add(playerTeam);
				}
			}
		}
	}

	public void DisbandTeam(PlayerTeam teamToDisband)
	{
		if (Interface.CallHook("OnTeamDisband", teamToDisband) == null)
		{
			teams.Remove(teamToDisband.teamID);
			Interface.CallHook("OnTeamDisbanded", teamToDisband);
			Pool.Free(ref teamToDisband);
		}
	}

	public static BasePlayer FindByID(ulong userID)
	{
		BasePlayer value = null;
		if (Instance.cachedPlayers.TryGetValue(userID, out value))
		{
			if (value != null)
			{
				return value;
			}
			Instance.cachedPlayers.Remove(userID);
		}
		BasePlayer basePlayer = BasePlayer.FindByID(userID);
		if (!basePlayer)
		{
			basePlayer = BasePlayer.FindSleeping(userID);
		}
		if (basePlayer != null)
		{
			Instance.cachedPlayers.Add(userID, basePlayer);
		}
		return basePlayer;
	}

	public PlayerTeam FindTeam(ulong TeamID)
	{
		if (teams.ContainsKey(TeamID))
		{
			return teams[TeamID];
		}
		return null;
	}

	public PlayerTeam FindPlayersTeam(ulong userID)
	{
		PlayerTeam value;
		if (playerToTeam.TryGetValue(userID, out value))
		{
			return value;
		}
		return null;
	}

	public PlayerTeam CreateTeam()
	{
		PlayerTeam playerTeam = Pool.Get<PlayerTeam>();
		playerTeam.teamID = lastTeamIndex;
		playerTeam.teamStartTime = Time.realtimeSinceStartup;
		teams.Add(lastTeamIndex, playerTeam);
		lastTeamIndex++;
		return playerTeam;
	}

	[ServerUserVar]
	public static void trycreateteam(ConsoleSystem.Arg arg)
	{
		if (maxTeamSize == 0)
		{
			arg.ReplyWith("Teams are disabled on this server");
			return;
		}
		BasePlayer basePlayer = ArgEx.Player(arg);
		if (basePlayer.currentTeam == 0L && Interface.CallHook("OnTeamCreate", basePlayer) == null)
		{
			PlayerTeam playerTeam = Instance.CreateTeam();
			playerTeam.teamLeader = basePlayer.userID;
			playerTeam.AddPlayer(basePlayer);
			Interface.CallHook("OnTeamCreated", basePlayer, playerTeam);
		}
	}

	[ServerUserVar]
	public static void promote(ConsoleSystem.Arg arg)
	{
		BasePlayer basePlayer = ArgEx.Player(arg);
		if (basePlayer.currentTeam == 0L)
		{
			return;
		}
		BasePlayer lookingAtPlayer = GetLookingAtPlayer(basePlayer);
		if (!(lookingAtPlayer == null) && !lookingAtPlayer.IsDead() && !(lookingAtPlayer == basePlayer) && lookingAtPlayer.currentTeam == basePlayer.currentTeam)
		{
			PlayerTeam playerTeam = Instance.teams[basePlayer.currentTeam];
			if (playerTeam != null && playerTeam.teamLeader == basePlayer.userID && Interface.CallHook("OnTeamPromote", playerTeam, lookingAtPlayer) == null)
			{
				playerTeam.SetTeamLeader(lookingAtPlayer.userID);
			}
		}
	}

	[ServerUserVar]
	public static void leaveteam(ConsoleSystem.Arg arg)
	{
		BasePlayer basePlayer = ArgEx.Player(arg);
		if (!(basePlayer == null) && basePlayer.currentTeam != 0L)
		{
			PlayerTeam playerTeam = Instance.FindTeam(basePlayer.currentTeam);
			if (playerTeam != null && Interface.CallHook("OnTeamLeave", playerTeam, basePlayer) == null)
			{
				playerTeam.RemovePlayer(basePlayer.userID);
				basePlayer.ClearTeam();
			}
		}
	}

	[ServerUserVar]
	public static void acceptinvite(ConsoleSystem.Arg arg)
	{
		BasePlayer basePlayer = ArgEx.Player(arg);
		if (!(basePlayer == null) && basePlayer.currentTeam == 0L)
		{
			ulong uLong = arg.GetULong(0, 0uL);
			PlayerTeam playerTeam = Instance.FindTeam(uLong);
			if (playerTeam == null)
			{
				basePlayer.ClearPendingInvite();
			}
			else if (Interface.CallHook("OnTeamAcceptInvite", playerTeam, basePlayer) == null)
			{
				playerTeam.AcceptInvite(basePlayer);
			}
		}
	}

	[ServerUserVar]
	public static void rejectinvite(ConsoleSystem.Arg arg)
	{
		BasePlayer basePlayer = ArgEx.Player(arg);
		if (!(basePlayer == null) && basePlayer.currentTeam == 0L)
		{
			ulong uLong = arg.GetULong(0, 0uL);
			PlayerTeam playerTeam = Instance.FindTeam(uLong);
			if (playerTeam == null)
			{
				basePlayer.ClearPendingInvite();
			}
			else if (Interface.CallHook("OnTeamRejectInvite", basePlayer, playerTeam) == null)
			{
				playerTeam.RejectInvite(basePlayer);
			}
		}
	}

	public static BasePlayer GetLookingAtPlayer(BasePlayer source)
	{
		RaycastHit hitInfo;
		if (Physics.Raycast(source.eyes.position, source.eyes.HeadForward(), out hitInfo, 5f, 1218652417, QueryTriggerInteraction.Ignore))
		{
			BaseEntity entity = RaycastHitEx.GetEntity(hitInfo);
			if ((bool)entity)
			{
				return entity.GetComponent<BasePlayer>();
			}
		}
		return null;
	}

	[ServerVar]
	public static void sleeptoggle(ConsoleSystem.Arg arg)
	{
		BasePlayer basePlayer = ArgEx.Player(arg);
		RaycastHit hitInfo;
		if (basePlayer == null || !Physics.Raycast(basePlayer.eyes.position, basePlayer.eyes.HeadForward(), out hitInfo, 5f, 1218652417, QueryTriggerInteraction.Ignore))
		{
			return;
		}
		BaseEntity entity = RaycastHitEx.GetEntity(hitInfo);
		if (!entity)
		{
			return;
		}
		BasePlayer component = entity.GetComponent<BasePlayer>();
		if ((bool)component && component != basePlayer && !component.IsNpc)
		{
			if (component.IsSleeping())
			{
				component.EndSleeping();
			}
			else
			{
				component.StartSleeping();
			}
		}
	}

	[ServerUserVar]
	public static void kickmember(ConsoleSystem.Arg arg)
	{
		BasePlayer basePlayer = ArgEx.Player(arg);
		if (basePlayer == null)
		{
			return;
		}
		PlayerTeam playerTeam = Instance.FindTeam(basePlayer.currentTeam);
		if (playerTeam != null && !(playerTeam.GetLeader() != basePlayer))
		{
			ulong uLong = arg.GetULong(0, 0uL);
			if (basePlayer.userID != uLong && Interface.CallHook("OnTeamKick", playerTeam, basePlayer, uLong) == null)
			{
				playerTeam.RemovePlayer(uLong);
			}
		}
	}

	[ServerUserVar]
	public static void sendinvite(ConsoleSystem.Arg arg)
	{
		BasePlayer basePlayer = ArgEx.Player(arg);
		PlayerTeam playerTeam = Instance.FindTeam(basePlayer.currentTeam);
		RaycastHit hitInfo;
		if (playerTeam == null || playerTeam.GetLeader() == null || playerTeam.GetLeader() != basePlayer || !Physics.Raycast(basePlayer.eyes.position, basePlayer.eyes.HeadForward(), out hitInfo, 5f, 1218652417, QueryTriggerInteraction.Ignore))
		{
			return;
		}
		BaseEntity entity = RaycastHitEx.GetEntity(hitInfo);
		if ((bool)entity)
		{
			BasePlayer component = entity.GetComponent<BasePlayer>();
			if ((bool)component && component != basePlayer && !component.IsNpc && component.currentTeam == 0L && Interface.CallHook("OnTeamInvite", basePlayer, component) == null)
			{
				playerTeam.SendInvite(component);
			}
		}
	}

	[ServerVar]
	public static void fakeinvite(ConsoleSystem.Arg arg)
	{
		BasePlayer basePlayer = ArgEx.Player(arg);
		ulong uLong = arg.GetULong(0, 0uL);
		PlayerTeam playerTeam = Instance.FindTeam(uLong);
		if (playerTeam != null)
		{
			if (basePlayer.currentTeam != 0L)
			{
				Debug.Log("already in team");
			}
			playerTeam.SendInvite(basePlayer);
			Debug.Log("sent bot invite");
		}
	}

	[ServerVar]
	public static void addtoteam(ConsoleSystem.Arg arg)
	{
		BasePlayer basePlayer = ArgEx.Player(arg);
		PlayerTeam playerTeam = Instance.FindTeam(basePlayer.currentTeam);
		RaycastHit hitInfo;
		if (playerTeam == null || playerTeam.GetLeader() == null || playerTeam.GetLeader() != basePlayer || !Physics.Raycast(basePlayer.eyes.position, basePlayer.eyes.HeadForward(), out hitInfo, 5f, 1218652417, QueryTriggerInteraction.Ignore))
		{
			return;
		}
		BaseEntity entity = RaycastHitEx.GetEntity(hitInfo);
		if ((bool)entity)
		{
			BasePlayer component = entity.GetComponent<BasePlayer>();
			if ((bool)component && component != basePlayer && !component.IsNpc)
			{
				playerTeam.AddPlayer(component);
			}
		}
	}

	public static bool TeamsEnabled()
	{
		return maxTeamSize > 0;
	}

	public override void Load(LoadInfo info)
	{
		base.Load(info);
		if (info.fromDisk && info.msg.relationshipManager != null)
		{
			lastTeamIndex = info.msg.relationshipManager.lastTeamIndex;
			foreach (ProtoBuf.PlayerTeam team in info.msg.relationshipManager.teamList)
			{
				PlayerTeam playerTeam = Pool.Get<PlayerTeam>();
				playerTeam.teamLeader = team.teamLeader;
				playerTeam.teamID = team.teamID;
				playerTeam.teamName = team.teamName;
				playerTeam.members = new List<ulong>();
				foreach (ProtoBuf.PlayerTeam.TeamMember member in team.members)
				{
					playerTeam.members.Add(member.userID);
				}
				teams[playerTeam.teamID] = playerTeam;
			}
			foreach (PlayerTeam value in teams.Values)
			{
				foreach (ulong member2 in value.members)
				{
					playerToTeam[member2] = value;
					BasePlayer basePlayer = FindByID(member2);
					if (basePlayer != null && basePlayer.currentTeam != value.teamID)
					{
						Debug.LogWarning($"Player {member2} has the wrong teamID: got {basePlayer.currentTeam}, expected {value.teamID}. Fixing automatically.");
						basePlayer.currentTeam = value.teamID;
					}
				}
			}
		}
	}
}
