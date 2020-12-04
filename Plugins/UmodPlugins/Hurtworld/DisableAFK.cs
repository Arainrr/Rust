using System;

namespace Oxide.Plugins
{
	[Info("Disable AFK", "Obito", "1.0.0")]
	[Description("Disable the hurtworld afk system.")]

	class DisableAFK : HurtworldPlugin
	{
		float infinite = float.PositiveInfinity;
		
		void OnServerInitialized()
		{
			if (GameManager.Instance.ServerConfig.AfkKickTime != infinite || AFKManager.Instance.WarningTime != infinite)
			{
				try
				{
					var warnTime = AFKManager.Instance.WarningTime = infinite;
					var kickTime = GameManager.Instance.ServerConfig.AfkKickTime = infinite;
					PrintWarning($"The afk kick time as changed to: {kickTime.ToString()}\n"
								+ $"The afk warning time as changed to: {warnTime.ToString()}");
				}
				catch (Exception e)
				{
					PrintError(e.Message);
				}
			}
		}
	}
}