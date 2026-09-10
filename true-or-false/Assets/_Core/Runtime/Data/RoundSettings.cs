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
        public float secondsPerQuestion;
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
            feedbackSeconds = 2f,
            pointsPerCorrect = 100,
            pointsPerWrong = 0,
            pointsPerTimeout = 0,
            bonusPointsPerSecondLeft = 0f,
            shuffleQuestions = true,
        };
    }
}
