using CallOfTheWild;
using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Root;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProperFlanking20.NewMechanics
{

    [AllowedOn(typeof(BlueprintUnitFact))]
    [AllowMultipleComponents]
    public class TeamworkBonusAgainstFlanked : RuleInitiatorLogicComponent<RuleCalculateAttackBonus>
    {
        public int bonus = 2;
        public Kingmaker.RuleSystem.AttackType[] allowed_types;


        public override void OnEventAboutToTrigger(RuleCalculateAttackBonus evt)
        {
            if (evt.Weapon == null)
            {
                return;
            }

            if (!allowed_types.Contains(evt.Weapon.Blueprint.AttackType))
            {
                return;
            }
            if (!evt.Weapon.Blueprint.IsRanged)
                return;

            bool solo_tactics = (bool)this.Owner.State.Features.SoloTactics;

            foreach (UnitEntityData unit in evt.Target.CombatState.EngagedBy)
            {
                if (unit == this.Owner.Unit)
                {
                    continue;
                }

                if ((unit.Descriptor.HasFact(this.Fact) || solo_tactics) && evt.Target.isFlankedByAttacker(unit))
                {
                    evt.AddBonus(bonus, this.Fact);
                    return;
                }
            }
        }

        public override void OnEventDidTrigger(RuleCalculateAttackBonus evt)
        {
        }
    }


    [AllowedOn(typeof(BlueprintUnitFact))]
    [AllowMultipleComponents]
    public class FriendlyFireSavingThrowBonus : RuleInitiatorLogicComponent<RuleSavingThrow>
    {
        public StatType SavingThrow;
        public ContextValue value;
        public ModifierDescriptor Descriptor;

        public override void OnEventAboutToTrigger(RuleSavingThrow evt)
        {
            var caster = evt.Reason.Caster;
            if (caster == null || caster.IsPlayersEnemy || caster == this.Owner.Unit)
                return;

            int bonus = value.Calculate(this.Fact.MaybeContext);
            if (caster.Descriptor.HasFact(this.Fact) || (bool)this.Owner.State.Features.SoloTactics)
            {
                evt.AddTemporaryModifier(evt.Initiator.Stats.GetStat(SavingThrow).AddModifier(bonus * this.Fact.GetRank(), (GameLogicComponent)this, this.Descriptor));
            }
        }

        public override void OnEventDidTrigger(RuleSavingThrow evt)
        {
        }
    }


    [AllowedOn(typeof(BlueprintBuff))]
    public class FlatFootedAgainstCaster : BuffLogic, ITargetRulebookHandler<RuleCheckTargetFlatFooted>,  ITargetRulebookHandler<RuleAttackRoll>
    {
        public bool remove_after_attack;
        public BlueprintUnitFact ranged_allowed_fact;

        private bool allowed = false;

        public void OnEventAboutToTrigger(RuleCheckTargetFlatFooted evt)
        {
            if (allowed && evt.Initiator == this.Context?.MaybeCaster)
            {
                evt.ForceFlatFooted = true;
            }
        }


        public void OnEventAboutToTrigger(RuleAttackRoll evt)
        {
            if (evt.Weapon.Blueprint.IsMelee
                ||(evt.Initiator != null && ranged_allowed_fact != null && evt.Initiator.Descriptor.HasFact(ranged_allowed_fact)))
            {
                allowed = true;
            }
        }

        public void OnEventDidTrigger(RuleCheckTargetFlatFooted evt)
        {
        }

        public void OnEventDidTrigger(RuleAttackRoll evt)
        {
            allowed = false;
            if (!remove_after_attack)
            {
                return;
            }

            if (evt.Initiator == this.Fact.MaybeContext?.MaybeCaster)
            {
                this.Buff.Remove();
            }
        }
    }


    [AllowedOn(typeof(BlueprintBuff))]
    public class FlatFootedAgainstAttacType: BuffLogic, ITargetRulebookHandler<RuleCheckTargetFlatFooted>, ITargetRulebookHandler<RuleAttackRoll>
    {
        public bool remove_after_attack;
        public AttackType[] allowed_attack_types;

        private bool allowed = false;

        public void OnEventAboutToTrigger(RuleCheckTargetFlatFooted evt)
        {
            if (allowed)
            {
                evt.ForceFlatFooted = true;
            }
        }

        public void OnEventAboutToTrigger(RuleAttackRoll evt)
        {
            if (allowed_attack_types.Empty() || allowed_attack_types.Contains(evt.Weapon.Blueprint.AttackType))
            {
                allowed = true;
            }
        }

        public void OnEventDidTrigger(RuleCheckTargetFlatFooted evt)
        {
        }

        public void OnEventDidTrigger(RuleAttackRoll evt)
        {
            allowed = false;
            if (!remove_after_attack)
            {
                return;
            }

            if (evt.Initiator == this.Fact.MaybeContext?.MaybeCaster)
            {
                this.Buff.Remove();
            }
        }
    }


    public class ContextFeintSkillCheck : ContextAction
    {
        public ActionList Success;
        public ActionList Failure;
        static BlueprintFeature[] single_penalty_facts = new BlueprintFeature[] {Main.library.Get<BlueprintFeature>("455ac88e22f55804ab87c2467deff1d6"), //dragons
                                                                                 Main.library.Get<BlueprintFeature>("625827490ea69d84d8e599a33929fdc6"), //magical beasts
                                                                                };

        static BlueprintFeature[] double_penalty_facts = new BlueprintFeature[] {Main.library.Get<BlueprintFeature>("a95311b3dc996964cbaa30ff9965aaf6"), //animals
                                                                                };


        public override string GetCaption()
        {
            return "Feint check";
        }

        public override void RunAction()
        {
            if (this.Target.Unit == null)
            {
                UberDebug.LogError((object)"Target unit is missing", (object[])Array.Empty<object>());
            }
            else if (this.Context.MaybeCaster == null)
            {
                UberDebug.LogError((object)"Caster is missing", (object[])Array.Empty<object>());
            }
            else
            {
                int dc_bab = this.Target.Unit.Descriptor.Stats.BaseAttackBonus.ModifiedValue + this.Target.Unit.Descriptor.Stats.Wisdom.Bonus;
                int dc_sense_motive = (this.Target.Unit.Descriptor.Stats.SkillPerception.BaseValue > 0) ? this.Target.Unit.Descriptor.Stats.SkillPerception.ModifiedValue : 0;

                //int dc = 10 + Math.Max(dc_bab, dc_sense_motive);
                int dc = 10 + dc_bab;

                if (targetHasFactFromList(double_penalty_facts))
                {
                    dc += 8;
                }
                else if (targetHasFactFromList(single_penalty_facts))
                {
                    dc += 4;
                }
              
                if (this.Context.TriggerRule<RuleSkillCheck>(new RuleSkillCheck(this.Context.MaybeCaster, StatType.CheckBluff, dc)
                {
                    ShowAnyway = true
                }).IsPassed)
                    this.Success.Run();
                else
                    this.Failure.Run();
            }
        }

        private bool targetHasFactFromList(params BlueprintFeature[] facts)
        {
            foreach (var f in facts)
            {
                if (this.Target.Unit.Descriptor.HasFact(f))
                {
                    return true;
                }
            }
            return false;
        }
    }


    [AllowedOn(typeof(BlueprintAbility))]
    [AllowMultipleComponents]
    public class AbilityCasterMainWeaponIsMeleeUnlessHasFact : BlueprintComponent, IAbilityCasterChecker
    {
        public BlueprintFeature ranged_allowed_fact;

        public bool CorrectCaster(UnitEntityData caster)
        {
            var weapon = caster.Body.PrimaryHand.MaybeWeapon;
            if (weapon == null)
            {
                return true;
            }

            if (weapon.Blueprint.IsMelee || (ranged_allowed_fact != null && caster.Descriptor.HasFact(ranged_allowed_fact)))
            {
                return true;
            }

            return false;
        }

        public string GetReason()
        {
            return (string)LocalizedTexts.Instance.Reasons.SpecificWeaponRequired;
        }
    }


    class PrerequisiteFeatFromGroup: Prerequisite
    {
        public FeatureGroup group;

        public override bool Check([CanBeNull] FeatureSelectionState selectionState, [NotNull] UnitDescriptor unit, [NotNull] LevelUpState state)
        {
            return unit.Progression.Features.Enumerable.Any(f => f.Blueprint.Groups.Contains(group));
        }

        public override string GetUIText()
        {
            string group_string = string.Concat(group.ToString().Select(x => Char.IsUpper(x) ? " " + char.ToLower(x) : x.ToString()));

            return "One" + group_string;
        }
    }


    


    class PrerequisiteCharacterSize : Prerequisite
    {
        public Size value;
        public bool or_smaller;
        public bool or_larger;

        public override bool Check([CanBeNull] FeatureSelectionState selectionState, [NotNull] UnitDescriptor unit, [NotNull] LevelUpState state)
        {
            return CheckUnit(unit);
        }

        public bool CheckUnit(UnitDescriptor unit)
        {
            if (unit.OriginalSize == value)
                return true;

            if (or_smaller && unit.OriginalSize < value)
                return true;

            if (or_larger && unit.OriginalSize > value)
                return true;

            return false;
        }

        public override string GetUIText()
        {
            StringBuilder stringBuilder = new StringBuilder();
            string text = $"Size: {Kingmaker.Blueprints.Root.LocalizedTexts.Instance.Sizes.GetText(value)}";
            stringBuilder.Append(text);
            if (or_smaller)
                stringBuilder.Append(" or smaller");
            if (or_larger)
                stringBuilder.Append(" or larger");
            return stringBuilder.ToString();
        }
    }



    [AllowedOn(typeof(BlueprintUnitFact))]
    [AllowMultipleComponents]
    public class WildFlanking : OwnedGameLogicComponent<UnitDescriptor>, IInitiatorRulebookHandler<RuleAttackWithWeapon>, IInitiatorRulebookHandler<RuleCalculateDamage>
    {
        public BlueprintUnitFact wild_flanking_mark;
        private UnitEntityData unit = null;
        private int damage;
        public BlueprintFeature GreaterPowerAttackBlueprint;

        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
        {
            if (!evt.Weapon.Blueprint.IsMelee || ! evt.Target.isFlankedByAttacker(evt.Initiator))
            {
                unit = null;
            }

            foreach (var u in evt.Target.CombatState.EngagedBy)
            {
                if (u != evt.Initiator && u.Buffs.HasFact(wild_flanking_mark) && evt.Target.isFlankedByAttacker(u))
                {
                    unit = u;
                    damage = getPowerAttackBonus(this.Owner.Unit, evt.Weapon);
                    break;
                }
            }
        }

        public void OnEventAboutToTrigger(RuleCalculateDamage evt)
        {
            //will be applied to both: attacker and partner
            if (unit != null)
            {
                evt.DamageBundle.First?.AddBonusTargetRelated(damage);
            }
        }

        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            if (unit == null)
            {
                return;
            }
            var unit_attack_roll = Rulebook.Trigger<RuleAttackRoll>(new RuleAttackRoll(evt.Initiator, unit, evt.WeaponStats, evt.AttackBonusPenalty));
            if (unit_attack_roll.IsHit)
            {
                var damage_base = evt.Weapon.Blueprint.DamageType.CreateDamage(DiceFormula.Zero, 0);//we write 0 damage here, since bonus damage is added in OnEventAboutToTrigger(RuleCalculateDamage evt)
                RuleDealDamage rule = new RuleDealDamage(this.Owner.Unit, unit, new DamageBundle(damage_base));
                Rulebook.Trigger<RuleDealDamage>(rule);
            }
            unit = null;
        }

        public void OnEventDidTrigger(RuleCalculateDamage evt)
        {

        }


        private int getPowerAttackBonus(UnitEntityData unit, ItemEntityWeapon weapon)
        {
            if (weapon == null || unit == null)
            {
                return 0;
            }

            int dmg = 2*(1 + unit.Descriptor.Stats.BaseAttackBonus.ModifiedValue / 4);

            if (weapon.Blueprint.Type.IsLight && !weapon.Blueprint.IsUnarmed && !weapon.Blueprint.IsNatural || weapon.IsSecondary)
                return dmg / 2;
            if (!weapon.HoldInTwoHands)
                return dmg;
            if (unit.Descriptor.HasFact((BlueprintUnitFact)this.GreaterPowerAttackBlueprint))
                return dmg * 2;
            return dmg * 3 / 2;
        }
    }




    [AllowedOn(typeof(BlueprintUnitFact))]
    [AllowMultipleComponents]
    public class CMBBonusAgainstFlanked : RuleInitiatorLogicComponent<RuleCombatManeuver>
    {
        public ContextValue Value;

        private MechanicsContext Context
        {
            get
            {
                MechanicsContext context = (this.Fact as Buff)?.Context;
                if (context != null)
                    return context;
                return (this.Fact as Feature)?.Context;
            }
        }

        public override void OnEventAboutToTrigger(RuleCombatManeuver evt)
        {
            if (!evt.Target.isFlankedByAttacker(evt.Initiator))
            {
                return;
            }
            evt.AddBonus(this.Value.Calculate(this.Context), this.Fact);
        }

        public override void OnEventDidTrigger(RuleCombatManeuver evt)
        {
        }
    }

}
