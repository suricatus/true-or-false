using System;
using System.Collections.Generic;

namespace Suricatus.TrueOrFalse.Core
{
    /// <summary>
    /// Nucleo do jogo: a maquina de estados de uma rodada de verdadeiro ou falso.
    ///
    /// Classe C# pura, sem UnityEngine e sem MonoBehaviour, dirigida de fora por
    /// <see cref="Tick"/> e <see cref="Answer"/>. Toda a apresentacao (UI, som, animacao,
    /// branding) reage aos eventos publicados aqui e nunca altera este estado diretamente.
    ///
    /// Customizar um jogo para um cliente NAO deve exigir mudanca neste arquivo.
    /// </summary>
    public sealed class GameSession
    {
        public event Action<Question, int, int> QuestionPresented;
        public event Action<float, float> TimerTicked;
        public event Action<AnswerOutcome> Answered;
        public event Action<RoundResult> RoundFinished;

        private RoundSettings _settings;
        private List<Question> _questions = new List<Question>();

        private int _index;
        private int _score;
        private int _correct;
        private int _wrong;
        private int _timedOut;

        private float _secondsLeft;
        private float _feedbackLeft;
        private float _elapsed;

        public SessionState State { get; private set; } = SessionState.Idle;
        public int Score => _score;
        public float SecondsLeft => _secondsLeft;
        public int QuestionNumber => _index + 1;
        public int TotalQuestions => _questions.Count;
        public Question CurrentQuestion => _index >= 0 && _index < _questions.Count ? _questions[_index] : null;

        /// <summary>True quando o timer esta desligado por configuracao (secondsPerQuestion &lt;= 0).</summary>
        public bool IsUntimed => _settings.secondsPerQuestion <= 0f;

        /// <summary>
        /// Inicia uma rodada sorteando as perguntas a partir de <paramref name="pool"/>.
        /// </summary>
        /// <param name="seed">0 usa aleatoriedade do sistema; qualquer outro valor torna o sorteio reproduzivel.</param>
        public void Start(IReadOnlyList<Question> pool, RoundSettings settings, int seed = 0)
        {
            if (pool == null || pool.Count == 0)
                throw new ArgumentException("Nao ha perguntas para iniciar a rodada.", nameof(pool));

            _settings = settings;
            _questions = QuestionDeck.Draw(pool, settings.questionsPerRound, settings.shuffleQuestions, seed);

            _index = 0;
            _score = 0;
            _correct = 0;
            _wrong = 0;
            _timedOut = 0;
            _elapsed = 0f;

            Present();
        }

        /// <summary>
        /// Registra a resposta do jogador. Ignorada (retorna false) fora de
        /// <see cref="SessionState.AwaitingAnswer"/>, o que protege contra duplo toque na tela.
        /// </summary>
        public bool Answer(bool playerSaysTrue)
        {
            if (State != SessionState.AwaitingAnswer) return false;

            var question = CurrentQuestion;
            bool correct = playerSaysTrue == question.isTrue;
            Resolve(correct ? AnswerVerdict.Correct : AnswerVerdict.Wrong, playerSaysTrue);
            return true;
        }

        /// <summary>Avanca o tempo. Deve ser chamado a cada frame pela camada de apresentacao.</summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return;

            switch (State)
            {
                case SessionState.AwaitingAnswer:
                    _elapsed += deltaTime;
                    if (IsUntimed) return;

                    _secondsLeft -= deltaTime;
                    if (_secondsLeft <= 0f)
                    {
                        _secondsLeft = 0f;
                        TimerTicked?.Invoke(0f, _settings.secondsPerQuestion);
                        Resolve(AnswerVerdict.TimedOut, false);
                        return;
                    }

                    TimerTicked?.Invoke(_secondsLeft, _settings.secondsPerQuestion);
                    break;

                case SessionState.ShowingFeedback:
                    _elapsed += deltaTime;
                    if (_settings.feedbackSeconds <= 0f) return;

                    _feedbackLeft -= deltaTime;
                    if (_feedbackLeft <= 0f) Advance();
                    break;
            }
        }

        /// <summary>
        /// Sai do feedback manualmente. Use quando <c>feedbackSeconds</c> for 0 ou menor,
        /// isto e, quando a tela tiver um botao "proxima" no lugar do avanco automatico.
        /// </summary>
        public bool ContinueFromFeedback()
        {
            if (State != SessionState.ShowingFeedback) return false;
            Advance();
            return true;
        }

        /// <summary>Encerra a rodada imediatamente (ex.: inatividade no totem).</summary>
        public void Abort()
        {
            if (State == SessionState.Idle || State == SessionState.Finished) return;
            Finish();
        }

        private void Present()
        {
            State = SessionState.AwaitingAnswer;
            _secondsLeft = _settings.secondsPerQuestion;

            QuestionPresented?.Invoke(CurrentQuestion, QuestionNumber, TotalQuestions);
            if (!IsUntimed) TimerTicked?.Invoke(_secondsLeft, _settings.secondsPerQuestion);
        }

        private void Resolve(AnswerVerdict verdict, bool playerAnswer)
        {
            int points;
            switch (verdict)
            {
                case AnswerVerdict.Correct:
                    _correct++;
                    points = _settings.pointsPerCorrect
                             + (int)(Math.Max(0f, _secondsLeft) * _settings.bonusPointsPerSecondLeft);
                    break;
                case AnswerVerdict.Wrong:
                    _wrong++;
                    points = _settings.pointsPerWrong;
                    break;
                default:
                    _timedOut++;
                    points = _settings.pointsPerTimeout;
                    break;
            }

            _score += points;

            var outcome = new AnswerOutcome(CurrentQuestion, verdict, playerAnswer,
                Math.Max(0f, _secondsLeft), points, _score, QuestionNumber);

            State = SessionState.ShowingFeedback;
            _feedbackLeft = _settings.feedbackSeconds;
            Answered?.Invoke(outcome);
        }

        private void Advance()
        {
            _index++;
            if (_index >= _questions.Count) Finish();
            else Present();
        }

        private void Finish()
        {
            State = SessionState.Finished;
            RoundFinished?.Invoke(new RoundResult(_score, _correct, _wrong, _timedOut, _questions.Count, _elapsed));
        }
    }
}
