using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using WuxiaMod.Cultivation;

namespace WuxiaMod.Game.Patches
{
    /// <summary>
    /// Ganho de Qi. Quem já despertou o cultivo ganha Qi enquanto está no lazer
    /// (Hero_Action_Play no estado "jogando"); na nossa almofada de meditação ganha bem mais.
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

        private const sbyte StatePlaying = 2;
        // Uma hora de jogo em unidades de getTime(): o jogo usa gameHour_to_mm(1) = 60000
        // (não 3.600.000 ms). Com 3.600.000 o Qi subia 60 vezes mais devagar.
        private static readonly double MsPerHour = WorldTimeTools.gameHour_to_mm(1f);

        private sealed class LastTick { public long Time; }
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

                var hero = __instance.hero?.heroBean;
                if (hero == null || hero.isAndroid()) return;
                var state = CultivationRegistry.Get(hero.uid, true);
                if (state.Realm == null) return;

                var build = t.Field("useBuild").GetValue<BaseBuild>();
                bool cushion = IsOurs(build?.buildDef);
                float rate = cushion ? QiPerHourMeditation : QiPerHourRecreation;
                rate *= 1f + state.ActiveQiBoost(now); // pílulas (PillEffects)

                int levels = state.AddQi((float)(rate * hours));
                if (levels > 0)
                    ModLog.Info($"{hero.name} subiu para o nível {state.SubLevel} de {state.RealmOrder}º estágio ({(cushion ? "almofada" : "lazer")}).");
            }
            catch (Exception ex)
            {
                ModLog.Error(ex, "ganho de Qi no lazer");
            }
        }

        private static bool IsOurs(BaseDef def) =>
            def?.mod != null && def.mod.packageId == ModEntry.HarmonyId;
    }
}
