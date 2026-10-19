using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using WuxiaMod.Cultivation;
using WuxiaMod.Json;
using WuxiaMod.Localization;

namespace WuxiaMod.Game
{
    /// <summary>Carrega dados e idiomas e aplica os patches Harmony. Chamado pelo ModPluginLauncher.</summary>
    public static class ModEntry
    {
        public const string HarmonyId = "pauloraz3n.wuxia";

        /// <summary>Pasta do mod ao lado dos dados do jogo (LocalLow\CatStudio\Outer Space).</summary>
        public const string UserFolderName = "Wuxia The New Dawn";

        /// <summary>Pasta onde o mod grava tudo o que gera: Logs\ e, no futuro, dados próprios.</summary>
        public static string UserDir { get; private set; }

        /// <summary>Pasta v_common do mod (tem Data/, Languages/, config.json).</summary>
        public static string VersionDir { get; private set; }
        public static bool Initialized { get; private set; }
        public static bool VerboseLog { get; private set; }

        public static void Init(string versionDir)
        {
            if (Initialized) return;
            ModLog.Sink = UnityEngine.Debug.Log;
            UserDir = Path.Combine(UnityEngine.Application.persistentDataPath, UserFolderName);
            ModLog.OpenFile(Path.Combine(UserDir, "Logs"));
            ModLog.Sink("[Wuxia] Log do mod em " + ModLog.LogFile);
            try
            {
                VersionDir = !string.IsNullOrEmpty(versionDir) && Directory.Exists(Path.Combine(versionDir, "Data"))
                    ? versionDir
                    : FindVersionDir();
                ModLog.Info("Iniciando a partir de " + VersionDir);

                string language = "auto";
                var configPath = Path.Combine(VersionDir, "config.json");
                if (File.Exists(configPath))
                {
                    var cfg = (Dictionary<string, object>)MiniJson.Parse(File.ReadAllText(configPath));
                    language = cfg.Str("language", "auto");
                    VerboseLog = cfg.Bool("verboseLog");
                }

                Loc.LoadAll(Path.Combine(VersionDir, "Languages"));
                Loc.SetLanguage(language == "auto" ? DetectLanguage() : language);
                SpiritRootDatabase.Load(Path.Combine(VersionDir, "Data"));
                RealmDatabase.Load(Path.Combine(VersionDir, "Data"));

                // Harmony fica em outra classe: se o 0Harmony.dll faltar, só os patches falham,
                // não o carregamento do mod inteiro.
                HarmonyPatcher.PatchAll(HarmonyId);

                Initialized = true;
                ModLog.Info($"Pronto. Idioma: {Loc.Current}.");
            }
            catch (Exception ex)
            {
                ModLog.Error(ex, "Falha ao iniciar o mod");
            }
        }

        /// <summary>Plano B: procura a pasta v_common a partir da DLL (v_common/Assemblies/WuxiaMod.dll).</summary>
        private static string FindVersionDir()
        {
            var location = Assembly.GetExecutingAssembly().Location;
            var dir = string.IsNullOrEmpty(location) ? null : Path.GetDirectoryName(location);
            for (int i = 0; i < 3 && dir != null; i++)
            {
                if (Directory.Exists(Path.Combine(dir, "Data"))) return dir;
                dir = Path.GetDirectoryName(dir);
            }
            throw new DirectoryNotFoundException("Pasta v_common do mod não encontrada.");
        }

        /// <summary>
        /// "auto": o jogo só tem chinês e inglês, e a tradução PT-BR pede o jogo
        /// em inglês, então não dá para saber pelo jogo. Usa o idioma do Windows.
        /// O CultureInfo do Mono do Unity costuma vir vazio/inglês, então pergunta
        /// direto ao Windows. Para forçar, ponha "en" ou "pt-BR" no config.json.
        /// </summary>
        private static string DetectLanguage()
        {
            try
            {
                // Idioma primário do LANGID: 0x16 = português.
                if ((GetUserDefaultUILanguage() & 0x3FF) == 0x16) return Loc.Portuguese;
                return Loc.English;
            }
            catch (Exception)
            {
                return System.Globalization.CultureInfo.CurrentUICulture.Name;
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern ushort GetUserDefaultUILanguage();
    }
}
