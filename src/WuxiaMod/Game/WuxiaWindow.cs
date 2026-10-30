using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using WuxiaMod.Cultivation;
using WuxiaMod.Localization;

namespace WuxiaMod.Game
{
    /// <summary>
    /// Janela própria do Wuxia (F9, ou o botão na aba Cultivo da ficha do personagem).
    /// Feita com IMGUI (GUI.Window), como o mod Editor de Personagem: desenhada pelo
    /// WuxiaRuntime.OnGUI, por cima da interface do jogo.
    ///
    /// - À esquerda, os colonos no mapa; à direita, as abas.
    /// - Aba Cultivo: raiz, estágio, barras de Qi e de progresso que enchem, chance de
    ///   ruptura e os botões Despertar / Tentar ruptura.
    /// - Botão "Dev" no canto: liga e desliga a aba Dev (testes): soltar itens, encher o
    ///   cultivo, romper na hora, soltar o item de ruptura e a lista de todos os itens.
    /// </summary>
    internal static class WuxiaWindow
    {
        public static bool Open;
        private static bool _dev;
        private static int _tab; // 0 = Cultivo, 1 = Dev
        private static long _heroUid;
        private static Rect _rect = new Rect(80, 60, 1040, 660);
        private static Vector2 _heroScroll, _itemScroll;
        private static string _filter = "";
        private static List<BaseDef> _items;

        private const int WindowId = 96000001;
        private const float Scale = 1.25f;

        public static void Toggle()
        {
            Open = !Open;
            _items = null;
        }

        public static void OpenFor(long heroUid)
        {
            _heroUid = heroUid;
            _tab = 0;
            Open = true;
        }

        public static void Update()
        {
            try
            {
                var kb = Keyboard.current;
                if (kb == null) return;
                if (kb.f9Key.wasPressedThisFrame) Toggle();
                else if (Open && kb.escapeKey.wasPressedThisFrame) Open = false;
            }
            catch { }
        }

        public static void Draw()
        {
            if (!Open) return;
            if (UserBean.tryGetInstance()?.gameData == null || MapManager.getInstance() == null) { Open = false; return; }
            Theme.Build();
            var matrix = GUI.matrix;
            var skinWindow = GUI.skin.window;
            try
            {
                GUI.depth = -20000;
                GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * Scale);
                float w = Screen.width / Scale, h = Screen.height / Scale;
                _rect.width = Mathf.Min(1040, w - 12);
                _rect.height = Mathf.Min(660, h - 12);
                _rect.x = Mathf.Clamp(_rect.x, 6, Mathf.Max(6, w - _rect.width - 6));
                _rect.y = Mathf.Clamp(_rect.y, 6, Mathf.Max(6, h - _rect.height - 6));
                GUI.skin.window = Theme.Window;
                _rect = GUI.Window(WindowId, _rect, DrawWindow, "");
            }
            finally
            {
                GUI.matrix = matrix;
                GUI.skin.window = skinWindow;
                GUI.depth = 0;
            }
        }

        private static void DrawWindow(int id)
        {
            try
            {
                float w = _rect.width, h = _rect.height;
                GUI.Label(new Rect(20, 12, 400, 30), Loc.T("mod.title"), Theme.Title);
                GUI.Label(new Rect(20, 40, 400, 20), Loc.T("ui.window.hint"), Theme.Weak);

                // Canto de cima: idioma, Dev e fechar.
                if (GUI.Button(new Rect(w - 44, 12, 30, 30), new GUIContent("X"), Theme.Button)) Open = false;
                bool dev = GUI.Toggle(new Rect(w - 124, 12, 72, 30), _dev, new GUIContent("Dev"), Theme.Button);
                if (dev != _dev) { _dev = dev; if (!_dev) _tab = 0; }
                if (GUI.Button(new Rect(w - 244, 12, 112, 30), new GUIContent(Loc.T("ui.language_button")), Theme.Button))
                    ModEntry.SetModLanguage(Loc.Current == Loc.Portuguese ? Loc.English : Loc.Portuguese);

                var heroes = Heroes();
                DrawHeroList(new Rect(20, 72, 230, h - 92), heroes);

                var hero = heroes.Find(x => x.heroBean.uid == _heroUid);
                if (hero == null && heroes.Count > 0) { hero = heroes[0]; _heroUid = hero.heroBean.uid; }

                // Abas.
                float x0 = 270;
                if (GUI.Toggle(new Rect(x0, 72, 140, 32), _tab == 0, new GUIContent(Loc.T("ui.cultivation")), Theme.Tab)) _tab = 0;
                if (_dev && GUI.Toggle(new Rect(x0 + 148, 72, 140, 32), _tab == 1, new GUIContent("Dev"), Theme.Tab)) _tab = 1;
                var body = new Rect(x0, 112, w - x0 - 20, h - 132);
                GUI.Box(body, GUIContent.none, Theme.Card);
                var inner = new Rect(body.x + 16, body.y + 14, body.width - 32, body.height - 28);
                if (hero == null) GUI.Label(inner, Loc.T("ui.window.no_hero"), Theme.Text);
                else if (_tab == 1 && _dev) DrawDev(inner, hero);
                else DrawCultivation(inner, hero);
            }
            catch (Exception ex)
            {
                ModLog.Error(ex, "janela Wuxia");
                Open = false;
            }
            GUI.DragWindow(new Rect(0, 0, 10000, 64));
        }

