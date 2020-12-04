using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.DiscordEvents;
using Oxide.Ext.Discord.DiscordObjects;
using Oxide.Ext.Discord.Exceptions;

namespace Oxide.Plugins
{
    [Info("Discord Team", "Owned", "1.1.6")]
    [Description("Creates a private voice channel in Discord when creating a team in-game")]
    class DiscordTeam : CovalencePlugin
    {
      #region Plugin variables
      [PluginReference] private Plugin DiscordConnect, DiscordAuth;
      [DiscordClient] private DiscordClient _client;
      private Role role;
      private Hash<string, Channel> listTeamChannels = new Hash<string, Channel>();
      private confData config;
      private bool _init;
      #endregion
      protected override void LoadDefaultConfig()
      {
        Config.WriteObject(new confData(),true);
      }
      public class confData
      {
        [JsonProperty("Discord Bot Token")]
        public string Token = string.Empty;

        [JsonProperty("Change channel when user create the team")]
        public bool moveLeader = true;

        [JsonProperty("Discord users can see other team's private vocal channel")]
        public bool seeOtherTeam = false;

        [JsonProperty("Using roles")]
        public bool roleUsage = false;

        [JsonProperty("Name of the player's role on discord (not @everyone)")]
        public string rolePlayer = "Player";

        [JsonProperty("Max players in a voice channel")]
        public int maxPlayersChannel = 3;
      }

      protected override void LoadDefaultMessages()
      {
        lang.RegisterMessages(new Dictionary<string, string>
        {
          ["messageChannelCreated"] = "A private voice channel was created on Discord for your team !",
          ["messageChannelDeleted"] = "Your private voice channel on Discord has been deleted !",
          ["messageMemberJoin"] = "You have been added to your team's private voice channel on Discord !",
          ["messageMemberLeft"] = "You have been removed from your team's private voice channel on Discord !",
          ["channelName"] = "{0}'s Team"

        }, this);

        lang.RegisterMessages(new Dictionary<string, string>
        {
          ["messageChannelCreated"] = "Un salon vocal privé a été crée sur Discord pour votre équipe !",
          ["messageChannelDeleted"] = "Votre salon vocal privé sur Discord a été supprimé !",
          ["messageMemberJoin"] = "Vous avez été ajouté au salon vocal privé de votre équipe sur Discord !",
          ["messageMemberLeft"] = "Vous avez été retiré du salon vocal privé de votre équipe sur Discord !",
          ["channelName"] = "L'équipe de {0}"
        }, this, "fr");
      }

      string GetMessage(string key, string steamId = null) => lang.GetMessage(key, this, steamId);
      private void Init()
      {
        config = Config.ReadObject<confData>();
      }
      private void OnServerInitialized()
      {
        StartDiscordTeam();
      }

      private void StartDiscordTeam()
      {
        if (!string.IsNullOrEmpty(config.Token))
        {
            Discord.CreateClient(this, config.Token);
        }
        else
        {
          PrintError("Discord Bot Token (API key) in the configuration file is missing");
          return;
        }
        if(_client?.DiscordServer != null)
        {
          if(config.roleUsage)
          {
            if(string.IsNullOrEmpty(config.rolePlayer))
            {
              PrintError("The role specified in the configuration file is missing");
              return;
            }
          }
          Puts("Discord Team initialized");
          _init = true;
          initializeTeam();
        }
        else
        {
          Puts("There is no Discord connection ... Please check your API key");
        }
      }

      void Unload()
      {
        if(_init)
        {
          if(listTeamChannels.Count > 0)
          {
            foreach(var leaderGameId in listTeamChannels)
            {
              deleteChannel(listTeamChannels[leaderGameId.Key]);
            }
            listTeamChannels.Clear();
          }
        }
      }

