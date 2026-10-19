using System;
using UnityEngine;

namespace WuxiaMod.Game
{
    /// <summary>
    /// Objeto que vive o jogo todo (DontDestroyOnLoad) e roda tarefas periódicas,
    /// como manter os IDs curtos das nossas definições registrados (DefAliases).
    /// </summary>
    internal sealed class WuxiaRuntime : MonoBehaviour
    {
        private const float Interval = 1f;
        private float _next;
        private int _errors;

        public static void Create()
        {
            var go = new GameObject("Wuxia.Runtime");
            DontDestroyOnLoad(go);
            go.AddComponent<WuxiaRuntime>();
        }

        private void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + Interval;
            try
            {
                DefAliases.Apply();
            }
            catch (Exception ex)
            {
                // Não enche o log: mostra os primeiros erros e depois só a cada 60.
                if (_errors++ < 3 || _errors % 60 == 0) ModLog.Error(ex, "registro das definições do mod");
            }
        }
    }
}
