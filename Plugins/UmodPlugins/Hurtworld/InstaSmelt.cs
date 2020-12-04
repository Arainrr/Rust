using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Insta Smelt", "Mr. Blue", "2.0.5")]
    [Description("Smelts stacks of items at once")]
    class InstaSmelt : HurtworldPlugin
    {
        protected void OnServerInitialized()
        {
            List<string> instaSmeltItems = new List<string> { "Ash", "Shaped Iron", "Shaped Mondinium", "Shaped Titranium", "Shaped Ultranium", "Shaped Galvanite", "Steak (Cooked)" };
            EntityFluidEffectKey k = RuntimeHurtDB.Instance.GetAll<EntityFluidEffectKey>().FirstOrDefault(e => e.NameKey == "EntityStats/Effects/Storage Temperature");
            if (k == null)
            {
                Puts("Couln't find the EntityFluidEffectKey for Internal Temperature, cancelling...");
                return;
            }
            foreach (var l in RuntimeHurtDB.Instance.GetAll<ItemGeneratorStaticAsset>())
            {
                var trans = l.Components.FirstOrDefault(e => e is ItemComponentTransitionConfig);
                if (trans != null)
                {
                    var trans2 = (ItemComponentTransitionConfig)trans;
                    foreach (var t in trans2.Transitions)
                    {
                        if (t.SourceEffectType == k && instaSmeltItems.Contains(t.ResultItemGenerator.ToString()) && t.Description != "Burns in 50s when over cooked")
                        {
                            t.RequiredDuration = 1;
                            t.TransitionStack = true;
                        }
                    }
                }
            }
        }
    }
}
