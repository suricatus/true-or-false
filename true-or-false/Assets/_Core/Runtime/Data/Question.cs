using System;

namespace Suricatus.TrueOrFalse.Core
{
    /// <summary>
    /// Uma afirmacao que o jogador julga como verdadeira ou falsa.
    /// Os nomes dos campos sao o contrato do JSON de conteudo do cliente:
    /// renomear qualquer um deles quebra os arquivos ja entregues.
    /// </summary>
    [Serializable]
    public class Question
    {
        public string id;
        public string statement;
        public bool isTrue;

        /// <summary>Texto opcional exibido no feedback, apos a resposta.</summary>
        public string explanation;

        /// <summary>Rotulo livre para agrupar/filtrar perguntas (ex.: "Seguranca", "Onboarding").</summary>
        public string category;

        /// <summary>Nome do sprite opcional que acompanha a pergunta, resolvido pelo tema.</summary>
        public string imageKey;
    }
}
