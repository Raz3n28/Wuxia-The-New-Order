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
    /// Também põe os textos em inglês das nossas linhas no LanguageDef e registra
    /// as "comidas" escondidas que dão os bônus das pílulas (veja PillEffects).
    /// </summary>
    internal static class DefAliases
    {
        private static string Prefix => ModEntry.HarmonyId + "_";
        private static bool _logged;

        public static void Apply()
        {
            int added = 0;
            added += Register<T_MaterialDef>();
            added += Register<T_MedicineDef>();
            added += Register<F_ProductListDef>();
            added += Register<F_ManufactureDef>();
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
