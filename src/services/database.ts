import {
  AvaliacaoNota,
  CategoriaParticipante,
  ConsultaSalva,
  Criterio,
  DatabaseState,
  Jurado,
  Participante,
} from '../domain/types';

const STORAGE_KEY = 'votacao_sistema_db_v1';

function criarEstadoInicial(): DatabaseState {
  const participantes: Participante[] = [
    { Id: 1, Nome: 'Ana Clara Ferreira', DataNascimento: '2001-04-15', Categoria: CategoriaParticipante.Adulto },
    { Id: 2, Nome: 'Beatriz Oliveira Santos', DataNascimento: '1999-09-22', Categoria: CategoriaParticipante.Adulto },
    { Id: 3, Nome: 'Camila Rodrigues Lima', DataNascimento: '2002-11-08', Categoria: CategoriaParticipante.Adulto },
    { Id: 4, Nome: 'Isabela Martins Costa', DataNascimento: '2009-02-19', Categoria: CategoriaParticipante.Teen },
    { Id: 5, Nome: 'Larissa Almeida Souza', DataNascimento: '2010-06-30', Categoria: CategoriaParticipante.Teen },
    { Id: 6, Nome: 'Sofia Mendes Pereira', DataNascimento: '2008-12-03', Categoria: CategoriaParticipante.Teen },
  ];

  const jurados: Jurado[] = [
    { Id: 1, Nome: 'Carlos Eduardo Nogueira', Descricao: 'Diretor de Moda e Passarela' },
    { Id: 2, Nome: 'Fernanda Vasconcelos', Descricao: 'Fotógrafa Editorial e Produtora' },
    { Id: 3, Nome: 'Ricardo Monteiro', Descricao: 'Consultor de Imagem e Estilo' },
  ];

  const criterios: Criterio[] = [
    { Id: 1, Nome: 'Passarela' },
    { Id: 2, Nome: 'Simpatia' },
    { Id: 3, Nome: 'Fotogenia' },
    { Id: 4, Nome: 'Elegância' },
  ];

  // Notas demonstrativas iniciais (escala de 5 a 10)
  const matrizNotas: Record<number, number[][]> = {
    1: [
      [9, 10, 9, 10],
      [8, 9, 9, 8],
      [9, 8, 10, 9],
      [10, 9, 9, 9],
      [8, 9, 8, 8],
      [9, 10, 9, 9],
    ],
    2: [
      [10, 9, 10, 9],
      [9, 8, 9, 9],
      [8, 9, 9, 8],
      [9, 10, 9, 10],
      [9, 8, 9, 8],
      [10, 9, 8, 9],
    ],
    3: [
      [9, 9, 10, 10],
      [8, 9, 8, 9],
      [9, 9, 9, 9],
      [9, 9, 10, 9],
      [8, 8, 9, 8],
      [9, 9, 9, 10],
    ],
  };

  const votos: AvaliacaoNota[] = [];
  let votoId = 1;
  for (const jurado of jurados) {
    participantes.forEach((p, pIdx) => {
      criterios.forEach((c, cIdx) => {
        votos.push({
          Id: votoId++,
          IdJurado: jurado.Id,
          IdParticipante: p.Id,
          IdCriterio: c.Id,
          Nota: matrizNotas[jurado.Id]?.[pIdx]?.[cIdx] ?? 5,
        });
      });
    });
  }

  const consultas: ConsultaSalva[] = [
    {
      Id: 1,
      Titulo: 'Classificação Geral por Pontuação',
      SQL: `SELECT p.Nome AS Participante, p.Categoria, SUM(v.Nota) AS TotalPontos, AVG(v.Nota) AS Media
FROM Voto v
INNER JOIN Participantes p ON v.IdParticipante = p.Id
GROUP BY p.Nome, p.Categoria
ORDER BY TotalPontos DESC;`,
    },
    {
      Id: 2,
      Titulo: 'Ranking Categoria Adulto',
      SQL: `SELECT p.Nome AS Participante, SUM(v.Nota) AS TotalPontos, AVG(v.Nota) AS Media
FROM Voto v
INNER JOIN Participantes p ON v.IdParticipante = p.Id
WHERE p.Categoria = 1
GROUP BY p.Nome
ORDER BY TotalPontos DESC;`,
    },
    {
      Id: 3,
      Titulo: 'Ranking Categoria Teen',
      SQL: `SELECT p.Nome AS Participante, SUM(v.Nota) AS TotalPontos, AVG(v.Nota) AS Media
FROM Voto v
INNER JOIN Participantes p ON v.IdParticipante = p.Id
WHERE p.Categoria = 2
GROUP BY p.Nome
ORDER BY TotalPontos DESC;`,
    },
    {
      Id: 4,
      Titulo: 'Notas Detalhadas por Jurado',
      SQL: `SELECT j.Nome AS Jurado, p.Nome AS Participante, c.Nome AS Criterio, v.Nota
FROM Voto v
INNER JOIN Jurados j ON v.IdJurado = j.Id
INNER JOIN Participantes p ON v.IdParticipante = p.Id
INNER JOIN Criterios c ON v.IdCriterio = c.Id
ORDER BY p.Nome ASC, j.Nome ASC;`,
    },
  ];

  return {
    Participantes: participantes,
    Jurados: jurados,
    Criterios: criterios,
    Voto: votos,
    Auditoria: [
      {
        Id: 1,
        Acao: 'INICIALIZACAO',
        Detalhes: 'Base de dados inicializada com registros demonstrativos.',
        Usuario: 'Sistema',
        DataHora: new Date().toISOString(),
      },
    ],
    Consultas: consultas,
    nextIds: {
      Participantes: participantes.length + 1,
      Jurados: jurados.length + 1,
      Criterios: criterios.length + 1,
      Voto: votoId,
      Auditoria: 2,
      Consultas: consultas.length + 1,
    },
  };
}

