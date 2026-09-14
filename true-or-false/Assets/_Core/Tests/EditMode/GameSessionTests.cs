using System.Collections.Generic;
using NUnit.Framework;

namespace Suricatus.TrueOrFalse.Core.Tests
{
    /// <summary>
    /// Estes testes sao a blindagem do core: se uma customizacao de cliente quebrar
    /// alguma delas, a mecanica do jogo mudou e a mudanca precisa ser deliberada.
    /// </summary>
    public class GameSessionTests
    {
        private static List<Question> Pool(int count)
        {
            var list = new List<Question>();
            for (int i = 0; i < count; i++)
            {
                list.Add(new Question { id = $"q{i}", statement = $"Afirmacao {i}", isTrue = i % 2 == 0 });
            }
            return list;
        }

        private static RoundSettings Settings(int questions = 3, float seconds = 10f, float feedback = 1f)
        {
            var s = RoundSettings.Default;
            s.questionsPerRound = questions;
            s.secondsPerQuestion = seconds;
            s.feedbackSeconds = feedback;
            s.shuffleQuestions = false;
            return s;
        }

        [Test]
        public void Start_PresentsFirstQuestion()
        {
            var session = new GameSession();
            session.Start(Pool(5), Settings());

            Assert.AreEqual(SessionState.AwaitingAnswer, session.State);
            Assert.AreEqual(1, session.QuestionNumber);
            Assert.AreEqual(3, session.TotalQuestions);
        }

        [Test]
        public void CorrectAnswer_ScoresAndEntersFeedback()
        {
            var session = new GameSession();
            session.Start(Pool(5), Settings());

            Assert.IsTrue(session.Answer(session.CurrentQuestion.isTrue));
            Assert.AreEqual(100, session.Score);
            Assert.AreEqual(SessionState.ShowingFeedback, session.State);
        }

        [Test]
        public void WrongAnswer_DoesNotScore()
        {
            var session = new GameSession();
            session.Start(Pool(5), Settings());

            session.Answer(!session.CurrentQuestion.isTrue);
            Assert.AreEqual(0, session.Score);
        }

        [Test]
        public void SpeedBonus_RewardsRemainingTime()
        {
            var settings = Settings();
            settings.bonusPointsPerSecondLeft = 10f;

            var session = new GameSession();
            session.Start(Pool(5), settings);
            session.Tick(2f); // sobram 8 segundos
            session.Answer(session.CurrentQuestion.isTrue);

            Assert.AreEqual(100 + 80, session.Score);
        }

        [Test]
        public void SecondAnswer_IsIgnoredDuringFeedback()
        {
            var session = new GameSession();
            session.Start(Pool(5), Settings());

            session.Answer(session.CurrentQuestion.isTrue);
            Assert.IsFalse(session.Answer(true), "Toque duplo nao pode pontuar duas vezes.");
            Assert.AreEqual(100, session.Score);
        }

        [Test]
        public void Timeout_CountsAsUnansweredAndAdvances()
        {
            AnswerOutcome captured = default;
            var session = new GameSession();
            session.Answered += o => captured = o;
            session.Start(Pool(5), Settings());

            session.Tick(10f);
            Assert.AreEqual(AnswerVerdict.TimedOut, captured.Verdict);

            session.Tick(1f); // consome o feedback
            Assert.AreEqual(2, session.QuestionNumber);
        }

        [Test]
        public void Round_FinishesAfterConfiguredQuestions()
        {
            RoundResult result = default;
            bool finished = false;

            var session = new GameSession();
            session.RoundFinished += r => { result = r; finished = true; };
            session.Start(Pool(5), Settings(questions: 3));

            for (int i = 0; i < 3; i++)
            {
                session.Answer(session.CurrentQuestion.isTrue);
                session.Tick(1f);
            }

            Assert.IsTrue(finished);
            Assert.AreEqual(SessionState.Finished, session.State);
            Assert.AreEqual(3, result.Correct);
            Assert.AreEqual(3, result.TotalQuestions);
            Assert.AreEqual(1f, result.Accuracy);
        }

