using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Suricatus.TrueOrFalse.Presentation
{
    /// <summary>
    /// Tela de escolha do assunto, entre a atracao e a primeira pergunta.
    ///
    /// Os botoes NAO sao montados um a um na cena: o painel recebe do
    /// <see cref="GameRunner"/> a lista de assuntos que existe no catalogo carregado e clona
    /// um botao para cada um, a partir de um modelo. Trocar o JSON do cliente por outro com
    /// mais (ou menos) assuntos nao exige mexer na cena nem no codigo.
    ///
    /// O modelo fica DENTRO do container, montado do jeito que o botao deve aparecer.
    /// Este script o desativa na inicializacao e usa apenas como molde.
    /// </summary>
    public class TopicSelectPanelView : PanelView
    {
        [Header("Marca")]
        [Tooltip("Image do logo do cliente. Opcional.")]
        [SerializeField] private Image logo;

        [Header("Textos")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text callToActionText;

        [Header("Botoes")]
        [Tooltip("Objeto que recebe os botoes gerados. Costuma ter um Vertical Layout Group " +
                 "ou um Grid Layout Group para organizar sozinho.")]
        [SerializeField] private RectTransform buttonsContainer;

        [Tooltip("Botao usado como MODELO. Monte um botao do jeito que ele deve aparecer, deixe-o " +
                 "dentro do container e arraste aqui. O jogo clona um por assunto e esconde o modelo.")]
        [SerializeField] private Button buttonTemplate;

        [Tooltip("Nome do objeto filho do botao que recebe o icone do assunto. " +
                 "Deixe vazio, ou sem filho com esse nome, se os botoes nao tem icone.")]
        [SerializeField] private string iconChildName = "Icon";

        /// <summary>Botoes gerados na ultima montagem. Destruidos e refeitos a cada exibicao.</summary>
        private readonly List<GameObject> _spawned = new List<GameObject>();

        protected override void Initialize()
        {
            // O modelo nunca aparece em jogo: ele so existe para ser clonado.
            if (buttonTemplate != null) buttonTemplate.gameObject.SetActive(false);
            else Debug.LogError("[TrueOrFalse] TopicSelectPanelView sem 'Button Template'. " +
                                "A tela de assuntos vai aparecer vazia.", this);
        }

        protected override void ApplyTheme()
        {
            base.ApplyTheme();
            if (Theme == null) return;

            SetSprite(logo, Theme.logo);
            SetSprite(background, Theme.topics.background);
            SetText(titleText, Theme.topics.title);
            SetText(callToActionText, Theme.topics.callToAction);
        }

        /// <summary>
        /// Monta um botao por assunto. Chamado pelo <see cref="GameRunner"/> a cada entrada na tela,
        /// e nao uma unica vez, porque o catalogo em uso pode ter sido trocado na pasta externa.
        /// </summary>
        public void ShowTopics(IReadOnlyList<string> topics)
        {
            Clear();

            if (buttonTemplate == null || topics == null) return;

            var parent = buttonsContainer != null ? buttonsContainer : (RectTransform)buttonTemplate.transform.parent;

            for (int i = 0; i < topics.Count; i++)
            {
                string topic = topics[i];
                if (string.IsNullOrWhiteSpace(topic)) continue;
                Spawn(parent, topic, Label(topic), Style(topic));
            }

            // Botao opcional que joga com o catalogo inteiro. Vai por ultimo, depois dos assuntos.
            if (Theme != null && Theme.topics.showAllTopics)
                Spawn(parent, null, Theme.topics.allTopicsLabel, null);
        }

        public override void Hide()
        {
            // Evita deixar botoes de um catalogo antigo pendurados na cena.
            Clear();
            base.Hide();
        }

        // ---------------------------------------------------------------- montagem

        /// <param name="topic">Assunto do botao. Null significa "todos os assuntos".</param>
        private void Spawn(RectTransform parent, string topic, string label, ThemeConfig.TopicStyle style)
        {
            var clone = Instantiate(buttonTemplate.gameObject, parent);
            clone.name = topic != null ? $"AssuntoButton ({topic})" : "AssuntoButton (todos)";
            clone.SetActive(true);
            _spawned.Add(clone);

            // O primeiro TMP_Text do clone e o rotulo. Basta o modelo ter um texto dentro.
            var text = clone.GetComponentInChildren<TMP_Text>(true);
            if (text != null && !string.IsNullOrEmpty(label)) text.text = label;

            var button = clone.GetComponent<Button>();
            if (button != null)
            {
                string captured = topic;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => Runner?.StartRound(captured));

                // Alfa 0 no tema significa "mantem a cor que veio do modelo".
                if (style != null && style.color.a > 0f && button.targetGraphic != null)
                    button.targetGraphic.color = style.color;
            }

            if (style != null && style.sprite != null) SetIcon(clone, style.sprite);
        }

        private void SetIcon(GameObject clone, Sprite sprite)
        {
            if (string.IsNullOrWhiteSpace(iconChildName)) return;

            var children = clone.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] == clone.transform) continue;
                if (children[i].name != iconChildName) continue;

                var image = children[i].GetComponent<Image>();
                if (image == null) continue;

                image.sprite = sprite;
                image.enabled = true;
                return;
            }
        }

        private void Clear()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null) Destroy(_spawned[i]);
            }
            _spawned.Clear();
        }

        // ---------------------------------------------------------------- tema

        private ThemeConfig.TopicStyle Style(string topic)
            => Theme != null ? Theme.topics.Resolve(topic) : null;

        /// <summary>Nome exibido. Sem item no tema, o botao mostra a propria categoria do JSON.</summary>
        private string Label(string topic)
        {
            var style = Style(topic);
            return style != null && !string.IsNullOrWhiteSpace(style.label) ? style.label : topic;
        }
    }
}