export function loadDb(): DatabaseState {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      const initial = criarEstadoInicial();
      saveDb(initial);
      return initial;
    }
    const parsed = JSON.parse(raw) as DatabaseState;
    if (!parsed || !Array.isArray(parsed.Participantes)) {
      const initial = criarEstadoInicial();
      saveDb(initial);
      return initial;
    }
    return parsed;
  } catch {
    const initial = criarEstadoInicial();
    saveDb(initial);
    return initial;
  }
}

export function saveDb(state: DatabaseState): void {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
}

function registrarAuditoria(db: DatabaseState, acao: string, detalhes: string): void {
  db.Auditoria.push({
    Id: db.nextIds.Auditoria++,
    Acao: acao,
    Detalhes: detalhes,
    Usuario: 'Operador',
    DataHora: new Date().toISOString(),
  });
}

// ===== PARTICIPANTE SERVICE =====
export const participanteService = {
  obterTodos(): Participante[] {
    return [...loadDb().Participantes].sort((a, b) => a.Nome.localeCompare(b.Nome, 'pt-BR', { sensitivity: 'base' }));
  },

  pesquisar(termo: string): Participante[] {
    const lista = this.obterTodos();
    const q = termo.trim().toLowerCase();
    if (!q) return lista;
    return lista.filter(
      (p) =>
        p.Nome.toLowerCase().includes(q) ||
        String(p.Id) === q ||
        (p.Categoria === CategoriaParticipante.Adulto ? 'adulto' : 'teen').includes(q)
    );
  },

  obterPorId(id: number): Participante | undefined {
    return loadDb().Participantes.find((p) => p.Id === id);
  },

  salvar(dto: { Id: number; Nome: string; DataNascimento: string; Categoria: CategoriaParticipante }): number {
    const nome = dto.Nome.trim();
    if (!nome) throw new Error('O nome do participante é obrigatório.');
    if (nome.length < 3) throw new Error('O nome deve conter pelo menos 3 caracteres.');
    if (!dto.DataNascimento) throw new Error('A data de nascimento é obrigatória.');

    const hojeStr = new Date().toISOString().slice(0, 10);
    if (dto.DataNascimento > hojeStr) {
      throw new Error('A data de nascimento não pode ser uma data futura.');
    }

    if (dto.Categoria !== CategoriaParticipante.Adulto && dto.Categoria !== CategoriaParticipante.Teen) {
      throw new Error('A categoria informada é inválida. Use ADULTO ou TEEN.');
    }

    const db = loadDb();
    if (dto.Id === 0) {
      const novoId = db.nextIds.Participantes++;
      db.Participantes.push({
        Id: novoId,
        Nome: nome,
        DataNascimento: dto.DataNascimento,
        Categoria: dto.Categoria,
      });
      registrarAuditoria(db, 'INCLUSAO_PARTICIPANTE', `Participante ${nome} (ID: ${novoId}) cadastrado.`);
      saveDb(db);
      return novoId;
    }

    const existente = db.Participantes.find((p) => p.Id === dto.Id);
    if (!existente) throw new Error('Participante não encontrado para atualização.');

    existente.Nome = nome;
    existente.DataNascimento = dto.DataNascimento;
    existente.Categoria = dto.Categoria;
    registrarAuditoria(db, 'ALTERACAO_PARTICIPANTE', `Participante ID: ${dto.Id} alterado.`);
    saveDb(db);
    return dto.Id;
  },

  excluir(id: number): boolean {
    const db = loadDb();
    const existente = db.Participantes.find((p) => p.Id === id);
    if (!existente) throw new Error('Participante não encontrado para exclusão.');

    db.Participantes = db.Participantes.filter((p) => p.Id !== id);
    db.Voto = db.Voto.filter((v) => v.IdParticipante !== id);
    registrarAuditoria(db, 'EXCLUSAO_PARTICIPANTE', `Participante '${existente.Nome}' (ID: ${id}) excluído.`);
    saveDb(db);
    return true;
  },
};

