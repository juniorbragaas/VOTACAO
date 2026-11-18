import React, { useEffect, useState } from 'react';
import { DialogConfig } from '../components/DialogModal';
import { Criterio } from '../domain/types';
import { criterioService } from '../services/database';

interface CriteriosViewProps {
  showDialog: (config: DialogConfig) => void;
}

const REGISTROS_POR_PAGINA = 10;

export const CriteriosView: React.FC<CriteriosViewProps> = ({ showDialog }) => {
  const [todos, setTodos] = useState<Criterio[]>([]);
  const [termo, setTermo] = useState('');
  const [paginaAtual, setPaginaAtual] = useState(1);
  const [selectedId, setSelectedId] = useState<number | null>(null);

  const [modalAberto, setModalAberto] = useState(false);
  const [editId, setEditId] = useState(0);
  const [nome, setNome] = useState('');

  const carregar = (filtro = '') => {
    const lista = criterioService.pesquisar(filtro);
    setTodos(lista);
    setPaginaAtual(1);
    setSelectedId(null);
  };

  useEffect(() => {
    carregar();
  }, []);

  const totalPaginas = Math.max(1, Math.ceil(todos.length / REGISTROS_POR_PAGINA));
  const paginaSegura = Math.min(Math.max(1, paginaAtual), totalPaginas);
  const indiceInicio = (paginaSegura - 1) * REGISTROS_POR_PAGINA;
  const indiceFim = Math.min(indiceInicio + REGISTROS_POR_PAGINA, todos.length);
  const itensPagina = todos.slice(indiceInicio, indiceFim);
  const primeiroReg = todos.length === 0 ? 0 : indiceInicio + 1;

  const abrirNovo = () => {
    setEditId(0);
    setNome('');
    setModalAberto(true);
  };

  const abrirEdicao = () => {
    if (selectedId === null) {
      showDialog({ title: 'Aviso', message: 'Selecione um critério para editar.', type: 'info' });
      return;
    }
    const c = criterioService.obterPorId(selectedId);
    if (!c) {
      showDialog({ title: 'Aviso', message: 'Critério não encontrado.', type: 'info' });
      return;
    }
    setEditId(c.Id);
    setNome(c.Nome);
    setModalAberto(true);
  };

  const salvarModal = (e: React.FormEvent) => {
    e.preventDefault();
    try {
      criterioService.salvar({ Id: editId, Nome: nome });
      setModalAberto(false);
      carregar(termo);
    } catch (err) {
      showDialog({
        title: 'Validação',
        message: err instanceof Error ? err.message : 'Erro ao salvar critério.',
        type: 'warning',
      });
    }
  };

  const excluirSelecionado = () => {
    if (selectedId === null) {
      showDialog({ title: 'Aviso', message: 'Selecione um critério para excluir.', type: 'info' });
      return;
    }
    const c = todos.find((item) => item.Id === selectedId);
    if (!c) return;

    showDialog({
      title: 'Confirmar exclusão',
      message: `Deseja excluir o critério '${c.Nome}'?`,
      type: 'confirm',
      onConfirm: () => {
        try {
          criterioService.excluir(c.Id);
          carregar(termo);
          showDialog({ title: 'Sucesso', message: 'Critério excluído com sucesso.', type: 'info' });
        } catch (err) {
          showDialog({
            title: 'Erro ao excluir critério',
            message: err instanceof Error ? err.message : String(err),
            type: 'error',
          });
        }
      },
    });
  };

  return (
    <div className="flex h-full flex-col bg-[#F8FAFC]">
      <div className="flex flex-wrap items-center gap-2.5 border-b border-[#E2E8F0] bg-white px-4 py-3.5">
        <label className="text-sm font-bold text-[#1E293B]">Buscar:</label>
        <input
          type="text"
          value={termo}
          onChange={(e) => setTermo(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter') carregar(termo);
          }}
          placeholder="Nome do critério..."
          className="h-8 w-64 rounded border border-[#CBD5E1] px-2.5 text-sm text-[#1E293B] focus:border-[#2563EB] focus:outline-none"
        />
        <button
          type="button"
          onClick={() => carregar(termo)}
          className="h-8 cursor-pointer rounded bg-[#2563EB] px-4 text-xs font-bold text-white hover:bg-[#1D4ED8]"
        >
          Filtrar
        </button>
        <button
          type="button"
          onClick={abrirNovo}
          className="h-8 cursor-pointer rounded bg-[#059669] px-4 text-xs font-bold text-white hover:bg-[#047857]"
        >
          + Novo Critério
        </button>
        <button
          type="button"
          onClick={abrirEdicao}
          className="h-8 cursor-pointer rounded bg-[#D97706] px-4 text-xs font-bold text-white hover:bg-[#B45309]"
        >
          Editar
        </button>
        <button
          type="button"
          onClick={excluirSelecionado}
          className="h-8 cursor-pointer rounded bg-[#DC2626] px-4 text-xs font-bold text-white hover:bg-[#B91C1C]"
        >
          Excluir
        </button>
        <button
          type="button"
          onClick={() => {
            setTermo('');
            carregar('');
          }}
          className="h-8 cursor-pointer rounded bg-[#64748B] px-4 text-xs font-bold text-white hover:bg-[#475569]"
        >
          Recarregar
        </button>
      </div>

      <div className="flex-1 overflow-auto bg-white">
        <table className="w-full border-collapse text-left text-sm">
          <thead>
            <tr className="bg-[#1E293B] text-white">
              <th className="w-20 border-r border-[#334155] px-3 py-2.5 font-bold">ID</th>
              <th className="px-3 py-2.5 font-bold">Nome</th>
            </tr>
          </thead>
          <tbody>
            {itensPagina.length === 0 ? (
              <tr>
                <td colSpan={2} className="px-4 py-8 text-center text-sm text-[#64748B]">
                  Nenhum critério cadastrado.
                </td>
              </tr>
            ) : (
              itensPagina.map((c) => {
                const selected = c.Id === selectedId;
                return (
                  <tr
                    key={c.Id}
                    onClick={() => setSelectedId(c.Id)}
                    onDoubleClick={() => {
                      setSelectedId(c.Id);
                      setEditId(c.Id);
                      setNome(c.Nome);
                      setModalAberto(true);
                    }}
                    className={`cursor-pointer border-b border-[#E2E8F0] transition-colors ${
                      selected ? 'bg-[#DBEAFE] text-black' : 'hover:bg-[#F8FAFC]'
                    }`}
                  >
                    <td className="px-3 py-2">{c.Id}</td>
                    <td className="px-3 py-2 font-medium">{c.Nome}</td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>

      <div className="flex h-11 items-center justify-between border-t border-[#E2E8F0] bg-white px-4">
        <button
          type="button"
          disabled={paginaSegura <= 1}
          onClick={() => setPaginaAtual((p) => Math.max(1, p - 1))}
          className="h-8 cursor-pointer rounded bg-[#F1F5F9] px-3.5 text-xs font-bold text-[#475569] hover:bg-[#E2E8F0] disabled:cursor-not-allowed disabled:opacity-50"
        >
          ◀ Anterior
        </button>
        <span className="text-xs text-[#475569]">
          Página {paginaSegura} de {totalPaginas} • {primeiroReg}-{indiceFim} de {todos.length}{' '}
          registros
        </span>
        <button
          type="button"
          disabled={paginaSegura >= totalPaginas}
          onClick={() => setPaginaAtual((p) => Math.min(totalPaginas, p + 1))}
          className="h-8 cursor-pointer rounded bg-[#F1F5F9] px-3.5 text-xs font-bold text-[#475569] hover:bg-[#E2E8F0] disabled:cursor-not-allowed disabled:opacity-50"
        >
          Próxima ▶
        </button>
      </div>

      {modalAberto && (
        <div className="fixed inset-0 z-40 flex items-center justify-center bg-black/40 p-4">
          <form
            onSubmit={salvarModal}
            className="w-full max-w-md rounded border border-[#CBD5E1] bg-[#F8FAFC] p-6 shadow-xl"
          >
            <h2 className="mb-5 text-base font-bold text-[#1E293B]">
              {editId === 0 ? 'NOVO CRITÉRIO' : `EDITAR CRITÉRIO (ID: ${editId})`}
            </h2>

            <div>
              <label className="mb-1 block text-xs font-bold text-[#1E293B]">Nome:</label>
              <input
                type="text"
                value={nome}
                onChange={(e) => setNome(e.target.value)}
                className="h-9 w-full rounded border border-[#CBD5E1] bg-white px-2.5 text-sm focus:border-[#2563EB] focus:outline-none"
                autoFocus
              />
            </div>

            <div className="mt-6 flex justify-end gap-2.5">
              <button
                type="submit"
                className="h-8 w-24 cursor-pointer rounded bg-[#16A34A] text-xs font-bold text-white hover:bg-[#15803D]"
              >
                Salvar
              </button>
              <button
                type="button"
                onClick={() => setModalAberto(false)}
                className="h-8 w-24 cursor-pointer rounded bg-[#94A3B8] text-xs font-bold text-white hover:bg-[#64748B]"
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
