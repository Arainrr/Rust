using System.Collections.Generic;
using Oxide.Core.Configuration;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using System;
using System.Linq;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Sleeping Assignee", "Ryz0r", "1.0.4")]
	[Description("Allows you to check whom a sleeping bag or bed is assigned to, and who it was deployed by.")]
    class SleepingAssignee : RustPlugin
    {
        private Configuration config;
        private const string UsePerm = "sleepingassignee.use";
        
      	private class Configuration
		{
			[JsonProperty(PropertyName = "CommandToCheck")]
			public string CommandToCheck = "bag";
		}
    		
    	protected override void LoadConfig()
    	{
			base.LoadConfig();
			try
			{
				config = Config.ReadObject<Configuration>();
				if (config == null) throw new Exception();
					SaveConfig();
				}
			catch
			{
				PrintError("Your configuration file contains an error. Using default configuration values.");
				LoadDefaultConfig();
			}
		}
		
		protected override void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			{
				["NoTarget"] = "There is no valid object that you are looking at!",
				["AssignedTo"] = "This sleeping bag/bed has been assigned to {0}, and was deployed by {1}.",
				["NoPerm"] = "You lack the required permissions to use this command.",
				["NotBag"] = "This object is not a sleeping bag or a bed."
			}, this);
		}
		
		protected override void LoadDefaultConfig()
		{
			PrintWarning("A new configuration file is being generated.");
			config = new Configuration();
		}
		
		
		protected override void SaveConfig() => Config.WriteObject(config);    

        private void Init()
        {
            permission.RegisterPermission(UsePerm, this);

            cmd.AddChatCommand(config.CommandToCheck, this, nameof(CheckBag));
        }
		
		private void CheckBag(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, UsePerm))
            {
                player.ChatMessage(lang.GetMessage("NoPerm", this, player.UserIDString));
                return;
            }
            
			RaycastHit raycastHit;
			if(Physics.Raycast(player.eyes.HeadRay(), out raycastHit))
			{
				var target = raycastHit.GetEntity();
				
				if (!target)
				{
					player.ChatMessage(lang.GetMessage("NoTarget", this, player.UserIDString));
					return;
				}
				
				if (target is SleepingBag)
				{
					SleepingBag sleepingBag = target as SleepingBag;
					player.ChatMessage(string.Format(lang.GetMessage("AssignedTo", this, player.UserIDString), GetPlayerName(sleepingBag.deployerUserID), GetPlayerName(sleepingBag.OwnerID)));
				}
				else
				{
				    player.ChatMessage(lang.GetMessage("NotBag", this, player.UserIDString));
				}
            }
			else
            {
                player.ChatMessage(lang.GetMessage("NoTarget", this, player.UserIDString));
				return;
            }
				
		}
        
        string GetPlayerName (ulong playerID)
        {
			var player = covalence.Players.FindPlayerById(playerID.ToString());
			if(player != null) {
				return player.Name;
			} else {
				return playerID + " (Unknown Player)";
			}
        }
    }
}