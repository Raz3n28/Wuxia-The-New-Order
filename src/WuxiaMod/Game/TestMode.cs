using System;
using WuxiaMod.Cultivation;
using WuxiaMod.Localization;

namespace WuxiaMod.Game
{
    /// <summary>
    /// Modo de teste: ligado com "testMode": true no config.json (desligado por padrão).
    /// - Botão "Kit de teste" na aba Cultivo: põe no chão, ao lado do personagem, todas as
    ///   pílulas e ingredientes do mod, desperta o cultivo e enche o Qi até o auge do
    ///   Refinamento de Qi (pronto para a ruptura com o Elixir da Fundação).
    /// - As bancadas e receitas do mod ficam com trabalho 1 (quase instantâneas).
    /// </summary>
    internal static class TestMode
    {
        public static bool Enabled;

        private const int PillCount = 5;
        private const int IngredientCount = 30;
        private const int FastWorkload = 1;

        // Faixas de ID do def.xml (veja tools/gen_phase1.py).
        private const int FirstPill = 96001, LastPill = 96017;
        private const int FirstIngredient = 96101, LastIngredient = 96114;

        public static void GiveKit(FWHeroBean heroBean)
        {
            var hero = MapManager.getInstance()?.heroManager?.getHeroByUID(heroBean.uid);
            if (hero == null)
            {
                ModLog.Warn($"Kit de teste: {heroBean.name} não está no mapa.");
                return;
            }

            DefAliases.Apply();
            int given = 0;
            for (int id = FirstPill; id <= LastPill; id++) given += Put(hero, 29, id, PillCount);
            for (int id = FirstIngredient; id <= LastIngredient; id++) given += Put(hero, 12, id, IngredientCount);

            var state = CultivationRegistry.Get(heroBean.uid, true);
            string message;
            if (!state.CanCultivate)
            {
                message = Loc.T("test.no_root");
            }
            else
            {
                state.Awaken();
                var realm = state.Realm;
                if (state.RealmOrder == 1 && realm != null)
                {
                    state.SubLevel = realm.SubLevels;
                    state.Qi = realm.QiPerLevel;
                }
                message = Loc.T("test.kit_given");
            }
            GUI_RoleInfo.showRoleInfo(hero, message);
            ModLog.Info($"Kit de teste para {heroBean.name}: {given} pilhas de itens; estágio {state.RealmOrder}, nível {state.SubLevel}.");
        }

        private static int Put(BaseHero hero, int type, int shortId, int count)
        {
            string id = ModEntry.HarmonyId + "_" + shortId;
            try
            {
                if (DefLauncher.getDefBean(type, id) == null) return 0;
                ThingUntils.putThing(type, id, count, hero.getAlliance(), hero.getAllianceGroupUID(), hero.cellX, hero.cellY);
                return 1;
            }
            catch (Exception ex)
            {
                ModLog.Error(ex, $"kit de teste: item {type}_{id}");
                return 0;
            }
        }

        /// <summary>Chamado pelo DefAliases para cada bancada e receita nossa.</summary>
        public static void ApplyFastWork(BaseDef def)
        {
            if (!Enabled) return;
            if (def is F_ManufactureDef bench) bench.Workload = FastWorkload;
            else if (def is F_ProductListDef recipe) recipe.Workload = FastWorkload;
        }
    }
}
