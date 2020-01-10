using CallOfTheWild;
using Kingmaker.Blueprints;
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
                f.ReplaceComponent(weapon_training, CallOfTheWild.Helpers.Create<NewMechanics.WeaponGroupAttackBonus>(w =>
                                                                                                                     {
                                                                                                                         w.AttackBonus = weapon_training.AttackBonus;
                                                                                                                         w.WeaponGroup = weapon_training.WeaponGroup;
                                                                                                                     }
                                                                                                                     )
                                  );
            }
        }
    }
}
