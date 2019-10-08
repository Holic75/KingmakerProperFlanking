using Kingmaker.Blueprints.Facts;
using Kingmaker.UnitLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProperFlanking20
{
    class UnloadCTT
    {//attempts to remove abilities given by Closer To Table Top
        static public void run()
        {
            List<BlueprintUnitFact> missing_facts = new List<BlueprintUnitFact>();
            missing_facts.Add(CallOfTheWild.SaveGameFix.createDummyAbility("DropWeaponsAction", "47148ed4e2b747a0be47858716a33ae9"));
            missing_facts.Add(CallOfTheWild.SaveGameFix.createDummyAbility("CombatManeuversStandard", "708e4347b8f74e61bc87495eedeba198"));
            missing_facts.Add(CallOfTheWild.SaveGameFix.createDummyActivatableAbility("DisarmToggleAbility", "dc8f4de1da32443db350ac82385ac13d"));
            missing_facts.Add(CallOfTheWild.SaveGameFix.createDummyActivatableAbility("SunderArmorToggleAbility", "a0808193670849199f52deb731638297"));
            missing_facts.Add(CallOfTheWild.SaveGameFix.createDummyActivatableAbility("TripToggleAbility", "f59e405d9b3840a181a5668aae07c18b"));

            Action<UnitDescriptor> fix_action = delegate (UnitDescriptor u)
            {
                foreach (var missing_fact in missing_facts)
                {
                    if (u.HasFact(missing_fact))
                    {
                        u.RemoveFact(missing_fact);
                    }
                }
            };

            CallOfTheWild.SaveGameFix.save_game_actions.Add(fix_action);
        }
    }
}
