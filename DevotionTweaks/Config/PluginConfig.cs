using BepInEx.Configuration;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace LemurFusion.Config
{
    public static class PluginConfig
    {
        public static ConfigFile myConfig;

        #region Config Entries
        public static ConfigEntry<int> maxLemurs;
        public static ConfigEntry<int> teleportDistance;
        public static ConfigEntry<bool> enableMinionScoreboard;
        public static ConfigEntry<bool> showPersonalInventory;
        public static ConfigEntry<bool> highTierElitesOnly;
        public static ConfigEntry<bool> permaDevotion;
        public static ConfigEntry<int> eggSpawnChance;

        public static ConfigEntry<bool> enableSharedInventory;
        public static ConfigEntry<bool> miniElders;
        public static ConfigEntry<int> scaleValue;
        public static ConfigEntry<int> initScaleValue;
        public static ConfigEntry<bool> enableDetailedLogs;
        public static ConfigEntry<bool> disableTeamCollision;

        public static ConfigEntry<bool> rebalanceStatScaling;
        public static ConfigEntry<int> statMultHealth;
        public static ConfigEntry<int> statMultDamage;
        public static ConfigEntry<int> statMultAttackSpeed;
        public static ConfigEntry<int> statMultEvo;
        #endregion

        #region Config Binding
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static ConfigEntry<T> BindOption<T>(string section, string name, T defaultValue, string description = "", bool restartRequired = false)
        {
            if (string.IsNullOrEmpty(description))
            {
                description = name;
            }

            if (restartRequired)
            {
                description += " (restart required)";
            }

            var configEntry = myConfig.Bind(section, name, defaultValue, description);

            if (LemurFusionPlugin.rooInstalled)
            {
                TryRegisterOption(configEntry, restartRequired);
            }

            return configEntry;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static ConfigEntry<T> BindOptionSlider<T>(string section, string name, T defaultValue, string description = "", float min = 0, float max = 20, bool restartRequired = false)
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

            var configEntry = myConfig.Bind(section, name, defaultValue, description);

            if (LemurFusionPlugin.rooInstalled)
            {
                TryRegisterOptionSlider(configEntry, min, max, restartRequired);
            }

            return configEntry;
        }
        #endregion

        #region RoO
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void InitRoO()
        {
            try
            {
                RiskOfOptions.ModSettingsManager.SetModDescription("Devotion Artifact but better.", LemurFusionPlugin.PluginGUID, LemurFusionPlugin.PluginName);

                var iconStream = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(LemurFusionPlugin.instance.Info.Location), "icon.png"));
                var tex = new Texture2D(256, 256);
                tex.LoadImage(iconStream);
                var icon = Sprite.Create(tex, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));

                RiskOfOptions.ModSettingsManager.SetModIcon(icon);
            }
            catch (Exception e)
            {
                LemurFusionPlugin.LogDebug(e.ToString());
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void TryRegisterOption<T>(ConfigEntry<T> entry, bool restartRequired)
        {
            if (entry is ConfigEntry<string> stringEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.StringInputFieldOption(stringEntry, restartRequired), LemurFusionPlugin.PluginGUID, LemurFusionPlugin.PluginName);
                return;
            }
            if (entry is ConfigEntry<float> floatEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.SliderOption(floatEntry, new RiskOfOptions.OptionConfigs.SliderConfig()
                {
                    min = 0,
                    max = 20,
                    FormatString = "{0:0.00}",
                    restartRequired = restartRequired
                }), LemurFusionPlugin.PluginGUID, LemurFusionPlugin.PluginName);
                return;
            }
            if (entry is ConfigEntry<int> intEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.IntSliderOption(intEntry, restartRequired), LemurFusionPlugin.PluginGUID, LemurFusionPlugin.PluginName);
                return;
            }
            if (entry is ConfigEntry<bool> boolEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.CheckBoxOption(boolEntry, restartRequired), LemurFusionPlugin.PluginGUID, LemurFusionPlugin.PluginName);
                return;
            }
            if (entry is ConfigEntry<KeyboardShortcut> shortCutEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(shortCutEntry, restartRequired), LemurFusionPlugin.PluginGUID, LemurFusionPlugin.PluginName);
                return;
            }
            if (typeof(T).IsEnum)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.ChoiceOption(entry, restartRequired), LemurFusionPlugin.PluginGUID, LemurFusionPlugin.PluginName);
                return;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void TryRegisterOptionSlider<T>(ConfigEntry<T> entry, float min, float max, bool restartRequired)
        {
            if (entry is ConfigEntry<int> intEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.IntSliderOption(intEntry, new RiskOfOptions.OptionConfigs.IntSliderConfig()
                {
                    min = (int)min,
                    max = (int)max,
                    formatString = "{0:0.00}",
                    restartRequired = restartRequired
                }), LemurFusionPlugin.PluginGUID, LemurFusionPlugin.PluginName);
                return;
            }

            if (entry is ConfigEntry<float> floatEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.SliderOption(floatEntry, new RiskOfOptions.OptionConfigs.SliderConfig()
                {
                    min = min,
                    max = max,
                    FormatString = "{0:0.00}",
                    restartRequired = restartRequired
                }), LemurFusionPlugin.PluginGUID, LemurFusionPlugin.PluginName);
            }
        }
        #endregion
    }
}