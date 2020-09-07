using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.UnitLogic;
using Kingmaker.Utility;
using Kingmaker.View.Equipment;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TurnBased.Controllers;

namespace ProperFlanking20.QuickDraw
{
    public class UnitPartQuickDraw : UnitPart
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
            if (buffs.Empty())
            {
                this.Owner.Remove<UnitPartQuickDraw>();
            }
        }

        public bool active()
        {
            return !buffs.Empty();
        }
    }


    public class QuickDraw : OwnedGameLogicComponent<UnitDescriptor>
    {
        public override void OnTurnOn()
        {
            this.Owner.Ensure<UnitPartQuickDraw>().addBuff(this.Fact);
        }

        public override void OnTurnOff()
        {
            this.Owner.Ensure<UnitPartQuickDraw>().removeBuff(this.Fact);
        }
    }


    [Harmony12.HarmonyPatch(typeof(UnitViewHandsEquipment))]
    [Harmony12.HarmonyPatch("HandleEquipmentSlotUpdated", Harmony12.MethodType.Normal)]
    class UnitViewHandsEquipment_HandleEquipmentSlotUpdated_Patch
    {
        static bool Prefix(UnitViewHandsEquipment __instance, HandSlot slot, ItemEntity previousItem)
        {
            var tr = Harmony12.Traverse.Create(__instance);

            if (!tr.Property("Active").GetValue<bool>() || tr.Method("GetSlotData", slot).GetValue<UnitViewHandSlotData>() == null)
            {
                return true;
            }


            if (__instance.Owner.Ensure<UnitPartQuickDraw>().active()
                 && __instance.InCombat && (__instance.Owner.Descriptor.State.CanAct || __instance.IsDollRoom) && slot.Active)
            {
                tr.Method("ChangeEquipmentWithoutAnimation").GetValue();
                return false;
            }

            return true;
        }
    }


    [Harmony12.HarmonyPatch(typeof(UnitViewHandsEquipment))]
    [Harmony12.HarmonyPatch("HandleEquipmentSetChanged", Harmony12.MethodType.Normal)]
    class UnitViewHandsEquipment_HandleEquipmentSetChanged_Patch
    {
        static bool Prefix(UnitViewHandsEquipment __instance)
        {
            var tr = Harmony12.Traverse.Create(__instance);

            if (!tr.Property("Active").GetValue<bool>())
            {
                return true;
            }

            if (__instance.Owner.Ensure<UnitPartQuickDraw>().active()
                 && __instance.InCombat && (__instance.Owner.Descriptor.State.CanAct || __instance.IsDollRoom))
            {
                tr.Method("UpdateActiveWeaponSetImmediately").GetValue();
                return false;
            }

            return true;
        }
    }


    [Harmony12.HarmonyPatch(typeof(TurnController))]
    [Harmony12.HarmonyPatch("HandleUnitChangeActiveEquipmentSet", Harmony12.MethodType.Normal)]
    class TurnController_HandleEquipmentSetChanged_Patch
    {
        static bool Prefix(TurnController __instance, UnitDescriptor unit)
        {
            if ((unit?.Get<UnitPartQuickDraw>()?.active()).GetValueOrDefault())
            {
                return false;
            }

            return true;
        }
    }
}
