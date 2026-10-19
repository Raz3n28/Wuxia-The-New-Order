using System;
using System.Collections.Generic;
using WuxiaMod.Cultivation;
using WuxiaMod.Localization;

namespace WuxiaMod.Game
{
    /// <summary>
    /// O que cada pílula faz além da cura normal do jogo.
    ///
    /// As pílulas são remédios (T_MedicineDef, tabela 29). O jogo já consome o item,
    /// cura (Recover_HP/Immunity) e põe o bônus de remédio. Aqui somamos:
    /// - Qi na hora e bônus de cultivo (ganho de Qi, chance de ruptura), guardados no CultivationState;
    /// - bônus de atributo (velocidade, dano, temperatura...) com o mesmo mecanismo das comidas:
    ///   um bônus tipo 9 que aponta para uma "comida" escondida (T_FoodDef) com AdditionEffect.
    ///   Essas comidas só existem no dicionário do jogo (beansDict), não em listas nem no comércio.
    /// Os números de AdditionEffect são os da tabela EffectEntries (0.5 = +50%).
    /// </summary>
    internal static class PillEffects
    {
        private sealed class Pill
        {
            public string Key;
            public float Qi;            // Qi na hora
            public float QiBoost;       // +ganho de Qi
            public float BreakBonus;    // +chance de ruptura
            public float Hours;         // duração dos bônus
            public string Effects;      // AdditionEffect da comida escondida ("id_valor|...")
            public float Hunger;        // saciedade na hora
            public float Energy;        // energia na hora
            public bool Foundation;     // tenta a ruptura para o Estabelecimento da Fundação
        }

        // Chave = ID curto da pílula no def.xml.
        private static readonly Dictionary<string, Pill> Pills = new Dictionary<string, Pill>
        {
            ["96001"] = new Pill { Key = "guiling", Qi = 50 },
            ["96002"] = new Pill { Key = "baoxue", Effects = "70_0.5", Hours = 24 },
            ["96003"] = new Pill { Key = "lingyun", Effects = "3030_0.5", Hours = 3 },
            ["96004"] = new Pill { Key = "sanshi", Effects = "2010_0.3|2020_0.2", Hours = 4 },
            ["96005"] = new Pill { Key = "guyuan", QiBoost = 0.25f, BreakBonus = 0.10f, Hours = 8 },
            ["96006"] = new Pill { Key = "bailiang", Hunger = 100, Effects = "20_-1", Hours = 72 },
            ["96007"] = new Pill { Key = "qingfeng", Energy = 50, Effects = "30_-1", Hours = 18 },
            ["96008"] = new Pill { Key = "dieda" },
            ["96009"] = new Pill { Key = "xuanhan", Effects = "191_25", Hours = 14 },
            ["96010"] = new Pill { Key = "zhifeng", Effects = "190_-25", Hours = 14 },
            ["96011"] = new Pill { Key = "lingyu", Qi = 150, QiBoost = 0.5f, Hours = 6 },
            ["96012"] = new Pill { Key = "jinxue", Effects = "3010_-0.5|3030_-0.5", Hours = 6 },
            ["96013"] = new Pill { Key = "huichun", Effects = "70_0.5", Hours = 48 },
            ["96014"] = new Pill { Key = "shengji", Effects = "70_2", Hours = 12 },
            ["96015"] = new Pill { Key = "xiuluo", Effects = "2010_1|2020_0.5|0_-0.5", Hours = 24 },
            ["96016"] = new Pill { Key = "tianling", QiBoost = 1.5f, Hours = 240 },
            ["96017"] = new Pill { Key = "tongxuan", Foundation = true, QiBoost = 0.25f, Hours = 24 },
        };

        /// <summary>ID da comida escondida de cada pílula (fica no save dentro do bônus do personagem).</summary>
        private static string BuffFoodId(Pill p) => ModEntry.HarmonyId + "_buff_" + p.Key;

        /// <summary>Template das comidas escondidas: 浓缩果蔬汁, a comida do jogo que já tem bônus.</summary>
        private const string FoodTemplateId = "25";

        private static readonly Dictionary<string, T_FoodDef> BuffFoods = new Dictionary<string, T_FoodDef>();