        // ---------- colonos ----------

        private static List<BaseHero> Heroes()
        {
            var list = new List<BaseHero>();
            var manager = MapManager.getInstance()?.heroManager;
            var all = UserBean.getInstance()?.getAllHeros();
            if (manager == null || all == null) return list;
            foreach (var bean in all)
            {
                if (bean == null) continue;
                var hero = manager.getHeroByUID(bean.uid);
                if (hero != null && !list.Contains(hero)) list.Add(hero);
            }
            return list;
        }

        private static void DrawHeroList(Rect area, List<BaseHero> heroes)
        {
            GUI.Box(area, GUIContent.none, Theme.Card);
            var view = new Rect(0, 0, area.width - 24, heroes.Count * 50);
            BeginScroll(new Rect(area.x + 4, area.y + 4, area.width - 8, area.height - 8), ref _heroScroll, view.height);
            for (int i = 0; i < heroes.Count; i++)
            {
                var bean = heroes[i].heroBean;
                bool android = IsAndroid(bean);
                var state = CultivationRegistry.Get(bean.uid, false);
                string realm = android ? Loc.T("ui.window.android") : state?.Realm != null ? Loc.T(state.Realm.NameKey) : Loc.T("ui.mortal");
                var r = new Rect(0, i * 50, view.width, 46);
                if (GUI.Toggle(r, bean.uid == _heroUid, new GUIContent(""), Theme.ListItem)) _heroUid = bean.uid;
                GUI.Label(new Rect(r.x + 10, r.y + 3, r.width - 14, 22), bean.name, Theme.Text);
                GUI.Label(new Rect(r.x + 10, r.y + 23, r.width - 14, 20), realm, Theme.Weak);
            }
            EndScroll();
        }

        /// <summary>
        /// Rolagem feita à mão: o IMGUI do jogo vem sem GUI.BeginScrollView (o Unity tira o que o
        /// jogo não usa). Barra à direita + roda do mouse; o conteúdo vai num grupo deslocado.
        /// Fechar com GUI.EndGroup().
        /// </summary>
        private static void BeginScroll(Rect area, ref Vector2 scroll, float contentHeight)
        {
            float max = Math.Max(0, contentHeight - area.height);
            var e = Event.current;
            if (e != null && e.type == EventType.ScrollWheel && area.Contains(e.mousePosition))
            {
                scroll.y += e.delta.y * 30f;
                e.Use();
            }
            if (max > 0)
                scroll.y = GUI.VerticalScrollbar(new Rect(area.xMax - 16, area.y, 16, area.height), scroll.y, area.height, 0, contentHeight, GUI.skin.verticalScrollbar);
            scroll.y = Mathf.Clamp(scroll.y, 0, max);
            GUI.BeginGroup(area, GUIContent.none, GUIStyle.none);
            GUI.BeginGroup(new Rect(0, -scroll.y, area.width - 18, contentHeight + area.height), GUIContent.none, GUIStyle.none);
        }

        private static void EndScroll()
        {
            GUI.EndGroup();
            GUI.EndGroup();
        }

