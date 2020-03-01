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
    }
}
