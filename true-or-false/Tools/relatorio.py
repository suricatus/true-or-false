#!/usr/bin/env python3
"""
Consolida os CSVs dos totens e gera o relatorio final da acao, em HTML.

Uso:
    python relatorio.py <pasta-com-os-csvs> [-o relatorio.html] [--cliente "PROCON-RJ"]

A pasta deve conter os arquivos copiados de cada totem:
    partidas-totem-01.csv   respostas-totem-01.csv
    partidas-totem-02.csv   respostas-totem-02.csv
    ...

So biblioteca padrao: roda em qualquer maquina com Python, sem instalar nada.
"""

import argparse
import csv
import html
import sys
from collections import Counter, defaultdict
from datetime import datetime
from pathlib import Path

SEP = ";"


# ----------------------------------------------------------------- leitura


def ler_csvs(pasta: Path, prefixo: str) -> list[dict]:
    """Junta todos os arquivos de um tipo, de todos os totens."""
    linhas = []
    arquivos = sorted(pasta.glob(f"{prefixo}-*.csv"))

    for arquivo in arquivos:
        # utf-8-sig descarta o BOM que o Unity grava no inicio do arquivo.
        with arquivo.open(encoding="utf-8-sig", newline="") as f:
            for linha in csv.DictReader(f, delimiter=SEP):
                linha["_arquivo"] = arquivo.name
                linhas.append(linha)

    return linhas, arquivos


def totens_dos_arquivos(arquivos: list[Path], prefixo: str) -> set[str]:
    """Extrai o nome do totem do nome do arquivo: partidas-totem-03.csv -> totem-03."""
    return {a.stem[len(prefixo) + 1:] for a in arquivos if a.stem.startswith(prefixo + "-")}


def conferir_coleta(arq_partidas, arq_respostas, esperados: int | None) -> list[str]:
    """
    Avisa sobre buraco na coleta.

    Um totem que nao teve os arquivos copiados some do relatorio em silencio — e quando
    alguem percebe, os totens ja foram desmontados e nao ha como recuperar.
    """
    avisos = []
    com_partidas = totens_dos_arquivos(arq_partidas, "partidas")
    com_respostas = totens_dos_arquivos(arq_respostas, "respostas")

    if esperados and len(com_partidas) != esperados:
        avisos.append(
            f"Esperava {esperados} totens e encontrei {len(com_partidas)}: "
            + ", ".join(sorted(com_partidas))
        )

    for totem in sorted(com_partidas - com_respostas):
        avisos.append(
            f"O totem '{totem}' tem partidas mas nao tem o arquivo de respostas. "
            "As metricas por pergunta ficam incompletas."
        )

    for totem in sorted(com_respostas - com_partidas):
        avisos.append(f"O totem '{totem}' tem respostas mas nao tem o arquivo de partidas.")

    return avisos


def inteiro(linha: dict, campo: str) -> int:
    try:
        return int(linha.get(campo) or 0)
    except ValueError:
        return 0


# ----------------------------------------------------------------- metricas


def resumo(partidas: list[dict]) -> dict:
    concluidas = [p for p in partidas if p.get("status") == "Concluiu"]
    jogadas = [p for p in partidas if p.get("status") != "AbandonouNaSelecao"]

    pontuacoes = [inteiro(p, "score") for p in concluidas]
    duracoes = [inteiro(p, "durationSeconds") for p in concluidas]

    return {
        "visitas": len(partidas),
        "jogadas": len(jogadas),
        "concluidas": len(concluidas),
        "desistiu_selecao": sum(1 for p in partidas if p.get("status") == "AbandonouNaSelecao"),
        "desistiu_jogo": sum(1 for p in partidas if p.get("status") == "AbandonouNoJogo"),
        "media_pontos": round(sum(pontuacoes) / len(pontuacoes)) if pontuacoes else 0,
        "maior_pontuacao": max(pontuacoes) if pontuacoes else 0,
        "media_duracao": round(sum(duracoes) / len(duracoes)) if duracoes else 0,
        "totens": len({p.get("totem", "") for p in partidas}),
    }


