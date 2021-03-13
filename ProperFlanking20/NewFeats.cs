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
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Mechanics.Actions;

namespace ProperFlanking20
{
    public class NewFeats
    {
        static LibraryScriptableObject library = Main.library;
        static public BlueprintFeature quick_draw;
        static public BlueprintFeature enfilading_fire;
        static public BlueprintFeature phalanx_formation;
        static public BlueprintFeature pack_flanking;
        static public BlueprintFeature gang_up;
        static public BlueprintFeature friendly_fire_maneuvers;
        static public BlueprintFeature low_profile;

        static public BlueprintFeature wild_flanking;

        static public BlueprintFeature quick_dirty_trick;
        static public BlueprintFeature dirty_fighting;
        static public BlueprintFeature paired_opportunists;
        static public BlueprintFeature improved_outflank;

        static internal BlueprintBuff maneuver_as_attack_buff;



        internal static void load()
        {
            fixBuffsCover();
            createLowProfile();
            createPhalanxFormation();
            createQuickDraw();
            fixImprovedPreciseShot();

            createPackFlanking();
            createGangUp();
            createFriendlyFireManeuvers();
            createEnfiladingFire();

            createWildFlanking();
            createManeuverAsAttack();
            createQuickDirtyTrick();

            createDirtyFighting();
            createPairedOpportunists();

            createImprovedOutFlank();
        }


        static void createDirtyFighting()
        {
            var icon = CallOfTheWild.LoadIcons.Image2Sprite.Create(@"FeatIcons/DirtyFighting.png");
            dirty_fighting = CallOfTheWild.Helpers.CreateFeature("DirtyFightingFeature",
                                                                 "Dirty Fighting",
                                                                 "When you attempt a combat maneuver check against a foe you are flanking, you receive +2 bonus to combat maneuver check.\n" +
                                                                 "Special: This feat counts as having Dex 13, Int 13, Combat Expertise, and Improved Unarmed Strike for the purposes of meeting the prerequisites of the various improved combat maneuver feats, as well as feats that require those improved combat maneuver feats as prerequisites.",
                                                                 "",
                                                                 icon,
                                                                 FeatureGroup.Feat,
                                                                 CallOfTheWild.Helpers.Create<NewMechanics.CMBBonusAgainstFlanked>(c => c.Value = 2)
                                                                 );
            dirty_fighting.Groups = dirty_fighting.Groups.AddToArray(FeatureGroup.CombatFeat);
            library.AddCombatFeats(dirty_fighting);

            var improved_unarmed_strike = library.Get<BlueprintFeature>("7812ad3672a4b9a4fb894ea402095167");
            var combat_expertise = library.Get<BlueprintFeature>("4c44724ffa8844f4d9bedb5bb27d144a");

            string[] maneuver_features_guids = new string[] { "0f15c6f70d8fb2b49aa6cc24239cc5fa", //improved trip
                                                            "4cc71ae82bdd85b40b3cfe6697bb7949", //greater trip
                                                            "25bc9c439ac44fd44ac3b1e58890916f", //improved disarm
                                                            "ed699d64870044b43bb5a7fbe3f29494", //improved dirty trick
                                                            "52c6b07a68940af41b270b3710682dc7", //greater Dirty trick
                                                            "63d8e3a9ab4d72e4081a7862d7246a79", //greater disarm
                                                            CallOfTheWild.NewFeats.felling_smash.AssetGuid,
                                                            quick_dirty_trick.AssetGuid,
                                                            //also allow to work for feint
                                                            CallOfTheWild.NewFeats.improved_feint.AssetGuid,
                                                            CallOfTheWild.NewFeats.greater_feint.AssetGuid,
                                                            CallOfTheWild.NewFeats.ranged_feint.AssetGuid,
                                                            CallOfTheWild.NewFeats.two_weapon_feint.AssetGuid,
                                                            CallOfTheWild.NewFeats.improved_two_weapon_feint.AssetGuid
                                                          };

            var dirty_fighting_prereq = CallOfTheWild.Helpers.PrerequisiteFeature(dirty_fighting);
            foreach (var guid in maneuver_features_guids)
            {
                var f = library.Get<BlueprintFeature>(guid);
                var stat_reqs = f.GetComponents<PrerequisiteStatValue>().Where(p => (p.Stat == StatType.Dexterity || p.Stat == StatType.Intelligence) && p.Value <= 13).ToArray();
                var comp_reqs = f.GetComponents<CallOfTheWild.PrerequisiteMechanics.CompoundPrerequisites>().Where(p => p.any && p.prerequisites.Any(pp =>
                {
                    var ps = pp as PrerequisiteStatValue;
                    if (ps == null)
                    {
                        return false;
                    }
                    return (ps.Stat == StatType.Dexterity || ps.Stat == StatType.Intelligence) && ps.Value <= 13;
                })
                ).ToArray();

                var feat_reqs = f.GetComponents<PrerequisiteFeature>().Where(p => p.Feature == improved_unarmed_strike || p.Feature == combat_expertise).ToArray();
                foreach (var sr in stat_reqs)
                {
                    f.ReplaceComponent(sr, CallOfTheWild.Helpers.Create<CallOfTheWild.PrerequisiteMechanics.PrerequsiteOrAlternative>(pa => { pa.base_prerequsite = sr; pa.alternative_prerequsite = dirty_fighting_prereq; pa.Group = sr.Group; }));
                }

                foreach (var cp in comp_reqs)
                {
                    f.ReplaceComponent(cp, CallOfTheWild.Helpers.Create<CallOfTheWild.PrerequisiteMechanics.CompoundPrerequisites>(pa => { pa.prerequisites = cp.prerequisites.AddToArray(CallOfTheWild.Helpers.PrerequisiteFeature(dirty_fighting)); pa.any = true; }));
                }

                foreach (var fr in feat_reqs)
                {
                    f.ReplaceComponent(fr, CallOfTheWild.Helpers.PrerequisiteFeaturesFromList(new BlueprintFeature[] { fr.Feature, dirty_fighting}, any: fr.Group == Prerequisite.GroupType.Any));
                }
            }
        }


