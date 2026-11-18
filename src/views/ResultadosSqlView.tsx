import React, { useEffect, useState } from 'react';
import { DialogConfig } from '../components/DialogModal';
import { ConsultaSalva, SqlExecutionResult } from '../domain/types';
import { consultasService } from '../services/database';
import { exportarTabelaPdf } from '../services/pdfExporter';
import { executarSql } from '../services/sqlEngine';

interface ResultadosSqlViewProps {
  showDialog: (config: DialogConfig) => void;
}

export const ResultadosSqlView: React.FC<ResultadosSqlViewProps> = ({ showDialog }) => {
  const [consultas, setConsultas] = useState<ConsultaSalva[]>([]);
  const [termo, setTermo] = useState('');
  const [selectedId, setSelectedId] = useState<number | null>(null);

  // Modal CRUD state
  const [modalAberto, setModalAberto] = useState(false);
  const [editId, setEditId] = useState(0);
  const [modalTitulo, setModalTitulo] = useState('');
  const [modalSql, setModalSql] = useState('');

  // Execution result state
  const [mensagem, setMensagem] = useState('');
  const [tituloResultado, setTituloResultado] = useState('Resultado');
  const [resultado, setResultado] = useState<SqlExecutionResult | null>(null);
  const [executando, setExecutando] = useState(false);

  const carregarConsultas = (filtro = '', preserveSelectedId?: number | null) => {
    const lista = consultasService.pesquisar(filtro);
    setConsultas(lista);

    if (preserveSelectedId !== undefined) {
      const existe = lista.some((c) => c.Id === preserveSelectedId);
      setSelectedId(existe ? preserveSelectedId : lista[0]?.Id ?? null);
    } else if (lista.length > 0) {
      setSelectedId((prev) => (prev && lista.some((c) => c.Id === prev) ? prev : lista[0].Id));
    } else {
      setSelectedId(null);
    }
  };

  useEffect(() => {
    carregarConsultas('');
  }, []);

  const consultaSelecionada = consultas.find((c) => c.Id === selectedId) ?? null;

  const abrirNovaModal = () => {
    setEditId(0);
    setModalTitulo('');
    setModalSql('SELECT * FROM Participantes;');
    setModalAberto(true);
  };

  const abrirEdicaoModal = (consulta?: ConsultaSalva) => {
    const alvo = consulta ?? consultaSelecionada;
    if (!alvo) {
      showDialog({
        title: 'Aviso',
        message: 'Selecione uma consulta salva para editar.',
        type: 'info',
      });
      return;
    }
    setSelectedId(alvo.Id);
    setEditId(alvo.Id);
    setModalTitulo(alvo.Titulo);
    setModalSql(alvo.SQL);
    setModalAberto(true);
  };

  const salvarModal = (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const salva = consultasService.salvarRegistro({
        Id: editId,
        Titulo: modalTitulo,
        SQL: modalSql,
      });
      setModalAberto(false);
      carregarConsultas(termo, salva.Id);
      setMensagem(
        editId === 0
          ? `Consulta '${salva.Titulo}' cadastrada com sucesso.`
          : `Consulta '${salva.Titulo}' atualizada com sucesso.`
      );
    } catch (err) {
      showDialog({
        title: 'Validação',
        message: err instanceof Error ? err.message : 'Erro ao salvar a consulta.',
        type: 'warning',
      });
    }
  };

  const excluirConsulta = (consulta?: ConsultaSalva) => {
    const alvo = consulta ?? consultaSelecionada;
    if (!alvo) {
      showDialog({
        title: 'Aviso',
        message: 'Selecione uma consulta salva para excluir.',
        type: 'info',
      });
      return;
    }

    showDialog({
      title: 'Confirmar exclusão',
      message: `Deseja realmente excluir a consulta '${alvo.Titulo}' (ID: ${alvo.Id})?`,
      type: 'confirm',
      onConfirm: () => {
        try {
          consultasService.excluir(alvo.Id);
          carregarConsultas(termo, null);
          if (tituloResultado === alvo.Titulo) {
            setResultado(null);
            setTituloResultado('Resultado');
          }
          setMensagem(`Consulta '${alvo.Titulo}' excluída com sucesso.`);
          showDialog({
            title: 'Sucesso',
            message: 'Consulta excluída com sucesso.',
            type: 'info',
          });
        } catch (err) {
          showDialog({
            title: 'Erro ao excluir consulta',
            message: err instanceof Error ? err.message : String(err),
            type: 'error',
          });
        }
      },
    });
  };

  const handleExecutarSql = async (consulta?: ConsultaSalva) => {
    const alvo = consulta ?? consultaSelecionada;
    if (!alvo) {
      showDialog({
        title: 'Aviso',
        message: 'Selecione uma consulta salva para executar.',
        type: 'warning',
      });
      return;
    }

    setSelectedId(alvo.Id);
    const query = alvo.SQL.trim();
    if (!query) {
      showDialog({
        title: 'Aviso',
        message: 'A consulta selecionada não possui comando SQL.',
        type: 'warning',
      });
      return;
    }

    setExecutando(true);
    setMensagem(`Executando "${alvo.Titulo}"...`);

    try {
      const res = await executarSql(query);
      setTituloResultado(alvo.Titulo);
      if (res.isQuery) {
        setResultado(res);
        setMensagem(
          `Consulta "${alvo.Titulo}" executada: ${res.rows.length.toLocaleString('pt-BR')} registro(s) encontrado(s).`
        );
      } else {
        setResultado(null);
        setMensagem(
          `Comando executado com sucesso. ${res.affectedRows} linha(s) afetada(s).`
        );
      }
    } catch (err) {
      setResultado(null);
      setTituloResultado(alvo.Titulo);
      setMensagem('Erro ao executar SQL.');
      showDialog({
        title: 'Erro na consulta',
        message: err instanceof Error ? err.message : String(err),
        type: 'error',
      });
    } finally {
      setExecutando(false);
    }
  };

  const handleSalvarPdf = () => {
    if (!resultado || !resultado.isQuery) {
      showDialog({
        title: 'Aviso',
        message: 'Não há resultado para salvar em PDF.',
        type: 'info',
      });
      return;
    }

    const tituloFinal = tituloResultado.trim() || 'Resultados do Banco';
    try {
      exportarTabelaPdf(resultado.columns, resultado.rows, tituloFinal);
      showDialog({
        title: 'Sucesso',
        message: `Relatório PDF '${tituloFinal}.pdf' gerado com sucesso.`,
        type: 'info',
      });
    } catch (err) {
      showDialog({
        title: 'Erro',
        message: `Erro ao salvar o relatório PDF: ${err instanceof Error ? err.message : String(err)}`,
        type: 'error',
      });
    }
  };

  const handleLimparBanco = () => {
    showDialog({
      title: 'Confirmar limpeza do banco',
      message:
        'Esta operação apagará todos os registros das tabelas Jurados, Participantes e Voto. Deseja continuar?',
      type: 'confirm',
      onConfirm: () => {
        try {
          const removidos = consultasService.limparBanco();
          setResultado(null);
          setTituloResultado('Banco limpo');
          setMensagem(
            `Limpeza concluída. ${removidos.toLocaleString('pt-BR')} registro(s) removido(s).`
          );
          showDialog({
            title: 'Limpeza concluída',
            message: 'As tabelas Jurados, Participantes e Voto foram limpas.',
            type: 'info',
          });
        } catch (err) {
          setMensagem('Não foi possível limpar o banco.');
          showDialog({
            title: 'Erro na limpeza',
            message: `Erro ao limpar as tabelas: ${err instanceof Error ? err.message : String(err)}`,
            type: 'error',
          });
        }
      },
    });
  };

  return (
    <div className="flex h-full flex-col bg-[#F8FAFC]">
      {/* Cabeçalho */}
      <div className="bg-[#1E293B] px-5 py-3.5">
        <h2 className="text-lg font-bold text-white">CONSULTAS SQL</h2>
        <p className="mt-0.5 text-xs text-[#CBD5E1]">
          Gerencie as consultas salvas e execute comandos SQL (incluindo subconsultas e JOINs) para
          visualizar os resultados.
        </p>
      </div>

      {/* Barra de ações */}
      <div className="flex flex-wrap items-center gap-2 border-b border-[#E2E8F0] bg-white px-4 py-3">
        <label htmlFor="busca-consulta" className="text-sm font-bold text-[#1E293B]">
          Buscar:
        </label>
        <input
          id="busca-consulta"
          type="text"
          value={termo}
          onChange={(e) => setTermo(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter') carregarConsultas(termo);
          }}
          placeholder="Título ou SQL..."
          className="h-8 w-52 rounded border border-[#CBD5E1] px-2.5 text-sm text-[#1E293B] focus:border-[#2563EB] focus:outline-none"
        />
        <button
          type="button"
          onClick={() => carregarConsultas(termo)}
          className="h-8 cursor-pointer rounded bg-[#475569] px-3.5 text-xs font-bold text-white hover:bg-[#334155]"
        >
          Filtrar
        </button>

        <button
          type="button"
          onClick={abrirNovaModal}
          className="h-8 cursor-pointer rounded bg-[#059669] px-3.5 text-xs font-bold text-white hover:bg-[#047857]"
        >
          + Nova Consulta
        </button>
        <button
          type="button"
          onClick={() => abrirEdicaoModal()}
          className="h-8 cursor-pointer rounded bg-[#D97706] px-3.5 text-xs font-bold text-white hover:bg-[#B45309]"
        >
          Editar
        </button>
        <button
          type="button"
          onClick={() => excluirConsulta()}
          className="h-8 cursor-pointer rounded bg-[#DC2626] px-3.5 text-xs font-bold text-white hover:bg-[#B91C1C]"
        >
          Excluir
        </button>

        <button
          type="button"
          disabled={executando || !consultaSelecionada}
          onClick={() => handleExecutarSql()}
          className="h-8 cursor-pointer rounded bg-[#2563EB] px-4 text-xs font-bold text-white hover:bg-[#1D4ED8] disabled:cursor-not-allowed disabled:opacity-50"
        >
          {executando ? 'Executando...' : 'Executar SQL'}
        </button>

        <button
          type="button"
          disabled={!resultado}
          onClick={handleSalvarPdf}
          className="h-8 cursor-pointer rounded bg-[#DC267F] px-3.5 text-xs font-bold text-white hover:bg-[#BE185D] disabled:cursor-not-allowed disabled:opacity-50"
        >
          Salvar PDF
        </button>
        <button
          type="button"
          onClick={handleLimparBanco}
          className="h-8 cursor-pointer rounded border border-[#991B1B] bg-[#B91C1C] px-3.5 text-xs font-bold text-white hover:bg-[#991B1B]"
        >
          Limpar banco
        </button>
        <button
          type="button"
          onClick={() => {
            setTermo('');
            carregarConsultas('');
          }}
          className="h-8 cursor-pointer rounded bg-[#64748B] px-3.5 text-xs font-bold text-white hover:bg-[#475569]"
        >
          Recarregar
        </button>
      </div>

      {/* Lista de Consultas Salvas */}
      <div className="flex max-h-[38%] min-h-[170px] flex-col border-b border-[#CBD5E1] bg-white">
        <div className="flex items-center justify-between border-b border-[#E2E8F0] bg-[#F8FAFC] px-4 py-1.5">
          <h3 className="text-xs font-bold uppercase tracking-wider text-[#475569]">
            Consultas Salvas ({consultas.length})
          </h3>
          {mensagem && <span className="text-xs italic text-[#334155]">{mensagem}</span>}
        </div>

        <div className="flex-1 overflow-auto">
          <table className="w-full border-collapse text-left text-sm">
            <thead>
              <tr className="sticky top-0 bg-[#1E293B] text-white">
                <th className="w-16 border-r border-[#334155] px-3 py-2 font-bold">ID</th>
                <th className="w-72 border-r border-[#334155] px-3 py-2 font-bold">Título</th>
                <th className="border-r border-[#334155] px-3 py-2 font-bold">Comando SQL</th>
                <th className="w-56 px-3 py-2 text-center font-bold">Ações</th>
              </tr>
            </thead>
            <tbody>
              {consultas.length === 0 ? (
                <tr>
                  <td colSpan={4} className="px-4 py-6 text-center text-sm text-[#64748B]">
                    Nenhuma consulta salva encontrada. Clique em &quot;+ Nova Consulta&quot; para
                    cadastrar.
                  </td>
                </tr>
              ) : (
                consultas.map((c) => {
                  const selected = c.Id === selectedId;
                  return (
                    <tr
                      key={c.Id}
                      onClick={() => setSelectedId(c.Id)}
                      onDoubleClick={() => abrirEdicaoModal(c)}
                      className={`cursor-pointer border-b border-[#E2E8F0] transition-colors ${
                        selected ? 'bg-[#DBEAFE] text-black' : 'hover:bg-[#F8FAFC]'
                      }`}
                    >
                      <td className="px-3 py-2">{c.Id}</td>
                      <td className="px-3 py-2 font-medium">{c.Titulo}</td>
                      <td className="max-w-md truncate px-3 py-2 font-mono text-xs text-[#475569]">
                        {c.SQL.replace(/\s+/g, ' ')}
                      </td>
                      <td className="px-3 py-1.5 text-center">
                        <div className="inline-flex items-center gap-1.5">
                          <button
                            type="button"
                            aria-label={`Executar "${c.Titulo}"`}
                            disabled={executando}
                            onClick={(e) => {
                              e.stopPropagation();
                              handleExecutarSql(c);
                            }}
                            className="cursor-pointer rounded bg-[#2563EB] px-2.5 py-1 text-xs font-bold text-white hover:bg-[#1D4ED8] disabled:opacity-50"
                          >
                            Executar
                          </button>
                          <button
                            type="button"
                            aria-label={`Editar "${c.Titulo}"`}
                            onClick={(e) => {
                              e.stopPropagation();
                              abrirEdicaoModal(c);
                            }}
                            className="cursor-pointer rounded bg-[#D97706] px-2.5 py-1 text-xs font-bold text-white hover:bg-[#B45309]"
                          >
                            Editar
                          </button>
                          <button
                            type="button"
                            aria-label={`Excluir "${c.Titulo}"`}
                            onClick={(e) => {
                              e.stopPropagation();
                              excluirConsulta(c);
                            }}
                            className="cursor-pointer rounded bg-[#DC2626] px-2.5 py-1 text-xs font-bold text-white hover:bg-[#B91C1C]"
                          >
                            Excluir
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Área de Resultado */}
      <div className="flex flex-1 flex-col overflow-hidden px-5 py-3">
        <div className="mb-2 flex items-center justify-between">
          <h3 className="text-sm font-bold text-[#1E293B]">{tituloResultado}</h3>
          {resultado && (
            <span className="text-xs font-medium text-[#475569]">
              {resultado.rows.length.toLocaleString('pt-BR')} registro(s)
            </span>
          )}
        </div>

        <div className="flex-1 overflow-auto rounded border border-[#E2E8F0] bg-white">
          {!resultado ? (
            <div className="p-8 text-center text-sm text-[#64748B]">
              Selecione uma consulta salva acima e clique em &quot;Executar SQL&quot; (ou em
              &quot;Executar&quot; na linha) para visualizar o resultado.
            </div>
          ) : (
            <table className="w-full border-collapse text-left text-sm">
              <thead>
                <tr className="sticky top-0 bg-[#1E293B] text-white">
                  {resultado.columns.map((col) => (
                    <th
                      key={col}
                      className="border-r border-[#334155] px-3 py-2 font-bold last:border-r-0"
                    >
                      {col}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {resultado.rows.length === 0 ? (
                  <tr>
                    <td
                      colSpan={Math.max(1, resultado.columns.length)}
                      className="px-4 py-6 text-center text-sm text-[#64748B]"
                    >
                      Nenhum registro encontrado.
                    </td>
                  </tr>
                ) : (
                  resultado.rows.map((row, idx) => (
                    <tr key={idx} className="border-b border-[#E2E8F0] hover:bg-[#F8FAFC]">
                      {resultado.columns.map((col) => (
                        <td key={col} className="px-3 py-2 text-[#1E293B]">
                          {row[col] !== null && row[col] !== undefined ? String(row[col]) : ''}
                        </td>
                      ))}
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          )}
        </div>
      </div>

      {/* Modal CRUD de Consulta */}
      {modalAberto && (
        <div
          className="fixed inset-0 z-40 flex items-center justify-center bg-black/40 p-4"
          role="dialog"
          aria-modal="true"
          aria-labelledby="modal-consulta-titulo"
        >
          <form
            onSubmit={salvarModal}
            className="w-full max-w-2xl rounded border border-[#CBD5E1] bg-[#F8FAFC] p-6 shadow-xl"
          >
            <h2 id="modal-consulta-titulo" className="mb-4 text-base font-bold text-[#1E293B]">
              {editId === 0 ? 'NOVA CONSULTA' : `EDITAR CONSULTA (ID: ${editId})`}
            </h2>

            <div className="space-y-4">
              <div>
                <label
                  htmlFor="modal-consulta-nome"
                  className="mb-1 block text-xs font-bold text-[#1E293B]"
                >
                  Título da Consulta:
                </label>
                <input
                  id="modal-consulta-nome"
                  type="text"
                  value={modalTitulo}
                  onChange={(e) => setModalTitulo(e.target.value)}
                  placeholder="Ex.: Ranking Geral com Subconsultas"
                  className="h-9 w-full rounded border border-[#CBD5E1] bg-white px-2.5 text-sm text-[#1E293B] focus:border-[#2563EB] focus:outline-none"
                  autoFocus
                />
              </div>

              <div>
                <label
                  htmlFor="modal-consulta-sql"
                  className="mb-1 block text-xs font-bold text-[#1E293B]"
                >
                  Comando SQL:
                </label>
                <textarea
                  id="modal-consulta-sql"
                  value={modalSql}
                  onChange={(e) => setModalSql(e.target.value)}
                  rows={8}
                  spellCheck={false}
                  placeholder="SELECT * FROM Participantes;"
                  className="w-full rounded border border-[#CBD5E1] bg-white p-2.5 font-mono text-xs leading-relaxed text-[#1E293B] focus:border-[#2563EB] focus:outline-none"
                />
              </div>
            </div>

            <div className="mt-6 flex justify-end gap-2.5">
              <button
                type="submit"
                className="h-8 w-28 cursor-pointer rounded bg-[#16A34A] text-xs font-bold text-white hover:bg-[#15803D]"
              >
                Salvar
              </button>
              <button
                type="button"
                onClick={() => setModalAberto(false)}
                className="h-8 w-28 cursor-pointer rounded bg-[#94A3B8] text-xs font-bold text-white hover:bg-[#64748B]"
              >
                Cancelar
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
};
