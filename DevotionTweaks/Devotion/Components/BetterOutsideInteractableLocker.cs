using HG;
using RoR2;
using RoR2.CharacterAI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace LemurFusion.Devotion.Components
{
    public class BetterOutsideInteractableLocker : RoR2.OutsideInteractableLocker
    {
        private new void OnEnable()
        {
            if (NetworkServer.active)
            {
                this.currentCoroutine = this.BetterChestLockCoroutine();
                this.updateTimer = this.updateInterval;
            }
        }

        // Token: 0x06002C78 RID: 11384 RVA: 0x000BE018 File Offset: 0x000BC218
        private IEnumerator BetterChestLockCoroutine()
        {
            Candidate[] candidates = new Candidate[64];
            if (DevotionInventoryController.isDevotionEnable)
            {
                List<LemurianEggController> instancesList = InstanceTracker.GetInstancesList<LemurianEggController>();
                Debug.Log("there are eggs " + instancesList.Count);
                foreach (LemurianEggController lemurianEggController in instancesList)
                {
                    this.eggLockInfoMap[lemurianEggController] = new LockInfo
                    {
                        lockObj = null,
                        distSqr = (lemurianEggController.transform.position - base.transform.position).sqrMagnitude
                    };
                }
            }
            for (; ; )
            {
                Vector3 position = base.transform.position;
                if (DevotionInventoryController.isDevotionEnable)
                {
                    foreach (var eggLockInfo in this.eggLockInfoMap)
                    {
                        // rewritten with (excessively) comprehensible names and logic.
                        float zoneSize = this.radius * this.radius;
                        float eggDistance = eggLockInfo.Value.distSqr;
                        bool isLocked = eggLockInfo.Value.IsLocked();

                        // is egg within our zone?
                        if (eggDistance <= zoneSize && isLocked)
                        {
                            // unlock inner egg
                            this.UnlockLemurianEgg(eggLockInfo.Key);
                        }
                        else if (eggDistance > zoneSize && !isLocked)
                        {
                            //lock outer egg
                            this.LockLemurianEgg(eggLockInfo.Key);
                        }
                        // Normally this works just fine for teleporters, however void seeds want the inverse.
                        // Since this.lockInside is never checked here, void seeds are treated the same as teleporters (lock outside, unlock inside)
                        // it should look something like this (but with less branching!)
                        bool lockInside = this.lockInside;
                        bool lockOutside = !this.lockInside;
                        bool isUnlocked = !isLocked;

                        // is the egg within our sphere?
                        if (eggDistance <= zoneSize)
                        {
                            // if this is a teleporter then we should unlock eggs within our sphere.
                            // if isn't already unlocked then do so now. this worked fine before.
                            // in fact, this code may have some edge cases. oh well, you get the point. 
                            if (lockOutside && isLocked)
                            {
                                this.UnlockLemurianEgg(eggLockInfo.Key);
                            }
                            // now the inverse, if this inner egg isnt locked but should be, (think void seed...) then lock it now.
                            // this case isn't covered by the original code! easy fix.
                            else if (lockInside && isUnlocked)
                            {
                                this.LockLemurianEgg(eggLockInfo.Key);
                            }
                        }
                        else // egg is outside our sphere. we now do the same thing as before but inverted.
                        {
                            // if an outer egg is unlocked but shouldn't be, (think teleporter...) then lock it now.
                            if (lockOutside && isUnlocked)
                            {
                                this.LockLemurianEgg(eggLockInfo.Key);
                            }
                            // and finally, if this distant egg is locked  isn't locked, then do so now.
                            else if (lockInside && isLocked)
                            {
                                this.UnlockLemurianEgg(eggLockInfo.Key);
                            }
                        }
                    }
                }
                List<PurchaseInteraction> instancesList2 = InstanceTracker.GetInstancesList<PurchaseInteraction>();
                var candidatesCount = instancesList2.Count;
                ArrayUtils.EnsureCapacity(ref candidates, candidatesCount);
                for (int k = 0; k < candidatesCount; k++)
                {
                    PurchaseInteraction purchaseInteraction = instancesList2[k];
                    candidates[k] = new Candidate
                    {
                        purchaseInteraction = purchaseInteraction,
                        distanceSqr = (purchaseInteraction.transform.position - position).sqrMagnitude
                    };
                }
                yield return null;

                Array.Sort(candidates, 0, candidatesCount, default);
                yield return null;

                for (int i = 0; i < candidatesCount; i++)
                {
                    PurchaseInteraction purchaseInteraction2 = candidates[i].purchaseInteraction;
                    if (purchaseInteraction2)
                    {
                        float num3 = this.radius * this.radius;
                        if (candidates[i].distanceSqr <= num3 != this.lockInside || !purchaseInteraction2.available)
                        {
                            this.UnlockPurchasable(purchaseInteraction2);
                        }
                        else
                        {
                            this.LockPurchasable(purchaseInteraction2);
                        }
                        yield return null;
                    }
                }
                yield return null;
            }
            yield break;
        }
    }
}
