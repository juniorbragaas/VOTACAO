import initSqlJs from 'sql.js/dist/sql-wasm-inline.js';
import type { Database, SqlJsStatic, SqlValue } from 'sql.js';
import {
  AvaliacaoNota,
  CategoriaParticipante,
  ConsultaSalva,
  Criterio,
  DatabaseState,
  Jurado,
  LogAuditoria,
  Participante,
  SqlExecutionResult,
} from '../domain/types';
import { loadDb, saveDb } from './database';

let sqlModulePromise: Promise<SqlJsStatic> | null = null;

async function getSqlModule(): Promise<SqlJsStatic> {
  if (!sqlModulePromise) {
    sqlModulePromise = initSqlJs();
  }
  return sqlModulePromise;
}

/**
 * Converte cláusulas Access `SELECT [DISTINCT|ALL] TOP N ...` (inclusive dentro de subconsultas)
 * para `SELECT [DISTINCT|ALL] ... LIMIT N` compatível com SQLite.
 */
export function preprocessarSqlAccess(rawSql: string): string {
  const input = rawSql.trim();
  if (!input) return '';

  // Tokeniza respeitando strings ('...', "...") e identificadores ([...], `...`)
  interface Token {
    text: string;
    isWord: boolean;
    upper: string;
  }

  const tokens: Token[] = [];
  let i = 0;
  while (i < input.length) {
    const ch = input[i];

    // Comentário de linha --
    if (ch === '-' && input[i + 1] === '-') {
      const end = input.indexOf('\n', i);
      const slice = end === -1 ? input.slice(i) : input.slice(i, end + 1);
      tokens.push({ text: slice, isWord: false, upper: '' });
      i += slice.length;
      continue;
    }

    // Comentário de bloco /* ... */
    if (ch === '/' && input[i + 1] === '*') {
      const end = input.indexOf('*/', i + 2);
      const slice = end === -1 ? input.slice(i) : input.slice(i, end + 2);
      tokens.push({ text: slice, isWord: false, upper: '' });
      i += slice.length;
      continue;
    }

    // String ou identificador delimitado
    if (ch === "'" || ch === '"' || ch === '`' || ch === '[') {
      const closeCh = ch === '[' ? ']' : ch;
      let j = i + 1;
      while (j < input.length) {
        if (input[j] === closeCh) {
          if (closeCh !== ']' && input[j + 1] === closeCh) {
            j += 2;
            continue;
          }
          j++;
          break;
        }
        j++;
      }
      const slice = input.slice(i, j);
      tokens.push({ text: slice, isWord: false, upper: '' });
      i = j;
      continue;
    }

    // Palavra / número
    if (/[A-Za-z0-9_]/.test(ch)) {
      let j = i + 1;
      while (j < input.length && /[A-Za-z0-9_]/.test(input[j])) j++;
      const slice = input.slice(i, j);
      tokens.push({ text: slice, isWord: true, upper: slice.toUpperCase() });
      i = j;
      continue;
    }

    // Espaço ou pontuação
    if (/\s/.test(ch)) {
      let j = i + 1;
      while (j < input.length && /\s/.test(input[j])) j++;
      tokens.push({ text: input.slice(i, j), isWord: false, upper: '' });
      i = j;
      continue;
    }

    tokens.push({ text: ch, isWord: false, upper: '' });
    i++;
  }

  // Encontra índices de palavras não-espaço
  const nextWordIdx = (start: number): number => {
    for (let k = start; k < tokens.length; k++) {
      if (tokens[k].text.trim() !== '') return k;
    }
    return -1;
  };

  interface PendingLimit {
    depth: number;
    limitValue: string;
    hasLimit: boolean;
  }

  const pendingLimits: PendingLimit[] = [];
  const out: string[] = [];
  let depth = 0;

  for (let idx = 0; idx < tokens.length; idx++) {
    const tok = tokens[idx];

    if (tok.text === '(') {
      depth++;
      out.push(tok.text);
      continue;
    }

    if (tok.text === ')') {
      // Fecha qualquer SELECT TOP pendente nesse nível antes do ')'
      for (let p = pendingLimits.length - 1; p >= 0; p--) {
        if (pendingLimits[p].depth === depth) {
          const item = pendingLimits.splice(p, 1)[0];
          if (!item.hasLimit) {
            out.push(` LIMIT ${item.limitValue}`);
          }
        }
      }
      depth = Math.max(0, depth - 1);
      out.push(tok.text);
      continue;
    }

    if (tok.text === ';') {
      for (let p = pendingLimits.length - 1; p >= 0; p--) {
        if (pendingLimits[p].depth === depth) {
          const item = pendingLimits.splice(p, 1)[0];
          if (!item.hasLimit) {
            out.push(` LIMIT ${item.limitValue}`);
          }
        }
      }
      out.push(tok.text);
      continue;
    }

    if (tok.isWord && tok.upper === 'LIMIT') {
      for (let p = pendingLimits.length - 1; p >= 0; p--) {
        if (pendingLimits[p].depth === depth) {
          pendingLimits[p].hasLimit = true;
        }
      }
      out.push(tok.text);
      continue;
    }

    if (
      tok.isWord &&
      (tok.upper === 'UNION' || tok.upper === 'INTERSECT' || tok.upper === 'EXCEPT')
    ) {
      for (let p = pendingLimits.length - 1; p >= 0; p--) {
        if (pendingLimits[p].depth === depth) {
          const item = pendingLimits.splice(p, 1)[0];
          if (!item.hasLimit) {
            out.push(`LIMIT ${item.limitValue} `);
          }
        }
      }
      out.push(tok.text);
      continue;
    }

    if (tok.isWord && tok.upper === 'SELECT') {
      out.push(tok.text);
      let cursor = nextWordIdx(idx + 1);
      if (
        cursor !== -1 &&
        tokens[cursor].isWord &&
        (tokens[cursor].upper === 'DISTINCT' || tokens[cursor].upper === 'ALL')
      ) {
        // Preserva espaços e DISTINCT/ALL
        for (let k = idx + 1; k <= cursor; k++) out.push(tokens[k].text);
        idx = cursor;
        cursor = nextWordIdx(idx + 1);
      }

      if (cursor !== -1 && tokens[cursor].isWord && tokens[cursor].upper === 'TOP') {
        const numIdx = nextWordIdx(cursor + 1);
        if (numIdx !== -1 && tokens[numIdx].isWord && /^\d+$/.test(tokens[numIdx].text)) {
          pendingLimits.push({
            depth,
            limitValue: tokens[numIdx].text,
            hasLimit: false,
          });
          idx = numIdx;
        }
      }
      continue;
    }

    out.push(tok.text);
  }

  for (let p = pendingLimits.length - 1; p >= 0; p--) {
    const item = pendingLimits.splice(p, 1)[0];
    if (!item.hasLimit) {
      out.push(` LIMIT ${item.limitValue}`);
    }
  }

  return out.join('');
}

