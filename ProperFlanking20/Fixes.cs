using CallOfTheWild;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProperFlanking20
{
    class Fixes
    {
        static LibraryScriptableObject library = Main.library;
        internal static void fixWeaponTrainingToWorkWithCombatManeuvers()
        {
            var features = library.Get<BlueprintFeatureSelection>("5f3cc7b9a46b880448275763fe70c0b0").AllFeatures;

            foreach (var f in features)
            {
                var weapon_training = f.GetComponent<WeaponGroupAttackBonus>();
                f.ReplaceComponent(weapon_training, CallOfTheWild.Helpers.Create<NewMechanics.WeaponGroupAttackBonusCompatibleWithCMB>(w =>
                                                                                                                                         {
                                                                                                                                             w.AttackBonus = weapon_training.AttackBonus;
                                                                                                                                             w.WeaponGroup = weapon_training.WeaponGroup;
                                                                                                                                             w.Descriptor = weapon_training.Descriptor;
                                                                                                                                         }
                                                                                                                                         )
                                                                                                                                      );
            }
        }

        static internal void fixVarnFeats()
        {
            var varn_companion = ResourcesLibrary.TryGetBlueprint<BlueprintUnit>("e83a03d50fedd35449042ce73f1b6908");
            var varn_feature = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("2babd2d4687b5ee428966322eccfe4b6");
            var varn_class_levels = varn_feature.GetComponent<AddClassLevels>();
            varn_class_levels.Selections[2].Features[1] = NewFeats.dirty_fighting;
            varn_class_levels.Selections[1].Features[0] = RogueTalents.underhanded;
        }


        static internal void fixAldoriSwordlordPrc()
        {
            var dueling_mastery = library.Get<BlueprintFeature>("c3a66c1bbd2fb65498b130802d5f183a");
                                                                 
            var selection = CallOfTheWild.Helpers.CreateFeatureSelection("AldoriSwordLordQuickDrawSelection",
                                                                         "Quick Draw",
                                                                         "An Aldori swordlord gains Quick Draw as a bonus feat. If the character already has this feat, he instead gains Aldori Dueling Mastery. If he already has both feats, he instead gains a Combat feat of his choice as a bonus feat. The swordlord must meet all prerequisites of the selected Combat feat.",
                                                                         "",
                                                                         NewFeats.quick_draw.Icon,
                                                                         FeatureGroup.None);

            var dueling_mastery_selection = CallOfTheWild.Helpers.CreateFeature("AldoriSwordLordQuickDrawDuelingMasteryFeature",
                                                                                dueling_mastery.Name,
                                                                                dueling_mastery.Description,
                                                                                "",
                                                                                dueling_mastery.Icon,
                                                                                FeatureGroup.None,
                                                                                CallOfTheWild.Helpers.PrerequisiteNoFeature(dueling_mastery),
                                                                                CallOfTheWild.Helpers.PrerequisiteFeature(NewFeats.quick_draw),
                                                                                CallOfTheWild.Helpers.CreateAddFact(dueling_mastery)
                                                                                );

            var combat_feat = library.Get<BlueprintFeatureSelection>("41c8486641f7d6d4283ca9dae4147a9f");

            selection.AllFeatures = new BlueprintFeature[] { NewFeats.quick_draw, dueling_mastery_selection, combat_feat };

            var swordlord_prc = library.Get<BlueprintProgression>("71edc73e46794fc44925259322c146e5");
            swordlord_prc.LevelEntries[0].Features.Add(selection);
        }
    }
}
