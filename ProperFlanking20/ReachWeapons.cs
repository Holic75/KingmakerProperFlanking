using Kingmaker.Controllers.Combat;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items.Slots;
using Kingmaker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProperFlanking20.ReachWeapons
{
    [Harmony12.HarmonyPatch(typeof(UnitEngagementExtension))]
    [Harmony12.HarmonyPatch("IsReach", Harmony12.MethodType.Normal)]
    class Patch_UnitEngagementExtension__IsReach__Patch
    {
        static void Postfix(UnitEntityData unit, UnitEntityData enemy, WeaponSlot hand, ref bool __result)
        {
            var weapon = hand.Weapon;
            if (weapon == null)
            {
                return;
            }

            bool is_reach = weapon.Blueprint.IsMelee && weapon.Blueprint.Type.AttackRange > GameConsts.MinWeaponRange;

            if (__result == true && is_reach) 
            {  //we are going to use half of treat range in this case for dead zone
               //if unit is medium  - it will be 6/2 = 3 feet
               //if unit is large - it will be 10/2 = 5 feet
               //if unit is huge - it will be 13/2 = 6.5 feet
               //if unit is gargantuan - it will be 16/2 = 8 feet
               //if unit is clossal - it will be 19/2 = 9.5 feet
                float meters = hand.Weapon.AttackRange.Meters * 0.5f;
                //Main.logger.Log(unit.CharacterName + " TestingReach: " + meters.ToString() + " Distance: " + unit.DistanceTo(enemy).ToString());
                //Main.logger.Log(unit.CharacterName + " Min Distance: " + (unit.View.Corpulence + meters + enemy.View.Corpulence).ToString());
                __result = (unit.DistanceTo(enemy) >= unit.View.Corpulence + meters + enemy.View.Corpulence);
                //Main.logger.Log("TestingReachResult: " + __result.ToString());
            }
        }
    }
}
