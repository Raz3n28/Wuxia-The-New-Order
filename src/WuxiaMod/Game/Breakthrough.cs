using System;
using WuxiaMod.Cultivation;
using WuxiaMod.Localization;

namespace WuxiaMod.Game
{
    /// <summary>
    /// Tentativa de ruptura, de onde vier: pílula/item de ruptura, Plataforma de Ruptura
    /// (MeditationPatch) ou o botão da aba Cultivo. Mostra a mensagem acima do colono,
    /// grava no log e atualiza os bônus de estágio e o Desvio de Qi (RealmBuffs).
    /// </summary>
    internal static class Breakthrough
    {
        /// <summary>Desvio de Qi: 3 dias, como o 经脉错乱 do RimImmortal.</summary>
        public const float DeviationHours = 72f;

        /// <summary>Bônus de romper sentado na Plataforma de Ruptura.</summary>
        public const float PlatformBonus = 0.05f;

        private static readonly Random Rng = new Random();

        public static BreakthroughResult Attempt(BaseHero target, CultivationState state, bool hasItem, float extraBonus, string source)
        {
            var hero = target.heroBean;
            long now = UserBean.getInstance().getTime();
            var from = state.Realm;
            float bonus = state.ActiveBreakBonus(now) + extraBonus;
            float chance = state.BreakthroughChance(bonus);
            var result = state.TryBreakthrough(hasItem, bonus, Rng, now, WorldTimeTools.gameHour_to_mm(DeviationHours));

            string message;
            switch (result)
            {
                case BreakthroughResult.Success:
                    message = Loc.T("ui.breakthrough.success", hero.name, Loc.T(state.Realm.NameKey));
                    break;
                case BreakthroughResult.Failed:
                    message = Loc.T("ui.breakthrough.failed", hero.name, DeviationHours.ToString("0"));
                    break;
                case BreakthroughResult.MissingItem:
                    message = Loc.T("ui.breakthrough.missing_item", ItemName(from.BreakthroughItem));
                    break;
                case BreakthroughResult.AtPeak:
                    message = Loc.T("ui.breakthrough.at_peak");
                    break;
                default:
                    message = Loc.T("ui.breakthrough.not_ready");
                    break;
            }
            ModLog.Info($"{hero.name}: ruptura ({source}) de {from?.Id} com chance {chance:P0} = {result}.");
            GUI_RoleInfo.showRoleInfo(target, message);
            if (result == BreakthroughResult.Success || result == BreakthroughResult.Failed)
                RealmBuffs.Sync(target, state);
            return result;
        }

        /// <summary>Nome do item de ruptura (ID curto de um remédio do mod) no idioma do jogo.</summary>
        public static string ItemName(string shortId)
        {
            if (string.IsNullOrEmpty(shortId)) return "";
            try { return ImplDef<T_MedicineDef>.getDataID(shortId)?.getName() ?? shortId; }
            catch { return shortId; }
        }
    }
}
