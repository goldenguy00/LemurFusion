using System.Collections.Generic;
using RoR2;
using UnityEngine;
using BepInEx.Configuration;

namespace LemurFusion.Config
{
    public class HeavyChanges
    {
        internal class BodyEvolutionStage
        {
            public string BodyName = "LemurianBody";
            public int EvolutionStage = -1;
        }

        public enum DeathPenalty : byte
        {
            TrueDeath,
            Devolve,
            ResetToBaby
        }

        internal static ConfigEntry<bool> Enable;
        internal static ConfigEntry<int> EvoMax;
        internal static ConfigEntry<string> Evo_BodyStages_Raw;
        internal static ConfigEntry<bool> DropEggOnDeath;
        internal static ConfigEntry<DeathPenalty> OnDeathPenalty;
        internal static ConfigEntry<bool> ImproveAI;
        internal static ConfigEntry<bool> CapEvo;

        internal static List<BodyEvolutionStage> Evo_BodyStages;
        internal static List<EliteDef> Elite_Blacklist;
        internal static ConfigEntry<string> Elite_Blacklist_Raw;

        internal static void PostLoad()
        {
            if (Enable.Value)
            {
                var itemString = Evo_BodyStages_Raw.Value.Split(',');
                Evo_BodyStages = [];
                for (int i = 0; i + 1 < itemString.Length; i += 2)
                {
                    if (int.TryParse(itemString[i + 1].Trim(), out var intResult) && intResult > 1)
                    {
                        Evo_BodyStages.Add(new()
                        {
                            BodyName = itemString[i].Trim(),
                            EvolutionStage = intResult
                        });
                    }
                    else
                    {
                        LemurFusionPlugin._logger.LogWarning(string.Format("{0}'{1}' Is being assigned to an invalid evolution stage, ignoring.", LemurFusionPlugin.PluginName, itemString[i]));
                    }
                }

                var Evo_BaseMaster = MasterCatalog.FindMasterPrefab(DevotionTweaks.masterPrefabName);
                if (Evo_BaseMaster)
                {
                    LemurFusionPlugin._logger.LogInfo("Set [" + Evo_BaseMaster.name + "] as base master form.");
                }
                else
                {
                    LemurFusionPlugin._logger.LogWarning(string.Format("{0} Could not find master for [{1}] this will cause errors.", LemurFusionPlugin.PluginName, DevotionTweaks.masterPrefabName));
                }

                CharacterMaster master = Evo_BaseMaster.GetComponent<CharacterMaster>();
                if (master)
                {
                    GameObject bodyPrefab = master.bodyPrefab;
                    if (bodyPrefab)
                    {
                        LemurFusionPlugin._logger.LogInfo("Base Master has [" + bodyPrefab.name + "] as base body form.");
                    }
                }
            }
        }
    }
}
