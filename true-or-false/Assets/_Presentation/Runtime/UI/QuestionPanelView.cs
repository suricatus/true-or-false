using System;
using System.Collections;
using Suricatus.TrueOrFalse.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Suricatus.TrueOrFalse.Presentation
{
    /// <summary>
    /// Tela da pergunta: cronometro, pontuacao, enunciado e os dois botoes de resposta.
    ///
    /// Depois que o jogador responde, esta tela assume o controle das cores dos botoes
    /// (o Button volta a transicao original quando a proxima pergunta aparece), para que
    /// o destaque da escolha nao se perca quando o dedo sai da tela.
    /// </summary>
    public class QuestionPanelView : PanelView
    {
        [Header("Marca")]
        [Tooltip("Image do logo do cliente. Opcional.")]
        [SerializeField] private Image logo;

        [Header("Cronometro")]
        [Tooltip("Image com Image Type = Filled. Esvazia conforme o tempo passa.")]
        [SerializeField] private Image timerFill;
        [SerializeField] private TMP_Text timerText;
        [Tooltip("Objeto do cronometro inteiro, escondido quando a rodada nao tem tempo.")]
        [SerializeField] private GameObject timerRoot;

        [Header("Pontuacao")]
        [SerializeField] private TMP_Text scoreText;

        [Header("Pergunta")]
        [SerializeField] private TMP_Text questionText;
        [Tooltip("Image DEDICADA a ilustracao da pergunta, resolvida pelo campo 'imageKey' do JSON. " +
                 "Deixe vazio se as perguntas nao tem imagem. Nao arraste aqui o container do texto.")]
        [SerializeField] private Image questionImage;

        [Header("Respostas")]
        [SerializeField] private AnswerButton trueButton = new AnswerButton();
        [SerializeField] private AnswerButton falseButton = new AnswerButton();

        private Coroutine _blink;

        protected override void Initialize()
        {
            trueButton.Cache();
            falseButton.Cache();

            if (trueButton.button != null) trueButton.button.onClick.AddListener(OnTrueClicked);
            if (falseButton.button != null) falseButton.button.onClick.AddListener(OnFalseClicked);
        }

        private void OnDestroy()
        {
            if (trueButton.button != null) trueButton.button.onClick.RemoveListener(OnTrueClicked);
            if (falseButton.button != null) falseButton.button.onClick.RemoveListener(OnFalseClicked);
        }

        protected override void ApplyTheme()
        {
            base.ApplyTheme();
            if (Theme == null) return;

            SetSprite(logo, Theme.logo);
            SetSprite(background, Theme.question.background);
            SetText(trueButton.label, Theme.question.trueLabel);
            SetText(falseButton.label, Theme.question.falseLabel);

            // O tema manda nas cores: alinhar o ColorBlock do Button evita que ele
            // repinte por cima do que este script pinta.
            trueButton.ApplyTheme(Theme.question);
            falseButton.ApplyTheme(Theme.question);
        }

        private void OnTrueClicked() => Runner?.Answer(true);
        private void OnFalseClicked() => Runner?.Answer(false);

        /// <summary>Prepara a tela para uma pergunta nova: botoes destravados e no estado de repouso.</summary>
        public void ShowQuestion(Question question, int score)
        {
            if (Theme == null) return;
            StopBlink();

            var theme = Theme.question;
            trueButton.Unlock(theme);
            falseButton.Unlock(theme);

            SetText(questionText, question != null ? question.statement : string.Empty);
            UpdateScore(score);

            if (questionImage != null)
            {
                // Desliga so o desenho da Image, nunca o GameObject: se este campo for ligado
                // por engano a um container, desativa-lo levaria junto o texto da pergunta.
                var sprite = Runner != null ? Runner.ResolveQuestionImage(question) : null;
                questionImage.sprite = sprite;
                questionImage.enabled = sprite != null;
            }

            if (timerRoot != null) timerRoot.SetActive(Runner == null || !Runner.Session.IsUntimed);
        }

        public void UpdateScore(int score)
        {
            if (scoreText == null || Theme == null) return;
            scoreText.text = string.Format(Theme.question.scoreFormat, score);
        }

        /// <summary>Atualiza o preenchimento e o texto do cronometro.</summary>
        public void UpdateTimer(float secondsLeft, float secondsTotal)
        {
            if (Theme == null) return;
            var theme = Theme.question;

            if (timerFill != null)
            {
                timerFill.fillAmount = secondsTotal <= 0f ? 1f : Mathf.Clamp01(secondsLeft / secondsTotal);
            }

            if (timerText != null)
            {
                // Arredonda para cima: o jogador ve "1" enquanto ainda ha tempo, e nunca "0" jogavel.
                int display = Mathf.Max(0, Mathf.CeilToInt(secondsLeft));
                timerText.text = string.Format(theme.timerFormat, display);
                timerText.color = secondsLeft <= theme.urgentThresholdSeconds
                    ? theme.timerUrgentColor
                    : theme.timerNormalColor;
            }
        }

        /// <summary>
        /// Trava os botoes e mantem destacado o que o jogador escolheu, durante o suspense.
        /// Em tempo esgotado nenhum botao fica destacado.
        /// </summary>
        public void LockAndHighlight(AnswerOutcome outcome)
        {
            if (Theme == null) return;
            var theme = Theme.question;
            trueButton.Lock(theme);
            falseButton.Lock(theme);

            if (outcome.Verdict == AnswerVerdict.TimedOut) return;

            var chosen = outcome.PlayerAnswer ? trueButton : falseButton;
            chosen.SetSelected(theme);
        }

        /// <summary>Pisca o botao da resposta CERTA, seja ela a escolhida ou nao.</summary>
        public IEnumerator RevealCorrect(bool correctIsTrue, float seconds, float blinkInterval)
        {
            if (Theme == null) yield break;
            StopBlink();
            var target = correctIsTrue ? trueButton : falseButton;
            _blink = StartCoroutine(BlinkRoutine(target, seconds, blinkInterval));
            yield return _blink;
        }

        private IEnumerator BlinkRoutine(AnswerButton target, float seconds, float blinkInterval)
        {
            var theme = Theme.question;
            float elapsed = 0f;
            bool on = false;
            float interval = Mathf.Max(0.02f, blinkInterval);

            while (elapsed < seconds)
            {
                on = !on;
                if (on) target.SetReveal(theme);
                else target.SetIdle(theme);

                float step = Mathf.Min(interval, seconds - elapsed);
                elapsed += step;
                yield return new WaitForSeconds(step);
            }

            // Termina aceso, para que a resposta certa fique legivel ate a troca de tela.
            target.SetReveal(theme);
            _blink = null;
        }

        private void StopBlink()
        {
            if (_blink == null) return;
            StopCoroutine(_blink);
            _blink = null;
        }

        // ------------------------------------------------------------------

        [Serializable]
        public class AnswerButton
        {
            public Button button;

            [Tooltip("Image pintada nas trocas de estado. Se vazio, usa o Target Graphic do Button. " +
                     "Num prefab em camadas (Outer/Shadow/Mask/Front), aponte para a Image da FRENTE: " +
                     "e ela que da a cor do botao.")]
            public Image image;

            [Tooltip("Texto do botao (VERDADEIRO / FALSO).")]
            public TMP_Text label;

            private Selectable.Transition _originalTransition;

            // Aparencia que veio do prefab. Sem isso, o repouso seria a cor do tema e os dois
            // botoes ficariam iguais — era assim que o verde e o vermelho do prefab viravam
            // um circulo branco em jogo.
            private Sprite _prefabSprite;
            private Color _prefabColor = Color.white;

            private bool _cached;

            public void Cache()
            {
                if (image == null && button != null) image = button.targetGraphic as Image;

                if (image != null)
                {
                    _prefabSprite = image.sprite;
                    _prefabColor = image.color;
                }

                if (button != null) _originalTransition = button.transition;
                _cached = true;
            }

            /// <summary>
            /// Alinha as cores do Button com as do tema, para que os dois nao disputem
            /// a mesma Image. Chamado uma vez, na inicializacao.
            ///
            /// No modo Prefab nao toca em nada: o ColorBlock que veio do prefab continua valendo.
            /// </summary>
            public void ApplyTheme(ThemeConfig.QuestionTheme theme)
            {
                if (button == null) return;
                if (theme.buttonVisuals != ThemeConfig.ButtonVisualSource.Theme) return;

                var colors = button.colors;
                colors.normalColor = theme.idleColor;
                colors.highlightedColor = theme.idleColor;
                colors.pressedColor = theme.selectedColor;
                colors.selectedColor = theme.selectedColor;
                colors.disabledColor = theme.idleColor;
                button.colors = colors;

                if (theme.idleSprite != null && image != null) image.sprite = theme.idleSprite;
            }

            /// <summary>Devolve o botao ao controle do Button (transicao original) e ao estado de repouso.</summary>
            public void Unlock(ThemeConfig.QuestionTheme theme)
            {
                SetIdle(theme);

                if (button == null) return;
                if (_cached) button.transition = _originalTransition;
                button.interactable = true;
            }

            /// <summary>Impede novos toques e passa o controle das cores para esta classe.</summary>
            public void Lock(ThemeConfig.QuestionTheme theme)
            {
                if (button != null)
                {
                    // Transition None em vez de interactable=false: assim o ColorBlock do Button
                    // nao sobrescreve com a cor de "desabilitado" o destaque que vamos pintar.
                    button.transition = Selectable.Transition.None;
                    button.interactable = false;
                }
                SetIdle(theme);
            }

            public void SetIdle(ThemeConfig.QuestionTheme theme)
                => Paint(IdleColor(theme), IdleSprite(theme));

            public void SetSelected(ThemeConfig.QuestionTheme theme)
                => Paint(SelectedColor(theme), FromPrefab(theme) ? _prefabSprite : theme.selectedSprite);

            public void SetReveal(ThemeConfig.QuestionTheme theme)
                => Paint(RevealColor(theme), FromPrefab(theme) ? _prefabSprite : theme.revealSprite);

            private static bool FromPrefab(ThemeConfig.QuestionTheme theme)
                => theme.buttonVisuals == ThemeConfig.ButtonVisualSource.Prefab;

            private Color IdleColor(ThemeConfig.QuestionTheme theme)
                => FromPrefab(theme) ? _prefabColor : theme.idleColor;

            private Sprite IdleSprite(ThemeConfig.QuestionTheme theme)
                => FromPrefab(theme) ? _prefabSprite : theme.idleSprite;

            // Os destaques saem da propria cor do botao: escurecer e clarear funcionam em
            // qualquer cor, enquanto uma cor fixa de revelacao desapareceria num botao que
            // ja e verde.
            private Color SelectedColor(ThemeConfig.QuestionTheme theme)
                => FromPrefab(theme) ? Shade(_prefabColor, 0f, theme.selectedDarken) : theme.selectedColor;

            private Color RevealColor(ThemeConfig.QuestionTheme theme)
                => FromPrefab(theme) ? Shade(_prefabColor, 1f, theme.revealBrighten) : theme.revealColor;

            /// <summary>Aproxima a cor do preto (target 0) ou do branco (target 1), preservando o alfa.</summary>
            private static Color Shade(Color color, float target, float amount)
            {
                float t = Mathf.Clamp01(amount);
                return new Color(
                    Mathf.Lerp(color.r, target, t),
                    Mathf.Lerp(color.g, target, t),
                    Mathf.Lerp(color.b, target, t),
                    color.a);
            }

            /// <summary>Pinta a Image. Sprite nulo nao apaga o que ja esta la.</summary>
            private void Paint(Color color, Sprite sprite)
            {
                if (image == null) return;
                image.color = color;
                if (sprite != null) image.sprite = sprite;
            }
        }
    }
}
