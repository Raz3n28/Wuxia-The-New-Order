using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using WuxiaMod.Json;
using WuxiaMod.Localization;

namespace WuxiaMod.Game
{
    /// <summary>
    /// Mostra os textos das nossas linhas do def.xml (itens, construções, receitas)
    /// no idioma escolhido no mod (inglês ou português), seja qual for o idioma do jogo.
    ///
    /// O def.xml tem o texto em chinês nos campos (Name, Desc, Show_value4...) e uma
    /// linha LanguageDef "id_" + chinês com o inglês. O jogo em chinês mostra o chinês,
    /// e em inglês procura essa linha. Aqui trocamos as duas coisas:
    /// - CN e EN das nossas linhas LanguageDef viram o texto do idioma do mod;
    /// - os campos de texto das nossas definições viram esse texto também (alguns
    ///   lugares do jogo mostram o campo direto, sem passar pelo LanguageDef).
    /// O português vem de Languages/defs.pt-BR.json, com o inglês como chave.
    /// </summary>
    internal static class DefTexts
    {
        // chinês original -> inglês (das nossas linhas LanguageDef)
        private static readonly Dictionary<string, string> English = new Dictionary<string, string>();
        // inglês -> português
        private static readonly Dictionary<string, string> Portuguese = new Dictionary<string, string>();
        // qualquer texto que já pusemos num campo -> chinês original, para trocar de idioma depois
        private static readonly Dictionary<string, string> Original = new Dictionary<string, string>();

        // Objetos já atualizados para a versão atual do idioma (o jogo pode recriar as linhas).
        private static readonly Dictionary<object, int> Done = new Dictionary<object, int>();
        private static int _version = 1;

        private static readonly Dictionary<Type, FieldInfo[]> StringFields = new Dictionary<Type, FieldInfo[]>();

        public static void LoadPortuguese(string languagesDir)
        {
            Portuguese.Clear();
            var path = Path.Combine(languagesDir, "defs.pt-BR.json");
            if (!File.Exists(path))
            {
                ModLog.Warn($"Arquivo de idioma não encontrado: {path}");
                return;
            }
            var root = (Dictionary<string, object>)MiniJson.Parse(File.ReadAllText(path));
            foreach (var kv in root)
                if (kv.Value is string s && !kv.Key.StartsWith("_", StringComparison.Ordinal)) Portuguese[kv.Key] = s;
        }

        /// <summary>Idioma do mod mudou: na próxima passada tudo é atualizado de novo.</summary>
        public static void Invalidate() => _version++;

        /// <summary>Chamado pelo DefAliases a cada passada. Só mexe no que ainda não foi atualizado.</summary>
        public static int ApplyLanguageRows(IList beans)
        {
            int changed = 0;
            for (int i = 0; i < beans.Count; i++)
            {
                if (!(beans[i] is LanguageDef row) || row.mod == null || row.mod.packageId != ModEntry.HarmonyId) continue;
                if (Done.TryGetValue(row, out var v) && v == _version) continue;
                if (!Done.ContainsKey(row))
                {
                    // Linha nova (vinda do def.xml): guarda o chinês e o inglês originais.
                    if (row.CN != null && row.EN != null) English[row.CN] = row.EN;
                }
                string cn = OriginalOf(row.CN);
                if (cn != null && English.ContainsKey(cn))
                {
                    string text = Translate(cn);
                    Remember(text, cn);
                    row.CN = text;
                    row.EN = text;
                    changed++;
                }
                Done[row] = _version;
            }
            return changed;
        }

        /// <summary>Troca os campos de texto de uma definição nossa pelo idioma do mod.</summary>
        public static void ApplyDef(object def)
        {
            if (def == null || English.Count == 0) return;
            if (Done.TryGetValue(def, out var v) && v == _version) return;
            foreach (var field in FieldsOf(def.GetType()))
            {
                if (!(field.GetValue(def) is string value) || value.Length == 0) continue;
                string cn = OriginalOf(value);
                if (cn == null || !English.ContainsKey(cn)) continue;
                string text = Translate(cn);
                if (text == value) continue;
                Remember(text, cn);
                field.SetValue(def, text);
            }
            ClearInfoCaches(def);
            Done[def] = _version;
        }

        private static string Translate(string cn)
        {
            string en = English[cn];
            if (Loc.Current == Loc.Portuguese && Portuguese.TryGetValue(en, out var pt)) return pt;
            return en;
        }

        private static string OriginalOf(string value)
        {
            if (value == null) return null;
            if (English.ContainsKey(value)) return value;
            return Original.TryGetValue(value, out var cn) ? cn : null;
        }

        private static void Remember(string text, string cn)
        {
            if (!English.ContainsKey(text)) Original[text] = cn;
        }

        private static FieldInfo[] FieldsOf(Type type)
        {
            if (StringFields.TryGetValue(type, out var fields)) return fields;
            var list = new List<FieldInfo>();
            foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                if (f.FieldType == typeof(string) && !f.IsInitOnly && f.Name != "ID") list.Add(f);
            fields = list.ToArray();
            StringFields[type] = fields;
            return fields;
        }

        /// <summary>
        /// A janela de detalhes guarda as linhas "Efeito: valor" numa lista na primeira vez
        /// (baseDefShowInfos...). Esvaziar faz o jogo montar de novo com o texto novo.
        /// </summary>
        private static void ClearInfoCaches(object def)
        {
            for (var t = def.GetType(); t != null && t != typeof(object); t = t.BaseType)
            {
                foreach (var f in t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (f.FieldType != typeof(List<string>) || !f.Name.StartsWith("baseDef", StringComparison.Ordinal)) continue;
                    (f.GetValue(def) as List<string>)?.Clear();
                }
            }
        }
    }
}
