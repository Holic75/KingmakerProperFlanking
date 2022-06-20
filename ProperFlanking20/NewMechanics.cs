using CallOfTheWild;
using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Root;
using Kingmaker.Designers.Mechanics.Facts;
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

                if ((unit.Descriptor.HasFact(this.Fact.Blueprint as BlueprintUnitFact) || solo_tactics) && evt.Target.isFlankedByAttacker(unit))
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
            if (caster.Descriptor.HasFact(this.Fact.Blueprint as BlueprintUnitFact) || (bool)this.Owner.State.Features.SoloTactics)
            {
                evt.AddTemporaryModifier(evt.Initiator.Stats.GetStat(SavingThrow).AddModifier(bonus * this.Fact.GetRank(), (GameLogicComponent)this, this.Descriptor));
            }
        }

        public override void OnEventDidTrigger(RuleSavingThrow evt)
        {
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

            int current_hp = 0;
            foreach (var u in evt.Target.CombatState.EngagedBy)
            {
                if (u != evt.Initiator 
                    && (u.Buffs.HasFact(wild_flanking_mark) || evt.Initiator.Descriptor.State.Features.SoloTactics)
                    && evt.Target.isFlankedByAttackerWith(evt.Initiator, u))
                {
                    if (unit.Stats.HitPoints.ModifiedValue > current_hp)
                    {
                        current_hp = unit.Stats.HitPoints.ModifiedValue;
                        unit = u;
                    }
                }
            }
            if (unit != null)
            {
                damage = getPowerAttackBonus(this.Owner.Unit, evt.Weapon);
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
    public class DamageBonusAgainstFlankedTarget : RuleInitiatorLogicComponent<RuleCalculateDamage>
    {
        public int bonus;

        public override void OnEventAboutToTrigger(RuleCalculateDamage evt)
        {
            if (!evt.Target.isFlankedByAttacker(evt.Initiator))
            {
                return;
            }

             evt.DamageBundle.WeaponDamage?.AddBonusTargetRelated(bonus);
        }

        public override void OnEventDidTrigger(RuleCalculateDamage evt) { }
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



    [ComponentName("Weapon group attack bonus")]
    [AllowedOn(typeof(BlueprintUnitFact))]
    public class WeaponGroupAttackBonusCompatibleWithCMB : WeaponGroupAttackBonus, IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>
    {
        public override void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
        {

        }

        public void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget evt)
        {
            if (evt.Weapon == null || evt.Weapon.Blueprint.FighterGroup != this.WeaponGroup)
                return;
            evt.AddBonus(this.AttackBonus * this.Fact.GetRank(), this.Fact);
        }

        public void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget evt)
        {
        }
    }

}
