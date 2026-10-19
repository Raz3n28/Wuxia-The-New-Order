using System;
using System.Collections.Generic;
using System.IO;
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
    /// - O jogo junta todos os RawIcon de mods numa textura só (ModIconCombine). O menu e a
    ///   prévia leem pedaços diferentes dela, e o ícone do Caldeirão Pequeno saía com a imagem
    ///   do Taiyi. As nossas construções ganham uma textura própria cada, sem atlas.
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

        // ---------- textura própria ----------

        [HarmonyPatch(typeof(ModIconCombine), nameof(ModIconCombine.combineIcon))]
        [HarmonyPostfix]
        private static void CombineIcon_Postfix()
        {
            foreach (var def in ModIconCombine.additions)
            {
                if (!(def is F_ManufactureDef) && !(def is F_Entertainment)) continue;
                if (def.mod?.packageId != ModEntry.HarmonyId || def.RawIcon == null) continue;
                try
                {
                    var path = def.mod.getVersionPath("Resources/Textures/", def.RawIcon);
                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                    if (!texture.LoadImage(File.ReadAllBytes(path))) continue;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    def.Sprite_RawIcon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                }
                catch (Exception ex) { ModLog.Error(ex, "imagem de " + def.ID); }
            }
        }

        // ---------- ícone das construções sem imagem própria ----------

        // A Bancada de Elixires usa o modelo da bancada de remédios do jogo. O ícone do
        // jogo para ela (Icon_Builds) é outro desenho, que não bate com a bancada no mapa.
        // Para as nossas construções sem RawIcon, o ícone passa a ser o próprio modelo
        // (B_Manufacture, ação ModuleId, quadro 0), o mesmo que aparece construído.
        [HarmonyPatch(typeof(PlayerStatic), nameof(PlayerStatic.getPlayerInfo))]
        [HarmonyPostfix]
        private static void PlayerInfo_Postfix(int type, string id, PlayerInfo __result)
        {
            try
            {
                if (type != 27 || __result == null) return;
                var def = ImplDef<F_ManufactureDef>.getDataID(id);
                if (def?.mod == null || def.mod.packageId != ModEntry.HarmonyId || def.RawIcon != null) return;
                __result.iconPlayer = __result.player;
                __result.iconActionID = __result.actionId;
                __result.iconFrameID = __result.frameId;
            }
            catch (Exception ex) { ModLog.Error(ex, "ícone da construção"); }
        }

        // O quadro do modelo vem espelhado em relação ao que aparece no mapa (pilão à
        // direita no ícone, à esquerda no mapa). Desenhamos o ícone com escala X = -1.
        [HarmonyPatch(typeof(CommonIconUtils), nameof(CommonIconUtils.initPlayerNode),
            new[] { typeof(UISpriteCatPlayer), typeof(int), typeof(string) })]
        [HarmonyPrefix]
        private static bool IconNode_Prefix(UISpriteCatPlayer player, int type, string id)
        {
            try
            {
                if (type != 27 || player == null) return true;
                var def = ImplDef<F_ManufactureDef>.getDataID(id);
                if (def?.mod == null || def.mod.packageId != ModEntry.HarmonyId || def.RawIcon != null) return true;
                var info = PlayerStatic.getPlayerInfo(type, id);
                var key = UISpriteCatPlayer.getKey(info.iconPlayer, "", null);
                if (player.player == null || !key.Equals(player.player.key))
                {
                    var config = PlayerInitConfig.getPlayerConfig(info.iconPlayer);
                    player.loadBin(info.iconPlayer, false, info.smooth, config == null || config.bilinear);
                    player.SetMaterialDirty();
                }
                player.paintActionFrame(info.iconActionID, info.iconFrameID, 0f, 0f, 0f, true, -1f, 1f);
                player.updatePaint();
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Error(ex, "ícone espelhado da construção");
                return true;
            }
        }

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
