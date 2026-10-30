using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using WuxiaMod.Localization;

namespace WuxiaMod.Game.Patches
{
    /// <summary>
    /// Aba "Wuxia" no menu de construção com todas as construções do mod.
    ///
    /// Como o jogo faz (lido do Assembly-CSharp, BuildingView_PC):
    /// - Awake cria as abas copiando o objeto "tab0" para dentro de "Titles"; a última é a
    ///   de projetos (tabBlue). Cada aba tem um tipo (array types) e getBeans(tipo, tech)
    ///   devolve as construções daquele tipo; initData(tipo, tech) enche a grade.
    /// - Os arrays de abas têm tamanho fixo (11), então a nossa aba fica FORA da lista
    ///   "tabs": criamos o botão, cuidamos do clique e usamos um tipo nosso (WuxiaType).
    ///   getBeans devolve só as nossas construções para esse tipo e tira elas das outras abas.
    /// - Update usa tabs[indexType(selectType)] para posicionar a barra de nível (Tech);
    ///   para o nosso tipo indexType devolve 0 e a barra fica escondida.
    /// </summary>
    [HarmonyPatch]
    internal static class BuildTabPatch
    {
        private const int WuxiaType = -7700;
        private static UISwitchButton _tab;

        private static bool IsOurs(BaseDef def) => def?.mod != null && def.mod.packageId == ModEntry.HarmonyId;

        [HarmonyPatch(typeof(BuildingView_PC), "Awake")]
        [HarmonyPostfix]
        private static void Awake_Postfix(BuildingView_PC __instance)
        {
            try
            {
                var t = Traverse.Create(__instance);
                var template = t.Field("obj_Tab").GetValue<GameObject>();
                var parent = ((Component)t.Field("obj_TabRoot").GetValue()).gameObject;
                if (template == null || parent == null) return;
                var go = PlatformTools.AddChild(parent, template);
                go.name = "wuxiaTab";
                UIUntils.setActive(go, true);
                // Antes da aba de projetos (a última).
                go.transform.SetSiblingIndex(Math.Max(0, parent.transform.childCount - 2));
                _tab = go.GetComponent<UISwitchButton>();
                _tab.setText(Loc.T("ui.build_tab"));
                _tab.setSelectedState(false);
                _tab.onSelectedChange += (obj, flag) => Select(__instance);
            }
            catch (Exception ex) { ModLog.Error(ex, "aba Wuxia no menu de construção"); }
        }

        private static void Select(BuildingView_PC view)
        {
            try
            {
                var t = Traverse.Create(view);
                foreach (var tab in t.Field("tabs").GetValue<List<UISwitchButton>>()) tab.setSelectedState(false);
                _tab.setSelectedState(true);
                ShowTech(view, false);
                t.Method("initData", WuxiaType, -1).GetValue();
            }
            catch (Exception ex) { ModLog.Error(ex, "abrir a aba Wuxia"); }
        }

        private static void ShowTech(BuildingView_PC view, bool show)
        {
            var tech = Traverse.Create(view).Field("techRT").GetValue<RectTransform>();
            if (tech != null) UIUntils.setActive(((Component)tech).gameObject, show);
        }

        // Clicou numa aba do jogo: a nossa desmarca e a barra de nível volta.
        [HarmonyPatch(typeof(BuildingView_PC), "setSelect")]
        [HarmonyPostfix]
        private static void SetSelect_Postfix(BuildingView_PC __instance) => Leave(__instance);

        [HarmonyPatch(typeof(BuildingView_PC), "initBlue")]
        [HarmonyPostfix]
        private static void InitBlue_Postfix(BuildingView_PC __instance) => Leave(__instance);

        private static void Leave(BuildingView_PC view)
        {
            try
            {
                if (_tab != null) _tab.setSelectedState(false);
                ShowTech(view, true);
            }
            catch (Exception ex) { ModLog.Error(ex, "sair da aba Wuxia"); }
        }

        [HarmonyPatch(typeof(BuildingView_PC), nameof(BuildingView_PC.indexType))]
        [HarmonyPostfix]
        private static void IndexType_Postfix(int type, ref int __result)
        {
            if (type == WuxiaType) __result = 0;
        }

        [HarmonyPatch(typeof(BuildingView_PC), "getBeans")]
        [HarmonyPostfix]
        private static void GetBeans_Postfix(int _selectType, ref IList __result)
        {
            try
            {
                if (_selectType != WuxiaType)
                {
                    for (int i = __result.Count - 1; i >= 0; i--)
                        if (IsOurs(__result[i] as BaseDef)) __result.RemoveAt(i);
                    return;
                }
                var list = new List<BaseDef>();
                foreach (var d in ImplDef<F_ManufactureDef>.beans) if (IsOurs(d)) list.Add(d);
                foreach (var d in ImplDef<F_Entertainment>.beans) if (IsOurs(d)) list.Add(d);
                foreach (var d in ImplDef<F_DecroDef>.beans) if (IsOurs(d)) list.Add(d);
                list.Sort((a, b) => a.SortId.CompareTo(b.SortId));
                __result = list;
            }
            catch (Exception ex) { ModLog.Error(ex, "construções da aba Wuxia"); }
        }
    }
}
