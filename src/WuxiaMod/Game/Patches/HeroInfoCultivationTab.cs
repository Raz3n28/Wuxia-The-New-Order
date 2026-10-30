using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WuxiaMod.Cultivation;
using WuxiaMod.Localization;

namespace WuxiaMod.Game.Patches
{
    /// <summary>
    /// Adiciona a aba "Cultivo" na janela do personagem (Dialog_MainHeroInfo),
    /// ao lado de Informação / Equipamento / Saúde.
    ///
    /// Como a janela do jogo funciona (lido do Assembly-CSharp):
    /// - Awake pega as abas em "TabPanel/Tab0..2" (UISwitchButton) e as telas
    ///   "ViewInfo", "EquipRoot" e "HealthRoot".
    /// - showTab(i) liga a aba i e mostra a tela dela; showHero(hero) troca o personagem.
    /// </summary>
    [HarmonyPatch]
    internal static class HeroInfoCultivationTab
    {
        private const string TabName = "TabWuxia";
        private const string ViewName = "WuxiaCultivationView";
        private const string AwakenButtonName = "AwakenButton";
        private const string TestButtonName = "TestKitButton";
        private const string BreakButtonName = "BreakthroughButton";
        private const string LanguageButtonName = "LanguageButton";

        private const float ButtonY = 40, ButtonWidth = 280, ButtonHeight = 54;

        private static readonly Color Gold = new Color(0.90f, 0.75f, 0.35f);

        [HarmonyPatch(typeof(Dialog_MainHeroInfo), "Awake")]
        [HarmonyPostfix]
        private static void Awake_Postfix(Dialog_MainHeroInfo __instance)
        {
            try
            {
                CreateTab(__instance);
                CreateView(__instance);
                ModLog.Info("Aba de cultivo criada na janela do personagem.");
            }
            catch (Exception ex)
            {
                ModLog.Error(ex, "Falha ao criar a aba de cultivo");
            }
        }

        // Qualquer aba do jogo selecionada: esconde a nossa.
        [HarmonyPatch(typeof(Dialog_MainHeroInfo), "showTab")]
        [HarmonyPostfix]
        private static void ShowTab_Postfix(Dialog_MainHeroInfo __instance)
        {
            try { SetOurTab(__instance, false); }
            catch (Exception ex) { ModLog.Error(ex, "showTab"); }
        }

        // Trocar de personagem: o jogo chama showTab(aba atual), que esconde a nossa.
        // Se a nossa estava aberta, abre de novo para o novo personagem, como o jogo faz com as dele.
        [HarmonyPatch(typeof(Dialog_MainHeroInfo), "showHero")]
        [HarmonyPrefix]
        private static void ShowHero_Prefix(Dialog_MainHeroInfo __instance, out bool __state)
        {
            __state = false;
            try
            {
                var view = FindChild(__instance.transform, ViewName);
                __state = view != null && view.gameObject.activeSelf;
            }
            catch (Exception ex) { ModLog.Error(ex, "showHero"); }
        }

        [HarmonyPatch(typeof(Dialog_MainHeroInfo), "showHero")]
        [HarmonyPostfix]
        private static void ShowHero_Postfix(Dialog_MainHeroInfo __instance, bool __state)
        {
            if (!__state) return;
            try { SetOurTab(__instance, true); }
            catch (Exception ex) { ModLog.Error(ex, "showHero"); }
        }

        // ---------- criação ----------

        private static void CreateTab(Dialog_MainHeroInfo dialog)
        {
            if (FindChild(dialog.transform, "TabPanel/" + TabName) != null) return;

            var tab1 = dialog.transform.Find("TabPanel/Tab1");
            var tab2 = dialog.transform.Find("TabPanel/Tab2");
            if (tab2 == null) throw new InvalidOperationException("TabPanel/Tab2 não encontrado");

            var clone = UnityEngine.Object.Instantiate(tab2.gameObject, tab2.parent, false);
            // O jogo identifica as abas pelo fim do nome ("0", "1", "2"); o nosso não termina em número.
            clone.name = TabName;

            // Sem layout automático no painel, posiciona depois da última aba com o mesmo espaçamento.
            if (tab2.parent.GetComponent<LayoutGroup>() == null && tab1 != null)
            {
                var r1 = (RectTransform)tab1;
                var r2 = (RectTransform)tab2;
                ((RectTransform)clone.transform).anchoredPosition = r2.anchoredPosition + (r2.anchoredPosition - r1.anchoredPosition);
            }

            foreach (var label in clone.GetComponentsInChildren<TextMeshProUGUI>(true))
                label.text = Loc.T("ui.cultivation");

            var sw = clone.GetComponent<UISwitchButton>();
            sw.onSelectedChange += (obj, flag) =>
            {
                try { SetOurTab(dialog, true); }
                catch (Exception ex) { ModLog.Error(ex, "clique na aba de cultivo"); }
            };
            sw.setSelectedState(false, false);
        }

