using UnityEngine.AddressableAssets;
using UnityEngine;
using RoR2;
using System.Collections.Generic;
using System.Linq;

namespace LemurFusion.Devotion.Components
{
    public class MechaLemur
    {
        private static GameObject chaingunDisplay, launcherDisplay, robotArm;
        private static Material mechaMat, wolfMat;

        public static MechaLemur instance { get; private set; }

        public static void Init()
        {
            if (instance != null)
                return;

            instance = new MechaLemur();
        }

        public MechaLemur()
        {
            mechaMat = Addressables.LoadAssetAsync<Material>("RoR2/DLC1/DroneCommander/matDroneCommander.mat").WaitForCompletion();
            wolfMat = Addressables.LoadAssetAsync<Material>("RoR2/Base/AttackSpeedOnCrit/matWolfhatOverlay.mat").WaitForCompletion();
            robotArm = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/DroneWeapons/DisplayDroneWeaponRobotArm.prefab").WaitForCompletion();
            chaingunDisplay = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/DroneWeapons/DisplayDroneWeaponMinigun.prefab").WaitForCompletion();
            launcherDisplay = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/DroneWeapons/DisplayDroneWeaponLauncher.prefab").WaitForCompletion();
            
            SetupChildLocator();
            SetupBigChildLocator();

            PopulatePrefab(DevotionTweaks.instance.bodyPrefab);
            PopulateBigPrefab(DevotionTweaks.instance.bigBodyPrefab);

            On.RoR2.CharacterModel.UpdateOverlays += CharacterModel_UpdateOverlays;
        }
        private static void SetupChildLocator()
        {
            var loc = DevotionTweaks.instance.bodyPrefab.GetComponent<ModelLocator>().modelTransform.GetComponent<ChildLocator>();
            var root = loc.transform.Find("LemurianArm").Find("ROOT").Find("base");

            loc.transformPairs =
            [
            #region Base
                new ChildLocator.NameTransformPair()
                {
                    name = "Base",
                    transform = root
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "Hip",
                    transform = root.Find("hip")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "Pelvis",
                    transform = root.Find("hip")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "Stomach",
                    transform = root.Find("stomach")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "Chest",
                    transform = root.Find("stomach/chest")
                },
            #endregion

            #region Head
                new ChildLocator.NameTransformPair()
                {
                    name = "Head",
                    transform = root.Find("stomach/chest/neck_low/neck_high/head")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "Jaw",
                    transform = root.Find("stomach/chest/neck_low/neck_high/head/jaw")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "MuzzleMouth",
                    transform = root.Find("stomach/chest/neck_low/neck_high/head/MuzzleMouth")
                },
            #endregion

            #region Arms
                new ChildLocator.NameTransformPair()
                {
                    name = "ShoulderL",
                    transform = root.Find("stomach/chest/shoulder.l")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "ShoulderR",
                    transform = root.Find("stomach/chest/shoulder.r")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "UpperArmL",
                    transform = root.Find("stomach/chest/shoulder.l/upper_arm.l")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "UpperArmR",
                    transform = root.Find("stomach/chest/shoulder.r/upper_arm.r")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "LowerArmL",
                    transform = root.Find("stomach/chest/shoulder.l/upper_arm.l/lower_arm.l")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "LowerArmR",
                    transform = root.Find("stomach/chest/shoulder.r/upper_arm.r/lower_arm.r")
                },
            #endregion

            #region Hands
                new ChildLocator.NameTransformPair()
                {
                    name = "HandL",
                    transform = root.Find("stomach/chest/shoulder.l/upper_arm.l/lower_arm.l")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "HandR",
                    transform = root.Find("stomach/chest/shoulder.r/upper_arm.r/lower_arm.r")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "Finger11L",
                    transform = root.Find("stomach/chest/shoulder.l/upper_arm.l/lower_arm.l/finger1.l")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "Finger21L",
                    transform = root.Find("stomach/chest/shoulder.l/upper_arm.l/lower_arm.l/finger2.l")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "Finger11R",
                    transform = root.Find("stomach/chest/shoulder.r/upper_arm.r/lower_arm.r/finger1.r")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "Finger21R",
                    transform = root.Find("stomach/chest/shoulder.r/upper_arm.r/lower_arm.r/finger2.r")
                },
            #endregion

            #region Legs
                new ChildLocator.NameTransformPair()
                {
                    name = "ThighR",
                    transform = root.Find("hip/thigh.r")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "ThighL",
                    transform = root.Find("hip/thigh.l")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "CalfR",
                    transform = root.Find("hip/thigh.r/calf.r")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "CalfL",
                    transform = root.Find("hip/thigh.l/calf.l")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "FootL",
                    transform = root.Find("hip/thigh.l/calf.l/foot.l/toe.l")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "FootR",
                    transform = root.Find("hip/thigh.r/calf.r/foot.r/toe.r")
                },
            #endregion
            ];
        }
        private static void SetupBigChildLocator()
        {
            var loc = DevotionTweaks.instance.bigBodyPrefab.GetComponent<ModelLocator>().modelTransform.GetComponent<ChildLocator>();
            var root = loc.transform.Find("LemurianBruiserArmature").Find("ROOT").Find("base");
            var pairs = loc.transformPairs.ToArray();
            loc.transformPairs =
            [.. pairs,
            #region Base
                new ChildLocator.NameTransformPair()
                {
                    name = "Base",
                    transform = root
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "Hip",
                    transform = root.Find("hip")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "Stomach",
                    transform = root.Find("stomach")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "SpineChest3",
                    transform = root.Find("stomach/chest/neck.1")
                },
            #endregion

            #region Arms
                new ChildLocator.NameTransformPair()
                {
                    name = "ClavicleL",
                    transform = root.Find("stomach/chest/clavicle.l")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "ClavicleR",
                    transform = root.Find("stomach/chest/clavicle.r")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "UpperArmL",
                    transform = root.Find("stomach/chest/clavicle.l/upper_arm.l")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "UpperArmR",
                    transform = root.Find("stomach/chest/clavicle.r/upper_arm.r")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "LowerArmL",
                    transform = root.Find("stomach/chest/clavicle.l/upper_arm.l/lower_arm.l")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "LowerArmR",
                    transform = root.Find("stomach/chest/clavicle.r/upper_arm.r/lower_arm.r")
                },
            #endregion

            #region Hands
                new ChildLocator.NameTransformPair()
                {
                    name = "HandL",
                    transform = root.Find("stomach/chest/clavicle.l/upper_arm.l/lower_arm.l/hand.l")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "HandR",
                    transform = root.Find("stomach/chest/clavicle.r/upper_arm.r/lower_arm.r/hand.r")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "Finger21L",
                    transform = root.Find("stomach/chest/clavicle.l/upper_arm.l/lower_arm.l/hand.l/finger2.l")
                },
            #endregion

            #region Legs
                new ChildLocator.NameTransformPair()
                {
                    name = "ThighR",
                    transform = root.Find("hip/thigh.r")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "ThighL",
                    transform = root.Find("hip/thigh.l")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "CalfR",
                    transform = root.Find("hip/thigh.r/calf.r")
                },
                new ChildLocator.NameTransformPair()
                {
                    name = "CalfL",
                    transform = root.Find("hip/thigh.l/calf.l")
                },
            #endregion
            ];
        }

