/**
 * Recebe as partidas dos totens e grava numa planilha do Google.
 *
 * Este arquivo NAO roda no jogo. Ele e colado dentro do Google Apps Script.
 * Veja o passo a passo em Tools/README.md.
 *
 * Por que Apps Script e nao a API do Google Sheets: a API exigiria uma chave de
 * conta de servico embarcada no executavel, entregue em 5 maquinas que ficam com
 * o cliente. Aqui o que vai no build e apenas uma URL mais uma senha combinada,
 * sem credencial de conta Google em lugar nenhum.
 */

// ---------------------------------------------------------------- configuracao

// Troque por uma senha qualquer e repita o MESMO valor no campo
// "Cloud Token" do GameConfig, no Unity.
var TOKEN = 'troque-esta-senha';

// Id da planilha de destino. Esta na URL dela, entre /d/ e /edit:
//   docs.google.com/spreadsheets/d/[ESTE-PEDACO-AQUI]/edit
//
// Deixar vazio so funciona se o script estiver PRESO a uma planilha (criado por
// "Extensoes > Apps Script" de dentro dela). Num script avulso, criado pelo
// script.google.com, nao existe planilha ativa e nada e gravado — que e a causa
// mais comum de "executou sem erro e nao apareceu nada".
var PLANILHA_ID = '';

// Nome da aba. O script cria se nao existir — repare que ela nasce como uma aba
// NOVA, e nao na "Pagina1" que voce esta vendo.
var ABA = 'partidas';

var COLUNAS = [
  'startedAt', 'finishedAt', 'totem', 'sessionId', 'topic', 'status',
  'questions', 'answered', 'correct', 'wrong', 'timedOut', 'score', 'durationSeconds'
];

// ---------------------------------------------------------------- recebimento

function doPost(e) {
  try {
    var body = JSON.parse(e.postData.contents);

    if (body.token !== TOKEN) {
      return responder({ ok: false, erro: 'token invalido' });
    }

    var linhas = body.rows || [];
    if (linhas.length === 0) {
      return responder({ ok: true, gravadas: 0 });
    }

    // Cinco totens podem enviar ao mesmo tempo. Sem trava, duas escritas
    // simultaneas disputam a mesma linha e uma sobrescreve a outra.
    var trava = LockService.getScriptLock();
    trava.waitLock(20000);

    try {
      var aba = prepararAba();
      var valores = linhas.map(function (linha) {
        return COLUNAS.map(function (coluna) {
          return linha[coluna] !== undefined ? linha[coluna] : '';
        });
      });

      aba.getRange(aba.getLastRow() + 1, 1, valores.length, COLUNAS.length).setValues(valores);
      SpreadsheetApp.flush();

      return responder({ ok: true, gravadas: valores.length });
    } finally {
      trava.releaseLock();
    }
  } catch (erro) {
    // O erro volta no corpo da resposta para o totem registrar no log.
    return responder({ ok: false, erro: String(erro) });
  }
}

/** Abre a planilha, por id ou pela que estiver presa ao script. */
function abrirPlanilha() {
  if (PLANILHA_ID) {
    return SpreadsheetApp.openById(PLANILHA_ID);
  }

  var ativa = SpreadsheetApp.getActiveSpreadsheet();
  if (!ativa) {
    throw new Error(
      'Nenhuma planilha encontrada. Este script nao esta preso a uma planilha: ' +
      'preencha PLANILHA_ID com o id da sua planilha.'
    );
  }

  return ativa;
}

/** Abre a aba de destino, criando com cabecalho na primeira vez. */
function prepararAba() {
  var planilha = abrirPlanilha();
  var aba = planilha.getSheetByName(ABA);

  if (!aba) {
    aba = planilha.insertSheet(ABA);
  }

  if (aba.getLastRow() === 0) {
    aba.appendRow(COLUNAS);
    aba.setFrozenRows(1);
  }

  return aba;
}

function responder(objeto) {
  return ContentService
    .createTextOutput(JSON.stringify(objeto))
    .setMimeType(ContentService.MimeType.JSON);
}

// ---------------------------------------------------------------- teste

/**
 * Rode pelo editor antes de levar os totens para o evento.
 *
 * Em caso de falha ele LANCA o erro de proposito, para aparecer em vermelho na
 * lista de execucoes. Uma funcao que "completa" silenciosamente sem gravar nada
 * e exatamente o que faz perder tempo procurando problema no lugar errado.
 */
function testar() {
  var resposta = doPost({
    postData: {
      contents: JSON.stringify({
        token: TOKEN,
        rows: [{
          startedAt: '2026-09-18 10:00:00',
          finishedAt: '2026-09-18 10:01:12',
          totem: 'teste',
          sessionId: 'teste-0001',
          topic: 'Direitos do Consumidor',
          status: 'Concluiu',
          questions: 10, answered: 10, correct: 7, wrong: 2, timedOut: 1,
          score: 700, durationSeconds: 72
        }]
      })
    }
  });

  var resultado = JSON.parse(resposta.getContent());
  Logger.log('Resposta do doPost: ' + resposta.getContent());

  if (!resultado.ok) {
    throw new Error('FALHOU: ' + resultado.erro);
  }

  var planilha = abrirPlanilha();
  Logger.log('OK. Gravado na aba "' + ABA + '" da planilha "' + planilha.getName() + '".');
  Logger.log('Link: ' + planilha.getUrl());
  Logger.log('Apague a linha de teste antes do evento.');
}

/**
 * Diagnostico rapido: diz a qual planilha o script esta ligado, sem escrever nada.
 */
function ondeEstouGravando() {
  try {
    var planilha = abrirPlanilha();
    Logger.log('Planilha: ' + planilha.getName());
    Logger.log('Link: ' + planilha.getUrl());
    Logger.log('Abas: ' + planilha.getSheets().map(function (a) { return a.getName(); }).join(', '));
  } catch (erro) {
    Logger.log('PROBLEMA: ' + erro);
  }
}
