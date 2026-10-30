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
            CultivationSave.Dir = Path.Combine(UserDir, "Saves");
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
                    TestMode.Enabled = cfg.Bool("testMode");
                    Patches.MeditationPatch.QiPerHourRecreation = cfg.Float("qiPerHourRecreation", Patches.MeditationPatch.QiPerHourRecreation);
                    Patches.MeditationPatch.QiPerHourMeditation = cfg.Float("qiPerHourMeditation", Patches.MeditationPatch.QiPerHourMeditation);
                    Patches.MeditationPatch.QiPerHourPlatform = cfg.Float("qiPerHourPlatform", Patches.MeditationPatch.QiPerHourPlatform);
                    Patches.EquipSpritePatch.WeaponRotation = cfg.Float("weaponSpriteRotation", Patches.EquipSpritePatch.WeaponRotation);
                    Patches.EquipSpritePatch.WeaponScale = cfg.Float("weaponSpriteScale", Patches.EquipSpritePatch.WeaponScale);
                }

                Loc.LoadAll(Path.Combine(VersionDir, "Languages"));
                DefTexts.LoadPortuguese(Path.Combine(VersionDir, "Languages"));
                // O botão de idioma da aba Cultivo grava a escolha em settings.json e ela vale mais que o config.json.
                string chosen = LoadChosenLanguage();
                Loc.SetLanguage(chosen ?? (language == "auto" ? DetectLanguage() : language));
                SpiritRootDatabase.Load(Path.Combine(VersionDir, "Data"));
                RealmDatabase.Load(Path.Combine(VersionDir, "Data"));

                // Harmony fica em outra classe: se o 0Harmony.dll faltar, só os patches falham,
                // não o carregamento do mod inteiro.
                HarmonyPatcher.PatchAll(HarmonyId);

                // Tarefas periódicas: IDs curtos das receitas, textos em inglês, bônus das pílulas.
                WuxiaRuntime.Create();

                Initialized = true;
                ModLog.Info($"Pronto. Idioma: {Loc.Current}." + (TestMode.Enabled ? " MODO DE TESTE LIGADO." : ""));
            }
            catch (Exception ex)
            {
                ModLog.Error(ex, "Falha ao iniciar o mod");
            }
        }

        private static string SettingsPath => Path.Combine(UserDir, "settings.json");

        private static string LoadChosenLanguage()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return null;
                var settings = (Dictionary<string, object>)MiniJson.Parse(File.ReadAllText(SettingsPath));
                return settings.Str("language");
            }
            catch (Exception ex)
            {
                ModLog.Error(ex, "Falha ao ler " + SettingsPath);
                return null;
            }
        }

        /// <summary>Troca o idioma do mod (botão na aba Cultivo) e grava a escolha.</summary>
        public static void SetModLanguage(string code)
        {
            Loc.SetLanguage(code);
            DefTexts.Invalidate();
            DefAliases.Apply();
            try
            {
                Directory.CreateDirectory(UserDir);
                File.WriteAllText(SettingsPath, "{\n  \"language\": \"" + Loc.Current + "\"\n}\n");
            }
            catch (Exception ex)
            {
                ModLog.Error(ex, "Falha ao gravar " + SettingsPath);
            }
            ModLog.Info("Idioma do mod: " + Loc.Current + ".");
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