        /// <summary>Cria (uma vez) e registra as comidas escondidas. Retorna quantas entraram no dicionário agora.</summary>
        public static int RegisterBuffFoods()
        {
            var dict = ImplDef<T_FoodDef>.beansDict;
            if (dict == null) return 0;
            int added = 0;
            foreach (var pair in Pills)
            {
                var p = pair.Value;
                if (string.IsNullOrEmpty(p.Effects)) continue;
                string id = BuffFoodId(p);
                if (!BuffFoods.TryGetValue(id, out var food))
                {
                    food = ImplDef<T_FoodDef>.getTemplateDef(FoodTemplateId);
                    if (food == null) return added;
                    food.ID = id;
                    var pill = ImplDef<T_MedicineDef>.getDataID(pair.Key);
                    food.Name = pill?.getName() ?? id;
                    food.AdditionEffect = p.Effects;
                    food.AdditionTime = (int)Math.Ceiling(p.Hours);
                    food.additionEffs = AdditionEffectData.parseList(p.Effects);
                    BuffFoods[id] = food;
                }
                if (dict.TryGetValue(id, out var existing) && ReferenceEquals(existing, food)) continue;
                dict[id] = food;
                added++;
            }
            // Bônus que vieram do save antes de as comidas existirem: recalcula.
            if (added > 0) RefreshFoodBuffs();
            return added;
        }

        private static void RefreshFoodBuffs()
        {
            var heroes = UserBean.getInstance()?.gameData?.heros;
            if (heroes == null) return;
            for (int i = 0; i < heroes.Count; i++)
            {
                try { heroes[i]?.updateEffectris_FoodBuf(); }
                catch (Exception ex) { ModLog.Error(ex, "recalcular bônus de pílula"); }
            }
        }

        /// <summary>Chamado depois que o jogo aplicou o remédio (veja PillUsePatch).</summary>
        public static void OnUsed(BaseHero target, T_MedicineDef def)
        {
            if (target?.heroBean == null || def?.mod == null || def.mod.packageId != ModEntry.HarmonyId) return;
            string shortId = def.ID.StartsWith(ModEntry.HarmonyId + "_", StringComparison.Ordinal)
                ? def.ID.Substring(ModEntry.HarmonyId.Length + 1)
                : def.ID;
            if (!Pills.TryGetValue(shortId, out var p)) return;

            var hero = target.heroBean;
            long now = UserBean.getInstance().getTime();
            long duration = WorldTimeTools.gameHour_to_mm(p.Hours);
            var messages = new List<string>();

            if (p.Hunger > 0) target.attrDriven?.addHunger(p.Hunger);
            if (p.Energy > 0) target.attrDriven?.addEnergy(p.Energy);

            if (!string.IsNullOrEmpty(p.Effects))
            {
                RegisterBuffFoods();
                var buff = new FWRoleBuff(target.getUID());
                buff.setAddBuff(9, 0, (int)Math.Ceiling(p.Hours));
                buff.setFromTypeID(13, BuffFoodId(p));
                LocalCoreHandle.Hero_BuffAdd_LN(target, buff);
            }

            bool cultivationPill = p.Qi > 0 || p.QiBoost > 0 || p.BreakBonus > 0 || p.Foundation;
            if (cultivationPill)
            {
                var state = hero.isAndroid() ? null : CultivationRegistry.Get(hero.uid, true);
                if (state?.Realm == null)
                {
                    // Mortal ou androide: a pílula não faz efeito de cultivo.
                    messages.Add(Loc.T("pill.no_cultivation"));
                }
                else if (p.Foundation && state.RealmOrder == 1 && state.ReadyForBreakthrough)
                {
                    var from = state.Realm;
                    float bonus = state.ActiveBreakBonus(now);
                    var result = state.TryBreakthrough(true, bonus, new Random());
                    if (result == BreakthroughResult.Success)
                    {
                        messages.Add(Loc.T("ui.breakthrough.success", hero.name, Loc.T(state.Realm.NameKey)));
                        ModLog.Info($"{hero.name} rompeu de {from.Id} para {state.Realm.Id} com a pílula (bônus {bonus:P0}).");
                    }
                    else
                    {
                        messages.Add(Loc.T("ui.breakthrough.failed", hero.name));
                        ModLog.Info($"{hero.name} falhou a ruptura com a pílula ({result}).");
                    }
                }
                else
                {
                    if (p.Qi > 0)
                    {
                        int levels = state.AddQi(p.Qi);
                        messages.Add(Loc.T("pill.qi", p.Qi.ToString("0")));
                        if (levels > 0) ModLog.Info($"{hero.name} subiu para o nível {state.SubLevel} com uma pílula.");
                    }
                    if (p.QiBoost > 0)
                    {
                        state.AddQiBoost(p.QiBoost, now, duration);
                        messages.Add(Loc.T("pill.qi_boost", (p.QiBoost * 100).ToString("0"), p.Hours.ToString("0")));
                    }
                    if (p.BreakBonus > 0)
                    {
                        state.AddBreakBonus(p.BreakBonus, now, duration);
                        messages.Add(Loc.T("pill.break_bonus", (p.BreakBonus * 100).ToString("0"), p.Hours.ToString("0")));
                    }
                    if (p.Foundation && state.RealmOrder == 1)
                        messages.Add(Loc.T("pill.foundation_not_ready"));
                }
            }

            if (messages.Count > 0)
                GUI_RoleInfo.showRoleInfo(target, string.Join("\n", messages));
        }
    }
}
