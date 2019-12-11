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
    class RuleCalculateBaseCMB__OnTrigger__Patch
    {
        static bool Prefix(RuleCalculateBaseCMB __instance)
        {
            if (__instance.ReplaceBAB.HasValue)
            {
                return true;
            }
            //Main.logger.Log("Attack Roll Check");
            var attack = Rulebook.CurrentContext.AllEvents.LastOfType<RuleAttackWithWeapon>();
            if (attack == null || (attack.AttackRoll != null && attack.AttackRoll.IsTriggererd))
            {
                //if maneuver happens after attack roll than it means it is a free attempt or ther is no corresponding attack with weapon than it means it is
                // a (probably free) attempt and we should use normal cmb here
                //if maneuver is associated with weapon attack and happens before attack roll than we assume it replaces an attack and thus we just take corresponding attack bonus
                //
                return true;
            }
            // in order to properly get attack bonus we normally need to trigger RuleCalculateAttackBonus
            // the problem is that a lot of abilities add additional attack bonuses in OnEventAboutToTrigger( RuleAttackRoll),
            // and thus might happen after we trigger this combat maneuver
            // so instead we trigger a complete RuleAttackRoll to correctly get the bonus
            // as an unfortunate side effect it might trigger something that should not be triggered (like limited use rerolls), so it is not ideal
            // so we make a trick - we patch RuleAttackRoll to always trigger RuleCalculateAttackBonus and than call a fake RuleAttackRoll with auto hit 
            // which does not make a roll

            var attack_roll = new RuleAttackRoll(attack.Initiator, attack.Target, attack.Weapon, 0);
            attack_roll.IgnoreConcealment = true;
            attack_roll.AutoHit = true;
            attack_roll.SuspendCombatLog = true;

            var AttackBonus = Rulebook.Trigger<RuleAttackRoll>(attack_roll).AttackBonus;

            //var AttackBonus = Rulebook.Trigger<RuleCalculateAttackBonus>(new RuleCalculateAttackBonus(attack.Initiator, attack.Target, attack.Weapon, attack.IsFirstAttack ? 0 : attack.AttackBonusPenalty)).Result;
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


    [Harmony12.HarmonyPatch(typeof(RuleAttackRoll))]
    [Harmony12.HarmonyPatch("OnTrigger", Harmony12.MethodType.Normal)]
    class RuleAttackRoll__OnTrigger__Patch
    {
        //force to always calcualte ruleAttackBonus
        static bool Prefix(RuleAttackRoll __instance)
        {
            var tr = Harmony12.Traverse.Create(__instance);
            if (!__instance.WeaponStats.IsTriggererd)
                Rulebook.Trigger<RuleCalculateWeaponStats>(__instance.WeaponStats);

            tr.Property("ACRule").SetValue(Rulebook.Trigger<RuleCalculateAC>(new RuleCalculateAC(__instance.Initiator, __instance.Target, __instance.AttackType)));
            tr.Property("IsTargetFlatFooted").SetValue(__instance.ACRule.IsTargetFlatFooted);
            tr.Property("TargetAC").SetValue(__instance.ACRule.TargetAC);
            tr.Property("AttackBonusRule").SetValue(Rulebook.Trigger<RuleCalculateAttackBonus>(new RuleCalculateAttackBonus(__instance.Initiator, __instance.Target, __instance.Weapon, __instance.AttackBonusPenalty)));
            tr.Property("AttackBonus").SetValue(__instance.AttackBonusRule.Result);

            return true;
        }
    }
}
