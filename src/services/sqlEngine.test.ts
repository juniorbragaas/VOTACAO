import test from 'node:test';
import assert from 'node:assert/strict';
import { CategoriaParticipante, DatabaseState } from '../domain/types';
import { executarSqlComEstado, preprocessarSqlAccess } from './sqlEngine';

function createSampleState(): DatabaseState {
  return {
    Participantes: [
      { Id: 1, Nome: 'Ana', DataNascimento: '2001-01-10', Categoria: CategoriaParticipante.Adulto },
      { Id: 2, Nome: 'Bia', DataNascimento: '2000-05-12', Categoria: CategoriaParticipante.Adulto },
      { Id: 3, Nome: 'Carla', DataNascimento: '2009-07-20', Categoria: CategoriaParticipante.Teen },
    ],
    Jurados: [
      { Id: 1, Nome: 'Jurado 1', Descricao: 'Moda' },
      { Id: 2, Nome: 'Jurado 2', Descricao: 'Foto' },
    ],
    Criterios: [
      { Id: 1, Nome: 'Passarela' },
      { Id: 2, Nome: 'Simpatia' },
    ],
    Voto: [
      { Id: 1, IdJurado: 1, IdParticipante: 1, IdCriterio: 1, Nota: 10 },
      { Id: 2, IdJurado: 1, IdParticipante: 1, IdCriterio: 2, Nota: 9 },
      { Id: 3, IdJurado: 2, IdParticipante: 1, IdCriterio: 1, Nota: 10 },
      { Id: 4, IdJurado: 1, IdParticipante: 2, IdCriterio: 1, Nota: 8 },
      { Id: 5, IdJurado: 2, IdParticipante: 2, IdCriterio: 2, Nota: 7 },
    ],
    Auditoria: [],
    Consultas: [],
    nextIds: {
      Participantes: 4,
      Jurados: 3,
      Criterios: 3,
      Voto: 6,
      Auditoria: 1,
      Consultas: 1,
    },
  };
}

test('preprocessarSqlAccess converts TOP N inside main query and subqueries without touching strings', () => {
  assert.equal(
    preprocessarSqlAccess("SELECT TOP 3 Nome FROM Participantes WHERE Nome <> 'TOP 5'"),
    "SELECT Nome FROM Participantes WHERE Nome <> 'TOP 5' LIMIT 3"
  );
  assert.equal(
    preprocessarSqlAccess(
      'SELECT * FROM (SELECT TOP 2 Id, Nome FROM Participantes ORDER BY Id DESC) t'
    ),
    'SELECT * FROM (SELECT Id, Nome FROM Participantes ORDER BY Id DESC LIMIT 2) t'
  );
});

test('supports multi-table INNER JOIN and LEFT JOIN', async () => {
  const state = createSampleState();
  const { result } = await executarSqlComEstado(
    `SELECT p.Nome AS Participante,
            COALESCE(SUM(v.Nota), 0) AS TotalPontos,
            COUNT(v.Id) AS QtdNotas
     FROM Participantes p
     LEFT JOIN Voto v ON v.IdParticipante = p.Id
     LEFT JOIN Jurados j ON v.IdJurado = j.Id
     LEFT JOIN Criterios c ON v.IdCriterio = c.Id
     GROUP BY p.Id, p.Nome
     ORDER BY TotalPontos DESC, p.Nome ASC;`,
    state
  );

  assert.equal(result.isQuery, true);
  assert.deepEqual(result.columns, ['Participante', 'TotalPontos', 'QtdNotas']);
  assert.equal(result.rows.length, 3);
  assert.deepEqual(result.rows[0], { Participante: 'Ana', TotalPontos: 29, QtdNotas: 3 });
  assert.deepEqual(result.rows[1], { Participante: 'Bia', TotalPontos: 15, QtdNotas: 2 });
  assert.deepEqual(result.rows[2], { Participante: 'Carla', TotalPontos: 0, QtdNotas: 0 });
});

