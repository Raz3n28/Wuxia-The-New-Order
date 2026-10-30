using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using WuxiaMod.Cultivation;
using WuxiaMod.Localization;

namespace WuxiaMod.Game.Patches
{
    /// <summary>
    /// Ganho de Qi. Quem já despertou o cultivo ganha Qi enquanto está no lazer
    /// (Hero_Action_Play no estado "jogando"); nas nossas construções ganha bem mais:
    /// Almofada de Meditação (9601), Plataforma de Cultivo (9602) e Plataforma de Ruptura
    /// (9603). Perto de uma Rocha Espiritual (9604, raio de 15 casas) o ganho sobe 50%.
    /// No Desvio de Qi não ganha nada.
    ///
    /// Na Plataforma de Ruptura, quem está pronto para romper tenta a ruptura depois de
    /// meia hora sentado (+5% de chance). Estágios que pedem item rompem só com o item.
    ///
    /// Do Assembly-CSharp: Hero_Action_Play.logic roda a cada passo da ação;
    /// _state 2 = STATE_PLAYING; useBuild.buildDef é o F_Entertainment usado;
    /// UserBean.getInstance().getTime() é o tempo de jogo (para quando o jogo pausa);
    /// uma hora de jogo = WorldTimeTools.gameHour_to_mm(1).
    /// </summary>
    [HarmonyPatch(typeof(Hero_Action_Play), "logic")]
    internal static class MeditationPatch
    {
        public static float QiPerHourRecreation = 3f;
        public static float QiPerHourMeditation = 15f;
        public static float QiPerHourPlatform = 25f;

        public const string CushionId = "9601";
        public const string PlatformId = "9602";
        public const string BreakPlatformId = "9603";
        public const string SpiritRockId = "9604";
        private const int SpiritRockRadius = 15;
        private const float SpiritRockBonus = 0.5f;
        private const double BreakAfterHours = 0.5d;

        private const sbyte StatePlaying = 2;
        // Uma hora de jogo em unidades de getTime(): o jogo usa gameHour_to_mm(1) = 60000
        // (não 3.600.000 ms). Com 3.600.000 o Qi subia 60 vezes mais devagar.
        private static readonly double MsPerHour = WorldTimeTools.gameHour_to_mm(1f);

        private sealed class LastTick { public long Time; public double ReadyHours; public bool Told; }
        private static readonly ConditionalWeakTable<Hero_Action_Play, LastTick> Ticks = new ConditionalWeakTable<Hero_Action_Play, LastTick>();

        private static void Postfix(Hero_Action_Play __instance)
        {
            try
            {
                var t = Traverse.Create(__instance);
                if (t.Field("_state").GetValue<sbyte>() != StatePlaying) return;

                long now = UserBean.getInstance().getTime();
                var tick = Ticks.GetOrCreateValue(__instance);
                long last = tick.Time;
                tick.Time = now;
                if (last == 0 || now <= last) return;
                // No máximo 1 hora de jogo por passo, caso algo fique muito tempo sem rodar.
                double hours = Math.Min((now - last) / MsPerHour, 1d);

                var role = __instance.hero;
                var hero = role?.heroBean;
                if (hero == null || hero.isAndroid()) return;
                var state = CultivationRegistry.Get(hero.uid, true);
                if (state.Realm == null) return;

                var build = t.Field("useBuild").GetValue<BaseBuild>();
                string ours = OurId(build?.buildDef);
                float rate;
                if (ours == PlatformId) rate = QiPerHourPlatform;
                else if (ours != null) rate = QiPerHourMeditation;
                else rate = QiPerHourRecreation;

                if (ours == BreakPlatformId && state.ReadyForBreakthrough)
                {
                    TryAtPlatform(role, state, tick, hours);
                    return;
                }
                tick.ReadyHours = 0;
                if (state.InDeviation(now)) return;

                float bonus = state.ActiveQiBoost(now); // pílulas (PillEffects)
                if (ours != null && NearSpiritRock(role)) bonus += SpiritRockBonus;
                if (state.AddQi((float)(rate * hours * (1f + bonus))))
                {
                    ModLog.Info($"{hero.name} está pronto para romper o estágio {state.Realm.Id} ({ours ?? "lazer"}).");
                    GUI_RoleInfo.showRoleInfo(role, Loc.T("ui.ready", hero.name));
                }
            }
            catch (Exception ex)
            {
                ModLog.Error(ex, "ganho de Qi no lazer");
            }
        }

        private static void TryAtPlatform(BaseHero role, CultivationState state, LastTick tick, double hours)
        {
            if (state.Realm.BreakthroughItem != null)
            {
                if (!tick.Told)
                {
                    tick.Told = true;
                    GUI_RoleInfo.showRoleInfo(role, Loc.T("ui.breakthrough.missing_item", Breakthrough.ItemName(state.Realm.BreakthroughItem)));
                }
                return;
            }
            tick.ReadyHours += hours;
            if (tick.ReadyHours < BreakAfterHours) return;
            tick.ReadyHours = 0;
            Breakthrough.Attempt(role, state, false, Breakthrough.PlatformBonus, "plataforma");
        }

        /// <summary>ID curto (9601...) se a construção é nossa, senão null.</summary>
        internal static string OurId(BaseDef def)
        {
            if (def?.mod == null || def.mod.packageId != ModEntry.HarmonyId || def.ID == null) return null;
            int i = def.ID.LastIndexOf('_');
            return i >= 0 ? def.ID.Substring(i + 1) : def.ID;
        }

        // A lista de rochas é refeita no máximo a cada 10 s de jogo real.
        private static List<BaseBuild> _rocks = new List<BaseBuild>();
        private static float _rocksAt = -100f;

        private static bool NearSpiritRock(BaseHero role)
        {
            if (UnityEngine.Time.unscaledTime - _rocksAt > 10f)
            {
                _rocksAt = UnityEngine.Time.unscaledTime;
                _rocks = MapManager.getInstance()?.buildManager?.getBuildTypeID(56, ModEntry.HarmonyId + "_" + SpiritRockId) ?? new List<BaseBuild>();
            }
            foreach (var rock in _rocks)
            {
                if (rock == null || !rock.isPutdown()) continue;
                int dx = rock.getCellx() - role.cellX, dy = rock.getCelly() - role.cellY;
                if (dx * dx + dy * dy <= SpiritRockRadius * SpiritRockRadius) return true;
            }
            return false;
        }
    }
}
