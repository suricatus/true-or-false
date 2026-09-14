# Registro da ação e relatório final

O totem grava **dois CSVs por aparelho**, na mesma pasta gravável onde já fica o
`questions-report.txt` e onde se troca o `questions.json`:

| Arquivo | Uma linha por | Serve para |
|---|---|---|
| `partidas-<totem>.csv` | visita ao totem | total de partidas, assunto escolhido, pontuação, abandono |
| `respostas-<totem>.csv` | pergunta respondida | **acertos e erros de cada pergunta** |

A gravação é local e por acréscimo, com o arquivo fechado a cada linha. Totem
desligado na tomada não corrompe o que já foi registrado, e nada depende de rede.

> **Atenção ao contar gente.** Cada linha é uma *partida*, não uma pessoa. Um grupo
> se reveza no mesmo aparelho e a mesma pessoa joga várias vezes. No relatório para o
> cliente, fale em partidas — público alcançado é estimativa, e deve ser declarado como tal.

## Antes do evento: batizar cada totem

Os 5 totens rodam o mesmo APK. Se nada for feito, o jogo usa um resumo do
identificador do aparelho e os arquivos saem distintos, mas com nomes ilegíveis
(`partidas-totem-a3f91c.csv`).

Para nomes limpos, crie em cada aparelho um arquivo **`totem.txt`** na pasta de
conteúdo (a mesma do `questions.json`), com uma linha só:

```
totem-01
```

É o mesmo gesto de trocar as perguntas: copiar um arquivo e reabrir o app. Confira
no `questions-report.txt` ou no primeiro CSV gerado se o nome pegou.

## Durante o evento

Nada a fazer. Vale abrir o `partidas-<totem>.csv` uma vez no primeiro dia para
confirmar que está crescendo.

## Depois do evento

1. Copie os CSVs dos 5 totens para uma pasta só no computador. Não renomeie: o nome
   do arquivo carrega o totem, e a coluna `totem` repete a informação dentro das linhas.
2. Gere o relatório:

```bash
python Tools/relatorio.py ./csvs-do-evento -o relatorio.html --cliente "PROCON-RJ"
```

Sai um HTML único, que abre em qualquer navegador e imprime em PDF para entregar
ao cliente. Só biblioteca padrão do Python — nada para instalar.

O relatório traz: partidas jogadas e concluídas, pontuação média, duração média, taxa
de conclusão, ranking de assuntos, desempenho por totem e o ranking de perguntas mais
erradas.

## Ligar e desligar o registro

O `SessionLogger` é um componente opcional no objeto do `GameRunner`. Sem ele na cena,
o jogo roda exatamente como antes. No Inspector:

- **Log Answers** — desligue só se o arquivo precisar ficar mínimo. Desligado, você
  perde a métrica de acertos e erros por pergunta, que é a mais valiosa do relatório.
- **Totem Id** (no GameConfig) — alternativa ao `totem.txt`, mas sai igual nos 5
  aparelhos se todos usarem o mesmo build. Prefira o arquivo.

---

# Acompanhar a ação à distância (Google Sheets)

Opcional. Liga um envio das partidas para uma planilha do Google, para acompanhar
o evento sem estar lá.

> **A planilha não substitui os CSVs.** Ela serve para acompanhar em tempo quase real.
> O relatório final continua saindo dos arquivos locais, por três motivos: a planilha
> não recebe o detalhe por pergunta, um totem sem Wi-Fi só aparece quando reconectar, e
> uma resposta perdida no caminho pode gerar linha repetida. Os CSVs não têm nenhum
> desses problemas.

## Montar a planilha (uma vez)

1. Crie uma planilha nova no Google Sheets.
2. Abra o Apps Script **de dentro dela**: `Extensões > Apps Script`.
   Criar o script pelo `script.google.com` gera um projeto avulso, sem planilha
   ligada — ele executa sem erro e não grava nada.
3. Apague o conteúdo e cole o [`AppsScript.gs`](AppsScript.gs).
4. Troque `troque-esta-senha` por uma senha sua.
   Preencha também `PLANILHA_ID` com o id que está na URL da planilha, entre
   `/d/` e `/edit`. Com ele preenchido funciona nos dois casos.
5. Rode a função `testar()` pelo editor e confira a planilha antes de seguir.
6. `Implantar > Nova implantação > Tipo: App da Web`
   - Executar como: **Eu**
   - Quem pode acessar: **Qualquer pessoa**
7. Autorize quando pedir e **copie a URL** gerada (termina em `/exec`).

O passo 6 assusta, mas "qualquer pessoa" significa apenas que a URL não exige login
do Google — é a senha do passo 4 que barra escrita indevida. Sem ela, o script recusa.

## Ligar no jogo

No `GameConfig` do cliente:

| Campo | Valor |
|---|---|
| Cloud Endpoint | a URL do passo 7 |
| Cloud Token | a senha do passo 4 |

E na cena, adicione o componente **Cloud Sync** no mesmo objeto do `Session Logger`.
Sem endereço preenchido o componente se desliga sozinho — é o estado normal de um
cliente que não quer acompanhamento remoto.

## O que esperar

- Uma linha por partida encerrada, a cada 30 segundos, em lote.
- Sem internet, as partidas ficam em `nuvem-pendentes.jsonl` na pasta de dados e sobem
  quando a conexão volta. Nada se perde, nem se o totem for desligado na tomada.
- Nada disso bloqueia o jogo: falha de rede vira aviso no log, nunca erro em tela.

Para acompanhar, monte um gráfico dinâmico na própria planilha por `totem`, `topic` e
`status`. Contar partidas por hora dá a curva de movimento do evento.
