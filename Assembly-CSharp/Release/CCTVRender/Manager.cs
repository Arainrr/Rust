using Facepunch;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CCTVRender
{
	public static class Manager
	{
		private struct ScoredPlayer
		{
			public BasePlayer Player;

			public float Score;
		}

		private static Dictionary<ulong, Job> _playerAssignments;

		private static MruDictionary<uint, RenderState> _renderStates;

		private static MruDictionary<ulong, ClientState> _clientStates;

		private static RealTimeSince _lastCleanup;

		public static void Initialize()
		{
			_playerAssignments = new Dictionary<ulong, Job>();
			_renderStates = new MruDictionary<uint, RenderState>(250, delegate(uint id, RenderState state)
			{
				Pool.Free(ref state);
			});
			_clientStates = new MruDictionary<ulong, ClientState>(250, delegate(ulong id, ClientState state)
			{
				Pool.Free(ref state);
			});
		}

		public static void Update()
		{
			if (Settings.Enabled && !((float)_lastCleanup < 3f))
			{
				_lastCleanup = 0f;
				using (TimeWarning.New("CCTVRender.Manager.Update"))
				{
					float realtimeSinceStartup = Time.realtimeSinceStartup;
					List<KeyValuePair<ulong, Job>> obj = Pool.GetList<KeyValuePair<ulong, Job>>();
					foreach (KeyValuePair<ulong, Job> playerAssignment in _playerAssignments)
					{
						if (realtimeSinceStartup - playerAssignment.Value.Assigned >= Settings.AssignmentTimeout)
						{
							obj.Add(playerAssignment);
						}
					}
					foreach (KeyValuePair<ulong, Job> item in obj)
					{
						_playerAssignments.Remove(item.Key);
						RenderState value;
						if (_renderStates.TryGetValue(item.Value.NetId, out value))
						{
							value.AbortRequest();
						}
					}
					Pool.FreeList(ref obj);
				}
			}
		}

		public static bool TryRequest(uint requestId, IReceiver receiver, CCTV_RC camera, uint frame)
		{
			if (!Settings.Enabled)
			{
				return false;
			}
			using (TimeWarning.New("CCTVRender.Manager.TryRequest"))
			{
				uint iD = camera.net.ID;
				RenderState value;
				if (!_renderStates.TryGetValue(iD, out value))
				{
					value = Pool.Get<RenderState>();
					value.Initialize(iD);
					_renderStates.Add(iD, value);
				}
				if (value.HasCachedFrame && value.Frame > frame)
				{
					receiver.RenderCompleted(requestId, value.Frame, value.CachedFrame);
					return true;
				}
				if (value.IsLocked)
				{
					return value.AddReceiver(new JobReceiver(requestId, receiver));
				}
				if (value.WasRecentlyRendered)
				{
					return false;
				}
				BasePlayer basePlayer = ChooseEligiblePlayer(camera.ServerPosition);
				if (basePlayer == null)
				{
					return false;
				}
				value.BeginRequest();
				value.AddReceiver(new JobReceiver(requestId, receiver));
				_playerAssignments.Add(basePlayer.userID, new Job(iD, requestId, Time.realtimeSinceStartup));
				Transform transform = camera.viewEyes.transform;
				basePlayer.ClientRPCPlayer(null, basePlayer, "HandleCCTVRenderRequest", transform.position, transform.eulerAngles);
				camera.PingFromExternalViewer();
				return true;
			}
		}

		public static void CompleteRequest(BasePlayer player, Span<byte> jpgImage)
		{
			if (!Settings.Enabled)
			{
				DebugEx.LogWarning($"CompleteRequest from player {player.userID} when feature is disabled");
			}
			else
			{
				using (TimeWarning.New("CCTVRender.Manager.CompleteRequest"))
				{
					Job value;
					if (player == null || !_playerAssignments.TryGetValue(player.userID, out value))
					{
						DebugEx.LogWarning("CompleteRequest with null or unassigned player");
					}
					else
					{
						_playerAssignments.Remove(player.userID);
						RenderState value2;
						if (!_renderStates.TryGetValue(value.NetId, out value2))
						{
							DebugEx.LogWarning("Job completed but RenderState wasn't found!");
						}
						else
						{
							value2.CompleteRequest(jpgImage);
						}
					}
				}
			}
		}

		private static BasePlayer ChooseEligiblePlayer(Vector3 position)
		{
			List<ScoredPlayer> obj = Pool.GetList<ScoredPlayer>();
			GetEligiblePlayers(obj, position);
			if (obj.Count == 0)
			{
				Pool.FreeList(ref obj);
				return null;
			}
			ScoredPlayer scoredPlayer = obj[UnityEngine.Random.Range(0, obj.Count)];
			Pool.FreeList(ref obj);
			BasePlayer player = scoredPlayer.Player;
			ClientState value;
			if (!_clientStates.TryGetValue(player.userID, out value))
			{
				value = Pool.Get<ClientState>();
				_clientStates.Add(player.userID, value);
			}
			value.LastAssigned = Time.realtimeSinceStartup + Mathf.Floor(scoredPlayer.Score);
			return player;
		}

		private static void GetEligiblePlayers(List<ScoredPlayer> scoredPlayers, Vector3 position)
		{
			using (TimeWarning.New("CCTVRender.Manager.GetEligiblePlayers"))
			{
				float realtimeSinceStartup = Time.realtimeSinceStartup;
				foreach (BasePlayer activePlayer in BasePlayer.activePlayerList)
				{
					float num = Vector3.Distance(position, activePlayer.ServerPosition);
					ClientState value;
					if (!(num > Settings.MaxDistance) && !_playerAssignments.ContainsKey(activePlayer.userID) && (!_clientStates.TryGetValue(activePlayer.userID, out value) || !(realtimeSinceStartup - value.LastAssigned < Settings.AssignmentCooldown)))
					{
						float num2 = Mathf.Min(num / Settings.MaxDistance);
						if (activePlayer.GetHeldEntity() is BaseProjectile)
						{
							num2 += 1f;
						}
						num2 += InverseRange(realtimeSinceStartup - activePlayer.stats.combat.LastActive, Settings.CombatTime) * 2f;
						num2 += InverseRange(activePlayer.IdleTime, Settings.IdleTime) * 1f;
						scoredPlayers.Add(new ScoredPlayer
						{
							Player = activePlayer,
							Score = num2
						});
					}
				}
				scoredPlayers.Sort((ScoredPlayer a, ScoredPlayer b) => a.Score.CompareTo(b.Score));
				if (scoredPlayers.Count > 3)
				{
					int num3 = scoredPlayers.Count / 2;
					scoredPlayers.RemoveRange(num3, scoredPlayers.Count - num3);
				}
			}
		}

		private static float InverseRange(float x, float range)
		{
			return 1f - Mathf.Clamp01(x / range);
		}
	}
}
