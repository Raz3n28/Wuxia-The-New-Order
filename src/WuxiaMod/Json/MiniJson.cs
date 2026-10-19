using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WuxiaMod.Json
{
    /// <summary>
    /// Leitor de JSON pequeno e sem dependências, para não depender de qual
    /// biblioteca de JSON o jogo carrega. Retorna Dictionary&lt;string, object&gt;,
    /// List&lt;object&gt;, string, double, bool ou null.
    /// </summary>
    public static class MiniJson
    {
        public static object Parse(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            var parser = new Parser(text);
            var value = parser.ReadValue();
            parser.SkipWhitespace();
            if (!parser.AtEnd) throw parser.Error("conteúdo extra depois do JSON");
            return value;
        }

        private sealed class Parser
        {
            private readonly string _s;
            private int _i;

            public Parser(string s)
            {
                _s = s;
                // Ignora BOM do UTF-8, comum em arquivos salvos no Windows.
                if (_s.Length > 0 && _s[0] == '﻿') _i = 1;
            }

            public bool AtEnd => _i >= _s.Length;

            public FormatException Error(string message) =>
                new FormatException($"JSON inválido na posição {_i}: {message}");

            public void SkipWhitespace()
            {
                while (!AtEnd && char.IsWhiteSpace(_s[_i])) _i++;
            }

            public object ReadValue()
            {
                SkipWhitespace();
                if (AtEnd) throw Error("fim inesperado");
                char c = _s[_i];
                switch (c)
                {
                    case '{': return ReadObject();
                    case '[': return ReadArray();
                    case '"': return ReadString();
                    case 't': Expect("true"); return true;
                    case 'f': Expect("false"); return false;
                    case 'n': Expect("null"); return null;
                    default:
                        if (c == '-' || char.IsDigit(c)) return ReadNumber();
                        throw Error($"caractere inesperado '{c}'");
                }
            }

            private void Expect(string word)
            {
                if (string.CompareOrdinal(_s, _i, word, 0, word.Length) != 0) throw Error($"esperava '{word}'");
                _i += word.Length;
            }

            private Dictionary<string, object> ReadObject()
            {
                var result = new Dictionary<string, object>();
                _i++; // {
                SkipWhitespace();
                if (!AtEnd && _s[_i] == '}') { _i++; return result; }
                while (true)
                {
                    SkipWhitespace();
                    if (AtEnd || _s[_i] != '"') throw Error("esperava nome de campo");
                    string key = ReadString();
                    SkipWhitespace();
                    if (AtEnd || _s[_i] != ':') throw Error("esperava ':'");
                    _i++;
                    result[key] = ReadValue();
                    SkipWhitespace();
                    if (AtEnd) throw Error("objeto sem '}'");
                    if (_s[_i] == ',') { _i++; continue; }
                    if (_s[_i] == '}') { _i++; return result; }
                    throw Error("esperava ',' ou '}'");
                }
            }

            private List<object> ReadArray()
            {
                var result = new List<object>();
                _i++; // [
                SkipWhitespace();
                if (!AtEnd && _s[_i] == ']') { _i++; return result; }
                while (true)
                {
                    result.Add(ReadValue());
                    SkipWhitespace();
                    if (AtEnd) throw Error("lista sem ']'");
                    if (_s[_i] == ',') { _i++; continue; }
                    if (_s[_i] == ']') { _i++; return result; }
                    throw Error("esperava ',' ou ']'");
                }
            }

            private string ReadString()
            {
                var sb = new StringBuilder();
                _i++; // "
                while (true)
                {
                    if (AtEnd) throw Error("texto sem aspas de fechamento");
                    char c = _s[_i++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\') { sb.Append(c); continue; }
                    if (AtEnd) throw Error("escape incompleto");
                    char e = _s[_i++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (_i + 4 > _s.Length) throw Error("escape \\u incompleto");
                            sb.Append((char)int.Parse(_s.Substring(_i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            _i += 4;
                            break;
                        default: throw Error($"escape desconhecido '\\{e}'");
                    }
                }
            }

            private double ReadNumber()
            {
                int start = _i;
                if (_s[_i] == '-') _i++;
                while (!AtEnd && (char.IsDigit(_s[_i]) || _s[_i] == '.' || _s[_i] == 'e' || _s[_i] == 'E' || _s[_i] == '+' || _s[_i] == '-')) _i++;
                string token = _s.Substring(start, _i - start);
                if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                    throw Error($"número inválido '{token}'");
                return value;
            }
        }
    }

    /// <summary>Atalhos para ler campos de um objeto JSON com valor padrão.</summary>
    public static class JsonExt
    {
        public static string Str(this Dictionary<string, object> o, string key, string fallback = null) =>
            o.TryGetValue(key, out var v) && v is string s ? s : fallback;

        public static int Int(this Dictionary<string, object> o, string key, int fallback = 0) =>
            o.TryGetValue(key, out var v) && v is double d ? (int)d : fallback;

        public static float Float(this Dictionary<string, object> o, string key, float fallback = 0f) =>
            o.TryGetValue(key, out var v) && v is double d ? (float)d : fallback;

        public static bool Bool(this Dictionary<string, object> o, string key, bool fallback = false) =>
            o.TryGetValue(key, out var v) && v is bool b ? b : fallback;

        public static List<object> List(this Dictionary<string, object> o, string key) =>
            o.TryGetValue(key, out var v) && v is List<object> l ? l : new List<object>();
    }
}
