using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using LemurFusion.AI;
using LemurFusion.Config;
using R2API;
using RoR2;
using System;
using System.Runtime.CompilerServices;

namespace LemurFusion
{
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("bouncyshield.LemurianNames", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.KingEnderBrine.ProperSave", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.RiskyLives.RiskyMod", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class LemurFusionPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.score.LemurFusion";
        public const string PluginName = "LemurFusion";
        public const string PluginVersion = "1.2.3";

        public static LemurFusionPlugin instance;

        private static ManualLogSource _logger;
        
        public static bool rooInstalled => Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions");
        public static bool lemNamesInstalled => Chainloader.PluginInfos.ContainsKey("bouncyshield.LemurianNames");
        public static bool properSaveInstalled => Chainloader.PluginInfos.ContainsKey("com.KingEnderBrine.ProperSave");
        public static bool riskyInstalled => Chainloader.PluginInfos.ContainsKey("com.RiskyLives.RiskyMod");

        public void Awake()
        {
            instance = this;
            _logger = Logger;

            PluginConfig.myConfig = Config;

            ConfigReader.Setup();

            DevotionTweaks.Init();
            StatHooks.Init();
            AITweaks.Init();

            CreateHarmonyPatches();
            CreateProperSaveCompat();

            RoR2Application.onLoad += ConfigExtended.PostLoad;
        }

        public static void LogInfo(string message) => Log(LogLevel.Info, message);
        public static void LogMessage(string message) => Log(LogLevel.Message, message);
        public static void LogWarning(string message) => Log(LogLevel.Warning, message);
        public static void LogError(string message) => Log(LogLevel.Error, message);
        public static void LogFatal(string message) => Log(LogLevel.Fatal, message);

        public static void Log(LogLevel logLevel, string message)
        {
            if (PluginConfig.enableDetailedLogs.Value)
            {
                _logger.Log(logLevel, message);
            }
        }


        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void CreateHarmonyPatches()
        {
            if (LemurFusionPlugin.lemNamesInstalled)
            {
                var harmony = new Harmony(PluginGUID);
                harmony.CreateClassProcessor(typeof(LemurianNameFriend)).Patch();
                harmony.CreateClassProcessor(typeof(LemurianUpdateNameFriend)).Patch();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void CreateProperSaveCompat()
        {
            if (LemurFusionPlugin.properSaveInstalled)
            {
                new ProperSaveManager();
            }
        }
    }
}
