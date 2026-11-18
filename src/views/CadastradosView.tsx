import React, { useEffect, useState } from 'react';
import { DialogConfig } from '../components/DialogModal';
import { CategoriaParticipante, formatarCategoria, Participante } from '../domain/types';
import { participanteService } from '../services/database';

interface CadastradosViewProps {
  showDialog: (config: DialogConfig) => void;
}

const REGISTROS_POR_PAGINA = 10;

function formatarDataBr(isoDate: string): string {
  if (!isoDate) return '';
  const parts = isoDate.split('-');
  if (parts.length !== 3) return isoDate;
  return `${parts[2]}/${parts[1]}/${parts[0]}`;
}

export const CadastradosView: React.FC<CadastradosViewProps> = ({ showDialog }) => {
  const [todos, setTodos] = useState<Participante[]>([]);
  const [termo, setTermo] = useState('');
  const [paginaAtual, setPaginaAtual] = useState(1);
  const [selectedId, setSelectedId] = useState<number | null>(null);

  const [modalAberto, setModalAberto] = useState(false);
  const [editId, setEditId] = useState(0);
  const [nome, setNome] = useState('');
  const [dataNascimento, setDataNascimento] = useState('');
  const [categoria, setCategoria] = useState<CategoriaParticipante>(CategoriaParticipante.Adulto);

  const carregar = (filtro = '') => {
    const lista = participanteService.pesquisar(filtro);
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
    setDataNascimento(new Date().toISOString().slice(0, 10));
    setCategoria(CategoriaParticipante.Adulto);
    setModalAberto(true);
  };

  const abrirEdicao = () => {
    if (selectedId === null) {
      showDialog({
        title: 'Atenção',
        message: 'Selecione um participante para editar.',
        type: 'info',
      });
      return;
    }
    const p = participanteService.obterPorId(selectedId);
    if (!p) {
      showDialog({ title: 'Aviso', message: 'Participante não encontrado.', type: 'info' });
      return;
    }
    setEditId(p.Id);
    setNome(p.Nome);
    setDataNascimento(p.DataNascimento);
    setCategoria(p.Categoria);
    setModalAberto(true);
  };

  const salvarModal = (e: React.FormEvent) => {
    e.preventDefault();
    try {
      participanteService.salvar({
        Id: editId,
        Nome: nome,
        DataNascimento: dataNascimento,
        Categoria: categoria,
      });
      setModalAberto(false);
      carregar(termo);
    } catch (err) {
      showDialog({
        title: 'Validação',
        message: err instanceof Error ? err.message : 'Erro ao salvar participante.',
        type: 'warning',
      });
    }
  };

  const excluirSelecionado = () => {
    if (selectedId === null) {
      showDialog({
        title: 'Atenção',
        message: 'Selecione um participante para excluir.',
        type: 'info',
      });
      return;
    }
    const p = todos.find((item) => item.Id === selectedId);
    if (!p) return;

    showDialog({
      title: 'Confirmação de Exclusão',
      message: `Deseja realmente excluir o participante '${p.Nome}' (ID: ${p.Id})?`,
      type: 'confirm',
      onConfirm: () => {
        try {
          participanteService.excluir(p.Id);
          carregar(termo);
          showDialog({
            title: 'Sucesso',
            message: 'Participante excluído com sucesso!',
            type: 'info',
          });
        } catch (err) {
          showDialog({
            title: 'Erro',
            message: `Erro ao excluir participante: ${err instanceof Error ? err.message : String(err)}`,
            type: 'error',
          });
        }
      },
    });
  };

  return (
    <div className="flex h-full flex-col bg-[#F8FAFC]">
      {/* Barra de ações */}
      <div className="flex flex-wrap items-center gap-2.5 border-b border-[#E2E8F0] bg-white px-4 py-3.5">
        <label className="text-sm font-bold text-[#1E293B]">Buscar:</label>
        <input
          type="text"
          value={termo}
          onChange={(e) => setTermo(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter') carregar(termo);
          }}
          placeholder="Nome ou categoria..."
          className="h-8 w-56 rounded border border-[#CBD5E1] px-2.5 text-sm text-[#1E293B] focus:border-[#2563EB] focus:outline-none"
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
          + Novo Participante
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

      {/* Grid */}
      <div className="flex-1 overflow-auto bg-white">
        <table className="w-full border-collapse text-left text-sm">
          <thead>
            <tr className="bg-[#1E293B] text-white">
              <th className="w-20 border-r border-[#334155] px-3 py-2.5 font-bold">ID</th>
              <th className="border-r border-[#334155] px-3 py-2.5 font-bold">
                Nome do Participante
              </th>
              <th className="w-48 border-r border-[#334155] px-3 py-2.5 font-bold">
                Data de Nascimento
              </th>
              <th className="w-36 px-3 py-2.5 font-bold">Categoria</th>
            </tr>
          </thead>
          <tbody>
            {itensPagina.length === 0 ? (
              <tr>
                <td colSpan={4} className="px-4 py-8 text-center text-sm text-[#64748B]">
                  Nenhum participante cadastrado.
                </td>
              </tr>
            ) : (
              itensPagina.map((p) => {
                const selected = p.Id === selectedId;
                return (
                  <tr
                    key={p.Id}
                    onClick={() => setSelectedId(p.Id)}
                    onDoubleClick={() => {
                      setSelectedId(p.Id);
                      setEditId(p.Id);
                      setNome(p.Nome);
                      setDataNascimento(p.DataNascimento);
                      setCategoria(p.Categoria);
                      setModalAberto(true);
                    }}
                    className={`cursor-pointer border-b border-[#E2E8F0] transition-colors ${
                      selected ? 'bg-[#DBEAFE] text-black' : 'hover:bg-[#F8FAFC]'
                    }`}
                  >
                    <td className="px-3 py-2">{p.Id}</td>
                    <td className="px-3 py-2 font-medium">{p.Nome}</td>
                    <td className="px-3 py-2">{formatarDataBr(p.DataNascimento)}</td>
                    <td className="px-3 py-2">{formatarCategoria(p.Categoria)}</td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>

      {/* Paginação */}
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

      {/* Modal Novo / Editar */}
      {modalAberto && (
        <div className="fixed inset-0 z-40 flex items-center justify-center bg-black/40 p-4">
          <form
            onSubmit={salvarModal}
            className="w-full max-w-xl rounded border border-[#CBD5E1] bg-[#F8FAFC] p-6 shadow-xl"
          >
            <h2 className="mb-5 text-base font-bold text-[#1E293B]">
              {editId === 0 ? 'NOVO PARTICIPANTE' : `EDITAR PARTICIPANTE (ID: ${editId})`}
            </h2>

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-12">
              <div className="sm:col-span-5">
                <label className="mb-1 block text-xs font-bold text-[#1E293B]">
                  Nome Completo:
                </label>
                <input
                  type="text"
                  value={nome}
                  onChange={(e) => setNome(e.target.value)}
                  className="h-9 w-full rounded border border-[#CBD5E1] bg-white px-2.5 text-sm focus:border-[#2563EB] focus:outline-none"
                  autoFocus
                />
              </div>
              <div className="sm:col-span-4">
                <label className="mb-1 block text-xs font-bold text-[#1E293B]">
                  Data de Nascimento:
                </label>
                <input
                  type="date"
                  max={new Date().toISOString().slice(0, 10)}
                  value={dataNascimento}
                  onChange={(e) => setDataNascimento(e.target.value)}
                  className="h-9 w-full rounded border border-[#CBD5E1] bg-white px-2.5 text-sm focus:border-[#2563EB] focus:outline-none"
                />
              </div>
              <div className="sm:col-span-3">
                <label className="mb-1 block text-xs font-bold text-[#1E293B]">Categoria:</label>
                <select
                  value={categoria}
                  onChange={(e) => setCategoria(Number(e.target.value) as CategoriaParticipante)}
                  className="h-9 w-full rounded border border-[#CBD5E1] bg-white px-2 text-sm focus:border-[#2563EB] focus:outline-none"
                >
                  <option value={CategoriaParticipante.Adulto}>Adulto</option>
                  <option value={CategoriaParticipante.Teen}>Teen</option>
                </select>
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
