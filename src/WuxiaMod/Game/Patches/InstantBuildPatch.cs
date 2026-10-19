using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace WuxiaMod.Game.Patches
{
    /// <summary>
    /// Modo de teste: as construções do mod ficam prontas assim que são colocadas.
    ///
    /// Como o jogo faz (lido do Assembly-CSharp): as plantas colocadas no modo de construção
    /// ficam em LocalCoreHandle.newPlans e são enviadas por Plan_CacheUpdate_LN. Com o
    /// modo GM do jogo (GM_Statics.DEBUG_ENABLE) e gmEnable = true, esse mesmo método
    /// já constrói na hora toda planta de construção (Build_AddBuild), sem material.
    /// Aqui ligamos isso só durante essa chamada, e só quando há uma construção nossa
    /// na lista. Construções do jogo colocadas na mesma leva também ficam prontas.
    /// </summary>
    [HarmonyPatch(typeof(LocalCoreHandle), nameof(LocalCoreHandle.Plan_CacheUpdate_LN))]
    internal static class InstantBuildPatch
    {
        private static FieldInfo _newPlans;

        [HarmonyPrefix]
        private static void Prefix(ref bool gmEnable, out bool __state)
        {
            __state = false;
            if (!TestMode.Enabled || GM_Statics.DEBUG_ENABLE) return;
            try
            {
                if (_newPlans == null) _newPlans = AccessTools.Field(typeof(LocalCoreHandle), "newPlans");
                var plans = _newPlans?.GetValue(null) as List<RolePlan>;
                if (plans == null || !plans.Exists(IsOurs)) return;
                GM_Statics.DEBUG_ENABLE = true;
                gmEnable = true;
                __state = true;
            }
            catch (Exception ex)
            {
                ModLog.Error(ex, "construção na hora (modo de teste)");
            }
        }

        // Finalizer roda mesmo se o método do jogo der erro: o modo GM nunca fica ligado.
        [HarmonyFinalizer]
        private static void Finalizer(bool __state)
        {
            if (__state) GM_Statics.DEBUG_ENABLE = false;
        }

        private static bool IsOurs(RolePlan plan)
        {
            var id = plan?.getBuildId();
            return id != null && id.StartsWith(ModEntry.HarmonyId + "_", StringComparison.Ordinal);
        }
    }
}