function registrarFuncoesAccess(db: Database): void {
  db.create_function('NOW', () => new Date().toISOString());
  db.create_function('DATE', () => new Date().toISOString().slice(0, 10));
  db.create_function('NZ', (val: SqlValue, fallback: SqlValue = '') =>
    val === null || val === undefined ? fallback : val
  );
  db.create_function('UCASE', (val: SqlValue) =>
    val === null || val === undefined ? null : String(val).toUpperCase()
  );
  db.create_function('LCASE', (val: SqlValue) =>
    val === null || val === undefined ? null : String(val).toLowerCase()
  );
  db.create_function('LEN', (val: SqlValue) =>
    val === null || val === undefined ? null : String(val).length
  );
  db.create_function('LEFT', (val: SqlValue, n: number) =>
    val === null || val === undefined ? null : String(val).slice(0, Math.max(0, Number(n) || 0))
  );
  db.create_function('RIGHT', (val: SqlValue, n: number) => {
    if (val === null || val === undefined) return null;
    const s = String(val);
    const len = Math.max(0, Number(n) || 0);
    return len === 0 ? '' : s.slice(-len);
  });
  db.create_function('MID', (val: SqlValue, start: number, len?: number) => {
    if (val === null || val === undefined) return null;
    const s = String(val);
    const idx = Math.max(0, (Number(start) || 1) - 1);
    return len === undefined ? s.slice(idx) : s.slice(idx, idx + Math.max(0, Number(len) || 0));
  });
  db.create_function('CINT', (val: SqlValue) => {
    const n = parseInt(String(val ?? 0), 10);
    return Number.isNaN(n) ? 0 : n;
  });
  db.create_function('CDBL', (val: SqlValue) => {
    const n = parseFloat(String(val ?? 0));
    return Number.isNaN(n) ? 0 : n;
  });
}

