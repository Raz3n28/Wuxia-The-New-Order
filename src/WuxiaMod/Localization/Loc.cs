using System;
using System.Collections.Generic;
using System.IO;
using WuxiaMod.Json;

namespace WuxiaMod.Localization
{
    /// <summary>
    /// Textos do mod em inglês e português. Cada idioma é um JSON plano
    /// "chave": "texto" em v_common/Languages. Se faltar uma chave no idioma
    /// atual, usa o inglês; se faltar no inglês, mostra a própria chave.
    /// </summary>
    public static class Loc
    {
        public const string English = "en";
        public const string Portuguese = "pt-BR";
        public static readonly string[] Supported = { English, Portuguese };

        private static readonly Dictionary<string, Dictionary<string, string>> Tables =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public static string Current { get; private set; } = English;

        public static void LoadAll(string languagesDir)
        {
            Tables.Clear();
            foreach (var lang in Supported)
            {
                var path = Path.Combine(languagesDir, lang + ".json");
                if (!File.Exists(path))
                {
                    ModLog.Warn($"Arquivo de idioma não encontrado: {path}");
                    continue;
                }
                var table = new Dictionary<string, string>();
                var root = (Dictionary<string, object>)MiniJson.Parse(File.ReadAllText(path));
                foreach (var kv in root)
                    if (kv.Value is string s) table[kv.Key] = s;
                Tables[lang] = table;
            }
        }

        /// <summary>
        /// Escolhe o idioma a partir de um código qualquer ("pt", "pt-BR",
        /// "Portuguese", "en-US"...). Tudo que não for português vira inglês.
        /// </summary>
        public static void SetLanguage(string code)
        {
            Current = Normalize(code);
        }

        public static string Normalize(string code)
        {
            if (string.IsNullOrEmpty(code)) return English;
            var c = code.Trim().ToLowerInvariant();
            return c.StartsWith("pt") || c.StartsWith("portu") ? Portuguese : English;
        }

        public static string T(string key)
        {
            if (Tables.TryGetValue(Current, out var t) && t.TryGetValue(key, out var s)) return s;
            if (Tables.TryGetValue(English, out var en) && en.TryGetValue(key, out var e)) return e;
            return key;
        }

        /// <summary>Texto com parâmetros: T("realm.level", 3) para "Nível {0}".</summary>
        public static string T(string key, params object[] args) => string.Format(T(key), args);
    }
}
