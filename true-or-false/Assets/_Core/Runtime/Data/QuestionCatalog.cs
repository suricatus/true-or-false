using System;
using System.Collections.Generic;

namespace Suricatus.TrueOrFalse.Core
{
    /// <summary>
    /// Formato do arquivo de conteudo entregue ao cliente (StreamingAssets/Suricatus/questions.json).
    /// </summary>
    [Serializable]
    public class QuestionCatalog
    {
        public string schemaVersion = "1";
        public string clientName;
        public List<Question> questions = new List<Question>();
    }
}
