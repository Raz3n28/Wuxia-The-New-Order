using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Script.PublicChannel.Scripts.Steam;
using Steamworks;
using UnityEngine;

namespace WuxiaMod.Game
{
    /// <summary>
    /// Publica o mod na Workshop quando o jogo é aberto com -wuxiapublicar (pelo Publicar.bat).
    ///
    /// Mesmo esquema dos mods de tradução do Paulo: usa o gerenciador da Workshop do próprio
    /// jogo (ChannelSteam.ugc). Na primeira vez cria o item com título, descrição e capa; nas
    /// seguintes envia só os arquivos (título, descrição e capa ficam como estão na Steam).
    /// O andamento aparece num painel no canto da tela e em arquivos na pasta do mod em
    /// LocalLow, que o .bat lê. O ID do item fica em workshop_item_id.txt na mesma pasta.
    /// </summary>
    internal static class WorkshopPublisher
    {
        private const string Flag = "-wuxiapublicar";
        // Só troca a capa (preview.png) de um item que já existe. Usado pelo "Atualizar capa.bat".
        private const string PreviewFlag = "-wuxiacapa";
        private static bool _previewOnly;
        public const string StatusFile = "status_publicacao.txt";
        public const string ResultFile = "resultado_publicacao.txt";
        public const string IdFile = "workshop_item_id.txt";

        private static int _armed = -1;
        private static bool _done, _running, _toldWaiting;
        private static readonly List<string> Lines = new List<string>();
        private static string _state = "Iniciando...";
        private static int _result; // 0 andando, 1 ok, 2 falhou
        private static float _start = -1f, _sendStart = -1f, _lastChange = -1f, _nextStatus;
        private static ulong _bytesDone, _bytesTotal;
        private static string _phase = "", _lastPhase = "";
        private static object _ugc;
        private static FieldInfo _handleField;
        private static bool _searchedHandle;
        private static UGCUpdateHandle_t _directHandle = UGCUpdateHandle_t.Invalid;
        private static CallResult<SubmitItemUpdateResult_t> _submitResult;
        private static ulong _itemId;
        private static GUIStyle _text, _title;
        private static Texture2D _bg, _bar, _barBg;

        private static string ModFolder =>
            string.IsNullOrEmpty(ModEntry.VersionDir) ? null : Directory.GetParent(ModEntry.VersionDir)?.FullName;

        private static string Title
        {
            get
            {
                try
                {
                    var manifest = Path.Combine(Path.Combine(ModFolder, "Manifest"), "manifest.xml");
                    var xml = File.ReadAllText(manifest);
                    int a = xml.IndexOf("<name>", StringComparison.Ordinal), b = xml.IndexOf("</name>", StringComparison.Ordinal);
                    if (a >= 0 && b > a) return xml.Substring(a + 6, b - a - 6).Trim();
                }
                catch { }
                return "Wuxia: The New Dawn";
            }
        }

        public static bool Armed()
        {
            if (_armed < 0)
            {
                _armed = 0;
                try
                {
                    foreach (var arg in Environment.GetCommandLineArgs())
                    {
                        var a = arg?.ToLowerInvariant();
                        if (a == Flag) _armed = 1;
                        else if (a == PreviewFlag) { _armed = 1; _previewOnly = true; }
                    }
                }
                catch { }
            }
            return _armed == 1;
        }

