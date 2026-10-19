using System;

namespace WuxiaMod
{
    /// <summary>
    /// Log do mod com a tag [Wuxia], para achar fácil no Player.log.
    /// O ModEntry troca o Sink pelo log do Unity quando o jogo carrega.
    /// </summary>
    public static class ModLog
    {
        public static Action<string> Sink = Console.WriteLine;

        public static void Info(string msg) => Sink("[Wuxia] " + msg);
        public static void Warn(string msg) => Sink("[Wuxia][AVISO] " + msg);
        public static void Error(string msg) => Sink("[Wuxia][ERRO] " + msg);
        public static void Error(Exception ex, string msg) => Sink("[Wuxia][ERRO] " + msg + "\n" + ex);
    }
}
