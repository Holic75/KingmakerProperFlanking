using Kingmaker.Blueprints.Classes;
using Kingmaker.Designers.Mechanics.Facts;
using CallOfTheWild;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingmaker.Blueprints;
using Kingmaker.RuleSystem;
using Kingmaker.Enums;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.EntitySystem.Stats;

namespace ProperFlanking20
{
    class NewFeats
    {
        static LibraryScriptableObject library = Main.library;
        static public BlueprintFeature quick_draw;
        static public BlueprintFeature enfilading_fire;
        static public BlueprintFeature phalanx_formation;
        static public BlueprintFeature pack_flanking;
        static public BlueprintFeature gang_up;
        static public BlueprintFeature friendly_fire_maneuvers;
        static public BlueprintFeature low_profile;


        internal static void load()
        {
            createLowProfile();
            createPhalanxFormation();
            createQuickDraw();
            fixImprovedPreciseShotDescription();

            createPackFlanking();
            createGangUp();
            createFriendlyFireManeuvers();
            createEnfiladingFire();
        }


        static void createLowProfile()
        {
            var ac_bonus = CallOfTheWild.Helpers.Create<ACBonusAgainstAttacks>(a => { a.AgainstRangedOnly = true; a.ArmorClassBonus = 1; a.Descriptor = ModifierDescriptor.Dodge; });
            low_profile = CallOfTheWild.Helpers.CreateFeature("LowProfileFeature",
                                                               "Low Profile",
                                                               "Yours small stature helps you avoid ranged attacks.\n" +
                                                               "Benefit: You gain a +1 dodge bonus to AC against ranged attacks. In addition, you do not provide soft cover to creatures when ranged attacks pass through your square.",
                                                               "adb468d1af064635bd61c1bc606eb724",
                                                               null,
                                                               FeatureGroup.Feat,
                                                               ac_bonus,
                                                               CallOfTheWild.Helpers.PrerequisiteStatValue(Kingmaker.EntitySystem.Stats.StatType.Dexterity, 13),
                                                               CallOfTheWild.Helpers.Create<CoverSpecial.DoesNotProvideCover>(),
                                                               CallOfTheWild.Helpers.Create<NewMechanics.PrerequisiteCharacterSize>(p => { p.value = Kingmaker.Enums.Size.Small; p.or_smaller = true; })
                                                               );
            library.AddFeats(low_profile);
        }


        static void createPhalanxFormation()
        {
            phalanx_formation = CallOfTheWild.Helpers.CreateFeature("PhalanxFormationFeature",
                                                           "Phalanx Formation",
                                                           "You are trained to use long weapons in tight formations.\n" +
                                                           "Benefit: When you wield a reach weapon with which you are proficient, allies don’t provide soft cover to opponents you attack with reach.\n" +
                                                           "Normal: Attacking a target that is beyond another creature, even an ally, can result in the target having soft cover from you.",
                                                           "f1e93e123f4c4e04978d0ec58597aa5a",
                                                           null,
                                                           FeatureGroup.Feat,
                                                           CallOfTheWild.Helpers.Create<CoverSpecial.IgnoreCoverForAttackType>(i => i.allowed_types = new AttackType[] { AttackType.Melee, AttackType.Touch }),
                                                           CallOfTheWild.Helpers.PrerequisiteStatValue(Kingmaker.EntitySystem.Stats.StatType.BaseAttackBonus, 1));
            phalanx_formation.Groups = phalanx_formation.Groups.AddToArray(FeatureGroup.CombatFeat);
            library.AddCombatFeats(phalanx_formation);
        }


        static void createQuickDraw()
        {
            quick_draw = CallOfTheWild.Helpers.CreateFeature("QuickDrawFeature",
                                                           "Quick Draw",
                                                           "You can switch to a different set of weapons as a free action.\n" +               
                                                           "Normal: Without this feat, you may switch weapons as a move action.",
                                                           "ca0ac04222414763abd00f25c83e0e83",
                                                           null,
                                                           FeatureGroup.Feat,
                                                           CallOfTheWild.Helpers.Create<QuickDraw.QuickDraw>(),
                                                           CallOfTheWild.Helpers.PrerequisiteStatValue(Kingmaker.EntitySystem.Stats.StatType.BaseAttackBonus, 1));

            quick_draw.Groups = quick_draw.Groups.AddToArray(FeatureGroup.CombatFeat);
            library.AddCombatFeats(quick_draw);
        }


        static void fixImprovedPreciseShotDescription()
        {
            var improved_precise_shot = library.Get<BlueprintFeature>("46f970a6b9b5d2346b10892673fe6e74");
            improved_precise_shot.SetDescription("our ranged attacks ignore the AC bonus granted to targets by anything less than total cover, and the miss chance granted to targets by anything less than total concealment. Total cover and total concealment provide their normal benefits against your ranged attacks.");
        }