        // ---------- aba Cultivo ----------

        private static void DrawCultivation(Rect r, BaseHero hero)
        {
            var bean = hero.heroBean;
            float y = r.y;
            GUI.Label(new Rect(r.x, y, r.width, 30), bean.name, Theme.Title);
            y += 36;
            if (IsAndroid(bean))
            {
                GUI.Label(new Rect(r.x, y, r.width, 40), Loc.T("ui.android_no_cultivation"), Theme.Text);
                return;
            }
            var state = CultivationRegistry.Get(bean.uid, true);
            long now = UserBean.getInstance().getTime();
            double hour = WorldTimeTools.gameHour_to_mm(1f);

            var root = state.Root;
            Line(r, ref y, Loc.T("ui.spirit_root"), root != null ? Loc.T(root.NameKey) : "?");
            if (root != null && root.CanCultivate) { GUI.Label(new Rect(r.x, y, r.width, 22), Loc.T(root.DescKey), Theme.Weak); y += 26; }

            var realm = state.Realm;
            if (realm == null)
            {
                Line(r, ref y, Loc.T("ui.realm"), Loc.T("ui.mortal"));
                GUI.Label(new Rect(r.x, y, r.width, 24), state.CanCultivate ? Loc.T("ui.can_awaken") : Loc.T("ui.cannot_cultivate"), Theme.Text);
                y += 36;
                if (state.CanCultivate && GUI.Button(new Rect(r.x, y, 220, 40), new GUIContent(Loc.T("ui.awaken_button")), Theme.Primary))
                {
                    if (state.Awaken()) ModLog.Info($"{bean.name} (uid {bean.uid}) despertou o cultivo com {state.RootId}.");
                }
                return;
            }

            Line(r, ref y, Loc.T("ui.realm"), $"{Loc.T(realm.NameKey)}  ({realm.Order}/{RealmDatabase.All.Count})");
            GUI.Label(new Rect(r.x, y, r.width, 40), Loc.T(realm.DescKey), Theme.Weak);
            y += 44;

            Bar(new Rect(r.x, y, r.width, 30), Loc.T("ui.qi"), state.Qi, realm.MaxQi, Theme.QiColor);
            y += 38;
            Bar(new Rect(r.x, y, r.width, 30), Loc.T("ui.progress"), state.Progress, realm.Exp, Theme.ProgressColor);
            y += 44;

            GUI.Label(new Rect(r.x, y, r.width, 22), Loc.T("ui.points", state.UnspentPoints), Theme.Text);
            y += 26;
            if (state.InDeviation(now))
            {
                GUI.Label(new Rect(r.x, y, r.width, 22), Loc.T("ui.deviation_left", Math.Ceiling((state.DeviationUntil - now) / hour)), Theme.Bad);
                y += 26;
            }
            if (state.AtPeakRealm)
            {
                GUI.Label(new Rect(r.x, y, r.width, 22), Loc.T("ui.breakthrough.at_peak"), Theme.Accent);
                y += 26;
            }
            else
            {
                float chance = state.BreakthroughChance(state.ActiveBreakBonus(now));
                string line = Loc.T("ui.break_chance", (chance * 100f).ToString("0"), Loc.T(state.NextRealm.NameKey));
                if (realm.BreakthroughItem != null) line += "  " + Loc.T("ui.breakthrough.missing_item", Breakthrough.ItemName(realm.BreakthroughItem));
                GUI.Label(new Rect(r.x, y, r.width, 22), line, Theme.Text);
                y += 26;
                if (state.ReadyForBreakthrough)
                {
                    GUI.Label(new Rect(r.x, y, r.width, 22), Loc.T(realm.BreakthroughItem != null ? "ui.ready_item" : "ui.ready_platform"), Theme.Accent);
                    y += 26;
                }
            }
            float qiBoost = state.ActiveQiBoost(now);
            if (qiBoost > 0f) { GUI.Label(new Rect(r.x, y, r.width, 22), Loc.T("ui.qi_boost", (qiBoost * 100f).ToString("0"), Math.Ceiling((state.QiBoostUntil - now) / hour)), Theme.Ok); y += 26; }
            float breakBonus = state.ActiveBreakBonus(now);
            if (breakBonus > 0f) { GUI.Label(new Rect(r.x, y, r.width, 22), Loc.T("ui.break_bonus", (breakBonus * 100f).ToString("0"), Math.Ceiling((state.BreakBonusUntil - now) / hour)), Theme.Ok); y += 26; }
            GUI.Label(new Rect(r.x, y, r.width, 40), Loc.T("ui.meditation_hint"), Theme.Weak);

            if (!state.AtPeakRealm && GUI.Button(new Rect(r.x, r.yMax - 40, 240, 40), new GUIContent(Loc.T("ui.break_button")), Theme.Primary))
                TryBreakthrough(hero, state);
        }

