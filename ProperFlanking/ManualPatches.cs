using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProperFlanking
{
    namespace ManualPatching
    {

        class CoordinatedShotAttcakBonus_OnEventAboutToTrigger_Patch
        {
            static bool Prefix(object __instance, RuleCalculateAttackBonus evt)
            {
                var tr = Harmony12.Traverse.Create(__instance);

                if (!evt.Weapon.Blueprint.IsRanged)
                    return false;

                int attack_bonus = tr.Field("AttackBonus").GetValue<int>();
                int additional_flank_bonus = tr.Field("AdditionalFlankBonus").GetValue<int>();
                BlueprintUnitFact coordinated_shot_fact = tr.Field("CoordinatedShotFact").GetValue<BlueprintUnitFact>();
                UnitDescriptor owner = tr.Property("Owner").GetValue<UnitDescriptor>();

                int bonus = 0;
                bool solo_tactics = owner.State.Features.SoloTactics;

                foreach (UnitEntityData unitEntityData in evt.Target.CombatState.EngagedBy)
                {
                    if ((unitEntityData.Descriptor.HasFact(coordinated_shot_fact) || solo_tactics)
                        && unitEntityData != owner.Unit)
                    {
                        bonus = Math.Max(bonus, (Flanking.isFlankedBy(evt.Target, unitEntityData) ? attack_bonus + additional_flank_bonus : attack_bonus));
                    }
                }

                if (bonus == 0)
                {
                    return false;
                }

                evt.AddBonus(bonus, tr.Property("Fact").GetValue<Fact>());
                return false;
            }
        }
        
    }
}
