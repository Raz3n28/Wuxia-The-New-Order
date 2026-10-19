using System;
using WuxiaMod;
using WuxiaMod.Game;

// Entrada exigida pelo carregador de mods do jogo: namespace global, nome
// fixo ModPluginLauncher, herda BaseModLauncher e sobrescreve launcher().
// O campo "config" (ModXmlManifest) vem preenchido pelo jogo.
public class ModPluginLauncher : BaseModLauncher
{
    public override void launcher()
    {
        string versionDir = null;
        try
        {
            // Mesmo formato usado pelo jogo: getRootPath("Manifest", config.icon).
            versionDir = System.IO.Path.GetDirectoryName(config.getRootPath("v_common", "config.json"));
        }
        catch (Exception ex)
        {
            ModLog.Warn("getRootPath falhou, procurando a pasta pela DLL: " + ex.Message);
        }
        ModEntry.Init(versionDir);
    }
}
