using System;
using System.Collections;
using System.Collections.Generic;
using Suricatus.TrueOrFalse.Core;
using UnityEngine;

namespace Suricatus.TrueOrFalse.Presentation
{
    /// <summary>
    /// Ponte entre o nucleo e a cena: carrega o conteudo, liga e desliga os paineis,
    /// faz o Tick da <see cref="GameSession"/> e conduz a coreografia da resposta
    /// (suspense -> revelacao -> tela de feedback).
    ///
    /// Os paineis apenas desenham e reportam toques; quem decide o que acontece e este script.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameRunner : MonoBehaviour
    {
        public enum Phase { Loading, Attract, Playing, Feedback, Result }

        [Header("Configuracao do cliente")]
        [SerializeField] private GameConfig config;
        [SerializeField] private ThemeConfig theme;

        [Header("Paineis")]
        [SerializeField] private AttractPanelView attractPanel;
        [SerializeField] private QuestionPanelView questionPanel;
        [SerializeField] private FeedbackPanelView feedbackPanel;
        [SerializeField] private ResultPanelView resultPanel;

        [Header("Audio (opcional)")]
        [SerializeField] private AudioSource audioSource;

        [Header("Debug")]
        [Tooltip("Semente fixa para o sorteio, util em demo e teste. 0 = aleatorio a cada partida.")]
        [SerializeField] private int shuffleSeed;

        public event Action<Phase> PhaseChanged;
        public event Action<RoundResult> RoundFinished;

        public GameSession Session { get; } = new GameSession();
        public GameConfig Config => config;
        public ThemeConfig Theme => theme;
        public Phase Current { get; private set; } = Phase.Loading;

        private List<Question> _pool;
        private float _idleSeconds;
        private Coroutine _answerRoutine;
        private bool _abortingToHome;

        private void Awake()
        {
            if (config == null || theme == null)
            {
                Debug.LogError("[TrueOrFalse] GameRunner precisa de um GameConfig e de um Theme.", this);
                enabled = false;
                return;
            }

            foreach (var panel in Panels()) panel?.Bind(this);

            Session.QuestionPresented += OnQuestionPresented;
            Session.TimerTicked += OnTimerTicked;
            Session.Answered += OnAnswered;
            Session.RoundFinished += OnRoundFinished;
        }

        private void OnDestroy()
        {
            Session.QuestionPresented -= OnQuestionPresented;
            Session.TimerTicked -= OnTimerTicked;
            Session.Answered -= OnAnswered;
            Session.RoundFinished -= OnRoundFinished;
        }

        private IEnumerator Start()
        {
            SetPhase(Phase.Loading);

            yield return QuestionCatalogLoader.Load(config, (catalog, source) =>
            {
                _pool = catalog?.questions;
                if (_pool != null) Debug.Log($"[TrueOrFalse] {_pool.Count} perguntas carregadas de {source}.");
            });

            if (_pool == null || _pool.Count == 0)
            {
                enabled = false;
                yield break;
            }

            SetPhase(Phase.Attract);
        }

        private void Update()
        {
            switch (Current)
            {
                case Phase.Playing:
                    Session.Tick(Time.deltaTime);
                    break;

                case Phase.Result:
                    // Volta sozinho para a atracao: a tela nao pode ficar travada no
                    // resultado do participante anterior enquanto a fila anda.
                    if (config.idleResetSeconds > 0f)
                    {
                        _idleSeconds += Time.deltaTime;
                        if (_idleSeconds >= config.idleResetSeconds) GoHome();
                    }
                    break;
            }
        }

        // ---------------------------------------------------------------- acoes

        /// <summary>Inicia uma partida. Chamado pelo toque na tela de atracao.</summary>
        public void StartRound()
        {
            if (Current != Phase.Attract || _pool == null) return;
            Session.Start(_pool, config.ToRoundSettings(), shuffleSeed);
        }

        /// <summary>Registra a resposta. Toques fora da hora sao ignorados pelo proprio nucleo.</summary>
        public void Answer(bool playerSaysTrue) => Session.Answer(playerSaysTrue);

        /// <summary>Avanca a partir da tela de feedback.</summary>
        public void Continue()
        {
            if (Current != Phase.Feedback) return;
            Session.ContinueFromFeedback();
        }

        /// <summary>Volta para a atracao, pronto para o proximo participante.</summary>
        public void GoHome()
        {
            if (_answerRoutine != null)
            {
                StopCoroutine(_answerRoutine);
                _answerRoutine = null;
            }

            if (Current == Phase.Playing || Current == Phase.Feedback)
            {
                // Abort encerra a sessao e dispara RoundFinished. Aqui a partida foi
                // abandonada, entao a tela de resultado nao deve aparecer.
                _abortingToHome = true;
                Session.Abort();
                _abortingToHome = false;
            }

            SetPhase(Phase.Attract);
        }

        public Sprite ResolveQuestionImage(Question question)
        {
            if (question == null || theme == null) return null;
            return theme.question.ResolveImage(question.imageKey);
        }

        // ---------------------------------------------------------------- sessao

        private void OnQuestionPresented(Question question, int number, int total)
        {
            // Ativa o painel antes de escrever nele: um painel desativado nao roda corrotinas.
            SetPhase(Phase.Playing);
            questionPanel?.ShowQuestion(question, Session.Score);
        }

        private void OnTimerTicked(float secondsLeft, float secondsTotal)
        {
            questionPanel?.UpdateTimer(secondsLeft, secondsTotal);
        }

        private void OnAnswered(AnswerOutcome outcome)
        {
            if (_answerRoutine != null) StopCoroutine(_answerRoutine);
            _answerRoutine = StartCoroutine(AnswerRoutine(outcome));
        }

        /// <summary>
        /// A coreografia que o jogador ve depois de responder:
        /// 1. o botao escolhido fica destacado durante o suspense;
        /// 2. o botao da resposta CERTA pisca;
        /// 3. entra a tela de feedback com a curiosidade e a pontuacao.
        /// </summary>
        private IEnumerator AnswerRoutine(AnswerOutcome outcome)
        {
            questionPanel?.LockAndHighlight(outcome);

            if (config.suspenseSeconds > 0f)
                yield return new WaitForSeconds(config.suspenseSeconds);

            // O som so entra na revelacao: tocado antes, entregaria a resposta durante o suspense.
            PlaySfx(outcome.Verdict);

            if (questionPanel != null && outcome.Question != null && config.revealSeconds > 0f)
            {
                yield return questionPanel.RevealCorrect(
                    outcome.Question.isTrue, config.revealSeconds, config.revealBlinkInterval);
            }

            SetPhase(Phase.Feedback);
            feedbackPanel?.Show(outcome, config.scoreCountSeconds);

            if (config.feedbackAutoAdvanceSeconds > 0f)
            {
                yield return new WaitForSeconds(config.feedbackAutoAdvanceSeconds);
                Continue();
            }

            _answerRoutine = null;
        }

        private void OnRoundFinished(RoundResult result)
        {
            if (_abortingToHome) return;

            SetPhase(Phase.Result);
            resultPanel?.Show(result);
            RoundFinished?.Invoke(result);
        }

        // ---------------------------------------------------------------- paineis

        private IEnumerable<PanelView> Panels()
        {
            yield return attractPanel;
            yield return questionPanel;
            yield return feedbackPanel;
            yield return resultPanel;
        }

        private void SetPhase(Phase phase)
        {
            Current = phase;
            _idleSeconds = 0f;

            SetVisible(attractPanel, phase == Phase.Attract);
            SetVisible(questionPanel, phase == Phase.Playing);
            SetVisible(feedbackPanel, phase == Phase.Feedback);
            SetVisible(resultPanel, phase == Phase.Result);

            PhaseChanged?.Invoke(phase);
        }

        private static void SetVisible(PanelView panel, bool visible)
        {
            if (panel == null) return;
            if (visible) panel.Show();
            else panel.Hide();
        }

        private void PlaySfx(AnswerVerdict verdict)
        {
            if (audioSource == null || theme == null) return;

            var clip = verdict == AnswerVerdict.Correct ? theme.correctSfx
                : verdict == AnswerVerdict.TimedOut ? theme.timeoutSfx
                : theme.wrongSfx;

            if (clip != null) audioSource.PlayOneShot(clip);
        }
    }
}
