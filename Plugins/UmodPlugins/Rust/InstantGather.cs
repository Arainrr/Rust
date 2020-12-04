﻿
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Instant Gather", "supreme", "1.0.6")]
    [Description("Enhances the tools used for mining in order to gather instantly")]
    public class InstantGather : RustPlugin
    {
        const string permUse = "instantgather.use";
        
        #region Configuration
        
        private Configuration _config;

        private class Configuration
        {
            [JsonProperty(PropertyName = "Tools")]
            public List<string> Tools;
        }
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new Exception();
                SaveConfig();
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        protected override void LoadDefaultConfig()
        {
            _config = new Configuration
            {
                Tools = new List<string>
                {
                    "pickaxe",
                    "jackhammer",
                    "rock",
                    "hammer.salvaged",
                    "icepick.salvaged",
                    "stone.pickaxe",
                    "axe.salvaged",
                    "boneclub",
                    "hatchet",
                    "stonehatchet",
                    "chainsaw"
                }
            };
        }
        
        #endregion
        
        void Init() => permission.RegisterPermission(permUse, this);
        
        private void OnPlayerAttack(BasePlayer attacker, HitInfo hit)
        {
            if (!permission.UserHasPermission(attacker.UserIDString, permUse) || hit.HitEntity == null) return;

            Item item = attacker.GetActiveItem();
   
            if (item == null) return;

            if (!_config.Tools.Contains(item.info.shortname)) return;
   
            BasePlayer target = hit.HitEntity?.ToPlayer();

            BaseEntity entity = hit.HitEntity?.GetEntity();

            OreResourceEntity ore = entity as OreResourceEntity;
            if (ore != null)
            {
                ore._hotSpot?.Kill();
                ore._hotSpot = null;
            }
    
            TreeEntity tree = entity as TreeEntity;
            if (tree != null) tree.hasBonusGame = false;

            if (!(entity is OreResourceEntity || entity is TreeEntity || entity.PrefabName.Contains("driftwood") || entity.PrefabName.Contains("dead_log"))) return;
   
            if (target != null && !target.IsNpc) return;
   
            hit.gatherScale = 100;
        }
    }
}