using UnityModManagerNet;
using System;
using System.Reflection;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Designers.Mechanics.Buffs;
using System.Collections.Generic;
using Kingmaker.Blueprints.Items;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;


public class Main
{
    public class Settings
    {
        public bool ignore_cover { get; }

        public Settings()
        {

            using (StreamReader settings_file = File.OpenText("Mods/ProperFlanking/settings.json"))
            using (JsonTextReader reader = new JsonTextReader(settings_file))
            {
                JObject jo = (JObject)JToken.ReadFrom(reader);
                ignore_cover = (bool)jo["ignore_cover"];
            }
        }
    }

    public static UnityModManagerNet.UnityModManager.ModEntry.ModLogger logger;
    internal static LibraryScriptableObject library;

    static readonly Dictionary<Type, bool> typesPatched = new Dictionary<Type, bool>();
    static readonly List<String> failedPatches = new List<String>();
    static readonly List<String> failedLoading = new List<String>();
    static public Settings settings = new Settings();



    [System.Diagnostics.Conditional("DEBUG")]
        public static void DebugLog(string msg)
        {
            if (logger != null) logger.Log(msg);
        }
        public static void DebugError(Exception ex)
        {
            if (logger != null) logger.Log(ex.ToString() + "\n" + ex.StackTrace);
        }
        public static bool enabled;
        static bool Load(UnityModManager.ModEntry modEntry)
        {
            try
            {
                logger = modEntry.Logger;
                Main.DebugLog("Loading Proper Flanking");
                
                var harmony = Harmony12.HarmonyInstance.Create(modEntry.Info.Id);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                //patch coordinated shot
                Type CoordinatedShotAttackBonusType = Type.GetType("CallOfTheWild.NewMechanics.CoordinatedShotAttackBonus, CallOfTheWild");
                if (CoordinatedShotAttackBonusType != null)
                {
                    logger.Log("Found CallOfTheWild.NewMechanics.CoordinatedShotAttackBonus, patching...");
                    harmony.Patch(Harmony12.AccessTools.Method(CoordinatedShotAttackBonusType, "OnEventAboutToTrigger"),
                                   prefix: new Harmony12.HarmonyMethod(typeof(ProperFlanking.ManualPatching.CoordinatedShotAttcakBonus_OnEventAboutToTrigger_Patch), "Prefix")
                                 );
                }
                else
                {
                    logger.Log("CallOfTheWild.NewMechanics.CoordinatedShotAttackBonus not found.");
                }
            }
            catch (Exception ex)
            {
                DebugError(ex);
                throw ex;   
            }
            return true;
        }

        [Harmony12.HarmonyPatch(typeof(LibraryScriptableObject), "LoadDictionary")]
        [Harmony12.HarmonyPatch(typeof(LibraryScriptableObject), "LoadDictionary", new Type[0])]
        static class LibraryScriptableObject_LoadDictionary_Patch
        {
            static void Postfix(LibraryScriptableObject __instance)
            {
                var self = __instance;
                if (Main.library != null) return;
                Main.library = self;
                try
                {
                    ProperFlanking.Cover.load(settings.ignore_cover);
            }
                catch (Exception ex)
                {
                    Main.DebugError(ex);
                }
            }
        }

        internal static Exception Error(String message)
        {
            logger?.Log(message);
            return new InvalidOperationException(message);
        }
}
