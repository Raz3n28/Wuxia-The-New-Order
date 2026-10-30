using System;
using HarmonyLib;
using Spine;

namespace WuxiaMod.Game.Patches
{
    /// <summary>
    /// Roupas e armas do mod no boneco usam o sistema oficial de skins do jogo
    /// (Resources/Textures/role-customize.xml, gerado por tools/gen_spine.py). Aqui só o que o
    /// XML não alcança: o modelo do tronco (DG_KJ_000_M) tem uma segunda peça de costas
    /// ("00_back/body b2") que não está na tabela de peças, então ficava a arte do jogo por
    /// cima da nossa. Tiramos essa peça das nossas skins de tronco.
    /// </summary>
    [HarmonyPatch(typeof(SpineCombine), nameof(SpineCombine.characterCombine))]
    internal static class CostumePatch
    {
        private static bool _done;

        [HarmonyPostfix]
        private static void Postfix() { _done = false; Fix(); }

        /// <summary>Também chamado pelo WuxiaRuntime, caso a montagem tenha rodado antes dos patches.</summary>
        public static void Fix()
        {
            if (_done) return;
            try
            {
                var data = global::SpineLoader.getSpineLoaderBean("allrr.skel", "allrr.atlas", false, false, false, true)?.skeletonData;
                if (data == null) return;
                _done = true;
                var slot = data.FindSlot("00_back/body b2");
                if (slot == null) return;
                int n = 0;
                foreach (var skin in data.Skins)
                {
                    if (skin?.Name == null || !skin.Name.StartsWith("wuxia_")) continue;
                    if (!(skin.Name.EndsWith("_m") || skin.Name.EndsWith("_w"))) continue;
                    if (skin.GetAttachment(slot.Index, "body b") == null) continue;
                    skin.RemoveAttachment(slot.Index, "body b");
                    n++;
                }
                ModLog.Info($"Skins do mod: {n} troncos sem a peça extra de costas.");
            }
            catch (Exception ex) { ModLog.Error(ex, "ajustar skins do mod"); }
        }
    }
}
