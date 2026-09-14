using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Suricatus.TrueOrFalse.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace Suricatus.TrueOrFalse.Presentation
{
    /// <summary>
    /// Envia as partidas para uma planilha do Google, para acompanhar a acao a distancia.
    ///
    /// REGRA QUE SUSTENTA ESTE COMPONENTE: a nuvem e um canal ADICIONAL, nunca a fonte da verdade.
    /// O <see cref="SessionLogger"/> ja gravou a partida em disco antes de este codigo rodar, e o
    /// relatorio final sai dos CSVs. Wi-Fi de evento cai justamente no pico de movimento; se a
    /// contagem dependesse da rede, o dado perdido seria exatamente o do horario mais cheio.
    ///
    /// Por isso aqui nada bloqueia o jogo:
    ///
    /// - o envio acontece fora do fluxo da partida, numa corrotina propria;
    /// - o que nao subiu fica numa fila em disco e e reenviado quando a conexao volta;
    /// - qualquer falha de rede vira aviso no log, nunca erro em tela.
    ///
    /// O componente e opcional. Sem ele na cena, ou sem endereco configurado, o jogo roda igual.
    /// </summary>
    [DisallowMultipleComponent]
    public class CloudSync : MonoBehaviour
    {
        /// <summary>Fila em disco: uma linha por partida pendente, em JSON.</summary>
        public const string QueueFileName = "nuvem-pendentes.jsonl";

        [SerializeField] private SessionLogger logger;

        [Header("Envio")]
        [Tooltip("Segundos entre tentativas de envio. Nao precisa ser curto: a fila garante que " +
                 "nada se perde, e envio em lote pesa menos que um POST por partida.")]
        [SerializeField, Min(5f)] private float intervalSeconds = 30f;

        [Tooltip("Maximo de partidas por requisicao.")]
        [SerializeField, Min(1)] private int batchSize = 25;

        [Tooltip("Tempo limite de cada requisicao. Curto de proposito: num Wi-Fi ruim, requisicao " +
                 "pendurada so empilha trabalho.")]
        [SerializeField, Min(3)] private int timeoutSeconds = 15;

        private GameConfig _config;
        private string _queuePath;

        private readonly List<string> _pending = new List<string>();
        private bool _sending;

        /// <summary>Pede uma tentativa rapida na proxima volta, logo depois de uma partida fechar.</summary>
        private bool _kick;

        private void Awake()
        {
            if (logger == null) logger = GetComponent<SessionLogger>();
            if (logger == null)
            {
                Debug.LogWarning("[TrueOrFalse] CloudSync sem SessionLogger. Envio para a nuvem desligado.", this);
                enabled = false;
                return;
            }

            _config = logger.Runner != null ? logger.Runner.Config : null;

            if (_config == null || string.IsNullOrWhiteSpace(_config.cloudEndpoint))
            {
                // Sem endereco configurado o componente simplesmente nao faz nada. E o estado
                // normal de um cliente que nao quer acompanhamento remoto.
                enabled = false;
                return;
            }

            _queuePath = Path.Combine(QuestionCatalogLoader.ExternalFolder(_config), QueueFileName);
            logger.SessionClosed += OnSessionClosed;
        }

        private void OnDestroy()
        {
            if (logger != null) logger.SessionClosed -= OnSessionClosed;
        }

        private IEnumerator Start()
        {
            // Recupera o que ficou para tras num evento anterior, ou numa queda de energia.
            LoadQueue();
            Debug.Log($"[TrueOrFalse] Envio para a nuvem ligado. {_pending.Count} partida(s) na fila.");

            // Com fila herdada de uma sessao anterior, tenta logo. Esperar o intervalo cheio
            // atrasava a unica pista de que algo esta errado — quem olha o log nos primeiros
            // segundos conclui que o envio nem foi tentado.
            _kick = _pending.Count > 0;

            while (true)
            {
                // Instancia nova a cada volta: reaproveitar um WaitForSecondsRealtime entre
                // iteracoes ja rendeu espera que nao espera em algumas versoes do Unity.
                yield return new WaitForSecondsRealtime(_kick ? 2f : intervalSeconds);
                _kick = false;

                if (_pending.Count > 0 && !_sending) yield return SendBatch();
            }
        }

        // ---------------------------------------------------------------- fila

        private void OnSessionClosed(SessionRecord record)
        {
            _pending.Add(JsonUtility.ToJson(CloudRow.From(record)));
            SaveQueue();
            _kick = true;
        }

        private void LoadQueue()
        {
            try
            {
                if (!File.Exists(_queuePath)) return;
                foreach (var line in File.ReadAllLines(_queuePath, Encoding.UTF8))
                {
                    if (!string.IsNullOrWhiteSpace(line)) _pending.Add(line);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TrueOrFalse] Nao foi possivel ler a fila da nuvem: {e.Message}");
            }
        }

        /// <summary>
        /// Reescreve a fila inteira a cada mudanca. Sao poucas linhas curtas, e assim o arquivo
        /// nunca fica descrito pela metade se o totem for desligado no meio da gravacao.
        /// </summary>
        private void SaveQueue()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_queuePath) ?? ".");

                if (_pending.Count == 0)
                {
                    if (File.Exists(_queuePath)) File.Delete(_queuePath);
                    return;
                }

                File.WriteAllLines(_queuePath, _pending, new UTF8Encoding(false));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TrueOrFalse] Nao foi possivel gravar a fila da nuvem: {e.Message}");
            }
        }

        // ---------------------------------------------------------------- envio

        private IEnumerator SendBatch()
        {
            _sending = true;

            int count = Mathf.Min(batchSize, _pending.Count);
            string payload = BuildPayload(count);
            string url = _config.cloudEndpoint;

            // O log da tentativa vem ANTES do envio. Sem isso, uma falha que nao chega a completar
            // (app fechado no meio, requisicao pendurada) nao deixa rastro nenhum, e o problema
            // fica invisivel no totem, onde nao da para abrir o Console.
            Debug.Log($"[TrueOrFalse] Enviando {count} partida(s) para {url}");

            // O Apps Script SEMPRE responde com redirecionamento para googleusercontent.com.
            // Seguindo automaticamente, o Unity refaz a chamada como GET e o corpo do POST se
            // perde — o doPost nunca recebe os dados. Por isso o redirecionamento e seguido na
            // mao, repetindo o POST no endereco novo.
            for (int hop = 0; hop < 5; hop++)
            {
                using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.timeout = timeoutSeconds;
                    request.redirectLimit = 0;

                    yield return request.SendWebRequest();

                    long code = request.responseCode;

                    if (code == 301 || code == 302 || code == 303 || code == 307 || code == 308)
                    {
                        string destino = request.GetResponseHeader("Location");
                        if (string.IsNullOrEmpty(destino))
                        {
                            Debug.LogWarning("[TrueOrFalse] Redirecionamento sem endereco de destino.");
                            break;
                        }

                        url = destino;
                        continue;
                    }

                    string corpo = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                    bool rede = request.result == UnityWebRequest.Result.Success;

                    // HTTP 200 nao basta: o Apps Script responde 200 com ok:false quando a senha
                    // esta errada. Sem conferir o corpo, a partida sairia da fila como se tivesse
                    // sido gravada, e o dado sumiria de vez.
                    bool aceito = rede && corpo.Contains("\"ok\":true");

                    if (aceito)
                    {
                        // So tira da fila depois da confirmacao. Se a resposta se perder no caminho,
                        // a partida sobe duas vezes — o sessionId e unico, entao a duplicata e
                        // descartavel na leitura, e repetir e melhor que perder.
                        _pending.RemoveRange(0, count);
                        SaveQueue();
                        Debug.Log($"[TrueOrFalse] {count} partida(s) gravada(s) na planilha. " +
                                  $"Restam {_pending.Count} na fila.");
                    }
                    else if (rede)
                    {
                        Debug.LogWarning($"[TrueOrFalse] A planilha recusou o envio (HTTP {code}). " +
                                         $"Resposta: {Resumir(corpo)}. Confira a senha em 'Cloud Token'. " +
                                         $"{_pending.Count} partida(s) seguem na fila.");
                    }
                    else
                    {
                        Debug.LogWarning($"[TrueOrFalse] Envio falhou ({request.error}, HTTP {code}). " +
                                         $"{_pending.Count} partida(s) aguardando na fila.");
                    }
                }

                break;
            }

            _sending = false;
        }

        /// <summary>
        /// Monta o corpo na mao porque o JsonUtility do Unity nao serializa lista de strings
        /// ja em JSON sem escapar tudo de novo.
        /// </summary>
        private string BuildPayload(int count)
        {
            var body = new StringBuilder();
            body.Append("{\"token\":\"").Append(Escape(_config.cloudToken)).Append("\",\"rows\":[");

            for (int i = 0; i < count; i++)
            {
                if (i > 0) body.Append(',');
                body.Append(_pending[i]);
            }

            body.Append("]}");
            return body.ToString();
        }

        /// <summary>Corta a resposta para o log nao virar uma pagina de HTML inteira.</summary>
        private static string Resumir(string corpo)
        {
            if (string.IsNullOrWhiteSpace(corpo)) return "(vazia)";
            corpo = corpo.Replace("\n", " ").Replace("\r", " ").Trim();
            return corpo.Length <= 200 ? corpo : corpo.Substring(0, 200) + "...";
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        // ---------------------------------------------------------------- dados

        /// <summary>
        /// Espelho serializavel do <see cref="SessionRecord"/>. Existe porque o JsonUtility nao
        /// converte DateTime nem enum para texto legivel — e a planilha precisa de ambos legiveis.
        /// </summary>
        [Serializable]
        private class CloudRow
        {
            public string startedAt;
            public string finishedAt;
            public string totem;
            public string sessionId;
            public string topic;
            public string status;
            public int questions;
            public int answered;
            public int correct;
            public int wrong;
            public int timedOut;
            public int score;
            public int durationSeconds;

            public static CloudRow From(SessionRecord r) => new CloudRow
            {
                startedAt = Csv.Timestamp(r.StartedAt),
                finishedAt = Csv.Timestamp(r.FinishedAt),
                totem = r.Totem,
                sessionId = r.SessionId,
                topic = r.Topic,
                status = r.Status.ToString(),
                questions = r.Questions,
                answered = r.Answered,
                correct = r.Correct,
                wrong = r.Wrong,
                timedOut = r.TimedOut,
                score = r.Score,
                durationSeconds = r.DurationSeconds,
            };
        }
    }
}
