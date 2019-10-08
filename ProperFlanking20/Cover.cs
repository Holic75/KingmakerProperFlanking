using CallOfTheWild;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Controllers.Combat;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
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
    [Harmony12.HarmonyPatch(typeof(RuleCalculateAttackBonus))]
    [Harmony12.HarmonyPatch("OnTrigger", Harmony12.MethodType.Normal)]
    class RuleCalculateAttackBonus__OnTrigger__CoverFix
    {
        static bool Prefix(RuleCalculateAttackBonus __instance)
        {
            if (__instance.Target.hasCoverFrom(__instance.Initiator, __instance.Weapon))
            {
                __instance.AddBonus(Cover.cover_penalty, Cover.soft_cover_fact);
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
            if (target.hasCoverFrom(__instance.Unit, __instance.Unit.GetFirstWeapon()))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    static public class Cover
    {
        static internal LibraryScriptableObject library = Main.library;
        static internal Fact soft_cover_fact;

        static internal int cover_penalty;


        static internal void load(int penalty)
        {
            cover_penalty = Math.Min(0, penalty);
            Main.logger.Log($"Use cover with penalty: {cover_penalty}.");
            createSoftCoverFact();
        }

        static void createSoftCoverFact()
        {
            var soft_cover_unti_fact = CallOfTheWild.Helpers.Create<BlueprintUnitFact>();

            soft_cover_unti_fact.name = "SoftCoverFact";
            soft_cover_unti_fact.SetName("Soft Cover");
            soft_cover_unti_fact.SetDescription("");
            library.AddAsset(soft_cover_unti_fact, "31a5d72594eb4b49aaad3b86c88f84cb");
            soft_cover_fact = new Fact(soft_cover_unti_fact);
        }

        public class UnitPartNoCover: UnitPart
        {
            [JsonProperty]
            private List<Fact> buffs = new List<Fact>();

            public void addBuff(Fact buff)
            {
                buffs.Add(buff);
            }


            public void removeBuff(Fact buff)
            {
                buffs.Remove(buff);
            }


            public bool hasBuff(BlueprintFact blueprint)
            {
                return buffs.Any(b => b.Blueprint == blueprint);
            }


            public bool doesNotProvideCoverToFrom(UnitEntityData target, UnitEntityData attacker, ItemEntityWeapon weapon)
            {
                foreach (var b in buffs)
                {
                    if (b.Blueprint.GetComponent<SpecialProvideNoCover>() != null)
                    {
                        bool result = false;
                        b.CallComponents<SpecialProvideNoCover>(a => { result = a.doesNotProvideCoverToFrom(target, attacker, weapon); });
                        if (result)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }


        public class UnitParIgnoreCover : UnitPart
        {
            [JsonProperty]
            private List<Fact> buffs = new List<Fact>();

            public void addBuff(Fact buff)
            {
                buffs.Add(buff);
            }


            public void removeBuff(Fact buff)
            {
                buffs.Remove(buff);
            }


            public bool hasBuff(BlueprintFact blueprint)
            {
                return buffs.Any(b => b.Blueprint == blueprint);
            }


            public bool ignoresCover(ItemEntityWeapon weapon)
            {
                foreach (var b in buffs)
                {
                    if (b.Blueprint.GetComponent<SpecialIgnoreCover>() != null)
                    {
                        bool result = false;
                        b.CallComponents<SpecialIgnoreCover>(a => { result = a.ignoresCover(weapon); });
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
            public override void OnFactActivate()
            {
                this.Owner.Ensure<UnitPartNoCover>().addBuff(this.Fact);
            }

            public override void OnFactDeactivate()
            {
                this.Owner.Ensure<UnitPartNoCover>().removeBuff(this.Fact);
            }

            abstract public bool doesNotProvideCoverToFrom(UnitEntityData target, UnitEntityData attacker, ItemEntityWeapon weapon);
        }



        public abstract class SpecialIgnoreCover : OwnedGameLogicComponent<UnitDescriptor>
        {
            public override void OnFactActivate()
            {
                this.Owner.Ensure<UnitPartNoCover>().addBuff(this.Fact);
            }

            public override void OnFactDeactivate()
            {
                this.Owner.Ensure<UnitPartNoCover>().removeBuff(this.Fact);
            }

            abstract public bool ignoresCover(ItemEntityWeapon weapon);
        }

        static internal bool hasCoverFrom(this UnitEntityData unit, UnitEntityData attacker, ItemEntityWeapon weapon)
        {
            if (unit.Ensure<UnitParIgnoreCover>().ignoresCover(weapon))
            {
                return false;
            }
            var c = (unit.Position + attacker.Position) / 2.0f;
            var r = (unit.Position - attacker.Position) / 2.0f;
            //account for units inside circle
            var norm_r = r.normalized;
            var unit_radius = Helpers.unitSizeToDiameter(unit.Descriptor.State.Size).Feet().Meters / 2.0f;
            r = r - norm_r * unit_radius;
            c = c - norm_r * unit_radius;

            float radius = (float)Math.Sqrt(Vector3.Dot(r, r));
            var units_around = GameHelper.GetTargetsAround(c, radius, true);

            var unit_position = unit.Position;
            var attacker_position = attacker.Position;

            foreach (var u in units_around)
            {
                if (u.providesCoverToFrom(unit, attacker, weapon))
                {
#if DEBUG
                    Main.logger.Log($"{unit.CharacterName} has cover from {attacker.CharacterName} due to {u.CharacterName}");
#endif
                    return true;
                }
            }
            return false;
        }

        static internal bool providesCoverToFrom(this UnitEntityData cover, UnitEntityData unit, UnitEntityData attacker, ItemEntityWeapon weapon)
        {
            if (cover == null || unit == null || cover == unit || cover == attacker)
            {
                return false;
            }
            if (cover.Ensure<UnitPartNoCover>().doesNotProvideCoverToFrom(unit, attacker, weapon))
            {//check if special cases
                return false;
            }
            else if ((int)(unit.Descriptor.State.Size - cover.Descriptor.State.Size) >= 2)
            {//if unit is at least two size categories smaller than target it does not provide cover
                return false;
            }
            else if ((int)(attacker.Descriptor.State.Size - cover.Descriptor.State.Size) >= 2
                    && (unit.Descriptor.State.Size > cover.Descriptor.State.Size
                        || unit.DistanceTo(cover) > (Math.Max(Helpers.unitSizeToDiameter(unit.Descriptor.State.Size), Helpers.unitSizeToDiameter(cover.Descriptor.State.Size)).Feet().Meters /2.0f + 10.Feet().Meters))
                    )
            {//if unit is at least two categories smaller than attacker it does not provide cover if it is smaller than target or
             //if it is further from target than 10 ft
                return false;
            }
            else if (cover.Descriptor.State.Prone.Active || cover.Descriptor.State.IsDead || cover.Descriptor.State.IsUnconscious || cover.Descriptor.State.HasCondition(UnitCondition.Sleeping))
            {//units lying on the ground do not provide cover
                return false;
            }
            else 
            { //check geoemtry
                var ur = Helpers.unitSizeToDiameter(cover.Descriptor.State.Size).Feet().Meters / 2.0f * 0.9f;
                return Helpers.isCircleIntersectedByLine(cover.Position, ur * ur, attacker.Position, unit.Position);
            }
        }
    }
}