// ===== JURADO SERVICE =====
export const juradoService = {
  obterTodos(): Jurado[] {
    return [...loadDb().Jurados].sort((a, b) => a.Nome.localeCompare(b.Nome, 'pt-BR', { sensitivity: 'base' }));
  },

  pesquisar(termo: string): Jurado[] {
    const lista = this.obterTodos();
    const q = termo.trim().toLowerCase();
    if (!q) return lista;
    return lista.filter(
      (j) => j.Nome.toLowerCase().includes(q) || j.Descricao.toLowerCase().includes(q) || String(j.Id) === q
    );
  },

  obterPorId(id: number): Jurado | undefined {
    return loadDb().Jurados.find((j) => j.Id === id);
  },

  salvar(jurado: { Id: number; Nome: string; Descricao: string }): number {
    const nome = jurado.Nome.trim();
    const descricao = jurado.Descricao.trim();

    if (!nome) throw new Error('O nome do jurado é obrigatório.');
    if (nome.length < 3) throw new Error('O nome do jurado deve conter pelo menos 3 caracteres.');
    if (!descricao) throw new Error('A descrição do jurado é obrigatória.');

    const db = loadDb();
    const duplicado = db.Jurados.find(
      (j) => j.Nome.trim().toLowerCase() === nome.toLowerCase() && j.Id !== jurado.Id
    );
    if (duplicado) {
      throw new Error(`Já existe um jurado cadastrado com o nome '${nome}'.`);
    }

    if (jurado.Id === 0) {
      const novoId = db.nextIds.Jurados++;
      db.Jurados.push({ Id: novoId, Nome: nome, Descricao: descricao });
      registrarAuditoria(db, 'INCLUSAO_JURADO', `Jurado ${nome} (ID: ${novoId}) cadastrado.`);
      saveDb(db);
      return novoId;
    }

    const atual = db.Jurados.find((j) => j.Id === jurado.Id);
    if (!atual) throw new Error('Jurado não encontrado para atualização.');

    atual.Nome = nome;
    atual.Descricao = descricao;
    registrarAuditoria(db, 'ALTERACAO_JURADO', `Jurado ID: ${jurado.Id} alterado.`);
    saveDb(db);
    return jurado.Id;
  },

  excluir(id: number): boolean {
    const db = loadDb();
    const existente = db.Jurados.find((j) => j.Id === id);
    if (!existente) throw new Error('Jurado não encontrado para exclusão.');

    db.Jurados = db.Jurados.filter((j) => j.Id !== id);
    db.Voto = db.Voto.filter((v) => v.IdJurado !== id);
    registrarAuditoria(db, 'EXCLUSAO_JURADO', `Jurado '${existente.Nome}' (ID: ${id}) excluído.`);
    saveDb(db);
    return true;
  },
};

