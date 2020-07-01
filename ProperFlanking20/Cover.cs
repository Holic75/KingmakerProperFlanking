using CallOfTheWild;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Controllers.Combat;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProperFlanking20
{
    [Harmony12.HarmonyPatch(typeof(RuleCalculateAC))]
    [Harmony12.HarmonyPatch("OnTrigger", Harmony12.MethodType.Normal)]
    class RuleCalculateAC__OnTrigger__CoverFix
    {
        static bool Prefix(RuleCalculateAC __instance, RulebookEventContext context)
        {
            bool veering = false;
            var weapon = __instance.Reason?.Item as ItemEntityWeapon;
            if (weapon != null && weapon.EnchantmentsCollection != null)
            {
                veering = weapon.EnchantmentsCollection.HasFact(Cover.veering);
            }
            
            var current_cover = __instance.Target.hasCoverFrom(__instance.Initiator, __instance.AttackType, weapon);
            if (current_cover.isFull())
            {
                __instance.AddBonus(Cover.cover_ac_bonus, Cover.soft_cover_fact);
                if (veering)
                {
                    __instance.AddBonus(Cover.partial_cover_ac_bonus - Cover.cover_ac_bonus, Cover.veering_fact);
                }

            }
            else if (current_cover.isPartial())
            {
                __instance.AddBonus(Cover.partial_cover_ac_bonus, Cover.partial_soft_cover_fact);
                if (veering)
                {
                    __instance.AddBonus(-Cover.partial_cover_ac_bonus, Cover.veering_fact);
                }
            }

            return true;
        }
    }



    [Harmony12.HarmonyPatch(typeof(UnitCombatState))]
    [Harmony12.HarmonyPatch("AttackOfOpportunity", Harmony12.MethodType.Normal)]
    class UnitCombatState__AttackOfOpportunity__Patch
    {
        static bool Prefix(UnitCombatState __instance, UnitEntityData target, ref bool __result)
        {//prevent attack of opportunity if target has cover
            var weapon = __instance?.Unit?.GetFirstWeapon();
            AttackType attack_type = weapon == null ? AttackType.Melee : weapon.Blueprint.AttackType;
            if (target.hasCoverFrom(__instance.Unit, __instance.Unit.GetFirstWeapon().Blueprint.AttackType, weapon) != Cover.CoverType.None)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    static public class Cover
    {
        [Flags]
        public enum CoverType
        {
            None = 0,
            Low = 1,
            Left = 2,
            Right = 4,
            Center = 8,
            High = 16,
            Full = Right | Left | Center,
            NoCover = Low | High
        }

        public static bool isFull(this CoverType this_type)
        {
            return (~this_type & CoverType.Full) == 0;
        }

        public static bool isNone(this CoverType this_type)
        {
            return (this_type | CoverType.NoCover) == CoverType.NoCover;
        }

        public static bool isPartial(this CoverType this_type)
        {
            return !this_type.isFull() && !this_type.isNone();
        }

        static internal LibraryScriptableObject library = Main.library;
        static internal Fact soft_cover_fact;
        static internal Fact veering_fact;
        static internal Fact partial_soft_cover_fact;
        static public BlueprintWeaponEnchantment veering;

        static internal int cover_ac_bonus;
        static internal int partial_cover_ac_bonus;

        static internal void load(int bonus, int partial_bonus)
        {
            cover_ac_bonus = Math.Max(0, bonus);
            partial_cover_ac_bonus = Math.Max(0, partial_bonus);
            createSoftCoverFact();

            createVeeringEnchant();
        }


        static void createVeeringEnchant()
        {
            veering = Common.createWeaponEnchantment("VeeringWeaponEnchantment", "Veering",
                                                    "Veering weapons feature feathers or carved images of wings or winged creatures in their construction. Attacks with a veering weapon ignore a target’s bonus to AC from partial cover and reduce the target’s bonus to AC from full soft cover to that of partial. A veering weapon bestows no benefit against targets with total cover.",
                                                    "Veering", "", "8d1096d3c259455cb438c6abb333dd8b", 3, 1, null);

            var weapon_guids = new string[]
            {
                "05550fe34742a344c968839ba9e5284a", //fiery eye
                "ad5cbd0bec32b6042abb0a0e82a43925", //shady bow
                "1f546ab76bb0e77478ad08248795f7d7", //whirlwind
                "4636ef8b8a64b3941ac5dda42918d765", //lucky longbow
                "2f771b62ffb4bdf45a425ba0a0130217", //longbow of erastil
                "b6c71cc97303b0f4f9f0a28e14bbb66e", //eye of the tornado
                "051ccf83137987847aade5287788bf9c", //prowling cheetah
                "007d72299a0c85743bccd47fb5ed6bdc", //hunter's blessing
                "137fc6acf3f645a40a3ec041b472ba86", //planar hunter
                "84094c6a1e5ba694f9b1b6492d6fcdf3", //greater sting
                "d2127cb56fa4c4c43a6af226ffb21ac1", //ankle breaker
                "a7712968bf669534799ba0c6639543af", //savage bow
            };

            foreach (var wg in weapon_guids)
            {
                var weapon = library.Get<BlueprintItemWeapon>(wg);
                Common.addEnchantment(weapon, veering);
            }


            var veering_unit_fact = CallOfTheWild.Helpers.Create<BlueprintUnitFact>();

            veering_unit_fact.name = "VeeringFact";
            veering_unit_fact.SetName("Veering");
            veering_unit_fact.SetDescription("");
            library.AddAsset(veering_unit_fact, "721e98b30521407ba72564f16f031e3a");
            veering_fact = new Fact(veering_unit_fact);
        }

        static void createSoftCoverFact()
        {
            var soft_cover_unit_fact = CallOfTheWild.Helpers.Create<BlueprintUnitFact>();

            soft_cover_unit_fact.name = "SoftCoverFact";
            soft_cover_unit_fact.SetName("Soft Cover");
            soft_cover_unit_fact.SetDescription("");
            library.AddAsset(soft_cover_unit_fact, "31a5d72594eb4b49aaad3b86c88f84cb");
            soft_cover_fact = new Fact(soft_cover_unit_fact);

            var partial_soft_cover_unit_fact = CallOfTheWild.Helpers.Create<BlueprintUnitFact>();

            partial_soft_cover_unit_fact.name = "PartialSoftCoverFact";
            partial_soft_cover_unit_fact.SetName("Partial Soft Cover");
            partial_soft_cover_unit_fact.SetDescription("");
            library.AddAsset(partial_soft_cover_unit_fact, "7e1f43698239476ba573054aa200bd94");
            partial_soft_cover_fact = new Fact(partial_soft_cover_unit_fact);
        }

        public class UnitPartNoCover: AdditiveUnitPart
        {
            public bool hasBuff(BlueprintFact blueprint)
            {
                return buffs.Any(b => b.Blueprint == blueprint);
            }


            public bool doesNotProvideCoverToFrom(UnitEntityData target, UnitEntityData attacker, AttackType attack_type)
            {
                foreach (var b in buffs)
                {
                    if (b.Blueprint.GetComponent<SpecialProvideNoCover>() != null)
                    {
                        bool result = false;
                        b.CallComponents<SpecialProvideNoCover>(a => { result = a.doesNotProvideCoverToFrom(target, attacker, attack_type); });
                        if (result)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }


        public class UnitPartIgnoreCoverFromOneUnit : CallOfTheWild.AdditiveUnitPart
        {
            public bool active(ItemEntityWeapon weapon)
            {
                foreach (var b in buffs)
                {
                    if (b.Blueprint.GetComponent<IgnoreCoverFromOneUnitBase>() != null)
                    {
                        bool result = false;
                        b.CallComponents<IgnoreCoverFromOneUnitBase>(a => { result = a.ignoresCover(weapon); });
                        if (result)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public class UnitPartIgnoreCover : CallOfTheWild.AdditiveUnitPart
        {
            public bool hasBuff(BlueprintFact blueprint)
            {
                return buffs.Any(b => b.Blueprint == blueprint);
            }


            public bool ignoresCover(UnitEntityData target, UnitEntityData cover, AttackType attack_type)
            {
                foreach (var b in buffs)
                {
                    if (b.Blueprint.GetComponent<SpecialIgnoreCover>() != null)
                    {
                        bool result = false;
                        b.CallComponents<SpecialIgnoreCover>(a => { result = a.ignoresCover(target, cover, attack_type); });
                        if (result)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }


        public abstract class SpecialProvideNoCover : OwnedGameLogicComponent<UnitDescriptor>
        {
            public override void OnTurnOn()
            {
                this.Owner.Ensure<UnitPartNoCover>().addBuff(this.Fact);
            }

            public override void OnTurnOff()
            {
                this.Owner.Ensure<UnitPartNoCover>().removeBuff(this.Fact);
            }

            abstract public bool doesNotProvideCoverToFrom(UnitEntityData target, UnitEntityData attacker, AttackType attack_type);
        }


        public abstract class IgnoreCoverFromOneUnitBase : OwnedGameLogicComponent<UnitDescriptor>
        {
            public override void OnTurnOn()
            {
                this.Owner.Ensure<UnitPartIgnoreCoverFromOneUnit>().addBuff(this.Fact);
            }

            public override void OnTurnOff()
            {
                this.Owner.Ensure<UnitPartIgnoreCoverFromOneUnit>().removeBuff(this.Fact);
            }

            abstract public bool ignoresCover(ItemEntityWeapon weapon);
        }


        public abstract class SpecialIgnoreCover : OwnedGameLogicComponent<UnitDescriptor>
        {
            public override void OnTurnOn()
            {
                this.Owner.Ensure<UnitPartIgnoreCover>().addBuff(this.Fact);
            }

            public override void OnTurnOff()
            {
                this.Owner.Ensure<UnitPartIgnoreCover>().removeBuff(this.Fact);
            }

            abstract public bool ignoresCover(UnitEntityData target, UnitEntityData cover, AttackType attack_type);
        }

        static internal CoverType hasCoverFrom(this UnitEntityData unit, UnitEntityData attacker, AttackType attack_type, ItemEntityWeapon weapon)
        {
            if (unit == null || attacker == null || unit == attacker)
            {
                return CoverType.None;
            }
            var current_cover = CoverType.None;

            var c = (unit.Position + attacker.Position) / 2.0f;
            var r = (unit.Position - attacker.Position) / 2.0f;
            if (attack_type == AttackType.Melee || attack_type == AttackType.Touch)
            {
                //account for units inside circle
                var norm_r = r.normalized;
                var unit_radius = unit?.View?.Corpulence;// Helpers.unitSizeToDiameter(unit.Descriptor.State.Size).Feet().Meters / 2.0f;
                r = r - norm_r * (unit_radius.HasValue ? unit_radius.Value :  0.25f);
                //c = c - norm_r * unit_radius;
            }

            float radius = (float)Math.Sqrt(Vector3.Dot(r, r));
            var units_around = GameHelper.GetTargetsAround(c, radius, false);

            var unit_position = unit.Position;
            var attacker_position = attacker.Position;

            int cover_providers = 0;
            var ignore_cover_from_one_unit_part = attacker.Get<UnitPartIgnoreCoverFromOneUnit>();
            bool ignore_cover_from_one_unit = ignore_cover_from_one_unit_part != null ? ignore_cover_from_one_unit_part.active(weapon) : false;
            foreach (var u in units_around)
            {
                if (u == null)
                {
                    continue;
                }
                var u_cover = u.providesCoverToFrom(unit, attacker, attack_type);
                if (!u_cover.isNone())
                {
                    cover_providers++;
                }
                current_cover = current_cover | u_cover; //sum covers
                if (current_cover.isFull() && !(ignore_cover_from_one_unit && cover_providers <= 1))
                { //full cover is maximum possible cover
                    return current_cover;
                }
            }
            return (ignore_cover_from_one_unit && cover_providers <= 1) ? CoverType.None : current_cover;
        }

        static internal CoverType providesCoverToFrom(this UnitEntityData cover, UnitEntityData unit, UnitEntityData attacker, AttackType attack_type)
        {
            if (cover == null || unit == null || cover == unit || cover == attacker)
            {
                return CoverType.None;
            }
            if (cover.Ensure<UnitPartNoCover>().doesNotProvideCoverToFrom(unit, attacker, attack_type))
            {//check if special cases
                return CoverType.None;
            }
            if (attacker.Ensure<UnitPartIgnoreCover>().ignoresCover(unit, cover, attack_type))
            {//check if special cases
                return CoverType.None;
            }
            else if ((int)(unit.Descriptor.State.Size - cover.Descriptor.State.Size) >= 2)
            {//if unit is at least two size categories smaller than target it does not provide cover
                return CoverType.None;
            }
            else if (cover.Descriptor.State.Prone.Active || cover.Descriptor.State.IsDead || cover.Descriptor.State.IsUnconscious || cover.Descriptor.State.HasCondition(UnitCondition.Sleeping))
            {//units lying on the ground do not provide cover
                return CoverType.None;
            }
            else 
            {
                Cover.CoverType current_cover = CoverType.High;
                if ((int)(attacker.Descriptor.State.Size - cover.Descriptor.State.Size) >= 1)
                {
                    if (unit.DistanceTo(cover) > (Math.Max(Helpers.unitSizeToDiameter(unit.Descriptor.State.Size), Helpers.unitSizeToDiameter(cover.Descriptor.State.Size)).Feet().Meters / 2.0f + 20.Feet().Meters))
                    {
                        //if unit is at least at 20 ft from attacker (in pnp 30 ft but ranges are lower in crpg) it does not provide cover [low obstacle]
                        return CoverType.None;
                    }
                    else if (unit.DistanceTo(cover) > attacker.DistanceTo(cover))
                    {//if unit is closer to attacker than target - it does not provide cover [low obstacle]
                        return CoverType.None;
                    }
                    else
                    {//possibly partial cover
                        current_cover = CoverType.Low;
                    }
                }
                //check geometry
                var cover_r = unit.View.Corpulence; //Helpers.unitSizeToDiameter(cover.Descriptor.State.Size).Feet().Meters / 2.0f * 0.9f;
                var unit_r = attacker.View.Corpulence; //assume the window required for unimepeded shooting is based on attacker size (i.e smaller units need less space) ?  //5.Feet().Meters / 2.0f; 
                var unit_center = unit.Position.To2D();
                if (Helpers.isCircleIntersectedByLine(cover.Position.To2D(), cover_r * cover_r, attacker.Position.To2D(), unit_center))
                {
                    current_cover = current_cover | CoverType.Center;
                }

                //check cover from left and right (if possible) to determine if cover is full or partial
                var n = (unit.Position - attacker.Position).To2D();
                if (n.magnitude <= 1e-5f)
                {
                    return current_cover;
                }

                n = new Vector2(-n.y, n.x).normalized;
                var unit_left = unit_center + unit_r * 0.75f * n;
                var unit_right = unit_center - unit_r * 0.75f * n;
                if (Helpers.isCircleIntersectedByLine(cover.Position.To2D(), cover_r * cover_r, attacker.Position.To2D(), unit_left))
                {
                    current_cover = current_cover | CoverType.Left;
                }
                if (Helpers.isCircleIntersectedByLine(cover.Position.To2D(), cover_r * cover_r, attacker.Position.To2D(), unit_right))
                {
                    current_cover = current_cover | CoverType.Right;
                }

                return current_cover;
            }
        }
    }
}