        private static List<ItemDisplayRuleSet.KeyAssetRuleGroup> PopulateFromBody(CharacterModel model, bool useCommando = true)
        {
            var keyAssetRuleGroups = Resources.Load<GameObject>("Prefabs/CharacterBodies/CommandoBody").GetComponent<ModelLocator>().modelTransform.GetComponent<CharacterModel>().itemDisplayRuleSet.keyAssetRuleGroups;
            var lemGroups = model.itemDisplayRuleSet.keyAssetRuleGroups.ToList();
            var loc = model.GetComponent<ChildLocator>();
            var pairs = loc.transformPairs.Select(pair => pair.name).ToList();

            if (useCommando)
            {
                LemurFusionPlugin.LogError("From commando");
                for (int i = 0; i < keyAssetRuleGroups.Length; i++)
                {
                    var group = keyAssetRuleGroups[i];
                    if (!group.displayRuleGroup.isEmpty && !model.itemDisplayRuleSet.keyAssetRuleGroups.Any(x => x.keyAsset == group.keyAsset))
                    {
                        var rule = group.displayRuleGroup.rules.FirstOrDefault();
                        var childName = rule.childName;
                        if (string.IsNullOrEmpty(childName) || !loc.FindChild(childName) || childName is "Base" or "Stomach" or "Chest" or "GunL" or "GunR")
                        {
                            if (!(group.keyAsset.name is "Bear" or "BearVoid" or "KnockBackHitEnemies"))
                                continue;
                           // LemurFusionPlugin.LogError($"Skipping item {group.keyAsset?.name} :: {childName}");
                        }
                        var rules = new ItemDisplayRule[group.displayRuleGroup.rules.Length];
                        for (int j = 0; j < rules.Length; j++)
                        {
                            rule = group.displayRuleGroup.rules[j];
                            rules[j] = new()
                            {
                                localScale = rule.localScale * 10f,
                                localPos = rule.localPos * 10f,
                                localAngles = rule.localAngles,
                                childName = rule.childName,
                                followerPrefab = rule.followerPrefab,
                                limbMask = rule.limbMask,
                                ruleType = rule.ruleType
                            };
                            pairs.Remove(rule.childName);
                        }

                        lemGroups.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup()
                        {
                            keyAsset = group.keyAsset,
                            displayRuleGroup = new DisplayRuleGroup()
                            {
                                rules = rules
                            }
                        });
                    }
                }
            }
            keyAssetRuleGroups = Resources.Load<GameObject>("Prefabs/CharacterBodies/CrocoBody").GetComponent<ModelLocator>().modelTransform.GetComponent<CharacterModel>().itemDisplayRuleSet.keyAssetRuleGroups;
            List<string> missingPairs = [];
            LemurFusionPlugin.LogError("From croc");
            for (int i = 0; i < keyAssetRuleGroups.Length; i++)
            {
                var group = keyAssetRuleGroups[i];
                if (!group.displayRuleGroup.isEmpty && !lemGroups.Any(x => x.keyAsset == group.keyAsset))
                {
                    var rule = group.displayRuleGroup.rules.FirstOrDefault();
                    var childName = rule.childName;
                    if (string.IsNullOrEmpty(childName))
                    {
                        LemurFusionPlugin.LogError($"Skipping item {group.keyAsset?.name} :: {childName}");
                    }
                    else
                    {
                        var rules = new ItemDisplayRule[group.displayRuleGroup.rules.Length];
                        for (int j = 0; j < rules.Length; j++)
                        {
                            rule = group.displayRuleGroup.rules[j];
                            rules[j] = new()
                            {
                                localScale = rule.localScale,
                                localPos = rule.localPos,
                                localAngles = rule.localAngles,
                                childName = rule.childName,
                                followerPrefab = rule.followerPrefab,
                                limbMask = rule.limbMask,
                                ruleType = rule.ruleType
                            };

                            if (!loc.FindChild(rule.childName))
                            {
                                LemurFusionPlugin.LogError($"Missing child {group.keyAsset?.name} :: {rule.childName}, adding anyways");
                                missingPairs.Add(childName);
                            }
                            else
                            {
                                //LemurFusionPlugin.LogError($"Adding missing asset for item {group.keyAsset?.name} :: {childName}");
                                pairs.Remove(childName);
                            }
                        }

                        lemGroups.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup()
                        {
                            keyAsset = group.keyAsset,
                            displayRuleGroup = new DisplayRuleGroup()
                            {
                                rules = rules
                            }
                        });
                    }
                }
            }
            /*
            LemurFusionPlugin.LogError("****************************************** UNUSUED");
            foreach (var pair in pairs)
            {
                LemurFusionPlugin.LogError(pair);
            }
            LemurFusionPlugin.LogError("****************************************** NEED");
            foreach (var pair in missingPairs.Distinct())
            {
                LemurFusionPlugin.LogError(pair);
            }*/
            return lemGroups;
        }

        private static void PopulatePrefab(GameObject prefab)
        {
            var model = prefab.GetComponent<ModelLocator>().modelTransform.GetComponent<CharacterModel>();
            var itemDisplayRules = PopulateFromBody(model);

            itemDisplayRules.AddRange(
            [
                new ItemDisplayRuleSet.KeyAssetRuleGroup
                {
                    keyAsset = Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC1/DroneWeapons/DroneWeaponsDisplay1.asset").WaitForCompletion(),
                    displayRuleGroup = new DisplayRuleGroup
                    {
                        rules =
                        [
                            new ItemDisplayRule
                            {
                                ruleType = ItemDisplayRuleType.ParentedPrefab,
                                followerPrefab = chaingunDisplay,
                                limbMask = LimbFlags.None,
                                childName = "Chest",
                                localPos = new Vector3(0F, 2F, 1.3F),
                                localAngles = new Vector3(0F, 270F, 122F),
                                localScale = new Vector3(3F, 3F, 3F)
                            }
                        ]
                    }
                },
                new ItemDisplayRuleSet.KeyAssetRuleGroup
                {
                    keyAsset = Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC1/DroneWeapons/DroneWeaponsDisplay2.asset").WaitForCompletion(),
                    displayRuleGroup = new DisplayRuleGroup
                    {
                        rules =
                        [
                            new ItemDisplayRule
                            {
                                ruleType = ItemDisplayRuleType.ParentedPrefab,
                                followerPrefab = launcherDisplay,
                                limbMask = LimbFlags.None,
                                childName = "Chest",
                                localPos = new Vector3(0F, 0.6F, 1F),
                                localAngles = new Vector3(15F, 0F, 0F),
                                localScale = new Vector3(4F, 4F, 4F)
                            }
                        ]
                    }
                },
                new ItemDisplayRuleSet.KeyAssetRuleGroup
                {
                    keyAsset = Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC1/DroneWeapons/DroneWeaponsBoost.asset").WaitForCompletion(),
                    displayRuleGroup = new DisplayRuleGroup
                    {
                        rules =
                        [
                            new ItemDisplayRule
                            {
                                ruleType = ItemDisplayRuleType.ParentedPrefab,
                                followerPrefab = robotArm,
                                limbMask = LimbFlags.None,
                                childName = "MuzzleMouth",
                                localPos = new Vector3(0.75F, 0.68F, -1.1F),
                                localAngles = new Vector3(50F, 234F, 314F),
                                localScale = new Vector3(4.8F, 4.8F, 4.8F)
                            }
                        ]
                    }
                },/*
                new ItemDisplayRuleSet.KeyAssetRuleGroup
                {
                    keyAsset = Sandswept.Items.Greens.NuclearSalvo.instance.ItemDef,
                    displayRuleGroup = new DisplayRuleGroup
                    {
                        rules =
                        [
                            new ItemDisplayRule
                            {
                                ruleType = ItemDisplayRuleType.ParentedPrefab,
                                followerPrefab = Sandswept.Items.Greens.NuclearSalvo.instance.SalvoPrefab,
                                limbMask = LimbFlags.None,
                                childName = "MuzzleMouth",
                                localPos = new Vector3(0.75F, 0.68F, -1.1F),
                                localAngles = new Vector3(50F, 234F, 314F),
                                localScale = new Vector3(4.8F, 4.8F, 4.8F)
                            }
                        ]
                    }
                }*/
            ]);

            UpdateDisplayRule(itemDisplayRules, "Behemoth", new ItemDisplayRule()
            {
                childName = "Head",
                localPos = new Vector3(0F, 2F, 2F),
                localAngles = new Vector3(0F, 0F, 0F),
                localScale = new Vector3(0.8F, 0.8F, 0.8F)
            });
            UpdateDisplayRule(itemDisplayRules, "ArmorReductionOnHit", new ItemDisplayRule()
            {
                childName = "MuzzleMouth",
                localPos = new Vector3(2.9F, 0.5F, -2F),
                localAngles = new Vector3(1.5F, 93.3F, 13.4F),
                localScale = new Vector3(2F, 2F, 2F)
            });
            UpdateDisplayRule(itemDisplayRules, "AttackSpeedOnCrit", new ItemDisplayRule()
            {
                childName = "Head",
                localPos = new Vector3(0, 1.45F, -0.5F),
                localAngles = new Vector3(275F, 200F, 158F),
                localScale = new Vector3(7.5F, 7.5F, 7.5F)
            });
            UpdateDisplayRule(itemDisplayRules, "BonusGoldPackOnKill", new ItemDisplayRule()
            {
                childName = "Chest",
                localPos = new Vector3(0F, -0.5F, 2.3F),
                localAngles = new Vector3(30F, 0F, 0F),
                localScale = new Vector3(0.8F, 0.8F, 0.8F)
            });
            UpdateDisplayRule(itemDisplayRules, "BounceNearby", new ItemDisplayRule()
            {
                childName = "Chest",
                localPos = new Vector3(0F, 1.7F, 3F),
                localAngles = new Vector3(40.2F, 48.8F, 327.3F),
                localScale = new Vector3(1F, 1F, 1F)
            });
            UpdateDisplayRule(itemDisplayRules, "CritDamage", new ItemDisplayRule()
            {
                childName = "MuzzleMouth",
                localPos = new Vector3(-2F, 0.5F, -2F),
                localAngles = new Vector3(0F, 0F, 343.6F),
                localScale = new Vector3(1F, 1F, 1F)
            });
            UpdateDisplayRule(itemDisplayRules, "CritGlasses", new ItemDisplayRule()
            {
                childName = "MuzzleMouth",
                localPos = new Vector3(0F, 1.5F, -0.6F),
                localAngles = new Vector3(0F, 0F, 0F),
                localScale = new Vector3(2.3F, 2.3F, 2.3F)
            });
            UpdateDisplayRule(itemDisplayRules, "Crowbar", new ItemDisplayRule()
            {
                childName = "Chest",
                localPos = new Vector3(-1.1F, -0.4F, 2.3F),
                localAngles = new Vector3(7.2F, 76.7F, 34.2F),
                localScale = new Vector3(3F, 3F, 3F)
            });
            UpdateDisplayRule(itemDisplayRules, "DelayedDamage", "Stomach");
            UpdateDisplayRule(itemDisplayRules, "EquipmentMagazine", new ItemDisplayRule()
            {
                childName = "Chest",
                localPos = new Vector3(0F, 0.4F, 2.8F),
                localAngles = new Vector3(0F, 91.3F, 0.4F),
                localScale = new Vector3(1.5F, 1.5F, 1.5F)
            });
            UpdateDisplayRule(itemDisplayRules, "EquipmentMagazineVoid", new ItemDisplayRule()
            {
                childName = "Chest",
                localPos = new Vector3(0F, 0.4F, 2.8F),
                localAngles = new Vector3(0F, 91.3F, 0.4F),
                localScale = new Vector3(1.5F, 1.5F, 1.5F)
            });
            UpdateDisplayRule(itemDisplayRules, "ExtraLife", new ItemDisplayRule()
            {
                childName = "Chest",
                localPos = new Vector3(1.1F, 2.9F, 1.8F),
                localAngles = new Vector3(296.1F, 152.7F, 8.5F),
                localScale = new Vector3(2F, 2F, 2F)
            });
            UpdateDisplayRule(itemDisplayRules, "ExtraLifeVoid", new ItemDisplayRule()
            {
                childName = "Head",
                localPos = new Vector3(0F, 3F, -1F),
                localAngles = new Vector3(284.1F, 173.6F, 196.4F),
                localScale = new Vector3(2F, 2F, 2F)
            });
            UpdateDisplayRule(itemDisplayRules, "ExtraShrineItem", new ItemDisplayRule()
            {
                childName = "Hip",
                localPos = new Vector3(2.4F, 2.2F, 1F),
                localAngles = new Vector3(7.9F, 26.5F, 173.9F),
                localScale = new Vector3(1.4F, 1.4F, 1.4F)
            });
            UpdateDisplayRule(itemDisplayRules, "ExtraShrineItem", new ItemDisplayRule()
            {
                childName = "Hip",
                localPos = new Vector3(2.4F, 2.2F, 1F),
                localAngles = new Vector3(7.9F, 26.5F, 173.9F),
                localScale = new Vector3(1.4F, 1.4F, 1.4F)
            });
            UpdateDisplayRule(itemDisplayRules, "ExtraStatsOnLevelUp", "UpperArmL");
            UpdateDisplayRule(itemDisplayRules, "FallBoots", new ItemDisplayRule()
            {
                childName = "FootL",
                localPos = new Vector3(0.1F, -0.9F, -0.9F),
                localAngles = new Vector3(274.9F, 244.6F, 121.6F),
                localScale = new Vector3(2F, 2F, 2F)
            }, 
            new ItemDisplayRule()
            {
                childName = "FootR",
                localPos = new Vector3(-0.1F, -0.9F, -0.7F),
                localAngles = new Vector3(273.7F, 282F, 78.4F),
                localScale = new Vector3(2F, 2F, 2F)
            });
            UpdateDisplayRule(itemDisplayRules, "FlatHealth", new ItemDisplayRule()
            {
                childName = "Head",
                localPos = new Vector3(0.7F, 3.3F, 0.4F),
                localAngles = new Vector3(360F, 165.4F, 192.7F),
                localScale = new Vector3(1F, 1F, 1F)
            });
            UpdateDisplayRule(itemDisplayRules, "GhostOnKill", new ItemDisplayRule()
            {
                childName = "Head",
                localPos = new Vector3(0F, 2.8F, -0.1F),
                localAngles = new Vector3(285.8F, 183.9F, 175.5F),
                localScale = new Vector3(6F, 6F, 6F)
            });
            UpdateDisplayRule(itemDisplayRules, "GoldOnHit", new ItemDisplayRule()
            {
                childName = "Head",
                localPos = new Vector3(0.1F, -1.7F, -0.1F),
                localAngles = new Vector3(8.5F, 0F, 0F),
                localScale = new Vector3(11.5F, 11.5F, 11.5F)
            });
            UpdateDisplayRule(itemDisplayRules, "HealOnCrit", new ItemDisplayRule()
            {
                childName = "Chest",
                localPos = new Vector3(1.6F, 1.7F, 3.1F),
                localAngles = new Vector3(285.8F, 28.9F, 243.1F),
                localScale = new Vector3(2F, 2F, 2F)
            });

            model.itemDisplayRuleSet.keyAssetRuleGroups = [.. itemDisplayRules];
        }

        private static void UpdateDisplayRule(List<ItemDisplayRuleSet.KeyAssetRuleGroup> rules, string key, ItemDisplayRule newRule)
        {
            var group = rules.First(x => x.keyAsset.name == key).displayRuleGroup;
            newRule.followerPrefab = group.rules[0].followerPrefab;
            newRule.limbMask = group.rules[0].limbMask;
            newRule.ruleType = group.rules[0].ruleType;

            group.rules[0] = newRule;
        }
        private static void UpdateDisplayRule(List<ItemDisplayRuleSet.KeyAssetRuleGroup> rules, string key, ItemDisplayRule newRule, ItemDisplayRule newRule2)
        {
            var group = rules.First(x => x.keyAsset.name == key).displayRuleGroup;
            newRule.followerPrefab = group.rules[0].followerPrefab;
            newRule.limbMask = group.rules[0].limbMask;
            newRule.ruleType = group.rules[0].ruleType;

            group.rules[0] = newRule;
            
            newRule2.followerPrefab = group.rules[1].followerPrefab;
            newRule2.limbMask = group.rules[1].limbMask;
            newRule2.ruleType = group.rules[1].ruleType;

            group.rules[1] = newRule2;
        }
        private static void UpdateDisplayRule(List<ItemDisplayRuleSet.KeyAssetRuleGroup> rules, string key, string newParent)
        {
            var group = rules.First(x => x.keyAsset.name == key).displayRuleGroup;

            var newRule = group.rules[0];
            newRule.childName = newParent;

            group.rules[0] = newRule;
        }

        private static void PopulateBigPrefab(GameObject prefab)
        {
            var model = prefab.GetComponent<ModelLocator>().modelTransform.GetComponent<CharacterModel>();
            var itemDisplayRules = PopulateFromBody(model, false);

            itemDisplayRules.AddRange(
            [
                new ItemDisplayRuleSet.KeyAssetRuleGroup
                {
                    keyAsset = Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC1/DroneWeapons/DroneWeaponsDisplay1.asset").WaitForCompletion(),
                    displayRuleGroup = new DisplayRuleGroup
                    {
                        rules =
                        [
                            new ItemDisplayRule
                            {
                                ruleType = ItemDisplayRuleType.ParentedPrefab,
                                followerPrefab = chaingunDisplay,
                                limbMask = LimbFlags.None,
                                childName = "Chest",
                                localPos = new Vector3(0F, 2F, 1.3F),
                                localAngles = new Vector3(0F, 270F, 122F),
                                localScale = new Vector3(3F, 3F, 3F)
                            }
                        ]
                    }
                },
                new ItemDisplayRuleSet.KeyAssetRuleGroup
                {
                    keyAsset = Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC1/DroneWeapons/DroneWeaponsDisplay2.asset").WaitForCompletion(),
                    displayRuleGroup = new DisplayRuleGroup
                    {
                        rules =
                        [
                            new ItemDisplayRule
                            {
                                ruleType = ItemDisplayRuleType.ParentedPrefab,
                                followerPrefab = launcherDisplay,
                                limbMask = LimbFlags.None,
                                childName = "Chest",
                                localPos = new Vector3(0F, 0.6F, 1F),
                                localAngles = new Vector3(15F, 0F, 0F),
                                localScale = new Vector3(4F, 4F, 4F)
                            }
                        ]
                    }
                },
                new ItemDisplayRuleSet.KeyAssetRuleGroup
                {
                    keyAsset = Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC1/DroneWeapons/DroneWeaponsBoost.asset").WaitForCompletion(),
                    displayRuleGroup = new DisplayRuleGroup
                    {
                        rules =
                        [
                            new ItemDisplayRule
                            {
                                ruleType = ItemDisplayRuleType.ParentedPrefab,
                                followerPrefab = robotArm,
                                limbMask = LimbFlags.None,
                                childName = "MuzzleMouth",
                                localPos = new Vector3(0.75F, 0.68F, -1.1F),
                                localAngles = new Vector3(50F, 234F, 314F),
                                localScale = new Vector3(4.8F, 4.8F, 4.8F)
                            }
                        ]
                    }
                },/*
                new ItemDisplayRuleSet.KeyAssetRuleGroup
                {
                    keyAsset = Sandswept.Items.Greens.NuclearSalvo.instance.ItemDef,
                    displayRuleGroup = new DisplayRuleGroup
                    {
                        rules =
                        [
                            new ItemDisplayRule
                            {
                                ruleType = ItemDisplayRuleType.ParentedPrefab,
                                followerPrefab = Sandswept.Items.Greens.NuclearSalvo.instance.SalvoPrefab,
                                limbMask = LimbFlags.None,
                                childName = "MuzzleMouth",
                                localPos = new Vector3(0.75F, 0.68F, -1.1F),
                                localAngles = new Vector3(50F, 234F, 314F),
                                localScale = new Vector3(4.8F, 4.8F, 4.8F)
                            }
                        ]
                    }
                }*/
            ]);

            model.itemDisplayRuleSet.keyAssetRuleGroups = [.. itemDisplayRules];
        }

        private void CharacterModel_UpdateOverlays(On.RoR2.CharacterModel.orig_UpdateOverlays orig, CharacterModel self)
        {
            orig.Invoke(self);
            if (self.materialsDirty && Utils.IsDevoted(self.body) && self.body.inventory.GetItemCount(DLC1Content.Items.DroneWeaponsBoost) > 0)
            {
                if (self.activeOverlayCount < CharacterModel.maxOverlays)
                    self.currentOverlays[self.activeOverlayCount++] = wolfMat;

                if (self.activeOverlayCount < CharacterModel.maxOverlays)
                    self.currentOverlays[self.activeOverlayCount++] = mechaMat;
            }
        }
    }
}
