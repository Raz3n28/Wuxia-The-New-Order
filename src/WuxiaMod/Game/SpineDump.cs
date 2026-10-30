using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Spine;
using UnityEngine;

namespace WuxiaMod.Game
{
    /// <summary>
    /// Ferramenta de desenvolvimento: exporta as peças do boneco do jogo (capacete, tronco,
    /// corpo nu e as armas que os nossos itens copiam) para a pasta "dump" do mod. Para cada
    /// peça grava o PNG e uma linha em parts.csv com o tamanho e a posição no boneco em pose
    /// de referência. Com isso as imagens das nossas roupas e armas são feitas no tamanho e
    /// no ângulo certos para o arquivo role-customize.xml (sistema oficial de skins do jogo).
    /// Liga com "dumpSpine": true no config.json ou pelo botão na aba Dev.
    /// </summary>
    internal static class SpineDump
    {
        public static bool Auto;
        private static bool _done;

        // Skins usadas pelo SpineCombine como modelo (cabelo, pele, rosto, capacete, tronco, pernas, enfeite).
        private static readonly string[] Templates = { "TY_TF_001_M", "00azipifuman", "TY_MS_002_M", "DG_TK_002_M", "DG_KJ_000_M", "DG_HX_000_M", "bb" };

        public static void Tick()
        {
            if (!Auto || _done || Time.unscaledTime < 15f) return;
            _done = true;
            Run();
        }

        public static string Run()
        {
            string dir = Path.Combine(Directory.GetParent(ModEntry.VersionDir).FullName, "dump");
            try
            {
                Directory.CreateDirectory(dir);
                var data = global::SpineLoader.getSpineLoaderBean("allrr.skel", "allrr.atlas", false, false, false, true);
                var sd = data.skeletonData;
                var csv = new StringBuilder("kind;skin;slot;bone;attachment;type;regionW;regionH;rotate;x;y;rotation;scaleX;scaleY;width;height;minX;minY;maxX;maxY;png\n");
                var names = new HashSet<string>(Templates);
                foreach (var def in ImplDef<T_ClothDef>.beans)
                {
                    if (def?.mod == null || def.mod.packageId != ModEntry.HarmonyId) continue;
                    foreach (var s in def.clothSkin.girlWares) names.Add(s);
                    foreach (var s in def.clothSkin.boyWares) names.Add(s);
                }
                foreach (var name in names)
                {
                    var skin = sd.FindSkin(name);
                    if (skin == null) { csv.Append("missing;" + name + "\n"); continue; }
                    var sk = Pose(sd, skin);
                    foreach (var e in skin.Attachments)
                        Row(csv, dir, "skin", name, sk.Slots.Items[e.SlotIndex], e.Name, e.Attachment);
                }

                // Armas: os pares (slot, anexo) dos nossos itens, que vêm do item do jogo copiado.
                var seen = new HashSet<string>();
                var def0 = Pose(sd, null);
                foreach (var w in ImplDef<T_WeaponDef>.beans)
                {
                    if (w?.mod == null || w.mod.packageId != ModEntry.HarmonyId) continue;
                    var pairs = new[]
                    {
                        (w.f_Slot, w.f_Slot_Weapon), (w.x3_Slot, w.x3_Slot_Weapon), (w.s_Slot, w.s_Slot_Weapon), (w.x9_Slot, w.x9_Slot_Weapon), (w.b_Slot, w.b_Slot_Weapon),
                        (w.f_StandBy_Slot, w.f_StandBy_Weapon), (w.x3_StandBy_Slot, w.x3_StandBy_Weapon), (w.s_StandBy_Slot, w.s_StandBy_Weapon), (w.x9_StandBy_Slot, w.x9_StandBy_Weapon), (w.b_StandBy_Slot, w.b_StandBy_Weapon),
                    };
                    foreach (var (slotName, att) in pairs)
                    {
                        if (string.IsNullOrEmpty(slotName) || string.IsNullOrEmpty(att) || !seen.Add(slotName + "|" + att)) continue;
                        var slotData = sd.FindSlot(slotName);
                        var a = slotData == null ? null : sd.DefaultSkin.GetAttachment(slotData.Index, att);
                        if (a == null) { csv.Append($"missing;weapon {w.ID};{slotName};;{att}\n"); continue; }
                        Row(csv, dir, "weapon", "w" + w.ID, def0.Slots.Items[slotData.Index], att, a);
                    }
                }

                var parts = new StringBuilder("Key;PartType;Direction;SlotDesc;Slot;Rack\n");
                foreach (var p in ImplDef<S_CharacterSpinePartsDef>.beans)
                    parts.Append($"{p.Key};{p.PartType};{p.Direction};{p.SlotDesc};{p.Slot};{p.Rack}\n");

                var bones = new StringBuilder("bone;parent;worldX;worldY;rotation;scaleX;scaleY\n");
                foreach (var b in def0.Bones)
                    bones.Append($"{b.Data.Name};{b.Parent?.Data.Name};{b.WorldX:0.##};{b.WorldY:0.##};{b.WorldRotationX:0.##};{b.WorldScaleX:0.###};{b.WorldScaleY:0.###}\n");

                // Ordem de desenho (setup): índice do slot, para montar o boneco inteiro fora do jogo.
                var slots = new StringBuilder("index;slot;bone\n");
                foreach (var sl in sd.Slots)
                    slots.Append($"{sl.Index};{sl.Name};{sl.BoneData.Name}\n");

                File.WriteAllText(Path.Combine(dir, "slots.csv"), slots.ToString());
                File.WriteAllText(Path.Combine(dir, "parts.csv"), csv.ToString());
                File.WriteAllText(Path.Combine(dir, "partsdef.csv"), parts.ToString());
                File.WriteAllText(Path.Combine(dir, "bones.csv"), bones.ToString());
                File.WriteAllText(Path.Combine(dir, "done.txt"), DateTime.Now.ToString("s"));
                ModLog.Info("Peças do boneco exportadas para " + dir);
                return dir;
            }
            catch (Exception ex)
            {
                ModLog.Error(ex, "exportar peças do boneco");
                return null;
            }
        }

