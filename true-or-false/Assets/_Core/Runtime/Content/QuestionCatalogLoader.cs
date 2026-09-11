using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Suricatus.TrueOrFalse.Core
{
    /// <summary>
    /// Le o catalogo de perguntas tentando tres fontes, nesta ordem:
    ///
    /// 1. a pasta externa gravavel do aparelho;
    /// 2. o arquivo que veio no build (StreamingAssets);
    /// 3. o banco embutido do <see cref="GameConfig"/>.
    ///
    /// A pasta externa existe por causa do evento: em Android o StreamingAssets vive DENTRO do APK
    /// e ninguem consegue escrever nele, entao trocar as perguntas exigiria um build novo.
    /// Com a pasta externa, trocar e copiar um arquivo e reabrir o app.
    ///
    /// Cada fonte e validada antes de ser aceita, e a busca continua quando uma falha: um JSON
    /// externo com virgula sobrando nunca derruba a partida, apenas e ignorado.
    /// </summary>
    public static class QuestionCatalogLoader
    {
        /// <summary>Relatorio deixado na pasta externa. No totem e a unica forma de conferir a troca sem um PC.</summary>
        public const string ReportFileName = "questions-report.txt";

        /// <summary>
        /// Carrega e valida o catalogo. O callback sempre recebe um catalogo utilizavel ou null
        /// (nesse caso nao ha conteudo algum e a rodada nao deve iniciar).
        /// </summary>
        public static IEnumerator Load(GameConfig config, Action<QuestionCatalog, string> onDone)
        {
            var log = new List<string>();
            QuestionCatalog catalog = null;
            string source = null;

            string externalFolder = ExternalFolder(config);
            string externalFile = ExternalFile(config, externalFolder);

            // 1. Pasta externa. E a unica fonte que pode mudar sem gerar um APK novo, por isso vem primeiro.
            if (!string.IsNullOrEmpty(externalFile))
            {
                catalog = TryLocalFile(externalFile, config.questionsPerRound, log);
                if (catalog != null) source = externalFile;
            }

            // 2. O arquivo do build. UnityWebRequest porque em Android o caminho aponta para dentro do APK.
            if (catalog == null)
            {
                string url = StreamingAssetsUrl(config.questionsFile);
                string json = null;

                using (var request = UnityWebRequest.Get(url))
                {
                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success) json = request.downloadHandler.text;
                    else log.Add($"Arquivo do build nao pode ser lido ({url}): {request.error}");
                }

                catalog = Parse(json, config.questionsPerRound, log);
                if (catalog != null) source = $"{config.questionsFile} (arquivo do build)";
            }

            // 3. Banco embutido. Ultimo recurso para o totem nunca ficar sem conteudo no meio do evento.
            if (catalog == null)
            {
                catalog = Parse(config.fallbackCatalog != null ? config.fallbackCatalog.text : null,
                    config.questionsPerRound, log);

                if (catalog != null) source = "banco embutido (fallback)";
                else log.Add("O GameConfig nao tem 'Fallback Catalog' preenchido: nao sobrou nenhuma fonte.");
            }

            if (catalog == null)
                Debug.LogError("[TrueOrFalse] Nenhum catalogo valido encontrado. Verifique o JSON e o fallback do GameConfig.");

            if (config.writeLoadReport) WriteReport(externalFolder, externalFile, source, catalog, log);

            onDone?.Invoke(catalog, source);
        }

        // ---------------------------------------------------------------- caminhos

        /// <summary>
        /// Pasta gravavel consultada antes do arquivo do build. Vazia no config significa a pasta
        /// de dados do app, que e sempre gravavel e nao depende de permissao nenhuma.
        /// </summary>
        public static string ExternalFolder(GameConfig config)
        {
            string configured = config != null ? config.externalContentFolder : null;
            return string.IsNullOrWhiteSpace(configured) ? Application.persistentDataPath : configured.Trim();
        }

        /// <summary>
        /// Arquivo externo esperado: so o NOME do arquivo configurado, direto na pasta externa.
        /// Sem replicar subpastas — em campo, copiar um arquivo para uma pasta e o que tem que ser simples.
        /// </summary>
        public static string ExternalFile(GameConfig config, string folder)
        {
            if (string.IsNullOrEmpty(folder)) return null;

            string name = Path.GetFileName(config != null ? config.questionsFile : null);
            if (string.IsNullOrWhiteSpace(name)) name = "questions.json";

            return Path.Combine(folder, name);
        }

        private static string StreamingAssetsUrl(string relativePath)
        {
            string full = Path.Combine(Application.streamingAssetsPath, relativePath ?? string.Empty);
            return full.Contains("://") ? full : "file://" + full;
        }

        // ---------------------------------------------------------------- leitura

        private static QuestionCatalog TryLocalFile(string path, int questionsPerRound, List<string> log)
        {
            string json;
            try
            {
                if (!File.Exists(path))
                {
                    log.Add($"Nenhum arquivo externo em '{path}'. Usando o conteudo do build.");
                    return null;
                }

                json = File.ReadAllText(path, Encoding.UTF8);
            }
            catch (Exception e)
            {
                log.Add($"Arquivo externo '{path}' nao pode ser lido: {e.Message}");
                return null;
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                log.Add($"Arquivo externo '{path}' esta VAZIO e nao esta em uso.");
                return null;
            }

            var catalog = Parse(json, questionsPerRound, log);
            if (catalog == null) log.Add($"Arquivo externo '{path}' foi RECUSADO e nao esta em uso. Corrija o JSON e reabra o app.");
            return catalog;
        }

        private static QuestionCatalog Parse(string json, int questionsPerRound, List<string> log)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            QuestionCatalog catalog;
            try
            {
                catalog = JsonUtility.FromJson<QuestionCatalog>(json);
            }
            catch (Exception e)
            {
                Record(log, $"JSON malformado: {e.Message}");
                return null;
            }

            if (!QuestionCatalogValidator.Validate(catalog, questionsPerRound, out var errors, out var warnings))
            {
                foreach (var error in errors) Record(log, error);
                return null;
            }

            foreach (var warning in warnings) Record(log, warning);
            return catalog;
        }

        private static void Record(List<string> log, string message)
        {
            log.Add(message);
            Debug.LogWarning($"[TrueOrFalse] {message}");
        }

        // ---------------------------------------------------------------- relatorio

        /// <summary>
        /// Deixa na pasta externa um arquivo de texto dizendo qual fonte entrou em uso e por que.
        /// Sem isso, trocar as perguntas no totem seria as cegas: nao da para abrir o Console em campo.
        /// A pasta e criada mesmo quando nao ha arquivo externo, para aparecer no gerenciador de arquivos.
        /// </summary>
        private static void WriteReport(string folder, string expectedFile, string source,
            QuestionCatalog catalog, List<string> log)
        {
            if (string.IsNullOrEmpty(folder)) return;

            var text = new StringBuilder();
            text.AppendLine("True or False - relatorio de carga das perguntas");
            text.AppendLine($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            text.AppendLine();
            text.AppendLine("Para trocar as perguntas, copie o seu JSON para:");
            text.AppendLine($"  {expectedFile}");
            text.AppendLine("e reabra o aplicativo.");
            text.AppendLine();

            int count = catalog != null && catalog.questions != null ? catalog.questions.Count : 0;
            text.AppendLine(catalog != null
                ? $"RESULTADO: {count} pergunta(s) carregada(s)."
                : "RESULTADO: NENHUMA pergunta carregada. O jogo nao vai iniciar partida.");
            text.AppendLine($"FONTE EM USO: {source ?? "nenhuma"}");

            if (catalog != null && !string.IsNullOrWhiteSpace(catalog.clientName))
                text.AppendLine($"CLIENTE NO ARQUIVO: {catalog.clientName}");

            if (log.Count > 0)
            {
                text.AppendLine();
                text.AppendLine("Detalhes:");
                foreach (var line in log) text.AppendLine($"  - {line}");
            }

            try
            {
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, ReportFileName), text.ToString(), Encoding.UTF8);
            }
            catch (Exception e)
            {
                // Relatorio e conveniencia: se a pasta nao aceita escrita, o jogo segue normalmente.
                Debug.LogWarning($"[TrueOrFalse] Nao foi possivel gravar o relatorio em '{folder}': {e.Message}");
            }
        }
    }
}