        static void createManeuverAsAttack()
        {
            maneuver_as_attack_buff = CallOfTheWild.Helpers.CreateBuff("ManeuverAsAttackBuff",
                                                                         "",
                                                                         "",
                                                                         "",
                                                                         null,
                                                                         null,
                                                                         CallOfTheWild.Helpers.Create<CombatManeuverBonus.UseWeaponForCombatManeuverLogic>());
            maneuver_as_attack_buff.SetBuffFlags(BuffFlags.HiddenInUi);

            var apply_buff = CallOfTheWild.Common.createContextActionApplyBuffToCaster(maneuver_as_attack_buff, CallOfTheWild.Helpers.CreateContextDuration(1), dispellable: false);
            var remove_buff = CallOfTheWild.Common.createContextActionOnContextCaster(CallOfTheWild.Common.createContextActionRemoveBuffFromCaster(maneuver_as_attack_buff));

            var bull_rush = library.Get<BlueprintFeature>("b3614622866fe7046b787a548bbd7f59");
            var features = new BlueprintFeature[] {library.Get<BlueprintFeature>("0f15c6f70d8fb2b49aa6cc24239cc5fa"), //trip
                                                   library.Get<BlueprintFeature>("9719015edcbf142409592e2cbaab7fe1"), //sunder
                                                   library.Get<BlueprintFeature>("25bc9c439ac44fd44ac3b1e58890916f"), //disarm
                                                   bull_rush
                                                  };

            foreach (var f in features)
            {
                bool is_bull_rush = f == bull_rush;
                var ability = f.GetComponent<AddFacts>().Facts[0] as BlueprintAbility;
                var action = ability.GetComponent<AbilityEffectRunAction>().Actions;
                var maneuver_type = (action.Actions[0] as ContextActionCombatManeuver).Type;
                string action_description = is_bull_rush ? "charge you can replace an attack" : "standard or full attack action you can replace any attack";
                var buff = CallOfTheWild.Helpers.CreateBuff(ability.name + "ToggleBuff",
                                                            ability.Name + $" ({(is_bull_rush ? "Charge " : "")}Attack Replacement)",
                                                            $"When performing {action_description} with {ability.Name} combat maneuver.",
                                                            "",
                                                            ability.Icon,
                                                            null,
                                                            CallOfTheWild.Helpers.Create<ManeuverAsAttack.AttackReplacementWithCombatManeuver>(a =>
                                                                                                                                              {
                                                                                                                                                a.maneuver = maneuver_type;
                                                                                                                                                a.only_first_attack = is_bull_rush;
                                                                                                                                                a.only_charge = is_bull_rush;
                                                                                                                                              }
                                                                                                                                              )
                                                            );

                var toggle = CallOfTheWild.Helpers.CreateActivatableAbility(ability.name + "ToggleAbility",
                                                                            buff.Name,
                                                                            buff.Description,
                                                                            "",
                                                                            buff.Icon,
                                                                            buff,
                                                                            AbilityActivationType.Immediately,
                                                                            Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Free,
                                                                            null,
                                                                            CallOfTheWild.Helpers.Create<CallOfTheWild.NewMechanics.PrimaryHandMeleeWeaponRestriction>());
                toggle.Group = CallOfTheWild.ActivatableAbilityGroupExtension.AttackReplacement.ToActivatableAbilityGroup();
                toggle.DeactivateImmediately = true;
                toggle.IsOnByDefault = true;
                //toggle.DeactivateIfCombatEnded = true;
                f.AddComponent(CallOfTheWild.Helpers.CreateAddFact(toggle));

                if (!is_bull_rush)
                {
                    ability.ReplaceComponent<AbilityEffectRunAction>(a => a.Actions = CallOfTheWild.Helpers.CreateActionList(apply_buff, a.Actions.Actions[0], remove_buff));
                }
            }

            var aspect_of_wolf_trip = library.Get<BlueprintAbility>("a4445991c5bb0ca40ac152bb4bf46a3c");
            aspect_of_wolf_trip.ReplaceComponent<AbilityEffectRunAction>(a => a.Actions = CallOfTheWild.Helpers.CreateActionList(apply_buff, a.Actions.Actions[0], remove_buff));
            NewSpells.blade_lash.ReplaceComponent<AbilityEffectRunAction>(a => a.Actions = CallOfTheWild.Helpers.CreateActionList(apply_buff, a.Actions.Actions[0], a.Actions.Actions[1], a.Actions.Actions[2], remove_buff));
        }


