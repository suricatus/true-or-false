# Customização por cliente

Cada cliente é uma pasta aqui dentro (`_Clients/<Cliente>/`). O núcleo do jogo
(`Assets/_Core/`) **não é tocado** em nenhuma customização — se você precisou editar
um arquivo do `_Core`, é sinal de que uma regra nova deve virar configuração.

## Checklist de um cliente novo

1. Duplique `_Clients/_Template/` e renomeie para o nome do cliente.
2. **Regras** — abra o `GameConfig` da pasta e ajuste nº de perguntas, tempo,
   pontuação e o timeout de inatividade do totem.
3. **Marca** — abra o `Theme` e troque logo, cores, fontes, sprites dos botões,
   textos de tela e sons.
4. **Conteúdo** — edite `Assets/StreamingAssets/Suricatus/questions.json`.
5. Rode os testes (`Window > General > Test Runner > EditMode`) antes de gerar o build.

## O arquivo de perguntas

Fica em `StreamingAssets/Suricatus/questions.json` e vai **junto do build**, na pasta
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

Regras que o jogo aplica sozinho:

- Se o arquivo faltar, estiver com JSON quebrado ou sem nenhuma pergunta válida,
  o jogo cai no **banco embutido** definido em `GameConfig > Fallback Catalog`.
  Um totem em evento nunca fica sem conteúdo.
- Perguntas com `statement` vazio invalidam o arquivo; `id` repetido gera só um aviso.
- Se houver menos perguntas que o configurado na rodada, a partida fica mais curta
  em vez de repetir pergunta.

Sempre valide o JSON antes de entregar ao cliente (qualquer validador online serve) —
vírgula sobrando é o erro mais comum.
