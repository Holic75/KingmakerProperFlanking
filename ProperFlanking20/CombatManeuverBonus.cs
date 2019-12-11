using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProperFlanking20.CombatManeuverBonus
{

    [Harmony12.HarmonyPatch(typeof(RuleCalculateBaseCMB))]
    [Harmony12.HarmonyPatch("OnTrigger", Harmony12.MethodType.Normal)]
    class OutflankAttackBonus__OnEventAboutToTrigger__Patch
    {
        static bool Prefix(RuleCalculateBaseCMB __instance)
        {
            //Main.logger.Log("Attack Roll Check");
            var attack = Rulebook.CurrentContext.AllEvents.LastOfType<RuleAttackWithWeapon>();
            if (attack == null)
            {
                return true;
            }

            var AttackBonus = Rulebook.Trigger<RuleCalculateAttackBonus>(new RuleCalculateAttackBonus(attack.Initiator, attack.Target, attack.Weapon, attack.IsFirstAttack ? 0 : attack.AttackBonusPenalty)).Result;
            var ResultSizeBonus = __instance.Initiator.Descriptor.State.Size.GetModifiers().CMDAndCMD;
            var ResultMiscBonus = (int)__instance.Initiator.Stats.AdditionalCMB;
            
            //Main.logger.Log("Attack Detected: " + AttackBonus.ToString());
            //Main.logger.Log("Misc: " + ResultMiscBonus.ToString());
            //Main.logger.Log("Size: " + ResultSizeBonus.ToString());
            //Main.logger.Log("Additional Bonus: " + __instance.AdditionalBonus.ToString());

            var tr = Harmony12.Traverse.Create(__instance);
            tr.Property("Result").SetValue(AttackBonus + ResultSizeBonus + ResultMiscBonus + __instance.AdditionalBonus);
            return false;
        }
    }
}
