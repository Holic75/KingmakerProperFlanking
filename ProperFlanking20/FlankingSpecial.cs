using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;

namespace ProperFlanking20.FlankingSpecial
{
    class GangUp : Flanking.SpecialFlanking
    {
        public int min_additional_flankers = 2;

        public override bool isFlanking(UnitEntityData target)
        {         
            var engaged_array = target.CombatState.EngagedBy.ToArray();

            if (!engaged_array.Contains(this.Owner.Unit) || engaged_array.Length < min_additional_flankers + 1)
            {
                return false;
            }

            var need_flankers = min_additional_flankers;

            foreach (var teammate  in engaged_array)
            {
                if (teammate == this.Owner.Unit)
                {
                    continue;
                }

                if (teammate.IsFlatFootedTo(target))
                {
                    continue;
                }

                need_flankers--;

                if (need_flankers <= 0)
                {
                    return true;
                }
            }
            return false;
        }

        public override bool isFlankingTogether(UnitEntityData target, UnitEntityData partner)
        {
            return isFlanking(target) && target.CombatState.EngagedBy.Contains(partner);
        }
    }

    class ImprovedOutflank : Flanking.ModifyFlankingAngle
    {
        public float angle_increase;

        public override float getFlankingAngleIncrease(UnitEntityData target, UnitEntityData partner)
        {
            if (this.Owner.Unit == partner)
            {
                return 0f;
            }

            if (partner.Descriptor.HasFact(this.Fact) || this.Owner.State.Features.SoloTactics)
            {
                return angle_increase;
            }
            return 0f;
        }
    }


    class PackFlanking : Flanking.SpecialFlanking
    {
        public int radius = 5;
        public override bool isFlanking(UnitEntityData target)
        {
            bool solo_tactics = (bool)this.Owner.State.Features.SoloTactics;

            if (!(this.Owner.Unit.Descriptor.IsPet || (this.Owner.Unit.Descriptor.Pet != null)))
            {
                return false;
            }

            var teammate = this.Owner.Unit.Descriptor.IsPet ? this.Owner.Unit.Descriptor.Master.Value : this.Owner.Unit.Descriptor.Pet;
            if (!GameHelper.IsUnitInRange(teammate, this.Owner.Unit.Position, radius, false))
            {
                return false;
            }

            if (teammate.IsFlatFootedTo(target))
            {
                return false;
            }

            return teammate.CombatState.IsEngage(target) && (teammate.Ensure<Flanking.UnitPartSpecialFlanking>().hasBuff(this.Fact.Blueprint) || solo_tactics);
        }

        public override bool isFlankingTogether(UnitEntityData target, UnitEntityData partner)
        {
            return isFlanking(target) && ((partner == this.Owner.Unit.Descriptor.Pet) || (partner == this.Owner.Unit.Descriptor.Master.Value));
        }
    }


    //always flanks independently of position if there is at least one other attacker
    class AlwaysFlanking : Flanking.SpecialFlanking
    {
        public override bool isFlanking(UnitEntityData target)
        {
            return target.CombatState.EngagedBy.Count > 1 && target.CombatState.EngagedBy.Contains(this.Owner.Unit);
        }

        public override bool isFlankingTogether(UnitEntityData target, UnitEntityData partner)
        {
            return isFlanking(target) && target.CombatState.EngagedBy.Contains(partner);
        }
    }

    //caster always flank independently of his position as long as target is engaged by another ally
    public class AlwaysFlankedByCaster: CallOfTheWild.FlankingMechanics.IAlwaysFlanked
    {
        public override bool worksFor(UnitEntityData attacker)
        {
            return (attacker == this.Fact.MaybeContext.MaybeCaster)
                   && (this.Fact.MaybeContext?.MaybeCaster?.CombatState.IsEngage(this.Owner.Unit)).GetValueOrDefault()
                   && this.Owner.Unit.CombatState.EngagedBy.Count > 1;
        }
    }
}
