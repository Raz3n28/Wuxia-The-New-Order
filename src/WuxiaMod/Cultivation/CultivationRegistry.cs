using System;
using System.Collections.Generic;

namespace WuxiaMod.Cultivation
{
    /// <summary>
    /// Estado de cultivo de cada personagem, pelo uid do jogo.
    /// Por enquanto a raiz espiritual é sorteada com o uid como semente, então
    /// o mesmo personagem sempre tem a mesma raiz, mesmo sem salvar nada.
    /// Salvar Qi e estágio no save vem na próxima etapa.
    /// </summary>
    public static class CultivationRegistry
    {
        private static readonly Dictionary<long, CultivationState> States = new Dictionary<long, CultivationState>();

        /// <param name="canHaveRoot">false para androides/robôs: nunca cultivam.</param>
        public static CultivationState Get(long uid, bool canHaveRoot)
        {
            if (States.TryGetValue(uid, out var state)) return state;
            state = new CultivationState();
            if (canHaveRoot)
            {
                var root = SpiritRootDatabase.Roll(new Random(SeedFor(uid)));
                state.RootId = root?.Id;
            }
            States[uid] = state;
            return state;
        }

        public static void Clear() => States.Clear();

        private static int SeedFor(long uid) => unchecked((int)(uid ^ (uid >> 32)) ^ 0x5EED);
    }
}
