using CallOfTheWild;
using Kingmaker.Blueprints;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProperFlanking20
{
    class Compatibility
    {
        static LibraryScriptableObject library = Main.library;
        static internal void load()
        {
            //no cover provided by those under effect of arrowsongm isntrel bardic performance
            var buffs = new BlueprintBuff[]
            {
                library.Get<BlueprintBuff>("6d6d9e06b76f5204a8b7856c78607d5d"), //courage
                library.Get<BlueprintBuff>("1fa5f733fa1d77743bf54f5f3da5a6b1"), //competence
                library.Get<BlueprintBuff>("ec38c2e60d738584983415cb8a4f508d"), //greatness
                library.Get<BlueprintBuff>("31e1f369cf0e4904887c96e4ef97a9cb"), //heroics
            };

            foreach (var b in buffs)
            {
                b.AddComponent(CallOfTheWild.Helpers.Create<CoverSpecial.NoCoverToCasterWithFact>(n => 
                                                                                                 { n.fact = CallOfTheWild.Archetypes.ArrowsongMinstrel.precise_minstrel;
                                                                                                   n.attack_types = new AttackType[] { AttackType.Ranged };
                                                                                                 }
                                                                                                 )
                              );
            }
        }
    }
}
