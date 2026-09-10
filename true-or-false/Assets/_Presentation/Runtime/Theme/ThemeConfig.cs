using System;
using TMPro;
using UnityEngine;

namespace Suricatus.TrueOrFalse.Presentation
{
    /// <summary>
    /// Identidade visual e textos de um cliente. Duplique em Assets/_Clients/&lt;Cliente&gt;/
    /// e troque os assets no Inspector. O nucleo do jogo nao conhece este arquivo.
    /// </summary>
    [CreateAssetMenu(menuName = "Suricatus/True or False/Theme", fileName = "Theme")]
    public class ThemeConfig : ScriptableObject
    {
        [Header("Marca")]
        public string clientName = "Suricatus";
        public Sprite logo;
        public Sprite background;

        [Header("Cores")]
        public Color primary = new Color(0.10f, 0.45f, 0.90f);
        public Color secondary = new Color(0.95f, 0.60f, 0.10f);
        public Color surface = Color.white;
        public Color textOnSurface = new Color(0.10f, 0.10f, 0.12f);
        public Color correct = new Color(0.15f, 0.70f, 0.35f);
        public Color wrong = new Color(0.85f, 0.20f, 0.25f);

        [Header("Tipografia")]
        public TMP_FontAsset titleFont;
        public TMP_FontAsset bodyFont;

        [Header("Botoes de resposta")]
        public Sprite trueButtonSprite;
        public Sprite falseButtonSprite;
        [Tooltip("Rotulos dos botoes. Trocar para IDIOMA ou linguagem do cliente (ex.: Fato / Fake).")]
        public string trueLabel = "VERDADEIRO";
        public string falseLabel = "FALSO";

        [Header("Textos de tela")]
        public string attractTitle = "VERDADEIRO OU FALSO?";
        public string attractCallToAction = "Toque para comecar";
        public string correctFeedback = "Acertou!";
        public string wrongFeedback = "Errou!";
        public string timeoutFeedback = "Tempo esgotado!";
        public string resultTitle = "Sua pontuacao";
        public string playAgainLabel = "Jogar de novo";

        [Header("Audio")]
        public AudioClip correctSfx;
        public AudioClip wrongSfx;
        public AudioClip timeoutSfx;
        public AudioClip tickSfx;
        public AudioClip music;

        [Header("Imagens de pergunta")]
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

        [Serializable]
        public struct NamedSprite
        {
            public string key;
            public Sprite sprite;
        }
    }
}
