using UnityEngine;

namespace Suricatus.TrueOrFalse.Core
{
    /// <summary>
    /// Regras da partida, por cliente. Duplique este asset em Assets/_Clients/&lt;Cliente&gt;/
    /// e ajuste os valores no Inspector: nenhuma dessas mudancas exige tocar em codigo.
    /// </summary>
    [CreateAssetMenu(menuName = "Suricatus/True or False/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Rodada")]
        [Tooltip("Quantas perguntas por partida. Use 0 para usar todas as perguntas do arquivo.")]
        [Min(0)] public int questionsPerRound = 10;

        [Tooltip("Tempo base de resposta por pergunta. 0 desliga o cronometro.")]
        [Min(0f)] public float secondsPerQuestion = 10f;

        [Tooltip("Segundos somados ao tempo base a cada 100 caracteres do enunciado, para dar folego " +
                 "as perguntas mais longas. 0 mantem o mesmo tempo para todas, como antes. " +
                 "Uma pergunta pode furar esta conta escrevendo 'seconds' na propria pergunta do JSON.")]
        [Min(0f)] public float extraSecondsPer100Characters = 0f;

        [Tooltip("Teto do tempo calculado pelo tamanho, para que um enunciado enorme nao trave a fila " +
                 "do totem. 0 desliga o teto. Nao limita o 'seconds' escrito na pergunta.")]
        [Min(0f)] public float maxSecondsPerQuestion = 0f;

        public bool shuffleQuestions = true;

        [Header("Ritmo da resposta")]
        [Tooltip("Suspense: quanto tempo o botao escolhido fica destacado antes de o jogo revelar a resposta.")]
        [Min(0f)] public float suspenseSeconds = 2f;

        [Tooltip("Duracao do piscar no botao da resposta certa.")]
        [Min(0f)] public float revealSeconds = 1.5f;

        [Tooltip("Intervalo de cada piscada. Menor = pisca mais rapido.")]
        [Min(0.02f)] public float revealBlinkInterval = 0.18f;

        [Tooltip("Segundos ate a tela de feedback avancar sozinha. 0 = espera o toque do jogador.")]
        [Min(0f)] public float feedbackAutoAdvanceSeconds = 0f;

        [Tooltip("Duracao da contagem animada da pontuacao na tela de feedback.")]
        [Min(0f)] public float scoreCountSeconds = 0.6f;

        [Header("Pontuacao")]
        public int pointsPerCorrect = 100;
        public int pointsPerWrong = 0;
        public int pointsPerTimeout = 0;

        [Tooltip("Pontos extras por segundo restante no acerto. 0 desliga o bonus de velocidade.")]
        [Min(0f)] public float bonusPointsPerSecondLeft = 0f;

        [Header("Totem")]
        [Tooltip("Segundos sem toque na tela de resultado antes de voltar sozinho para a atracao. 0 desliga.")]
        [Min(0f)] public float idleResetSeconds = 45f;

        [Header("Escolha de assunto")]
        [Tooltip("Liga a tela de escolha de assunto entre a atracao e a primeira pergunta. " +
                 "Os assuntos saem do campo 'category' das perguntas — nao ha lista a cadastrar. " +
                 "Desligado, o toque na atracao inicia a rodada com o catalogo inteiro, como antes.")]
        public bool selectTopicBeforeRound = false;

        [Tooltip("Minimo de perguntas para um assunto aparecer na tela de escolha. " +
                 "Evita oferecer um assunto com conteudo raso demais para uma rodada.")]
        [Min(1)] public int minQuestionsPerTopic = 1;

        [Header("Registro da acao")]
        [Tooltip("Nome deste totem nos CSVs de registro. Com varios totens rodando o MESMO APK, " +
                 "prefira batizar cada aparelho com um arquivo 'totem.txt' na pasta externa: o valor " +
                 "daqui sairia igual em todos. Vazio e o normal.")]
        public string totemId = "";

        [Tooltip("Endereco do aplicativo Web do Google Apps Script que grava na planilha. " +
                 "VAZIO desliga o envio para a nuvem, que e o padrao. O CSV local nao depende " +
                 "disto: a planilha serve para acompanhar a acao a distancia, e o relatorio " +
                 "final continua saindo dos arquivos.")]
        public string cloudEndpoint = "";

        [Tooltip("Senha combinada com o script da planilha. Impede que alguem que descubra o " +
                 "endereco escreva linhas falsas.")]
        public string cloudToken = "";

        [Header("Conteudo")]
        [Tooltip("Caminho do arquivo de perguntas, relativo a StreamingAssets.")]
        public string questionsFile = "Suricatus/questions.json";

        [Tooltip("Banco embutido usado se o arquivo externo faltar ou estiver invalido. O jogo nunca fica sem conteudo no evento.")]
        public TextAsset fallbackCatalog;

        [Header("Troca de conteudo no evento")]
        [Tooltip("Pasta gravavel consultada ANTES do arquivo do build. Deixe vazio para usar a pasta de " +
                 "dados do app (Application.persistentDataPath), que e sempre gravavel e nao pede permissao. " +
                 "E por aqui que se troca as perguntas no totem sem gerar um APK novo.")]
        public string externalContentFolder = "";

        [Tooltip("Grava um relatorio de carga na pasta externa, dizendo qual arquivo entrou em uso e por que. " +
                 "No totem e a unica forma de conferir a troca sem conectar um PC. Deixe ligado.")]
        public bool writeLoadReport = true;

        /// <summary>
        /// Converte para os parametros do nucleo. O tempo de feedback vai zerado de proposito:
        /// a coreografia (suspense, revelacao e tela de feedback) e responsabilidade da apresentacao,
        /// que avanca a rodada chamando ContinueFromFeedback.
        /// </summary>
        public RoundSettings ToRoundSettings() => new RoundSettings
        {
            questionsPerRound = questionsPerRound,
            secondsPerQuestion = secondsPerQuestion,
            extraSecondsPer100Characters = extraSecondsPer100Characters,
            maxSecondsPerQuestion = maxSecondsPerQuestion,
            feedbackSeconds = 0f,
            pointsPerCorrect = pointsPerCorrect,
            pointsPerWrong = pointsPerWrong,
            pointsPerTimeout = pointsPerTimeout,
            bonusPointsPerSecondLeft = bonusPointsPerSecondLeft,
            shuffleQuestions = shuffleQuestions,
        };
    }
}
