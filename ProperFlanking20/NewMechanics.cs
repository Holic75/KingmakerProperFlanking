using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Mechanics;
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


    class PrerequisiteFeatFromGroup: Prerequisite
    {
        public FeatureGroup group;
        public string description;

        public override bool Check([CanBeNull] FeatureSelectionState selectionState, [NotNull] UnitDescriptor unit, [NotNull] LevelUpState state)
        {
            return unit.Progression.Features.Enumerable.Any(f => f.Blueprint.Groups.Contains(group));
        }

        public override string GetUIText()
        {
            return description;
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
}
