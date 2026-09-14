using System;
using System.IO;
using System.Text;
using Suricatus.TrueOrFalse.Core;
using UnityEngine;

namespace Suricatus.TrueOrFalse.Presentation
{
    /// <summary>
    /// Grava o que aconteceu no totem em dois CSVs, na mesma pasta gravavel onde o
    /// <see cref="QuestionCatalogLoader"/> ja deixa o relatorio de carga.
    ///
    /// Gravacao LOCAL e por acrescimo, uma linha por vez, com descarga imediata. Wi-Fi de evento
    /// cai justamente no pico de movimento, e totem sai da tomada sem aviso: o que ja foi escrito
    /// nunca se perde, e nenhuma partida depende de rede para ser contada.
    ///
    /// O componente e opcional. Sem ele na cena, o jogo roda exatamente como antes.
    /// </summary>
    [DisallowMultipleComponent]
    public class SessionLogger : MonoBehaviour
    {
        public const string SessionsFilePrefix = "partidas";
        public const string AnswersFilePrefix = "respostas";

        /// <summary>Arquivo opcional na pasta externa que batiza o aparelho. Uma linha, so o nome.</summary>
        public const string TotemFileName = "totem.txt";

        /// <summary>
        /// Partida fechada e ja gravada em disco. Quem quiser levar o dado para outro lugar
        /// (nuvem, painel, integracao) escuta aqui, em vez de refazer o rastreio da visita.
        /// O disparo acontece DEPOIS da gravacao local: o arquivo e a fonte da verdade.
        /// </summary>
        public event Action<SessionRecord> SessionClosed;

        [SerializeField] private GameRunner runner;

        [Header("Registro")]
        [Tooltip("Grava tambem uma linha por pergunta respondida. E o que permite medir acertos e " +
                 "erros de CADA pergunta no relatorio final. Desligue so se o arquivo precisar ficar minimo.")]
        [SerializeField] private bool logAnswers = true;

        /// <summary>Runner observado. Exposto para quem escuta <see cref="SessionClosed"/>.</summary>
        public GameRunner Runner => runner;

        private GameConfig _config;
        private string _totem;
        private string _folder;

        private SessionRecord _current;
        private bool _visitOpen;
        private int _counter;
        private float _startedAt;

        private void Awake()
        {
            if (runner == null) runner = GetComponent<GameRunner>();
            if (runner == null)
            {
                Debug.LogError("[TrueOrFalse] SessionLogger sem GameRunner. Nada sera registrado.", this);
                enabled = false;
                return;
            }

            _config = runner.Config;
            _folder = QuestionCatalogLoader.ExternalFolder(_config);
            _totem = ResolveTotemId();

            runner.PhaseChanged += OnPhaseChanged;
            runner.RoundStarted += OnRoundStarted;
            runner.RoundFinished += OnRoundFinished;
            runner.RoundAborted += OnRoundAborted;
            runner.Session.Answered += OnAnswered;

            Debug.Log($"[TrueOrFalse] Registro de partidas ligado. Totem '{_totem}', pasta: {_folder}");
        }

        private void OnDestroy()
        {
            if (runner == null) return;

            runner.PhaseChanged -= OnPhaseChanged;
            runner.RoundStarted -= OnRoundStarted;
            runner.RoundFinished -= OnRoundFinished;
            runner.RoundAborted -= OnRoundAborted;
            runner.Session.Answered -= OnAnswered;
        }

        /// <summary>Fecha a visita em aberto quando o app e encerrado com alguem no meio da partida.</summary>
        private void OnApplicationQuit() => CloseIfOpen(SessionStatus.AbandonouNoJogo);

        // ---------------------------------------------------------------- identidade do aparelho

        /// <summary>
        /// Descobre o nome deste totem, nesta ordem:
        ///
        /// 1. um arquivo 'totem.txt' na pasta externa;
        /// 2. o campo 'Totem Id' do GameConfig;
        /// 3. um resumo do identificador do aparelho.
        ///
        /// O arquivo vem primeiro por um motivo pratico: os 5 totens da acao rodam o MESMO APK,
        /// entao um id fixo no GameConfig sairia identico nos cinco e os CSVs nao teriam como ser
        /// separados na hora de consolidar. Com o arquivo, batizar cada aparelho e copiar uma linha
        /// de texto — o mesmo gesto que ja se faz para trocar as perguntas. O item 3 existe para o
        /// caso de alguem esquecer: mesmo assim os cinco arquivos saem distintos.
        /// </summary>
        private string ResolveTotemId()
        {
            try
            {
                string path = Path.Combine(_folder, TotemFileName);
                if (File.Exists(path))
                {
                    string name = File.ReadAllText(path, Encoding.UTF8).Trim();
                    if (!string.IsNullOrWhiteSpace(name)) return Sanitize(name);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TrueOrFalse] Nao foi possivel ler o arquivo de nome do totem: {e.Message}");
            }

            if (_config != null && !string.IsNullOrWhiteSpace(_config.totemId)) return Sanitize(_config.totemId);

            string device = SystemInfo.deviceUniqueIdentifier;
            if (string.IsNullOrWhiteSpace(device) || device == SystemInfo.unsupportedIdentifier) return "totem-sem-nome";

            return "totem-" + device.Substring(0, Math.Min(6, device.Length));
        }

