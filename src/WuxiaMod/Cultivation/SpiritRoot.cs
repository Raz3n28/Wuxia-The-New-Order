using System;
using System.Collections.Generic;
using System.IO;
using WuxiaMod.Json;

namespace WuxiaMod.Cultivation
{
    /// <summary>Aptidão para cultivar (raiz espiritual), lida de spirit_roots.json.</summary>
    public sealed class SpiritRoot
    {
        public string Id;
        public float Weight;         // peso no sorteio
        public bool CanCultivate;    // "Sem Raiz" não cultiva
        public float QiGainMultiplier;
        public string Element;       // fire, water, metal, wood, earth, thunder, void ou null

        public string NameKey => "root." + Id + ".name";
        public string DescKey => "root." + Id + ".desc";
    }

    public static class SpiritRootDatabase
    {
        private static readonly List<SpiritRoot> Roots = new List<SpiritRoot>();
        public static IReadOnlyList<SpiritRoot> All => Roots;

        public static void Load(string dataDir)
        {
            Roots.Clear();
            var path = Path.Combine(dataDir, "spirit_roots.json");
            var root = (Dictionary<string, object>)MiniJson.Parse(File.ReadAllText(path));
            foreach (var item in root.List("roots"))
            {
                var o = (Dictionary<string, object>)item;
                Roots.Add(new SpiritRoot
                {
                    Id = o.Str("id"),
                    Weight = o.Float("weight", 1f),
                    CanCultivate = o.Bool("canCultivate", true),
                    QiGainMultiplier = o.Float("qiGainMultiplier", 1f),
                    Element = o.Str("element"),
                });
            }
            ModLog.Info($"{Roots.Count} raízes espirituais carregadas.");
        }

        public static SpiritRoot ById(string id) => Roots.Find(r => r.Id == id);

        /// <summary>Sorteia uma raiz pelos pesos. Use um Random com semente para resultados reproduzíveis.</summary>
        public static SpiritRoot Roll(Random rng)
        {
            float total = 0f;
            foreach (var r in Roots) total += r.Weight;
            float pick = (float)(rng.NextDouble() * total);
            foreach (var r in Roots)
            {
                pick -= r.Weight;
                if (pick < 0f) return r;
            }
            return Roots.Count > 0 ? Roots[Roots.Count - 1] : null;
        }
    }
}
