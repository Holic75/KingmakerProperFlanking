using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Controllers.Combat;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProperFlanking20.PairedOpportuists
{

    [Harmony12.HarmonyPatch(typeof(UnitCombatEngagementController))]
    [Harmony12.HarmonyPatch("ForceAttackOfOpportunity", Harmony12.MethodType.Normal)]
    class UnitCombatEngagementController_ForceAttackOfOpportunity_Patch
    {
        static readonly Type UnitCombatEngagementController_AooPair = Harmony12.AccessTools.Inner(typeof(UnitCombatEngagementController), "AoOPair");

        public static BlueprintUnitFact PairedOpportunistFact;
        static Object createAooPair(UnitEntityData attacker, UnitEntityData target)
        {
            var aoo_pair = Activator.CreateInstance(UnitCombatEngagementController_AooPair);
            CallOfTheWild.Helpers.SetField(aoo_pair, "Attacker", attacker);
            CallOfTheWild.Helpers.SetField(aoo_pair, "Target", target);

            return aoo_pair;
        }


        static bool hasPair(IList attack_of_opportunity_pairs, UnitEntityData attacker, UnitEntityData target)
        {
            //Main.logger.Log(attack_of_opportunity_pairs.Count.ToString());
            foreach (var aoo_pair in attack_of_opportunity_pairs)
            {
                var attacker_i = CallOfTheWild.Helpers.GetField<UnitEntityData>(aoo_pair, "Attacker");
                var target_i = CallOfTheWild.Helpers.GetField<UnitEntityData>(aoo_pair, "Target");

                if (target_i == target && attacker_i == attacker)
                {
                    return true;
                }
            }

            return false;
        }


        static void Postfix(UnitCombatEngagementController __instance, UnitEntityData attacker, UnitEntityData target)
        {
            var tr = Harmony12.Traverse.Create(__instance);
            var m_ForceAttackOfOpportunity = tr.Field("m_ForceAttackOfOpportunity").GetValue<IList>();
            //get units adjacent to attacker
            //check if they have solo tactics with paired opportunist or both attacker and unit have paired opportunist

            bool attacker_has_paired_ooportunist = attacker.Descriptor.Progression.Features.HasFact(PairedOpportunistFact);

            foreach (var u in target.CombatState.EngagedBy)
            {
                if (u == attacker)
                {
                    continue;
                }
                if (u.Descriptor.Progression.Features.HasFact(PairedOpportunistFact)
                     && (attacker_has_paired_ooportunist || (bool)u.Descriptor.State.Features.SoloTactics)
                     && (u.DistanceTo(attacker) < u.Corpulence + attacker.Corpulence + 5.Feet().Meters)
                     && !hasPair(m_ForceAttackOfOpportunity, u, target)
                     )
                {
                    __instance.ForceAttackOfOpportunity(u, target);
                }
            }
        }
    }


    [AllowedOn(typeof(BlueprintUnitFact))]
    [AllowMultipleComponents]
    public class PairedOpportunistsAttackBonus : RuleInitiatorLogicComponent<RuleAttackRoll>
    {
        public int bonus;
        public Kingmaker.Enums.ModifierDescriptor descriptor;
        public override void OnEventAboutToTrigger(RuleAttackRoll evt)
        {
            var attack_with_weapon = evt.RuleAttackWithWeapon;

            if (attack_with_weapon == null || !attack_with_weapon.IsAttackOfOpportunity)
            {
                return;
            }

            bool has_solo_tactics = evt.Initiator.Descriptor.State.Features.SoloTactics != null;
            foreach (var u in evt.Target.CombatState.EngagedBy)
            {
                if ((u.Descriptor.Progression.Features.HasFact(this.Fact.Blueprint) || has_solo_tactics)
                     && u.DistanceTo(this.Owner.Unit) < u.Corpulence + evt.Initiator.Corpulence + 5.Feet().Meters)
                {
                    evt.AddTemporaryModifier(evt.Initiator.Stats.AdditionalAttackBonus.AddModifier(bonus, (GameLogicComponent)this, descriptor));
                }
            }
        }

        public override void OnEventDidTrigger(RuleAttackRoll evt)
        {

        }
    }
}