def por_tema(partidas: list[dict], respostas: list[dict]) -> list[dict]:
    """Quantas partidas cada assunto puxou e quao bem o publico se saiu nele."""
    escolhas = Counter(p.get("topic", "") for p in partidas if p.get("topic"))

    acertos = Counter()
    total = Counter()
    for r in respostas:
        categoria = r.get("category", "")
        if not categoria:
            continue
        total[categoria] += 1
        if r.get("verdict") == "Correct":
            acertos[categoria] += 1

    linhas = []
    for tema, partidas_do_tema in escolhas.most_common():
        respondidas = total.get(tema, 0)
        linhas.append(
            {
                "tema": tema,
                "partidas": partidas_do_tema,
                "respostas": respondidas,
                "acertos": acertos.get(tema, 0),
                "taxa": round(100 * acertos.get(tema, 0) / respondidas) if respondidas else 0,
            }
        )
    return linhas


def por_pergunta(respostas: list[dict]) -> list[dict]:
    """
    Acertos e erros de cada pergunta.

    E a metrica de maior valor do relatorio: mostra onde o publico esta desinformado,
    que e exatamente o que justifica a proxima acao de educacao do consumidor.
    """
    dados = defaultdict(lambda: {"acertos": 0, "erros": 0, "tempo": 0, "enunciado": "", "categoria": ""})

    for r in respostas:
        chave = r.get("questionId") or r.get("statement", "")[:60]
        item = dados[chave]
        item["enunciado"] = r.get("statement", "")
        item["categoria"] = r.get("category", "")

        veredito = r.get("verdict")
        if veredito == "Correct":
            item["acertos"] += 1
        else:
            # Tempo esgotado conta como erro: o publico nao soube responder a tempo.
            item["erros"] += 1

    linhas = []
    for chave, item in dados.items():
        total = item["acertos"] + item["erros"]
        linhas.append(
            {
                "id": chave,
                "enunciado": item["enunciado"],
                "categoria": item["categoria"],
                "acertos": item["acertos"],
                "erros": item["erros"],
                "total": total,
                "taxa_erro": round(100 * item["erros"] / total) if total else 0,
            }
        )

    # Mais errada primeiro; com poucas respostas o percentual e ruido, entao desempata pelo volume.
    linhas.sort(key=lambda x: (-x["taxa_erro"], -x["total"]))
    return linhas


def por_totem(partidas: list[dict]) -> list[dict]:
    dados = defaultdict(lambda: {"visitas": 0, "concluidas": 0, "pontos": []})

    for p in partidas:
        item = dados[p.get("totem", "?")]
        item["visitas"] += 1
        if p.get("status") == "Concluiu":
            item["concluidas"] += 1
            item["pontos"].append(inteiro(p, "score"))

    linhas = []
    for totem, item in sorted(dados.items()):
        pontos = item["pontos"]
        linhas.append(
            {
                "totem": totem,
                "visitas": item["visitas"],
                "concluidas": item["concluidas"],
                "media": round(sum(pontos) / len(pontos)) if pontos else 0,
            }
        )
    return linhas


def periodo(partidas: list[dict]) -> str:
    datas = [p.get("startedAt", "") for p in partidas if p.get("startedAt")]
    if not datas:
        return "sem registros"
    return f"{min(datas)} ate {max(datas)}"


# ----------------------------------------------------------------- html


