using R2API;
using Risky_Artifacts.Artifacts;
using Risky_Artifacts;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DevotionTweaks
{
    internal class CrueltyElite
    {
        private static System.Random rng = new System.Random();

        public static void Shuffle<T>(this IEnumerable<T> list)
        {
            T[] elements = list.ToArray();
            for (int i = elements.Length - 1; i >= 0; i--)
            {
                // Swap element "i" with a random earlier element it (or itself)
                // ... except we don't really need to swap it fully, as we can
                // return it immediately, and afterwards it's irrelevant.
                int swapIndex = rng.Next(i + 1);
                yield return elements[swapIndex];
                elements[swapIndex] = elements[i];
            }
        }

        // moffein is literal jesus
        public static void CreateCrueltyElite(BetterLemurController lemCtrl, CharacterBody characterBody, Inventory inventory)
        {
            if (!lemCtrl || !characterBody)
                return;

            //Check amount of elite buffs the target has
            List<BuffIndex> currentEliteBuffs = [];
            foreach (var b in BuffCatalog.eliteBuffIndices)
            {
                if (characterBody.HasBuff(b) && !currentEliteBuffs.Contains(b))
                {
                    currentEliteBuffs.Add(b);
                }
            }

            int eliteBuffs = currentEliteBuffs.Count();
            int addedAffixes = 0;
            int t2Count = 0;
            int generalCount = 0;

            bool hasEquip = inventory.GetEquipmentIndex() != EquipmentIndex.None;

            List<EliteDef> selectedElites = new List<EliteDef>();

            //Roll for failure each time an affix is added.

            bool isT2 = false;
            int maxT2 = 2;
            int maxGeneral = 2;

            //Iterate through all elites, starting from the most expensive
            //Seems very inefficient
            var eliteTiersList = EliteAPI.GetCombatDirectorEliteTiers().ToList();
            eliteTiersList.Sort(Utils.CompareEliteTierCost);
            foreach (var etd in eliteTiersList)
            {
                //Super scuffed. Checking for the Elite Type directly didn't work.
                isT2 = false;
                if (etd.eliteTypes != null)
                {
                    foreach (EliteDef ed in etd.eliteTypes)
                    {
                        if (ed != null && (ed.eliteEquipmentDef == RoR2Content.Equipment.AffixPoison || ed.eliteEquipmentDef == RoR2Content.Equipment.AffixHaunted))
                        {
                            isT2 = true;
                            break;
                        }
                    }
                }

                IEnumerable<EliteDef> availableElitesInTier = etd.eliteTypes != null ? etd.eliteTypes : etd.availableDefs;

                if (availableElitesInTier?.Any() != true) continue;

                availableElitesInTier = availableElitesInTier.Where(def => def != null && !selectedElites.Contains(def)).Shuffle();

                foreach (EliteDef ed in availableElitesInTier)
                {
                    bool reachedTierLimit = (isT2 && maxT2 >= 0 && t2Count >= maxT2) || (!isT2 && maxGeneral >= 0 && generalCount >= maxGeneral);
                    if (reachedTierLimit) break;

                    //Check if EliteDef has an associated buff and the character doesn't already have the buff.
                    bool isBuffValid = ed && ed.eliteEquipmentDef
                        && ed.eliteEquipmentDef.passiveBuffDef
                        && ed.eliteEquipmentDef.passiveBuffDef.isElite
                        && !BlacklistedElites.Contains(ed.eliteEquipmentDef)
                        && !currentEliteBuffs.Contains(ed.eliteEquipmentDef.passiveBuffDef.buffIndex);
                    if (!isBuffValid) continue;

                    bool hasEnoughCredits = true;
                    switch (costScaling)
                    {
                        case ScalingMode.Multiplicative:
                            //Always calculate multiplicative off of the total credits to maintain consistency.
                            hasEnoughCredits = currentDirectorCredits - cardCost * totalCostMult * etd.costMultiplier >= 0f;
                            break;
                        case ScalingMode.Additive:
                            hasEnoughCredits = availableCredits - (cardCost * (etd.costMultiplier - 1f)) >= 0f;
                            break;
                        default:
                            hasEnoughCredits = true;
                            break;
                    }

                    if (hasEnoughCredits && ((affixCount > 0 && addedAffixes < affixCount) || (affixCount <= 0 && UnityEngine.Random.Range(1, 100) > Cruelty.failureChance)))
                    {
                        switch (costScaling)
                        {
                            case ScalingMode.Multiplicative:
                                //Always calculate multiplicative off of the total credits to maintain consistency.
                                availableCredits = currentDirectorCredits - cardCost * totalCostMult * etd.costMultiplier;
                                totalCostMult *= etd.costMultiplier;
                                break;
                            case ScalingMode.Additive:
                                availableCredits -= cardCost * (etd.costMultiplier - 1f);
                                break;
                            default:
                                break;
                        }

                        if (etd.costMultiplier > 1f)
                        {
                            switch (rewardScaling)
                            {
                                case ScalingMode.Multiplicative:
                                    deathRewardsMultiplier *= etd.costMultiplier;
                                    break;
                                case ScalingMode.Additive:
                                    deathRewardsAdd += etd.costMultiplier - 1f;
                                    break;
                                default:
                                    break;
                            }
                        }

                        //Fill in equipment slot if it isn't filled
                        if (!hasEquip && ed.eliteEquipmentDef)
                        {
                            inventory.SetEquipmentIndex(ed.eliteEquipmentDef.equipmentIndex);
                            hasEquip = true;
                        }

                        //Apply Elite Bonus
                        BuffIndex buff = ed.eliteEquipmentDef.passiveBuffDef.buffIndex;
                        currentEliteBuffs.Add(buff);
                        characterBody.AddBuff(buff);
                        addedAffixes++;
                        if (isT2)
                        {
                            t2Count++;
                        }
                        else
                        {
                            generalCount++;
                        }

                        if (isNotElite)
                        {
                            desiredDamageMult = ed.damageBoostCoefficient;
                            desiredHealthMult = ed.healthBoostCoefficient;
                            isNotElite = false;
                        }
                        else
                        {
                            switch (Cruelty.damageScaling)
                            {
                                case ScalingMode.Multiplicative:
                                    desiredDamageMult *= ed.damageBoostCoefficient;
                                    break;
                                case ScalingMode.Additive:
                                    desiredDamageMult += ed.damageBoostCoefficient - 1f;
                                    break;
                                default:
                                    break;
                            }

                            switch (Cruelty.healthScaling)
                            {
                                case ScalingMode.Multiplicative:
                                    desiredHealthMult *= ed.healthBoostCoefficient;
                                    break;
                                case ScalingMode.Additive:
                                    desiredHealthMult += ed.healthBoostCoefficient - 1f;
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }

            int boostDamagecount = Mathf.FloorToInt((desiredDamageMult - currentDamageMult) / 0.1f);
            inventory.GiveItem(RoR2Content.Items.BoostDamage, boostDamagecount);

            int boostHealthcount = Mathf.FloorToInt((desiredHealthMult - currentHealthMult) / 0.1f);
            inventory.GiveItem(RoR2Content.Items.BoostHp, boostHealthcount);

            if (Cruelty.rewardScaling != ScalingMode.None)
            {
                DeathRewards dr = characterBody.GetComponent<DeathRewards>();
                if (dr)
                {
                    float scaledXpRewards = (dr.expReward + dr.expReward * deathRewardsAdd) * deathRewardsMultiplier;
                    float scaledGoldRewards = (dr.goldReward + dr.goldReward * deathRewardsAdd) * deathRewardsMultiplier;

                    uint scaledXpFloor = (uint)Mathf.CeilToInt(scaledXpRewards);
                    uint scaledGoldFloor = (uint)Mathf.CeilToInt(scaledGoldRewards);

                    if (scaledXpFloor > dr.expReward) dr.expReward = scaledXpFloor;
                    if (scaledGoldFloor > dr.goldReward) dr.goldReward = scaledGoldFloor;
                }
            }

            return currentDirectorCredits - availableCredits;
        }
    }
}
