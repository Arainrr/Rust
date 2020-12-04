﻿using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("More Resources Team", "Owned", "1.0.9")]
    [Description("Get more resources when you're farming close to a team member")]
    class MoreResourcesTeam : CovalencePlugin
    {
      private confData config;
      protected override void LoadDefaultConfig()
      {
          Config.WriteObject(new confData(),true);
      }
      private void Init()
      {
          config = Config.ReadObject<confData>();
      }
      private new void SaveConfig()
      {
          Config.WriteObject(config,true);
      }
      public class confData
      {
          [JsonProperty("Distance between you and your mates (in feet)")]
          public float distanceMate = 32f;

          [JsonProperty("Bonus percentage")]
          public int bonusMate = 10;
      }
      protected override void LoadDefaultMessages()
      {
        lang.RegisterMessages(new Dictionary<string, string>
        {
            ["GatherMoreMessage"] = "You got {0}% more because you're close to a member of your team"
        }, this);
        lang.RegisterMessages(new Dictionary<string, string>
        {
            ["GatherMoreMessage"] = "Vous avez reçu {0}% de ressources en plus car vous êtes proche d'un membre de votre équipe"
        }, this, "fr");
      }
      string GetMessage(string key, string steamId = null) => lang.GetMessage(key, this, steamId);
      private void notifyPlayer(IPlayer player, string text)
      {
          text = Formatter.ToPlaintext(text);
          player.Command("gametip.hidegametip");
          player.Command("gametip.showgametip", text);
          timer.In(5, () => player?.Command("gametip.hidegametip"));
      }
      void OnDispenserGather(ResourceDispenser dispenser, BasePlayer player, Item item)
      {
          var currentTeam = player.currentTeam;
          if(currentTeam != 0)
          {
              RelationshipManager.PlayerTeam team = RelationshipManager.Instance.FindTeam(currentTeam);
              if (team != null)
              {
                var players = team.members;
                if ( team.members.Count > 1 )
                {
                  int totalPercentage = 0;
                  foreach (var teamMember in players)
                  {
                      if(teamMember != player.userID)
                      {
                        BasePlayer member = RelationshipManager.FindByID(teamMember);
                        if(member != null)
                        {
                          if(Vector3.Distance(player.transform.position, member.transform.position) <= config.distanceMate)
                          {
                            totalPercentage = totalPercentage + config.bonusMate;
                            item.amount = item.amount + (item.amount * config.bonusMate/100);
                          }
                        }
                      }
                    }
                    notifyPlayer(player.IPlayer,string.Format(GetMessage("GatherMoreMessage", player.UserIDString), totalPercentage.ToString()));
                }
              }
          }
          return;
      }
    }
}