        private static Skeleton Pose(SkeletonData sd, Skin skin)
        {
            var sk = new Skeleton(sd);
            if (skin != null) sk.SetSkin(skin);
            sk.SetToSetupPose();
            sk.UpdateWorldTransform();
            return sk;
        }

        private static void Row(StringBuilder csv, string dir, string kind, string skin, Slot slot, string name, Attachment a)
        {
            var region = (a as IHasRendererObject)?.RendererObject as AtlasRegion;
            float[] v = null;
            string extra = ";;;;;;;";
            if (a is RegionAttachment r)
            {
                v = new float[8];
                r.ComputeWorldVertices(slot.Bone, v, 0);
                extra = $"{r.X:0.##};{r.Y:0.##};{r.Rotation:0.##};{r.ScaleX:0.###};{r.ScaleY:0.###};{r.Width:0.##};{r.Height:0.##}";
            }
            else if (a is VertexAttachment va)
            {
                v = new float[va.WorldVerticesLength];
                va.ComputeWorldVertices(slot, v);
                extra = ";;;;;;";
            }
            float x0 = 0, y0 = 0, x1 = 0, y1 = 0;
            if (v != null && v.Length >= 2)
            {
                x0 = x1 = v[0]; y0 = y1 = v[1];
                for (int i = 0; i + 1 < v.Length; i += 2)
                {
                    x0 = Math.Min(x0, v[i]); x1 = Math.Max(x1, v[i]);
                    y0 = Math.Min(y0, v[i + 1]); y1 = Math.Max(y1, v[i + 1]);
                }
            }
            string png = region == null ? "" : Save(dir, skin, slot.Data.Name, name, region);
            csv.Append($"{kind};{skin};{slot.Data.Name};{slot.Bone.Data.Name};{name};{a?.GetType().Name};{region?.width};{region?.height};{region?.rotate};{extra};{x0:0.##};{y0:0.##};{x1:0.##};{y1:0.##};{png}\n");
        }

        private static readonly Dictionary<Texture, Texture2D> Readable = new Dictionary<Texture, Texture2D>();

        private static string Save(string dir, string skin, string slot, string name, AtlasRegion region)
        {
            try
            {
                var tex = (region.page?.rendererObject as Material)?.mainTexture;
                if (tex == null) return "";
                var src = ReadableCopy(tex);
                // Depois do OptimizeSkin as regiões vêm do spine-unity (UVRectToAtlasRegion): v e v2
                // já contam de baixo para cima como no Texture2D, sem inverter.
                int px0 = Mathf.RoundToInt(Math.Min(region.u, region.u2) * tex.width);
                int px1 = Mathf.RoundToInt(Math.Max(region.u, region.u2) * tex.width);
                int py0 = Mathf.RoundToInt(Math.Min(region.v, region.v2) * tex.height);
                int py1 = Mathf.RoundToInt(Math.Max(region.v, region.v2) * tex.height);
                int w = Math.Max(1, px1 - px0), h = Math.Max(1, py1 - py0);
                var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                outTex.SetPixels(src.GetPixels(px0, py0, w, h));
                outTex.Apply();
                string file = Clean(skin + "__" + slot + "__" + name) + (region.degrees != 0 ? "__deg" + region.degrees : "") + ".png";
                File.WriteAllBytes(Path.Combine(dir, file), outTex.EncodeToPNG());
                UnityEngine.Object.Destroy(outTex);
                return file;
            }
            catch (Exception ex)
            {
                ModLog.Warn("PNG de " + name + ": " + ex.Message);
                return "";
            }
        }

        private static Texture2D ReadableCopy(Texture tex)
        {
            if (Readable.TryGetValue(tex, out var copy)) return copy;
            var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(tex, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            copy = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            copy.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return Readable[tex] = copy;
        }

        private static string Clean(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s.Replace(' ', '_');
        }
    }
}