function criarEsquemaEPopular(db: Database, state: DatabaseState): void {
  db.run(`
    CREATE TABLE Participantes (
      Id INTEGER PRIMARY KEY AUTOINCREMENT,
      Nome TEXT NOT NULL,
      DataNascimento TEXT NOT NULL,
      Categoria INTEGER NOT NULL
    );
    CREATE TABLE Jurados (
      Id INTEGER PRIMARY KEY AUTOINCREMENT,
      Nome TEXT NOT NULL,
      Descricao TEXT NOT NULL
    );
    CREATE TABLE Criterios (
      Id INTEGER PRIMARY KEY AUTOINCREMENT,
      Nome TEXT NOT NULL
    );
    CREATE TABLE Voto (
      Id INTEGER PRIMARY KEY AUTOINCREMENT,
      IdJurado INTEGER NOT NULL,
      IdParticipante INTEGER NOT NULL,
      IdCriterio INTEGER NOT NULL,
      Nota INTEGER NOT NULL
    );
    CREATE TABLE Auditoria (
      Id INTEGER PRIMARY KEY AUTOINCREMENT,
      Acao TEXT NOT NULL,
      Detalhes TEXT NOT NULL,
      Usuario TEXT NOT NULL,
      DataHora TEXT NOT NULL
    );
    CREATE TABLE Consultas (
      Id INTEGER PRIMARY KEY AUTOINCREMENT,
      Titulo TEXT NOT NULL,
      SQL TEXT NOT NULL
    );

    -- Aliases de conveniência para consultas com nomes no singular ou plural
    CREATE VIEW Participante AS SELECT * FROM Participantes;
    CREATE VIEW Jurado AS SELECT * FROM Jurados;
    CREATE VIEW Criterio AS SELECT * FROM Criterios;
    CREATE VIEW Votos AS SELECT * FROM Voto;
    CREATE VIEW Consulta AS SELECT * FROM Consultas;
  `);

  db.run('BEGIN TRANSACTION;');
  try {
    const insPart = db.prepare(
      'INSERT INTO Participantes (Id, Nome, DataNascimento, Categoria) VALUES (?, ?, ?, ?)'
    );
    for (const p of state.Participantes) {
      insPart.run([p.Id, p.Nome, p.DataNascimento, p.Categoria]);
    }
    insPart.free();

    const insJur = db.prepare('INSERT INTO Jurados (Id, Nome, Descricao) VALUES (?, ?, ?)');
    for (const j of state.Jurados) {
      insJur.run([j.Id, j.Nome, j.Descricao]);
    }
    insJur.free();

    const insCrit = db.prepare('INSERT INTO Criterios (Id, Nome) VALUES (?, ?)');
    for (const c of state.Criterios) {
      insCrit.run([c.Id, c.Nome]);
    }
    insCrit.free();

    const insVoto = db.prepare(
      'INSERT INTO Voto (Id, IdJurado, IdParticipante, IdCriterio, Nota) VALUES (?, ?, ?, ?, ?)'
    );
    for (const v of state.Voto) {
      insVoto.run([v.Id, v.IdJurado, v.IdParticipante, v.IdCriterio, v.Nota]);
    }
    insVoto.free();

    const insAud = db.prepare(
      'INSERT INTO Auditoria (Id, Acao, Detalhes, Usuario, DataHora) VALUES (?, ?, ?, ?, ?)'
    );
    for (const a of state.Auditoria) {
      insAud.run([a.Id, a.Acao, a.Detalhes, a.Usuario, a.DataHora]);
    }
    insAud.free();

    const insCons = db.prepare('INSERT INTO Consultas (Id, Titulo, SQL) VALUES (?, ?, ?)');
    for (const c of state.Consultas) {
      insCons.run([c.Id, c.Titulo, c.SQL]);
    }
    insCons.free();

    db.run('COMMIT;');
  } catch (err) {
    db.run('ROLLBACK;');
    throw err;
  }
}

