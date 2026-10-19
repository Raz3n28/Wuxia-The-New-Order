using System;
using HarmonyLib;

namespace WuxiaMod.Game.Patches
{
    /// <summary>
    /// Efeitos das nossas pílulas. Usar um remédio (botão direito no item, ou a IA
    /// curando alguém) termina em LocalCoreHandle.Hero_Cure_LN(quem usa, quem recebe,
    /// item, quantidade, ...). O jogo consome o item e aplica a cura; depois disso
    /// PillEffects soma Qi, bônus de cultivo e bônus de atributo.
    /// </summary>
    [HarmonyPatch(typeof(LocalCoreHandle), nameof(LocalCoreHandle.Hero_Cure_LN))]
    internal static class PillUsePatch
    {
        // O item é consumido dentro do método; pega o def antes.
        private static void Prefix(FWThingBean useMedichineThing, out T_MedicineDef __state)
        {
            __state = null;
            try { __state = useMedichineThing?.getDef() as T_MedicineDef; }
            catch (Exception ex) { ModLog.Error(ex, "pílula (antes)"); }
        }

        private static void Postfix(BaseHero actRole, T_MedicineDef __state)
        {
            if (__state == null) return;
            try { PillEffects.OnUsed(actRole, __state); }
            catch (Exception ex) { ModLog.Error(ex, "efeito da pílula"); }
        }
    }
}
