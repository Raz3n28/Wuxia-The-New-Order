using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace WuxiaMod.Game
{
    /// <summary>
    /// As imagens das roupas e armas no boneco (centenas de PNGs pequenos, gerados por
    /// tools/gen_spine.py) vêm num arquivo só, Resources/Textures/spine_wuxia.pack. Ao iniciar
    /// o mod separamos em Resources/Textures/spine_wuxia/, que é onde o role-customize.xml
    /// aponta. Roda no launcher, antes do jogo montar as skins (SpineCombine.characterCombine).
    /// Só reescreve quando o pacote muda (hash em spine_wuxia/.version).
    /// </summary>
    internal static class SpinePack
    {
        public static void Unpack(string versionDir)
        {
            try
            {
                var tex = Path.Combine(Path.Combine(versionDir, "Resources"), "Textures");
                var pack = Path.Combine(tex, "spine_wuxia.pack");
                if (!File.Exists(pack)) return;
                var bytes = File.ReadAllBytes(pack);
                string hash;
                using (var sha = SHA1.Create()) hash = BitConverter.ToString(sha.ComputeHash(bytes));
                var dir = Path.Combine(tex, "spine_wuxia");
                var marker = Path.Combine(dir, ".version");
                if (File.Exists(marker) && File.ReadAllText(marker) == hash) return;

                Directory.CreateDirectory(dir);
                using (var r = new BinaryReader(new MemoryStream(bytes)))
                {
                    if (Encoding.ASCII.GetString(r.ReadBytes(5)) != "WXPK1") throw new InvalidDataException("spine_wuxia.pack inválido");
                    int count = r.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        var name = Encoding.UTF8.GetString(r.ReadBytes(r.ReadInt32()));
                        var data = r.ReadBytes(r.ReadInt32());
                        File.WriteAllBytes(Path.Combine(dir, Path.GetFileName(name)), data);
                    }
                    ModLog.Info($"Imagens das roupas e armas: {count} arquivos em {dir}");
                }
                File.WriteAllText(marker, hash);
            }
            catch (Exception ex) { ModLog.Error(ex, "separar spine_wuxia.pack"); }
        }
    }
}
