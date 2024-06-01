using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using System;

namespace DevotionTweaks
{
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("bouncyshield.LemurianNames", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class LemurFusionPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.score.LemurFusion";
        public const string PluginName = "LemurFusion";
        public const string PluginVersion = "1.0.2";

        public static LemurFusionPlugin instance;

        public static ManualLogSource _logger;
         
        public static bool rooInstalled => Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions");
        public static bool riskyInstalled => Chainloader.PluginInfos.ContainsKey("com.RiskyLives.RiskyMod");
        public static bool lemNamesInstalled => Chainloader.PluginInfos.ContainsKey("bouncyshield.LemurianNames");

        public void Awake()
        {
            instance = this;
            _logger = Logger;

            PluginConfig.myConfig = Config;
            PluginConfig.ReadConfig();

            new DevotionTweaks();
            new StatHooks();

            // this is absurd, change anything that this mod references and it instantly explodes.
            // fucking hell man
            if (lemNamesInstalled)
            {
                var harmony = new Harmony(PluginGUID);
                harmony.PatchAll();
            }
        }
    }

    [HarmonyPatch(typeof(ExamplePlugin.ExamplePlugin), "NameFriend")]
    public class Class_Method
    {
        [HarmonyFinalizer]
        public static Exception Finalizer(Exception __exception)
        {
            if (__exception != null)
            {
                // An exception was thrown by the method!
                LemurFusionPlugin._logger.LogWarning("Exception was thrown by dependency bouncyshield.LemurianNames!");
                LemurFusionPlugin._logger.LogWarning(__exception.Message);
                LemurFusionPlugin._logger.LogWarning(__exception.StackTrace);
            }

            // return null so that no Exception is thrown. You could re-throw as a different Exception as well.
            return null;
        }
    }
}
