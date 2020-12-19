using Facepunch.Extend;
using Facepunch.Math;
using Network;
using Oxide.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ConnectionAuth : MonoBehaviour
{
	[NonSerialized]
	public static List<Connection> m_AuthConnection = new List<Connection>();

	public bool IsConnected(ulong iSteamID)
	{
		if ((bool)BasePlayer.FindByID(iSteamID))
		{
			return true;
		}
		return m_AuthConnection.Any((Connection item) => item.userid == iSteamID);
	}

	public static void Reject(Connection connection, string strReason)
	{
		DebugEx.Log(connection.ToString() + " Rejecting connection - " + strReason);
		Net.sv.Kick(connection, strReason);
		m_AuthConnection.Remove(connection);
	}

	public static void OnDisconnect(Connection connection)
	{
		m_AuthConnection.Remove(connection);
	}

	public void Approve(Connection connection)
	{
		m_AuthConnection.Remove(connection);
		SingletonComponent<ServerMgr>.Instance.connectionQueue.Join(connection);
	}

	public void OnNewConnection(Connection connection)
	{
		connection.connected = false;
		if (connection.token == null || connection.token.Length < 32)
		{
			Reject(connection, "Invalid Token");
			return;
		}
		if (connection.userid == 0L)
		{
			Reject(connection, "Invalid SteamID");
			return;
		}
		if (connection.protocol != 2274)
		{
			if (!DeveloperList.Contains(connection.userid))
			{
				Reject(connection, "Incompatible Version");
				return;
			}
			DebugEx.Log("Not kicking " + connection.userid + " for incompatible protocol (is a developer)");
		}
		if (ServerUsers.Is(connection.userid, ServerUsers.UserGroup.Banned))
		{
			ServerUsers.User user = ServerUsers.Get(connection.userid);
			string text = user?.notes ?? "no reason given";
			string text2 = (user != null && user.expiry > 0) ? (" for " + (user.expiry - Epoch.Current).FormatSecondsLong()) : "";
			Reject(connection, "You are banned from this server" + text2 + " (" + text + ")");
			return;
		}
		if (ServerUsers.Is(connection.userid, ServerUsers.UserGroup.Moderator))
		{
			DebugEx.Log(connection.ToString() + " has auth level 1");
			connection.authLevel = 1u;
		}
		if (ServerUsers.Is(connection.userid, ServerUsers.UserGroup.Owner))
		{
			DebugEx.Log(connection.ToString() + " has auth level 2");
			connection.authLevel = 2u;
		}
		if (DeveloperList.Contains(connection.userid))
		{
			DebugEx.Log(connection.ToString() + " is a developer");
			connection.authLevel = 3u;
		}
		if (IsConnected(connection.userid))
		{
			Reject(connection, "You are already connected!");
		}
		else if (Interface.CallHook("IOnUserApprove", connection) == null)
		{
			m_AuthConnection.Add(connection);
			StartCoroutine(AuthorisationRoutine(connection));
		}
	}

	public IEnumerator AuthorisationRoutine(Connection connection)
	{
		yield return StartCoroutine(Auth_Steam.Run(connection));
		yield return StartCoroutine(Auth_EAC.Run(connection));
		if (!connection.rejected && connection.active)
		{
			BasePlayer basePlayer = BasePlayer.FindByID(connection.userid);
			if ((bool)basePlayer && basePlayer.net.connection != null)
			{
				Reject(connection, "You are already connected as a player!");
			}
			else
			{
				Approve(connection);
			}
		}
	}
}
