using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using LemurFusion.Compat;
using LemurFusion.Config;
using LemurFusion.Devotion;
using LemurFusion.Devotion.Components;

namespace LemurFusion
{
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("bouncyshield.LemurianNames", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.KingEnderBrine.ProperSave", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class LemurFusionPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.score.LemurFusion";
        public const string PluginName = "LemurFusion";
        public const string PluginVersion = "1.8.4";

        public static LemurFusionPlugin instance { get; private set; }
        
        public static bool rooInstalled => Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions");
        public static bool lemNamesInstalled => Chainloader.PluginInfos.ContainsKey("bouncyshield.LemurianNames");
        public static bool properSaveInstalled => Chainloader.PluginInfos.ContainsKey("com.KingEnderBrine.ProperSave");
        public static bool riskyInstalled => Chainloader.PluginInfos.ContainsKey("com.RiskyLives.RiskyMod");

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void Awake()
        {
            instance = this;
            _logger = Logger;

            PluginConfig.myConfig = Config;

            ConfigReader.Init();

            DevotionTweaks.Init();
            DevotedInventoryTweaks.Init();
            LemurControllerTweaks.Init();
            AITweaks.Init();
            RoR2.RoR2Application.onLoad += MechaLemur.Init;

            R2API.ContentAddition.AddMaster(DevotionTweaks.instance.bruiserMasterPrefab);

            var harmony = new HarmonyLib.Harmony(PluginGUID);

            if (LemurFusionPlugin.lemNamesInstalled)
                CreateLemNameCompat(harmony);

            if (LemurFusionPlugin.properSaveInstalled)
                CreateProperSaveCompat(harmony);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void CreateLemNameCompat(Harmony harmony)
        {
            harmony.CreateClassProcessor(typeof(LemurianUpdateNameFriend)).Patch();
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void CreateProperSaveCompat(Harmony harmony)
        {
            harmony.CreateClassProcessor(typeof(FixProperSave)).Patch();
            ProperSaveManager.Init();
        }

        #region Logging
        private static ManualLogSource _logger;
        public static void LogDebug(string message)
        {
#if DEBUG
            Log(LogLevel.Debug, message);
#endif
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
        #endregion

    }
}
