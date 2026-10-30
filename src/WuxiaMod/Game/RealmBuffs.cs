using System;
using System.Collections.Generic;
using WuxiaMod.Cultivation;

namespace WuxiaMod.Game
{
    /// <summary>
    /// Bônus de cada estágio (o LevelBuff do RimImmortal) e o Desvio de Qi, com o mesmo
    /// mecanismo das pílulas: um bônus tipo 9 do colono que aponta para uma "comida"
    /// escondida com AdditionEffect (veja PillEffects). Os efeitos de cada estágio estão
    /// em realms.json. O bônus fica no save do jogo; a cada poucos segundos (WuxiaRuntime)
    /// conferimos que cada colono tem o do seu estágio, e só ele.
    /// </summary>
    internal static class RealmBuffs
    {
        private static string RealmPrefix => ModEntry.HarmonyId + "_realm_";
        private static string DeviationId => ModEntry.HarmonyId + "_deviation";

        /// <summary>Efeitos do Desvio de Qi: eficiência -30%, velocidade -20%, fome x3.</summary>
        private const string DeviationEffects = "0_-0.3|3030_-0.2|20_2";

        // O jogo guarda a duração em int de "ms" de jogo (60000 por hora): cabe ~35000 h.
        private const int RealmHours = 30000;
        private const int RefreshBelowHours = 1000;

        private static string RealmFoodId(int order) => RealmPrefix + order;

        /// <summary>Registra as comidas escondidas dos estágios e do desvio. Retorna quantas entraram agora.</summary>
        public static int Register()
        {
            int added = 0;
            foreach (var realm in RealmDatabase.All)
            {
                if (string.IsNullOrEmpty(realm.Effects)) continue;
                if (PillEffects.RegisterHiddenFood(RealmFoodId(realm.Order), WuxiaMod.Localization.Loc.T(realm.NameKey), realm.Effects, RealmHours)) added++;
            }
            if (PillEffects.RegisterHiddenFood(DeviationId, WuxiaMod.Localization.Loc.T("ui.deviation"), DeviationEffects, (int)Breakthrough.DeviationHours)) added++;
            return added;
        }

        /// <summary>Confere todos os colonos do mapa.</summary>
        public static void SyncAll()
        {
            var heroes = MapManager.getInstance()?.heroManager?.mines;
            if (heroes == null) return;
            for (int i = 0; i < heroes.Count; i++)
            {
                var hero = heroes[i];
                var bean = hero?.heroBean;
                if (bean == null || bean.isAndroid()) continue;
                Sync(hero, CultivationRegistry.Get(bean.uid, true));
            }
        }

        public static void Sync(BaseHero hero, CultivationState state)
        {
            var bean = hero.heroBean;
            long now = UserBean.getInstance().getTime();
            var realm = state.Realm;
            string wanted = realm != null && !string.IsNullOrEmpty(realm.Effects) ? RealmFoodId(realm.Order) : null;
            bool deviation = state.InDeviation(now);

            FWRoleBuff current = null, currentDeviation = null;
            List<FWRoleBuff> remove = null;
            foreach (var buff in bean.buffs)
            {
                if (buff == null || buff.type != 9 || buff.fromType != 13 || buff.fromID == null) continue;
                if (buff.fromID.StartsWith(RealmPrefix, StringComparison.Ordinal))
                {
                    if (buff.fromID == wanted && current == null && !buff.isFinish()) current = buff;
                    else (remove ?? (remove = new List<FWRoleBuff>())).Add(buff);
                }
                else if (buff.fromID == DeviationId)
                {
                    if (deviation && currentDeviation == null && !buff.isFinish()) currentDeviation = buff;
                    else (remove ?? (remove = new List<FWRoleBuff>())).Add(buff);
                }
            }
            if (remove != null)
                foreach (var buff in remove) LocalCoreHandle.Hero_BuffRemove_LN(hero, buff);

            if (wanted != null)
            {
                if (current == null) Add(hero, wanted, RealmHours);
                else if (current.durationMM - (now - current.startMM) < WorldTimeTools.gameHour_to_mm(RefreshBelowHours))
                    current.startMM = now;
            }
            if (deviation && currentDeviation == null)
            {
                int hours = (int)Math.Ceiling((state.DeviationUntil - now) / (double)WorldTimeTools.gameHour_to_mm(1f));
                if (hours > 0) Add(hero, DeviationId, hours);
            }
        }

        private static void Add(BaseHero hero, string foodId, int hours)
        {
            var buff = new FWRoleBuff(hero.getUID());
            buff.setAddBuff(9, 0, hours);
            buff.setFromTypeID(13, foodId);
            LocalCoreHandle.Hero_BuffAdd_LN(hero, buff);
        }
    }
}
