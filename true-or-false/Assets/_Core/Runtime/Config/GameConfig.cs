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

        [Tooltip("Tempo da tela de feedback. 0 exige que o jogador toque para avancar.")]
        [Min(0f)] public float feedbackSeconds = 2f;

        public bool shuffleQuestions = true;

        [Header("Pontuacao")]
        public int pointsPerCorrect = 100;
        public int pointsPerWrong = 0;
        public int pointsPerTimeout = 0;

        [Tooltip("Pontos extras por segundo restante no acerto. 0 desliga o bonus de velocidade.")]
        [Min(0f)] public float bonusPointsPerSecondLeft = 0f;

        [Header("Totem")]
        [Tooltip("Segundos sem toque na tela de atracao ou de resultado antes de reiniciar o ciclo. 0 desliga.")]
        [Min(0f)] public float idleResetSeconds = 45f;

        [Header("Conteudo")]
        [Tooltip("Caminho do arquivo de perguntas, relativo a StreamingAssets.")]
        public string questionsFile = "Suricatus/questions.json";

        [Tooltip("Banco embutido usado se o arquivo externo faltar ou estiver invalido. O jogo nunca fica sem conteudo no evento.")]
        public TextAsset fallbackCatalog;

        public RoundSettings ToRoundSettings() => new RoundSettings
        {
            questionsPerRound = questionsPerRound,
            secondsPerQuestion = secondsPerQuestion,
            feedbackSeconds = feedbackSeconds,
            pointsPerCorrect = pointsPerCorrect,
            pointsPerWrong = pointsPerWrong,
            pointsPerTimeout = pointsPerTimeout,
            bonusPointsPerSecondLeft = bonusPointsPerSecondLeft,
            shuffleQuestions = shuffleQuestions,
        };
    }
}
