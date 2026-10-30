using System;

namespace WuxiaMod.Cultivation
{
    public enum BreakthroughResult
    {
        NotReady,        // falta Qi ou progresso
        MissingItem,     // falta o item de ruptura
        Success,
        Failed,          // Desvio de Qi
        AtPeak,          // já está no último estágio
    }

    /// <summary>
    /// Progresso de cultivo de um personagem, no modelo do RimImmortal: o Qi enche a
    /// reserva do estágio (MaxQi) e o que sobra vira progresso (Exp). Com os dois cheios
    /// dá para tentar romper. Não depende do jogo: o código que liga isso a um personagem
    /// real fica em Game/.
    /// </summary>
    [Serializable]
    public sealed class CultivationState
    {
        public string RootId;
        public int RealmOrder;   // 0 = mortal (ainda não cultiva)
        public float Qi;         // reserva de Qi (até Realm.MaxQi)
        public float Progress;   // progresso no estágio (até Realm.Exp)
        public int UnspentPoints;

        // Prazos em tempo de jogo (ms, UserBean.getTime()).
        public float QiBoost;          // +fração no ganho de Qi (0.5 = +50%)
        public long QiBoostUntil;
        public float BreakBonus;       // +chance de ruptura (0.1 = +10%)
        public long BreakBonusUntil;
        public long DeviationUntil;    // Desvio de Qi: não cultiva até aqui

        public SpiritRoot Root => SpiritRootDatabase.ById(RootId);
        public Realm Realm => RealmDatabase.ByOrder(RealmOrder);
        public Realm NextRealm => RealmDatabase.ByOrder(RealmOrder + 1);
        public bool CanCultivate => Root != null && Root.CanCultivate;

        public float ActiveQiBoost(long now) => now < QiBoostUntil ? QiBoost : 0f;
        public float ActiveBreakBonus(long now) => now < BreakBonusUntil ? BreakBonus : 0f;
        public bool InDeviation(long now) => now < DeviationUntil;

        /// <summary>Bônus de pílula: fica o maior valor e o prazo mais longo.</summary>
        public void AddQiBoost(float value, long now, long durationMs)
        {
            QiBoost = Math.Max(ActiveQiBoost(now), value);
            QiBoostUntil = Math.Max(QiBoostUntil, now + durationMs);
        }

        public void AddBreakBonus(float value, long now, long durationMs)
        {
            BreakBonus = Math.Max(ActiveBreakBonus(now), value);
            BreakBonusUntil = Math.Max(BreakBonusUntil, now + durationMs);
        }

        /// <summary>Começa a cultivar: mortal com aptidão entra no Broto Espiritual.</summary>
        public bool Awaken()
        {
            if (!CanCultivate || RealmOrder > 0) return false;
            RealmOrder = 1;
            Qi = 0f;
            Progress = 0f;
            UnspentPoints += Realm?.Points ?? 0;
            return true;
        }

        /// <summary>
        /// Adiciona Qi (meditação, pílulas), já multiplicado pela raiz. Enche a reserva
        /// primeiro; o que passa dela vira progresso. Retorna true se ficou pronto para romper agora.
        /// </summary>
        public bool AddQi(float amount)
        {
            var realm = Realm;
            if (realm == null || amount <= 0f) return false;
            bool wasReady = ReadyForBreakthrough;
            float gain = amount * (Root?.QiGainMultiplier ?? 1f);
            float toPool = Math.Min(gain, Math.Max(0f, realm.MaxQi - Qi));
            Qi += toPool;
            Progress = Math.Min(realm.Exp, Progress + gain - toPool);
            return !wasReady && ReadyForBreakthrough;
        }

        public bool AtPeakRealm => Realm != null && NextRealm == null;

        /// <summary>Reserva e progresso cheios, e existe um estágio acima.</summary>
        public bool ReadyForBreakthrough
        {
            get
            {
                var realm = Realm;
                return realm != null && NextRealm != null && Qi >= realm.MaxQi && Progress >= realm.Exp;
            }
        }

        /// <summary>Chance de ruptura com os bônus (0..1).</summary>
        public float BreakthroughChance(float bonusChance)
        {
            var realm = Realm;
            return realm == null ? 0f : Math.Max(0f, Math.Min(1f, realm.BreakthroughChance + bonusChance));
        }

        /// <param name="hasItem">Se o personagem tem o item de ruptura deste estágio.</param>
        /// <param name="bonusChance">Bônus de pílulas, construções, itens (somado à chance base).</param>
        /// <param name="now">Hora do jogo (ms), para o prazo do Desvio de Qi.</param>
        /// <param name="deviationMs">Quanto dura o Desvio de Qi se falhar.</param>
        public BreakthroughResult TryBreakthrough(bool hasItem, float bonusChance, Random rng, long now, long deviationMs)
        {
            var realm = Realm;
            var next = NextRealm;
            if (realm == null || next == null) return BreakthroughResult.AtPeak;
            if (!ReadyForBreakthrough) return BreakthroughResult.NotReady;
            if (realm.BreakthroughItem != null && !hasItem) return BreakthroughResult.MissingItem;

            if (rng.NextDouble() < BreakthroughChance(bonusChance))
            {
                RealmOrder = next.Order;
                Progress = 0f; // o Qi fica: a reserva nova é maior
                UnspentPoints += next.Points;
                return BreakthroughResult.Success;
            }
            // Desvio de Qi (como no RimImmortal): perde a reserva e metade do progresso.
            Qi = 0f;
            Progress *= 0.5f;
            DeviationUntil = now + deviationMs;
            return BreakthroughResult.Failed;
        }
    }
}