test('supports scalar subqueries, WHERE IN / EXISTS subqueries, derived tables, and CTEs', async () => {
  const state = createSampleState();

  // Scalar + WHERE IN subquery
  const scalarRes = await executarSqlComEstado(
    `SELECT p.Nome,
            (SELECT SUM(v.Nota) FROM Voto v WHERE v.IdParticipante = p.Id) AS SomaNotas
     FROM Participantes p
     WHERE p.Id IN (SELECT DISTINCT IdParticipante FROM Voto WHERE Nota >= 10)
     ORDER BY p.Nome;`,
    state
  );
  assert.equal(scalarRes.result.rows.length, 1);
  assert.deepEqual(scalarRes.result.rows[0], { Nome: 'Ana', SomaNotas: 29 });

  // Derived table + JOIN + EXISTS
  const derivedRes = await executarSqlComEstado(
    `SELECT p.Nome, sub.Total
     FROM Participantes p
     INNER JOIN (
       SELECT IdParticipante, SUM(Nota) AS Total
       FROM Voto
       GROUP BY IdParticipante
     ) sub ON sub.IdParticipante = p.Id
     WHERE EXISTS (
       SELECT 1 FROM Voto v2 WHERE v2.IdParticipante = p.Id AND v2.Nota = 8
     );`,
    state
  );
  assert.equal(derivedRes.result.rows.length, 1);
  assert.deepEqual(derivedRes.result.rows[0], { Nome: 'Bia', Total: 15 });

  // CTE + Window Function
  const cteRes = await executarSqlComEstado(
    `WITH Totais AS (
       SELECT p.Id, p.Nome, p.Categoria, COALESCE(SUM(v.Nota), 0) AS Pontos
       FROM Participantes p
       LEFT JOIN Voto v ON v.IdParticipante = p.Id
       GROUP BY p.Id, p.Nome, p.Categoria
     )
     SELECT Nome,
            IIF(Categoria = 1, 'Adulto', 'Teen') AS CatNome,
            Pontos,
            ROW_NUMBER() OVER (ORDER BY Pontos DESC) AS Posicao
     FROM Totais
     ORDER BY Posicao;`,
    state
  );
  assert.equal(cteRes.result.rows.length, 3);
  assert.deepEqual(cteRes.result.rows[0], {
    Nome: 'Ana',
    CatNome: 'Adulto',
    Pontos: 29,
    Posicao: 1,
  });
  assert.deepEqual(cteRes.result.rows[2], {
    Nome: 'Carla',
    CatNome: 'Teen',
    Pontos: 0,
    Posicao: 3,
  });
});

test('supports Access TOP N, singular aliases, zero-row headers, and multi-statement queries', async () => {
  const state = createSampleState();

  const topRes = await executarSqlComEstado(
    'SELECT TOP 2 Nome FROM [Participante] ORDER BY Id ASC;',
    state
  );
  assert.deepEqual(topRes.result.rows, [{ Nome: 'Ana' }, { Nome: 'Bia' }]);

  const emptyRes = await executarSqlComEstado(
    'SELECT Id, Nome FROM Participantes WHERE Id = -999;',
    state
  );
  assert.equal(emptyRes.result.isQuery, true);
  assert.deepEqual(emptyRes.result.columns, ['Id', 'Nome']);
  assert.equal(emptyRes.result.rows.length, 0);

  const multiRes = await executarSqlComEstado(
    `CREATE TEMP TABLE Resumo AS SELECT IdParticipante, SUM(Nota) AS Soma FROM Voto GROUP BY IdParticipante;
     SELECT p.Nome, r.Soma FROM Participantes p INNER JOIN Resumo r ON r.IdParticipante = p.Id ORDER BY r.Soma DESC;`,
    state
  );
  assert.equal(multiRes.result.rows.length, 2);
  assert.deepEqual(multiRes.result.rows[0], { Nome: 'Ana', Soma: 29 });
});

test('persists INSERT, UPDATE, and DELETE without WHERE', async () => {
  const state = createSampleState();

  const ins = await executarSqlComEstado(
    "INSERT INTO Criterios (Nome) VALUES ('Criatividade');",
    state
  );
  assert.equal(ins.modified, true);
  assert.equal(ins.nextState.Criterios.length, 3);
  assert.equal(ins.nextState.Criterios[2].Nome, 'Criatividade');

  const upd = await executarSqlComEstado(
    "UPDATE Criterios SET Nome = 'Postura' WHERE Nome = 'Criatividade';",
    ins.nextState
  );
  assert.equal(upd.modified, true);
  assert.equal(upd.nextState.Criterios[2].Nome, 'Postura');

  const delAll = await executarSqlComEstado('DELETE FROM Auditoria;', upd.nextState);
  assert.equal(delAll.nextState.Auditoria.length, 0);
});
