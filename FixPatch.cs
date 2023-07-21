using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evacuation {
    [HarmonyPatch]
    public static class FixPatch {
        static bool _canBeDamaged = false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerResources), nameof(PlayerResources.ApplyInstantDamage))]
        public static bool PlayerResources_ApplyInstantDamage_Prefix(float damage, ref bool __result, PlayerResources __instance) {
            if(!_canBeDamaged) {
                //Evacuation.Log($"damage: {damage} gets to be zero, and the current health: {__instance._currentHealth}");
                __result = false;
                return false;
            }
            return true;
        }

//        [HarmonyPrefix]
//        [HarmonyPatch(typeof(PlayerAudioController), nameof(PlayerAudioController.PlayHazardFirstContactDamage))]
//        public static bool PlayerAudioController_PlayHazardFirstContactDamage_Prefix() {
//            Evacuation.Log($"PlayHazardFirstContactDamage is called");
//            return true;
//        }

//        [HarmonyPrefix]
//        [HarmonyPatch(typeof(PlayerAudioController), nameof(PlayerAudioController.UpdateHazardDamage))]
//        public static bool PlayerAudioController_UpdateHazardDamage_Prefix(float damage, HazardDetector hazardDetector) {
//            HazardVolume.HazardType latestHazardType = hazardDetector.GetLatestHazardType();
//            bool flag = damage > 0f && latestHazardType > HazardVolume.HazardType.NONE;
//            Evacuation.Log($"UpdateHazardDamage is called, damage: {damage}, flag: {flag}");
//            return true;
//        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerImpactAudio), nameof(PlayerImpactAudio.OnImpact))]
        public static bool PlayerImpactAudio_OnImpact_Prefix() {
            if(!_canBeDamaged) {
                //Evacuation.Log($"PlayerImpactAudio.OnImpact is ignored");
                return false;
            }
            return true;
        }

//        [HarmonyPrefix]
//        [HarmonyPatch(typeof(JetpackThrusterModel), nameof(JetpackThrusterModel.ActivateBoost))]
//        public static bool JetpackThrusterModel_ActivateBoost_Prefix() {
//            Evacuation.Log($"Jetpack boosted");
//            return true;
//        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ThrusterModel), nameof(ThrusterModel.GetLocalAcceleration))]
        public static void ThrusterModel_GetLocalAcceleration_Postfix(JetpackThrusterModel __instance) {
            if(_canBeDamaged) {
                return;
            }

            var sqrMagnitude = __instance._localAcceleration.sqrMagnitude;
            if(sqrMagnitude > 0.1f) {
                Evacuation.Log($"Thruster localAcceleration: {__instance._localAcceleration}");
                _canBeDamaged = true;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerState), nameof(PlayerState.OnPlayerDeath))]
        public static void PlayerState_OnPlayerDeath_Postfix() {
            Evacuation.Log("Player is dead now");
            _canBeDamaged = false;
        }
    }
}
