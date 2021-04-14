using CallOfTheWild;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
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
        static public BlueprintFeature dirty_fighter;
        static public BlueprintFeature deadeye_bowman;
        static internal void load()
        {
            fixArrowSongMinstrel();
            fixSharpenedAccuracy();
            fixSpiritualWeapons();
            createTraits();
            fixBrawlerImprovedAwesomeBlow();
        }


        static void fixBrawlerImprovedAwesomeBlow()
        {
            var ability = Brawler.awesome_blow_ability;
            var action = ability.GetComponent<AbilityEffectRunAction>().Actions;
            var maneuver_type = (action.Actions[0] as ContextActionCombatManeuver).Type;
            var buff = CallOfTheWild.Helpers.CreateBuff(ability.name + "ToggleBuff",
                                                        ability.Name + " (Attack Replacement)",
                                                        $"When performing standard or full attack action, you can replace any attack with {ability.Name} combat maneuver.",
                                                        "",
                                                        ability.Icon,
                                                        null,
                                                        CallOfTheWild.Helpers.Create<ManeuverAsAttack.AttackReplacementWithCombatManeuver>(a => 
                                                                                                                {
                                                                                                                    a.maneuver = maneuver_type;
                                                                                                                    a.only_first_attack = false;
                                                                                                                    a.only_full_attack = false;
                                                                                                                    a.weapon_categories = new WeaponCategory[] {WeaponCategory.UnarmedStrike,
                                                                                                                                                                WeaponCategory.PunchingDagger,
                                                                                                                                                                WeaponCategory.SpikedHeavyShield,
                                                                                                                                                                WeaponCategory.SpikedLightShield,
                                                                                                                                                                WeaponCategory.WeaponLightShield,
                                                                                                                                                                WeaponCategory.WeaponHeavyShield
                                                                                                                                                               };
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

            Brawler.awesome_blow_improved.AddComponent(CallOfTheWild.Helpers.CreateAddFact(toggle));
            Brawler.awesome_blow_improved.SetDescription("At 20th level, the brawler can use her awesome blow ability as an attack rather than as a standard action. She may use it on creatures of any size.");

            var apply_buff = CallOfTheWild.Common.createContextActionApplyBuffToCaster(NewFeats.maneuver_as_attack_buff, CallOfTheWild.Helpers.CreateContextDuration(1), dispellable: false);
            var remove_buff = CallOfTheWild.Common.createContextActionOnContextCaster(CallOfTheWild.Common.createContextActionRemoveBuffFromCaster(NewFeats.maneuver_as_attack_buff));

            ability.ReplaceComponent<AbilityEffectRunAction>(a => a.Actions = CallOfTheWild.Helpers.CreateActionList(CallOfTheWild.Helpers.CreateConditional(CallOfTheWild.Common.createContextConditionCasterHasFact(Brawler.awesome_blow_improved),
                                                                                                                                                             apply_buff
                                                                                                                                                             ),
                                                                                                                     a.Actions.Actions[0],
                                                                                                                     remove_buff)
                                                                                                                     );
        }


        static void fixSpiritualWeapons()
        {
            // twilight dagger should alwasy flank if possible
            var twilight_dagger_feature = CallOfTheWild.SpiritualWeapons.twilight_knife_unit.AddFacts[0];
            twilight_dagger_feature.AddComponent(CallOfTheWild.Helpers.Create<FlankingSpecial.AlwaysFlanking>());

            //spiritual weapon and spiritual ally should ignore reach dead zone
            var spiritual_weapon_feature = CallOfTheWild.SpiritualWeapons.spiritual_weapon_unit.AddFacts[0];
            spiritual_weapon_feature.AddComponent(CallOfTheWild.Helpers.Create<ReachWeapons.IgnoreReachDeadZone>());
            var spiritual_ally_feature = CallOfTheWild.SpiritualWeapons.spiritual_ally_unit.AddFacts[0];
            spiritual_ally_feature.AddComponent(CallOfTheWild.Helpers.Create<ReachWeapons.IgnoreReachDeadZone>());
            
        }


        static void createTraits()
        {
            dirty_fighter = CallOfTheWild.Helpers.CreateFeature("DirtyFighterProperFlankingTrait",
                                     "Dirty Fighter",
                                     "You wouldn’t have lived to make it out of childhood without the aid of a sibling, friend, or companion you could always count on to distract your enemies long enough for you to do a little bit more damage than normal. That companion may be another PC or an NPC (who may even be recently departed from your side).\n" +
                                     "Benefit: When you hit a foe you are flanking, you deal 1 additional point of damage (this damage is added to your base damage, and is multiplied on a critical hit). This additional damage is a trait bonus.",
                                     "b7677db3aa6f457a82c438481c04b659",
                                     CallOfTheWild.Helpers.GetIcon("5662d1b793db90c4b9ba68037fd2a768"), // precise strike
                                     FeatureGroup.Trait,
                                     CallOfTheWild.Helpers.Create<NewMechanics.DamageBonusAgainstFlankedTarget>(d => d.bonus = 1)
                                     );


            deadeye_bowman = CallOfTheWild.Helpers.CreateFeature("DeadEyeBowmanTrait",
                                                                 "Deadeye Bowman",
                                                                 "When you are using a longbow, if only a single creature is providing soft cover, you can ignore it.",
                                                                 "98656c735106478c9944316c2b62fa54",
                                                                 CallOfTheWild.Helpers.GetIcon("8f3d1e6b4be006f4d896081f2f889665"), // precise shot
                                                                 FeatureGroup.Trait,
                                                                 CallOfTheWild.Helpers.Create<CoverSpecial.IgnoreCoverFromOneUnitWithWeaponCategory>(i => i.categories = new WeaponCategory[] { WeaponCategory.Longbow }),
                                                                 CallOfTheWild.Helpers.PrerequisiteFeature(library.Get<BlueprintFeature>("afc775188deb7a44aa4cbde03512c671")) //erastil
                                                                 );
        }


        static void fixSharpenedAccuracy()
        {
            CallOfTheWild.NewRagePowers.sharpened_accuracy_buff.AddComponent(CallOfTheWild.Helpers.Create<CoverSpecial.IgnoreCover>());
        }


        static void fixArrowSongMinstrel()
        {
            //no cover provided by those under effect of arrowsongm isntrel bardic performance
            var buffs = new BlueprintBuff[]
            {
                library.Get<BlueprintBuff>("6d6d9e06b76f5204a8b7856c78607d5d"), //courage
                library.Get<BlueprintBuff>("1fa5f733fa1d77743bf54f5f3da5a6b1"), //competence
                library.Get<BlueprintBuff>("ec38c2e60d738584983415cb8a4f508d"), //greatness
                library.Get<BlueprintBuff>("31e1f369cf0e4904887c96e4ef97a9cb"), //heroics
                CallOfTheWild.VersatilePerformance.blazing_rondo_buff,
                CallOfTheWild.VersatilePerformance.symphony_of_elysian_heart_buff
            };

            foreach (var b in buffs)
            {
                b.AddComponent(CallOfTheWild.Helpers.Create<CoverSpecial.NoCoverToCasterWithFact>(n =>
                                                                                                    {
                                                                                                        n.fact = CallOfTheWild.Archetypes.ArrowsongMinstrel.precise_minstrel;
                                                                                                        n.attack_types = new AttackType[] { AttackType.Ranged };
                                                                                                    }
                                                                                                 )
                              );
            }
        }
    }
}