      private void OnUserConnected(IPlayer player)
      {
        if(_init)
        {
          string info = GetDiscord(player.Id);
          if (info == null)
          {
            return;
          }
          else
          {
            BasePlayer basePlayer = player.Object as BasePlayer;
            var currentTeam = basePlayer.currentTeam;
            if(currentTeam != 0)
            {
              RelationshipManager.PlayerTeam team = RelationshipManager.Instance.FindTeam(currentTeam);
              string leaderId = team.teamLeader.ToString();
              if(listTeamChannels[leaderId] != null)
              {
                if(basePlayer.UserIDString != leaderId)
                {
                  addPlayerChannel(basePlayer, listTeamChannels[leaderId]);
                }
              }
              else
              {
                if(basePlayer.UserIDString == leaderId)
                {
                  CreateChannelGuild(basePlayer);
                }
              }
            }
          }
        }
      }

      object OnTeamCreate(BasePlayer player)
      {
        if(_init)
        {
          string info = GetDiscord(player.UserIDString);
          if(info != null)
          {
            CreateChannelGuild(player);
          }
        }
        return null;
      }

      object OnTeamPromote(RelationshipManager.PlayerTeam team, BasePlayer newLeader)
      {
        if(_init)
        {
          string info = GetDiscord(newLeader.UserIDString);
          if(info != null)
          {
            string newLeaderId = newLeader.UserIDString;
            string oldLeaderId = team.teamLeader.ToString();
            if(listTeamChannels[oldLeaderId] != null)
            {
              listTeamChannels[newLeaderId] = listTeamChannels[oldLeaderId];
              listTeamChannels.Remove(oldLeaderId);
              renameChannel(listTeamChannels[newLeaderId], newLeader);
            }
          }
        }
        return null;
      }

      object OnTeamLeave(RelationshipManager.PlayerTeam team, BasePlayer player)
      {
        if(_init)
        {
          string info = GetDiscord(player.UserIDString);
          if(info != null)
          {
            string leaderId = team.teamLeader.ToString();
            if(listTeamChannels[leaderId] != null)
            {
              if(leaderId != player.UserIDString)
              {
                removePlayerChannel(player, listTeamChannels[leaderId]);
              }
            }
          }
        }
        return null;
        //quit channel
      }

      object OnTeamKick(RelationshipManager.PlayerTeam team, BasePlayer player)
      {
        if(_init)
        {
          string info = GetDiscord(player.UserIDString);
          if(info != null)
          {
            string leaderId = team.teamLeader.ToString();
            if(listTeamChannels[leaderId] != null)
            {
              removePlayerChannel(player, listTeamChannels[leaderId]);
            }
          }
        }
        return null;
      }

      object OnTeamAcceptInvite(RelationshipManager.PlayerTeam team, BasePlayer player)
      {
        if(_init)
        {
          string info = GetDiscord(player.UserIDString);
          if(info != null)
          {
            string leaderId = team.teamLeader.ToString();
            if(listTeamChannels[leaderId] != null)
            {
              addPlayerChannel(player, listTeamChannels[leaderId]);
            }
          }
        }
        return null;
      }

      void OnTeamDisbanded(RelationshipManager.PlayerTeam team)
      {
        if(_init)
        {
          string leaderId = team.teamLeader.ToString();
          var player = team.GetLeader();
          if(listTeamChannels[leaderId] != null)
          {
            deleteChannel(listTeamChannels[leaderId]);
            listTeamChannels.Remove(leaderId);
            if(player != null)
            {
              player.ChatMessage(GetMessage("messageChannelDeleted", leaderId));
            }
          }
        }
      }

      public void CreateChannelGuild(BasePlayer player)
      {
        string discordId = GetDiscord(player.UserIDString);
        if(discordId != null)
        {
          string playerId = player.UserIDString;
          List<Overwrite> permissionList = new List<Overwrite>();
          Role rolePlayer;
          if(config.roleUsage)
          {
            rolePlayer = GetRoleByName(config.rolePlayer);
            Role roleEveryone = GetRoleByName("@everyone");
            permissionList.Add(new Overwrite {id = roleEveryone.id,type = "role",deny = 66061568});
          }
          else
          {
            rolePlayer = GetRoleByName("@everyone");
          }
          if(config.seeOtherTeam)
          {
            permissionList.Add(new Overwrite {id = rolePlayer.id,type = "role",allow = 1024,deny = 66060544});
          }
          else
          {
            permissionList.Add(new Overwrite {id = rolePlayer.id,type = "role",deny = 66060544});
          }
          permissionList.Add(new Overwrite {id = discordId,type = "member",allow = 36701184});
          GuildMember guildMember = GetGuildMember(player.UserIDString);
          _client.DiscordServer.CreateGuildChannel(_client, string.Format(GetMessage("channelName", playerId), player.displayName), ChannelType.GUILD_VOICE, null, config.maxPlayersChannel, permissionList, null, channelCreated =>
          {
            listTeamChannels[playerId] = channelCreated;
            if(config.moveLeader)
            {
              _client.DiscordServer.ModifyGuildMember(_client, guildMember, guildMember.roles, channelCreated.id);
            }
            player.ChatMessage(GetMessage("messageChannelCreated", player.UserIDString));
          });
        }
      }

