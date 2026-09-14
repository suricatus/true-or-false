# Montagem da cena

Quatro painéis, quatro scripts, um maestro. Os painéis só desenham e reportam toques;
quem decide o que acontece é o `GameRunner`.

Ative os painéis temporariamente para arrastar as referências — qual deles está ativo
ao salvar a cena não importa, o `GameRunner` assume o controle no Play.

## 1. GameRunner

Crie um GameObject vazio na raiz da cena chamado `GameRunner` e adicione o componente
`GameRunner`.

| Campo | Arraste |
|---|---|
| Config | `_Clients/_Template/GameConfig` |
| Theme | `_Clients/_Template/Theme` |
| Attract Panel | `Canvas/AtracaoPanel` |
| Question Panel | `Canvas/PerguntasPanel` |
| Feedback Panel | `Canvas/FeedbackPanel` |
| Result Panel | `Canvas/ResultadoPanel` |
| Audio Source | opcional, um `AudioSource` qualquer da cena |

## 2. AtracaoPanel → `AttractPanelView`

O script vai **no próprio AtracaoPanel** (que já ocupa a tela inteira), não em um filho.
É isso que faz qualquer toque iniciar o jogo.

| Campo | Arraste |
|---|---|
| Background | `AtracaoPanel/Background` |
| Logo | `AtracaoPanel/Logo` |
| Title Text | `AtracaoPanel/TitleText` |
| Call To Action Text | `AtracaoPanel/StartText` |

## 3. PerguntasPanel → `QuestionPanelView`

| Campo | Arraste |
|---|---|
| Background | `PerguntasPanel/Background` |
| Logo | `PerguntasPanel/Logo` |
| Timer Fill | `PerguntasPanel/Timer` (a Image com Image Type = Filled) |
| Timer Text | `PerguntasPanel/Timer/TimerText` |
| Timer Root | `PerguntasPanel/Timer` |
| Timer Pulse Target | opcional — o que pulsa a cada segundo; vazio = `Timer Root` (intensidade no tema, em *Pulsacao do cronometro*) |
| Score Text | `PerguntasPanel/Score/TimerText` |
| Question Text | `PerguntasPanel/QuestionContainer/TitleText` |
| Question Image | opcional — só se você for usar imagem por pergunta |
| True Button → Button | `ButtonsContainer/TrueButton` |
| True Button → Image | a `Image` do próprio `TrueButton` |
| True Button → Label | `TrueButton/Text (TMP)` |
| False Button → Button | `ButtonsContainer/FalseButton` |
| False Button → Image | a `Image` do próprio `FalseButton` |
| False Button → Label | `FalseButton/Text (TMP)` |

> O objeto `Score` tem um filho chamado `TimerText` (herança de copiar e colar).
> É ele mesmo que recebe a pontuação — vale renomear para `ScoreText` para não confundir depois.

## 4. FeedbackPanel → `FeedbackPanelView`

| Campo | Arraste |
|---|---|
| Background | `FeedbackPanel/Background` |
| Title Text | `FeedbackPanel/FeedbackContainer/TitleText` — recebe MANDOU BEM! / QUE PENA VOCÊ ERROU |
| Explanation Text | `FeedbackPanel/FeedbackContainerText/TitleText` — recebe a curiosidade |
| Mood Image | **criar**: uma `Image` nova para o sprite de felicidade/tristeza |
| Score Text | `FeedbackPanel/Score/TimerText` |
| Points Delta Text | **criar** (opcional): um TMP para o `+100 PONTOS!` |
| Continue Text | `FeedbackPanel/ContinueText` |

## 5. ResultadoPanel → `ResultPanelView`

| Campo | Arraste |
|---|---|
| Background | `ResultadoPanel/Background` |
| Title Text | `ResultadoPanel/FeedbackContainer/TitleText` — O SEU RESULTADO FOI DE... |
| Score Text | `ResultadoPanel/PointsContainer/TitleText` — recebe a cor da faixa |
| Message Text | **criar**: um TMP para a frase da faixa |
| Tier Image | **criar**: uma `Image` para o sprite da faixa |
| Home Button | `ResultadoPanel/HomeButton` |

O logo não aparece nestas duas telas, e o botão Home não tem texto vindo do tema —
o que estiver dentro dele na cena (ícone ou label fixo) é o que vai aparecer.

## O que apagar da cena

Sobraram na cena dois objetos que nenhum script usa mais:
`FeedbackPanel/Logo` e `ResultadoPanel/Logo`.

## O que falta criar na cena

Três objetos que ainda não existem, todos opcionais no código (o jogo roda sem eles):

1. `FeedbackPanel` → uma **Image** para o sprite de felicidade/tristeza.
2. `ResultadoPanel` → um **TMP** para a frase da faixa e uma **Image** para o sprite da faixa.
3. `FeedbackPanel` → um **TMP** para o `+100 PONTOS!`, se você quiser mostrar o ganho separado do total.

---

# O fluxo, do toque ao resultado

```
AtracaoPanel   toque em qualquer lugar
     ↓
PerguntasPanel  cronômetro roda, jogador responde
     ↓          botão escolhido fica destacado         (suspenseSeconds, 2s)
     ↓          botão da resposta CERTA pisca verde    (revealSeconds)
FeedbackPanel   curiosidade + frase + sprite + pontos contando
     ↓          toque para continuar
     └─ mais perguntas? volta para PerguntasPanel
        acabou?         vai para ResultadoPanel
     ↓
ResultadoPanel  pontuação colorida pela faixa + frase + sprite
                botão Home, ou volta sozinho após idleResetSeconds
```

O ritmo inteiro está no `GameConfig`, não no código: `suspenseSeconds`, `revealSeconds`,
`revealBlinkInterval`, `scoreCountSeconds` e `feedbackAutoAdvanceSeconds`
(deixe 0 para esperar o toque, ou coloque segundos para o painel avançar sozinho).

# Faixas de pontuação

As três faixas do resultado (vermelho / amarelo / verde) são a lista **Tiers** no `Theme`,
não estão no código. Cada faixa tem cor, sprite e frase própria, e o jogo escolhe a faixa
de maior `Min Score` que caiba na pontuação final.

Os cortes padrão são 0, 301 e 601. Para mudar, altere o `Min Score` — e se um cliente
quiser cinco faixas em vez de três, é só adicionar itens na lista.

Na frase, `{0}` é substituído pela pontuação. Com `Color Score Inside Message` ligado,
essa pontuação sai pintada com a cor da faixa.