        private static void CreateView(Dialog_MainHeroInfo dialog)
        {
            if (FindChild(dialog.transform, ViewName) != null) return;

            // Ocupa o mesmo espaço da tela de Saúde.
            var health = (RectTransform)dialog.transform.Find("HealthRoot");
            var go = new GameObject(ViewName, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(health.parent, false);
            rect.anchorMin = health.anchorMin;
            rect.anchorMax = health.anchorMax;
            rect.pivot = health.pivot;
            rect.anchoredPosition = health.anchoredPosition;
            rect.sizeDelta = health.sizeDelta;
            rect.SetSiblingIndex(health.GetSiblingIndex() + 1);

            // Sem fundo próprio: a janela do jogo já tem. E nada aqui recebe clique,
            // senão a tela cobre as abas e o X (a HealthRoot ocupa a janela toda).

            var textGo = new GameObject("Text", typeof(RectTransform));
            var textRect = (RectTransform)textGo.transform;
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            // Topo abaixo da linha das abas; embaixo, para antes da linha de botões.
            textRect.offsetMin = new Vector2(60, ButtonY + ButtonHeight + 20);
            textRect.offsetMax = new Vector2(-60, -130);

            var text = textGo.AddComponent<TextMeshProUGUI>();
            // Mesma fonte das abas, que já tem acentos e o estilo do jogo.
            var sample = dialog.transform.Find("TabPanel/Tab0")?.GetComponentInChildren<TextMeshProUGUI>(true);
            if (sample != null) text.font = sample.font;
            // Diminui sozinho se o texto não couber acima dos botões.
            text.enableAutoSizing = true;
            text.fontSizeMax = 24;
            text.fontSizeMin = 14;
            text.fontSize = 24;
            text.color = Color.white;
            text.enableWordWrapping = true;
            text.richText = true;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.raycastTarget = false;

            CreateButtons(dialog, rect, text.font);

            go.SetActive(false);
        }

        /// <summary>Botões no canto de baixo da tela. Só eles recebem clique.</summary>
        private static void CreateButtons(Dialog_MainHeroInfo dialog, RectTransform parent, TMP_FontAsset font)
        {
            CreateButton(parent, font, AwakenButtonName, Loc.T("ui.awaken_button"), new Vector2(60, ButtonY),
                new Color(0.45f, 0.33f, 0.10f, 0.95f), () => Awaken(dialog));
            // Mesmo lugar do "Despertar": aparece depois de despertar.
            CreateButton(parent, font, BreakButtonName, Loc.T("ui.break_button"), new Vector2(60, ButtonY),
                new Color(0.55f, 0.20f, 0.15f, 0.95f), () => TryBreakthrough(dialog));
            // Abre a janela própria do Wuxia (o Kit de teste virou a aba Dev de lá).
            CreateButton(parent, font, TestButtonName, Loc.T("ui.window.open_button"), new Vector2(60 + ButtonWidth + 20, ButtonY),
                new Color(0.20f, 0.35f, 0.55f, 0.95f), () => OpenWindow(dialog));
            // Idioma dos textos do mod (aba, itens, construções), seja qual for o idioma do jogo.
            CreateButton(parent, font, LanguageButtonName, Loc.T("ui.language_button"), new Vector2(-60, ButtonY),
                new Color(0.30f, 0.30f, 0.30f, 0.95f), () => ToggleLanguage(dialog), rightCorner: true);
        }

        private static void ToggleLanguage(Dialog_MainHeroInfo dialog)
        {
            ModEntry.SetModLanguage(Loc.Current == Loc.Portuguese ? Loc.English : Loc.Portuguese);
            var tab = FindChild(dialog.transform, "TabPanel/" + TabName);
            if (tab != null)
                foreach (var label in tab.GetComponentsInChildren<TextMeshProUGUI>(true))
                    label.text = Loc.T("ui.cultivation");
            var view = FindChild(dialog.transform, ViewName);
            SetButtonText(view, AwakenButtonName, Loc.T("ui.awaken_button"));
            SetButtonText(view, TestButtonName, Loc.T("ui.window.open_button"));
            SetButtonText(view, BreakButtonName, Loc.T("ui.break_button"));
            SetButtonText(view, LanguageButtonName, Loc.T("ui.language_button"));
            Refresh(dialog);
        }

        private static void SetButtonText(Transform view, string name, string text)
        {
            var label = view?.Find(name + "/Label")?.GetComponent<TextMeshProUGUI>();
            if (label != null) label.text = text;
        }

        private static void CreateButton(RectTransform parent, TMP_FontAsset font, string name, string text,
            Vector2 position, Color color, Action onClick, bool rightCorner = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            // Canto de baixo à esquerda, ou à direita (position.x negativo conta a partir da borda direita).
            rect.anchorMin = rect.anchorMax = rect.pivot = rightCorner ? new Vector2(1, 0) : Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);

            var image = go.AddComponent<Image>();
            image.color = color;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            button.colors = colors;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            var labelRect = (RectTransform)labelGo.transform;
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.fontSize = 22;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.text = text;

            button.onClick.AddListener(() =>
            {
                try { onClick(); }
                catch (Exception ex) { ModLog.Error(ex, "botão " + name); }
            });
            go.SetActive(false);
        }

