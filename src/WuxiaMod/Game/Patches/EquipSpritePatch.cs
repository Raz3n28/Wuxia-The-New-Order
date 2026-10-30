using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Spine;
using Spine.Unity.AttachmentTools;
using UnityEngine;

namespace WuxiaMod.Game.Patches
{
    /// <summary>
    /// Armas e roupas do mod aparecem com a NOSSA imagem no personagem.
    ///
    /// Como o jogo faz (lido do Assembly-CSharp): o personagem é um Spine. A arma na mão é
    /// HeroAnim.appointWeapon -> SpinePlayer.setSlotTex(slot, anexo), com nomes que vêm da
    /// arma (b_/f_/s_/x3_/x9_Slot e _Weapon: costas, frente, lado, diagonais). Roupa é
    /// HeroAnim.appointCloth -> SpinePlayer.addSkin(nome), com as skins do campo Skin da
    /// roupa. Como cada item nosso é cópia de um item do jogo (DependID), herdava os
    /// nomes e aparecia a imagem do item do jogo.
    ///
    /// Aqui, depois do jogo montar o personagem, trocamos esses anexos por cópias que
    /// usam o PNG do item (RawIcon), no mesmo lugar (GetRemappedClone do spine-unity):
    /// - Arma: o PNG é um ícone na diagonal (cabo embaixo à esquerda); giramos 45° para
    ///   ficar na direção da arma do jogo e ajustamos o tamanho ao comprimento dela.
    ///   Ângulo e tamanho podem ser mudados no config.json (weaponSpriteRotation,
    ///   weaponSpriteScale) sem compilar de novo.
    /// - Roupa: as peças grandes da skin (pelo menos metade da maior) viram o PNG no
    ///   tamanho da peça; as pequenas (detalhes, mangas) somem. A lista de peças vai
    ///   para o wuxia.log uma vez por roupa, para ajustar depois.
    /// </summary>
    [HarmonyPatch]
    internal static class EquipSpritePatch
    {
        public static float WeaponRotation = -45f;
        public static float WeaponScale = 1f;

        private static readonly string[] WeaponSlots = { "b_slot", "f_slot", "s_slot", "x3_slot", "x9_slot" };
        private static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<(Attachment, string), Attachment> Clones = new Dictionary<(Attachment, string), Attachment>();
        private static readonly HashSet<string> LoggedCloth = new HashSet<string>();
        private static int _errors;

        private static bool IsOurs(BaseDef def) => def?.mod != null && def.mod.packageId == ModEntry.HarmonyId && !string.IsNullOrEmpty(def.RawIcon);

        // ---------- arma ----------

        [HarmonyPatch(typeof(HeroAnim), "appointWeapon")]
        [HarmonyPostfix]
        private static void AppointWeapon_Postfix(HeroAnim __instance)
        {
            try
            {
                var weapon = __instance.getWeapon();
                if (!IsOurs(weapon)) return;
                var t = Traverse.Create(__instance);
                var renderer = t.Field("spinePlayer").GetValue<SpinePlayer>()?.skeletonAnimation;
                foreach (var name in WeaponSlots)
                {
                    var slot = t.Field(name).GetValue<Slot>();
                    if (!(slot?.Attachment is RegionAttachment original)) continue;
                    var clone = WeaponClone(original, weapon);
                    if (clone != null && Register(renderer, clone)) slot.Attachment = clone;
                }
            }
            catch (Exception ex) { Fail(ex, "imagem da arma na mão"); }
        }

        private static Attachment WeaponClone(RegionAttachment original, BaseDef weapon)
        {
            var key = ((Attachment)original, weapon.ID);
            if (Clones.TryGetValue(key, out var cached)) return cached;
            var region = Region(original, weapon);
            if (region == null) return Clones[key] = null;

            // Lado do quadrado do nosso PNG em unidades do Spine: o ícone na diagonal tem
            // comprimento ~1,3 x o lado; fica do comprimento da arma do jogo.
            float length = Math.Max(Math.Abs(original.Width * original.ScaleX), Math.Abs(original.Height * original.ScaleY));
            float side = length / 1.3f * WeaponScale;
            var clone = original.GetRemappedClone(region, true, false, side / Math.Max(1, region.width)) as RegionAttachment;
            if (clone == null) return Clones[key] = null;
            clone.ScaleX = Math.Sign(original.ScaleX == 0 ? 1 : original.ScaleX);
            clone.ScaleY = Math.Sign(original.ScaleY == 0 ? 1 : original.ScaleY);
            clone.Rotation = original.Rotation + WeaponRotation;
            clone.UpdateOffset();
            return Clones[key] = clone;
        }

        // ---------- roupa ----------

        [HarmonyPatch(typeof(HeroAnim), "appointCloth")]
        [HarmonyPostfix]
        private static void AppointCloth_Postfix(HeroAnim __instance)
        {
            try
            {
                var cloths = __instance.getCloths();
                if (cloths == null) return;
                var spine = Traverse.Create(__instance).Field("spinePlayer").GetValue<SpinePlayer>();
                var renderer = spine?.skeletonAnimation;
                var skeleton = renderer?.Skeleton;
                if (skeleton?.Skin == null) return;
                int sex = __instance.hero.heroBean.sex;
                foreach (var equip in cloths)
                {
                    if (equip == null || !equip.hasEquip()) continue;
                    if (!(equip.equip.getDef() is T_ClothDef def) || !IsOurs(def)) continue;
                    foreach (var skinName in def.clothSkin.getClothSkin(sex))
                    {
                        var skin = skeleton.Data.FindSkin(skinName);
                        if (skin != null) ReplaceCloth(renderer, skeleton, skin, def);
                    }
                }
            }
            catch (Exception ex) { Fail(ex, "imagem da roupa no personagem"); }
        }

