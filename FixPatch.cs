using HarmonyLib;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Evacuation {
    [HarmonyPatch]
    public static class FixPatch {
        static bool _canBeDamaged = false;
        static bool _isRaftPushed = false;
        static Coroutine _fixRaftCoroutine;

        const string RAFT_PLATFORM_PATH = "LayeredLagoon_Body/Sector/Prefab_NOM_SimpleChair_NoSkeleton (1)";
        const string RAFT_PATH = "LayeredLagoonRaft_Body";

        public static void Initialize() {
            LoadManager.OnCompleteSceneLoad += (scene, loadScene) => {
                if (loadScene == OWScene.SolarSystem) {
                    Evacuation.Log("SolarSystem is loaded");
                    _fixRaftCoroutine = Evacuation.Instance.StartCoroutine(FixRaft());
                }
            };
        }

        // The raft is fixed on the platform before using the jetpack.
        static IEnumerator FixRaft() {
            GameObject raftPlatform = null;
            GameObject raft = null;
            Rigidbody raftRigidbody = null;
            //var time = new WaitForFixedUpdate();
            while(true) {
                yield return null;
                if(!raftPlatform) {
                    raftPlatform = GameObject.Find(RAFT_PLATFORM_PATH);
                    if(raftPlatform) {
                        Evacuation.Log("raftPlatform is found");
                    }
                }
                if(!raft) {
                    raft = GameObject.Find(RAFT_PATH);
                    if(raft) {
                        Evacuation.Log("raft is found");
                        raftRigidbody = raft.GetComponent<Rigidbody>();
                    }
                }
                if(!raft || !raftPlatform) {
                    continue;
                }

                raftRigidbody.isKinematic = true;
                raft.transform.position = raftPlatform.transform.position + raftPlatform.transform.up * 7f;
                raft.transform.eulerAngles = new Vector3(90, 0, 0);

                //if(_canBeDamaged) {
                if(_isRaftPushed) {
                //if(Input.GetKeyDown(KeyCode.T)) {
                    raftRigidbody.isKinematic = false;
                    yield break;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(RaftController), nameof(RaftController.OnPressInteract))]
        public static void RaftController_OnPressInteract_Prefix() {
            Evacuation.Log("Raft is pushed");
            _isRaftPushed = true;
        }

        // Player does not get damaged before using their jetpack.
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

        // Player does not get damaged sound before using their jetpack.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerImpactAudio), nameof(PlayerImpactAudio.OnImpact))]
        public static bool PlayerImpactAudio_OnImpact_Prefix() {
            if(!_canBeDamaged) {
                //Evacuation.Log($"PlayerImpactAudio.OnImpact is ignored");
                return false;
            }
            return true;
        }

        // Check using their jetpack.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ThrusterModel), nameof(ThrusterModel.GetLocalAcceleration))]
        public static void ThrusterModel_GetLocalAcceleration_Postfix(JetpackThrusterModel __instance) {
            if(_canBeDamaged) {
                return;
            }

            var sqrMagnitude = __instance._localAcceleration.sqrMagnitude;
            if(sqrMagnitude > 0.1f) {
                //Evacuation.Log($"Thruster localAcceleration: {__instance._localAcceleration}");
                _canBeDamaged = true;
            }
        }

        // Reset some states.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerState), nameof(PlayerState.OnPlayerDeath))]
        public static void PlayerState_OnPlayerDeath_Postfix() {
            //Evacuation.Log("Player is dead now");
            _canBeDamaged = false;
            _isRaftPushed = false;
            Evacuation.Instance.StopCoroutine(_fixRaftCoroutine);
            _fixRaftCoroutine = null;
        }
    }
}
