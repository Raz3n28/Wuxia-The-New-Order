using System;
using System.Collections.Generic;
using HarmonyLib;
using WuxiaMod.Cultivation;
using WuxiaMod.Localization;

namespace WuxiaMod.Game
{
    /// <summary>
    /// Modo de teste: ligado com "testMode": true no config.json (desligado por padrão).
    /// - Botão "Kit de teste" na aba Cultivo: põe no chão, ao lado do personagem, todas as
    ///   pílulas e ingredientes do mod, desperta o cultivo e enche o Qi até o auge do
    ///   Refinamento de Qi (pronto para a ruptura com o Elixir da Fundação).
    ///   O kit também traz os materiais para construir uma de cada construção do mod.
    /// - As bancadas e receitas do mod ficam com trabalho 1 (quase instantâneas).
    /// - As construções do mod ficam prontas assim que são colocadas (veja InstantBuildPatch).
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
            foreach (var kv in BuildingMaterials()) given += Put(hero, kv.Key.Item1, kv.Key.Item2, kv.Value);

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

        private static int Put(BaseHero hero, int type, int shortId, int count) =>
            Put(hero, type, ModEntry.HarmonyId + "_" + shortId, count);

        private static int Put(BaseHero hero, int type, string id, int count)
        {
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

        /// <summary>
        /// Soma os materiais (campo Materials, "tipo_ID_qtd|...") de todas as construções do mod:
        /// caldeirões, bancada e almofada de meditação.
        /// </summary>
        private static Dictionary<(int, string), int> BuildingMaterials()
        {
            var total = new Dictionary<(int, string), int>();
            AddMaterials<F_ManufactureDef>(total);
            AddMaterials<F_Entertainment>(total);
            return total;
        }

        private static void AddMaterials<T>(Dictionary<(int, string), int> total) where T : BaseDef
        {
            var beans = ImplDef<T>.beans;
            if (beans == null) return;
            var field = AccessTools.Field(typeof(T), "Materials");
            if (field == null) return;
            foreach (var bean in beans)
            {
                if (bean?.mod?.packageId != ModEntry.HarmonyId) continue;
                if (!(field.GetValue(bean) is string materials)) continue;
                foreach (var part in materials.Split('|'))
                {
                    var bits = part.Split('_');
                    if (bits.Length < 3 || !int.TryParse(bits[0], out int type) || !int.TryParse(bits[bits.Length - 1], out int count)) continue;
                    string id = string.Join("_", bits, 1, bits.Length - 2);
                    var key = (type, id);
                    total.TryGetValue(key, out int sum);
                    total[key] = sum + count;
                }
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
