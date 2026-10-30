using System;
using System.Collections.Generic;
using HarmonyLib;
using Spine;

namespace WuxiaMod.Game.Patches
{
    /// <summary>
    /// O cabelo é uma skin à parte que o jogo põe antes da roupa, e os capacetes do mod
    /// (arte do RimWorld) não o cobrem: o cabelo aparecia por cima e em volta do capacete.
    /// Com um capacete do mod equipado, tiramos as peças de cabelo do boneco (no mapa e na ficha).
    /// </summary>
    internal static class HairPatch
    {
        private static readonly Dictionary<string, bool> IsHelmet = new Dictionary<string, bool>();

        [HarmonyPatch(typeof(HeroAnim), nameof(HeroAnim.refreshEquip))]
        private static class OnMap
        {
            [HarmonyPostfix]
            private static void Postfix(HeroAnim __instance)
            {
                try { Apply(__instance.spinePlayer?.skeletonAnimation?.Skeleton, __instance.getCloths(), __instance.hero?.heroBean?.sex ?? 0); }
                catch (Exception ex) { ModLog.Error(ex, "esconder cabelo sob capacete"); }
            }
        }

        [HarmonyPatch(typeof(HeroHead), nameof(HeroHead.refreshEquip))]
        private static class OnPortrait
        {
            [HarmonyPostfix]
            private static void Postfix(HeroHead __instance, int ___gender)
            {
                try
                {
                    if (!__instance.allowAppointCloth()) return;
                    Apply(__instance.spinePlayer?.Skeleton, __instance.getCloths(), ___gender);
                }
                catch (Exception ex) { ModLog.Error(ex, "esconder cabelo sob capacete (ficha)"); }
            }
        }

        private static void Apply(Skeleton sk, List<FWRoleEquip> cloths, int sex)
        {
            if (sk?.Skin == null || cloths == null || !WearsHelmet(sk.Data, cloths, sex)) return;
            var hair = new List<Skin.SkinEntry>();
            foreach (var e in sk.Skin.Attachments)
                if (IsHair(sk.Data.Slots.Items[e.SlotIndex])) hair.Add(e);
            foreach (var e in hair) sk.Skin.RemoveAttachment(e.SlotIndex, e.Name);
            foreach (var slot in sk.Slots)
                if (IsHair(slot.Data)) slot.Attachment = null;
        }

        private static bool IsHair(SlotData slot) => slot.Name.IndexOf("/hair ", StringComparison.Ordinal) >= 0;

        private static bool WearsHelmet(SkeletonData data, List<FWRoleEquip> cloths, int sex)
        {
            foreach (var c in cloths)
            {
                if (c == null || !c.hasEquip() || !(c.equip.getDef() is T_ClothDef def) || def.clothSkin == null) continue;
                foreach (var name in def.clothSkin.getClothSkin(sex))
                {
                    if (name == null || !name.StartsWith("wuxia_")) continue;
                    if (!IsHelmet.TryGetValue(name, out bool helmet))
                    {
                        helmet = false;
                        var skin = data.FindSkin(name);
                        if (skin != null)
                            foreach (var e in skin.Attachments)
                                if (data.Slots.Items[e.SlotIndex].Name.IndexOf("casque", StringComparison.Ordinal) >= 0) { helmet = true; break; }
                        IsHelmet[name] = helmet;
                    }
                    if (helmet) return true;
                }
            }
            return false;
        }
    }
}