        static void createQuickDirtyTrick()
        {
            //var apply_buff = CallOfTheWild.Common.createContextActionApplyBuffToCaster(maneuver_as_attack_buff, CallOfTheWild.Helpers.CreateContextDuration(1), dispellable: false);
            //var remove_buff = CallOfTheWild.Common.createContextActionOnContextCaster(CallOfTheWild.Common.createContextActionRemoveBuffFromCaster(maneuver_as_attack_buff));

            var combat_expertise = library.Get<BlueprintFeature>("4c44724ffa8844f4d9bedb5bb27d144a");
            var dirty_trick = library.Get<BlueprintFeature>("ed699d64870044b43bb5a7fbe3f29494");
            quick_dirty_trick = CallOfTheWild.Helpers.CreateFeature("QuickDirtyTrickFeature",
                                                                    "Quick Dirty Trick",
                                                                    "On your turn, you can perform a single dirty trick combat maneuver in place of one of your melee attacks. You must choose the melee attack with the highest base attack bonus to make the dirty trick combat maneuver.",
                                                                    "",
                                                                    dirty_trick.Icon,
                                                                    FeatureGroup.Feat,
                                                                    CallOfTheWild.Helpers.Create<CallOfTheWild.PrerequisiteMechanics.CompoundPrerequisites>(cp =>
                                                                    {
                                                                        cp.any = true;
                                                                        cp.prerequisites = new Prerequisite[] { CallOfTheWild.Helpers.PrerequisiteStatValue(StatType.Intelligence, 13),
                                                                                                                CallOfTheWild.Helpers.PrerequisiteFeature(CallOfTheWild.Archetypes.SageCounselor.cunning_fist[0])
                                                                                                              };
                                                                    }
                                                                    ),
                                                                    CallOfTheWild.Helpers.PrerequisiteStatValue(StatType.BaseAttackBonus, 6),
                                                                    CallOfTheWild.Helpers.PrerequisiteFeature(combat_expertise),
                                                                    CallOfTheWild.Helpers.PrerequisiteFeature(dirty_trick)
                                                                    );
            quick_dirty_trick.Groups = quick_dirty_trick.Groups.AddToArray(FeatureGroup.CombatFeat);
            library.AddCombatFeats(quick_dirty_trick);

            foreach (var f in dirty_trick.GetComponent<AddFacts>().Facts.Reverse())
            {
                var ability = f as BlueprintAbility;
                var action = ability.GetComponent<AbilityEffectRunAction>().Actions;
                var maneuver_type = (action.Actions[0] as ContextActionCombatManeuver).Type;
                var buff = CallOfTheWild.Helpers.CreateBuff(ability.name + "ToggleBuff",
                                                            ability.Name + " (First Attack Replacement)",
                                                            $"When performing full attack action, if you can make more than one attack, you can replace first attack with {ability.Name} combat maneuver.",
                                                            "",
                                                            ability.Icon,
                                                            null,
                                                            CallOfTheWild.Helpers.Create<ManeuverAsAttack.AttackReplacementWithCombatManeuver>(a => { a.maneuver = maneuver_type;
                                                                                                                                                      a.only_first_attack = true;
                                                                                                                                                      a.only_full_attack = true;
                                                                                                                                                    }
                                                                                                                                               )
                                                            );

                var toggle = CallOfTheWild.Helpers.CreateActivatableAbility(ability.name + "ToggleAbility",
                                                                            buff.Name,
                                                                            buff.Description,
                                                                            "",
                                                                            buff.Icon,
                                                                            buff,
                                                                            AbilityActivationType.Immediately,
                                                                            Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Free,
                                                                            null,
                                                                            CallOfTheWild.Helpers.Create<CallOfTheWild.NewMechanics.PrimaryHandMeleeWeaponRestriction>());
                toggle.Group = CallOfTheWild.ActivatableAbilityGroupExtension.AttackReplacement.ToActivatableAbilityGroup();
                toggle.DeactivateImmediately = true;
                toggle.IsOnByDefault = true;
                //toggle.DeactivateIfCombatEnded = true;
                quick_dirty_trick.AddComponent(CallOfTheWild.Helpers.CreateAddFact(toggle));
                //ability.ReplaceComponent<AbilityEffectRunAction>(a => a.Actions = CallOfTheWild.Helpers.CreateActionList(apply_buff, a.Actions.Actions[0], remove_buff));
            }
        }


