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

        static public BlueprintFeature improved_feint;
        static public BlueprintAbility improved_feint_ability;
        static public BlueprintFeature greater_feint;
        static public BlueprintFeature ranged_feint;
        static public BlueprintFeature two_weapon_feint;
        static public BlueprintFeature improved_two_weapon_feint;

        static public BlueprintFeature swordplay_style;
        static public BlueprintActivatableAbility swordplay_style_ability;
        static public BlueprintFeature swordplay_upset;
        static public BlueprintFeature wild_flanking;

        static public BlueprintFeature quick_dirty_trick;
        static public BlueprintFeature dirty_fighting;


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

            createFeintFeats();

            createSwordplayStyle();

            createWildFlanking();
            createManeuverAsAttack();
            createQuickDirtyTrick();

            createDirtyFighting();
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
                                                            quick_dirty_trick.AssetGuid
                                                          };

            var dirty_fighting_prereq = CallOfTheWild.Helpers.PrerequisiteFeature(dirty_fighting);
            foreach (var guid in maneuver_features_guids)
            {
                var f = library.Get<BlueprintFeature>(guid);
                var stat_reqs = f.GetComponents<PrerequisiteStatValue>().Where(p => (p.Stat == StatType.Dexterity || p.Stat == StatType.Intelligence) && p.Value <= 13).ToArray();
                var feat_reqs = f.GetComponents<PrerequisiteFeature>().Where(p => p.Feature == improved_unarmed_strike || p.Feature == combat_expertise).ToArray();
                foreach (var sr in stat_reqs)
                {
                    f.ReplaceComponent(sr, CallOfTheWild.Helpers.Create<CallOfTheWild.PrerequisiteMechanics.PrerequsiteOrAlternative>(pa => { pa.base_prerequsite = sr; pa.alternative_prerequsite = dirty_fighting_prereq; pa.Group = sr.Group; }));
                }

                foreach (var fr in feat_reqs)
                {
                    f.ReplaceComponent(fr, CallOfTheWild.Helpers.Create<CallOfTheWild.PrerequisiteMechanics.PrerequsiteOrAlternative>(pa => { pa.base_prerequsite = fr; pa.alternative_prerequsite = dirty_fighting_prereq; pa.Group = fr.Group; }));
                }
            }
        }


        static void createManeuverAsAttack()
        {
            var features = new BlueprintFeature[] {library.Get<BlueprintFeature>("0f15c6f70d8fb2b49aa6cc24239cc5fa"), //trip
                                                   library.Get<BlueprintFeature>("9719015edcbf142409592e2cbaab7fe1"), //sunder
                                                   library.Get<BlueprintFeature>("25bc9c439ac44fd44ac3b1e58890916f"), //disarm
                                                  };

            foreach (var f in features)
            {
                var ability = f.GetComponent<AddFacts>().Facts[0] as BlueprintAbility;
                var action = ability.GetComponent<AbilityEffectRunAction>().Actions;

                var buff = CallOfTheWild.Helpers.CreateBuff(ability.name + "ToggleBuff",
                                                            ability.Name + " (First Attack Replacement)",
                                                            $"When performing full attack action, if you can make more than one attack, you can replace first attack with {ability.Name} combat maneuver.",
                                                            "",
                                                            ability.Icon,
                                                            null,
                                                            CallOfTheWild.Helpers.Create<CallOfTheWild.AttackReplacementMechanics.ReplaceAttackWithActionOnFullAttack>(r => r.action = action)
                                                            );

                var toggle = CallOfTheWild.Helpers.CreateActivatableAbility(ability.name + "ToggleAbility",
                                                                            buff.Name,
                                                                            buff.Description,
                                                                            "",
                                                                            buff.Icon,
                                                                            buff,
                                                                            AbilityActivationType.Immediately,
                                                                            Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Free,
                                                                            null);
                toggle.Group = CallOfTheWild.ActivatableAbilityGroupExtension.AttackReplacement.ToActivatableAbilityGroup();
                toggle.DeactivateImmediately = true;

                f.AddComponent(CallOfTheWild.Helpers.CreateAddFact(toggle));
            }
        }


        static void createQuickDirtyTrick()
        {
            var combat_expertise = library.Get<BlueprintFeature>("4c44724ffa8844f4d9bedb5bb27d144a");
            var dirty_trick = library.Get<BlueprintFeature>("ed699d64870044b43bb5a7fbe3f29494");
            quick_dirty_trick = CallOfTheWild.Helpers.CreateFeature("QuickDirtyTrickFeature",
                                                                    "Quick Dirty Trick",
                                                                    "On your turn, you can perform a single dirty trick combat maneuver in place of one of your melee attacks. You must choose the melee attack with the highest base attack bonus to make the dirty trick combat maneuver.",
                                                                    "",
                                                                    dirty_trick.Icon,
                                                                    FeatureGroup.Feat,
                                                                    CallOfTheWild.Helpers.PrerequisiteStatValue(StatType.BaseAttackBonus, 6),
                                                                    CallOfTheWild.Helpers.PrerequisiteStatValue(StatType.Intelligence, 13),
                                                                    CallOfTheWild.Helpers.PrerequisiteFeature(combat_expertise),
                                                                    CallOfTheWild.Helpers.PrerequisiteFeature(dirty_trick)
                                                                    );
            quick_dirty_trick.Groups = quick_dirty_trick.Groups.AddToArray(FeatureGroup.CombatFeat);
            library.AddCombatFeats(quick_dirty_trick);

            foreach (var f in dirty_trick.GetComponent<AddFacts>().Facts)
            {
                var ability = f as BlueprintAbility;
                var action = ability.GetComponent<AbilityEffectRunAction>().Actions;

                var buff = CallOfTheWild.Helpers.CreateBuff(ability.name + "ToggleBuff",
                                                            ability.Name + " (First Attack Replacement)",
                                                            $"When performing full attack action, if you can make more than one attack, you can replace first attack with {ability.Name} combat maneuver.",
                                                            "",
                                                            ability.Icon,
                                                            null,
                                                            CallOfTheWild.Helpers.Create<CallOfTheWild.AttackReplacementMechanics.ReplaceAttackWithActionOnFullAttack>(r => r.action = action)
                                                            );

                var toggle = CallOfTheWild.Helpers.CreateActivatableAbility(ability.name + "ToggleAbility",
                                                                            buff.Name,
                                                                            buff.Description,
                                                                            "",
                                                                            buff.Icon,
                                                                            buff,
                                                                            AbilityActivationType.Immediately,
                                                                            Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Free,
                                                                            null);
                toggle.Group = CallOfTheWild.ActivatableAbilityGroupExtension.AttackReplacement.ToActivatableAbilityGroup();
                toggle.DeactivateImmediately = true;

                quick_dirty_trick.AddComponent(CallOfTheWild.Helpers.CreateAddFact(toggle));
            }
        }


        static void fixBuffsCover()
        {
            //do not apply cover for certain melee attacks which might be initiated by game as attacks from range, but which are actually not
            var no_cover = CallOfTheWild.Helpers.Create<CoverSpecial.IgnoreCoverForAttackType>(i => i.allowed_types = new AttackType[] { AttackType.Melee, AttackType.Touch });
            CallOfTheWild.NewSpells.bladed_dash.AddComponent(no_cover);
            var charge_buff = library.Get<BlueprintBuff>("f36da144a379d534cad8e21667079066");
            charge_buff.AddComponent(no_cover);
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

            var apply_buff = Common.createContextActionApplyBuff(buff, CallOfTheWild.Helpers.CreateContextDuration(1), dispellable: false);
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
            wild_flanking.AddComponents(CallOfTheWild.Helpers.CreateAddFact(ability),
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


        static void createSwordplayStyle()
        {
            var combat_expertise_buff = library.Get<BlueprintBuff>("e81cd772a7311554090e413ea28ceea1");
            var fight_defensively_buff = library.Get<BlueprintBuff>("6ffd93355fb3bcf4592a5d976b1d32a9");

            var combat_expertise = library.Get<BlueprintFeature>("4c44724ffa8844f4d9bedb5bb27d144a");
            var icon = CallOfTheWild.LoadIcons.Image2Sprite.Create(@"FeatIcons/SwordplayStyle.png");
            var buff = CallOfTheWild.Helpers.CreateBuff("SwordplayStyleEffectBuff",
                                                        "Swordplay style",
                                                        "While using this style, wielding  a weapon from heavy or light blades fighter weapon group, and fighting defensively or using either the total defense action or the Combat Expertise feat, you gain a +1 shield bonus to your Armor Class. In addition, you do not take the penalty on melee attacks from Combat Expertise on the first attack roll you make each turn. You still take the penalty on additional attacks, including attacks of opportunity.",
                                                        "",
                                                        icon,
                                                        null,
                                                        CallOfTheWild.Helpers.Create<CallOfTheWild.NewMechanics.ACBonusIfHasFacts>(a =>
                                                                                                                                    {
                                                                                                                                        a.Bonus = 1;
                                                                                                                                        a.Descriptor = ModifierDescriptor.Shield;
                                                                                                                                        a.CheckedFacts = new Kingmaker.Blueprints.Facts.BlueprintUnitFact[] { fight_defensively_buff, combat_expertise_buff };
                                                                                                                                    }
                                                                                                                                    ),
                                                        CallOfTheWild.Helpers.Create<CallOfTheWild.NewMechanics.AttackBonusOnAttackInitiationIfHasFact>(a =>
                                                                                                                                                       {
                                                                                                                                                           a.CheckedFact = combat_expertise_buff;
                                                                                                                                                           a.Bonus = CallOfTheWild.Helpers.CreateContextValue(AbilityRankType.Default);
                                                                                                                                                           a.OnlyFirstAttack = true;
                                                                                                                                                           a.WeaponAttackTypes = new AttackType[] { AttackType.Melee };
                                                                                                                                                       }
                                                                                                                                                       ),
                                                        CallOfTheWild.Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.CustomProperty,
                                                                                                      progression: ContextRankProgression.AsIs, stepLevel: 1,
                                                                                                      customProperty: library.Get<BlueprintUnitProperty>("8a63b06d20838954e97eb444f805ec89")) //combat expertise custom property
                                                       );

            WeaponCategory[] categories = new WeaponCategory[] {WeaponCategory.BastardSword, WeaponCategory.Dagger, WeaponCategory.DoubleSword, WeaponCategory.DuelingSword, WeaponCategory.ElvenCurvedBlade,
                                                                WeaponCategory.Estoc, WeaponCategory.Falcata, WeaponCategory.Falchion, WeaponCategory.Kama, WeaponCategory.Kukri,
                                                                WeaponCategory.Longsword, WeaponCategory.Rapier, WeaponCategory.Sai, WeaponCategory.Scimitar, WeaponCategory.Shortsword,
                                                               WeaponCategory.Starknife, WeaponCategory.Scythe, WeaponCategory.Sickle};

            swordplay_style_ability = CallOfTheWild.Helpers.CreateActivatableAbility("SwordplayStyleActivatableAbility",
                                                                                     buff.Name,
                                                                                     buff.Description,
                                                                                     "",
                                                                                     buff.Icon,
                                                                                     buff,
                                                                                     AbilityActivationType.Immediately,
                                                                                     Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Free,
                                                                                     null,
                                                                                     CallOfTheWild.Helpers.Create<CallOfTheWild.NewMechanics.ActivatableAbilityMainWeaponCategoryAllowed>(a => a.categories = categories)
                                                                                     );
            swordplay_style_ability.Group = ActivatableAbilityGroup.CombatStyle;

            swordplay_style = Common.ActivatableAbilityToFeature(swordplay_style_ability, false);
            swordplay_style.Groups = new FeatureGroup[] { FeatureGroup.Feat, FeatureGroup.CombatFeat, FeatureGroup.StyleFeat };
            swordplay_style.AddComponents(CallOfTheWild.Helpers.PrerequisiteFeature(combat_expertise),
                                          CallOfTheWild.Helpers.PrerequisiteStatValue(StatType.BaseAttackBonus, 3)
                                          );

            var weapon_focus = library.Get<BlueprintParametrizedFeature>("1e1f627d26ad36f43bbd26cc2bf8ac7e");
            foreach (var c in categories)
            {
                swordplay_style.AddComponent(CallOfTheWild.Common.createPrerequisiteParametrizedFeatureWeapon(weapon_focus, c, any: true));
            }
            
            swordplay_upset = CallOfTheWild.Helpers.CreateFeature("SwordplayUpsetFeature",
                                                                  "Swordplay Upset",
                                                                  "While using Swordplay Style, you can attempt a feint against an opponent that makes a melee attack against you and misses.",
                                                                  "",
                                                                  CallOfTheWild.LoadIcons.Image2Sprite.Create(@"FeatIcons/SwordplayUpset.png"),
                                                                  FeatureGroup.Feat,
                                                                  CallOfTheWild.Helpers.PrerequisiteStatValue(StatType.BaseAttackBonus, 5),
                                                                  CallOfTheWild.Helpers.PrerequisiteFeature(swordplay_style),
                                                                  CallOfTheWild.Helpers.PrerequisiteFeature(improved_feint)
                                                                  );
            swordplay_upset.Groups = swordplay_upset.Groups.AddToArray(FeatureGroup.CombatFeat, FeatureGroup.StyleFeat);
            var feint_action = CallOfTheWild.Helpers.CreateConditional(Common.createContextConditionHasFacts(false, improved_feint_ability.GetComponent<AbilityTargetHasFact>().CheckedFacts),
                                                                       null,
                                                                       improved_feint_ability.GetComponent<AbilityEffectRunAction>().Actions.Actions
                                                                      );

            buff.AddComponent(CallOfTheWild.Helpers.Create<CallOfTheWild.NewMechanics.ActionOnNearMissIfHasFact>(a =>
                                                                                                                {
                                                                                                                    a.checked_fact = swordplay_upset;
                                                                                                                    a.Action = CallOfTheWild.Helpers.CreateActionList(feint_action);
                                                                                                                    a.HitAndArmorDifference = 1000;
                                                                                                                    a.MeleeOnly = true;
                                                                                                                    a.OnAttacker = true;
                                                                                                                }
                                                                                                                )
                            );
            library.AddCombatFeats(swordplay_style, swordplay_upset);
        }


        static void createFeintFeats()
        {
            var combat_expertise = library.Get<BlueprintFeature>("4c44724ffa8844f4d9bedb5bb27d144a");
            ranged_feint = CallOfTheWild.Helpers.CreateFeature("RangedFeintFeature",
                                                               "Ranged Feint",
                                                               "You can feint with a ranged weapon by throwing a thrown weapon or firing one arrow, bolt, bullet, or other piece of ammunition; this feint takes the same action as normal to feint, but depending on your weapon, you might have to reload or draw another weapon afterward. When you successfully use a ranged feint, you deny that enemy its Dexterity bonus to AC against your ranged attacks as well as your melee attacks for the same duration as normal. If your feints normally deny a foe its Dexterity bonus to AC against attacks other than your own, this applies only against others’ melee attacks.",
                                                               "",
                                                               null,
                                                               FeatureGroup.Feat,
                                                               CallOfTheWild.Helpers.PrerequisiteStatValue(StatType.BaseAttackBonus, 2),
                                                               CallOfTheWild.Helpers.PrerequisiteStatValue(StatType.SkillPersuasion, 2)
                                                               );
            ranged_feint.Groups = ranged_feint.Groups.AddToArray(FeatureGroup.CombatFeat);

            greater_feint = CallOfTheWild.Helpers.CreateFeature("GreaterFeintFeature",
                                                               "Greater Feint",
                                                               "Whenever you use feint to cause an opponent to lose his Dexterity bonus, he loses that bonus until the beginning of your next turn, in addition to losing his Dexterity bonus against your next attack.",
                                                               "",
                                                               null,
                                                               FeatureGroup.Feat,
                                                               CallOfTheWild.Helpers.PrerequisiteStatValue(StatType.BaseAttackBonus, 6),
                                                               CallOfTheWild.Helpers.PrerequisiteStatValue(StatType.Intelligence, 13),
                                                               CallOfTheWild.Helpers.PrerequisiteFeature(combat_expertise)
                                                               );
            greater_feint.Groups = greater_feint.Groups.AddToArray(FeatureGroup.CombatFeat);

            var buff = CallOfTheWild.Helpers.CreateBuff("FeintBuff",
                                                        "Feint",
                                                        "Your opponent is consedered flat-footed against your next melee attack.",
                                                        "",
                                                        CallOfTheWild.LoadIcons.Image2Sprite.Create(@"AbilityIcons/Feint.png"),
                                                        null,
                                                        CallOfTheWild.Helpers.Create<NewMechanics.FlatFootedAgainstCaster>(f => { f.remove_after_attack = true; f.ranged_allowed_fact = ranged_feint; })
                                                        );

            var greater_buff = CallOfTheWild.Helpers.CreateBuff("GreaterFeintBuff",
                                            "Greater Feint",
                                            "Your opponent is consedered flat-footed against melee attacks.",
                                            "",
                                            CallOfTheWild.LoadIcons.Image2Sprite.Create(@"AbilityIcons/Feint.png"),
                                            null,
                                            CallOfTheWild.Helpers.Create<NewMechanics.FlatFootedAgainstAttacType>(f => f.allowed_attack_types = new AttackType[] { AttackType.Melee, AttackType.Touch }),
                                            CallOfTheWild.Helpers.Create<NewMechanics.FlatFootedAgainstCaster>(f => { f.remove_after_attack = false; f.ranged_allowed_fact = ranged_feint; })
                                            );

            var action = CallOfTheWild.Helpers.CreateConditional(CallOfTheWild.Common.createContextConditionCasterHasFact(greater_feint),
                                                                CallOfTheWild.Common.createContextActionApplyBuff(greater_buff, CallOfTheWild.Helpers.CreateContextDuration(), dispellable: false, duration_seconds: 6),
                                                                CallOfTheWild.Common.createContextActionApplyBuff(buff, CallOfTheWild.Helpers.CreateContextDuration(), dispellable: false, duration_seconds: 6)
                                                                );
            var action_list = CallOfTheWild.Helpers.CreateActionList(action);

            var vermin = library.Get<BlueprintFeature>("09478937695300944a179530664e42ec");
            var construct = library.Get<BlueprintFeature>("fd389783027d63343b4a5634bd81645f");
            var aberration = library.Get<BlueprintFeature>("3bec99efd9a363242a6c8d9957b75e91");
            var plant = library.Get<BlueprintFeature>("706e61781d692a042b35941f14bc41c5");
            improved_feint_ability = CallOfTheWild.Helpers.CreateAbility("ImprovedFeintAbility",
                                                                         "Feint",
                                                                         "You can feint as a move action. To feint, make a Bluff skill check. The DC of this check is equal to 10 + your opponent’s base attack bonus + your opponent’s Wisdom modifier. If successful, the next melee attack you make against the target does not allow him to use his Dexterity bonus to AC (if any). This attack must be made on or before your next turn.\n"
                                                                         + "When feinting against a non - humanoid DC increases by 4. Against a creature of animal Intelligence (1 or 2), by 8. Against a creature lacking an Intelligence score, it’s impossible. Feinting in combat does not provoke attacks of opportunity.",
                                                                         "",
                                                                         buff.Icon,
                                                                         AbilityType.Special,
                                                                         Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Move,
                                                                         AbilityRange.Weapon,
                                                                         "Your next attack or until end of your next turn",
                                                                         "",
                                                                         CallOfTheWild.Helpers.CreateRunActions(CallOfTheWild.Helpers.Create<NewMechanics.ContextFeintSkillCheck>(c => c.Success = action_list)),
                                                                         CallOfTheWild.Helpers.Create<NewMechanics.AbilityCasterMainWeaponIsMeleeUnlessHasFact>(a => a.ranged_allowed_fact = ranged_feint),
                                                                         CallOfTheWild.Common.createAbilityTargetHasFact(true, vermin, construct, aberration, plant)
                                                                         );
            improved_feint_ability.setMiscAbilityParametersSingleTargetRangedHarmful(works_on_allies: true,
                                                                                     animation: Kingmaker.Visual.Animation.Kingmaker.Actions.UnitAnimationActionCastSpell.CastAnimationStyle.Special);
            
            improved_feint = CallOfTheWild.Helpers.CreateFeature("ImprovedFeintFeature",
                                                   "Improved Feint",
                                                   improved_feint_ability.Description,
                                                   "",
                                                   null,
                                                   FeatureGroup.Feat,
                                                   CallOfTheWild.Helpers.CreateAddFact(improved_feint_ability),
                                                   CallOfTheWild.Helpers.PrerequisiteStatValue(StatType.Intelligence, 13),
                                                   CallOfTheWild.Helpers.PrerequisiteFeature(combat_expertise)
                                                   );

            improved_feint.Groups = improved_feint.Groups.AddToArray(FeatureGroup.CombatFeat);

            greater_feint.AddComponent(improved_feint.PrerequisiteFeature());
            ranged_feint.AddComponent(improved_feint.PrerequisiteFeature());

            library.AddCombatFeats(improved_feint, greater_feint, ranged_feint);

            //two weapon feint
            var two_weapon_fighting = library.Get<BlueprintFeature>("ac8aaf29054f5b74eb18f2af950e752d");
            var improved_two_weapon_fighting = library.Get<BlueprintFeature>("9af88f3ed8a017b45a6837eab7437629");
            improved_two_weapon_feint = CallOfTheWild.Helpers.CreateFeature("ImprovedTwoWeaponFeintFeature",
                                                   "Improved Two-Weapon Feint",
                                                   "While using Two-Weapon Fighting to make melee attacks, you can forgo your first primary-hand melee attack to make a Bluff check to feint an opponent. If you successfully feint, that opponent is denied his Dexterity bonus to AC until the end of your turn.",
                                                   "",
                                                   CallOfTheWild.LoadIcons.Image2Sprite.Create(@"FeatIcons/TwoWeaponFeintImproved.png"),
                                                   FeatureGroup.Feat,
                                                   CallOfTheWild.Helpers.PrerequisiteStatValue(StatType.Dexterity, 15),
                                                   CallOfTheWild.Helpers.PrerequisiteStatValue(StatType.Intelligence, 13),
                                                   CallOfTheWild.Helpers.PrerequisiteFeature(two_weapon_fighting),
                                                   CallOfTheWild.Helpers.PrerequisiteFeature(improved_two_weapon_fighting),
                                                   CallOfTheWild.Helpers.PrerequisiteFeature(combat_expertise)
                                                   );
            improved_two_weapon_feint.Groups = improved_two_weapon_feint.Groups.AddToArray(FeatureGroup.CombatFeat);

            var twf_action = CallOfTheWild.Helpers.CreateConditional(CallOfTheWild.Helpers.CreateConditionsCheckerOr( CallOfTheWild.Common.createContextConditionCasterHasFact(greater_feint), CallOfTheWild.Common.createContextConditionCasterHasFact(improved_two_weapon_feint)),
                                                                    CallOfTheWild.Common.createContextActionApplyBuff(greater_buff, CallOfTheWild.Helpers.CreateContextDuration(), dispellable: false, duration_seconds: 6),
                                                                    CallOfTheWild.Common.createContextActionApplyBuff(buff, CallOfTheWild.Helpers.CreateContextDuration(), dispellable: false, duration_seconds: 6)
                                                                    );
            var twf_feint_action = CallOfTheWild.Helpers.Create<NewMechanics.ContextFeintSkillCheck>(c => c.Success = CallOfTheWild.Helpers.CreateActionList(twf_action));
            var twf_feint_action_list = CallOfTheWild.Helpers.CreateActionList(twf_feint_action);
            var twf_feint_buff = CallOfTheWild.Helpers.CreateBuff("TwoWeaponFeintBuff",
                                                                  "Two Weapon Feint",
                                                                  "While using Two-Weapon Fighting to make melee attacks, you can forgo your first primary-hand melee attack to make a Bluff check to feint an opponent.",
                                                                  "",
                                                                  CallOfTheWild.LoadIcons.Image2Sprite.Create(@"FeatIcons/TwoWeaponFeint.png"),
                                                                  null,
                                                                  CallOfTheWild.Helpers.Create<CallOfTheWild.AttackReplacementMechanics.ReplaceAttackWithActionOnFullAttack>(f => f.action = twf_feint_action_list)
                                                                  );

            var twf_feint_ability = CallOfTheWild.Helpers.CreateActivatableAbility("TwoWeaponFeintActivatableAbility",
                                                                                   twf_feint_buff.Name,
                                                                                   twf_feint_buff.Description,
                                                                                   "",
                                                                                   twf_feint_buff.Icon,
                                                                                   twf_feint_buff,
                                                                                   AbilityActivationType.Immediately,
                                                                                   Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Free,
                                                                                   null,
                                                                                   CallOfTheWild.Helpers.Create<CallOfTheWild.NewMechanics.TwoWeaponFightingRestriction>());
            twf_feint_ability.DeactivateImmediately = true;
            twf_feint_ability.Group = CallOfTheWild.ActivatableAbilityGroupExtension.AttackReplacement.ToActivatableAbilityGroup();

            two_weapon_feint = CallOfTheWild.Common.ActivatableAbilityToFeature(twf_feint_ability, false);
            two_weapon_feint.AddComponents(CallOfTheWild.Helpers.PrerequisiteStatValue(StatType.Dexterity, 15),
                                           CallOfTheWild.Helpers.PrerequisiteStatValue(StatType.Intelligence, 13),
                                           CallOfTheWild.Helpers.PrerequisiteFeature(two_weapon_fighting),
                                           CallOfTheWild.Helpers.PrerequisiteFeature(combat_expertise)
                                           );
            two_weapon_feint.Groups = new FeatureGroup[] { FeatureGroup.Feat, FeatureGroup.CombatFeat };

            improved_two_weapon_feint.AddComponent(CallOfTheWild.Helpers.PrerequisiteFeature(two_weapon_feint));
            library.AddCombatFeats(two_weapon_feint, improved_two_weapon_feint);
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
                                                                    CallOfTheWild.Helpers.Create<NewMechanics.PrerequisiteFeatFromGroup>(f => f.group = FeatureGroup.TeamworkFeat)
                                                                );

            enfilading_fire.Groups = enfilading_fire.Groups.AddToArray(FeatureGroup.CombatFeat, FeatureGroup.TeamworkFeat);
            library.AddCombatFeats(enfilading_fire);
            CallOfTheWild.Common.addTemworkFeats(enfilading_fire);
        }




    }
}