CSS = """
:root{--tinta:#16181d;--fraca:#6b7280;--linha:#e5e7eb;--fundo:#fff;--destaque:#0b7a3b;--alerta:#b42318;--caixa:#f7f8fa}
*{box-sizing:border-box}
body{margin:0;padding:40px 28px 72px;font:15px/1.55 -apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,sans-serif;color:var(--tinta);background:var(--fundo)}
main{max-width:960px;margin:0 auto}
h1{font-size:28px;margin:0 0 4px;letter-spacing:-.02em}
h2{font-size:19px;margin:44px 0 14px;letter-spacing:-.01em}
.sub{color:var(--fraca);margin:0 0 8px}
.cards{display:grid;grid-template-columns:repeat(auto-fit,minmax(150px,1fr));gap:12px;margin:24px 0 8px}
.card{background:var(--caixa);border:1px solid var(--linha);border-radius:10px;padding:14px 16px}
.card .n{font-size:26px;font-weight:650;letter-spacing:-.02em}
.card .r{color:var(--fraca);font-size:12.5px;text-transform:uppercase;letter-spacing:.04em;margin-top:2px}
table{width:100%;border-collapse:collapse;margin-top:6px;font-size:14px}
th,td{text-align:left;padding:9px 10px;border-bottom:1px solid var(--linha);vertical-align:top}
th{font-size:12px;text-transform:uppercase;letter-spacing:.04em;color:var(--fraca);font-weight:600}
td.n,th.n{text-align:right;font-variant-numeric:tabular-nums;white-space:nowrap}
.barra{position:relative;background:#eef0f3;border-radius:4px;height:8px;min-width:90px;overflow:hidden}
.barra i{position:absolute;inset:0 auto 0 0;background:var(--destaque);border-radius:4px}
.barra.erro i{background:var(--alerta)}
.enunciado{color:var(--tinta)}
.cat{color:var(--fraca);font-size:12.5px;margin-top:2px}
.nota{color:var(--fraca);font-size:13px;background:var(--caixa);border-left:3px solid var(--linha);padding:10px 14px;border-radius:0 8px 8px 0;margin:14px 0}
.nota.alerta{border-left-color:var(--alerta);color:var(--tinta)}
footer{color:var(--fraca);font-size:12.5px;margin-top:48px;border-top:1px solid var(--linha);padding-top:14px}
@media print{body{padding:0}h2{break-after:avoid}tr{break-inside:avoid}}
"""


def e(valor) -> str:
    return html.escape(str(valor))


def barra(percentual: int, erro: bool = False) -> str:
    classe = "barra erro" if erro else "barra"
    return f'<div class="{classe}"><i style="width:{max(0, min(100, percentual))}%"></i></div>'


