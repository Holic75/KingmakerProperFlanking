using CallOfTheWild;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Controllers.Combat;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
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
            var current_cover = __instance.Target.hasCoverFrom(__instance.Initiator, __instance.AttackType);
            //Main.logger.Log(current_cover.ToString() + " "  + __instance.AttackType.ToString());
            if (current_cover.isFull())
            {
                __instance.AddBonus(Cover.cover_ac_bonus, Cover.soft_cover_fact);
            }
            else if (current_cover.isPartial())
            {
                __instance.AddBonus(Cover.partial_cover_ac_bonus, Cover.partial_soft_cover_fact);
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
            if (target.hasCoverFrom(__instance.Unit, __instance.Unit.GetFirstWeapon().Blueprint.AttackType) != Cover.CoverType.None)
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
        static internal Fact partial_soft_cover_fact;

        static internal int cover_ac_bonus;
        static internal int partial_cover_ac_bonus;

        static internal void load(int bonus, int partial_bonus)
        {
            cover_ac_bonus = Math.Max(0, bonus);
            partial_cover_ac_bonus = Math.Max(0, partial_bonus);
            Main.logger.Log($"Use cover with ac bonus: {cover_ac_bonus} / {partial_cover_ac_bonus}.");
            createSoftCoverFact();
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

        static internal CoverType hasCoverFrom(this UnitEntityData unit, UnitEntityData attacker, AttackType attack_type)
        {
            var current_cover = CoverType.None;

            var c = (unit.Position + attacker.Position) / 2.0f;
            var r = (unit.Position - attacker.Position) / 2.0f;
            if (attack_type == AttackType.Melee || attack_type == AttackType.Touch)
            {
                //account for units inside circle
                var norm_r = r.normalized;
                var unit_radius = unit.View.Corpulence;// Helpers.unitSizeToDiameter(unit.Descriptor.State.Size).Feet().Meters / 2.0f;
                r = r - norm_r * unit_radius;
                //c = c - norm_r * unit_radius;
            }

            float radius = (float)Math.Sqrt(Vector3.Dot(r, r));
            var units_around = GameHelper.GetTargetsAround(c, radius, true);

            var unit_position = unit.Position;
            var attacker_position = attacker.Position;

            foreach (var u in units_around)
            {
                current_cover = current_cover | u.providesCoverToFrom(unit, attacker, attack_type); //sum covers
                if (current_cover.isFull())
                { //full cover is maximum possible cover
                    return current_cover;
                }
            }
            return current_cover;
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



