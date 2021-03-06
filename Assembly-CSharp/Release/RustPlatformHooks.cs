using ConVar;
using Network;
using Rust;
using Rust.Platform.Common;
using System;
using System.Net;

public class RustPlatformHooks : IPlatformHooks
{
	public static readonly RustPlatformHooks Instance = new RustPlatformHooks();

	public uint SteamAppId => Rust.Defines.appID;

	public ServerParameters? ServerParameters
	{
		get
		{
			if (Network.Net.sv == null)
			{
				return null;
			}
			IPAddress address = null;
			if (!string.IsNullOrEmpty(ConVar.Server.ip))
			{
				address = IPAddress.Parse(ConVar.Server.ip);
			}
			bool flag = !Network.Net.sv.AllowPassthroughMessages || (ConVar.Server.queryport > 0 && ConVar.Server.queryport != ConVar.Server.port);
			if (flag && (ConVar.Server.queryport <= 0 || ConVar.Server.queryport == ConVar.Server.port))
			{
				throw new Exception("Query port isn't set up properly");
			}
			return new ServerParameters("rust", "Rust", 2274.ToString(), ConVar.Server.secure, address, (ushort)Network.Net.sv.port, (ushort)(flag ? ((ushort)ConVar.Server.queryport) : 0));
		}
	}

	public void Abort()
	{
		Application.Quit();
	}

	public void OnItemDefinitionsChanged()
	{
		ItemManager.InvalidateWorkshopSkinCache();
	}

	public void AuthSessionValidated(ulong userId, ulong ownerUserId, AuthResponse response)
	{
		SingletonComponent<ServerMgr>.Instance.OnValidateAuthTicketResponse(userId, ownerUserId, response);
	}
}
