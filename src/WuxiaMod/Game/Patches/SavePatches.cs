using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using WuxiaMod.Cultivation;

namespace WuxiaMod.Game.Patches
{
    /// <summary>
    /// Liga o cultivo aos saves do jogo (lido do Assembly-CSharp):
    /// - UserBean.loadArchive(Archive): caminho comum de carregar save e de jogo novo.
    /// - Archive.saveSnapshotFile(...6 args): grava o arquivo nos salvamentos em thread (manual e automático).
    /// - Archive.saveFile(nome, ...): salvamento direto, sem snapshot.
    /// - ArchiveInfo.delArchive: save apagado no menu.
    /// O nome do save (ArchiveInfo.name) é a chave do nosso arquivo.
    /// </summary>
    internal static class SavePatches
    {
        private static string InfoName(object archive)
        {
            var info = archive == null ? null : Traverse.Create(archive).Field("info").GetValue();
            return info == null ? null : Traverse.Create(info).Field("name").GetValue<string>();
        }

        [HarmonyPatch]
        private static class LoadArchive
        {
            private static MethodBase TargetMethod() =>
                AccessTools.Method(typeof(UserBean), "loadArchive", new[] { typeof(Archive) });

            private static void Prefix(object[] __args)
            {
                try { CultivationSave.Load(InfoName(__args[0])); }
                catch (Exception ex) { ModLog.Error(ex, "carregar cultivo do save"); }
            }
        }

        [HarmonyPatch]
        private static class SaveSnapshotFile
        {
            // ArchiveSaveSnapshot é privada do Archive: acha a sobrecarga pelo número de parâmetros.
            private static MethodBase TargetMethod() =>
                AccessTools.GetDeclaredMethods(typeof(Archive))
                    .First(m => m.Name == "saveSnapshotFile" && m.GetParameters().Length == 6);

            private static void Postfix(object[] __args, bool __result)
            {
                if (!__result) return;
                try
                {
                    var name = Traverse.Create(__args[0]).Field("archiveName").GetValue<string>();
                    CultivationSave.Save(name);
                }
                catch (Exception ex) { ModLog.Error(ex, "gravar cultivo (snapshot)"); }
            }
        }

        [HarmonyPatch]
        private static class SaveFile
        {
            private static MethodBase TargetMethod() =>
                AccessTools.Method(typeof(Archive), "saveFile",
                    new[] { typeof(string), typeof(bool), typeof(bool), typeof(bool) });

            private static void Postfix(Archive __instance, object[] __args, bool __result)
            {
                if (!__result) return;
                try
                {
                    var name = __args[0] as string;
                    CultivationSave.Save(string.IsNullOrEmpty(name) ? InfoName(__instance) : name);
                }
                catch (Exception ex) { ModLog.Error(ex, "gravar cultivo"); }
            }
        }

        [HarmonyPatch(typeof(ArchiveInfo), "delArchive")]
        private static class DeleteArchive
        {
            private static void Postfix(ArchiveInfo __instance)
            {
                try { CultivationSave.Delete(Traverse.Create(__instance).Field("name").GetValue<string>()); }
                catch (Exception ex) { ModLog.Error(ex, "apagar cultivo do save"); }
            }
        }
    }
}