// ===== CRITERIO SERVICE =====
export const criterioService = {
  obterTodos(): Criterio[] {
    return [...loadDb().Criterios].sort((a, b) => a.Nome.localeCompare(b.Nome, 'pt-BR', { sensitivity: 'base' }));
  },

  pesquisar(termo: string): Criterio[] {
    const lista = this.obterTodos();
    const q = termo.trim().toLowerCase();
    if (!q) return lista;
    return lista.filter((c) => c.Nome.toLowerCase().includes(q) || String(c.Id) === q);
  },

  obterPorId(id: number): Criterio | undefined {
    return loadDb().Criterios.find((c) => c.Id === id);
  },

  salvar(criterio: { Id: number; Nome: string }): number {
    const nome = criterio.Nome.trim();
    if (!nome) throw new Error('O nome do critério é obrigatório.');
    if (nome.length < 2) throw new Error('O nome do critério deve conter pelo menos 2 caracteres.');

    const db = loadDb();
    const duplicado = db.Criterios.find(
      (c) => c.Nome.trim().toLowerCase() === nome.toLowerCase() && c.Id !== criterio.Id
    );
    if (duplicado) {
      throw new Error(`Já existe um critério cadastrado com o nome '${nome}'.`);
    }

    if (criterio.Id === 0) {
      const novoId = db.nextIds.Criterios++;
      db.Criterios.push({ Id: novoId, Nome: nome });
      registrarAuditoria(db, 'INCLUSAO_CRITERIO', `Critério ${nome} (ID: ${novoId}) cadastrado.`);
      saveDb(db);
      return novoId;
    }

    const atual = db.Criterios.find((c) => c.Id === criterio.Id);
    if (!atual) throw new Error('Critério não encontrado para atualização.');

    atual.Nome = nome;
    registrarAuditoria(db, 'ALTERACAO_CRITERIO', `Critério ID: ${criterio.Id} alterado.`);
    saveDb(db);
    return criterio.Id;
  },

  excluir(id: number): boolean {
    const db = loadDb();
    const existente = db.Criterios.find((c) => c.Id === id);
    if (!existente) throw new Error('Critério não encontrado para exclusão.');

    db.Criterios = db.Criterios.filter((c) => c.Id !== id);
    db.Voto = db.Voto.filter((v) => v.IdCriterio !== id);
    registrarAuditoria(db, 'EXCLUSAO_CRITERIO', `Critério '${existente.Nome}' (ID: ${id}) excluído.`);
    saveDb(db);
    return true;
  },
};