        /// <summary>Ruptura pelo botão: precisa de uma Plataforma de Ruptura pronta no mapa.</summary>
        private static void TryBreakthrough(BaseHero hero, CultivationState state)
        {
            var platforms = MapManager.getInstance().buildManager?.getBuildTypeID(11, ModEntry.HarmonyId + "_" + Patches.MeditationPatch.BreakPlatformId);
            if (platforms == null || !platforms.Exists(b => b != null && b.isPutdown()))
                GUI_RoleInfo.showRoleInfo(hero, Loc.T("ui.need_platform"));
            else
                Breakthrough.Attempt(hero, state, false, Breakthrough.PlatformBonus, "botão");
        }

        private static void Line(Rect r, ref float y, string label, string value)
        {
            GUI.Label(new Rect(r.x, y, 180, 24), label, Theme.Accent);
            GUI.Label(new Rect(r.x + 180, y, r.width - 180, 24), value, Theme.Text);
            y += 28;
        }

        /// <summary>Barra que enche conforme value/max, com "rótulo  atual / máximo" no meio.</summary>
        private static void Bar(Rect r, string label, float value, float max, Color fill)
        {
            float t = max > 0 ? Mathf.Clamp01(value / max) : 0f;
            GUI.DrawTexture(r, Theme.Tex(Theme.BarEdge));
            var inside = new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4);
            GUI.DrawTexture(inside, Theme.Tex(Theme.BarBack));
            if (t > 0) GUI.DrawTexture(new Rect(inside.x, inside.y, inside.width * t, inside.height), Theme.Tex(fill));
            // Brilho na metade de cima, para parecer uma barra e não um retângulo chapado.
            if (t > 0) GUI.DrawTexture(new Rect(inside.x, inside.y, inside.width * t, inside.height * 0.4f), Theme.Tex(new Color(1, 1, 1, 0.18f)));
            GUI.Label(r, $"{label}   {value:0} / {max:0}   ({t * 100f:0}%)", Theme.BarText);
        }

        // ---------- aba Dev ----------

        private static void DrawDev(Rect r, BaseHero hero)
        {
            var bean = hero.heroBean;
            float bw = (r.width - 30) / 4f, y = r.y;
            var state = IsAndroid(bean) ? null : CultivationRegistry.Get(bean.uid, true);

            if (GUI.Button(new Rect(r.x, y, bw, 40), new GUIContent(Loc.T("dev.drop_all")), Theme.Button))
            {
                int n = TestMode.DropAllItems(hero);
                GUI_RoleInfo.showRoleInfo(hero, Loc.T("dev.dropped", n));
            }
            if (GUI.Button(new Rect(r.x + (bw + 10), y, bw, 40), new GUIContent(Loc.T("dev.fill")), Theme.Button) && state != null)
                Fill(hero, state);
            if (GUI.Button(new Rect(r.x + 2 * (bw + 10), y, bw, 40), new GUIContent(Loc.T("dev.break_now")), Theme.Button) && state != null)
            {
                Fill(hero, state);
                if (!state.AtPeakRealm) Breakthrough.Attempt(hero, state, true, 10f, "dev");
            }
            if (GUI.Button(new Rect(r.x + 3 * (bw + 10), y, bw, 40), new GUIContent(Loc.T("dev.drop_break_item")), Theme.Button) && state != null)
            {
                string item = state.Realm?.BreakthroughItem;
                if (item == null) GUI_RoleInfo.showRoleInfo(hero, Loc.T("dev.no_break_item"));
                else
                {
                    var def = ImplDef<T_MedicineDef>.getDataID(item) as BaseDef;
                    if (def != null) TestMode.DropOne(hero, def);
                }
            }
            y += 54;

            GUI.Label(new Rect(r.x, y, 160, 26), Loc.T("dev.items"), Theme.Accent);
            // GUI.TextField não existe no IMGUI do jogo; o do GUILayout sim.
            GUILayout.BeginArea(new Rect(r.x + 170, y, 260, 26));
            _filter = GUILayout.TextField(_filter ?? "", GUILayout.Width(260), GUILayout.Height(26));
            GUILayout.EndArea();
            y += 34;

            var items = Items();
            var shown = new List<BaseDef>();
            foreach (var d in items)
                if (string.IsNullOrEmpty(_filter) || Name(d).IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0) shown.Add(d);

            const float rowH = 40;
            var area = new Rect(r.x, y, r.width, r.yMax - y);
            var view = new Rect(0, 0, area.width - 20, shown.Count * rowH);
            BeginScroll(area, ref _itemScroll, view.height);
            int first = Math.Max(0, (int)(_itemScroll.y / rowH) - 1);
            int last = Math.Min(shown.Count, first + (int)(area.height / rowH) + 3);
            for (int i = first; i < last; i++)
            {
                var d = shown[i];
                var row = new Rect(0, i * rowH, view.width, rowH - 4);
                GUI.Box(row, GUIContent.none, Theme.Row);
                DrawIcon(new Rect(row.x + 4, row.y + 2, 32, 32), d);
                GUI.Label(new Rect(row.x + 44, row.y + 6, row.width - 170, 24), Name(d), Theme.Text);
                if (GUI.Button(new Rect(row.xMax - 116, row.y + 4, 110, 28), new GUIContent(Loc.T("dev.drop")), Theme.Button))
                    TestMode.DropOne(hero, d);
            }
            EndScroll();
        }

        private static void Fill(BaseHero hero, CultivationState state)
        {
            if (!state.CanCultivate) { GUI_RoleInfo.showRoleInfo(hero, Loc.T("dev.no_root")); return; }
            state.Awaken();
            var realm = state.Realm;
            if (realm == null) return;
            state.Qi = realm.MaxQi;
            state.Progress = realm.Exp;
            state.DeviationUntil = 0;
            RealmBuffs.Sync(hero, state);
        }

        private static List<BaseDef> Items()
        {
            if (_items != null) return _items;
            var list = new List<BaseDef>();
            Add<T_MedicineDef>(list);
            Add<T_MaterialDef>(list);
            Add<T_WeaponDef>(list);
            Add<T_ClothDef>(list);
            _items = list;
            return list;
        }

        private static void Add<T>(List<BaseDef> list) where T : BaseDef
        {
            var beans = ImplDef<T>.beans;
            if (beans == null) return;
            foreach (var d in beans)
                if (d?.mod != null && d.mod.packageId == ModEntry.HarmonyId) list.Add(d);
        }

        private static string Name(BaseDef d)
        {
            try { return d.getName() ?? d.ID; } catch { return d.ID; }
        }

        private static void DrawIcon(Rect r, BaseDef d)
        {
            var tex = Icon(d);
            if (tex != null) GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit);
        }

        // Ícone lido do PNG do mod (o sprite do jogo fica num atlas, e o IMGUI do jogo não
        // tem DrawTextureWithTexCoords para recortar).
        private static readonly Dictionary<string, Texture2D> Icons = new Dictionary<string, Texture2D>();

        private static Texture2D Icon(BaseDef d)
        {
            if (string.IsNullOrEmpty(d.RawIcon) || d.mod == null) return null;
            if (Icons.TryGetValue(d.RawIcon, out var tex)) return tex;
            tex = null;
            try
            {
                var path = d.mod.getVersionPath("Resources/Textures/", d.RawIcon);
                if (System.IO.File.Exists(path))
                {
                    tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, hideFlags = HideFlags.HideAndDontSave };
                    if (!tex.LoadImage(System.IO.File.ReadAllBytes(path))) tex = null;
                }
            }
            catch { tex = null; }
            Icons[d.RawIcon] = tex;
            return tex;
        }

        private static bool IsAndroid(FWHeroBean hero)
        {
            try { return hero.isAndroid(); } catch { return false; }
        }

        // ---------- aparência ----------

        private static class Theme
        {
            public static GUIStyle Window, Title, Text, Weak, Accent, Ok, Bad, Button, Primary, Tab, Card, Row, ListItem, BarText;
            public static readonly Color QiColor = new Color(0.30f, 0.62f, 0.95f);
            public static readonly Color ProgressColor = new Color(0.90f, 0.70f, 0.25f);
            public static readonly Color BarBack = new Color(0.08f, 0.09f, 0.11f);
            public static readonly Color BarEdge = new Color(0.55f, 0.45f, 0.25f);
            private static readonly Dictionary<Color, Texture2D> Textures = new Dictionary<Color, Texture2D>();
            private static bool _built;

            private static readonly string[] Fonts = { "Microsoft YaHei UI", "Microsoft YaHei", "Segoe UI", "Noto Sans SC", "SimHei", "Arial" };

            public static Texture2D Tex(Color c)
            {
                if (Textures.TryGetValue(c, out var t) && t != null) return t;
                t = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
                t.SetPixel(0, 0, c);
                t.Apply();
                Textures[c] = t;
                return t;
            }

            public static void Build()
            {
                if (_built && Window?.normal.background != null) return;
                _built = true;
                Font font = null;
                try { font = Font.CreateDynamicFontFromOSFont(Fonts, 16); } catch { }

                Window = Box(new Color(0.12f, 0.13f, 0.16f, 0.98f));
                Window.border = new RectOffset(2, 2, 2, 2);
                Card = Box(new Color(0.16f, 0.17f, 0.21f, 1f));
                Row = Box(new Color(0.19f, 0.20f, 0.24f, 1f));

                Text = Label(font, 15, new Color(0.92f, 0.92f, 0.92f));
                Title = Label(font, 22, new Color(0.95f, 0.80f, 0.40f), FontStyle.Bold);
                Weak = Label(font, 13, new Color(0.65f, 0.67f, 0.70f));
                Accent = Label(font, 15, new Color(0.95f, 0.80f, 0.40f));
                Ok = Label(font, 15, new Color(0.55f, 0.85f, 0.55f));
                Bad = Label(font, 15, new Color(1f, 0.47f, 0.40f));
                BarText = Label(font, 15, Color.white, FontStyle.Bold);
                BarText.alignment = TextAnchor.MiddleCenter;
                BarText.wordWrap = false;

                Button = Btn(font, new Color(0.25f, 0.27f, 0.32f), new Color(0.33f, 0.36f, 0.42f), new Color(0.55f, 0.40f, 0.15f));
                Primary = Btn(font, new Color(0.62f, 0.40f, 0.12f), new Color(0.75f, 0.50f, 0.18f), new Color(0.50f, 0.30f, 0.08f));
                Tab = Btn(font, new Color(0.18f, 0.19f, 0.23f), new Color(0.25f, 0.27f, 0.32f), new Color(0.62f, 0.40f, 0.12f));
                ListItem = Btn(font, new Color(0.16f, 0.17f, 0.21f, 0f), new Color(0.25f, 0.27f, 0.32f), new Color(0.45f, 0.32f, 0.12f));


            }

            private static GUIStyle Box(Color c)
            {
                var s = new GUIStyle();
                s.normal.background = Tex(c);
                return s;
            }

            private static GUIStyle Label(Font font, int size, Color color, FontStyle style = FontStyle.Normal)
            {
                var s = new GUIStyle { font = font, fontSize = size, fontStyle = style, wordWrap = true, richText = true, alignment = TextAnchor.UpperLeft };
                s.normal.textColor = color;
                return s;
            }

            // "on" = selecionado (aba atual, Dev ligado, colono escolhido).
            private static GUIStyle Btn(Font font, Color normal, Color hover, Color on)
            {
                var s = new GUIStyle { font = font, fontSize = 15, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                s.normal.background = Tex(normal);
                s.hover.background = Tex(hover);
                s.active.background = Tex(on);
                s.onNormal.background = s.onHover.background = s.onActive.background = Tex(on);
                s.normal.textColor = s.hover.textColor = s.active.textColor = Color.white;
                s.onNormal.textColor = s.onHover.textColor = s.onActive.textColor = Color.white;
                s.padding = new RectOffset(6, 6, 4, 4);
                return s;
            }
        }
    }
}
