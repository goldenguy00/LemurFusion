using RoR2;
using System.Security;
using System.Security.Permissions;
using UnityEngine;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete

namespace LemurFusion
{
    public static class FuckMyAss
    {
        public static bool FuckingNullCheckNetId(CharacterMaster master, out NetworkUserId netId)
        {
            // this is how you correctly nullcheck in unity.
            // fucking kill me in the face man.
            netId = default;

            var minion = master.minionOwnership;
            if (!minion)
                return false;

            var ownerMaster = minion.ownerMaster;
            if (!ownerMaster)
                return false;

            var pCMC = ownerMaster.playerCharacterMasterController;
            if (!pCMC)
                return false;

            var networkUser = pCMC.networkUser;
            if (!networkUser)
                return false;

            netId = networkUser.id;
            return true;
        }

        public static bool FuckingNullCheckPing(CharacterMaster master, out GameObject target)
        {
            // this is how you correctly nullcheck in unity.
            // fucking kill me in the face man.
            target = null;

            if (!master)
                return false;

            var minion = master.minionOwnership;
            if (!minion)
                return false;

            var ownerMaster = minion.ownerMaster;
            if (!ownerMaster)
                return false;

            var pCMC = ownerMaster.playerCharacterMasterController;
            if (!pCMC)
                return false;

            var pCtrl = pCMC.pingerController;
            if (!pCtrl)
                return false;

            target = pCtrl.currentPing.targetGameObject;
            return pCtrl.currentPing.active && target;
        }

        public static bool FuckingNullCheckHurtBox(HurtBox hurtBox, out CharacterBody body, out Vector3 position)
        {
            body = null;
            position = Vector3.zero;

            if (!hurtBox)
                return false;

            var hp = hurtBox.healthComponent;
            if (!hp)
                return false;

            body = hp.body;
            if (!body)
                return false;

            position = hurtBox.transform.position;
            return body.characterMotor && !body.hasCloakBuff;
        }
    }
}