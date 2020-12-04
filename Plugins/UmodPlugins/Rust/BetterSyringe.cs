﻿using System;
using System.Collections.Generic;
using Facepunch.Extend;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Better Syringe", "Default", "1.4.2")]
    [Description("Buffs healing items.")]
    public class BetterSyringe : RustPlugin
    {

        public bool Changed = true;

        private float syringeHealAmount = 35f;
        private float syringePendingAmount = 25f;
        private float syringeRadRemoveAmount = 25f;
        private bool syringeBleedCancel = true;
        private float bandageHealAmount = 10f;
        private float bandagePendingAmount = 5f;
        private float medkitHealAmount = 15f;
        //private float medkitPendingAmount = 35f;


        private static string permissionName = "bettersyringe.use";

        void Init()
        {
            LoadVariables();
            permission.RegisterPermission(permissionName, this);

        }

        object OnHealingItemUse(MedicalTool tool, BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, permissionName)) {return null;}

            if (tool.GetItem()?.info.shortname.Contains("syringe") == true)
            {
                player.health = player.health + syringeHealAmount;
                player.metabolism.pending_health.value = player.metabolism.pending_health.value + syringePendingAmount;
                player.metabolism.radiation_poison.value = player.metabolism.radiation_poison.value - syringeRadRemoveAmount;
                if (syringeBleedCancel)
                {
                    player.metabolism.bleeding.value = 0f;
                }

            }

            else if (tool.GetItem()?.info.shortname.Contains("bandage") == true)
            {

                player.health = player.health + bandageHealAmount;
                player.metabolism.pending_health.value = player.metabolism.pending_health.value + bandagePendingAmount;
                player.metabolism.bleeding.value = 0f;

            }
            /*else if (tool.GetItem()?.info.shortname.Contains("medkit") == true)
            {
                player.metabolism.pending_health.value = medkitPendingAmount;
            }*/
            return true;
        }

        object OnItemAction(Item item, string action, BasePlayer player)
        {
            if (!item.info.shortname.Contains("medkit"))
            {
                return null;
            }

            if (!permission.UserHasPermission(player.UserIDString, permissionName)) { return null; }

            //player.metabolism.pending_health.value = 0f;


            //player.metabolism.pending_health.value = medkitPendingAmount;

            player.health = player.health + medkitHealAmount;
            player.metabolism.bleeding.value = 0f;

            return null;
        }


        object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value; 
                Changed = true;
            }
            return value;
        }

        void LoadVariables()
        {

            syringeHealAmount = Convert.ToSingle(GetConfig("Syringes", "Amount to heal (on use)", 5f));
            syringePendingAmount = Convert.ToSingle(GetConfig("Syringes", "Pending health to add", 15f));
            syringeRadRemoveAmount = Convert.ToSingle(GetConfig("Syringes", "Amount of rads to remove on use", 25f));
            syringeBleedCancel = Convert.ToBoolean(GetConfig("Syringes", "Should syringes remove bleeding effect?", true));
            bandageHealAmount = Convert.ToSingle(GetConfig("Bandages", "Amount to heal (on use)", 30f));
            bandagePendingAmount = Convert.ToSingle(GetConfig("Bandages", "Pending health to add", 60f));
            medkitHealAmount = Convert.ToSingle(GetConfig("Medkits", "Amount to heal (on use)", 15f));
            //medkitPendingAmount = Convert.ToSingle(GetConfig("Medkits", "Pending health to add", 35f));

            if (!Changed) return;
            SaveConfig();
            Changed = false;
        }

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            LoadVariables();
        }
        
        
    }
}