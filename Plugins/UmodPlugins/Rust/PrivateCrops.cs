﻿using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
	[Info("Private Crops", "NubbbZ", "1.1.4")]
	[Description("Protects player's crops from being stolen!")]
	class PrivateCrops : CovalencePlugin
	{
		#region Variables
		private const string messagebypass = "privatecrops.message.bypass";
		private const string protectionbypass = "privatecrops.protection.bypass";
		#endregion

		#region Setup
		private void Init()
		{
			permission.RegisterPermission(messagebypass, this);
			permission.RegisterPermission(protectionbypass, this);
		}

		protected override void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			{
				["message"] = "<color={0}>This crop is not yours! Do not steal from other players!</color>",
			}, this);
		}

		protected override void LoadDefaultConfig()
		{
			LogWarning("Creating a new configuration file");
			Config["ToolCupboardArea"] = true;
			Config["MessageColor"] = "#ff0000";
		}
		#endregion

		#region Hooks
		private object CanTakeCutting(BasePlayer player, GrowableEntity growable)
		{
			if (player == null | growable == null) return null;
			return CropsProtected(player, growable);
		}

		private object OnGrowableGather(GrowableEntity growable, BasePlayer player)
		{
			if (player == null | growable == null) return null;
			return CropsProtected(player, growable);
		}
		#endregion

		#region Helpers
		public object CropsProtected(BasePlayer player, GrowableEntity growable)
		{
			if (player.IPlayer.HasPermission(protectionbypass) == true)
			{
				return null;
			}

			if ((bool)Config["ToolCupboardArea"] == true)
			{
				BuildingPrivlidge TC = player.GetBuildingPrivilege();
				if (TC?.IsAuthed(player) == false)
				{
					WarnPlayer(player);
					return true;
				}
			}
			else if (growable.OwnerID != player.userID)
			{
				WarnPlayer(player);
				return true;
			}

			return null;
		}

		public void WarnPlayer(BasePlayer player)
		{
			if (player.IPlayer.HasPermission(messagebypass) == false)
			{
				player.IPlayer.Message(string.Format(lang.GetMessage("message", this, player.IPlayer.Id), Config["MessageColor"]));
			}
		}
		#endregion
	}
}
