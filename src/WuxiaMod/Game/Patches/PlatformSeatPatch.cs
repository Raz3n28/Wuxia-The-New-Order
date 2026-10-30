using System;
using HarmonyLib;

namespace WuxiaMod.Game.Patches
{
    /// <summary>
    /// Colono senta EM CIMA da almofada e das plataformas, não ao lado.
    ///
    /// Como o jogo faz (lido do Assembly-CSharp): lazer usa PlayerBuild_Base.tryUseFurniture,
    /// que pede BaseBuild.getUseBuildPos. Ela devolve a primeira casa livre de
    /// BuildTools.getBuildPos, que é o anel de casas EM VOLTA da construção. Por isso o
    /// colono ficava do lado. Para as nossas construções de cultivo devolvemos a casa do
    /// meio da construção; no def.xml elas têm PersonPassable 1 para dar para pisar.
    /// Para sair, getLeavePos já procura uma casa livre do anel.
    /// </summary>
    [HarmonyPatch(typeof(BaseBuild), nameof(BaseBuild.getUseBuildPos))]
    internal static class PlatformSeatPatch
    {
        internal static bool IsSeat(BaseDef def)
        {
            string id = MeditationPatch.OurId(def);
            return id == MeditationPatch.CushionId || id == MeditationPatch.PlatformId || id == MeditationPatch.BreakPlatformId;
        }

        [HarmonyPrefix]
        private static bool Prefix(BaseBuild __instance, BaseRole role, ref FairyPoint __result)
        {
            try
            {
                if (!IsSeat(__instance.buildDef) || role == null) return true;
                BuildTools.getSpaceCellRange(__instance.buildDef, __instance.buildDir, __instance.cellX, __instance.cellY,
                    out int rx0, out int ry0, out int rx1, out int ry1);
                int cx = (rx0 + rx1) / 2, cy = (ry0 + ry1) / 2;
                if (!role.canPassable(cx, cy)) return true;
                __result = new FairyPoint(cx, cy);
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Error(ex, "lugar de sentar na plataforma");
                return true;
            }
        }
    }
}
