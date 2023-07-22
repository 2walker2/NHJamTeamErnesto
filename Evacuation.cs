using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System.Reflection;

namespace Evacuation {
    public class Evacuation : ModBehaviour {
        public static Evacuation Instance;

        public static void Log(string text) {
            Instance.ModHelper.Console.WriteLine(text);
        }

        void Awake() {
            Instance = this;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }

        void Start() {
            var newHorizonsAPI = ModHelper.Interaction.TryGetModApi<INewHorizons>("xen.NewHorizons");
            newHorizonsAPI.LoadConfigs(this);
            ModHelper.Console.WriteLine($"{nameof(Evacuation)} is loaded!", MessageType.Success);

            FixPatch.Initialize();
        }
    }
}