        public static void Tick()
        {
            if (!Armed()) return;
            if (_start < 0f)
            {
                _start = Time.realtimeSinceStartup;
                Step("Modo de publicacao ligado (" + Title + ")");
            }
            if (_running) UpdateProgress();
            if (Time.realtimeSinceStartup >= _nextStatus)
            {
                _nextStatus = Time.realtimeSinceStartup + 1f;
                SaveStatus();
            }
            if (_done || _running) return;

            var folder = ModFolder;
            if (string.IsNullOrEmpty(folder) || folder.Replace('\\', '/').ToLowerInvariant().Contains("workshop/content"))
            {
                _done = true;
                Report("Publicacao ignorada: o mod precisa estar na pasta local 'mods' do jogo (rode o install.cmd).", false);
                return;
            }
            var steam = ChannelSteam.Instance;
            if ((object)steam == null || !steam || !steam.workshopInitialized)
            {
                if (!_toldWaiting) { _toldWaiting = true; Step("Esperando a Steam iniciar..."); }
                _state = "Esperando a Steam iniciar... (" + Seconds(_start) + ")";
                return;
            }
            var ugc = GetUgc(steam);
            if (ugc == null) { _done = true; return; }
            _running = true;
            try
            {
                ulong id = ReadId();
                if (_previewOnly)
                {
                    if (id == 0) Report("FALHOU: o mod ainda nao foi publicado (rode o Publicar.bat primeiro).", false);
                    else SendPreviewOnly(id, folder);
                    if (_result != 0) Finish();
                }
                else if (id != 0)
                {
                    Step("Steam pronta. Atualizando o item " + id);
                    Send(ugc, id, false);
                }
                else
                {
                    Step("Steam pronta. Criando item novo na Workshop...");
                    _state = "Criando o item na Workshop...";
                    ugc.createItem(OnCreated);
                }
            }
            catch (Exception ex)
            {
                Report("FALHOU ao iniciar a publicacao: " + ex.Message, false);
                _done = true;
                _running = false;
            }
        }

