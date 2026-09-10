using System;
using TMPro;
using UnityEngine;

namespace Suricatus.TrueOrFalse.Presentation
{
    /// <summary>
    /// Identidade visual e TODOS os textos do jogo, por cliente.
    /// Duplique este asset em Assets/_Clients/&lt;Cliente&gt;/ e troque os valores no Inspector.
    /// Nenhum texto ou sprite exibido em tela deve estar escrito dentro de um script.
    ///
    /// Onde aparecer {0} no texto, o jogo substitui pela pontuacao.
    /// </summary>
    [CreateAssetMenu(menuName = "Suricatus/True or False/Theme", fileName = "Theme")]
    public class ThemeConfig : ScriptableObject
    {
        [Header("Marca")]
        public string clientName = "Suricatus";
        public Sprite logo;

        [Header("Tipografia (opcional)")]
        [Tooltip("Se preenchidas, sobrescrevem a fonte dos textos. Deixe vazio para manter o que esta na cena.")]
        public TMP_FontAsset titleFont;
        public TMP_FontAsset bodyFont;

        public AttractTheme attract = new AttractTheme();
        public QuestionTheme question = new QuestionTheme();
        public FeedbackTheme feedback = new FeedbackTheme();
        public ResultTheme result = new ResultTheme();

        [Header("Audio")]
        public AudioClip correctSfx;
        public AudioClip wrongSfx;
        public AudioClip timeoutSfx;
        public AudioClip tapSfx;

        // ------------------------------------------------------------------

        [Serializable]
        public class AttractTheme
        {
            public Sprite background;
            [TextArea] public string title = "VERDADEIRO OU FALSO?";
            [TextArea] public string callToAction = "TOQUE NA TELA PARA INICIAR";
        }

        [Serializable]
        public class QuestionTheme
        {
            public Sprite background;

            [Header("Botoes de resposta")]
            [Tooltip("Rotulos dos botoes. Troque conforme a linguagem do cliente (Fato/Fake, Mito/Verdade...).")]
            public string trueLabel = "VERDADEIRO";
            public string falseLabel = "FALSO";

            [Tooltip("Cor de repouso do botao.")]
            public Color idleColor = Color.white;
            [Tooltip("Cor do botao que o jogador escolheu, mantida durante o suspense.")]
            public Color selectedColor = new Color(0.85f, 0.85f, 0.85f);
            [Tooltip("Cor que pisca no botao da resposta CERTA, na revelacao.")]
            public Color revealColor = new Color(0.15f, 0.78f, 0.35f);

            [Tooltip("Sprites opcionais dos botoes. Se vazios, apenas a cor muda.")]
            public Sprite idleSprite;
            public Sprite selectedSprite;
            public Sprite revealSprite;

            [Header("Formatos de texto")]
            [Tooltip("{0} = pontuacao atual do jogador.")]
            public string scoreFormat = "{0}";
            [Tooltip("{0} = segundos restantes, ja arredondados.")]
            public string timerFormat = "{0}";

            [Header("Imagens por pergunta")]
            [Tooltip("Sprites resolvidos pelo campo 'imageKey' de cada pergunta do JSON.")]
            public NamedSprite[] questionImages = Array.Empty<NamedSprite>();

            public Sprite ResolveImage(string key)
            {
                if (string.IsNullOrEmpty(key) || questionImages == null) return null;
                for (int i = 0; i < questionImages.Length; i++)
                {
                    if (string.Equals(questionImages[i].key, key, StringComparison.OrdinalIgnoreCase))
                        return questionImages[i].sprite;
                }
                return null;
            }

            [Header("Cores do cronometro")]
            public Color timerNormalColor = Color.white;
            [Tooltip("Cor assumida quando o tempo entra na reta final.")]
            public Color timerUrgentColor = new Color(0.90f, 0.20f, 0.20f);
            [Tooltip("Segundos restantes a partir dos quais o cronometro fica urgente.")]
            [Min(0f)] public float urgentThresholdSeconds = 3f;
        }

        [Serializable]
        public class FeedbackTheme
        {
            public Sprite background;

            [Header("Acerto")]
            [TextArea] public string correctTitle = "MANDOU BEM!";
            [Tooltip("Sprite de felicidade.")]
            public Sprite correctSprite;
            public Color correctColor = new Color(0.15f, 0.78f, 0.35f);

