using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace WuxiaMod.Game.Patches
{
    /// <summary>
    /// Imagens das nossas construções no mapa (caldeirões).
    ///
    /// Como o jogo faz (lido do Assembly-CSharp):
    /// - Menu de construção e prévia verde (BuildPlanMod) usam o RawIcon da linha do def.xml.
    /// - A construção pronta (BuildAnim_Player) usa sempre o modelo do jogo pelo ModuleId,
    ///   que copiamos da bancada de remédios (DependID 51). Por isso ela virava a mesa do jogo.
    ///   Aqui, para as linhas nossas com RawIcon, desenhamos o RawIcon no lugar do modelo,
    ///   do mesmo jeito que a prévia (centrado, no tamanho do PNG).
    /// - A prévia gira a imagem com a direção da construção; virada (180°) a imagem fica de
    ///   cabeça para baixo. Os caldeirões são quadrados, então mostramos sempre de pé.
    /// </summary>
    [HarmonyPatch]
    internal static class BuildVisualsPatch
    {
        private const string DrawerName = "WuxiaBuildSprite";
        private static readonly Dictionary<Texture, Material> Materials = new Dictionary<Texture, Material>();

        private static bool IsOurs(BaseDef def) =>
            def?.mod != null && def.mod.packageId == ModEntry.HarmonyId && def.Sprite_RawIcon != null;

        // ---------- prévia ----------

        [HarmonyPatch(typeof(BuildPlanMod), nameof(BuildPlanMod.casePaint))]
        [HarmonyPostfix]
        private static void PlanCasePaint_Postfix(BuildPlanMod __instance)
        {
            try
            {
                if (!IsOurs(__instance.defBean)) return;
                var angle = Traverse.Create(__instance).Field("angle");
                if (Mathf.Approximately(Mathf.Abs(angle.GetValue<float>()), 180f)) angle.SetValue(0f);
            }
            catch (Exception ex) { ModLog.Error(ex, "prévia da construção"); }
        }

        // ---------- construção pronta ----------

        [HarmonyPatch(typeof(BuildAnim_Player), nameof(BuildAnim_Player.paint))]
        [HarmonyPrefix]
        private static bool BuildPaint_Prefix(BuildAnim_Player __instance)
        {
            try
            {
                var t = Traverse.Create(__instance);
                var def = t.Field("buildDef").GetValue<BaseDef>();
                if (!IsOurs(def)) return true;
                var build = t.Field("build").GetValue<BaseBuild>();

                var drawer = GetDrawer(__instance, def);
                var body = drawer.getGroupRender().getMeshBody();
                if (build == null || build.isPutdown()) body.setColor(1f, 1f, 1f, 1f);
                else if (build.couldPutdown()) body.setColor(BuildAnim_Player.greenColor);
                else body.setColor(BuildAnim_Player.redColor);
                drawer.drawSprite(def.Sprite_RawIcon, 0f, 0f, 1f, 1f, 0f, 1f);
                drawer.updatePaint();
                return false; // não desenha o modelo do jogo
            }
            catch (Exception ex)
            {
                ModLog.Error(ex, "imagem da construção");
                return true;
            }
        }

        [HarmonyPatch(typeof(BuildAnim_Player), nameof(BuildAnim_Player.setSorting))]
        [HarmonyPostfix]
        private static void BuildSorting_Postfix(BuildAnim_Player __instance, int sortingLayerID, int sortingOrder)
        {
            try
            {
                var drawer = __instance.transform.Find(DrawerName)?.GetComponent<TextureDrawer>();
                if (drawer != null) drawer.setSorting(sortingLayerID, sortingOrder);
            }
            catch (Exception ex) { ModLog.Error(ex, "ordem da imagem da construção"); }
        }

        private static TextureDrawer GetDrawer(BuildAnim_Player anim, BaseDef def)
        {
            var existing = anim.transform.Find(DrawerName)?.GetComponent<TextureDrawer>();
            var texture = def.Sprite_RawIcon.texture;
            if (existing != null && existing.getMaterial()?.mainTexture == texture) return existing;
            if (existing != null) UnityEngine.Object.Destroy(existing.gameObject);

            var drawer = PlatformTools.createGameObject<TextureDrawer>(anim.gameObject);
            drawer.gameObject.name = DrawerName;
            drawer.init();
            if (!Materials.TryGetValue(texture, out var material) || material == null)
            {
                material = new Material(ResourceStatic.share_Stand()) { mainTexture = texture };
                Materials[texture] = material;
            }
            drawer.setMaterial(material);
            // Mesma camada que o jogo já deu a esta construção (BaseMono guarda a última).
            drawer.setSorting(anim.sortingLayerID, anim.sortingOrder);
            return drawer;
        }
    }
}