      public void addPlayerChannel(BasePlayer player, Channel channel)
      {
        string discordId = GetDiscord(player.UserIDString);
        if(discordId != null)
        {
          channel.EditChannelPermissions(_client, discordId, 36701184, null, "member");
          player.ChatMessage(GetMessage("messageMemberJoin", player.UserIDString));
        }
      }

      public void removePlayerChannel(BasePlayer player, Channel channel)
      {
        string discordId = GetDiscord(player.UserIDString);
        if(discordId != null)
        {
          string playerId = player.UserIDString;
          GuildMember guildMember = GetGuildMember(playerId);
          _client.DiscordServer.ModifyGuildMember(_client, guildMember, guildMember.roles, null);
          if(config.seeOtherTeam)
          {
            channel.EditChannelPermissions(_client, discordId, 1024, 66060544, "member");
          }
          else
          {
            channel.EditChannelPermissions(_client, discordId, null, 66060544, "member");
          }
          player.ChatMessage(GetMessage("messageMemberLeft", player.UserIDString));
        }
      }

      public void initializeTeam()
      {
        if(_init)
        {
          foreach (var player in BasePlayer.activePlayerList)
          {
            if(player.currentTeam != 0)
            {
              RelationshipManager.PlayerTeam team = RelationshipManager.Instance.FindTeam(player.currentTeam);
              string leaderId = team.teamLeader.ToString();
              if(leaderId == player.UserIDString)
              {
                if(listTeamChannels[leaderId] != null)
                {
                  foreach (var teamMember in team.members)
                  {
                    if(teamMember.ToString() != leaderId)
                    {
                      string discordId = GetDiscord(player.UserIDString);
                      if(discordId != null)
                      {
                        BasePlayer member = RelationshipManager.FindByID(teamMember);
                        if(member != null)
                        {
                          addPlayerChannel(member, listTeamChannels[leaderId]);
                        }
                      }
                    }
                  }
                }
                else
                {
                  string discordId = GetDiscord(player.UserIDString);
                  if(discordId != null)
                  {
                    CreateChannelGuild(player);
                  }
                }
              }
            }
          }
        }
      }

      #region Discord functions
      public void deleteChannel(Channel channel)
      {
        channel.DeleteChannel(_client);
      }

      public void renameChannel(Channel channel, BasePlayer newLeader)
      {
        channel.name = string.Format(GetMessage("channelName", newLeader.UserIDString), newLeader.displayName);
        channel.ModifyChannel(_client,channel);
      }

      public Role GetRoleByName(string roleName)
      {
        return _client.DiscordServer.roles.FirstOrDefault(r => string.Equals(r.name, roleName, StringComparison.OrdinalIgnoreCase));
      }

      private string GetDiscord(string steamId)
      {
          if (DiscordAuth != null)
          return (string) DiscordAuth.Call("API_GetDiscord", steamId);

          if (DiscordConnect != null)
          return (string) DiscordConnect.Call("GetDiscordOf", steamId);

          return null;
      }

      public GuildMember GetGuildMember(string steamId)
      {
        string discordId = GetDiscord(steamId);
        if(discordId != null)
        {
          return _client.DiscordServer.members.FirstOrDefault(m => discordId == m.user.id);
        }
        return null;
      }
      #endregion
    }
}
