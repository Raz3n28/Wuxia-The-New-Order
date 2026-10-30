using System.Collections.Generic;
using System.IO;
using WuxiaMod.Json;

namespace WuxiaMod.Cultivation
{
    /// <summary>Um estágio de cultivo (Broto Espiritual, Condensação de Qi...), lido de realms.json.</summary>
    public sealed class Realm
    {
        public string Id;
        public int Order;            // 1..11 (1 = Broto Espiritual)
        public float MaxQi;          // reserva de Qi do estágio
        public float Exp;            // progresso necessário para poder romper
        public float BreakthroughChance; // chance base de SAIR deste estágio (0..1)
        public string BreakthroughItem;  // item exigido para sair deste estágio (ID curto, null = nenhum)
        public int Points;           // pontos de cultivo ao entrar no estágio
        public string Effects;       // efeitos do jogo enquanto está no estágio ("id_valor|...")

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
                    MaxQi = o.Float("maxQi", 100f),
                    Exp = o.Float("exp", 1000f),
                    BreakthroughChance = o.Float("breakthroughChance", 1f),
                    BreakthroughItem = o.Str("breakthroughItem"),
                    Points = o.Int("points", 1),
                    Effects = o.Str("effects") ?? "",
                });
            }
            Realms.Sort((a, b) => a.Order.CompareTo(b.Order));
            ModLog.Info($"{Realms.Count} estágios de cultivo carregados.");
        }

        /// <summary>Estágio pela ordem (1 = Broto Espiritual). Null se não existir.</summary>
        public static Realm ByOrder(int order) =>
            order >= 1 && order <= Realms.Count ? Realms[order - 1] : null;
    }
}
