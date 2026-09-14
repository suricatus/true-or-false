using System;
using System.Globalization;
using System.Text;

namespace Suricatus.TrueOrFalse.Core
{
    /// <summary>Como a partida terminou. Vira a coluna 'status' do CSV de partidas.</summary>
    public enum SessionStatus
    {
        /// <summary>Tocou na atracao e saiu sem escolher assunto.</summary>
        AbandonouNaSelecao,

        /// <summary>Comecou a jogar e saiu antes da ultima pergunta.</summary>
        AbandonouNoJogo,

        /// <summary>Respondeu a rodada inteira e viu a tela de resultado.</summary>
        Concluiu,
    }

    /// <summary>
    /// Uma visita ao totem: comeca no toque da atracao e termina no resultado ou na desistencia.
    ///
    /// E a unidade honesta de medida da acao. NAO representa uma pessoa: um grupo pode se revezar
    /// num aparelho so, e a mesma pessoa pode jogar varias vezes. O relatorio final deve dizer
    /// "partidas", e tratar publico alcancado como estimativa.
    /// </summary>
    public struct SessionRecord
    {
        public string SessionId;
        public string Totem;
        public DateTime StartedAt;
        public DateTime FinishedAt;

        /// <summary>Assunto escolhido. Vazio quando a pessoa saiu antes de escolher.</summary>
        public string Topic;

        public SessionStatus Status;

        /// <summary>Perguntas sorteadas para a rodada.</summary>
        public int Questions;

        /// <summary>Perguntas efetivamente respondidas (inclui as que estouraram o tempo).</summary>
        public int Answered;

        public int Correct;
        public int Wrong;
        public int TimedOut;
        public int Score;
        public int DurationSeconds;

        public static string Header =>
            "startedAt;finishedAt;totem;sessionId;topic;status;questions;answered;correct;wrong;timedOut;score;durationSeconds";

        public string ToCsvLine()
        {
            var line = new StringBuilder();
            Csv.Append(line, Csv.Timestamp(StartedAt));
            Csv.Append(line, Csv.Timestamp(FinishedAt));
            Csv.Append(line, Totem);
            Csv.Append(line, SessionId);
            Csv.Append(line, Topic);
            Csv.Append(line, Status.ToString());
            Csv.Append(line, Questions);
            Csv.Append(line, Answered);
            Csv.Append(line, Correct);
            Csv.Append(line, Wrong);
            Csv.Append(line, TimedOut);
            Csv.Append(line, Score);
            Csv.Append(line, DurationSeconds);
            return Csv.Finish(line);
        }
    }

    /// <summary>
    /// Uma resposta. E a linha que responde "qual pergunta o publico mais erra" — a metrica
    /// de maior valor para um orgao de educacao do consumidor, e a que sustenta o case.
    /// </summary>
    public struct AnswerRecord
    {
        public string SessionId;
        public string Totem;
        public DateTime AnsweredAt;

        public int QuestionNumber;
        public string QuestionId;
        public string Category;

        /// <summary>Enunciado copiado para o CSV: evita precisar do JSON em maos para ler o relatorio.</summary>
        public string Statement;

        public bool CorrectAnswer;
        public bool PlayerAnswer;
        public AnswerVerdict Verdict;

        /// <summary>Tempo restante em milissegundos. Inteiro de proposito: CSV com decimal quebra entre locales.</summary>
        public int MillisecondsLeft;

        public int Points;

        public static string Header =>
            "answeredAt;totem;sessionId;questionNumber;questionId;category;statement;correctAnswer;playerAnswer;verdict;msLeft;points";

        public string ToCsvLine()
        {
            var line = new StringBuilder();
            Csv.Append(line, Csv.Timestamp(AnsweredAt));
            Csv.Append(line, Totem);
            Csv.Append(line, SessionId);
            Csv.Append(line, QuestionNumber);
            Csv.Append(line, QuestionId);
            Csv.Append(line, Category);
            Csv.Append(line, Statement);
            Csv.Append(line, CorrectAnswer ? "VERDADEIRO" : "FALSO");
            Csv.Append(line, PlayerAnswer ? "VERDADEIRO" : "FALSO");
            Csv.Append(line, Verdict.ToString());
            Csv.Append(line, MillisecondsLeft);
            Csv.Append(line, Points);
            return Csv.Finish(line);
        }
    }

    /// <summary>
    /// Escrita de CSV.
    ///
    /// Separador ';' porque e o que o Excel em portugues abre com duplo clique, sem assistente
    /// de importacao. TODO numero e inteiro: decimal em CSV e a origem classica de planilha
    /// lendo numero como texto quando o locale da maquina nao bate com o do arquivo.
    /// </summary>
    public static class Csv
    {
        public const char Separator = ';';

        public static string Timestamp(DateTime value)
            => value == default ? string.Empty : value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        public static void Append(StringBuilder line, int value)
            => Append(line, value.ToString(CultureInfo.InvariantCulture));

        public static void Append(StringBuilder line, string value)
        {
            if (line.Length > 0) line.Append(Separator);
            line.Append(Escape(value));
        }

        public static string Finish(StringBuilder line) => line.ToString();

        /// <summary>
        /// Protege o campo quando ele carrega separador, aspas ou quebra de linha —
        /// enunciados de pergunta trazem os tres com frequencia.
        /// </summary>
        public static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            bool needsQuotes = value.IndexOf(Separator) >= 0
                               || value.IndexOf('"') >= 0
                               || value.IndexOf('\n') >= 0
                               || value.IndexOf('\r') >= 0;

            if (!needsQuotes) return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
