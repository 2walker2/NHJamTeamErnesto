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
        static bool _wakeUp = false;
        static float _wakeLength = 1f;
        static bool _initialWakeUp = true;
        static Coroutine _fixRaftCoroutine;
        static Coroutine _fixPlayerSpawnPosition;

        const string RAFT_PLATFORM_PATH = "LayeredLagoon_Body/Sector/Prefab_NOM_SimpleChair_NoSkeleton (1)";
        const string RAFT_PATH = "LayeredLagoonRaft_Body";
        const string SLEEPING_BAG_PATH = "TheCampground_Body/Sector/Props_HEA_CampsiteSleepingBag";

        public static void Initialize() {
            LoadManager.OnCompleteSceneLoad += (scene, loadScene) => {
                if (loadScene == OWScene.SolarSystem) {
                    Evacuation.Log("SolarSystem is loaded");

                    if(_fixRaftCoroutine != null) {
                        Evacuation.Instance.StopCoroutine(_fixRaftCoroutine);
                        _fixRaftCoroutine = null;
                    }
                    _fixRaftCoroutine = Evacuation.Instance.StartCoroutine(FixRaft());

                    _fixPlayerSpawnPosition = Evacuation.Instance.StartCoroutine(FixPlayerSpawnPosition());
                }
                else if(loadScene == OWScene.TitleScreen) {
                    _initialWakeUp = true;
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

        static IEnumerator FixPlayerSpawnPosition() {
            Transform playerTransform = null;
            Rigidbody playerRigidbody = null;
            Transform sleepingBagTransform = null;

            float timeFromWakingUp = 0;

            Vector3 velocity = Vector3.zero;
            //Vector3 prevPosOfSleepingBag = Vector3.zero;
            Vector3 prevPosOfPlayer = Vector3.zero;

            while(true) {
                //yield return new WaitForEndOfFrame();
                yield return null;
                if(!playerTransform) {
                    var player = GameObject.FindObjectOfType<PlayerCharacterController>();
                    if(player) {
                        playerTransform = player.transform;
                        playerRigidbody = player.GetComponent<Rigidbody>();
                        prevPosOfPlayer = player.transform.position;
                        Evacuation.Log("player is found");
                    }
                }
                if(!sleepingBagTransform) {
                    var sleepingBag = GameObject.Find(SLEEPING_BAG_PATH);
                    if(sleepingBag) {
                        sleepingBagTransform = sleepingBag.transform;
                        //prevPosOfSleepingBag = sleepingBagTransform.position;
                        Evacuation.Log("sleeping bag is found");
                    }
                }
                if(!playerTransform || !sleepingBagTransform) {
                    continue;
                }

                //playerRigidbody.freezeRotation = true;
                //velocity = (sleepingBagTransform.position - prevPosOfSleepingBag) / Time.deltaTime;
                //velocity = (playerTransform.position - prevPosOfPlayer) / Time.fixedDeltaTime;
                velocity = (playerTransform.position - prevPosOfPlayer) / Time.deltaTime;
                prevPosOfPlayer = playerTransform.position;
                //Evacuation.Log($"velocity: {velocity}");

                //playerRigidbody.isKinematic = true;
                if(!float.IsInfinity(velocity.x) && !float.IsNaN(velocity.x)) {
                    playerRigidbody.velocity = velocity;
                }

                float coefficient;
                //playerTransform.transform.eulerAngles = new Vector3(0, 0, 0);
                if(_initialWakeUp) {
                    coefficient = 0.85f;
                }
                else {
                    coefficient = 0.9f;
                }
                playerTransform.position = sleepingBagTransform.transform.position + sleepingBagTransform.transform.up * coefficient;
                //playerTransform.parent = sleepingBagTransform;

                if(timeFromWakingUp > _wakeLength * 0.8f) {
                    //playerTransform.parent = null;
                    //playerRigidbody.isKinematic = false;
                    //playerRigidbody.freezeRotation = false;
                    yield break;
                }
                if(_wakeUp) {
                    //timeFromWakingUp += Time.fixedDeltaTime;
                    timeFromWakingUp += Time.deltaTime;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(RaftController), nameof(RaftController.OnPressInteract))]
        public static void RaftController_OnPressInteract_Prefix() {
            //Evacuation.Log("Raft is pushed");
            _isRaftPushed = true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerCameraEffectController), nameof(PlayerCameraEffectController.WakeUp))]
        public static void PlayerCameraEffectController_WakeUp_Prefix(PlayerCameraEffectController __instance) {
            _wakeUp = true;
            _wakeLength = __instance._wakeLength;
            Evacuation.Log($"Player is wake up. wakeLength: {_wakeLength}");
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
            _wakeUp = false;
            _initialWakeUp = false;

            //Evacuation.Instance.StopCoroutine(_fixRaftCoroutine); // this would move the raft when death (you can see it with closing your eyes)
            //_fixRaftCoroutine = null;

            Evacuation.Instance.StopCoroutine(_fixPlayerSpawnPosition);
            _fixPlayerSpawnPosition = null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DeathManager), nameof(DeathManager.KillPlayer))]
        public static bool DeathManager_KillPlayer_Prefix(DeathType deathType) {
            if(deathType == DeathType.Meditation) {
                return true;
            }
            if(_canBeDamaged) {
                return true;
            }
            return false;
        }
    }
}