        static void fixBuffsCover()
        {
            //do not apply cover for certain melee attacks which might be initiated by game as attacks from range, but which are actually not
            var no_cover = CallOfTheWild.Helpers.Create<CoverSpecial.IgnoreCoverForAttackType>(i => i.allowed_types = new AttackType[] { AttackType.Melee, AttackType.Touch });
            CallOfTheWild.NewSpells.bladed_dash_buff.AddComponent(no_cover);
            CallOfTheWild.KineticistFix.blade_rush_buff.AddComponent(no_cover);
        }


        static void createImprovedOutFlank()
        {
            var outflank = library.Get<BlueprintFeature>("422dab7309e1ad343935f33a4d6e9f11");
            
            improved_outflank = CallOfTheWild.Helpers.CreateFeature("ImprovedOutflankFeature",
                                                                "Improved Outflank",
                                                                "Whenever you and an ally who also has this feat are threatening the same foe, you are considered to be flanking that foe if you are adjacent to the point from which you would be able to flank the foe with your ally.",
                                                                "",
                                                                outflank.Icon,
                                                                FeatureGroup.Feat,
                                                                CallOfTheWild.Helpers.Create<FlankingSpecial.ImprovedOutflank>(i => i.angle_increase = (float)Math.PI/4),//45 degrees down to 90 degrees angle between attackers
                                                                CallOfTheWild.Helpers.PrerequisiteFeature(outflank),
                                                                CallOfTheWild.Helpers.PrerequisiteStatValue(StatType.BaseAttackBonus, 6)
                                                               );


            improved_outflank.Groups = improved_outflank.Groups.AddToArray(FeatureGroup.TeamworkFeat);
            library.AddCombatFeats(improved_outflank);
            CallOfTheWild.Common.addTemworkFeats(improved_outflank);
        }


