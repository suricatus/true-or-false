# Customização por cliente

Cada cliente é uma pasta aqui dentro (`_Clients/<Cliente>/`). O núcleo do jogo
(`Assets/_Core/`) **não é tocado** em nenhuma customização — se você precisou editar
um arquivo do `_Core`, é sinal de que uma regra nova deve virar configuração.

## Checklist de um cliente novo

1. Duplique `_Clients/_Template/` e renomeie para o nome do cliente.
2. **Regras** — abra o `GameConfig` da pasta e ajuste nº de perguntas, tempo,
   ritmo da resposta, pontuação e o timeout de inatividade do totem.
3. **Marca** — abra o `Theme` e troque logo, cores, fontes, sprites e **todos** os
   textos de tela, incluindo as faixas de pontuação do resultado.
4. **Conteúdo** — edite `Assets/StreamingAssets/<Cliente>/questions.json`.
5. No `GameRunner` da cena, troque `Config` e `Theme` pelos assets do cliente novo.
6. Rode os testes (`Window > General > Test Runner > EditMode`) antes de gerar o build.

Para saber qual objeto da cena vai em qual campo, veja
[`_Presentation/README.md`](../_Presentation/README.md).

## O arquivo de perguntas

Fica em `StreamingAssets/<Cliente>/questions.json` e vai **junto do build**, na pasta
`<Jogo>_Data/StreamingAssets/`. O cliente pode editá-lo no local do evento sem a Unity
e sem gerar build novo — basta reabrir o jogo.

| Campo | Obrigatório | Descrição |
|---|---|---|
| `id` | recomendado | Identificador da pergunta, usado nos relatórios |
| `statement` | **sim** | A afirmação exibida na tela |
| `isTrue` | **sim** | `true` ou `false` — a resposta correta |
| `explanation` | não | Texto exibido no feedback, depois da resposta |
| `category` | não | Rótulo livre para agrupar perguntas |
| `imageKey` | não | Nome de um sprite cadastrado em `Theme > Question Images` |
| `seconds` | não | Tempo só desta pergunta, em segundos. Ausente ou `0` deixa o jogo calcular pelo tamanho do enunciado |

Regras que o jogo aplica sozinho:

- Se o arquivo faltar, estiver com JSON quebrado ou sem nenhuma pergunta válida,
  o jogo cai no **banco embutido** definido em `GameConfig > Fallback Catalog`.
  Um totem em evento nunca fica sem conteúdo.
- Perguntas com `statement` vazio invalidam o arquivo; `id` repetido gera só um aviso.
- Se houver menos perguntas que o configurado na rodada, a partida fica mais curta
  em vez de repetir pergunta.
- `seconds` negativo invalida o arquivo, do mesmo jeito que um `statement` vazio.

### Tempo por pergunta

Enunciado longo pede mais tempo de leitura que um curto. Em vez de subir o tempo de
todas as perguntas para caber a maior, o `GameConfig` tem dois campos:

| Campo do `GameConfig` | O que faz |
|---|---|
| `Seconds Per Question` | Tempo base de qualquer pergunta. `0` desliga o cronômetro |
| `Extra Seconds Per 100 Characters` | Quanto somar ao tempo base a cada 100 caracteres do enunciado. `0` mantém o tempo igual para todas |
| `Max Seconds Per Question` | Teto do tempo calculado, para um enunciado enorme não travar a fila do totem. `0` desliga o teto |

A conta é `base + extra × (caracteres ÷ 100)`, limitada pelo teto. Com base 6 e extra 5,
uma afirmação de 40 caracteres vale 8s e uma de 150 vale 13,5s.

A ordem de decisão é: o `seconds` escrito na pergunta vence tudo (nem o teto o limita);
sem ele, vale a conta acima; com o cronômetro desligado, nenhuma pergunta tem tempo.

Se `Bonus Points Per Second Left` estiver ligado, lembre que perguntas longas passam a
render mais bônus — vale zerar o bônus ou aceitar essa diferença de propósito.

Sempre valide o JSON antes de entregar ao cliente (qualquer validador online serve) —
vírgula sobrando é o erro mais comum.
