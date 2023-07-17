using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System.Reflection;

namespace NHJamTeamErnesto {
    public class NHJamTeamErnesto : ModBehaviour {
        public static NHJamTeamErnesto Instance;

        void Awake() {
            Instance = this;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }

        void Start() {
            ModHelper.Console.WriteLine($"{nameof(NHJamTeamErnesto)} is loaded!", MessageType.Success);
        }
    }
}