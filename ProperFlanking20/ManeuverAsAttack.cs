using Kingmaker.Blueprints.Root;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProperFlanking20.ManeuverAsAttack
{

    public class UnitPartAttackReplacementWithAction : CallOfTheWild.AdditiveUnitPart
    {
        public bool maybeReplaceAttackWithAction(RuleAttackWithWeapon attack_rule)
        {
            if (buffs.Empty())
            {
                return false;
            }

            //Main.logger.Log("Checking For Attack Replacement");
            foreach (var b in buffs)
            {
                if (b.Get<AttackReplacementWithActionLogic>().maybeReplaceAttackWithAction(attack_rule))
                {
                    //Main.logger.Log("Attack Replaced");
                    return true;
                }
            }

            return false;
        }
    }


    public abstract class AttackReplacementWithActionLogic : BuffLogic
    {
        public abstract bool maybeReplaceAttackWithAction(RuleAttackWithWeapon attack_rule);

        public override void OnTurnOn()
        {
            this.Owner.Ensure<UnitPartAttackReplacementWithAction>().addBuff(this.Fact);
        }


        public override void OnTurnOff()
        {
            this.Owner.Ensure<UnitPartAttackReplacementWithAction>().removeBuff(this.Fact);
        }
    }


    public class AttackReplacementWithCombatManeuver : AttackReplacementWithActionLogic
    {
        public CombatManeuver maneuver;
        public bool only_full_attack = false;
        public bool only_first_attack = false;

        public override bool maybeReplaceAttackWithAction(RuleAttackWithWeapon attack_rule)
        {
            //Main.logger.Log("Checking for replacement with: " + maneuver.ToString() );
            if (!attack_rule.IsFullAttack && only_full_attack)
            {
                return false;
            }

            if (attack_rule.AttackNumber != 0 && only_first_attack)
            {
                return false;
            }

            if (!attack_rule.Weapon.Blueprint.IsMelee)
            {
                return false;
            }

            //Main.logger.Log("First Conditions Ok");
            if (maneuver == CombatManeuver.Trip)
            {
                UnitState state = attack_rule.Target.Descriptor.State;
                // same checks as in UnitProneController, if this is true (and the unit is not in a cutscene), state.Prone.Active will be true on the next tick and we also don't want to trip again.
                if (state.Prone.Active || state.Prone.ShouldBeActive || !state.IsConscious || state.HasCondition(UnitCondition.Prone) || state.HasCondition(UnitCondition.Sleeping) || state.HasCondition(UnitCondition.Unconscious))
                {
                    return false;
                }
            }
            else if (maneuver == CombatManeuver.Disarm)
            {
                // same checks as in RuleCombatManeuver. If the unit cannot be disarmed (further), don't attempt to disarm.
                ItemEntityWeapon maybe_weapon = attack_rule.Target.Body.PrimaryHand.MaybeWeapon;
                ItemEntityWeapon maybe_weapon2 = attack_rule.Target.Body.SecondaryHand.MaybeWeapon;
                bool can_disarm = false;
                if (maybe_weapon != null && !maybe_weapon.Blueprint.IsUnarmed && !maybe_weapon.Blueprint.IsNatural && !attack_rule.Target.Descriptor.Buffs.HasFact(BlueprintRoot.Instance.SystemMechanics.DisarmMainHandBuff))
                    can_disarm = true;
                else if (maybe_weapon2 != null && !maybe_weapon2.Blueprint.IsUnarmed && !maybe_weapon2.Blueprint.IsNatural && !attack_rule.Target.Descriptor.Buffs.HasFact(BlueprintRoot.Instance.SystemMechanics.DisarmOffHandBuff))
                    can_disarm = true;

                if (!can_disarm)
                {
                    return false;
                }
            }
            else if (maneuver == CombatManeuver.SunderArmor)
            {
                if (attack_rule.Target.Descriptor.Buffs.HasFact(BlueprintRoot.Instance.SystemMechanics.SunderArmorBuff))
                    return false;
            }
            else if (maneuver == CombatManeuver.DirtyTrickBlind)
            {
                if (attack_rule.Target.Descriptor.Buffs.HasFact(BlueprintRoot.Instance.SystemMechanics.DirtyTrickBlindnessBuff))
                    return false;
            }
            else if (maneuver == CombatManeuver.DirtyTrickEntangle)
            {
                if (attack_rule.Target.Descriptor.Buffs.HasFact(BlueprintRoot.Instance.SystemMechanics.DirtyTrickEntangledBuff))
                    return false;
            }
            else if (maneuver == CombatManeuver.DirtyTrickSickened)
            {
                if (attack_rule.Target.Descriptor.Buffs.HasFact(BlueprintRoot.Instance.SystemMechanics.DirtyTrickSickenedBuff))
                    return false;
            }
            else
            {
                Main.logger.Log("Trying to replace attack with unsupported maneuver type: " + maneuver.ToString());
                return false;
            }

            //Main.logger.Log("Second Conditions Ok");
            attack_rule.Initiator.Ensure<CombatManeuverBonus.UnitPartUseWeaponForCombatManeuver>().force(attack_rule.Weapon, attack_rule.AttackBonusPenalty);
            RuleCombatManeuver rule = new RuleCombatManeuver(this.Context.MaybeCaster, attack_rule.Target, maneuver);
            
            var result = Rulebook.CurrentContext.Trigger<RuleCombatManeuver>(rule);
            attack_rule.Initiator.Ensure<CombatManeuverBonus.UnitPartUseWeaponForCombatManeuver>().unForce();

            //Main.logger.Log("Maneuver Ok");
            Harmony12.Traverse.Create(attack_rule).Property("AttackRoll").SetValue(new RuleAttackRoll(attack_rule.Initiator, attack_rule.Target, attack_rule.Weapon, attack_rule.AttackBonusPenalty));
            Harmony12.Traverse.Create(attack_rule).Property("AttackRoll").Property("Result").SetValue(result.Success ? AttackResult.Hit : AttackResult.Miss);
            Harmony12.Traverse.Create(attack_rule).Property("AttackRoll").Property("Roll").SetValue(result.InitiatorRoll);
            Harmony12.Traverse.Create(attack_rule).Property("AttackRoll").Property("AttackBonus").SetValue(result.InitiatorCMB);
            Harmony12.Traverse.Create(attack_rule).Property("AttackRoll").Property("TargetAC").SetValue(result.TargetCMD);

            //Main.logger.Log("Attack Rule Updated");
            return true;
        }

    }



    [Harmony12.HarmonyPatch(typeof(UnitAttack))]
    [Harmony12.HarmonyPatch("TriggerAttackRule", Harmony12.MethodType.Normal)]
    class UnitAttack__TriggerAttackRule__Patch
    {
        static IEnumerable<Harmony12.CodeInstruction> Transpiler(IEnumerable<Harmony12.CodeInstruction> instructions)
        {
            List<Harmony12.CodeInstruction> codes = new List<Harmony12.CodeInstruction>();
            try
            {
                codes = instructions.ToList();
                var trigger_attack_idx = codes.FindIndex(x => x.opcode == System.Reflection.Emit.OpCodes.Call && x.operand.ToString().Contains("Trigger"));
                codes[trigger_attack_idx] = new Harmony12.CodeInstruction(System.Reflection.Emit.OpCodes.Call, new Func<RuleAttackWithWeapon, RuleAttackWithWeapon>(performAttackOrManeuver).Method);
            }
            catch (Exception ex)
            {
                Main.logger.Log(ex.ToString());
            }

            return codes.AsEnumerable();
        }


        static RuleAttackWithWeapon performAttackOrManeuver(RuleAttackWithWeapon attack_rule)
        {
            var attack_replacement_part = attack_rule.Initiator.Get<UnitPartAttackReplacementWithAction>();

            if (attack_replacement_part != null && attack_replacement_part.maybeReplaceAttackWithAction(attack_rule))
            {
                return attack_rule;
            }
            else
            {
                return Rulebook.Trigger<RuleAttackWithWeapon>(attack_rule);
            }            
        }
    }


    [Harmony12.HarmonyPatch(typeof(UnitAttackOfOpportunity))]
    [Harmony12.HarmonyPatch("OnAction", Harmony12.MethodType.Normal)]
    class UnitAttackOfOpportunity__OnAction__Patch
    {
        static IEnumerable<Harmony12.CodeInstruction> Transpiler(IEnumerable<Harmony12.CodeInstruction> instructions)
        {
            List<Harmony12.CodeInstruction> codes = new List<Harmony12.CodeInstruction>();
            try
            {
                codes = instructions.ToList();
                var trigger_attack_idx = codes.FindIndex(x => x.opcode == System.Reflection.Emit.OpCodes.Call && x.operand.ToString().Contains("Trigger"));
                codes[trigger_attack_idx] = new Harmony12.CodeInstruction(System.Reflection.Emit.OpCodes.Call, new Func<RuleAttackWithWeapon, RuleAttackWithWeapon>(performAttackOrManeuver).Method);
            }
            catch (Exception ex)
            {
                Main.logger.Log(ex.ToString());
            }

            return codes.AsEnumerable();
        }


        static RuleAttackWithWeapon performAttackOrManeuver(RuleAttackWithWeapon attack_rule)
        {
            var attack_replacement_part = attack_rule.Initiator.Get<UnitPartAttackReplacementWithAction>();

            if (attack_replacement_part != null && attack_replacement_part.maybeReplaceAttackWithAction(attack_rule))
            {
                return attack_rule;
            }
            else
            {
                return Rulebook.Trigger<RuleAttackWithWeapon>(attack_rule);
            }
        }
    }
}
