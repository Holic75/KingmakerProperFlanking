
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Controllers.Combat;
using Kingmaker.Controllers.Projectiles;
using Kingmaker.Designers;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.SettingsUI;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProperFlanking
{
    [Harmony12.HarmonyPatch(typeof(RulePrepareDamage))]
    [Harmony12.HarmonyPatch("OnTrigger", Harmony12.MethodType.Normal)]
    class RulePrepareDamage__OnTrigger__Patch
    {
        static bool Prefix(RulePrepareDamage __instance, RulebookEventContext context)
        {
            RuleAttackRoll ruleAttackRoll = context.AllEvents.LastOfType<RuleAttackRoll>();
            if ((int)__instance.Initiator.Stats.SneakAttack > 0)
            {
                bool flag = ruleAttackRoll != null && ruleAttackRoll.IsSneakAttack && ruleAttackRoll.Target == __instance.Target && !ruleAttackRoll.IsSneakAttackUsed;
                AbilityData ability = __instance.ParentRule.Reason.Ability;
                BlueprintAbility unityObject = (object)ability != null ? ability.Blueprint : (BlueprintAbility)null;
                if (!flag && __instance.IsSurpriseSpell && ruleAttackRoll == null)
                {
                    AbilityType? type = unityObject.Or<BlueprintAbility>((BlueprintAbility)null)?.Type;
                    if ((type.GetValueOrDefault() != AbilityType.Spell ? 0 : (type.HasValue ? 1 : 0)) != 0)
                    {
                        int num1 = Flanking.isFlankedBy(__instance.Target, __instance.Initiator) ? (true ? 1 : 0) : (Rulebook.Trigger<RuleCheckTargetFlatFooted>(new RuleCheckTargetFlatFooted(__instance.Initiator, __instance.Target)).IsFlatFooted ? 1 : 0);
                        Projectile projectile = __instance.ParentRule.Projectile;
                        int num2 = !(bool)(projectile != null ? projectile.Blueprint.GetComponent<CannotSneakAttack>() : null) ? 1 : 0;
                        flag = (num1 & num2) != 0;
                    }
                }
                if (flag)
                {
                    DamageTypeDescription typeDescription = __instance.DamageBundle.First<BaseDamage>().CreateTypeDescription();
                    DiceType diceType = DiceType.D6;
                    if ((bool)__instance.Initiator.Descriptor.State.Features.KnifeMaster)
                        diceType = __instance.ParentRule.DamageBundle.Weapon == null || !__instance.ParentRule.DamageBundle.Weapon.Blueprint.Category.HasSubCategory(WeaponSubCategory.Knives) ? DiceType.D4 : DiceType.D8;
                    int sneakAttack = (int)__instance.Initiator.Stats.SneakAttack;
                    UnitPartSneakAttackModifications attackModifications = __instance.Initiator.Get<UnitPartSneakAttackModifications>();
                    int bonus = 0;
                    if (attackModifications != null)
                    {
                        bonus += attackModifications.AdditionalDamagePerDice * sneakAttack;
                        bool? isRanged = ruleAttackRoll?.Weapon?.Blueprint.IsRanged;
                        if ((!isRanged.GetValueOrDefault() ? 0 : (isRanged.HasValue ? 1 : 0)) == 0)
                        {
                            if (__instance.IsSurpriseSpell)
                            {
                                AbilityRange? range = unityObject.Or<BlueprintAbility>((BlueprintAbility)null)?.Range;
                                if ((range.GetValueOrDefault() != AbilityRange.Touch ? 1 : (!range.HasValue ? 1 : 0)) == 0)
                                    goto label_12;
                            }
                            else
                                goto label_12;
                        }
                        bonus += attackModifications.AdditionalDamagePerRangedDice * sneakAttack;
                    }
                label_12:
                    DiceFormula dice = new DiceFormula(sneakAttack, diceType);
                    BaseDamage damage = typeDescription.GetDamageDescriptor(dice, bonus).CreateDamage();
                    damage.Precision = true;
                    damage.Sneak = true;
                    __instance.DamageBundle.Add(damage);
                    ruleAttackRoll?.UseSneakAttack();
                }
            }
            if (ruleAttackRoll == null || ruleAttackRoll.PreciseStrike <= 0)
                return false;
            BaseDamage damage1 = __instance.DamageBundle.First<BaseDamage>().CreateTypeDescription().GetDamageDescriptor(new DiceFormula(ruleAttackRoll.PreciseStrike, DiceType.One), 0).CreateDamage();
            damage1.Precision = true;
            __instance.DamageBundle.Add(damage1);
            return false;
        }
    }



    [Harmony12.HarmonyPatch(typeof(RuleCalculateAttackBonus))]
    [Harmony12.HarmonyPatch("OnTrigger", Harmony12.MethodType.Normal)]
    class RuleCalculateAttackBonus__OnTrigger__Patch
    {
        static bool Prefix(RuleCalculateAttackBonus __instance, RulebookEventContext context)
        {
            var m_InnerRule = Harmony12.Traverse.Create(__instance).Field("m_InnerRule").GetValue<RuleCalculateAttackBonusWithoutTarget>();
            int Result;
            Result = Rulebook.Trigger<RuleCalculateAttackBonusWithoutTarget>(m_InnerRule).Result;
            if (UnitPartConcealment.Calculate(__instance.Target, __instance.Initiator) == Concealment.Total)
            {
                __instance.ConcealmentBonus = 2;
                  Result += __instance.ConcealmentBonus;
            }
            if (Flanking.isFlankedBy(__instance.Target, __instance.Initiator) && __instance.Weapon.Blueprint.IsMelee)
            {
                __instance.FlankingBonus = 2;
                Result += __instance.FlankingBonus;
            }
            if (__instance.Weapon.Blueprint.IsRanged && !__instance.IgnoreRangedPenalty)
            {
                foreach (UnitEntityData unit in Game.Instance.State.Units)
                {
                    if (!unit.IsEnemy(__instance.Initiator) && unit != __instance.Initiator && unit != __instance.Target && ((double)unit.DistanceTo(__instance.Target) <= (double)10.Feet().Meters && (unit.CombatState.EngagedUnits.Contains<UnitEntityData>(__instance.Target) || __instance.Target.CombatState.EngagedUnits.Contains<UnitEntityData>(unit))))
                    {
                        __instance.ShootIntoCombatBonus = -4;
                        Result += __instance.ShootIntoCombatBonus;
                        break;
                    }
                }
            }
            Harmony12.Traverse.Create(__instance).Property("Result").SetValue(Result);
            if (!__instance.Initiator.IsPlayerFaction || Game.Instance.Player.Difficulty.TrueDeath)
                return false;
            int num = Math.Max(0, (int)__instance.Initiator.Stats.BaseAttackBonus - __instance.AttackBonusPenalty - Result - 2);
            Result += num;
            Harmony12.Traverse.Create(__instance).Property("Result").SetValue(Result);
            if (num <= 0)
                return false;
            __instance.AddTemporaryModifier(__instance.Initiator.Stats.AdditionalAttackBonus.AddModifier(num, (GameLogicComponent)null, ModifierDescriptor.Difficulty));
            return false;
        }
    }


    [Harmony12.HarmonyPatch(typeof(RuleAttackRoll))]
    [Harmony12.HarmonyPatch("OnTrigger", Harmony12.MethodType.Normal)]
    class RuleAttackRoll__OnTrigger__Patch
    {
        static bool Prefix(RuleAttackRoll __instance, RulebookEventContext context)
        {
            var tr = Harmony12.Traverse.Create(__instance);
            AttackResult Result;
            if (!__instance.WeaponStats.IsTriggererd)
                Rulebook.Trigger<RuleCalculateWeaponStats>(__instance.WeaponStats);
            bool flag1 = __instance.Target.Descriptor.State.HasCondition(UnitCondition.Confusion);
            bool flag2 = !__instance.Target.IsEnemy(__instance.Initiator) && !__instance.Target.Faction.Neutral && !flag1;
            if (__instance.Initiator == __instance.Target || __instance.AttackType.IsTouch() && flag2)
                __instance.AutoHit = true;
            if (__instance.AutoHit)
            {
                Result = AttackResult.Hit;
                tr.Property("Result").SetValue(Result);
            }
            else if (__instance.AutoMiss)
            {
                Result = AttackResult.Miss;
                tr.Property("Result").SetValue(Result);
            }
            else if (tr.Method("TryOvercomeTargetConcealmentAndMissChance").GetValue<bool>())
            {
                tr.Property("ACRule").SetValue(Rulebook.Trigger<RuleCalculateAC>(new RuleCalculateAC(__instance.Initiator, __instance.Target, __instance.AttackType)));
                tr.Property("IsTargetFlatFooted").SetValue(__instance.ACRule.IsTargetFlatFooted);
                tr.Property("TargetAC").SetValue(__instance.ACRule.TargetAC);
                tr.Property("AttackBonusRule").SetValue(Rulebook.Trigger<RuleCalculateAttackBonus>(new RuleCalculateAttackBonus(__instance.Initiator, __instance.Target, __instance.Weapon, __instance.AttackBonusPenalty)));
                tr.Property("AttackBonus").SetValue(__instance.AttackBonusRule.Result);
                tr.Property("Roll").SetValue(RulebookEvent.Dice.D20);
                bool flag3 = __instance.IsSuccessRoll(__instance.Roll);
                Result = !flag3 ? __instance.Target.Stats.AC.SelectMissReason(__instance.IsTargetFlatFooted, __instance.AttackType.IsTouch()) : AttackResult.Hit;
                tr.Property("Result").SetValue(Result);
                var is_flanked = Flanking.isFlankedBy(__instance.Target, __instance.Initiator);

                tr.Property("IsSneakAttack").SetValue(__instance.IsHit && !__instance.ImmuneToSneakAttack && (__instance.IsTargetFlatFooted || is_flanked) && (int)__instance.Initiator.Stats.SneakAttack > 0);
                CriticalHitPower critsOnParty = Game.Instance.Player.Difficulty.CritsOnParty;
                tr.Property("IsCriticalRoll").SetValue(flag3 && !__instance.ImmuneToCriticalHit && (__instance.Roll >= __instance.WeaponStats.CriticalEdge || __instance.AutoCriticalThreat) && (!__instance.Target.IsPlayerFaction || critsOnParty == CriticalHitPower.Weak || critsOnParty == CriticalHitPower.Normal));
                if (__instance.IsCriticalRoll)
                {
                    tr.Property("TargetCriticalAC").SetValue(Rulebook.Trigger<RuleCalculateAC>(new RuleCalculateAC(__instance.Initiator, __instance.Target, __instance.AttackType)
                    {
                        IsCritical = true
                    }).TargetAC);
                    tr.Property("CriticalConfirmationRoll").SetValue(!__instance.AutoCriticalConfirmation ? RulebookEvent.Dice.D20 : 20);
                    tr.Property("IsCriticalConfirmed").SetValue(__instance.AutoCriticalConfirmation || __instance.CriticalConfirmationRoll + __instance.AttackBonus + __instance.CriticalConfirmationBonus >= __instance.TargetCriticalAC);
                }
                if (__instance.IsSneakAttack || __instance.IsCriticalConfirmed || __instance.PreciseStrike > 0)
                {
                    int? nullable = __instance.Target.Get<UnitPartFortification>()?.Value;
                    tr.Property("FortificationChance").SetValue(!nullable.HasValue ? 0 : nullable.Value);
                    if (__instance.TargetUseFortification)
                    {
                        tr.Property("FortificationRoll").SetValue(RulebookEvent.Dice.D100);
                        if (!__instance.FortificationOvercomed)
                        {
                            tr.Property("FortificationNegatesSneakAttack").SetValue(__instance.IsSneakAttack);
                            tr.Property("FortificationNegatesCriticalHit").SetValue(__instance.IsCriticalConfirmed);
                            tr.Property("IsSneakAttack").SetValue(false);
                            tr.Property("IsCriticalConfirmed").SetValue(false);
                            __instance.PreciseStrike = 0;
                        }
                    }
                }
                if (__instance.IsCriticalConfirmed)
                {
                    Result = AttackResult.CriticalHit;
                    tr.Property("Result").SetValue(Result);
                }
                bool force = !flag3 && __instance.TargetAC - __instance.Roll - __instance.AttackBonus <= 5;
                if ((flag3 || force) && !__instance.Initiator.Descriptor.IsImmuneToVisualEffects)
                {
                    UnitPartMirrorImage unitPartMirrorImage = __instance.Target.Get<UnitPartMirrorImage>();
                    if (unitPartMirrorImage != null)
                    {
                        tr.Property("HitMirrorImageIndex").SetValue(unitPartMirrorImage.TryAbsorbHit(force));
                        if (__instance.HitMirrorImageIndex > 0)
                        {
                            Result = AttackResult.MirrorImage;
                            tr.Property("Result").SetValue(Result);
                        }
                    }
                }
            }
            else
            {
                Result = AttackResult.Concealment;
                tr.Property("Result").SetValue(Result);
            }
            if (__instance.IsHit && !__instance.AutoHit && __instance.Parry != null)
            {
                __instance.Parry.Trigger(context);
                if (__instance.Parry.Roll + __instance.Parry.AttackBonus > __instance.Roll + __instance.AttackBonus)
                {
                    Result = AttackResult.Parried; tr.Property("Result").SetValue(Result);
                    tr.Property("Result").SetValue(Result);
                }
            }
            tr.Property("Result").SetValue(Result);
            tr.Property("IsSneakAttack").SetValue(tr.Property("IsSneakAttack").GetValue<bool>() & __instance.IsHit);
            EventBus.RaiseEvent<IAttackHandler>((Action<IAttackHandler>)(h => h.HandleAttackHitRoll(__instance)));
            return false;
        }

    }



    [Harmony12.HarmonyPatch(typeof(PreciseStrike))]
    [Harmony12.HarmonyPatch("OnEventAboutToTrigger", Harmony12.MethodType.Normal)]
    class PreciseStrike__OnEventAboutToTrigger__Patch
    {
        static bool Prefix(PreciseStrike __instance, RulePrepareDamage evt)
        {
            if (!Flanking.isFlankedBy(evt.Target, __instance.Owner.Unit) || evt.DamageBundle.Weapon == null)
                return false;
            bool flag = (bool)__instance.Owner.State.Features.SoloTactics;
            if (!flag)
            {
                foreach (UnitEntityData unitEntityData in evt.Target.CombatState.EngagedBy)
                {
                    flag = unitEntityData.Descriptor.HasFact(__instance.PreciseStrikeFact) && unitEntityData != __instance.Owner.Unit;
                    if (flag && Flanking.isFlankedBy(evt.Target, unitEntityData))
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


    [Harmony12.HarmonyPatch(typeof(OutflankProvokeAttack))]
    [Harmony12.HarmonyPatch("OnEventDidTrigger", Harmony12.MethodType.Normal)]
    class OutflankProvokeAttack__OnEventDidTrigger__Patch
    {
        static bool Prefix(OutflankProvokeAttack __instance, RuleAttackRoll evt)
        {
            if (!evt.IsCriticalConfirmed || !Flanking.isFlankedBy(evt.Target, __instance.Owner.Unit))
                return false;
            foreach (UnitEntityData attacker in evt.Target.CombatState.EngagedBy)
            {
                if ((((attacker.Descriptor.HasFact(__instance.OutflankFact) || (bool)__instance.Owner.State.Features.SoloTactics) && attacker != __instance.Owner.Unit)) 
                     && Flanking.isFlankedBy(evt.Target, attacker))
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
            if (!Flanking.isFlankedBy(evt.Target, __instance.Owner.Unit))
                return false;
            bool flag = (bool)__instance.Owner.State.Features.SoloTactics;
            if (!flag)
            {
                foreach (UnitEntityData unitEntityData in evt.Target.CombatState.EngagedBy)
                {
                    flag = unitEntityData.Descriptor.HasFact(__instance.OutflankFact) && unitEntityData != __instance.Owner.Unit && Flanking.isFlankedBy(evt.Target, unitEntityData);
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
            if (!Flanking.isFlankedBy(evt.Target, __instance.Owner.Unit))
                return false;
            bool flag = false;
            foreach (UnitEntityData unitEntityData in evt.Target.CombatState.EngagedBy)
            {
                flag = unitEntityData.Descriptor.IsPet && unitEntityData.Descriptor.Master == __instance.Owner.Unit 
                       || __instance.Owner.IsPet && (UnitReference)unitEntityData == (UnitEntityData)__instance.Owner.Master;
                flag = flag && Flanking.isFlankedBy(evt.Target, unitEntityData);
                if (flag) 
                    break;
            }
            if (!flag)
                return false;
            evt.AddBonus(2, __instance.Fact);
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
            if (!Flanking.isFlankedBy(evt.Target, evt.Initiator) && !isFlatFooted)
                return false;
            evt.AddBonus(__instance.AttackBonus * __instance.Fact.GetRank(), __instance.Fact);
            return false;
        }
    }



    [Harmony12.HarmonyPatch(typeof(BackToBack))]
    [Harmony12.HarmonyPatch("OnEventAboutToTrigger", Harmony12.MethodType.Normal)]
    class BackToBack__OnEventAboutToTrigger__Patch
    {
        static bool Prefix(BackToBack __instance, RuleCalculateAC evt)
        {
            if (!Flanking.isFlankedBy(evt.Target, evt.Initiator))
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





    class Flanking
    {
        static internal bool isFlankedBy(UnitEntityData unit, UnitEntityData attacker)
        {
            float unit_radius = unitSizeToDiameter(unit.Blueprint.Size)/2;
            float unit_radius2 = unit_radius * unit_radius;
            var unit_position = unit.Position;


            var engaged_array = unit.CombatState.EngagedBy.ToArray();

            if (!engaged_array.Contains(attacker) || !isAttackingInMelee(unit, attacker))
            {
                return false;
            }
            var attacker_position = attacker.Position - unit_position;
            for (int i = 0; i < engaged_array.Length; i++)
            {
                if (engaged_array[i] == attacker || !isAttackingInMelee(unit, engaged_array[i]))
                {
                    continue;
                }
                var position_i = engaged_array[i].Position - unit_position;

                var ao = (-attacker_position).To2D();
                var ab = (position_i - attacker_position).To2D();
                float norm_ab = (float)Math.Sqrt(Vector2.Dot(ab, ab));
                float proj = Vector2.Dot(ao, ab) / norm_ab;
                if (proj > norm_ab || proj < 0.0f)
                {
                    continue;
                }
                float dist2 = Vector2.Dot(ao, ao) - proj * proj;
                if (dist2 < unit_radius2)
                {
#if DEBUG
                    Main.logger.Log($"{attacker.CharacterName} and {engaged_array[i].CharacterName} are flanking {unit.CharacterName}");
#endif
                    return true;
                }
            }
            return false;
        }


        static bool isAttackingInMelee(UnitEntityData unit, UnitEntityData attacker)
        {
            return attacker.Commands.AnyCommandTargets(unit) && attacker.Body.PrimaryHand.Weapon.Blueprint.IsMelee;
        }


        static float unitSizeToDiameter(Size sz)
        {
            switch (sz)
            {
                case Size.Large:
                    return 10.Feet().Meters;
                case Size.Huge:
                    return 15.Feet().Meters;
                case Size.Gargantuan:
                    return 20.Feet().Meters;
                case Size.Colossal:
                    return 30.Feet().Meters;
                default:
                    return 5.Feet().Meters;
            }
        }
    }
}
