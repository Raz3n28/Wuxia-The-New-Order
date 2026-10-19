using System.Collections.Generic;
using System.IO;
using WuxiaMod.Json;

namespace WuxiaMod.Cultivation
{
    /// <summary>Um estágio de cultivo (Refinamento de Qi, Núcleo Dourado...), lido de realms.json.</summary>
    public sealed class Realm
    {
        public string Id;
        public int Order;            // 1..12
        public string Tier;          // initial, intermediate, advanced, immortal
        public int SubLevels;        // quantos níveis dentro do estágio
        public float QiPerLevel;     // Qi para subir um nível dentro do estágio
        public float StatMultiplier; // bônus de atributos no nível 1 do estágio
        public int LifespanYears;    // expectativa de vida extra ao chegar no estágio
        public int PointsPerLevel;   // pontos de cultivo por nível (árvore e passivas)
        public string BreakthroughItem; // item exigido para romper para este estágio (null = nenhum)
        public float BreakthroughChance; // chance base de sucesso da ruptura (0..1)
        public bool Tribulation;     // ruptura dispara a Tribulação Celestial

        public string NameKey => "realm." + Id + ".name";
        public string DescKey => "realm." + Id + ".desc";
    }

    public static class RealmDatabase
    {
        private static readonly List<Realm> Realms = new List<Realm>();
        public static IReadOnlyList<Realm> All => Realms;

        public static void Load(string dataDir)
        {
            Realms.Clear();
            var path = Path.Combine(dataDir, "realms.json");
            var root = (Dictionary<string, object>)MiniJson.Parse(File.ReadAllText(path));
            foreach (var item in root.List("realms"))
            {
                var o = (Dictionary<string, object>)item;
                Realms.Add(new Realm
                {
                    Id = o.Str("id"),
                    Order = o.Int("order"),
                    Tier = o.Str("tier"),
                    SubLevels = o.Int("subLevels", 1),
                    QiPerLevel = o.Float("qiPerLevel", 100f),
                    StatMultiplier = o.Float("statMultiplier", 1f),
                    LifespanYears = o.Int("lifespanYears"),
                    PointsPerLevel = o.Int("pointsPerLevel", 1),
                    BreakthroughItem = o.Str("breakthroughItem"),
                    BreakthroughChance = o.Float("breakthroughChance", 1f),
                    Tribulation = o.Bool("tribulation"),
                });
            }
            Realms.Sort((a, b) => a.Order.CompareTo(b.Order));
            ModLog.Info($"{Realms.Count} estágios de cultivo carregados.");
        }

        /// <summary>Estágio pela ordem (1 = Refinamento de Qi). Null se não existir.</summary>
        public static Realm ByOrder(int order) =>
            order >= 1 && order <= Realms.Count ? Realms[order - 1] : null;
    }
}
