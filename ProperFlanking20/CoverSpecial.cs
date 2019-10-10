using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProperFlanking20.CoverSpecial
{

    public class IgnoreCoverForAttackType : Cover.SpecialIgnoreCover
    {
        public Kingmaker.RuleSystem.AttackType[] allowed_types;

        public override bool ignoresCover(AttackType attack_type)
        {
            return allowed_types.Contains(attack_type);
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
        bool teamwork = true;

        public override bool doesNotProvideCoverToFrom(UnitEntityData target, UnitEntityData attacker, AttackType attack_type)
        {
            return ((teamwork && (bool)attacker.Descriptor.State.Features.SoloTactics) || attacker.Descriptor.HasFact(this.Fact)) && attacker.IsAlly(this.Owner.Unit);
        }
    }


}
