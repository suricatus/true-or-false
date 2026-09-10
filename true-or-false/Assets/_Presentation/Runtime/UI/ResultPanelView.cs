using Suricatus.TrueOrFalse.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Suricatus.TrueOrFalse.Presentation
{
    /// <summary>
    /// Tela final: pontuacao total, faixa de desempenho (cor, sprite e frase) e botao de voltar ao inicio.
    ///
    /// As faixas nao estao no codigo: sao a lista "Tiers" do <see cref="ThemeConfig"/>,
    /// entao cada cliente escolhe quantas quer e onde ficam os cortes.
    /// </summary>
    public class ResultPanelView : PanelView
    {
        [Header("Resultado")]
        [Tooltip("Titulo fixo: O SEU RESULTADO FOI DE...")]
        [SerializeField] private TMP_Text titleText;

        [Tooltip("Destaque da pontuacao, pintado com a cor da faixa.")]
        [SerializeField] private TMP_Text scoreText;

        [Tooltip("Frase da faixa de desempenho.")]
        [SerializeField] private TMP_Text messageText;

        [Tooltip("Image do sprite da faixa de desempenho.")]
        [SerializeField] private Image tierImage;

        [Header("Voltar ao inicio")]
        [SerializeField] private Button homeButton;

        protected override void Initialize()
        {
            if (homeButton != null) homeButton.onClick.AddListener(OnHomeClicked);
        }

        private void OnDestroy()
        {
            if (homeButton != null) homeButton.onClick.RemoveListener(OnHomeClicked);
        }

        protected override void ApplyTheme()
        {
            base.ApplyTheme();
            if (Theme == null) return;

            SetSprite(background, Theme.result.background);
            SetText(titleText, Theme.result.title);
        }

        public void Show(RoundResult result)
        {
            Show();
            if (Theme == null) return;

            var theme = Theme.result;
            var tier = theme.Resolve(result.Score);
            Color scoreColor = tier != null ? tier.scoreColor : Color.white;

            if (scoreText != null)
            {
                scoreText.text = string.Format(theme.scoreFormat, result.Score);
                scoreText.color = scoreColor;
            }

            if (messageText != null && tier != null)
            {
                string score = theme.colorScoreInsideMessage
                    ? $"<color=#{ColorUtility.ToHtmlStringRGB(scoreColor)}>{result.Score}</color>"
                    : result.Score.ToString();
                messageText.text = string.Format(tier.message, score);
            }

            if (tierImage != null)
            {
                Sprite sprite = tier != null ? tier.sprite : null;
                tierImage.sprite = sprite;
                tierImage.enabled = sprite != null;
            }
        }

        private void OnHomeClicked() => Runner?.GoHome();
    }
}
