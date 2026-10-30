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
    ///   pílulas e ingredientes do mod, desperta o cultivo e enche o Qi e o progresso do
    ///   estágio atual (pronto para romper; clicar de novo depois de romper enche o próximo).
    ///   O kit também traz os materiais para construir uma de cada construção do mod
    ///   e uma unidade (qualidade mais baixa) de cada arma e armadura da Fase 3.
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
        private const int FirstPill = 96001, LastPill = 96019;
        private const int FirstIngredient = 96101, LastIngredient = 96122;
        // Fase 3 (veja tools/gen_phase3.py): 4 qualidades por item, ID = base + qualidade-1.
        private const int FirstWeapon = 97000, LastWeapon = 97620;
        private const int FirstCloth = 98000, LastCloth = 98170;

        public static void GiveKit(FWHeroBean heroBean)
        {
            var hero = MapManager.getInstance()?.heroManager?.getHeroByUID(heroBean.uid);
            if (hero == null)
            {
                ModLog.Warn($"Kit de teste: {heroBean.name} não está no mapa.");
                return;
            }

            int given = DropAllItems(hero);

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
                if (realm != null)
                {
                    state.Qi = realm.MaxQi;
                    state.Progress = realm.Exp;
                    state.DeviationUntil = 0;
                    RealmBuffs.Sync(hero, state);
                }
                message = Loc.T("test.kit_given");
            }
            GUI_RoleInfo.showRoleInfo(hero, message);
            ModLog.Info($"Kit de teste para {heroBean.name}: {given} pilhas de itens; estágio {state.RealmOrder} ({state.Realm?.Id}).");
        }

        /// <summary>Põe no chão, ao lado do colono, um pouco de cada item do mod (aba Dev da janela Wuxia).</summary>
        public static int DropAllItems(BaseHero hero)
        {
            DefAliases.Apply();
            int given = 0;
            for (int id = FirstPill; id <= LastPill; id++) given += Put(hero, 29, id, PillCount);
            for (int id = FirstIngredient; id <= LastIngredient; id++) given += Put(hero, 12, id, IngredientCount);
            foreach (var kv in BuildingMaterials()) given += Put(hero, kv.Key.Item1, kv.Key.Item2, kv.Value);
            for (int id = FirstWeapon; id <= LastWeapon; id += 10) given += Put(hero, 18, id, 1);
            for (int id = FirstCloth; id <= LastCloth; id += 10) given += Put(hero, 17, id, 1);
            return given;
        }

        /// <summary>Um item só (lista de itens da aba Dev). Pílulas 5, materiais 30, o resto 1.</summary>
        public static bool DropOne(BaseHero hero, BaseDef def)
        {
            int count = def.Type == 29 ? PillCount : def.Type == 12 ? IngredientCount : 1;
            return Put(hero, def.Type, def.ID, count) > 0;
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
        /// caldeirões, bancada, almofada, plataformas e rocha espiritual.
        /// </summary>
        private static Dictionary<(int, string), int> BuildingMaterials()
        {
            var total = new Dictionary<(int, string), int>();
            AddMaterials<F_ManufactureDef>(total);
            AddMaterials<F_Entertainment>(total);
            AddMaterials<F_DecroDef>(total);
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
