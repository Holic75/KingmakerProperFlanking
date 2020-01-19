using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProperFlanking20.CombatManeuverBonus
{
    public class UnitPartUseWeaponForCombatManeuver : CallOfTheWild.AdditiveUnitPart
    {
        bool forced = false;
        ItemEntityWeapon weapon = null;
        int forced_penalty;
        public bool active()
        {
            return forced || !buffs.Empty();
        }

        public bool isForced()
        {
            return forced;
        }

        public ItemEntityWeapon forcedWeapon()
        {
            return weapon;
        }


        public void force(ItemEntityWeapon with_weapon, int penalty)
        {
            forced = true;
            weapon = with_weapon;
            forced_penalty = penalty;
        }


        public void unForce()
        {
            forced = false;
            weapon = null;
            forced_penalty = 0;
        }


        public int forcedPenalty()
        {
            return forced_penalty;
        }
    }


    public class UseWeaponForCombatManeuverLogic : BuffLogic
    {
        public override void OnTurnOn()
        {
            this.Owner.Ensure<UnitPartUseWeaponForCombatManeuver>().addBuff(this.Fact);
        }


        public override void OnTurnOff()
        {
            this.Owner.Ensure<UnitPartUseWeaponForCombatManeuver>().removeBuff(this.Fact);
        }
    }


    [Harmony12.HarmonyPatch(typeof(RuleCalculateBaseCMB))]
    [Harmony12.HarmonyPatch("OnTrigger", Harmony12.MethodType.Normal)]
    class RuleCalculateBaseCMB__OnTrigger__Patch
    {
        static bool Prefix(RuleCalculateBaseCMB __instance)
        {
            if (__instance.ReplaceBAB.HasValue)
            {
                // if bab is replaced it is not an attack (very likely a spell or some other ability)
                return true;
            }

            var attack = Rulebook.CurrentContext.AllEvents.LastOfType<RuleAttackWithWeapon>();
            var maneuver = Rulebook.CurrentContext.AllEvents.LastOfType<RuleCombatManeuver>();
            if (maneuver == null || !maneuverAsAttack(maneuver.Type, __instance.Initiator))
            {
                return true;
            }

            var weapon = __instance.Initiator.Body?.PrimaryHand?.MaybeWeapon;
            var penalty = 0;

            if (attack != null)
            {
                weapon = attack.Weapon;
                if (attack.AttackRoll != null && attack.AttackRoll.IsTriggererd)
                {
                    //if maneuver is after attack - it is a free attempt that is using iterative attack bonus
                    penalty = attack.AttackBonusPenalty;
                }
            }
            else if (__instance.Initiator.Ensure<UnitPartUseWeaponForCombatManeuver>().active())
            {
                var forced_weapon = __instance.Initiator.Ensure<UnitPartUseWeaponForCombatManeuver>().forcedWeapon();
                if (forced_weapon != null)
                {
                    weapon = forced_weapon;
                }
                penalty = __instance.Initiator.Ensure<UnitPartUseWeaponForCombatManeuver>().forcedPenalty();
            }
            else
            {
                return true;
            }

            if (weapon == null || !weapon.Blueprint.IsMelee)
            {
                //no maneuvers without weapon or ranged weapon
                return true;
            }
            // in order to properly get attack bonus we normally need to trigger RuleCalculateAttackBonus
            // the problem is that a lot of abilities add additional attack bonuses in OnEventAboutToTrigger( RuleAttackRoll),
            // and thus might happen after we trigger this combat maneuver
            // so instead we trigger a complete RuleAttackRoll to correctly get the bonus
            // as an unfortunate side effect it might trigger something that should not be triggered (like limited use rerolls), so it is not ideal
            // so we make a trick - we patch RuleAttackRoll to always trigger RuleCalculateAttackBonus and than call a fake RuleAttackRoll with auto hit 
            // which does not make a roll
            
            var attack_roll = new RuleAttackRoll(maneuver.Initiator, maneuver.Target, weapon, penalty);
            attack_roll.IgnoreConcealment = true;
            attack_roll.AutoHit = true;
            attack_roll.SuspendCombatLog = true;

            var AttackBonus = Rulebook.Trigger<RuleAttackRoll>(attack_roll).AttackBonus;

            var ResultSizeBonus = __instance.Initiator.Descriptor.State.Size.GetModifiers().CMDAndCMD + __instance.Initiator.Descriptor.State.Size.GetModifiers().AttackAndAC;
            var ResultMiscBonus = (int)__instance.Initiator.Stats.AdditionalCMB;

            /*Main.logger.Log("Attack Detected: " + AttackBonus.ToString());
            Main.logger.Log("Misc: " + ResultMiscBonus.ToString());
            Main.logger.Log("Size: " + ResultSizeBonus.ToString());
            Main.logger.Log("Additional Bonus: " + __instance.AdditionalBonus.ToString());*/

            var tr = Harmony12.Traverse.Create(__instance);
            tr.Property("Result").SetValue(AttackBonus + ResultSizeBonus + ResultMiscBonus + __instance.AdditionalBonus);
            return false;
        }


        static bool maneuverAsAttack(CombatManeuver maneuver, UnitEntityData unit)
        {
            return (maneuver == CombatManeuver.Trip || maneuver == CombatManeuver.Disarm || maneuver == CombatManeuver.SunderArmor || maneuver == CombatManeuver.BullRush)
                   || ((maneuver == CombatManeuver.DirtyTrickBlind || maneuver == CombatManeuver.DirtyTrickEntangle || maneuver == CombatManeuver.DirtyTrickSickened)
                      && unit.Descriptor.Progression.Features.HasFact(NewFeats.quick_dirty_trick));
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
