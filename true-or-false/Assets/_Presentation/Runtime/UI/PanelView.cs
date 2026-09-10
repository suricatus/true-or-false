using UnityEngine;
using UnityEngine.UI;

namespace Suricatus.TrueOrFalse.Presentation
{
    /// <summary>
    /// Base das quatro telas. Cada painel so desenha e reporta toques:
    /// quem decide o que acontece e o <see cref="GameRunner"/>.
    /// </summary>
    public abstract class PanelView : MonoBehaviour
    {
        [Header("Comuns")]
        [Tooltip("Image de fundo do painel. Opcional.")]
        [SerializeField] protected Image background;

        protected GameRunner Runner { get; private set; }
        protected ThemeConfig Theme => Runner != null ? Runner.Theme : null;

        private bool _initialized;

        /// <summary>
        /// Liga o painel ao runner e o inicializa.
        ///
        /// A inicializacao mora aqui, e nao em Awake, porque tres dos quatro paineis comecam
        /// desativados na cena — e o Awake de um objeto desativado nunca roda.
        /// </summary>
        public void Bind(GameRunner runner)
        {
            Runner = runner;

            if (!_initialized)
            {
                _initialized = true;
                Initialize();
            }

            ApplyTheme();
        }

        /// <summary>Cache de referencias e assinatura de eventos. Roda uma unica vez.</summary>
        protected virtual void Initialize() { }

        /// <summary>
        /// Aplica os assets do tema. Chamado uma vez, na inicializacao.
        ///
        /// O logo nao vive aqui: so as telas que realmente exibem a marca declaram esse campo.
        /// </summary>
        protected virtual void ApplyTheme() { }

        public virtual void Show() => gameObject.SetActive(true);
        public virtual void Hide() => gameObject.SetActive(false);

        /// <summary>Aplica o sprite so quando existem imagem e sprite; nunca apaga o que ja esta na cena.</summary>
        protected static void SetSprite(Image image, Sprite sprite)
        {
            if (image == null || sprite == null) return;
            image.sprite = sprite;
        }

        /// <summary>Escreve no texto se ele existir. Textos vazios do tema nao apagam o que esta na cena.</summary>
        protected static void SetText(TMPro.TMP_Text label, string value)
        {
            if (label == null || string.IsNullOrEmpty(value)) return;
            label.text = value;
        }

        /// <summary>
        /// Garante que o painel receba toque em qualquer ponto da sua area.
        /// Se nao houver nenhum Graphic no objeto, cria uma Image transparente para captar o raycast.
        /// </summary>
        protected void EnsureTouchArea()
        {
            var graphic = GetComponent<Graphic>();
            if (graphic == null)
            {
                var image = gameObject.AddComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0f);
                graphic = image;
            }

            graphic.raycastTarget = true;
        }
    }
}
