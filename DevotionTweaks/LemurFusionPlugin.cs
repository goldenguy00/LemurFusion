using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using LemurFusion.Config;
using R2API;
using R2API.ContentManagement;
using RoR2;
using System;
using System.Runtime.CompilerServices;

namespace LemurFusion
{
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("bouncyshield.LemurianNames", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.KingEnderBrine.ProperSave", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(R2API.R2API.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2APIContentManager.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(EliteAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(RecalculateStatsAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(ItemAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(PrefabAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class LemurFusionPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.score.LemurFusion";
        public const string PluginName = "LemurFusion";
        public const string PluginVersion = "1.1.0";
        public static PluginInfo pluginInfo;

        public static LemurFusionPlugin instance;

        public static ManualLogSource _logger;
        
        public static bool rooInstalled => Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions");
        public static bool lemNamesInstalled => Chainloader.PluginInfos.ContainsKey("bouncyshield.LemurianNames");
        public static bool properSaveInstalled => Chainloader.PluginInfos.ContainsKey("com.KingEnderBrine.ProperSave");

        public void Awake()
        {
            instance = this;
            _logger = Logger;
            pluginInfo = Info;

            PluginConfig.myConfig = Config;

            ConfigReader.Setup();

            new DevotionTweaks();
            new StatHooks();
            new AITweaks();

            CreateHarmonyPatches();
            CreateProperSaveCompat();

            ContentAddition.AddMaster(DevotionTweaks.masterPrefab);

            GameModeCatalog.availability.CallWhenAvailable(new Action(ConfigExtended.PostLoad));
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void CreateHarmonyPatches()
        {
            var harmony = new Harmony(PluginGUID);

            if (lemNamesInstalled)
            {
                PatchLemurNames(harmony);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void PatchLemurNames(Harmony harmony)
        {
            harmony.CreateClassProcessor(typeof(LemurianNamesPatch)).Patch();
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void CreateProperSaveCompat()
        {
            if (properSaveInstalled)
            {
                ProperSaveManager.Init();
            }
        }
    }
}
