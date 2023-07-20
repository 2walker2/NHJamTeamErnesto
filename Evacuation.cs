using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System.Reflection;

namespace Evacuation {
    public class Evacuation : ModBehaviour {
        public static Evacuation Instance;

        void Awake() {
            Instance = this;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }

        void Start() {
            var newHorizonsAPI = ModHelper.Interaction.TryGetModApi<INewHorizons>("xen.NewHorizons");
            newHorizonsAPI.LoadConfigs(this);
            ModHelper.Console.WriteLine($"{nameof(Evacuation)} is loaded!", MessageType.Success);
        }
    }
}