using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Suricatus.TrueOrFalse.Presentation
{
    /// <summary>
    /// Tela de atracao. Qualquer toque dentro do painel inicia a partida.
    ///
    /// Para o toque funcionar em toda a tela, este script precisa estar no AtracaoPanel
    /// (que cobre o Canvas inteiro), e nao em um filho menor.
    /// </summary>
    public class AttractPanelView : PanelView, IPointerClickHandler
    {
        [Header("Atracao")]
        [Tooltip("Image do logo do cliente. Opcional.")]
        [SerializeField] private UnityEngine.UI.Image logo;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text callToActionText;

        [Tooltip("Piscar do 'toque para iniciar'. 0 desliga.")]
        [SerializeField, Min(0f)] private float blinkSeconds = 1.2f;

        protected override void Initialize() => EnsureTouchArea();

        protected override void ApplyTheme()
        {
            base.ApplyTheme();
            if (Theme == null) return;

            SetSprite(logo, Theme.logo);
            SetSprite(background, Theme.attract.background);
            SetText(titleText, Theme.attract.title);
            SetText(callToActionText, Theme.attract.callToAction);
        }

        private void Update()
        {
            if (callToActionText == null || blinkSeconds <= 0f) return;

            // Respira a chamada para acao: em totem, texto parado nao convida ninguem.
            float t = Mathf.PingPong(Time.unscaledTime / blinkSeconds, 1f);
            var color = callToActionText.color;
            color.a = Mathf.Lerp(0.35f, 1f, t);
            callToActionText.color = color;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Runner == null) return;
            Runner.Begin();
        }
    }
}