// ===== APURACAO DE NOTAS SERVICE =====
export const apuracaoNotasService = {
  garantirNotasEmBranco(): void {
    const db = loadDb();
    if (!db.Jurados.length || !db.Participantes.length || !db.Criterios.length) return;

    const existentes = new Set(db.Voto.map((v) => `${v.IdJurado}_${v.IdParticipante}_${v.IdCriterio}`));
    let mudou = false;

    for (const j of db.Jurados) {
      for (const p of db.Participantes) {
        for (const c of db.Criterios) {
          const chave = `${j.Id}_${p.Id}_${c.Id}`;
          if (!existentes.has(chave)) {
            db.Voto.push({
              Id: db.nextIds.Voto++,
              IdJurado: j.Id,
              IdParticipante: p.Id,
              IdCriterio: c.Id,
              Nota: 5,
            });
            existentes.add(chave);
            mudou = true;
          }
        }
      }
    }

    if (mudou) saveDb(db);
  },

  obterNotasPorJurado(idJurado: number): AvaliacaoNota[] {
    return loadDb().Voto.filter((v) => v.IdJurado === idJurado);
  },

  salvarNotas(idJurado: number, notas: Array<{ IdParticipante: number; IdCriterio: number; Nota: number }>): number {
    if (idJurado <= 0) throw new Error('Jurado inválido.');
    if (!notas.length) return 0;

    const db = loadDb();
    const jurado = db.Jurados.find((j) => j.Id === idJurado);
    if (!jurado) throw new Error('Jurado não encontrado.');

    let count = 0;
    for (const item of notas) {
      const existente = db.Voto.find(
        (v) =>
          v.IdJurado === idJurado &&
          v.IdParticipante === item.IdParticipante &&
          v.IdCriterio === item.IdCriterio
      );
      if (existente) {
        existente.Nota = item.Nota;
      } else {
        db.Voto.push({
          Id: db.nextIds.Voto++,
          IdJurado: idJurado,
          IdParticipante: item.IdParticipante,
          IdCriterio: item.IdCriterio,
          Nota: item.Nota,
        });
      }
      count++;
    }

    registrarAuditoria(db, 'APURACAO_NOTAS', `Salvas ${count} notas para o jurado ${jurado.Nome}.`);
    saveDb(db);
    return count;
  },

  zerarTodasNotas(): void {
    const db = loadDb();
    db.Voto = [];
    for (const j of db.Jurados) {
      for (const p of db.Participantes) {
        for (const c of db.Criterios) {
          db.Voto.push({
            Id: db.nextIds.Voto++,
            IdJurado: j.Id,
            IdParticipante: p.Id,
            IdCriterio: c.Id,
            Nota: 0,
          });
        }
      }
    }
    registrarAuditoria(db, 'ZERAR_NOTAS', 'Todas as notas foram zeradas.');
    saveDb(db);
  },
};

// ===== CONSULTAS & LIMPEZA DE BANCO =====
export const consultasService = {
  obterTodas(): ConsultaSalva[] {
    return [...loadDb().Consultas].sort((a, b) => a.Titulo.localeCompare(b.Titulo, 'pt-BR', { sensitivity: 'base' }));
  },

  salvar(titulo: string, sql: string): ConsultaSalva {
    const t = titulo.trim();
    const s = sql.trim();
    if (!t || !s) throw new Error('Informe o título e o SQL antes de salvar a consulta.');

    const linhas = s.replace(/\r\n/g, '\n').split('\n').length;
    if (linhas > 40000) throw new Error('O SQL pode ter no máximo 40.000 linhas.');

    const db = loadDb();
    const nova: ConsultaSalva = {
      Id: db.nextIds.Consultas++,
      Titulo: t,
      SQL: s,
    };
    db.Consultas.push(nova);
    registrarAuditoria(db, 'SALVAR_CONSULTA', `Consulta '${t}' salva.`);
    saveDb(db);
    return nova;
  },

  limparBanco(): number {
    const db = loadDb();
    const removidos = db.Voto.length + db.Jurados.length + db.Participantes.length;
    db.Voto = [];
    db.Jurados = [];
    db.Participantes = [];
    registrarAuditoria(db, 'LIMPAR_BANCO', `Tabelas Voto, Jurados e Participantes limpas (${removidos} registros).`);
    saveDb(db);
    return removidos;
  },
};
