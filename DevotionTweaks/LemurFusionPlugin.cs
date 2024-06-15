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
    [BepInDependency("HIFU.Inferno", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Moffein.RiskyArtifacts", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class LemurFusionPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.score.LemurFusion";
        public const string PluginName = "LemurFusion";
        public const string PluginVersion = "1.1.1";

        public static PluginInfo pluginInfo;

        public static LemurFusionPlugin instance;

        public static ManualLogSource _logger;
        
        public static bool rooInstalled => Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions");
        public static bool lemNamesInstalled => Chainloader.PluginInfos.ContainsKey("bouncyshield.LemurianNames");
        public static bool properSaveInstalled => Chainloader.PluginInfos.ContainsKey("com.KingEnderBrine.ProperSave");
        public static bool riskyInstalled => Chainloader.PluginInfos.ContainsKey("com.RiskyLives.RiskyMod");

        public void Awake()
        {
            instance = this;
            _logger = Logger;
            pluginInfo = Info;

            PluginConfig.myConfig = Config;

            ConfigReader.Setup();

            DevotionTweaks.Init();
            StatHooks.Init();
            AITweaks.Init();

            CreateHarmonyPatches();
            CreateProperSaveCompat();

            ContentAddition.AddMaster(DevotionTweaks.masterPrefab);

            GameModeCatalog.availability.CallWhenAvailable(new Action(ConfigExtended.PostLoad));
        }


        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void CreateHarmonyPatches()
        {
            if (lemNamesInstalled)
            {
                var harmony = new Harmony(PluginGUID);
                harmony.CreateClassProcessor(typeof(LemurianNameFriend)).Patch();
                harmony.CreateClassProcessor(typeof(LemurianUpdateNameFriend)).Patch();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void CreateProperSaveCompat()
        {
            if (properSaveInstalled)
            {
                new ProperSaveManager();
            }
        }
    }
}