def gerar_html(cliente, partidas, respostas, arquivos, avisos=()) -> str:
    r = resumo(partidas)
    temas = por_tema(partidas, respostas)
    perguntas = por_pergunta(respostas)
    totens = por_totem(partidas)

    tema_top = temas[0]["tema"] if temas else "—"
    conversao = round(100 * r["jogadas"] / r["visitas"]) if r["visitas"] else 0
    conclusao = round(100 * r["concluidas"] / r["jogadas"]) if r["jogadas"] else 0

    cards = [
        (r["jogadas"], "partidas jogadas"),
        (r["concluidas"], "partidas concluidas"),
        (f"{r['media_pontos']}", "pontuacao media"),
        (f"{r['media_duracao']}s", "duracao media"),
        (f"{conclusao}%", "taxa de conclusao"),
        (r["totens"], "totens"),
    ]

    partes = [
        "<!doctype html><html lang='pt-BR'><head><meta charset='utf-8'>",
        "<meta name='viewport' content='width=device-width,initial-scale=1'>",
        f"<title>Relatorio da acao — {e(cliente)}</title><style>{CSS}</style></head><body><main>",
        f"<h1>Relatorio da acao — {e(cliente)}</h1>",
        f"<p class='sub'>Periodo: {e(periodo(partidas))}</p>",
        f"<p class='sub'>Assunto mais escolhido: <strong>{e(tema_top)}</strong></p>",
        "<div class='cards'>",
    ]

    for numero, rotulo in cards:
        partes.append(f"<div class='card'><div class='n'>{e(numero)}</div><div class='r'>{e(rotulo)}</div></div>")
    partes.append("</div>")

    for aviso in avisos:
        partes.append(f"<p class='nota alerta'><strong>Atencao na coleta.</strong> {e(aviso)}</p>")

    partes.append(
        "<p class='nota'><strong>Como ler estes numeros.</strong> Cada linha registrada e uma "
        "<em>partida</em>, nao uma pessoa: um grupo pode se revezar no mesmo totem e a mesma pessoa "
        f"pode jogar varias vezes. Das {e(r['visitas'])} interacoes iniciadas, {e(conversao)}% viraram "
        "partida — as demais desistiram ainda na tela de escolha do assunto.</p>"
    )

    # --- temas
    partes.append("<h2>Assuntos</h2><table><tr><th>Assunto</th><th class='n'>Partidas</th>"
                  "<th class='n'>Respostas</th><th class='n'>Acerto</th><th>&nbsp;</th></tr>")
    for t in temas:
        partes.append(
            f"<tr><td>{e(t['tema'])}</td><td class='n'>{e(t['partidas'])}</td>"
            f"<td class='n'>{e(t['respostas'])}</td><td class='n'>{e(t['taxa'])}%</td>"
            f"<td>{barra(t['taxa'])}</td></tr>"
        )
    partes.append("</table>")

    # --- totens
    partes.append("<h2>Por totem</h2><table><tr><th>Totem</th><th class='n'>Interacoes</th>"
                  "<th class='n'>Partidas concluidas</th><th class='n'>Pontuacao media</th></tr>")
    for t in totens:
        partes.append(
            f"<tr><td>{e(t['totem'])}</td><td class='n'>{e(t['visitas'])}</td>"
            f"<td class='n'>{e(t['concluidas'])}</td><td class='n'>{e(t['media'])}</td></tr>"
        )
    partes.append("</table>")

    # --- perguntas
    partes.append("<h2>Perguntas que o publico mais errou</h2>")
    partes.append(
        "<p class='nota'>Este e o dado de maior valor da acao: mostra onde a desinformacao esta "
        "concentrada. Tempo esgotado conta como erro.</p>"
    )
    partes.append("<table><tr><th>Pergunta</th><th class='n'>Acertos</th><th class='n'>Erros</th>"
                  "<th class='n'>Erro</th><th>&nbsp;</th></tr>")
    for p in perguntas:
        partes.append(
            f"<tr><td><div class='enunciado'>{e(p['enunciado'])}</div>"
            f"<div class='cat'>{e(p['categoria'])} · {e(p['id'])}</div></td>"
            f"<td class='n'>{e(p['acertos'])}</td><td class='n'>{e(p['erros'])}</td>"
            f"<td class='n'>{e(p['taxa_erro'])}%</td><td>{barra(p['taxa_erro'], erro=True)}</td></tr>"
        )
    partes.append("</table>")

    partes.append(
        f"<footer>Gerado em {datetime.now():%d/%m/%Y %H:%M} a partir de {len(arquivos)} arquivo(s): "
        + e(", ".join(a.name for a in arquivos))
        + ".</footer></main></body></html>"
    )

    return "".join(partes)


# ----------------------------------------------------------------- cli


def main() -> int:
    parser = argparse.ArgumentParser(description="Gera o relatorio final da acao a partir dos CSVs dos totens.")
    parser.add_argument("pasta", type=Path, help="pasta com os CSVs copiados dos totens")
    parser.add_argument("-o", "--saida", type=Path, default=Path("relatorio.html"))
    parser.add_argument("--cliente", default="PROCON-RJ")
    parser.add_argument("--totens", type=int, default=None,
                        help="quantos totens deveriam ter sido coletados; avisa se faltar algum")
    args = parser.parse_args()

    if not args.pasta.is_dir():
        print(f"Pasta nao encontrada: {args.pasta}", file=sys.stderr)
        return 1

    partidas, arq_partidas = ler_csvs(args.pasta, "partidas")
    respostas, arq_respostas = ler_csvs(args.pasta, "respostas")

    if not partidas:
        print(f"Nenhum 'partidas-*.csv' em {args.pasta}", file=sys.stderr)
        return 1

    avisos = conferir_coleta(arq_partidas, arq_respostas, args.totens)

    args.saida.write_text(
        gerar_html(args.cliente, partidas, respostas, arq_partidas + arq_respostas, avisos),
        encoding="utf-8",
    )

    for aviso in avisos:
        print(f"AVISO: {aviso}", file=sys.stderr)

    r = resumo(partidas)
    print(f"{r['jogadas']} partidas ({r['concluidas']} concluidas) de {r['totens']} totem(ns)")
    print(f"{len(respostas)} respostas registradas")
    print(f"Relatorio: {args.saida.resolve()}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
