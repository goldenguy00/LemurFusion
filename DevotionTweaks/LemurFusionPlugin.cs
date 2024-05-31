using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;

namespace DevotionTweaks
{
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class LemurFusionPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.score.LemurFusion";
        public const string PluginName = "LemurFusion";
        public const string PluginVersion = "1.0.0";

        public static LemurFusionPlugin instance;

        public static ManualLogSource _logger;
         
        public static bool rooInstalled => Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions");
        public static bool riskyInstalled => Chainloader.PluginInfos.ContainsKey("com.RiskyLives.RiskyMod");
        //public static bool riskyArtifactsInstalled => Chainloader.PluginInfos.ContainsKey("com.Moffein.RiskyArtifacts");

        public void Awake()
        {
            instance = this;
            _logger = Logger;

            PluginConfig.myConfig = Config;
            PluginConfig.ReadConfig();

            new DevotionTweaks();
            new StatHooks();
        }
    }
}
