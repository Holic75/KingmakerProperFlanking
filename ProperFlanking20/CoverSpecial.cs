using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProperFlanking20.CoverSpecial
{
    public class IgnoreCoverFromOneUnitWithWeaponCategory : Cover.IgnoreCoverFromOneUnitBase
    {
        public WeaponCategory[] categories = new WeaponCategory[0];
        public override bool ignoresCover(ItemEntityWeapon weapon)
        {
            return weapon != null && (categories.Empty() || categories.Contains(weapon.Blueprint.Category));
        }
    }


    public class IgnoreCoverForAttackType : Cover.SpecialIgnoreCover
    {
        public Kingmaker.RuleSystem.AttackType[] allowed_types;

        public override bool ignoresCover(UnitEntityData target, UnitEntityData cover, AttackType attack_type)
        {
            return allowed_types.Contains(attack_type);
        }
    }

    public class IgnoreCover : Cover.SpecialIgnoreCover
    {
        public override bool ignoresCover(UnitEntityData target, UnitEntityData cover, AttackType attack_type)
        {
            return true;
        }
    }


    public class DoesNotProvideCover : Cover.SpecialProvideNoCover
    {
        public override bool doesNotProvideCoverToFrom(UnitEntityData target, UnitEntityData attacker, AttackType attack_type)
        {
            return true;
        }
    }



    public class NoCoverToFactOwners : Cover.SpecialProvideNoCover
    {
        public bool teamwork = true;
        public AttackType[] attack_types = new AttackType[0];

        public override bool doesNotProvideCoverToFrom(UnitEntityData target, UnitEntityData attacker, AttackType attack_type)
        {
            return ((teamwork && (bool)attacker.Descriptor.State.Features.SoloTactics) || attacker.Descriptor.HasFact(this.Fact.Blueprint as BlueprintUnitFact)) 
                && attacker.IsAlly(this.Owner.Unit)
                && isCorrectAttackType(attack_type);
        }


        private bool isCorrectAttackType(AttackType attack_type)
        {
            return attack_types.Empty() || attack_types.Contains(attack_type);
        }
    }


    public class NoCoverFromFactOwners : Cover.SpecialIgnoreCover
    {
        public bool teamwork = true;
        public AttackType[] attack_types = new AttackType[0];

        public override bool ignoresCover(UnitEntityData target, UnitEntityData cover, AttackType attack_type)
        {
            return ((teamwork && (bool)Owner.State.Features.SoloTactics) || cover.Descriptor.HasFact(this.Fact.Blueprint as BlueprintUnitFact)) && cover.IsAlly(this.Owner.Unit) && isCorrectAttackType(attack_type);
        }

        private bool isCorrectAttackType(AttackType attack_type)
        {
            return attack_types.Empty() || attack_types.Contains(attack_type);
        }
    }


    public class NoCoverToCasterWithFact : Cover.SpecialProvideNoCover
    {
        public BlueprintUnitFact fact;
        public AttackType[] attack_types;
        public override bool doesNotProvideCoverToFrom(UnitEntityData target, UnitEntityData attacker, AttackType attack_type)
        {
            if (!attack_types.Contains(attack_type))
            {
                return false;
            }

            return (this.Fact.MaybeContext.MaybeCaster == attacker && attacker.Descriptor.HasFact(fact));
        }
    }


}
