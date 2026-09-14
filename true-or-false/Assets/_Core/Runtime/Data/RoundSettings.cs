using System;

namespace Suricatus.TrueOrFalse.Core
{
    /// <summary>
    /// Parametros de uma rodada. Struct puro, sem dependencia de Unity, para que
    /// <see cref="GameSession"/> seja testavel fora do editor.
    /// </summary>
    [Serializable]
    public struct RoundSettings
    {
        public int questionsPerRound;

        /// <summary>Tempo base de cada pergunta. 0 ou menos desliga o cronometro da rodada inteira.</summary>
        public float secondsPerQuestion;

        /// <summary>
        /// Segundos somados ao tempo base a cada 100 caracteres do enunciado, para que
        /// perguntas longas ganhem mais tempo de leitura. 0 mantem o tempo igual para todas.
        /// </summary>
        public float extraSecondsPer100Characters;

        /// <summary>Teto do tempo calculado pelo tamanho. 0 desliga o teto.</summary>
        public float maxSecondsPerQuestion;

        public float feedbackSeconds;

        public int pointsPerCorrect;
        public int pointsPerWrong;
        public int pointsPerTimeout;

        /// <summary>Pontos extras por segundo restante no acerto. Zero desliga o bonus.</summary>
        public float bonusPointsPerSecondLeft;

        public bool shuffleQuestions;

        public static RoundSettings Default => new RoundSettings
        {
            questionsPerRound = 10,
            secondsPerQuestion = 10f,
            extraSecondsPer100Characters = 0f,
            maxSecondsPerQuestion = 0f,
            feedbackSeconds = 2f,
            pointsPerCorrect = 100,
            pointsPerWrong = 0,
            pointsPerTimeout = 0,
            bonusPointsPerSecondLeft = 0f,
            shuffleQuestions = true,
        };
    }
}
