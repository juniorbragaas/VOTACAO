export enum CategoriaParticipante {
  Adulto = 1,
  Teen = 2,
}

export function formatarCategoria(categoria: CategoriaParticipante | number): string {
  return Number(categoria) === CategoriaParticipante.Teen ? 'Teen' : 'Adulto';
}

export interface Participante {
  Id: number;
  Nome: string;
  DataNascimento: string; // YYYY-MM-DD
  Categoria: CategoriaParticipante;
}

export interface Jurado {
  Id: number;
  Nome: string;
  Descricao: string;
}

export interface Criterio {
  Id: number;
  Nome: string;
}

export interface AvaliacaoNota {
  Id: number;
  IdJurado: number;
  IdParticipante: number;
  IdCriterio: number;
  Nota: number;
}

export interface LogAuditoria {
  Id: number;
  Acao: string;
  Detalhes: string;
  Usuario: string;
  DataHora: string;
}

export interface ConsultaSalva {
  Id: number;
  Titulo: string;
  SQL: string;
}

export interface DatabaseState {
  Participantes: Participante[];
  Jurados: Jurado[];
  Criterios: Criterio[];
  Voto: AvaliacaoNota[];
  Auditoria: LogAuditoria[];
  Consultas: ConsultaSalva[];
  nextIds: {
    Participantes: number;
    Jurados: number;
    Criterios: number;
    Voto: number;
    Auditoria: number;
    Consultas: number;
  };
}

export interface SqlExecutionResult {
  isQuery: boolean;
  columns: string[];
  rows: Record<string, unknown>[];
  affectedRows: number;
}
