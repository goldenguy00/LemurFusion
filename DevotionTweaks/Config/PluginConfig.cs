using BepInEx.Configuration;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace LemurFusion.Config
{
    public static class PluginConfig
    {
        public static ConfigFile myConfig;

        public static ConfigEntry<bool> disableFallDamage;
        public static ConfigEntry<bool> enableMinionScoreboard;
        public static ConfigEntry<bool> personalInventory;
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

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal static void ReadConfig()
        {
            if (LemurFusionPlugin.rooInstalled)
            {
                InitROO();
            }

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
                150, 
                "Sets the max distance a Lemurian can be from their owner before teleporting.",
                50, 400);

            enableMinionScoreboard = BindAndOptions(GENERAL,
                "Enable Minion Scoreboard",
                true,
                "Devoted Lemurians will show up on the scoreboard.");

            personalInventory = BindAndOptions(GENERAL,
                "Individual Scoreboard Inventories",
                true,
                "Enable to display a scoreboard entry for each lemurian you control. Scoreboard entry will show the items that each minion contributes to the shared inventory. \r\n\r\n" +
                "Purely visual, does not change vanilla item sharing mechanics.");

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
            var sprite = LoadSprite();
            if (sprite != null)
            {
                RiskOfOptions.ModSettingsManager.SetModIcon(sprite);
            }
            RiskOfOptions.ModSettingsManager.SetModDescription("Devotion Artifact but better.");
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static Sprite LoadSprite()
        {
            var filePath = Path.Combine(Assembly.GetExecutingAssembly().Location, "icon.png");

            if (File.Exists(filePath))
            {
                // i hate this tbh
                Texture2D texture = new(2, 2);
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
        public static ConfigEntry<T> BindAndOptionsSlider<T>(string section, string name, T defaultValue, string description = "", float min = 0, float max = 20, bool restartRequired = false)
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

            ConfigEntry<T> configEntry = myConfig.Bind(section, name, defaultValue, description);

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
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.StringInputFieldOption(stringEntry, restartRequired));
                return;
            }
            if (entry is ConfigEntry<float> floatEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.SliderOption(floatEntry, new RiskOfOptions.OptionConfigs.SliderConfig()
                {
                    min = 0,
                    max = 20,
                    formatString = "{0:0.00}",
                    restartRequired = restartRequired
                }));
                return;
            }
            if (entry is ConfigEntry<int> intEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.IntSliderOption(intEntry, restartRequired));
                return;
            }
            if (entry is ConfigEntry<bool> boolEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.CheckBoxOption(boolEntry, restartRequired));
                return;
            }
            if (entry is ConfigEntry<KeyboardShortcut> shortCutEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(shortCutEntry, restartRequired));
                return;
            }
            if (typeof(T).IsEnum)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.ChoiceOption(entry, restartRequired));
                return;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void TryRegisterOptionSlider<T>(ConfigEntry<T> entry, float min, float max, bool restartRequired)
        {
            if (entry is ConfigEntry<int> intEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.IntSliderOption(intEntry, new RiskOfOptions.OptionConfigs.IntSliderConfig()
                {
                    min = (int)min,
                    max = (int)max,
                    formatString = "{0:0.00}",
                    restartRequired = restartRequired
                }));
                return;
            }

            if (entry is ConfigEntry<float> floatEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.SliderOption(floatEntry, new RiskOfOptions.OptionConfigs.SliderConfig()
                {
                    min = min,
                    max = max,
                    formatString = "{0:0.00}",
                    restartRequired = restartRequired
                }));
            }
        }
        #endregion
    }
}