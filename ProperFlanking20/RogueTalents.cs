using CallOfTheWild;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProperFlanking20
{
    class RogueTalents
    {
        static LibraryScriptableObject library = Main.library;

        internal static void fixRogueTalents()
        {
            var classes = new BlueprintCharacterClass[] {library.Get<BlueprintCharacterClass>("299aa766dee3cbf4790da4efb8c72484"),
                                                         library.Get<BlueprintCharacterClass>("c75e0971973957d4dbad24bc7957e4fb")
                                                        };
            var rogue_talents = new BlueprintFeatureSelection[] {library.Get<BlueprintFeatureSelection>("04430ad24988baa4daa0bcd4f1c7d118"), //slayer 2
                                                                 library.Get<BlueprintFeatureSelection>("43d1b15873e926848be2abf0ea3ad9a8"), //slayer 6
                                                                 library.Get<BlueprintFeatureSelection>("913b9cf25c9536949b43a2651b7ffb66"), //slayer 10
                                                                 library.Get<BlueprintFeatureSelection>("c074a5d615200494b8f2a9c845799d93") //rogue talents
                                                                };

            var dirty_trick = library.Get<BlueprintFeature>("ed699d64870044b43bb5a7fbe3f29494");
            var greater_dirty_trick = library.Get<BlueprintFeature>("52c6b07a68940af41b270b3710682dc7");

            var greater_dirty_trick_allowed = CallOfTheWild.Helpers.CreateFeature("UnderhandedTrickRogueTalentGreaterAllowedFeature",
                                                        "Underhanded Trick and Character Level 6",
                                                        "",
                                                        "",
                                                        dirty_trick.Icon,
                                                        FeatureGroup.None
                                                        );
            greater_dirty_trick_allowed.HideInCharacterSheetAndLevelUp = true;
            var greater_dirty_trick_allowed_prereq = CallOfTheWild.Helpers.PrerequisiteFeature(greater_dirty_trick_allowed);
            var underhanded = CallOfTheWild.Helpers.CreateFeature("UnderhandedTrickRogueTalentFeature",
                                                                    "Underhanded Trick",
                                                                    "A rogue who selects this talent gains Improved Dirty Trick as a bonus feat, even if she does not meet the prerequisites. At 6th level, she is treated as if she meets all the prerequisites for Greater Dirty Trick (although she must take the feat as normal).",
                                                                    "",
                                                                    dirty_trick.Icon,
                                                                    FeatureGroup.RogueTalent,
                                                                    CallOfTheWild.Helpers.CreateAddFact(dirty_trick),
                                                                    CallOfTheWild.Helpers.CreateAddFeatureOnClassLevel(greater_dirty_trick_allowed, 6, classes)
                                                                    );

            foreach (var prereq in greater_dirty_trick.GetComponents<Prerequisite>().ToArray())
            {
                greater_dirty_trick.ReplaceComponent(prereq, CallOfTheWild.Helpers.Create<CallOfTheWild.PrerequisiteMechanics.PrerequsiteOrAlternative>(p => { p.base_prerequsite = prereq; p.alternative_prerequsite = greater_dirty_trick_allowed_prereq; }));
            }

            foreach (var rt in rogue_talents)
            {
                rt.AllFeatures = rt.AllFeatures.AddToArray(underhanded);
            }                                                                  
        }
    }
}
