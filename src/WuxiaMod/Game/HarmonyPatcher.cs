using System.Reflection;
using HarmonyLib;

namespace WuxiaMod.Game
{
    /// <summary>Aplica todos os [HarmonyPatch] desta DLL.</summary>
    internal static class HarmonyPatcher
    {
        public static Harmony Instance { get; private set; }

        public static void PatchAll(string id)
        {
            Instance = new Harmony(id);
            Instance.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
