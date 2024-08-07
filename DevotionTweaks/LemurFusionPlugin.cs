using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using LemurFusion.Compat;
using LemurFusion.Config;
using LemurFusion.Devotion;
using LemurFusion.Devotion.Components;

namespace LemurFusion
{
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("bouncyshield.LemurianNames", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.KingEnderBrine.ProperSave", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.RiskyLives.RiskyMod", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Nebby.VAPI", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class LemurFusionPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.score.LemurFusion";
        public const string PluginName = "LemurFusion";
        public const string PluginVersion = "1.3.2";

        public static LemurFusionPlugin instance { get; private set; }
        
        public static bool rooInstalled => Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions");
        public static bool lemNamesInstalled => Chainloader.PluginInfos.ContainsKey("bouncyshield.LemurianNames");
        public static bool properSaveInstalled => Chainloader.PluginInfos.ContainsKey("com.KingEnderBrine.ProperSave");
        public static bool riskyInstalled => Chainloader.PluginInfos.ContainsKey("com.RiskyLives.RiskyMod");
        public static bool vApiInstalled => Chainloader.PluginInfos.ContainsKey("com.Nebby.VAPI");

        #region Logging
        private static ManualLogSource _logger;
        public static void LogDebug(string message) => Log(LogLevel.Debug, message);
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

        public void Awake()
        {
            instance = this;
            _logger = Logger;

            PluginConfig.myConfig = Config;

            ConfigReader.Setup();

            DevotionTweaks.Init();
            DevotedInventoryTweaks.Init();
            LemurControllerTweaks.Init();
            AITweaks.Init();
            MechaLemur.Init();

            R2API.ContentAddition.AddBody(DevotionTweaks.instance.bodyPrefab);
            R2API.ContentAddition.AddBody(DevotionTweaks.instance.bigBodyPrefab);

            CreateHarmonyPatches();
            CreateProperSaveCompat();
        }

        private void CreateHarmonyPatches()
        {
            var harmony = new HarmonyLib.Harmony(PluginGUID);
            harmony.CreateClassProcessor(typeof(CombatSquadFixedUpdate)).Patch();

            if (LemurFusionPlugin.lemNamesInstalled)
            {
                harmony.CreateClassProcessor(typeof(LemurianNameFriend)).Patch();
                harmony.CreateClassProcessor(typeof(LemurianUpdateNameFriend)).Patch();
            }

            if (LemurFusionPlugin.vApiInstalled)
            {
                harmony.CreateClassProcessor(typeof(VarianceAPI)).Patch();
            }
        }

        private void CreateProperSaveCompat()
        {
            if (LemurFusionPlugin.properSaveInstalled)
            {
                new ProperSaveManager();
            }
        }
    }
}
