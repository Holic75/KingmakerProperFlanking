using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProperFlanking20
{
    [Harmony12.HarmonyPatch(typeof(RuleCalculateAttackBonus))]
    [Harmony12.HarmonyPatch("OnTrigger", Harmony12.MethodType.Normal)]
    class RuleCalculateAttackBonus__OnTrigger__BaseFix
    {
        static IEnumerable<Harmony12.CodeInstruction> Transpiler(IEnumerable<Harmony12.CodeInstruction> instructions)
        {
            List<Harmony12.CodeInstruction> codes = new List<Harmony12.CodeInstruction>();
            try
            {
                codes = instructions.ToList();
                var is_flanked_idx = codes.FindIndex(x => x.opcode == System.Reflection.Emit.OpCodes.Callvirt && x.operand.ToString().Contains("IsFlanked"));
                codes[is_flanked_idx] = new Harmony12.CodeInstruction(System.Reflection.Emit.OpCodes.Call, new Func<UnitEntityData, UnitEntityData, bool>(Flanking.isFlankedByAttacker).Method);
                codes.RemoveAt(is_flanked_idx - 1);
                codes.InsertRange(is_flanked_idx - 1, new Harmony12.CodeInstruction[]{new Harmony12.CodeInstruction(System.Reflection.Emit.OpCodes.Ldarg_0),
                                                                                      new Harmony12.CodeInstruction(System.Reflection.Emit.OpCodes.Ldfld,
                                                                                                                    typeof(Kingmaker.RuleSystem.RulebookEvent).GetField("Initiator"))
                                                                                     }
                                  );
            }
            catch (Exception ex)
            {
                Main.logger.Log(ex.ToString());
            }

            return codes.AsEnumerable();
        }
    }


    [Harmony12.HarmonyPatch(typeof(RuleCalculateDamage))]
    [Harmony12.HarmonyPatch("OnTrigger", Harmony12.MethodType.Normal)]
    class RuleCalculateDamage__OnTrigger__BaseFix
    {
        static bool Prefix(RuleCalculateDamage __instance, RulebookEventContext context)
        {
            if (UnitPartConcealment.Calculate(__instance.Initiator, __instance.Target, false) != Concealment.None)
            {//disable precision damage if target has concelement
                __instance.ParentRule.DisablePrecisionDamage = true;
            }

            return true;
        }
    }


    [Harmony12.HarmonyPatch(typeof(RulePrepareDamage))]
    [Harmony12.HarmonyPatch("OnTrigger", Harmony12.MethodType.Normal)]
    class RulePrepareDamage__OnTrigger__BaseFix
    {
        static IEnumerable<Harmony12.CodeInstruction> Transpiler(IEnumerable<Harmony12.CodeInstruction> instructions)
        {
            List<Harmony12.CodeInstruction> codes = new List<Harmony12.CodeInstruction>();
            try
            {
                codes = instructions.ToList();
                var is_flanked_idx = codes.FindIndex(x => x.opcode == System.Reflection.Emit.OpCodes.Callvirt && x.operand.ToString().Contains("IsFlanked"));
                codes[is_flanked_idx] = new Harmony12.CodeInstruction(System.Reflection.Emit.OpCodes.Call, new Func<UnitEntityData, UnitEntityData, bool>(Flanking.isFlankedByAttacker).Method);
                codes.RemoveAt(is_flanked_idx - 1);
                codes.InsertRange(is_flanked_idx - 1, new Harmony12.CodeInstruction[]{new Harmony12.CodeInstruction(System.Reflection.Emit.OpCodes.Ldarg_0),
                                                                                      new Harmony12.CodeInstruction(System.Reflection.Emit.OpCodes.Ldfld,
                                                                                                                    typeof(Kingmaker.RuleSystem.RulebookEvent).GetField("Initiator"))
                                                                                     }
                                  );
            }
            catch (Exception ex)
            {
                Main.logger.Log(ex.ToString());
            }

            return codes.AsEnumerable();
        }
    }


    [Harmony12.HarmonyPatch(typeof(RuleAttackRoll))]
    [Harmony12.HarmonyPatch("OnTrigger", Harmony12.MethodType.Normal)]
    class RuleAttackRoll__OnTrigger__BaseFix
    {
        static IEnumerable<Harmony12.CodeInstruction> Transpiler(IEnumerable<Harmony12.CodeInstruction> instructions)
        {
            List<Harmony12.CodeInstruction> codes = new List<Harmony12.CodeInstruction>();
            try
            {
                codes = instructions.ToList();
                var is_flanked_idx = codes.FindIndex(x => x.opcode == System.Reflection.Emit.OpCodes.Callvirt && x.operand.ToString().Contains("IsFlanked"));
                codes[is_flanked_idx] = new Harmony12.CodeInstruction(System.Reflection.Emit.OpCodes.Call, new Func<UnitEntityData, UnitEntityData, bool>(Flanking.isFlankedByAttacker).Method);
                codes.RemoveAt(is_flanked_idx - 1);
                codes.InsertRange(is_flanked_idx - 1, new Harmony12.CodeInstruction[]{new Harmony12.CodeInstruction(System.Reflection.Emit.OpCodes.Ldarg_0),
                                                                                      new Harmony12.CodeInstruction(System.Reflection.Emit.OpCodes.Ldfld,
                                                                                                                    typeof(Kingmaker.RuleSystem.RulebookEvent).GetField("Initiator"))
                                                                                     }
                                  );
            }
            catch (Exception ex)
            {
                Main.logger.Log(ex.ToString());
            }

            return codes.AsEnumerable();
        }
    }



    [Harmony12.HarmonyPatch(typeof(CallOfTheWild.TeamworkMechanics.CoordinatedShotAttackBonus))]
    [Harmony12.HarmonyPatch("OnEventAboutToTrigger", Harmony12.MethodType.Normal)]
    class CoordinatedShotAttcakBonus_OnEventAboutToTrigger_Patch
    {
        static bool Prefix(CallOfTheWild.TeamworkMechanics.CoordinatedShotAttackBonus __instance, RuleCalculateAttackBonus evt)
        {
            if (evt.Weapon == null)
            {
                return false;
            }
            if (!evt.Weapon.Blueprint.IsRanged)
                return false;

            int attack_bonus = __instance.AttackBonus;
            int additional_flank_bonus = __instance.AdditionalFlankBonus;
            BlueprintUnitFact coordinated_shot_fact = __instance.CoordinatedShotFact;
            UnitDescriptor owner = __instance.Owner;

            int bonus = 0;
            bool solo_tactics = owner.State.Features.SoloTactics;

            foreach (UnitEntityData unitEntityData in evt.Target.CombatState.EngagedBy)
            {
                AttackType attack_type = evt.Weapon == null ? AttackType.Melee : evt.Weapon.Blueprint.AttackType;
                if ((unitEntityData.Descriptor.HasFact(coordinated_shot_fact) || solo_tactics)
                    && unitEntityData != owner.Unit && unitEntityData.providesCoverToFrom(evt.Target, owner.Unit, attack_type).isNone())
                {
                    bonus = Math.Max(bonus, (evt.Target.isFlankedByAttacker(unitEntityData) ? attack_bonus + additional_flank_bonus : attack_bonus));
                }
            }

            if (bonus == 0)
            {
                return false;
            }

            evt.AddBonus(bonus, __instance.Fact);
            return false;
        }
    }



    [Harmony12.HarmonyPatch(typeof(PreciseStrike))]
    [Harmony12.HarmonyPatch("OnEventAboutToTrigger", Harmony12.MethodType.Normal)]
    class PreciseStrike__OnEventAboutToTrigger__Patch
    {
        static bool Prefix(PreciseStrike __instance, RulePrepareDamage evt)
        {
            if (!evt.Target.isFlankedByAttacker(__instance.Owner.Unit) || evt.DamageBundle.Weapon == null)
                return false;
            bool flag = (bool)__instance.Owner.State.Features.SoloTactics;
            if (!flag)
            {
                foreach (UnitEntityData unitEntityData in evt.Target.CombatState.EngagedBy)
                {
                    flag = unitEntityData.Descriptor.HasFact(__instance.PreciseStrikeFact) && unitEntityData != __instance.Owner.Unit;
                    if (flag && evt.Target.isFlankedByAttacker(unitEntityData))
                        break;
                }
            }
            if (!flag)
                return false;
            BaseDamage damage = __instance.Damage.CreateDamage();
            evt.DamageBundle.Add(damage);
            return false;
        }
    }


    [Harmony12.HarmonyPatch(typeof(FlankedAttackBonus))]
    [Harmony12.HarmonyPatch("OnEventAboutToTrigger", Harmony12.MethodType.Normal)]
    class FlankedAttackBonus__OnEventAboutToTrigger__Patch
    {
        static bool Prefix(FlankedAttackBonus __instance, RuleCalculateAttackBonus evt)
        {
            bool isFlatFooted = Rulebook.Trigger<RuleCheckTargetFlatFooted>(new RuleCheckTargetFlatFooted(evt.Initiator, evt.Target)).IsFlatFooted;

            bool is_flanked = false;

            foreach (var u in evt.Target.CombatState.EngagedBy)
            {
                is_flanked = evt.Target.isFlankedByAttacker(u);
                if (is_flanked)
                {
                    break;
                }
            }

            if (is_flanked || isFlatFooted)
            {
                evt.AddBonus(__instance.AttackBonus * __instance.Fact.GetRank(), __instance.Fact);
            }
                
            return false;
        }
    }


    [Harmony12.HarmonyPatch(typeof(CallOfTheWild.NewMechanics.FlankingAttackBonus))]
    [Harmony12.HarmonyPatch("OnEventAboutToTrigger", Harmony12.MethodType.Normal)]
    class FlankingAttackBonus__OnEventAboutToTrigger__Patch
    {
        static bool Prefix(CallOfTheWild.NewMechanics.FlankingAttackBonus __instance, RuleAttackRoll evt)
        {

            if (evt.Weapon == null)
                return false;

            if (evt.Target.isFlankedByAttacker(evt.Initiator))
            {
                evt.AddTemporaryModifier(evt.Initiator.Stats.AdditionalAttackBonus.AddModifier(__instance.Bonus, (GameLogicComponent)__instance, __instance.Descriptor));
            }

            return false;
        }
    }


    [Harmony12.HarmonyPatch(typeof(OutflankProvokeAttack))]
    [Harmony12.HarmonyPatch("OnEventDidTrigger", Harmony12.MethodType.Normal)]
    class OutflankProvokeAttack__OnEventDidTrigger__Patch
    {
        static bool Prefix(OutflankProvokeAttack __instance, RuleAttackRoll evt)
        {
            if (!evt.IsCriticalConfirmed || !evt.Target.isFlankedByAttacker(__instance.Owner.Unit))
                return false;
            foreach (UnitEntityData attacker in evt.Target.CombatState.EngagedBy)
            {
                if ((((attacker.Descriptor.HasFact(__instance.OutflankFact) || (bool)__instance.Owner.State.Features.SoloTactics) && attacker != __instance.Owner.Unit))
                     && evt.Target.isFlankedByAttacker(attacker))
                    Game.Instance.CombatEngagementController.ForceAttackOfOpportunity(attacker, evt.Target);
            }
            return false;
        }
    }



    [Harmony12.HarmonyPatch(typeof(OutflankAttackBonus))]
    [Harmony12.HarmonyPatch("OnEventAboutToTrigger", Harmony12.MethodType.Normal)]
    class OutflankAttackBonus__OnEventAboutToTrigger__Patch
    {
        static bool Prefix(OutflankAttackBonus __instance, RuleCalculateAttackBonus evt)
        {
            if (!evt.Target.isFlankedByAttacker(__instance.Owner.Unit))
                return false;
            bool flag = (bool)__instance.Owner.State.Features.SoloTactics;
            if (!flag)
            {
                foreach (UnitEntityData unitEntityData in evt.Target.CombatState.EngagedBy)
                {
                    flag = unitEntityData.Descriptor.HasFact(__instance.OutflankFact) && unitEntityData != __instance.Owner.Unit && evt.Target.isFlankedByAttacker(unitEntityData);
                    if (flag)
                        break;
                }
            }
            if (!flag)
                return false;
            evt.AddBonus(__instance.AttackBonus * __instance.Fact.GetRank(), __instance.Fact);
            return false;
        }
    }


    [Harmony12.HarmonyPatch(typeof(MadDogPackTactics))]
    [Harmony12.HarmonyPatch("OnEventAboutToTrigger", Harmony12.MethodType.Normal)]
    class MadDogPackTactics__OnEventAboutToTrigger__Patch
    {
        static bool Prefix(MadDogPackTactics __instance, RuleCalculateAttackBonus evt)
        {
            if (!evt.Target.isFlankedByAttacker( __instance.Owner.Unit))
                return false;
            bool flag = false;
            foreach (UnitEntityData unitEntityData in evt.Target.CombatState.EngagedBy)
            {
                flag = unitEntityData.Descriptor.IsPet && unitEntityData.Descriptor.Master == __instance.Owner.Unit
                       || __instance.Owner.IsPet && (UnitReference)unitEntityData == (UnitEntityData)__instance.Owner.Master;
                flag = flag && evt.Target.isFlankedByAttacker(unitEntityData);
                if (flag)
                    break;
            }
            if (!flag)
                return false;
            evt.AddBonus(2, __instance.Fact);
            return false;
        }
    }


    [Harmony12.HarmonyPatch(typeof(BackToBack))]
    [Harmony12.HarmonyPatch("OnEventAboutToTrigger", Harmony12.MethodType.Normal)]
    class BackToBack__OnEventAboutToTrigger__Patch
    {
        static bool Prefix(BackToBack __instance, RuleCalculateAC evt)
        {
            if (!evt.Target.isFlankedByAttacker(evt.Initiator))
                return false;
            foreach (UnitEntityData unitEntityData in GameHelper.GetTargetsAround(__instance.Owner.Unit.Position, (float)__instance.Radius, true, false))
            {
                if ((unitEntityData.Descriptor.HasFact(__instance.BackToBackFact)
                    || (bool)__instance.Owner.State.Features.SoloTactics) && unitEntityData != __instance.Owner.Unit && !unitEntityData.IsEnemy(__instance.Owner.Unit))
                {
                    evt.AddBonus(2, __instance.Fact);
                    break;
                }
            }
            return false;
        }
    }


    static class Flanking
    {
        public abstract class ModifyFlankingAngle : OwnedGameLogicComponent<UnitDescriptor>
        {
            public override void OnTurnOn()
            {
                this.Owner.Ensure<UnitPartModifyFlankingAngle>().addBuff(this.Fact);
            }

            public override void OnTurnOff()
            {
                this.Owner.Ensure<UnitPartModifyFlankingAngle>().removeBuff(this.Fact);
            }

            abstract public float getFlankingAngleIncrease(UnitEntityData target, UnitEntityData partner);
        }


        public class UnitPartModifyFlankingAngle : CallOfTheWild.AdditiveUnitPart
        {
            public bool hasBuff(BlueprintFact blueprint)
            {
                return buffs.Any(b => b.Blueprint == blueprint);
            }

            public float getFlankingAngleIncrease(UnitEntityData target, UnitEntityData partner)
            {
                float flanking_angle = 0f;
                foreach (var b in buffs)
                {
                    if (b.Blueprint.GetComponent<ModifyFlankingAngle>() != null)
                    {
                        float angle = 0f;
                        b.CallComponents<ModifyFlankingAngle>(a => { angle = a.getFlankingAngleIncrease(target, partner); });
                        if (angle > flanking_angle)
                        {
                            flanking_angle = angle;
                        }
                    }
                }
                return flanking_angle;
            }
        }


        public abstract class  SpecialFlanking: OwnedGameLogicComponent<UnitDescriptor>
        {
            public override void OnTurnOn()
            {
                this.Owner.Ensure<UnitPartSpecialFlanking>().addBuff(this.Fact);
            }

            public override void OnTurnOff()
            {
                this.Owner.Ensure<UnitPartSpecialFlanking>().removeBuff(this.Fact);
            }

            abstract public bool isFlanking(UnitEntityData target);
            abstract public bool isFlankingTogether(UnitEntityData target, UnitEntityData partner);
        }

        public class UnitPartSpecialFlanking : CallOfTheWild.AdditiveUnitPart
        {
            public bool hasBuff(BlueprintFact blueprint)
            {
                return buffs.Any(b => b.Blueprint == blueprint);
            }

            public bool isFlankingTogether(UnitEntityData target, UnitEntityData partner)
            {

                foreach (var b in buffs)
                {
                    if (b.Blueprint.GetComponent<SpecialFlanking>() != null)
                    {
                        bool result = false;
                        b.CallComponents<SpecialFlanking>(a => { result = a.isFlankingTogether(target, partner); });
                        if (result)
                        {
#if DEBUG
                            Main.logger.Log($"{this.Owner.Unit.CharacterName} is flanking {target.CharacterName} with {partner.CharacterName} due to {b.Name}");
#endif
                            return true;
                        }
                    }
                }
                return false;
            }



            public bool isFlanking(UnitEntityData target)
            {

                foreach (var b in buffs)
                {
                    if (b.Blueprint.GetComponent<SpecialFlanking>() != null)
                    {
                        bool result = false;
                        b.CallComponents<SpecialFlanking>(a => { result = a.isFlanking(target); });
                        if  (result)
                        {
#if DEBUG
                            Main.logger.Log($"{this.Owner.Unit.CharacterName} is flanking {target.CharacterName} due to {b.Name}");
#endif
                            return true;
                        }
                    }
                }
                return false;
            }
        }



        static internal bool isFlankedByAttacker(this UnitEntityData unit, UnitEntityData attacker)
        {
            if (unit == null || attacker == null || unit == attacker)
            {
                return false;
            }

            if (IsFlatFootedTo(attacker, unit))
            {
                return false;
            }

            if (unit.Descriptor.State.Features.CannotBeFlanked)
            {
                return false;
            }

            return unit.isFlankedByAttackerGeometrically(attacker) || unit.isFlankedBySpecial(attacker);
        }


        static internal bool isFlankedByAttackerWith(this UnitEntityData unit, UnitEntityData attacker, UnitEntityData partner)
        {
            if (unit == null || attacker == null || unit == attacker || partner == null || attacker == partner || unit == partner)
            {
                return false;
            }

            if (IsFlatFootedTo(attacker, unit))
            {
                return false;
            }

            if (IsFlatFootedTo(partner, unit))
            {
                return false;
            }

            if (unit.Descriptor.State.Features.CannotBeFlanked)
            {
                return false;
            }

            return unit.isFlankedBySpecialWith(attacker, partner) || unit.isFlankedByAttackerGeometricallyTogetherWith(attacker, partner);
        }


        static internal bool isFlankedBySpecial(this UnitEntityData unit, UnitEntityData attacker)
        {
            return attacker.Ensure<UnitPartSpecialFlanking>().isFlanking(unit);
        }


        static internal bool isFlankedBySpecialWith(this UnitEntityData unit, UnitEntityData attacker, UnitEntityData partner)
        {
            return attacker.Ensure<UnitPartSpecialFlanking>().isFlankingTogether(unit, partner);
        }



        static internal bool isFlankedByAttackerGeometrically(this UnitEntityData unit, UnitEntityData attacker)
        {
            float unit_radius = unit.View.Corpulence; //(Helpers.unitSizeToDiameter(unit.Descriptor.State.Size) / 2.0f).Feet().Meters;

            var unit_position = unit.Position;

            var engaged_array = unit.CombatState.EngagedBy.ToArray();

            if (!engaged_array.Contains(attacker))
            {
                return false;
            }

            for (int i = 0; i < engaged_array.Length; i++)
            {
                if (IsFlatFootedTo(engaged_array[i], unit))
                {
                    continue;
                }

                if (isFlankedByAttackerGeometricallyTogetherWith(unit, attacker, engaged_array[i]))
                {
                    return true;
                }
            }
            return false;
        }


        static internal bool isFlankedByAttackerGeometricallyTogetherWith(this UnitEntityData unit, UnitEntityData attacker, UnitEntityData partner)
        {
            float unit_radius = unit.View.Corpulence; //(Helpers.unitSizeToDiameter(unit.Descriptor.State.Size) / 2.0f).Feet().Meters;
            

            var engaged_array = unit.CombatState.EngagedBy.ToArray();

            if (!engaged_array.Contains(attacker))
            {
                return false;
            }

            if (!engaged_array.Contains(partner))
            {
                return false;
            }


            if (attacker == partner)
            {
                return false;
            }

            float flanking_angle_rad = (float)Math.Atan(unit_radius / Math.Max((attacker.Position.To2D() - unit.Position.To2D()).magnitude, unit_radius));//[0, pi/2]
            
            var unit_part_modify_flanking_angle = attacker.Get<UnitPartModifyFlankingAngle>();
            float angle_increase = unit_part_modify_flanking_angle == null ? 0.0f : unit_part_modify_flanking_angle.getFlankingAngleIncrease(unit, partner);
            flanking_angle_rad += angle_increase;
            /*if (attacker.IsPlayerFaction)
            {
                Main.logger.Log(attacker.CharacterName + "/" + partner.CharacterName + " Flanking Angle: " + flanking_angle_rad.ToString());
                Main.logger.Log(unit.Position.To2D().ToString());
                Main.logger.Log(attacker.Position.To2D().ToString());
                Main.logger.Log(partner.Position.To2D().ToString());
            }*/
            //Main.logger.Log(attacker.CharacterName + " Flanking Angle: " + flanking_angle_rad.ToString());
            if (Helpers.checkGeometricFlanking(unit.Position.To2D(), attacker.Position.To2D(), partner.Position.To2D(), flanking_angle_rad))
            {
                /*if (attacker.IsPlayerFaction)
                {
                    Main.logger.Log($"{attacker.CharacterName} and {partner.CharacterName} are flanking {unit.CharacterName} due to geometry");
                }*/
                return true;
            }
            return false;
            //float unit_radius2 = unit_radius * unit_radius;
            //return Helpers.isCircleIntersectedByLine(unit_position.To2D(), unit_radius2, attacker.Position.To2D(), partner.Position.To2D());
        }


        internal static bool IsFlatFootedTo(this UnitEntityData target, UnitEntityData attacker)
        {
            return Rulebook.Trigger(new RuleCheckTargetFlatFooted(attacker, target)).IsFlatFooted;
        }
    }


}