function extrairTabela<T>(db: Database, tableName: string, mapper: (row: Record<string, SqlValue>) => T): T[] {
  const stmt = db.prepare(`SELECT * FROM ${tableName}`);
  const cols = stmt.getColumnNames();
  const list: T[] = [];
  while (stmt.step()) {
    const vals = stmt.get();
    const obj: Record<string, SqlValue> = {};
    cols.forEach((c, idx) => {
      obj[c] = vals[idx];
    });
    list.push(mapper(obj));
  }
  stmt.free();
  return list;
}

function sincronizarEstadoDeSqlite(db: Database, prevState: DatabaseState): DatabaseState {
  const Participantes: Participante[] = extrairTabela(db, 'Participantes', (r) => ({
    Id: Number(r.Id ?? 0),
    Nome: String(r.Nome ?? ''),
    DataNascimento: String(r.DataNascimento ?? ''),
    Categoria:
      Number(r.Categoria) === CategoriaParticipante.Teen
        ? CategoriaParticipante.Teen
        : CategoriaParticipante.Adulto,
  }));

  const Jurados: Jurado[] = extrairTabela(db, 'Jurados', (r) => ({
    Id: Number(r.Id ?? 0),
    Nome: String(r.Nome ?? ''),
    Descricao: String(r.Descricao ?? ''),
  }));

  const Criterios: Criterio[] = extrairTabela(db, 'Criterios', (r) => ({
    Id: Number(r.Id ?? 0),
    Nome: String(r.Nome ?? ''),
  }));

  const Voto: AvaliacaoNota[] = extrairTabela(db, 'Voto', (r) => ({
    Id: Number(r.Id ?? 0),
    IdJurado: Number(r.IdJurado ?? 0),
    IdParticipante: Number(r.IdParticipante ?? 0),
    IdCriterio: Number(r.IdCriterio ?? 0),
    Nota: Number(r.Nota ?? 0),
  }));

  const Auditoria: LogAuditoria[] = extrairTabela(db, 'Auditoria', (r) => ({
    Id: Number(r.Id ?? 0),
    Acao: String(r.Acao ?? ''),
    Detalhes: String(r.Detalhes ?? ''),
    Usuario: String(r.Usuario ?? ''),
    DataHora: String(r.DataHora ?? ''),
  }));

  const Consultas: ConsultaSalva[] = extrairTabela(db, 'Consultas', (r) => ({
    Id: Number(r.Id ?? 0),
    Titulo: String(r.Titulo ?? ''),
    SQL: String(r.SQL ?? ''),
  }));

  const maxId = (items: Array<{ Id: number }>, minNext: number) =>
    items.reduce((acc, item) => Math.max(acc, (Number(item.Id) || 0) + 1), minNext);

  return {
    Participantes,
    Jurados,
    Criterios,
    Voto,
    Auditoria,
    Consultas,
    nextIds: {
      Participantes: maxId(Participantes, prevState.nextIds.Participantes),
      Jurados: maxId(Jurados, prevState.nextIds.Jurados),
      Criterios: maxId(Criterios, prevState.nextIds.Criterios),
      Voto: maxId(Voto, prevState.nextIds.Voto),
      Auditoria: maxId(Auditoria, prevState.nextIds.Auditoria),
      Consultas: maxId(Consultas, prevState.nextIds.Consultas),
    },
  };
}