        private static void OpenWindow(Dialog_MainHeroInfo dialog)
        {
            var hero = Traverse.Create(dialog).Field("heroBean").GetValue<FWHeroBean>();
            if (hero != null) WuxiaWindow.OpenFor(hero.uid);
        }

        private static void Awaken(Dialog_MainHeroInfo dialog)
        {
            var hero = Traverse.Create(dialog).Field("heroBean").GetValue<FWHeroBean>();
            if (hero == null || IsAndroid(hero)) return;
            var state = CultivationRegistry.Get(hero.uid, true);
            if (state.Awaken())
                ModLog.Info($"{hero.name} (uid {hero.uid}) despertou o cultivo com {state.RootId}.");
            Refresh(dialog);
        }

        /// <summary>
        /// Ruptura pelo botão: precisa de uma Plataforma de Ruptura (9603) pronta no mapa.
        /// Estágios que pedem item rompem só usando o item (botão direito nele).
        /// </summary>
        private static void TryBreakthrough(Dialog_MainHeroInfo dialog)
        {
            var heroBean = Traverse.Create(dialog).Field("heroBean").GetValue<FWHeroBean>();
            if (heroBean == null || IsAndroid(heroBean)) return;
            var hero = MapManager.getInstance()?.heroManager?.getHeroByUID(heroBean.uid);
            if (hero == null) return;
            var state = CultivationRegistry.Get(heroBean.uid, true);
            if (state.Realm == null) return;
            var platforms = MapManager.getInstance().buildManager?.getBuildTypeID(11, ModEntry.HarmonyId + "_" + MeditationPatch.BreakPlatformId);
            if (platforms == null || !platforms.Exists(b => b != null && b.isPutdown()))
                GUI_RoleInfo.showRoleInfo(hero, Loc.T("ui.need_platform"));
            else
                Breakthrough.Attempt(hero, state, false, Breakthrough.PlatformBonus, "botão");
            Refresh(dialog);
        }

        private static bool IsAndroid(FWHeroBean hero)
        {
            try { return hero.isAndroid(); } catch { return false; }
        }

        // ---------- troca de aba ----------

        private static void SetOurTab(Dialog_MainHeroInfo dialog, bool selected)
        {
            var tab = FindChild(dialog.transform, "TabPanel/" + TabName);
            var view = FindChild(dialog.transform, ViewName);
            if (tab == null || view == null) return;

            tab.GetComponent<UISwitchButton>().setSelectedState(selected, false);
            view.gameObject.SetActive(selected);
            if (!selected) return;

            var t = Traverse.Create(dialog);
            foreach (var other in t.Field("tabs").GetValue<List<UISwitchButton>>())
                other.setSelectedState(false, false);
            foreach (var field in new[] { "view_Info", "view_Equip", "view_Health" })
            {
                var comp = t.Field(field).GetValue<Component>();
                if (comp != null) comp.gameObject.SetActive(false);
            }
            Refresh(dialog);
        }

        private static void Refresh(Dialog_MainHeroInfo dialog)
        {
            var view = FindChild(dialog.transform, ViewName);
            var text = view?.Find("Text")?.GetComponent<TextMeshProUGUI>();
            var button = view?.Find(AwakenButtonName);
            var testButton = view?.Find(TestButtonName);
            var breakButton = view?.Find(BreakButtonName);
            var languageButton = view?.Find(LanguageButtonName);
            if (languageButton != null) languageButton.gameObject.SetActive(true);
            var hero = Traverse.Create(dialog).Field("heroBean").GetValue<FWHeroBean>();
            if (text == null) return;
            if (hero == null)
            {
                text.text = "";
                if (button != null) button.gameObject.SetActive(false);
                if (testButton != null) testButton.gameObject.SetActive(false);
                if (breakButton != null) breakButton.gameObject.SetActive(false);
                return;
            }

            bool android = IsAndroid(hero);
            var state = CultivationRegistry.Get(hero.uid, !android);
            text.text = Describe(hero.name, android, state);
            if (button != null) button.gameObject.SetActive(!android && state.CanCultivate && state.RealmOrder == 0);
            if (testButton != null) testButton.gameObject.SetActive(true);
            if (breakButton != null) breakButton.gameObject.SetActive(!android && state.Realm != null && !state.AtPeakRealm);
        }