        private static UGCManager GetUgc(ChannelSteam steam)
        {
            try
            {
                var field = typeof(ChannelSteam).GetField("ugc", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var ugc = field?.GetValue(steam) as UGCManager;
                if (ugc == null) Report("FALHOU: nao achei o gerenciador da Workshop do jogo.", false);
                return ugc;
            }
            catch (Exception ex)
            {
                Report("FALHOU ao acessar a Workshop: " + ex.Message, false);
                return null;
            }
        }

        private static void OnCreated(UGCManager.OperationResult r)
        {
            if (r == null) { Report("FALHOU: a Steam nao respondeu ao criar o item.", false); Finish(); return; }
            if (r.needsLegalAgreement)
            {
                Report("A Steam pede que voce aceite o Acordo Legal da Workshop. Abra a pagina da Workshop do jogo na Steam, aceite o acordo e rode o Publicar.bat de novo.", false);
                Finish();
                return;
            }
            ulong id = r.itemId.m_PublishedFileId;
            if (!r.success || id == 0)
            {
                Report("FALHOU ao criar o item (" + r.result + " " + (r.error ?? "") + ")", false);
                Finish();
                return;
            }
            SaveId(id);
            Step("Item criado: " + id);
            try
            {
                var ugc = GetUgc(ChannelSteam.Instance);
                if (ugc == null) Finish();
                else Send(ugc, id, true);
            }
            catch (Exception ex)
            {
                Report("FALHOU ao enviar: " + ex.Message, false);
                Finish();
            }
        }

        private static void Send(UGCManager ugc, ulong id, bool isNew)
        {
            var folder = ModFolder;
            if (!isNew) { SendFilesOnly(id, folder); return; }
            var update = new UGCManager.ItemUpdate
            {
                title = Title,
                description = ReadDescription(),
                contentFolder = folder,
                previewFile = Path.Combine(Path.Combine(folder, "Manifest"), "preview.png"),
                tags = new List<string>(),
                visibility = ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPrivate,
            };
            Step("Enviando arquivos de " + folder + " (" + FolderSize(folder) + ")");
            Step("O item comeca PRIVADO: deixe publico na pagina da Workshop quando conferir.");
            _state = "Enviando para a Steam...";
            _ugc = ugc;
            _sendStart = _lastChange = Time.realtimeSinceStartup;
            ugc.updateItem(new PublishedFileId_t(id), update, OnUpdated, "Primeira versao");
        }

        private static void SendFilesOnly(ulong id, string folder)
        {
            _itemId = id;
            Step("Enviando SO os arquivos (titulo/descricao/capa ficam como estao na Steam)");
            Step("Pasta: " + folder + " (" + FolderSize(folder) + ")");
            var handle = SteamUGC.StartItemUpdate(SteamUtils.GetAppID(), new PublishedFileId_t(id));
            if (handle == UGCUpdateHandle_t.Invalid)
            {
                Report("FALHOU: a Steam nao aceitou iniciar a atualizacao do item " + id + ".", false);
                Finish();
                return;
            }
            if (!SteamUGC.SetItemContent(handle, folder))
            {
                Report("FALHOU: a Steam nao aceitou a pasta do mod: " + folder, false);
                Finish();
                return;
            }
            _directHandle = handle;
            _state = "Enviando para a Steam...";
            _sendStart = _lastChange = Time.realtimeSinceStartup;
            var call = SteamUGC.SubmitItemUpdate(handle, "Atualizacao do mod");
            _submitResult = CallResult<SubmitItemUpdateResult_t>.Create(OnSubmitted);
            _submitResult.Set(call);
        }

        private static void SendPreviewOnly(ulong id, string folder)
        {
            _itemId = id;
            var preview = Path.Combine(Path.Combine(folder, "Manifest"), "preview.png");
            Step("Trocando SO a capa do item " + id + " (" + Mb((ulong)new FileInfo(preview).Length) + ")");
            var handle = SteamUGC.StartItemUpdate(SteamUtils.GetAppID(), new PublishedFileId_t(id));
            if (handle == UGCUpdateHandle_t.Invalid)
            {
                Report("FALHOU: a Steam nao aceitou iniciar a atualizacao do item " + id + ".", false);
                return;
            }
            if (!SteamUGC.SetItemPreview(handle, preview))
            {
                Report("FALHOU: a Steam nao aceitou a imagem: " + preview, false);
                return;
            }
            _directHandle = handle;
            _state = "Enviando a capa para a Steam...";
            _sendStart = _lastChange = Time.realtimeSinceStartup;
            var call = SteamUGC.SubmitItemUpdate(handle, "Capa nova");
            _submitResult = CallResult<SubmitItemUpdateResult_t>.Create(OnSubmitted);
            _submitResult.Set(call);
        }

        private static void OnSubmitted(SubmitItemUpdateResult_t r, bool ioFailure)
        {
            if (!ioFailure && r.m_eResult == EResult.k_EResultOK)
            {
                if (r.m_bUserNeedsToAcceptWorkshopLegalAgreement)
                    Report("Enviado, mas a Steam pede que voce aceite o Acordo Legal da Workshop para o item ficar visivel.", true);
                else
                    Report("SUCESSO! Mod enviado para a Workshop. Item: " + _itemId + "  https://steamcommunity.com/sharedfiles/filedetails/?id=" + _itemId, true);
            }
            else
            {
                string why = ioFailure ? "falha de conexao" : r.m_eResult.ToString();
                Report("FALHOU ao enviar (" + why + ")." + Hint(why, ioFailure), false);
            }
            Finish();
        }

        private static void OnUpdated(UGCManager.OperationResult r)
        {
            if (r != null && r.success)
            {
                ulong id = r.itemId.m_PublishedFileId;
                Report("SUCESSO! Mod enviado para a Workshop (PRIVADO). Item: " + id + "  https://steamcommunity.com/sharedfiles/filedetails/?id=" + id, true);
            }
            else if (r != null && r.needsLegalAgreement)
            {
                Report("Enviado, mas a Steam pede que voce aceite o Acordo Legal da Workshop para o item ficar visivel.", true);
            }
            else
            {
                string why = r != null ? r.result.ToString() : "sem resposta";
                Report("FALHOU ao enviar (" + why + " " + (r?.error ?? "") + ")." + Hint(why, false), false);
            }
            Finish();
        }

        private static string Hint(string why, bool ioFailure)
        {
            if (why.IndexOf("FileNotFound", StringComparison.Ordinal) >= 0)
                return " O item foi apagado na Steam? Apague o arquivo " + IdPath() + " e publique de novo.";
            if (why.IndexOf("Busy", StringComparison.Ordinal) >= 0) return " A Steam esta ocupada; tente de novo em alguns minutos.";
            if (ioFailure || why.IndexOf("Timeout", StringComparison.Ordinal) >= 0) return " A conexao com a Steam caiu no meio; tente de novo.";
            return "";
        }

        private static void Finish()
        {
            _running = false;
            _done = true;
            SaveStatus();
        }

        private static void UpdateProgress()
        {
            if (_sendStart < 0f) return;
            try
            {
                if (_directHandle != UGCUpdateHandle_t.Invalid) { ReadProgress(_directHandle); return; }
                if (_ugc == null) return;
                if (!_searchedHandle)
                {
                    _searchedHandle = true;
                    foreach (var f in _ugc.GetType().GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                        if (f.FieldType == typeof(UGCUpdateHandle_t)) { _handleField = f; break; }
                    if (_handleField == null) Step("(a Steam nao informa a porcentagem neste jogo; mostrando so o tempo)");
                }
                if (_handleField == null) return;
                var h = (UGCUpdateHandle_t)_handleField.GetValue(_handleField.IsStatic ? null : _ugc);
                if (h.m_UGCUpdateHandle != 0 && h != UGCUpdateHandle_t.Invalid) ReadProgress(h);
            }
            catch { _handleField = null; }
        }

        private static void ReadProgress(UGCUpdateHandle_t h)
        {
            try
            {
                ulong done = 0, total = 0;
                var status = SteamUGC.GetItemUpdateProgress(h, out done, out total);
                string phase = PhaseName((int)status);
                if (phase != _lastPhase && phase.Length > 0) { _lastPhase = phase; Step(phase); }
                _phase = phase;
                if (done != _bytesDone || total != _bytesTotal) _lastChange = Time.realtimeSinceStartup;
                _bytesDone = done;
                _bytesTotal = total;
            }
            catch { }
        }

        private static string PhaseName(int s)
        {
            switch (s)
            {
                case 1: return "Preparando as informacoes do item";
                case 2: return "Preparando os arquivos (a Steam esta compactando)";
                case 3: return "Enviando os arquivos";
                case 4: return "Enviando a imagem de capa";
                case 5: return "Finalizando (a Steam esta gravando as mudancas)";
                default: return "";
            }
        }

        public static void Draw()
        {
            if (!Armed() || _start < 0f) return;
            PrepareStyles();
            float w = Mathf.Min(720f, Screen.width - 40f);
            int n = Mathf.Min(Lines.Count, 10);
            var box = new Rect(20f, 20f, w, 150f + n * 24f);
            GUI.DrawTexture(box, _bg);
            float y = box.y + 12f, x = box.x + 16f, iw = w - 32f;
            _title.normal.textColor = _result == 1 ? new Color(0.45f, 1f, 0.45f) : _result == 2 ? new Color(1f, 0.45f, 0.4f) : new Color(1f, 0.84f, 0.3f);
            GUI.Label(new Rect(x, y, iw, 30f), "PUBLICANDO NA WORKSHOP - " + Title, _title);
            y += 32f;
            string timer = _sendStart > 0f && _result == 0 ? "  |  envio: " + Seconds(_sendStart) : "";
            GUI.Label(new Rect(x, y, iw, 26f), _state + timer, _text);
            y += 28f;
            var bar = new Rect(x, y, iw, 22f);
            GUI.DrawTexture(bar, _barBg);
            float frac = _result == 1 ? 1f : _bytesTotal != 0 ? (float)((double)_bytesDone / _bytesTotal) : 0f;
            if (frac > 0f) GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(frac), bar.height), _bar);
            string label = _result == 1 ? "100%"
                : _bytesTotal != 0 ? (frac * 100f).ToString("0") + "%  (" + Mb(_bytesDone) + " de " + Mb(_bytesTotal) + ")"
                : _phase.Length > 0 ? _phase : _result == 0 ? "aguardando..." : "";
            GUI.Label(new Rect(bar.x + 8f, bar.y, bar.width - 16f, bar.height), label, _text);
            y += 30f;
            if (_result == 0 && _sendStart > 0f && _lastChange > 0f && Time.realtimeSinceStartup - _lastChange > 180f)
                GUI.Label(new Rect(x, y, iw, 24f), "Sem progresso ha " + Seconds(_lastChange) + ". Confira a internet e o upload da Steam.", _text);
            y += 26f;
            for (int i = Lines.Count - n; i < Lines.Count; i++) { GUI.Label(new Rect(x, y, iw, 24f), Lines[i], _text); y += 24f; }
            if (_result != 0) GUI.Label(new Rect(x, box.yMax - 26f, iw, 24f), "Pode fechar o jogo.", _text);
        }