function deduplicarColunas(rawCols: string[]): string[] {
  const counts = new Map<string, number>();
  return rawCols.map((col) => {
    const base = col || 'Coluna';
    const seen = counts.get(base) ?? 0;
    counts.set(base, seen + 1);
    return seen === 0 ? base : `${base} (${seen + 1})`;
  });
}

/**
 * Executa qualquer SQL válido (SELECT com subconsultas, CTEs, JOINs, agregações,
 * funções de janela, UNION, INSERT, UPDATE, DELETE, CREATE TABLE/VIEW, PRAGMA, múltiplos comandos)
 * contra o estado do banco fornecido, retornando o resultado e o novo estado (se alterado).
 */
export async function executarSqlComEstado(
  rawSql: string,
  state: DatabaseState
): Promise<{ result: SqlExecutionResult; nextState: DatabaseState; modified: boolean }> {
  const sql = preprocessarSqlAccess(rawSql);
  if (!sql) {
    throw new Error('Informe um SQL para executar.');
  }

  const SQL = await getSqlModule();
  const db = new SQL.Database();

  try {
    registrarFuncoesAccess(db);
    criarEsquemaEPopular(db, state);

    let lastQueryResult: SqlExecutionResult | null = null;
    let totalModified = 0;

    const iterator = db.iterateStatements(sql);
    for (const stmt of iterator) {
      const rawCols = stmt.getColumnNames();
      if (rawCols.length > 0) {
        const columns = deduplicarColunas(rawCols);
        const rows: Record<string, unknown>[] = [];
        while (stmt.step()) {
          const values = stmt.get();
          const rowObj: Record<string, unknown> = {};
          columns.forEach((colName, idx) => {
            const val = values[idx];
            rowObj[colName] =
              typeof val === 'number' && !Number.isInteger(val)
                ? Number(val.toFixed(4))
                : val;
          });
          rows.push(rowObj);
        }
        lastQueryResult = {
          isQuery: true,
          columns,
          rows,
          affectedRows: rows.length,
        };
      } else {
        while (stmt.step()) {
          // executa comando sem retorno tabular
        }
        totalModified += db.getRowsModified();
      }
    }

    const nextState = sincronizarEstadoDeSqlite(db, state);
    const coreChanged =
      JSON.stringify({
        Participantes: state.Participantes,
        Jurados: state.Jurados,
        Criterios: state.Criterios,
        Voto: state.Voto,
        Auditoria: state.Auditoria,
        Consultas: state.Consultas,
      }) !==
      JSON.stringify({
        Participantes: nextState.Participantes,
        Jurados: nextState.Jurados,
        Criterios: nextState.Criterios,
        Voto: nextState.Voto,
        Auditoria: nextState.Auditoria,
        Consultas: nextState.Consultas,
      });
    const modified = totalModified > 0 || coreChanged;

    if (lastQueryResult) {
      return { result: lastQueryResult, nextState, modified };
    }

    const totalBefore =
      state.Participantes.length +
      state.Jurados.length +
      state.Criterios.length +
      state.Voto.length +
      state.Auditoria.length +
      state.Consultas.length;
    const totalAfter =
      nextState.Participantes.length +
      nextState.Jurados.length +
      nextState.Criterios.length +
      nextState.Voto.length +
      nextState.Auditoria.length +
      nextState.Consultas.length;

    return {
      result: {
        isQuery: false,
        columns: [],
        rows: [],
        affectedRows: totalModified > 0 ? totalModified : Math.abs(totalBefore - totalAfter),
      },
      nextState,
      modified,
    };
  } finally {
    db.close();
  }
}

export async function executarSql(rawSql: string): Promise<SqlExecutionResult> {
  const currentState = loadDb();
  const { result, nextState, modified } = await executarSqlComEstado(rawSql, currentState);
  if (modified) {
    saveDb(nextState);
  }
  return result;
}
