using System;
using System.IO;
using System.Text;

namespace WuxiaMod
{
    /// <summary>
    /// Log do mod. Tudo vai para o arquivo próprio do mod
    /// (...\LocalLow\CatStudio\Outer Space\Wuxia The New Dawn\Logs\wuxia.log);
    /// avisos e erros também vão para o Player.log do jogo, com a tag [Wuxia].
    /// </summary>
    public static class ModLog
    {
        /// <summary>Saída do jogo (Player.log). O ModEntry troca pelo Debug.Log do Unity.</summary>
        public static Action<string> Sink = Console.WriteLine;

        private static readonly object Lock = new object();
        private static StreamWriter _file;

        public static string LogFile { get; private set; }

        /// <summary>Abre Logs\wuxia.log dentro da pasta do mod; o log anterior vira wuxia-prev.log.</summary>
        public static void OpenFile(string logsDir)
        {
            lock (Lock)
            {
                try
                {
                    Directory.CreateDirectory(logsDir);
                    LogFile = Path.Combine(logsDir, "wuxia.log");
                    var prev = Path.Combine(logsDir, "wuxia-prev.log");
                    if (File.Exists(LogFile))
                    {
                        if (File.Exists(prev)) File.Delete(prev);
                        File.Move(LogFile, prev);
                    }
                    _file = new StreamWriter(LogFile, false, new UTF8Encoding(false)) { AutoFlush = true };
                }
                catch (Exception ex)
                {
                    _file = null;
                    Sink("[Wuxia][ERRO] Não foi possível criar o log em " + logsDir + ": " + ex.Message);
                }
            }
        }

        public static void Info(string msg) => Write("INFO", msg, false);
        public static void Warn(string msg) => Write("AVISO", msg, true);
        public static void Error(string msg) => Write("ERRO", msg, true);
        public static void Error(Exception ex, string msg) => Write("ERRO", msg + "\n" + ex, true);

        private static void Write(string level, string msg, bool alsoGameLog)
        {
            lock (Lock)
            {
                if (_file != null)
                {
                    try { _file.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {msg}"); }
                    catch { _file = null; }
                }
                // Sem arquivo (ainda não aberto ou falhou), tudo vai para o Player.log.
                if (alsoGameLog || _file == null) Sink($"[Wuxia][{level}] {msg}");
            }
        }
    }
}