        static void createWildFlanking()
        {
            var icon = CallOfTheWild.LoadIcons.Image2Sprite.Create(@"FeatIcons/WildFlanking.png");
            wild_flanking = CallOfTheWild.Helpers.CreateFeature("WildFlankingFeature",
                                                                "Wild Flanking",
                                                                "When you are flanking an opponent with an ally who also possesses this feat, you can throw yourself into your attacks in such a way that your opponent takes extra damage, at the risk of these attacks striking your ally as well. When you choose to use this feat, check the results of your attack roll against both your opponent’s AC and your ally’s AC. If you hit your opponent, you deal bonus damage as though you were using Power Attack. If you hit your ally, the ally takes no damage from your attack except this bonus damage. It is possible to hit both your enemy and your abettor with one attack. Extra damage from this feat stacks with Power Attack.",
                                                                "",
                                                                icon,
                                                                FeatureGroup.Feat,
                                                                CallOfTheWild.Helpers.PrerequisiteFeature(library.Get<BlueprintFeature>("9972f33f977fc724c838e59641b2fca5")),
                                                                CallOfTheWild.Helpers.PrerequisiteStatValue(StatType.BaseAttackBonus, 4)
                                                               );

            var buff = CallOfTheWild.Helpers.CreateBuff("WildFlankingBuff",
                                                          wild_flanking.Name + " Partner",
                                                          wild_flanking.Description,
                                                          "",
                                                          wild_flanking.Icon,
                                                          null,
                                                          CallOfTheWild.Helpers.Create<UniqueBuff>());
            buff.Stacking = StackingType.Stack;
            var apply_buff = Common.createContextActionApplyBuff(buff, CallOfTheWild.Helpers.CreateContextDuration(), dispellable: false, is_permanent: true);
            var ability = CallOfTheWild.Helpers.CreateAbility("WildFlankingAbility",
                                                        "Select " + wild_flanking.Name + " Partner",
                                                        wild_flanking.Description,
                                                        "",
                                                        wild_flanking.Icon,
                                                        AbilityType.Special,
                                                        Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Free,
                                                        AbilityRange.Close,
                                                        "",
                                                        "",
                                                        CallOfTheWild.Helpers.CreateRunActions(apply_buff),
                                                        CallOfTheWild.Helpers.Create<CallOfTheWild.TeamworkMechanics.AbilityTargetHasFactOrCasterHasSoloTactics>(a => a.fact = wild_flanking)
                                                        );
            ability.setMiscAbilityParametersSingleTargetRangedFriendly();

            var ability_remove = CallOfTheWild.Helpers.CreateAbility("WildFlankingRemoveAbility",
                                            "Unselect " + wild_flanking.Name + " Partner",
                                            wild_flanking.Description,
                                            "",
                                             CallOfTheWild.LoadIcons.Image2Sprite.Create(@"FeatIcons/WildFlankingDeactivate.png"),
                                            AbilityType.Special,
                                            Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Free,
                                            AbilityRange.Personal,
                                            "",
                                            "",
                                            CallOfTheWild.Helpers.CreateRunActions(CallOfTheWild.Helpers.Create<CallOfTheWild.BuffMechanics.RemoveUniqueBuff>(r => r.buff = buff))
                                            );
            ability_remove.setMiscAbilityParametersSingleTargetRangedFriendly();
            var wrapper = Common.createVariantWrapper("WildFlankingBase", "", ability, ability_remove);
            wrapper.SetName(wild_flanking.Name);
           
            wild_flanking.AddComponents(CallOfTheWild.Helpers.CreateAddFact(wrapper),
                                        CallOfTheWild.Helpers.Create<NewMechanics.WildFlanking>(w =>
                                                                                                {
                                                                                                    w.GreaterPowerAttackBlueprint = library.Get<BlueprintFeature>("1b058a5ce1de415449a0f105c55b5f8b"); //2h fighter power attack
                                                                                                    w.wild_flanking_mark = buff;
                                                                                                }
                                                                                                )
                                        );
            wild_flanking.Groups = wild_flanking.Groups.AddToArray(FeatureGroup.CombatFeat, FeatureGroup.TeamworkFeat);
            library.AddCombatFeats(wild_flanking);
            CallOfTheWild.Common.addTemworkFeats(wild_flanking);
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


        static void fixImprovedPreciseShot()
        {
            var improved_precise_shot = library.Get<BlueprintFeature>("46f970a6b9b5d2346b10892673fe6e74");
            improved_precise_shot.SetDescription("Your ranged attacks ignore the AC bonus granted to targets by anything less than total cover, and the miss chance granted to targets by anything less than total concealment. Total cover and total concealment provide their normal benefits against your ranged attacks.");
            improved_precise_shot.AddComponent(CallOfTheWild.Helpers.Create<CoverSpecial.IgnoreCoverForAttackType>(i => i.allowed_types = new AttackType[] { AttackType.Ranged, AttackType.RangedTouch }));
        }


        static void createPackFlanking()
        {
            var icon = CallOfTheWild.LoadIcons.Image2Sprite.Create(@"FeatIcons/PackFlanking.png");
            var combat_expertise = library.Get<BlueprintFeature>("4c44724ffa8844f4d9bedb5bb27d144a");

            pack_flanking = CallOfTheWild.Helpers.CreateFeature("PackFlankingFeature",
                                               "Pack Flanking",
                                               "When you and your companion creature have this feat, your companion creature is adjacent to you or sharing your square, and you both threaten the same opponent, you are both considered to be flanking that opponent, regardless of your actual positioning.",
                                               "c269e2f31508424ab5352b21a5db770e",
                                               icon,
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

            gang_up.Groups = gang_up.Groups.AddToArray(FeatureGroup.CombatFeat);
            library.AddCombatFeats(gang_up);
            //CallOfTheWild.Common.addTemworkFeats(gang_up);
        }


        static void createFriendlyFireManeuvers()
        {
            var icon = CallOfTheWild.LoadIcons.Image2Sprite.Create(@"FeatIcons/FriendlyFireManeuvers.png");
            var point_blank_shot = library.Get<BlueprintFeature>("0da0c194d6e1d43419eb8d990b28e0ab");
            var precise_shot = library.Get<BlueprintFeature>("8f3d1e6b4be006f4d896081f2f889665");
            friendly_fire_maneuvers = CallOfTheWild.Helpers.CreateFeature("FriendlyFireManeuversFeature",
                                                                           "Friendly Fire Maneuvers",
                                                                           "Allies who also have this feat cannot provide soft cover to enemies, allowing you to make attacks of opportunity against an enemy even if those allies grant you soft cover against that foe’s attacks. If an ally who also has this feat casts a spell that targets the area you are in as it allows a Reflex saving throw to avoid the effect (such as fireball), you gain a +4 dodge bonus on that saving throw.",
                                                                           "24e988c2acf245f085ed9ed2c490753e",
                                                                           icon,
                                                                           FeatureGroup.Feat,
                                                                           CallOfTheWild.Helpers.Create<CoverSpecial.NoCoverToFactOwners>(),
                                                                           CallOfTheWild.Helpers.Create<CoverSpecial.NoCoverFromFactOwners>(),
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
            var icon = CallOfTheWild.LoadIcons.Image2Sprite.Create(@"FeatIcons/EnfiladingFire.png");
            var point_blank_shot = library.Get<BlueprintFeature>("0da0c194d6e1d43419eb8d990b28e0ab");
            var precise_shot = library.Get<BlueprintFeature>("8f3d1e6b4be006f4d896081f2f889665");
            enfilading_fire = CallOfTheWild.Helpers.CreateFeature("EnfiladingFireFeature",
                                                                    "Enfilading Fire",
                                                                    "You receive a +2 bonus on ranged attacks made against a foe flanked by 1 or more allies with this feat.",
                                                                    "46332f6c8fe84583ac5b06cfc9d20b2f",
                                                                    icon,
                                                                    FeatureGroup.Feat,
                                                                    CallOfTheWild.Helpers.Create<NewMechanics.TeamworkBonusAgainstFlanked>(t => t.allowed_types = new AttackType[] { AttackType.Ranged, AttackType.RangedTouch }),
                                                                    CallOfTheWild.Helpers.PrerequisiteFeature(point_blank_shot),
                                                                    CallOfTheWild.Helpers.PrerequisiteFeature(precise_shot),
                                                                    CallOfTheWild.Helpers.Create<NewMechanics.PrerequisiteFeatFromGroup>(f => f.group = FeatureGroup.TeamworkFeat)
                                                                );

            enfilading_fire.Groups = enfilading_fire.Groups.AddToArray(FeatureGroup.CombatFeat, FeatureGroup.TeamworkFeat);
            library.AddCombatFeats(enfilading_fire);
            CallOfTheWild.Common.addTemworkFeats(enfilading_fire);
        }


        static void createPairedOpportunists()
        {
            var icon = CallOfTheWild.LoadIcons.Image2Sprite.Create(@"FeatIcons/PairedOpportunists.png");
            paired_opportunists = CallOfTheWild.Helpers.CreateFeature("PairedOpportunistsFeature",
                                                                    "Paired Opportunists",
                                                                    "Whenever you are adjacent to an ally who also has this feat, you receive a +4 circumstance bonus on attacks of opportunity against creatures that you both threaten. Enemies that provoke attacks of opportunity from your ally also provoke attacks of opportunity from you so long as you threaten them (even if the situation or an ability would normally deny you the attack of opportunity). This does not allow you to take more than one attack of opportunity against a creature for a given action.",
                                                                    "04d9fee2f3ec497395aba26230b48d2c",
                                                                    icon,
                                                                    FeatureGroup.Feat,
                                                                    CallOfTheWild.Helpers.Create<PairedOpportuists.PairedOpportunistsAttackBonus>(p => { p.bonus = 4; p.descriptor = ModifierDescriptor.Circumstance; })
                                                                );
            PairedOpportuists.UnitCombatEngagementController_ForceAttackOfOpportunity_Patch.PairedOpportunistFact = paired_opportunists;
            paired_opportunists.Groups = paired_opportunists.Groups.AddToArray(FeatureGroup.CombatFeat, FeatureGroup.TeamworkFeat);
            library.AddCombatFeats(paired_opportunists);
            CallOfTheWild.Common.addTemworkFeats(paired_opportunists);
        }

    }
}
