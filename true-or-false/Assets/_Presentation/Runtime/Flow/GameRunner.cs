using System;
using System.Collections;
using System.Collections.Generic;
using Suricatus.TrueOrFalse.Core;
using UnityEngine;

namespace Suricatus.TrueOrFalse.Presentation
{
    /// <summary>
    /// Ponte entre o nucleo e a cena: carrega o conteudo, controla o ciclo do totem
    /// (atracao -> partida -> resultado -> atracao) e faz o Tick da <see cref="GameSession"/>.
    ///
    /// A UI escuta os eventos daqui e da sessao. Nenhuma tela deve chamar a sessao diretamente.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameRunner : MonoBehaviour
    {
        public enum Phase { Loading, Attract, Playing, Result }

        [SerializeField] private GameConfig config;
        [SerializeField] private ThemeConfig theme;

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

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError("[TrueOrFalse] GameRunner sem GameConfig atribuido.", this);
                enabled = false;
                return;
            }

            Session.RoundFinished += OnRoundFinished;
        }

        private void OnDestroy()
        {
            Session.RoundFinished -= OnRoundFinished;
        }

        private IEnumerator Start()
        {
            SetPhase(Phase.Loading);

            yield return QuestionCatalogLoader.Load(config, (catalog, source) =>
            {
                _pool = catalog?.questions;
                if (_pool != null)
                    Debug.Log($"[TrueOrFalse] {_pool.Count} perguntas carregadas de {source}.");
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
                    // Volta sozinho para a atracao para nao deixar a tela travada no
                    // resultado do participante anterior enquanto a fila anda.
                    if (config.idleResetSeconds > 0f)
                    {
                        _idleSeconds += Time.deltaTime;
                        if (_idleSeconds >= config.idleResetSeconds) SetPhase(Phase.Attract);
                    }
                    break;
            }
        }

        /// <summary>Inicia uma partida. Chamado pelo toque na tela de atracao.</summary>
        public void StartRound()
        {
            if (Current == Phase.Playing || _pool == null) return;

            Session.Start(_pool, config.ToRoundSettings(), shuffleSeed);
            SetPhase(Phase.Playing);
        }

        public void AnswerTrue() => Session.Answer(true);
        public void AnswerFalse() => Session.Answer(false);

        /// <summary>Avanca o feedback quando o config usa <c>feedbackSeconds = 0</c>.</summary>
        public void Continue() => Session.ContinueFromFeedback();

        /// <summary>Abandona a partida em andamento e volta para a atracao.</summary>
        public void CancelRound()
        {
            if (Current != Phase.Playing) return;
            Session.Abort();
        }

        private void OnRoundFinished(RoundResult result)
        {
            SetPhase(Phase.Result);
            RoundFinished?.Invoke(result);
        }

        private void SetPhase(Phase phase)
        {
            Current = phase;
            _idleSeconds = 0f;
            PhaseChanged?.Invoke(phase);
        }
    }
}
