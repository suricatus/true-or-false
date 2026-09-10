using System.Collections;
using Suricatus.TrueOrFalse.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Suricatus.TrueOrFalse.Presentation
{
    /// <summary>
    /// Tela de feedback: diz se acertou, mostra a curiosidade da pergunta e anima a pontuacao.
    /// Um toque em qualquer ponto avanca para a proxima pergunta.
    /// </summary>
    public class FeedbackPanelView : PanelView, IPointerClickHandler
    {
        [Header("Feedback")]
        [Tooltip("Frase de acerto/erro: MANDOU BEM! / QUE PENA VOCE ERROU.")]
        [SerializeField] private TMP_Text titleText;

        [Tooltip("Curiosidade da pergunta, vinda do campo 'explanation' do JSON.")]
        [SerializeField] private TMP_Text explanationText;

        [Tooltip("Image do sprite de felicidade/tristeza.")]
        [SerializeField] private Image moodImage;

        [Header("Pontuacao")]
        [Tooltip("Pontuacao total, contada de forma animada.")]
        [SerializeField] private TMP_Text scoreText;

        [Tooltip("Pontos ganhos ou perdidos nesta pergunta (+100). Opcional.")]
        [SerializeField] private TMP_Text pointsDeltaText;

        [Header("Continuar")]
        [SerializeField] private TMP_Text continueText;

        private Coroutine _count;
        private int _targetScore;
        private bool _counting;

        protected override void Initialize() => EnsureTouchArea();

        protected override void ApplyTheme()
        {
            base.ApplyTheme();
            if (Theme == null) return;

            SetSprite(background, Theme.feedback.background);
            SetText(continueText, Theme.feedback.continueLabel);
        }

        /// <summary>Monta a tela para o resultado de uma pergunta.</summary>
        public void Show(AnswerOutcome outcome, float countSeconds)
        {
            Show();
            if (Theme == null) return;

            var theme = Theme.feedback;
            bool correct = outcome.Verdict == AnswerVerdict.Correct;
            bool timedOut = outcome.Verdict == AnswerVerdict.TimedOut;

            // Tempo esgotado reaproveita o visual de erro quando nao tem o proprio.
            string title = timedOut && !string.IsNullOrEmpty(theme.timeoutTitle)
                ? theme.timeoutTitle
                : correct ? theme.correctTitle : theme.wrongTitle;

            Sprite mood = correct
                ? theme.correctSprite
                : timedOut && theme.timeoutSprite != null ? theme.timeoutSprite : theme.wrongSprite;

            if (titleText != null)
            {
                titleText.text = title;
                titleText.color = correct ? theme.correctColor : theme.wrongColor;
            }

            if (moodImage != null)
            {
                moodImage.sprite = mood;
                moodImage.enabled = mood != null;
            }

            // A curiosidade aparece SEMPRE: acerto, erro ou tempo esgotado.
            // Quem errou precisa saber o porque; quem acertou merece a confirmacao.
            if (explanationText != null)
            {
                string explanation = outcome.Question != null ? outcome.Question.explanation : null;
                if (string.IsNullOrWhiteSpace(explanation)) explanation = theme.emptyExplanationFallback;
                explanationText.text = explanation;
                explanationText.enabled = !string.IsNullOrWhiteSpace(explanation);
            }

            if (pointsDeltaText != null)
            {
                int points = outcome.PointsAwarded;
                pointsDeltaText.text = points > 0 ? string.Format(theme.pointsGainedFormat, points)
                    : points < 0 ? string.Format(theme.pointsLostFormat, Mathf.Abs(points))
                    : theme.noPointsText;
                pointsDeltaText.color = points > 0 ? theme.correctColor
                    : points < 0 ? theme.wrongColor
                    : (titleText != null ? titleText.color : Color.white);
            }

            _targetScore = outcome.ScoreAfter;
            int from = outcome.ScoreAfter - outcome.PointsAwarded;

            if (_count != null) StopCoroutine(_count);
            _count = StartCoroutine(CountScore(from, _targetScore, countSeconds));
        }

        private IEnumerator CountScore(int from, int to, float seconds)
        {
            _counting = true;

            if (scoreText != null && seconds > 0f && from != to)
            {
                float elapsed = 0f;
                while (elapsed < seconds)
                {
                    elapsed += Time.deltaTime;
                    int value = Mathf.RoundToInt(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / seconds)));
                    WriteScore(value);
                    yield return null;
                }
            }

            WriteScore(to);
            _counting = false;
            _count = null;
        }

        private void WriteScore(int value)
        {
            if (scoreText == null || Theme == null) return;
            scoreText.text = string.Format(Theme.question.scoreFormat, value);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // O primeiro toque durante a contagem apenas a conclui: ninguem perde
            // a informacao de quantos pontos fez por ter tocado rapido demais.
            if (_counting)
            {
                if (_count != null) StopCoroutine(_count);
                WriteScore(_targetScore);
                _counting = false;
                _count = null;
                return;
            }

            Runner?.Continue();
        }
    }
}
