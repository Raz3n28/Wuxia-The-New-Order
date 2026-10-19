using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace WuxiaMod.Game
{
    /// <summary>
    /// Deixa o jogo achar as nossas linhas do def.xml pelo ID curto.
    ///
    /// O jogo troca o ID de cada linha de mod para packageId + "_" + ID
    /// (96101 vira pauloraz3n.wuxia_96101). As receitas usam texto "tipo_ID_qtd"
    /// separado por "_", então não cabem o ID completo: "12_96101_2" procura "96101".
    /// Aqui registramos o ID curto (e o completo) no beansDict de cada tabela.
    /// O jogo pode recriar esses dicionários (ao recarregar as definições),
    /// por isso o WuxiaRuntime chama Apply() de novo de tempos em tempos.
    ///
    /// Também põe os textos das nossas linhas no LanguageDef, troca os textos para o
    /// idioma do mod (veja DefTexts) e registra as "comidas" escondidas que dão os
    /// bônus das pílulas (veja PillEffects).
    /// </summary>
    internal static class DefAliases
    {
        private static string Prefix => ModEntry.HarmonyId + "_";
        private static bool _logged;

        public static void Apply()
        {
            int added = 0;
            if (ImplDef<LanguageDef>.beans != null) DefTexts.ApplyLanguageRows(ImplDef<LanguageDef>.beans);
            added += Register<T_MaterialDef>();
            added += Register<T_MedicineDef>();
            added += Register<F_ProductListDef>();
            added += Register<F_ManufactureDef>();
            ApplyTexts<F_Entertainment>();
            FixSortIds();
            added += RegisterLanguage();
            added += PillEffects.RegisterBuffFoods();
            if (added > 0 && (!_logged || ModEntry.VerboseLog))
            {
                ModLog.Info($"Definições do mod registradas no jogo ({added} entradas).");
                _logged = true;
            }
        }

        private static int Register<T>() where T : BaseDef
        {
            var dict = ImplDef<T>.beansDict;
            var beans = ImplDef<T>.beans;
            if (dict == null || beans == null) return 0;
            int added = 0;
            string prefix = Prefix;
            for (int i = 0; i < beans.Count; i++)
            {
                var bean = beans[i];
                if (bean?.mod == null || bean.mod.packageId != ModEntry.HarmonyId || bean.ID == null) continue;
                TestMode.ApplyFastWork(bean);
                DefTexts.ApplyDef(bean);
                if (!dict.ContainsKey(bean.ID)) { dict[bean.ID] = bean; added++; }
                if (!bean.ID.StartsWith(prefix, StringComparison.Ordinal)) continue;
                string shortId = bean.ID.Substring(prefix.Length);
                if (!dict.TryGetValue(shortId, out var existing))
                {
                    dict[shortId] = bean;
                    added++;
                }
                else if (!ReferenceEquals(existing, bean) && existing?.mod?.packageId != ModEntry.HarmonyId)
                {
                    // Linha do jogo ou de outro mod com o mesmo ID curto: não sobrescreve.
                    ModLog.Warn($"ID curto {shortId} já existe em {typeof(T).Name}; receitas com ele vão usar a outra linha.");
                }
            }
            return added;
        }

        private static readonly HashSet<object> Sorted = new HashSet<object>();

        /// <summary>
        /// O menu de construção ordena por SortId com um sort instável, e as nossas bancadas
        /// copiam o SortId da bancada de remédios. Com SortId igual, os ícones e o item escolhido
        /// podem sair em ordens diferentes (o Caldeirão Pequeno aparecia com a imagem do Taiyi).
        /// Damos a cada uma um SortId único, depois de todas as bancadas do jogo.
        /// </summary>
        private static void FixSortIds()
        {
            var beans = ImplDef<F_ManufactureDef>.beans;
            if (beans == null) return;
            bool pending = false;
            int max = int.MinValue;
            foreach (var bean in beans)
            {
                if (bean == null) continue;
                if (bean.mod?.packageId == ModEntry.HarmonyId) { if (!Sorted.Contains(bean)) pending = true; }
                else if (bean.SortId > max) max = bean.SortId;
            }
            if (!pending) return;
            // Ordem fixa pelo ID: Caldeirão Pequeno, Taiyi, Bancada de Elixires.
            var ours = beans.FindAll(b => b?.mod?.packageId == ModEntry.HarmonyId);
            ours.Sort((a, b) => string.CompareOrdinal(a.ID, b.ID));
            foreach (var bean in ours)
            {
                bean.SortId = ++max;
                Sorted.Add(bean);
            }
        }

        /// <summary>Só os textos, para tabelas que não precisam do ID curto (a almofada de meditação).</summary>
        private static void ApplyTexts<T>() where T : BaseDef
        {
            var beans = ImplDef<T>.beans;
            if (beans == null) return;
            for (int i = 0; i < beans.Count; i++)
                if (beans[i]?.mod?.packageId == ModEntry.HarmonyId) DefTexts.ApplyDef(beans[i]);
        }

        private static FieldInfo _langDicts;

        /// <summary>LanguageDef.loadFinish monta o dicionário de textos uma vez; garante as nossas linhas nele.</summary>
        private static int RegisterLanguage()
        {
            if (_langDicts == null)
                _langDicts = typeof(LanguageDef).GetField("dicts", BindingFlags.NonPublic | BindingFlags.Static);
            if (!(_langDicts?.GetValue(null) is IDictionary dicts)) return 0;
            var beans = ImplDef<LanguageDef>.beans;
            if (beans == null) return 0;
            int added = 0;
            for (int i = 0; i < beans.Count; i++)
            {
                var row = beans[i];
                if (row?.mod == null || row.mod.packageId != ModEntry.HarmonyId || row.Key == null) continue;
                if (dicts.Contains(row.Key)) continue;
                dicts[row.Key] = row;
                added++;
            }
            return added;
        }
    }
}
