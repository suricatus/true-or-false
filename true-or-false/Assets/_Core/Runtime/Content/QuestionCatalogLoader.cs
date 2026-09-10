using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Suricatus.TrueOrFalse.Core
{
    /// <summary>
    /// Le o catalogo de perguntas de StreamingAssets em runtime, sem rebuild.
    /// Usa UnityWebRequest porque em Android o StreamingAssets vive dentro do APK
    /// e nao pode ser lido com File.ReadAllText.
    /// </summary>
    public static class QuestionCatalogLoader
    {
        /// <summary>
        /// Carrega e valida o catalogo. Se o arquivo externo faltar ou estiver invalido,
        /// entrega o fallback embutido em <paramref name="config"/>. O callback sempre recebe
        /// um catalogo utilizavel ou null (nesse caso nao ha conteudo algum e a rodada nao deve iniciar).
        /// </summary>
        public static IEnumerator Load(GameConfig config, Action<QuestionCatalog, string> onDone)
        {
            string url = ResolveUrl(config.questionsFile);
            string json = null;

            using (var request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    json = request.downloadHandler.text;
                }
                else
                {
                    Debug.LogWarning($"[TrueOrFalse] Nao foi possivel ler '{url}': {request.error}. Usando o banco embutido.");
                }
            }

            var catalog = Parse(json, config.questionsPerRound);
            string source = config.questionsFile;

            if (catalog == null)
            {
                catalog = Parse(config.fallbackCatalog != null ? config.fallbackCatalog.text : null,
                    config.questionsPerRound);
                source = catalog != null ? "banco embutido (fallback)" : null;
            }

            if (catalog == null)
                Debug.LogError("[TrueOrFalse] Nenhum catalogo valido encontrado. Verifique o JSON e o fallback do GameConfig.");

            onDone?.Invoke(catalog, source);
        }

        private static QuestionCatalog Parse(string json, int questionsPerRound)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            QuestionCatalog catalog;
            try
            {
                catalog = JsonUtility.FromJson<QuestionCatalog>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TrueOrFalse] JSON malformado: {e.Message}");
                return null;
            }

            if (!QuestionCatalogValidator.Validate(catalog, questionsPerRound, out var errors, out var warnings))
            {
                foreach (var error in errors) Debug.LogWarning($"[TrueOrFalse] {error}");
                return null;
            }

            foreach (var warning in warnings) Debug.LogWarning($"[TrueOrFalse] {warning}");
            return catalog;
        }

        private static string ResolveUrl(string relativePath)
        {
            string full = Path.Combine(Application.streamingAssetsPath, relativePath ?? string.Empty);
            return full.Contains("://") ? full : "file://" + full;
        }
    }
}
