namespace Suricatus.TrueOrFalse.Core
{
    public enum SessionState
    {
        Idle,
        AwaitingAnswer,
        ShowingFeedback,
        Finished,
    }

    public enum AnswerVerdict
    {
        Correct,
        Wrong,
        TimedOut,
    }

    /// <summary>Resultado de uma unica pergunta.</summary>
    public readonly struct AnswerOutcome
    {
        public readonly Question Question;
        public readonly AnswerVerdict Verdict;
        public readonly bool PlayerAnswer;
        public readonly float SecondsLeft;
        public readonly int PointsAwarded;
        public readonly int ScoreAfter;
        public readonly int QuestionNumber;

        public AnswerOutcome(Question question, AnswerVerdict verdict, bool playerAnswer,
            float secondsLeft, int pointsAwarded, int scoreAfter, int questionNumber)
        {
            Question = question;
            Verdict = verdict;
            PlayerAnswer = playerAnswer;
            SecondsLeft = secondsLeft;
            PointsAwarded = pointsAwarded;
            ScoreAfter = scoreAfter;
            QuestionNumber = questionNumber;
        }
    }

    /// <summary>Resultado consolidado da rodada, pronto para a tela final e para o relatorio do cliente.</summary>
    public readonly struct RoundResult
    {
        public readonly int Score;
        public readonly int Correct;
        public readonly int Wrong;
        public readonly int TimedOut;
        public readonly int TotalQuestions;
        public readonly float ElapsedSeconds;

        public RoundResult(int score, int correct, int wrong, int timedOut, int totalQuestions, float elapsedSeconds)
        {
            Score = score;
            Correct = correct;
            Wrong = wrong;
            TimedOut = timedOut;
            TotalQuestions = totalQuestions;
            ElapsedSeconds = elapsedSeconds;
        }

        public float Accuracy => TotalQuestions == 0 ? 0f : (float)Correct / TotalQuestions;
    }
}