            [Header("Erro")]
            [TextArea] public string wrongTitle = "QUE PENA VOCÊ ERROU";
            [Tooltip("Sprite de tristeza.")]
            public Sprite wrongSprite;
            public Color wrongColor = new Color(0.88f, 0.22f, 0.25f);

            [Header("Tempo esgotado")]
            [Tooltip("Deixe vazio para reaproveitar o texto de erro.")]
            [TextArea] public string timeoutTitle = "ACABOU O TEMPO!";
            [Tooltip("Deixe vazio para reaproveitar o sprite de erro.")]
            public Sprite timeoutSprite;

            [Header("Textos")]
            [Tooltip("{0} = pontos ganhos nesta pergunta.")]
            public string pointsGainedFormat = "+{0} PONTOS!";
            [Tooltip("{0} = pontos perdidos, sem o sinal de menos.")]
            public string pointsLostFormat = "-{0} PONTOS";
            [Tooltip("Exibido quando a pergunta nao soma nem tira pontos.")]
            public string noPointsText = "SEM PONTOS";
            [Tooltip("Usado quando a pergunta nao tem curiosidade cadastrada no JSON.")]
            [TextArea] public string emptyExplanationFallback = "";
            public string continueLabel = "TOQUE NA TELA PARA CONTINUAR";
        }

        [Serializable]
        public class ResultTheme
        {
            public Sprite background;
            [TextArea] public string title = "O SEU RESULTADO FOI DE...";

            [Tooltip("{0} = pontuacao final. Usado no destaque de pontos.")]
            public string scoreFormat = "{0} PONTOS!";

            [Tooltip("Pinta tambem a pontuacao que aparece dentro da frase, com a cor da faixa.")]
            public bool colorScoreInsideMessage = true;

            [Tooltip("Faixas de desempenho. O jogo usa a faixa de maior 'Min Score' que caiba na pontuacao final. " +
                     "Adicione ou remova faixas livremente: nao ha limite de tres.")]
            public ScoreTier[] tiers =
            {
                new ScoreTier
                {
                    name = "Baixa",
                    minScore = 0,
                    scoreColor = new Color(0.88f, 0.22f, 0.25f),
                    message = "{0} pontos! Volte para a fila e tente melhorar essa pontuação. Eu sei que você consegue!",
                },
                new ScoreTier
                {
                    name = "Media",
                    minScore = 301,
                    scoreColor = new Color(0.98f, 0.78f, 0.15f),
                    message = "{0} pontos! Foi quase, mas eu sei que você pode melhorar! Que tal tentar novamente?",
                },
                new ScoreTier
                {
                    name = "Alta",
                    minScore = 601,
                    scoreColor = new Color(0.15f, 0.78f, 0.35f),
                    message = "{0} PONTOS! Você entende muito e mandou muito bem! Parabéns!",
                },
            };

            /// <summary>Faixa correspondente a pontuacao, ou null se nenhuma faixa estiver cadastrada.</summary>
            public ScoreTier Resolve(int score)
            {
                if (tiers == null || tiers.Length == 0) return null;

                ScoreTier match = null;
                for (int i = 0; i < tiers.Length; i++)
                {
                    var tier = tiers[i];
                    if (tier == null || score < tier.minScore) continue;
                    if (match == null || tier.minScore >= match.minScore) match = tier;
                }

                // Pontuacao abaixo de todas as faixas cai na mais baixa cadastrada.
                if (match != null) return match;

                var lowest = tiers[0];
                for (int i = 1; i < tiers.Length; i++)
                {
                    if (tiers[i] != null && (lowest == null || tiers[i].minScore < lowest.minScore)) lowest = tiers[i];
                }
                return lowest;
            }
        }

        [Serializable]
        public struct NamedSprite
        {
            [Tooltip("Mesmo valor do campo 'imageKey' no JSON de perguntas.")]
            public string key;
            public Sprite sprite;
        }

        [Serializable]
        public class ScoreTier
        {
            [Tooltip("So para voce se achar no Inspector. Nao aparece no jogo.")]
            public string name = "Faixa";

            [Tooltip("Pontuacao minima para cair nesta faixa.")]
            public int minScore;

            [Tooltip("Cor da pontuacao nesta faixa.")]
            public Color scoreColor = Color.white;

            [Tooltip("Sprite exibido nesta faixa.")]
            public Sprite sprite;

            [Tooltip("Frase da faixa. {0} = pontuacao final, ja pintada com a cor acima.")]
            [TextArea(2, 5)] public string message = "{0} pontos!";
        }
    }
}