        [Test]
        public void UntimedRound_NeverTimesOut()
        {
            var session = new GameSession();
            session.Start(Pool(5), Settings(seconds: 0f));

            session.Tick(600f);
            Assert.AreEqual(SessionState.AwaitingAnswer, session.State);
        }

        [Test]
        public void LongStatement_GetsMoreTimeThanShortOne()
        {
            var settings = Settings(questions: 2);
            settings.extraSecondsPer100Characters = 5f;

            var pool = new List<Question>
            {
                new Question { id = "curta", statement = new string('a', 50), isTrue = true },
                new Question { id = "longa", statement = new string('a', 200), isTrue = true },
            };

            var session = new GameSession();
            session.Start(pool, settings);

            // 10 base + 5 por 100 caracteres: 12,5 na curta e 20 na longa.
            Assert.AreEqual(12.5f, session.SecondsTotal, 0.001f);
            session.Answer(true);
            session.ContinueFromFeedback();
            Assert.AreEqual(20f, session.SecondsTotal, 0.001f);
        }

        [Test]
        public void MaxSecondsPerQuestion_CapsTheLengthBonus()
        {
            var settings = Settings();
            settings.extraSecondsPer100Characters = 5f;
            settings.maxSecondsPerQuestion = 15f;

            var session = new GameSession();
            session.Start(new List<Question>
            {
                new Question { id = "enorme", statement = new string('a', 400), isTrue = true },
            }, settings);

            Assert.AreEqual(15f, session.SecondsTotal, 0.001f);
        }

        [Test]
        public void QuestionSeconds_OverridesConfigAndCap()
        {
            var settings = Settings();
            settings.extraSecondsPer100Characters = 5f;
            settings.maxSecondsPerQuestion = 15f;

            var session = new GameSession();
            session.Start(new List<Question>
            {
                new Question { id = "manual", statement = "Curta.", isTrue = true, seconds = 25f },
            }, settings);

            Assert.AreEqual(25f, session.SecondsTotal, 0.001f);
        }

        [Test]
        public void UntimedRound_IgnoresPerQuestionSeconds()
        {
            var settings = Settings(seconds: 0f);

            var session = new GameSession();
            session.Start(new List<Question>
            {
                new Question { id = "manual", statement = "Curta.", isTrue = true, seconds = 5f },
            }, settings);

            Assert.IsTrue(session.IsUntimed);
            session.Tick(600f);
            Assert.AreEqual(SessionState.AwaitingAnswer, session.State);
        }

        [Test]
        public void TimerTicked_ReportsTheTotalOfTheCurrentQuestion()
        {
            var settings = Settings();
            settings.extraSecondsPer100Characters = 5f;

            float reportedTotal = 0f;
            var session = new GameSession();
            session.TimerTicked += (left, total) => reportedTotal = total;
            session.Start(new List<Question>
            {
                new Question { id = "longa", statement = new string('a', 200), isTrue = true },
            }, settings);

            session.Tick(1f);
            Assert.AreEqual(20f, reportedTotal, 0.001f);
        }

        [Test]
        public void ManualFeedback_WaitsForContinue()
        {
            var session = new GameSession();
            session.Start(Pool(5), Settings(feedback: 0f));

            session.Answer(session.CurrentQuestion.isTrue);
            session.Tick(30f);
            Assert.AreEqual(SessionState.ShowingFeedback, session.State);

            Assert.IsTrue(session.ContinueFromFeedback());
            Assert.AreEqual(2, session.QuestionNumber);
        }

        [Test]
        public void Deck_DoesNotMutateSourceList()
        {
            var pool = Pool(5);
            var settings = Settings();
            settings.shuffleQuestions = true;

            new GameSession().Start(pool, settings, seed: 42);
            Assert.AreEqual(5, pool.Count);
        }
    }
}
