using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using WuxiaMod.Json;

namespace WuxiaMod.Cultivation
{
    /// <summary>
    /// Estado de cultivo de cada personagem, pelo uid do jogo.
    /// A raiz espiritual é sorteada com o uid como semente na primeira vez que o
    /// personagem aparece; depois fica gravada no save (veja CultivationSave).
    /// O jogo salva em outra thread, então todo acesso passa pelo lock.
    /// </summary>
    public static class CultivationRegistry
    {
        private const int FormatVersion = 1;

        private static readonly object Lock = new object();
        private static readonly Dictionary<long, CultivationState> States = new Dictionary<long, CultivationState>();

        /// <param name="canHaveRoot">false para androides/robôs: nunca cultivam.</param>
        public static CultivationState Get(long uid, bool canHaveRoot)
        {
            lock (Lock)
            {
                if (States.TryGetValue(uid, out var state)) return state;
                state = new CultivationState();
                if (canHaveRoot)
                {
                    var root = SpiritRootDatabase.Roll(new Random(SeedFor(uid)));
                    state.RootId = root?.Id;
                }
                States[uid] = state;
                return state;
            }
        }

        public static int Count { get { lock (Lock) return States.Count; } }

        public static void Clear() { lock (Lock) States.Clear(); }

        /// <summary>
        /// Grava todos os personagens. Exemplo:
        /// {"version":1,"heroes":{"123":{"root":"fire","realm":1,"level":2,"qi":15.5,"points":1}}}
        /// </summary>
        public static string ToJson()
        {
            var sb = new StringBuilder();
            sb.Append("{\"version\":").Append(FormatVersion).Append(",\"heroes\":{");
            lock (Lock)
            {
                bool first = true;
                foreach (var pair in States)
                {
                    var s = pair.Value;
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append("\n\"").Append(pair.Key.ToString(CultureInfo.InvariantCulture)).Append("\":{");
                    sb.Append("\"root\":").Append(s.RootId == null ? "null" : "\"" + Escape(s.RootId) + "\"");
                    sb.Append(",\"realm\":").Append(s.RealmOrder.ToString(CultureInfo.InvariantCulture));
                    sb.Append(",\"level\":").Append(s.SubLevel.ToString(CultureInfo.InvariantCulture));
                    sb.Append(",\"qi\":").Append(s.Qi.ToString("R", CultureInfo.InvariantCulture));
                    sb.Append(",\"points\":").Append(s.UnspentPoints.ToString(CultureInfo.InvariantCulture));
                    if (s.QiBoostUntil > 0)
                        sb.Append(",\"qiBoost\":").Append(s.QiBoost.ToString("R", CultureInfo.InvariantCulture))
                          .Append(",\"qiBoostUntil\":").Append(s.QiBoostUntil.ToString(CultureInfo.InvariantCulture));
                    if (s.BreakBonusUntil > 0)
                        sb.Append(",\"breakBonus\":").Append(s.BreakBonus.ToString("R", CultureInfo.InvariantCulture))
                          .Append(",\"breakBonusUntil\":").Append(s.BreakBonusUntil.ToString(CultureInfo.InvariantCulture));
                    sb.Append('}');
                }
            }
            sb.Append("\n}}\n");
            return sb.ToString();
        }

        /// <summary>Troca tudo pelo conteúdo gravado. Retorna quantos personagens leu.</summary>
        public static int LoadJson(string json)
        {
            var root = MiniJson.Parse(json) as Dictionary<string, object>;
            if (root == null) throw new FormatException("save de cultivo não é um objeto JSON");
            var heroes = root.TryGetValue("heroes", out var h) ? h as Dictionary<string, object> : null;

            var loaded = new Dictionary<long, CultivationState>();
            if (heroes != null)
            {
                foreach (var pair in heroes)
                {
                    if (!long.TryParse(pair.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out long uid)) continue;
                    if (!(pair.Value is Dictionary<string, object> o)) continue;
                    var s = new CultivationState
                    {
                        RootId = o.Str("root"),
                        RealmOrder = o.Int("realm"),
                        SubLevel = o.Int("level"),
                        Qi = o.Float("qi"),
                        UnspentPoints = o.Int("points"),
                        QiBoost = o.Float("qiBoost"),
                        QiBoostUntil = o.Long("qiBoostUntil"),
                        BreakBonus = o.Float("breakBonus"),
                        BreakBonusUntil = o.Long("breakBonusUntil"),
                    };
                    // Estágio que não existe mais nos dados (realms.json mudou): volta para o último que existe.
                    while (s.RealmOrder > 0 && s.Realm == null) s.RealmOrder--;
                    if (s.Realm != null && s.SubLevel < 1) s.SubLevel = 1;
                    if (s.Realm != null && s.SubLevel > s.Realm.SubLevels) s.SubLevel = s.Realm.SubLevels;
                    loaded[uid] = s;
                }
            }

            lock (Lock)
            {
                States.Clear();
                foreach (var pair in loaded) States[pair.Key] = pair.Value;
            }
            return loaded.Count;
        }

        private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        private static int SeedFor(long uid) => unchecked((int)(uid ^ (uid >> 32)) ^ 0x5EED);
    }
}