        private static void PrepareStyles()
        {
            if (_text != null) return;
            _bg = Solid(new Color(0f, 0f, 0f, 0.82f));
            _barBg = Solid(new Color(1f, 1f, 1f, 0.15f));
            _bar = Solid(new Color(0.25f, 0.65f, 1f, 0.9f));
            _text = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = false, clipping = TextClipping.Clip };
            _text.normal.textColor = Color.white;
            _title = new GUIStyle(_text) { fontSize = 20, fontStyle = FontStyle.Bold };
        }

        private static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            t.hideFlags = HideFlags.HideAndDontSave;
            return t;
        }

        private static void Step(string msg)
        {
            Lines.Add(DateTime.Now.ToString("HH:mm:ss") + "  " + msg);
            if (Lines.Count > 50) Lines.RemoveAt(0);
            ModLog.Info("publicacao: " + msg);
            SaveStatus();
        }

        private static void SaveStatus()
        {
            try
            {
                string s = _state;
                if (_bytesTotal != 0 && _result == 0)
                    s += "  " + ((double)_bytesDone * 100.0 / _bytesTotal).ToString("0") + "% (" + Mb(_bytesDone) + " de " + Mb(_bytesTotal) + ")";
                else if (_phase.Length > 0 && _result == 0) s += "  [" + _phase + "]";
                if (_sendStart > 0f && _result == 0) s += "  tempo: " + Seconds(_sendStart);
                Directory.CreateDirectory(ModEntry.UserDir);
                File.WriteAllText(Path.Combine(ModEntry.UserDir, StatusFile), s);
            }
            catch { }
        }

        private static void Report(string msg, bool ok)
        {
            _result = ok ? 1 : 2;
            _state = ok ? "CONCLUIDO" : "FALHOU";
            Step(msg);
            try
            {
                File.WriteAllText(Path.Combine(ModEntry.UserDir, ResultFile), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + msg + "\r\n");
            }
            catch (Exception ex) { ModLog.Error(ex, "resultado da publicacao"); }
        }

        private static string Seconds(float since)
        {
            int s = Math.Max(0, (int)(Time.realtimeSinceStartup - since));
            return s / 60 + "m" + (s % 60).ToString("00") + "s";
        }

        private static string Mb(ulong b) => ((double)b / 1048576.0).ToString("0.0") + " MB";

        private static string FolderSize(string folder)
        {
            try
            {
                long total = 0;
                var files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
                foreach (var f in files) total += new FileInfo(f).Length;
                return files.Length + " arquivos, " + Mb((ulong)total);
            }
            catch { return "?"; }
        }

        private static string ReadDescription()
        {
            try
            {
                var path = Path.Combine(ModEntry.VersionDir, "workshop_description.txt");
                if (File.Exists(path)) return File.ReadAllText(path);
            }
            catch { }
            return "Wuxia/Xianxia cultivation for Order: The New Dawn. Work in progress, for testing only.\n\nSupport: https://ko-fi.com/pauloraz3n";
        }

        private static string IdPath() => Path.Combine(ModEntry.UserDir, IdFile);

        private static ulong ReadId()
        {
            try
            {
                var path = IdPath();
                if (File.Exists(path) && ulong.TryParse(File.ReadAllText(path).Trim(), out var id)) return id;
            }
            catch { }
            return 0;
        }

        private static void SaveId(ulong id)
        {
            try { File.WriteAllText(IdPath(), id.ToString()); }
            catch { }
        }
    }
}
