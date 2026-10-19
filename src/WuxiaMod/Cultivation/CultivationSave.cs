using System;
using System.IO;
using System.Text;

namespace WuxiaMod.Cultivation
{
    /// <summary>
    /// Guarda o cultivo ao lado de cada save do jogo, sem mexer no arquivo do jogo:
    /// ...\Wuxia The New Dawn\Saves\&lt;nome do save&gt;.json.
    /// Assim o save continua abrindo normalmente se o mod for desativado.
    /// </summary>
    public static class CultivationSave
    {
        public static string Dir { get; set; }

        // Salvar manual e automático podem chegar juntos, cada um na sua thread.
        private static readonly object FileLock = new object();

        public static string PathFor(string archiveName)
        {
            var safe = new StringBuilder(archiveName);
            foreach (var c in System.IO.Path.GetInvalidFileNameChars()) safe.Replace(c, '_');
            return System.IO.Path.Combine(Dir, safe + ".json");
        }

        /// <summary>Carrega o cultivo do save. Save novo ou sem arquivo começa vazio.</summary>
        public static void Load(string archiveName)
        {
            CultivationRegistry.Clear();
            if (string.IsNullOrEmpty(archiveName) || Dir == null) return;
            var path = PathFor(archiveName);
            if (!File.Exists(path))
            {
                ModLog.Info($"Save \"{archiveName}\" sem cultivo gravado: começando do zero.");
                return;
            }
            try
            {
                int n = CultivationRegistry.LoadJson(File.ReadAllText(path, Encoding.UTF8));
                ModLog.Info($"Cultivo carregado do save \"{archiveName}\": {n} personagens.");
            }
            catch (Exception ex)
            {
                ModLog.Error(ex, $"Falha ao ler {path}; o cultivo deste save começa do zero");
            }
        }

        /// <summary>Grava o cultivo. Escreve num .tmp e troca, para não corromper se o jogo fechar no meio.</summary>
        public static void Save(string archiveName)
        {
            if (string.IsNullOrEmpty(archiveName) || Dir == null) return;
            var path = PathFor(archiveName);
            try
            {
                lock (FileLock) Write(path);
                ModLog.Info($"Cultivo gravado no save \"{archiveName}\": {CultivationRegistry.Count} personagens.");
            }
            catch (Exception ex)
            {
                ModLog.Error(ex, $"Falha ao gravar {path}");
            }
        }

        private static void Write(string path)
        {
            Directory.CreateDirectory(Dir);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, CultivationRegistry.ToJson(), new UTF8Encoding(false));
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }

        public static void Delete(string archiveName)
        {
            if (string.IsNullOrEmpty(archiveName) || Dir == null) return;
            try
            {
                var path = PathFor(archiveName);
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex)
            {
                ModLog.Warn($"Não foi possível apagar o cultivo do save \"{archiveName}\": {ex.Message}");
            }
        }
    }
}