        static void createPackFlanking()
        {
            var combat_expertise = library.Get<BlueprintFeature>("4c44724ffa8844f4d9bedb5bb27d144a");

            pack_flanking = CallOfTheWild.Helpers.CreateFeature("PackFlankingFeature",
                                               "Pack Flanking",
                                               "When you and your companion creature have this feat, your companion creature is adjacent to you or sharing your square, and you both threaten the same opponent, you are both considered to be flanking that opponent, regardless of your actual positioning.",
                                               "c269e2f31508424ab5352b21a5db770e",
                                               null,
                                               FeatureGroup.Feat,
                                               CallOfTheWild.Helpers.Create<FlankingSpecial.PackFlanking>(),
                                               CallOfTheWild.Helpers.Create<PrerequisitePet>(),
                                               CallOfTheWild.Helpers.PrerequisiteFeature(combat_expertise),
                                               CallOfTheWild.Helpers.PrerequisiteStatValue(Kingmaker.EntitySystem.Stats.StatType.Intelligence, 13));

            pack_flanking.Groups = pack_flanking.Groups.AddToArray(FeatureGroup.CombatFeat, FeatureGroup.TeamworkFeat);
            library.AddCombatFeats(pack_flanking);
            CallOfTheWild.Common.addTemworkFeats(pack_flanking);
        }


        static void createGangUp()
        {
            var combat_expertise = library.Get<BlueprintFeature>("4c44724ffa8844f4d9bedb5bb27d144a");

            gang_up = CallOfTheWild.Helpers.CreateFeature("GangUpFeature",
                                               "Gang Up",
                                               "You are considered to be flanking an opponent if at least two of your allies are threatening that opponent, regardless of your actual positioning.",
                                               "20b6ff8d2d2d4482b83cb512d63f6069",
                                               null,
                                               FeatureGroup.Feat,
                                               CallOfTheWild.Helpers.Create<FlankingSpecial.GangUp>(),
                                               CallOfTheWild.Helpers.PrerequisiteFeature(combat_expertise),
                                               CallOfTheWild.Helpers.PrerequisiteStatValue(Kingmaker.EntitySystem.Stats.StatType.Intelligence, 13));

            gang_up.Groups = gang_up.Groups.AddToArray(FeatureGroup.CombatFeat, FeatureGroup.TeamworkFeat);
            library.AddCombatFeats(gang_up);
            CallOfTheWild.Common.addTemworkFeats(gang_up);
        }


        static void createFriendlyFireManeuvers()
        {

            var point_blank_shot = library.Get<BlueprintFeature>("0da0c194d6e1d43419eb8d990b28e0ab");
            var precise_shot = library.Get<BlueprintFeature>("8f3d1e6b4be006f4d896081f2f889665");
            friendly_fire_maneuvers = CallOfTheWild.Helpers.CreateFeature("FriendlyFireManeuversFeature",
                                                                           "Friendly Fire Maneuvers",
                                                                           "Allies who also have this feat cannot provide soft cover to enemies, allowing you to make attacks of opportunity against an enemy even if those allies grant you soft cover against that foe’s attacks. If an ally who also has this feat casts a spell that targets the area you are in as it allows a Reflex saving throw to avoid the effect (such as fireball), you gain a +4 dodge bonus on that saving throw.",
                                                                           "24e988c2acf245f085ed9ed2c490753e",
                                                                           null,
                                                                           FeatureGroup.Feat,
                                                                           CallOfTheWild.Helpers.Create<CoverSpecial.NoCoverToFactOwners>(),
                                                                           CallOfTheWild.Helpers.Create<NewMechanics.FriendlyFireSavingThrowBonus>(f => { f.SavingThrow = StatType.SaveReflex;
                                                                                                                                                          f.value = 4;
                                                                                                                                                          f.Descriptor = ModifierDescriptor.Dodge;
                                                                                                                                                        }
                                                                                                                                                   ),
                                                                           CallOfTheWild.Helpers.PrerequisiteFeature(point_blank_shot),
                                                                           CallOfTheWild.Helpers.PrerequisiteFeature(precise_shot));

            friendly_fire_maneuvers.Groups = friendly_fire_maneuvers.Groups.AddToArray(FeatureGroup.CombatFeat, FeatureGroup.TeamworkFeat);
            library.AddCombatFeats(friendly_fire_maneuvers);
            CallOfTheWild.Common.addTemworkFeats(friendly_fire_maneuvers);
        }


        static void createEnfiladingFire()
        {
            var point_blank_shot = library.Get<BlueprintFeature>("0da0c194d6e1d43419eb8d990b28e0ab");
            var precise_shot = library.Get<BlueprintFeature>("8f3d1e6b4be006f4d896081f2f889665");
            enfilading_fire = CallOfTheWild.Helpers.CreateFeature("EnfiladingFireFeature",
                                                                    "Enfilading Fire",
                                                                    "You receive a +2 bonus on ranged attacks made against a foe flanked by 1 or more allies with this feat.",
                                                                    "46332f6c8fe84583ac5b06cfc9d20b2f",
                                                                    null,
                                                                    FeatureGroup.Feat,
                                                                    CallOfTheWild.Helpers.Create<NewMechanics.TeamworkBonusAgainstFlanked>(t => t.allowed_types = new AttackType[] { AttackType.Ranged, AttackType.RangedTouch }),
                                                                    CallOfTheWild.Helpers.PrerequisiteFeature(point_blank_shot),
                                                                    CallOfTheWild.Helpers.PrerequisiteFeature(precise_shot),
                                                                    CallOfTheWild.Helpers.Create<NewMechanics.PrerequisiteFeatFromGroup>(f => { f.group = FeatureGroup.TeamworkFeat;
                                                                                                                                                f.description = "one other teamwork feat";
                                                                                                                                              }
                                                                                                                                        )
                                                                );

            enfilading_fire.Groups = enfilading_fire.Groups.AddToArray(FeatureGroup.CombatFeat, FeatureGroup.TeamworkFeat);
            library.AddCombatFeats(enfilading_fire);
            CallOfTheWild.Common.addTemworkFeats(enfilading_fire);
        }




    }
}
