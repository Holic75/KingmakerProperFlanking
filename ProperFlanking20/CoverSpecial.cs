using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
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

        public override bool ignoresCover(ItemEntityWeapon weapon)
        {
            if (weapon == null)
            {
                return false;
            }

            return allowed_types.Contains(weapon.Blueprint.AttackType);
        }
    }



    public class DoesNotProvideCover : Cover.SpecialProvideNoCover
    {
        public override bool doesNotProvideCoverToFrom(UnitEntityData target, UnitEntityData attacker, ItemEntityWeapon weapon)
        {
            return true;
        }
    }



    public class NoCoverToFactOwners : Cover.SpecialProvideNoCover
    {
        bool teamwork = true;

        public override bool doesNotProvideCoverToFrom(UnitEntityData target, UnitEntityData attacker, ItemEntityWeapon weapon)
        {
            return ((teamwork && (bool)attacker.Descriptor.State.Features.SoloTactics) || attacker.Descriptor.HasFact(this.Fact)) && attacker.IsAlly(this.Owner.Unit);
        }
    }


}
