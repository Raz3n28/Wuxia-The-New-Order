using System;

namespace WuxiaMod.Cultivation
{
    public enum BreakthroughResult
    {
        NotReady,        // ainda falta Qi no nível atual ou não está no último nível
        MissingItem,     // falta o item de ruptura
        Success,
        Failed,          // Desvio de Qi: perde parte do Qi
        AtPeak,          // já está no último estágio
    }

    /// <summary>
    /// Progresso de cultivo de um personagem. Não depende do jogo: o código
    /// que liga isso a um personagem real fica em Game/.
    /// </summary>
    [Serializable]
    public sealed class CultivationState
    {
        public string RootId;
        public int RealmOrder;   // 0 = mortal (ainda não cultiva)
        public int SubLevel;     // 1..Realm.SubLevels
        public float Qi;         // Qi acumulado no nível atual
        public int UnspentPoints;

        public SpiritRoot Root => SpiritRootDatabase.ById(RootId);
        public Realm Realm => RealmDatabase.ByOrder(RealmOrder);
        public bool CanCultivate => Root != null && Root.CanCultivate;

        /// <summary>Multiplicador de atributos do estágio atual (1 para mortais).</summary>
        public float StatMultiplier
        {
            get
            {
                var realm = Realm;
                if (realm == null) return 1f;
                // Cresce dentro do estágio até chegar no multiplicador do próximo.
                var next = RealmDatabase.ByOrder(RealmOrder + 1);
                float target = next?.StatMultiplier ?? realm.StatMultiplier * 1.5f;
                float t = realm.SubLevels <= 1 ? 0f : (SubLevel - 1) / (float)realm.SubLevels;
                return realm.StatMultiplier + (target - realm.StatMultiplier) * t;
            }
        }

        /// <summary>Começa a cultivar: mortal com aptidão entra no Refinamento de Qi nível 1.</summary>
        public bool Awaken()
        {
            if (!CanCultivate || RealmOrder > 0) return false;
            RealmOrder = 1;
            SubLevel = 1;
            Qi = 0f;
            return true;
        }

        /// <summary>
        /// Adiciona Qi (meditação, pílulas). Sobe de nível dentro do estágio
        /// sozinho; parar no último nível exige uma ruptura (TryBreakthrough).
        /// Retorna quantos níveis subiu.
        /// </summary>
        public int AddQi(float amount)
        {
            var realm = Realm;
            if (realm == null || amount <= 0f) return 0;
            Qi += amount * (Root?.QiGainMultiplier ?? 1f);
            int gained = 0;
            while (SubLevel < realm.SubLevels && Qi >= realm.QiPerLevel)
            {
                Qi -= realm.QiPerLevel;
                SubLevel++;
                UnspentPoints += realm.PointsPerLevel;
                gained++;
            }
            // No último nível o Qi acumula até o limite do gargalo.
            if (SubLevel == realm.SubLevels && Qi > realm.QiPerLevel) Qi = realm.QiPerLevel;
            return gained;
        }

        public bool ReadyForBreakthrough
        {
            get
            {
                var realm = Realm;
                return realm != null && SubLevel == realm.SubLevels && Qi >= realm.QiPerLevel;
            }
        }

        /// <param name="hasItem">Se o personagem tem o item de ruptura do próximo estágio.</param>
        /// <param name="bonusChance">Bônus de pílulas, formações, passivas (somado à chance base).</param>
        public BreakthroughResult TryBreakthrough(bool hasItem, float bonusChance, Random rng)
        {
            var next = RealmDatabase.ByOrder(RealmOrder + 1);
            if (next == null) return BreakthroughResult.AtPeak;
            if (!ReadyForBreakthrough) return BreakthroughResult.NotReady;
            if (next.BreakthroughItem != null && !hasItem) return BreakthroughResult.MissingItem;

            float chance = Math.Min(1f, next.BreakthroughChance + bonusChance);
            if (rng.NextDouble() < chance)
            {
                RealmOrder = next.Order;
                SubLevel = 1;
                Qi = 0f;
                UnspentPoints += next.PointsPerLevel;
                return BreakthroughResult.Success;
            }
            Qi *= 0.5f; // Desvio de Qi
            return BreakthroughResult.Failed;
        }
    }
}
