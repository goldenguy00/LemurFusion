using BepInEx.Configuration;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace LemurFusion.Config
{
    public static class PluginConfig
    {
        public static ConfigFile myConfig;

        public static ConfigEntry<bool> disableFallDamage;
        public static ConfigEntry<bool> enableMinionScoreboard;
        public static ConfigEntry<int> teleportDistance;
        public static ConfigEntry<int> maxLemurs;
        public static ConfigEntry<bool> highTierElitesOnly;

        public static ConfigEntry<bool> improveAI;
        public static ConfigEntry<bool> enableDetailedLogs;
        public static ConfigEntry<bool> miniElders;

        public static ConfigEntry<int> statMultHealth;
        public static ConfigEntry<int> statMultDamage;
        public static ConfigEntry<int> statMultAttackSpeed;
        //public static ConfigEntry<int> statMultSize;


        public const string GENERAL = "01 - General";
        public const string EXPERIMENTAL = "02 - Experimental";
        public const string STATS = "03 - Fusion Stats";

        internal static void ReadConfig()
        {
            InitROO();

            // general
            maxLemurs = BindAndOptionsSlider(GENERAL, 
                "Max Lemurs",
                1, 
                "Max dudes",
                1, 20);

            disableFallDamage = BindAndOptions(GENERAL,
                "Disable Fall Damage", 
                true, 
                "If true, prevents Lemurians from taking fall damage.");

            teleportDistance = BindAndOptionsSlider(GENERAL,
                "Teleport Distance",
                100, 
                "Sets the max distance a Lemurian can be from their owner before teleporting.",
                50, 400);

            enableMinionScoreboard = BindAndOptions(GENERAL,
                "Enable Minion Scoreboard", 
                true, 
                "Devoted Lemurians will show up on the scoreboard.");

            highTierElitesOnly = BindAndOptions(GENERAL,
                "High Tier Elites Only For Final Evolution",
                true,
                "When rerolling the fully evolved elite elder lemurian aspects, should it always be a lategame elite type?");

            // misc
            
            miniElders = BindAndOptions(EXPERIMENTAL, 
                "Mini Elder Lemurians",
                false,
                "Theyre so cute omg",
                true);
            
            improveAI = BindAndOptions(EXPERIMENTAL,
                "Improve AI",
                true,
                "Makes minions less likely to stand around",
                true);
            enableDetailedLogs = BindAndOptions(EXPERIMENTAL,
                "Enable Detailed Logs",
                true,
                "For dev use/debugging. Keep this on if you want to submit a bug report.",
                true);

            /*fixEvoWhenDisabled = BindAndOptions(EXPERIMENTAL,
                "Fix When Disabled",
                true,
                "Fixes the item orb not showing when giving items to eggs and allows devotion minions to evolve even when the Artifact is disabled.", true);
            */
            // stats
            statMultHealth = BindAndOptionsSlider(STATS, 
                "Fusion Health Increase",
                20, 
                "Health multiplier for each lemur fusion, in percent.",
                0, 100);

            statMultDamage = BindAndOptionsSlider(STATS, 
                "Fusion Damage Increase",
                20, 
                "Damage multiplier for each lemur fusion, in percent.", 
                0, 100);

            statMultAttackSpeed = BindAndOptionsSlider(STATS,
                "Fusion Attack Speed Increase",
                20,
                "Attack speed multiplier for each lemur fusion, in percent.",
                0, 100);
            /*
            statMultSize = BindAndOptionsSlider(STATS, 
                "Fusion Size Increase",
                2,
                "Base size multiplier for each lemur fusion, in percent.",
                0, 10);*/
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void InitROO()
        {
            if (LemurFusionPlugin.rooInstalled)
            {
                /*var sprite = LoadSprite();
                if (sprite != null)
                {
                    ModSettingsManager.SetModIcon(sprite);
                }*/
                ModSettingsManager.SetModDescription("Devotion Artifact but better.");
            }
        }

        public static Sprite LoadSprite()
        {
            var filePath = Path.Combine(Assembly.GetExecutingAssembly().Location, "icon.png");

            if (File.Exists(filePath))
            {
                // i hate this tbh
                Texture2D texture = new(1, 1);
                texture.LoadImage(File.ReadAllBytes(filePath));

                if (texture != null)
                {
                    var bounds = new Rect(0, 0, texture.width, texture.height);
                    return Sprite.Create(texture, bounds, new Vector2(bounds.width * 0.5f, bounds.height * 0.5f));
                }
            }

            return null;
        }

        #region Config
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static ConfigEntry<T> BindAndOptions<T>(string section, string name, T defaultValue, string description = "", bool restartRequired = false)
        {
            if (string.IsNullOrEmpty(description))
            {
                description = name;
            }

            if (restartRequired)
            {
                description += " (restart required)";
            }

            ConfigEntry<T> configEntry = myConfig.Bind(section, name, defaultValue, description);

            if (LemurFusionPlugin.rooInstalled)
            {
                TryRegisterOption(configEntry, restartRequired);
            }

            return configEntry;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static ConfigEntry<T> BindAndOptionsEnum<T>(string section, string name, T defaultValue, string description = "", bool restartRequired = false)
        {
            if (string.IsNullOrEmpty(description))
            {
                description = name;
            }

            if (restartRequired)
            {
                description += " (restart required)";
            }

            ConfigEntry<T> configEntry = myConfig.Bind(section, name, defaultValue, description);

            if (LemurFusionPlugin.rooInstalled)
            {
                TryRegisterOption(configEntry, restartRequired);
            }

            return configEntry;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static ConfigEntry<float> BindAndOptionsSlider(string section, string name, float defaultValue, string description = "", float min = 0, float max = 20, bool restartRequired = false)
        {
            if (string.IsNullOrEmpty(description))
            {
                description = name;
            }

            description += " (Default: " + defaultValue + ")";

            if (restartRequired)
            {
                description += " (restart required)";
            }

            ConfigEntry<float> configEntry = myConfig.Bind(section, name, defaultValue, description);

            if (LemurFusionPlugin.rooInstalled)
            {
                TryRegisterOptionSlider(configEntry, min, max, restartRequired);
            }

            return configEntry;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static ConfigEntry<int> BindAndOptionsSlider(string section, string name, int defaultValue, string description = "", int min = 0, int max = 20, bool restartRequired = false)
        {
            if (string.IsNullOrEmpty(description))
            {
                description = name;
            }

            description += " (Default: " + defaultValue + ")";

            if (restartRequired)
            {
                description += " (restart required)";
            }

            ConfigEntry<int> configEntry = myConfig.Bind(section, name, defaultValue, description);

            if (LemurFusionPlugin.rooInstalled)
            {
                TryRegisterOptionSlider(configEntry, min, max, restartRequired);
            }

            return configEntry;
        }
        #endregion

        #region RoO
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void TryRegisterOption<T>(ConfigEntry<T> entry, bool restartRequired)
        {
            if (entry is ConfigEntry<string> stringEntry)
            {
                ModSettingsManager.AddOption(new StringInputFieldOption(stringEntry, restartRequired));
            }
            if (entry is ConfigEntry<float>)
            {
                ModSettingsManager.AddOption(new SliderOption(entry as ConfigEntry<float>, new SliderConfig()
                {
                    min = 0,
                    max = 20,
                    formatString = "{0:0.00}",
                    restartRequired = restartRequired
                }));
            }
            if (entry is ConfigEntry<int>)
            {
                ModSettingsManager.AddOption(new IntSliderOption(entry as ConfigEntry<int>, restartRequired));
            }
            if (entry is ConfigEntry<bool>)
            {
                ModSettingsManager.AddOption(new CheckBoxOption(entry as ConfigEntry<bool>, restartRequired));
            }
            if (entry is ConfigEntry<KeyboardShortcut>)
            {
                ModSettingsManager.AddOption(new KeyBindOption(entry as ConfigEntry<KeyboardShortcut>, restartRequired));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void TryRegisterOptionSlider(ConfigEntry<int> entry, int min, int max, bool restartRequired)
        {
            ModSettingsManager.AddOption(new IntSliderOption(entry as ConfigEntry<int>, new IntSliderConfig()
            {
                min = min,
                max = max,
                formatString = "{0:0.00}",
                restartRequired = restartRequired
            }));
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void TryRegisterOptionSlider(ConfigEntry<float> entry, float min, float max, bool restartRequired)
        {
            ModSettingsManager.AddOption(new SliderOption(entry as ConfigEntry<float>, new SliderConfig()
            {
                min = min,
                max = max,
                formatString = "{0:0.00}",
                restartRequired = restartRequired
            }));
        }
        #endregion
    }
}