        private static void ReplaceCloth(Spine.Unity.SkeletonRenderer renderer, Skeleton skeleton, Skin skin, BaseDef def)
        {
            var entries = new List<Skin.SkinEntry>(skin.Attachments);
            float biggest = 0;
            foreach (var e in entries) biggest = Math.Max(biggest, Area(e.Attachment));

            bool log = LoggedCloth.Add(def.ID + "|" + skin.Name);
            if (log) ModLog.Info($"Roupa {def.ID} ({def.RawIcon}), skin {skin.Name}: {entries.Count} peças");

            foreach (var e in entries)
            {
                // Só as peças grandes (tronco, capacete) viram a nossa imagem; as pequenas
                // (mãos, braços, detalhes) ficam as do jogo.
                bool big = biggest > 0 && Area(e.Attachment) >= biggest * 0.5f;
                if (log) ModLog.Info($"  {skeleton.Slots.Items[e.SlotIndex].Data.Name}/{e.Name}: {e.Attachment?.GetType().Name} {Area(e.Attachment):0} {(big ? "-> nossa imagem" : "-> do jogo")}");
                if (!big) continue;
                var clone = ClothClone(e.Attachment, def);
                if (clone != null && Register(renderer, clone)) skeleton.Skin.SetAttachment(e.SlotIndex, e.Name, clone);
            }
        }

        /// <summary>Tamanho da peça em pixels do atlas (região ou malha).</summary>
        private static float Area(Attachment a)
        {
            var region = (a as IHasRendererObject)?.RendererObject as AtlasRegion;
            return region == null ? 0f : Math.Abs((float)region.width * region.height);
        }

        private static Attachment ClothClone(Attachment original, BaseDef def)
        {
            var key = (original, def.ID);
            if (Clones.TryGetValue(key, out var cached)) return cached;
            var region = Region(original, def);
            // Do tamanho da peça do jogo (useOriginalRegionSize); malha vira malha ligada
            // com a nossa imagem esticada sobre ela.
            var clone = region == null ? null : original.GetRemappedClone(region, true, true);
            return Clones[key] = clone;
        }

        // ---------- comum ----------

        private static readonly Dictionary<(string, int), AtlasRegion> Regions = new Dictionary<(string, int), AtlasRegion>();

        /// <summary>Região de atlas com o PNG do item, num material copiado do personagem.</summary>
        private static AtlasRegion Region(Attachment original, BaseDef def)
        {
            var tex = Texture(def);
            var material = (((original as IHasRendererObject)?.RendererObject as AtlasRegion)?.page?.rendererObject) as Material;
            if (tex == null || material == null) return null;
            var key = (def.RawIcon, material.GetInstanceID());
            if (Regions.TryGetValue(key, out var region)) return region;
            // O shader do Spine espera alfa pré-multiplicado.
            region = tex.ToAtlasRegionPMAClone(material);
            // ToAtlasRegion põe offset de meia imagem (pivô no canto): a imagem saía deslocada
            // para cima e para a direita, e na malha da armadura só um canto aparecia.
            region.offsetX = region.offsetY = 0;
            region.originalWidth = region.width;
            region.originalHeight = region.height;
            return Regions[key] = region;
        }

        /// <summary>
        /// O SkeletonRenderer do jogo é modificado: em replanceShareMaterials ele troca cada
        /// material pelo da lista loaderData.materials com a mesma textura, e se não acha fica
        /// null (a peça some). Por isso a primeira versão deixava arma e roupa invisíveis.
        /// Aqui pomos o material da nossa imagem nessa lista.
        /// </summary>
        private static bool Register(Spine.Unity.SkeletonRenderer renderer, Attachment clone)
        {
            var material = (((clone as IHasRendererObject)?.RendererObject as AtlasRegion)?.page?.rendererObject) as Material;
            if (renderer == null || material == null) return false;
            var data = Traverse.Create(renderer).Field("loaderData").GetValue<Spine.Unity.SpineLoaderData>();
            if (data == null) return true; // sem loaderData o renderer usa os materiais como vêm
            foreach (var m in data.materials)
                if (m != null && m.mainTexture == material.mainTexture) return true;
            var list = new List<Material>(data.materials) { material };
            data.materials = list.ToArray();
            return true;
        }

        private static Texture2D Texture(BaseDef def)
        {
            if (Textures.TryGetValue(def.RawIcon, out var tex)) return tex;
            tex = null;
            try
            {
                var path = def.mod.getVersionPath("Resources/Textures/", def.RawIcon);
                if (File.Exists(path))
                {
                    tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                    if (!tex.LoadImage(File.ReadAllBytes(path))) tex = null;
                }
            }
            catch (Exception ex) { Fail(ex, "PNG " + def.RawIcon); }
            return Textures[def.RawIcon] = tex;
        }

        private static void Fail(Exception ex, string what)
        {
            if (_errors++ < 5 || _errors % 100 == 0) ModLog.Error(ex, what);
        }
    }
}
