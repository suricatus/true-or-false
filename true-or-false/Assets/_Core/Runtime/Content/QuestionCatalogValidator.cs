using System.Collections.Generic;

namespace Suricatus.TrueOrFalse.Core
{
    /// <summary>
    /// Confere um catalogo antes de a rodada comecar. Erros invalidam o arquivo
    /// (o jogo cai no fallback); avisos apenas relatam problemas de conteudo.
    /// </summary>
    public static class QuestionCatalogValidator
    {
        public static bool Validate(QuestionCatalog catalog, int questionsPerRound,
            out List<string> errors, out List<string> warnings)
        {
            errors = new List<string>();
            warnings = new List<string>();

            if (catalog == null)
            {
                errors.Add("Catalogo nulo: o arquivo nao pode ser lido como JSON.");
                return false;
            }

            if (catalog.questions == null || catalog.questions.Count == 0)
            {
                errors.Add("O catalogo nao tem nenhuma pergunta.");
                return false;
            }

            var seen = new HashSet<string>();
            for (int i = 0; i < catalog.questions.Count; i++)
            {
                var q = catalog.questions[i];
                string where = $"pergunta #{i + 1}";

                if (q == null)
                {
                    errors.Add($"{where}: entrada vazia.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(q.statement))
                    errors.Add($"{where}: campo 'statement' vazio.");

                if (q.seconds < 0f)
                    errors.Add($"{where}: campo 'seconds' negativo ({q.seconds}).");

                if (string.IsNullOrWhiteSpace(q.id))
                    warnings.Add($"{where}: sem 'id'. Ids ajudam a rastrear a pergunta no relatorio.");
                else if (!seen.Add(q.id))
                    warnings.Add($"{where}: id '{q.id}' repetido.");
            }

            if (questionsPerRound > 0 && catalog.questions.Count < questionsPerRound)
            {
                warnings.Add($"O catalogo tem {catalog.questions.Count} perguntas, " +
                             $"menos que as {questionsPerRound} da rodada. A partida ficara mais curta.");
            }

            return errors.Count == 0;
        }
    }
}
