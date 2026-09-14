using System;
using NUnit.Framework;

namespace Suricatus.TrueOrFalse.Core.Tests
{
    /// <summary>
    /// Blindagem do formato dos CSVs da acao. Uma linha mal escapada so aparece semanas depois,
    /// na hora de consolidar os 5 totens — quando o evento ja acabou e nao da para refazer a coleta.
    /// </summary>
    public class SessionRecordsTests
    {
        [Test]
        public void Escape_CampoSimples_NaoGanhaAspas()
        {
            Assert.AreEqual("Bebidas Ilegais", Csv.Escape("Bebidas Ilegais"));
        }

        [Test]
        public void Escape_CampoComSeparador_VaiEntreAspas()
        {
            // Enunciado com ponto e virgula quebraria a contagem de colunas da planilha.
            Assert.AreEqual("\"Apostas; bets e azar\"", Csv.Escape("Apostas; bets e azar"));
        }

        [Test]
        public void Escape_AspasInternas_SaoDobradas()
        {
            Assert.AreEqual("\"Promessa de \"\"ganho facil\"\"\"", Csv.Escape("Promessa de \"ganho facil\""));
        }

        [Test]
        public void Escape_QuebraDeLinha_VaiEntreAspas()
        {
            Assert.AreEqual("\"linha um\nlinha dois\"", Csv.Escape("linha um\nlinha dois"));
        }

        [Test]
        public void SessionRecord_LinhaTemMesmasColunasDoCabecalho()
        {
            var record = new SessionRecord
            {
                SessionId = "totem-01-20260911-143000-0007",
                Totem = "totem-01",
                StartedAt = new DateTime(2026, 9, 11, 14, 30, 0),
                FinishedAt = new DateTime(2026, 9, 11, 14, 31, 12),
                Topic = "Direitos do Consumidor",
                Status = SessionStatus.Concluiu,
                Questions = 10,
                Answered = 10,
                Correct = 7,
                Wrong = 2,
                TimedOut = 1,
                Score = 700,
                DurationSeconds = 72,
            };

            Assert.AreEqual(Colunas(SessionRecord.Header), Colunas(record.ToCsvLine()));
            StringAssert.Contains("2026-09-11 14:30:00", record.ToCsvLine());
        }

        [Test]
        public void AnswerRecord_EnunciadoComSeparador_NaoDeslocaColunas()
        {
            var record = new AnswerRecord
            {
                SessionId = "totem-01-20260911-143000-0007",
                Totem = "totem-01",
                AnsweredAt = new DateTime(2026, 9, 11, 14, 30, 20),
                QuestionNumber = 3,
                QuestionId = "cdc-08",
                Category = "Direitos do Consumidor",
                Statement = "O consumidor tem 7 dias; contados da entrega, para desistir.",
                CorrectAnswer = true,
                PlayerAnswer = false,
                Verdict = AnswerVerdict.Wrong,
                MillisecondsLeft = 2400,
                Points = 0,
            };

            Assert.AreEqual(Colunas(AnswerRecord.Header), Colunas(record.ToCsvLine()));
        }

        /// <summary>Conta colunas respeitando as aspas, que e como a planilha vai ler o arquivo.</summary>
        private static int Colunas(string line)
        {
            int count = 1;
            bool quoted = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"') quoted = !quoted;
                else if (c == Csv.Separator && !quoted) count++;
            }

            return count;
        }
    }
}
