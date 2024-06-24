using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using LemurFusion.Compat;
using LemurFusion.Config;
using LemurFusion.Devotion.Tweaks;
using R2API;
using RoR2;
using UnityEngine.AddressableAssets;
using UnityEngine;
using UE = UnityEngine;
using System.Linq;

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

            LoadAssets();

            DevotionTweaks.Init();
            DevotedInventoryTweaks.Init();
            LemurControllerTweaks.Init();
            StatTweaks.Init();
            AITweaks.Init();

            CreateHarmonyPatches();
            CreateProperSaveCompat();
        }

        private static void LoadAssets()
        {
            //Fix up the tags on the Harness
            LemurFusionPlugin.LogInfo("Adding Tags ItemTag.BrotherBlacklist, ItemTag.CannotSteal, ItemTag.CannotCopy to Lemurian Harness.");
            ItemDef itemDef = Addressables.LoadAssetAsync<ItemDef>("RoR2/CU8/Harness/LemurianHarness.asset").WaitForCompletion();
            if (itemDef)
            {
                itemDef.tags = itemDef.tags.Concat([ItemTag.BrotherBlacklist, ItemTag.CannotSteal, ItemTag.CannotCopy]).Distinct().ToArray();
            }

            // dupe body
            GameObject lemurianBodyPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Lemurian/LemurianBody.prefab").WaitForCompletion();
            DevotionTweaks.bodyPrefab = lemurianBodyPrefab.InstantiateClone(DevotionTweaks.devotedLemBodyName, true);
            var body = DevotionTweaks.bodyPrefab.GetComponent<CharacterBody>();
            body.bodyFlags |= CharacterBody.BodyFlags.Devotion;
            body.baseMaxHealth = 360f;
            body.levelMaxHealth = 11f;
            body.baseMoveSpeed = 7f;

            // dupe body pt2
            GameObject lemurianBruiserBodyPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/LemurianBruiser/LemurianBruiserBody.prefab").WaitForCompletion();
            DevotionTweaks.bigBodyPrefab = lemurianBruiserBodyPrefab.InstantiateClone(DevotionTweaks.devotedBigLemBodyName, true);
            var bigBody = DevotionTweaks.bigBodyPrefab.GetComponent<CharacterBody>();
            bigBody.bodyFlags |= CharacterBody.BodyFlags.Devotion;
            bigBody.baseMaxHealth = 720f;
            bigBody.levelMaxHealth = 22f;
            bigBody.baseMoveSpeed = 10f;

            // fix original
            lemurianBodyPrefab.GetComponent<CharacterBody>().bodyFlags &= ~CharacterBody.BodyFlags.Devotion;
            lemurianBruiserBodyPrefab.GetComponent<CharacterBody>().bodyFlags &= ~CharacterBody.BodyFlags.Devotion;

            // better master
            DevotionTweaks.masterPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/CU8/LemurianEgg/DevotedLemurianMaster.prefab")
                .WaitForCompletion().InstantiateClone(DevotionTweaks.devotedMasterName, true);
            UE.Object.DestroyImmediate(DevotionTweaks.masterPrefab.GetComponent<DevotedLemurianController>());
            DevotionTweaks.masterPrefab.AddComponent<BetterLemurController>();
            DevotionTweaks.masterPrefab.GetComponent<CharacterMaster>().bodyPrefab = DevotionTweaks.bodyPrefab;

            ContentAddition.AddMaster(DevotionTweaks.masterPrefab);
            ContentAddition.AddBody(DevotionTweaks.bodyPrefab);
            ContentAddition.AddBody(DevotionTweaks.bigBodyPrefab);
        }

        private void CreateHarmonyPatches()
        {
            var harmony = new Harmony(PluginGUID);
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
