using System;
using System.Collections.Generic;

namespace Suricatus.TrueOrFalse.Core
{
    /// <summary>
    /// Seleciona e ordena as perguntas de uma rodada. Isolado de <see cref="GameSession"/>
    /// para que a regra de sorteio possa mudar sem tocar na maquina de estados.
    /// </summary>
    public static class QuestionDeck
    {
        /// <summary>
        /// Devolve ate <paramref name="count"/> perguntas. Com <paramref name="shuffle"/> ativo
        /// usa Fisher-Yates sobre uma copia, preservando a lista de origem.
        /// </summary>
        public static List<Question> Draw(IReadOnlyList<Question> source, int count, bool shuffle, int seed = 0)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var pool = new List<Question>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null) pool.Add(source[i]);
            }

            if (shuffle)
            {
                var rng = seed == 0 ? new Random() : new Random(seed);
                for (int i = pool.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (pool[i], pool[j]) = (pool[j], pool[i]);
                }
            }

            if (count > 0 && count < pool.Count)
            {
                pool.RemoveRange(count, pool.Count - count);
            }

            return pool;
        }
    }
}