        /// <summary>Tira do nome o que atrapalharia um nome de arquivo ou uma coluna de CSV.</summary>
        private static string Sanitize(string value)
        {
            var clean = new StringBuilder(value.Length);
            foreach (char c in value.Trim())
            {
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_') clean.Append(c);
                else if (char.IsWhiteSpace(c)) clean.Append('-');
            }
            return clean.Length > 0 ? clean.ToString().ToLowerInvariant() : "totem-sem-nome";
        }

        // ---------------------------------------------------------------- ciclo da visita

        private void OnPhaseChanged(GameRunner.Phase phase)
        {
            switch (phase)
            {
                // A visita comeca no toque da atracao, e nao na primeira pergunta: e assim que se
                // mede quantos pararam no totem mas desistiram antes de escolher um assunto.
                case GameRunner.Phase.SelectTopic:
                case GameRunner.Phase.Playing:
                    if (!_visitOpen) Open();
                    break;

                // Voltar para a atracao com visita aberta so acontece por desistencia: o caminho
                // normal fecha a visita em RoundFinished, antes de chegar aqui.
                case GameRunner.Phase.Attract:
                    CloseIfOpen(string.IsNullOrEmpty(_current.Topic)
                        ? SessionStatus.AbandonouNaSelecao
                        : SessionStatus.AbandonouNoJogo);
                    break;
            }
        }

        private void Open()
        {
            _counter++;
            _startedAt = Time.unscaledTime;

            _current = new SessionRecord
            {
                SessionId = $"{_totem}-{DateTime.Now:yyyyMMdd-HHmmss}-{_counter:D4}",
                Totem = _totem,
                StartedAt = DateTime.Now,
                Topic = string.Empty,
                Status = SessionStatus.AbandonouNaSelecao,
            };

            _visitOpen = true;
        }

        private void OnRoundStarted(string topic, int questions)
        {
            if (!_visitOpen) Open();

            // Assunto vazio significa catalogo inteiro — o caso de um cliente sem tela de escolha.
            _current.Topic = string.IsNullOrWhiteSpace(topic) ? "(todos)" : topic;
            _current.Questions = questions;
            _current.Status = SessionStatus.AbandonouNoJogo;
        }

        private void OnAnswered(AnswerOutcome outcome)
        {
            if (!_visitOpen) return;

            _current.Answered++;
            switch (outcome.Verdict)
            {
                case AnswerVerdict.Correct: _current.Correct++; break;
                case AnswerVerdict.Wrong: _current.Wrong++; break;
                case AnswerVerdict.TimedOut: _current.TimedOut++; break;
            }

            if (!logAnswers) return;

            var question = outcome.Question;
            var record = new AnswerRecord
            {
                SessionId = _current.SessionId,
                Totem = _totem,
                AnsweredAt = DateTime.Now,
                QuestionNumber = outcome.QuestionNumber,
                QuestionId = question != null ? question.id : string.Empty,
                Category = question != null ? question.category : string.Empty,
                Statement = question != null ? question.statement : string.Empty,
                CorrectAnswer = question != null && question.isTrue,
                PlayerAnswer = outcome.PlayerAnswer,
                Verdict = outcome.Verdict,
                MillisecondsLeft = Mathf.Max(0, Mathf.RoundToInt(outcome.SecondsLeft * 1000f)),
                Points = outcome.PointsAwarded,
            };

            Write(AnswersFilePrefix, AnswerRecord.Header, record.ToCsvLine());
        }

        private void OnRoundFinished(RoundResult result) => Close(result, SessionStatus.Concluiu);
        private void OnRoundAborted(RoundResult result) => Close(result, SessionStatus.AbandonouNoJogo);

        private void Close(RoundResult result, SessionStatus status)
        {
            if (!_visitOpen) return;

            _current.Score = result.Score;
            _current.Correct = result.Correct;
            _current.Wrong = result.Wrong;
            _current.TimedOut = result.TimedOut;
            if (result.TotalQuestions > 0) _current.Questions = result.TotalQuestions;

            Flush(status);
        }

        private void CloseIfOpen(SessionStatus status)
        {
            if (_visitOpen) Flush(status);
        }

        private void Flush(SessionStatus status)
        {
            _current.Status = status;
            _current.FinishedAt = DateTime.Now;
            _current.DurationSeconds = Mathf.Max(0, Mathf.RoundToInt(Time.unscaledTime - _startedAt));

            Write(SessionsFilePrefix, SessionRecord.Header, _current.ToCsvLine());
            SessionClosed?.Invoke(_current);

            _visitOpen = false;
            _current = default;
        }

        // ---------------------------------------------------------------- escrita

        /// <summary>
        /// Acrescenta uma linha ao arquivo do totem, criando o cabecalho na primeira vez.
        ///
        /// Abre e fecha a cada linha de proposito: e uma escrita a cada poucos segundos, e em troca
        /// o arquivo fica integro mesmo se o aparelho for desligado na tomada no meio do evento.
        /// </summary>
        private void Write(string prefix, string header, string line)
        {
            string path = Path.Combine(_folder, prefix + "-" + _totem + ".csv");

            try
            {
                Directory.CreateDirectory(_folder);
                bool isNew = !File.Exists(path) || new FileInfo(path).Length == 0;

                using (var writer = new StreamWriter(path, append: true, encoding: new UTF8Encoding(true)))
                {
                    if (isNew) writer.WriteLine(header);
                    writer.WriteLine(line);
                }
            }
            catch (Exception e)
            {
                // Registro nunca pode derrubar a partida: o publico esta na frente do totem.
                Debug.LogWarning("[TrueOrFalse] Falha ao gravar '" + path + "': " + e.Message);
            }
        }
    }
}
