
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

using CallOfTheWild;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Items;

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
            var tr = Harmony12.Traverse.Create(__instance);
            var m_InnerRule = tr.Field("m_InnerRule").GetValue<RuleCalculateAttackBonusWithoutTarget>();
            int Result;
            Result = Rulebook.Trigger<RuleCalculateAttackBonusWithoutTarget>(m_InnerRule).Result;
            tr.Property("Result").SetValue(Result);
            if (UnitPartConcealment.Calculate(__instance.Target, __instance.Initiator) == Concealment.Total)
            {
                __instance.ConcealmentBonus = 2;
                  Result += __instance.ConcealmentBonus;
                tr.Property("Result").SetValue(Result);
            }
            if (Flanking.isFlankedBy(__instance.Target, __instance.Initiator) && __instance.Weapon.Blueprint.IsMelee)
            {
                __instance.FlankingBonus = 2;
                Result += __instance.FlankingBonus;
                tr.Property("Result").SetValue(Result);
            }
            if (__instance.Weapon.Blueprint.IsRanged && !__instance.IgnoreRangedPenalty)
            {
                foreach (UnitEntityData unit in Game.Instance.State.Units)
                {
                    if (!unit.IsEnemy(__instance.Initiator) && unit != __instance.Initiator && unit != __instance.Target && ((double)unit.DistanceTo(__instance.Target) <= (double)10.Feet().Meters && (unit.CombatState.EngagedUnits.Contains<UnitEntityData>(__instance.Target) || __instance.Target.CombatState.EngagedUnits.Contains<UnitEntityData>(unit))))
                    {
                        __instance.ShootIntoCombatBonus = -4;
                        Result += __instance.ShootIntoCombatBonus;
                        tr.Property("Result").SetValue(Result);
                        break;
                    }
                }
            }

            if (Cover.hasCoverFrom(__instance.Target, __instance.Initiator, __instance.Weapon))
            {
                __instance.AddBonus(Cover.soft_cover_penalty, Cover.soft_cover_fact);
                Result += Cover.soft_cover_penalty;
                tr.Property("Result").SetValue(Result);
            }
            if (!__instance.Initiator.IsPlayerFaction || Game.Instance.Player.Difficulty.TrueDeath)
                return false;
            int num = Math.Max(0, (int)__instance.Initiator.Stats.BaseAttackBonus - __instance.AttackBonusPenalty - Result - 2);
            Result += num;
            tr.Property("Result").SetValue(Result);
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
            {
                Rulebook.Trigger<RuleCalculateWeaponStats>(__instance.WeaponStats);
            }
            bool flag1 = __instance.Target.Descriptor.State.HasCondition(UnitCondition.Confusion);
            bool flag2 = !__instance.Target.IsEnemy(__instance.Initiator) && !__instance.Target.Faction.Neutral && !flag1;
            if (__instance.Initiator == __instance.Target || __instance.AttackType.IsTouch() && flag2)
            {
                __instance.AutoHit = true;
            }
            if (__instance.AutoHit)
            {
                Result = AttackResult.Hit;
                tr.Property("Result").SetValue(Result);
                tr.Property("IsCriticalConfirmed").SetValue(__instance.AutoCriticalThreat && __instance.AutoCriticalConfirmation);
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
                    Result = AttackResult.Parried;
                    tr.Property("Result").SetValue(Result);
                }
            }
            tr.Property("Result").SetValue(Result);
            tr.Property("IsSneakAttack").SetValue(tr.Property("IsSneakAttack").GetValue<bool>() && __instance.IsHit);
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


    [Harmony12.HarmonyPatch(typeof(UnitCombatState))]
    [Harmony12.HarmonyPatch("AttackOfOpportunity", Harmony12.MethodType.Normal)]
    class UnitCombatState__AttackOfOpportunity__Patch
    {
        static bool Prefix(UnitCombatState __instance, UnitEntityData target, ref bool __result )
        {//prevent attack of opportunity if target has cover
            if (Cover.hasCoverFrom(target, __instance.Unit, __instance.Unit.Body.PrimaryHand.Weapon))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }



    class Cover
    {
        static internal LibraryScriptableObject library = Main.library;
        static internal Fact soft_cover_fact;
        static internal BlueprintFeature phalanx_formation_feat;
        static internal BlueprintFeature low_profile_feat;
        static internal int soft_cover_penalty = -4;

        static internal BlueprintFeature improved_precise_shot = library.Get<BlueprintFeature>("46f970a6b9b5d2346b10892673fe6e74");
        static internal bool ignore_cover;

        static void createSoftCoverFact()
        {
            var soft_cover_unti_fact = Helpers.Create<BlueprintUnitFact>();
            soft_cover_unti_fact.name = "SoftCoverFact";
            soft_cover_unti_fact.SetName("Soft Cover");
            soft_cover_unti_fact.SetDescription("");
            library.AddAsset(soft_cover_unti_fact, "31a5d72594eb4b49aaad3b86c88f84cb");
            soft_cover_fact = new Fact(soft_cover_unti_fact);
        }

        static void createPhalanxFormation()
        {
            phalanx_formation_feat = Helpers.CreateFeature("PhalanxFormationFeature",
                                                           "Phalanx Formation",
                                                           "You are trained to use long weapons in tight formations.\n" +
                                                           "Benefit: When you wield a reach weapon with which you are proficient, allies don’t provide soft cover to opponents you attack with reach.\n" +
                                                           "Normal: Attacking a target that is beyond another creature, even an ally, can result in the target having soft cover from you.",
                                                           "f1e93e123f4c4e04978d0ec58597aa5a",
                                                           null,
                                                           FeatureGroup.Feat,
                                                           Helpers.PrerequisiteStatValue(Kingmaker.EntitySystem.Stats.StatType.BaseAttackBonus, 1));
            library.AddFeats(phalanx_formation_feat);
        }





        static internal void load(bool ignore)
        {
            ignore_cover = ignore;
            Main.logger.Log($"Use cover: {!ignore_cover}.");
            createSoftCoverFact();
            createPhalanxFormation();
            createLowProfileFeat();
        }

        static void createLowProfileFeat()
        {
            var goblin_race = library.Get<BlueprintRace>("9d168ca7100e9314385ce66852385451");
            var gnome_race = library.Get<BlueprintRace>("ef35a22c9a27da345a4528f0d5889157");
            var halfling_race = library.Get<BlueprintRace>("b0c3ef2729c498f47970bb50fa1acd30");

            var ac_bonus = Helpers.Create<ACBonusAgainstAttacks>(a => { a.AgainstRangedOnly = true; a.ArmorClassBonus = 1; a.Descriptor = ModifierDescriptor.Dodge; });
            low_profile_feat = Helpers.CreateFeature("LowProfileFeature",
                                                           "Low Profile",
                                                           "Yours small stature helps you avoid ranged attacks.\n" +
                                                           "Benefit: You gain a +1 dodge bonus to AC against ranged attacks. In addition, you do not provide soft cover to creatures when ranged attacks pass through your square.",
                                                           "adb468d1af064635bd61c1bc606eb724",
                                                           null,
                                                           FeatureGroup.Feat,
                                                           ac_bonus,
                                                           Helpers.PrerequisiteStatValue(Kingmaker.EntitySystem.Stats.StatType.Dexterity, 13),
                                                           Helpers.PrerequisiteFeaturesFromList(goblin_race, gnome_race, halfling_race));
            library.AddFeats(low_profile_feat);
        }


        static internal bool hasCoverFrom(UnitEntityData unit, UnitEntityData attacker, ItemEntityWeapon weapon)
        {
            var c = (unit.Position + attacker.Position) / 2.0f;
            var r = (unit.Position - attacker.Position) / 2.0f;
            //account for units inside circle
            var norm_r = r.normalized;
            var unit_radius = FlankingHelpers.unitSizeToDiameter(unit.Blueprint.Size).Feet().Meters / 2.0f;
            r = r - norm_r * unit_radius;
            c = c - norm_r * unit_radius;

            float radius = (float)Math.Sqrt(Vector3.Dot(r, r));
            var units_around = GameHelper.GetTargetsAround(c, radius, true);

            var unit_position = unit.Position;
            var attacker_position = attacker.Position;

            if (attacker.Descriptor.HasFact(improved_precise_shot) 
                && weapon != null
                && weapon.Blueprint.IsRanged)
            {//precise shot ignores cover
                return false;
            }


            foreach (var u in units_around)
            {
                if (hasCoverDueFrom(unit, u, attacker, weapon))
                {
#if DEBUG
                    Main.logger.Log($"{unit.CharacterName} has cover from {attacker.CharacterName} due to {u.CharacterName}");
                    Main.logger.Log($"{unit.Position.To2D()} has cover from {attacker.Position.To2D()} due to {u.Position.To2D()}");
#endif
                    return true;
                }
            }
            return false;
        }


        static internal bool hasCoverDueFrom(UnitEntityData unit, UnitEntityData cover, UnitEntityData attacker, ItemEntityWeapon weapon)
        {
            if (ignore_cover)
            {
                return false;
            }

            if ((attacker.Descriptor.HasFact(improved_precise_shot) || cover.Descriptor.HasFact(low_profile_feat))
                && weapon != null
                && weapon.Blueprint.IsRanged)
            {//precise shot ignores cover, low profile does not provide cover
                return false;
            }


            if (cover == unit || cover == attacker || !cover.Descriptor.State.CanAct)
            {
                return false;
            }

            var ur = FlankingHelpers.unitSizeToDiameter(cover.Blueprint.Size).Feet().Meters / 2.0f * 0.9f;

            if (!FlankingHelpers.lineItersectsCircle(cover.Position, ur * ur, attacker.Position, unit.Position))
            {
                return false;
            }

            if (weapon != null
                && weapon.Blueprint.IsMelee
                && cover.IsAlly(attacker)
                && attacker.Descriptor.HasFact(phalanx_formation_feat)
                )
            {
                return false;
            }


            return true;
        }

    }

    class Flanking
    {
        static internal bool isFlankedBy(UnitEntityData unit, UnitEntityData attacker)
        {
            if (unit.Descriptor.State.Features.CannotBeFlanked)
            {
                return false;
            }

            float unit_radius = (FlankingHelpers.unitSizeToDiameter(unit.Blueprint.Size)/2.0f).Feet().Meters;
            float unit_radius2 = unit_radius * unit_radius;
            var unit_position = unit.Position;

            var engaged_array = unit.CombatState.EngagedBy.ToArray();

            if (!engaged_array.Contains(attacker))
            {
                return false;
            }

            for (int i = 0; i < engaged_array.Length; i++)
            {
                if (engaged_array[i] == attacker)
                {
                    continue;
                }

                if (FlankingHelpers.lineItersectsCircle(unit_position, unit_radius2, attacker.Position, engaged_array[i].Position))
                {
#if DEBUG
                    Main.logger.Log($"{attacker.CharacterName} and {engaged_array[i].CharacterName} are flanking {unit.CharacterName}");
#endif
                    return true;
                }
            }
            return false;
        }
    }


    class FlankingHelpers
    {
        internal static bool isAttacking(UnitEntityData unit, UnitEntityData attacker)
        {
            return attacker.Commands.AnyCommandTargets(unit);
        }


        internal static bool lineItersectsCircle(Vector3 o, float r2, Vector3 a, Vector3 b)
        {
            var ao = (o - a).To2D();
            var ab = (b - a).To2D();
            float norm_ab = (float)Math.Sqrt(Vector2.Dot(ab, ab));
            float proj = Vector2.Dot(ao, ab) / norm_ab;
            if (proj > norm_ab || proj < 0.0f)
            {
                return false;
            }
            float dist2 = Vector2.Dot(ao, ao) - proj * proj;
            return dist2 < r2;
        }

        internal static float unitSizeToDiameter(Size sz) //in feet
        {
            switch (sz)
            {
                case Size.Large:
                    return 10;
                case Size.Huge:
                    return 15;
                case Size.Gargantuan:
                    return 20;
                case Size.Colossal:
                    return 30;
                default:
                    return 5;
            }
        }
    }
}
