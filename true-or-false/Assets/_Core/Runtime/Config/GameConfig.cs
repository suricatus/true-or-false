using UnityEngine;

namespace Suricatus.TrueOrFalse.Core
{
    /// <summary>
    /// Regras da partida, por cliente. Duplique este asset em Assets/_Clients/&lt;Cliente&gt;/
    /// e ajuste os valores no Inspector: nenhuma dessas mudancas exige tocar em codigo.
    /// </summary>
    [CreateAssetMenu(menuName = "Suricatus/True or False/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Rodada")]
        [Tooltip("Quantas perguntas por partida. Use 0 para usar todas as perguntas do arquivo.")]
        [Min(0)] public int questionsPerRound = 10;

        [Tooltip("Tempo de resposta por pergunta. 0 desliga o cronometro.")]
        [Min(0f)] public float secondsPerQuestion = 10f;

        public bool shuffleQuestions = true;

        [Header("Ritmo da resposta")]
        [Tooltip("Suspense: quanto tempo o botao escolhido fica destacado antes de o jogo revelar a resposta.")]
        [Min(0f)] public float suspenseSeconds = 2f;

        [Tooltip("Duracao do piscar no botao da resposta certa.")]
        [Min(0f)] public float revealSeconds = 1.5f;

        [Tooltip("Intervalo de cada piscada. Menor = pisca mais rapido.")]
        [Min(0.02f)] public float revealBlinkInterval = 0.18f;

        [Tooltip("Segundos ate a tela de feedback avancar sozinha. 0 = espera o toque do jogador.")]
        [Min(0f)] public float feedbackAutoAdvanceSeconds = 0f;

        [Tooltip("Duracao da contagem animada da pontuacao na tela de feedback.")]
        [Min(0f)] public float scoreCountSeconds = 0.6f;

        [Header("Pontuacao")]
        public int pointsPerCorrect = 100;
        public int pointsPerWrong = 0;
        public int pointsPerTimeout = 0;

        [Tooltip("Pontos extras por segundo restante no acerto. 0 desliga o bonus de velocidade.")]
        [Min(0f)] public float bonusPointsPerSecondLeft = 0f;

        [Header("Totem")]
        [Tooltip("Segundos sem toque na tela de resultado antes de voltar sozinho para a atracao. 0 desliga.")]
        [Min(0f)] public float idleResetSeconds = 45f;

        [Header("Conteudo")]
        [Tooltip("Caminho do arquivo de perguntas, relativo a StreamingAssets.")]
        public string questionsFile = "Suricatus/questions.json";

        [Tooltip("Banco embutido usado se o arquivo externo faltar ou estiver invalido. O jogo nunca fica sem conteudo no evento.")]
        public TextAsset fallbackCatalog;

        /// <summary>
        /// Converte para os parametros do nucleo. O tempo de feedback vai zerado de proposito:
        /// a coreografia (suspense, revelacao e tela de feedback) e responsabilidade da apresentacao,
        /// que avanca a rodada chamando ContinueFromFeedback.
        /// </summary>
        public RoundSettings ToRoundSettings() => new RoundSettings
        {
            questionsPerRound = questionsPerRound,
            secondsPerQuestion = secondsPerQuestion,
            feedbackSeconds = 0f,
            pointsPerCorrect = pointsPerCorrect,
            pointsPerWrong = pointsPerWrong,
            pointsPerTimeout = pointsPerTimeout,
            bonusPointsPerSecondLeft = bonusPointsPerSecondLeft,
            shuffleQuestions = shuffleQuestions,
        };
    }
}
