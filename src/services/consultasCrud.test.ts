import test from 'node:test';
import assert from 'node:assert/strict';
import { DatabaseState } from '../domain/types';
import {
  excluirConsultaEmEstado,
  pesquisarConsultasEmEstado,
  salvarConsultaEmEstado,
} from './database';

function createBaseState(): DatabaseState {
  return {
    Participantes: [],
    Jurados: [],
    Criterios: [],
    Voto: [],
    Auditoria: [],
    Consultas: [
      { Id: 1, Titulo: 'Ranking Geral', SQL: 'SELECT * FROM Participantes;' },
      { Id: 2, Titulo: 'Notas por Jurado', SQL: 'SELECT * FROM Voto;' },
    ],
    nextIds: {
      Participantes: 1,
      Jurados: 1,
      Criterios: 1,
      Voto: 1,
      Auditoria: 1,
      Consultas: 3,
    },
  };
}

test('pesquisarConsultasEmEstado lists sorted queries and filters by title, SQL, or ID', () => {
  const state = createBaseState();
  const all = pesquisarConsultasEmEstado(state);
  assert.deepEqual(
    all.map((c) => c.Titulo),
    ['Notas por Jurado', 'Ranking Geral']
  );

  assert.equal(pesquisarConsultasEmEstado(state, 'Ranking').length, 1);
  assert.equal(pesquisarConsultasEmEstado(state, 'Voto').length, 1);
  assert.equal(pesquisarConsultasEmEstado(state, '2').length, 1);
});

test('salvarConsultaEmEstado validates required fields and creates/updates records', () => {
  const state = createBaseState();

  assert.throws(
    () => salvarConsultaEmEstado(state, { Id: 0, Titulo: '   ', SQL: 'SELECT 1' }),
    /título/i
  );
  assert.throws(
    () => salvarConsultaEmEstado(state, { Id: 0, Titulo: 'Teste', SQL: '   ' }),
    /SQL/i
  );

  // Create
  const created = salvarConsultaEmEstado(state, {
    Id: 0,
    Titulo: 'Subconsulta Top 3',
    SQL: 'SELECT TOP 3 Nome FROM Participantes;',
  });
  assert.equal(created.consulta.Id, 3);
  assert.equal(created.state.Consultas.length, 3);
  assert.equal(created.state.nextIds.Consultas, 4);

  // Update
  const updated = salvarConsultaEmEstado(created.state, {
    Id: 3,
    Titulo: 'Subconsulta Top 3 Editada',
    SQL: 'SELECT * FROM (SELECT Nome FROM Participantes) sub;',
  });
  assert.equal(updated.consulta.Titulo, 'Subconsulta Top 3 Editada');
  assert.equal(updated.state.Consultas.find((c) => c.Id === 3)?.SQL, 'SELECT * FROM (SELECT Nome FROM Participantes) sub;');

  // Update nonexistent
  assert.throws(
    () => salvarConsultaEmEstado(updated.state, { Id: 999, Titulo: 'X', SQL: 'SELECT 1' }),
    /não encontrada/i
  );
});

test('excluirConsultaEmEstado deletes query and throws on missing ID', () => {
  const state = createBaseState();
  const afterDelete = excluirConsultaEmEstado(state, 1);
  assert.equal(afterDelete.Consultas.length, 1);
  assert.equal(afterDelete.Consultas[0].Id, 2);

  assert.throws(() => excluirConsultaEmEstado(afterDelete, 1), /não encontrada/i);
});