        private static string Describe(string heroName, bool android, CultivationState state)
        {
            string gold = ColorUtility.ToHtmlStringRGB(Gold);
            var sb = new StringBuilder();
            sb.AppendLine($"<size=140%><color=#{gold}>{Loc.T("ui.cultivation")}</color></size>  <color=#AAAAAA>{heroName}</color>");
            sb.AppendLine();

            if (android)
            {
                sb.AppendLine(Loc.T("ui.android_no_cultivation"));
                return sb.ToString();
            }

            var root = state.Root;
            sb.AppendLine($"<color=#{gold}>{Loc.T("ui.spirit_root")}:</color> {(root != null ? Loc.T(root.NameKey) : "?")}");
            if (root != null && root.CanCultivate) sb.AppendLine($"<color=#BBBBBB><size=80%>{Loc.T(root.DescKey)}</size></color>");
            sb.AppendLine();

            var realm = state.Realm;
            if (realm == null)
            {
                sb.AppendLine($"<color=#{gold}>{Loc.T("ui.realm")}:</color> {Loc.T("ui.mortal")}");
                sb.AppendLine(state.CanCultivate ? Loc.T("ui.can_awaken") : Loc.T("ui.cannot_cultivate"));
            }
            else
            {
                sb.AppendLine($"<color=#{gold}>{Loc.T("ui.realm")}:</color> {Loc.T(realm.NameKey)}  <color=#AAAAAA>({realm.Order}/{RealmDatabase.All.Count})</color>");
                sb.AppendLine($"<color=#BBBBBB><size=80%>{Loc.T(realm.DescKey)}</size></color>");
                sb.AppendLine($"<color=#{gold}>{Loc.T("ui.qi")}:</color> {state.Qi:0} / {realm.MaxQi:0}    <color=#{gold}>{Loc.T("ui.progress")}:</color> {state.Progress:0} / {realm.Exp:0}");
                sb.AppendLine(Loc.T("ui.points", state.UnspentPoints));
                AppendBreakthrough(sb, state);
                AppendPillBonuses(sb, state);
                sb.AppendLine($"<color=#BBBBBB><size=80%>{Loc.T("ui.meditation_hint")}</size></color>");
            }
            sb.AppendLine();
            sb.AppendLine($"<color=#888888><size=70%>{Loc.T("ui.coming_soon")}</size></color>");
            return sb.ToString();
        }

        private static void AppendPillBonuses(StringBuilder sb, CultivationState state)
        {
            long now = UserBean.getInstance().getTime();
            double hour = WorldTimeTools.gameHour_to_mm(1f);
            float qiBoost = state.ActiveQiBoost(now);
            if (qiBoost > 0f)
                sb.AppendLine($"<color=#88DD88>{Loc.T("ui.qi_boost", (qiBoost * 100f).ToString("0"), Math.Ceiling((state.QiBoostUntil - now) / hour))}</color>");
            float breakBonus = state.ActiveBreakBonus(now);
            if (breakBonus > 0f)
                sb.AppendLine($"<color=#88DD88>{Loc.T("ui.break_bonus", (breakBonus * 100f).ToString("0"), Math.Ceiling((state.BreakBonusUntil - now) / hour))}</color>");
        }

        private static void AppendBreakthrough(StringBuilder sb, CultivationState state)
        {
            var realm = state.Realm;
            long now = UserBean.getInstance().getTime();
            double hour = WorldTimeTools.gameHour_to_mm(1f);
            if (state.InDeviation(now))
                sb.AppendLine($"<color=#FF7766>{Loc.T("ui.deviation_left", Math.Ceiling((state.DeviationUntil - now) / hour))}</color>");
            if (state.AtPeakRealm)
            {
                sb.AppendLine($"<color=#FFCC66>{Loc.T("ui.breakthrough.at_peak")}</color>");
                return;
            }
            float chance = state.BreakthroughChance(state.ActiveBreakBonus(now));
            string line = Loc.T("ui.break_chance", (chance * 100f).ToString("0"), Loc.T(state.NextRealm.NameKey));
            if (realm.BreakthroughItem != null) line += "  " + Loc.T("ui.breakthrough.missing_item", Breakthrough.ItemName(realm.BreakthroughItem));
            sb.AppendLine(line);
            if (state.ReadyForBreakthrough)
                sb.AppendLine($"<color=#FFCC66>{Loc.T(realm.BreakthroughItem != null ? "ui.ready_item" : "ui.ready_platform")}</color>");
        }

        private static Transform FindChild(Transform root, string path) => root == null ? null : root.Find(path) ?? FindDeep(root, path);

        private static Transform FindDeep(Transform root, string name)
        {
            if (name.Contains("/")) return null;
            foreach (Transform child in root)
            {
                if (child.name == name) return child;
                var found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
