using Kingmaker.Blueprints;
using Kingmaker.Controllers.Combat;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProperFlanking20.ReachWeapons
{
    public class UnitPartIgnoreReachDeadZone : CallOfTheWild.AdditiveUnitPart
    {
        public bool active()
        {
            return !buffs.Empty();
        }
    }


    public class IgnoreReachDeadZone : OwnedGameLogicComponent<UnitDescriptor>
    {
        public override void OnTurnOn()
        {
            this.Owner.Ensure<UnitPartIgnoreReachDeadZone>().addBuff(this.Fact);
        }

        public override void OnTurnOff()
        {
            this.Owner.Ensure<UnitPartIgnoreReachDeadZone>().removeBuff(this.Fact);
        }
    }

    [Harmony12.HarmonyPatch(typeof(UnitEngagementExtension))]
    [Harmony12.HarmonyPatch("IsReach", Harmony12.MethodType.Normal)]
    class Patch_UnitEngagementExtension__IsReach__Patch
    {
        static void Postfix(UnitEntityData unit, UnitEntityData enemy, WeaponSlot hand, ref bool __result)
        {
            if (!Main.settings.reach_weapons_dead_zone)
            {
                return;
            }
            var weapon = hand.Weapon;
            if (weapon == null)
            {
                return;
            }

            bool is_reach = weapon.Blueprint.IsMelee && weapon.Blueprint.Type.AttackRange > GameConsts.MinWeaponRange;
            is_reach = is_reach && !(unit?.Get<UnitPartIgnoreReachDeadZone>()?.active()).GetValueOrDefault();

            if (__result == true && is_reach)
            {  //we are going to use half of treat range in this case for dead zone
               //if unit is medium  - it will be 6/2 = 3 feet
               //if unit is large - it will be 10/2 = 5 feet
               //if unit is huge - it will be 13/2 = 6.5 feet
               //if unit is gargantuan - it will be 16/2 = 8 feet
               //if unit is clossal - it will be 19/2 = 9.5 feet
               //float meters = hand.Weapon.AttackRange.Meters * 0.5f;
                float meters = getDeadRange(unit, hand.Weapon);
                //Main.logger.Log(unit.CharacterName + " TestingReach: " + meters.ToString() + " Distance: " + unit.DistanceTo(enemy).ToString());
                //Main.logger.Log(unit.CharacterName + " Min Distance: " + (unit.View.Corpulence + meters + enemy.View.Corpulence).ToString());
                __result = (distance(unit, enemy) >= unit.View.Corpulence + meters + enemy.View.Corpulence);
                //Main.logger.Log("TestingReachResult: " + __result.ToString());
            }
        }


        public static float getDeadRange(UnitEntityData unit, ItemEntityWeapon weapon)
        {
            //return weapon.AttackRange.Meters * 0.5f;
            return  (unit.Descriptor.State.Size.GetModifiers().Reach + 1).Feet().Meters * 0.5f;
        }

        public static float distance(UnitEntityData unit, UnitEntityData enemy)
        {
            //return unit.DistanceTo(enemy);
            return (unit.Position - enemy.Position).magnitude;
        }

    }

    [Harmony12.HarmonyPatch(typeof(UnitCommand))]
    [Harmony12.HarmonyPatch("GetTargetPoint", Harmony12.MethodType.Normal)]
    class Patch_UnitCommand__GetTargetPoint__Patch
    {
        static void Postfix(UnitCommand __instance, ref Vector3 __result)
        {
            if (!Main.settings.reach_weapons_dead_zone)
            {
                return;
            }
            var attack_command = __instance as UnitAttack;
            if (attack_command == null || attack_command.PlannedAttack == null)
            {
                return;
            }

            var enemy = __instance.Target.Unit;
            var unit = __instance.Executor;

            bool is_reach = attack_command.PlannedAttack.Weapon.Blueprint.IsMelee && attack_command.PlannedAttack.Weapon.Blueprint.Type.AttackRange > GameConsts.MinWeaponRange;
            is_reach = is_reach && !(unit?.Get<UnitPartIgnoreReachDeadZone>()?.active()).GetValueOrDefault();

            if (!is_reach)
            {
                return;
            }


            if (enemy == null || unit == null)
            {
                return;
            }
            //float dead_meters = attack_command.PlannedAttack.Weapon.AttackRange.Meters * 0.5f;
            float dead_meters = Patch_UnitEngagementExtension__IsReach__Patch.getDeadRange(unit, attack_command.PlannedAttack.Weapon);
            if (GeometryUtils.SqrMechanicsDistance(__instance.Target.Point, unit.Position) > (double)__instance.ApproachRadius * (double)__instance.ApproachRadius)
            {
                //unit is too far from its target, no need to worry
                return;
            }

            float ud = Patch_UnitEngagementExtension__IsReach__Patch.distance(unit, enemy);
            var e = (unit.Position - enemy.Position).normalized;
            float margin = unit.View.Corpulence + dead_meters + enemy.View.Corpulence;
            if (ud < margin)
            {
                //we are too close, need to update approach point
                __result = unit.Position + e * (__instance.ApproachRadius + 1.1f*(margin - ud));
            }

        }
    }
}

