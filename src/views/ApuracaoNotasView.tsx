import React, { useEffect, useState } from 'react';
import { DialogConfig } from '../components/DialogModal';
import { Criterio, Jurado, Participante } from '../domain/types';
import {
  apuracaoNotasService,
  criterioService,
  juradoService,
  participanteService,
} from '../services/database';

interface ApuracaoNotasViewProps {
  showDialog: (config: DialogConfig) => void;
}

export const ApuracaoNotasView: React.FC<ApuracaoNotasViewProps> = ({ showDialog }) => {
  const [jurados, setJurados] = useState<Jurado[]>([]);
  const [participantes, setParticipantes] = useState<Participante[]>([]);
  const [criterios, setCriterios] = useState<Criterio[]>([]);
  const [juradoId, setJuradoId] = useState<number>(0);
  const [notas, setNotas] = useState<Record<string, string>>({});
  const [status, setStatus] = useState('Carregando jurados e critérios...');

  const carregarNotasJurado = (
    idJurado: number,
    listaJurados: Jurado[],
    listaParticipantes: Participante[],
    listaCriterios: Criterio[]
  ) => {
    if (!idJurado) {
      setNotas({});
      return;
    }
    const registros = apuracaoNotasService.obterNotasPorJurado(idJurado);
    const mapa: Record<string, string> = {};
    for (const p of listaParticipantes) {
      for (const c of listaCriterios) {
        const encontrado = registros.find(
          (r) => r.IdParticipante === p.Id && r.IdCriterio === c.Id
        );
        mapa[`${p.Id}_${c.Id}`] = String(encontrado ? encontrado.Nota : 0);
      }
    }
    setNotas(mapa);
    const juradoAtual = listaJurados.find((j) => j.Id === idJurado);
    if (juradoAtual) {
      setStatus(
        `Avaliando: ${juradoAtual.Nome} | ${listaParticipantes.length} participantes | ${listaCriterios.length} critérios`
      );
    }
  };

  useEffect(() => {
    apuracaoNotasService.garantirNotasEmBranco();
    const listaJurados = juradoService.obterTodos();
    const listaParticipantes = participanteService.obterTodos();
    const listaCriterios = criterioService.obterTodos();

    setJurados(listaJurados);
    setParticipantes(listaParticipantes);
    setCriterios(listaCriterios);

    if (listaJurados.length > 0) {
      const primeiroId = listaJurados[0].Id;
      setJuradoId(primeiroId);
      carregarNotasJurado(primeiroId, listaJurados, listaParticipantes, listaCriterios);
    } else {
      setJuradoId(0);
      setNotas({});
      setStatus(
        `${listaJurados.length} jurados, ${listaParticipantes.length} participantes e ${listaCriterios.length} critérios carregados.`
      );
    }
  }, []);

  const handleTrocarJurado = (novoId: number) => {
    setJuradoId(novoId);
    carregarNotasJurado(novoId, jurados, participantes, criterios);
  };

  const handleChangeNota = (idParticipante: number, idCriterio: number, valor: string) => {
    if (valor !== '' && !/^-?\d*$/.test(valor)) return;
    setNotas((prev) => ({
      ...prev,
      [`${idParticipante}_${idCriterio}`]: valor,
    }));
  };

  const handleSalvar = () => {
    if (!juradoId) {
      showDialog({
        title: 'Aviso',
        message: 'Selecione um jurado antes de salvar.',
        type: 'info',
      });
      return;
    }

    try {
      const payload: Array<{ IdParticipante: number; IdCriterio: number; Nota: number }> = [];
      for (const p of participantes) {
        for (const c of criterios) {
          const raw = (notas[`${p.Id}_${c.Id}`] ?? '').trim();
          const parsed = raw === '' ? 0 : parseInt(raw, 10);
          payload.push({
            IdParticipante: p.Id,
            IdCriterio: c.Id,
            Nota: Number.isNaN(parsed) ? 0 : parsed,
          });
        }
      }
      apuracaoNotasService.salvarNotas(juradoId, payload);
      carregarNotasJurado(juradoId, jurados, participantes, criterios);
      showDialog({
        title: 'Sucesso',
        message: 'Notas salvas com sucesso.',
        type: 'info',
      });
    } catch (err) {
      showDialog({
        title: 'Erro ao salvar notas',
        message: err instanceof Error ? err.message : String(err),
        type: 'error',
      });
    }
  };

  const handleZerar = () => {
    showDialog({
      title: 'Zerar notas',
      message: 'Deseja apagar todas as avaliações e recriar o painel com zero?',
      type: 'confirm',
      onConfirm: () => {
        try {
          apuracaoNotasService.zerarTodasNotas();
          carregarNotasJurado(juradoId, jurados, participantes, criterios);
          showDialog({
            title: 'Sucesso',
            message: 'Todas as notas foram zeradas com sucesso.',
            type: 'info',
          });
        } catch (err) {
          showDialog({
            title: 'Erro ao zerar notas',
            message: err instanceof Error ? err.message : String(err),
            type: 'error',
          });
        }
      },
    });
  };

  return (
    <div className="flex h-full flex-col bg-[#F8FAFC]">
      {/* Header escuro */}
      <div className="bg-[#1E293B] px-5 py-4">
        <h2 className="text-lg font-bold tracking-wide text-white">APURAÇÃO DE NOTAS</h2>
        <p className="mt-1 text-xs text-[#CBD5E1]">{status}</p>
      </div>

      {/* Filtro de jurado */}
      <div className="flex flex-wrap items-center gap-3 border-b border-[#E2E8F0] bg-white px-5 py-4">
        <label className="text-sm font-bold text-[#1E293B]">Selecione o jurado:</label>
        <select
          value={juradoId}
          onChange={(e) => handleTrocarJurado(Number(e.target.value))}
          className="h-9 w-full max-w-sm rounded border border-[#CBD5E1] bg-white px-3 text-sm text-[#1E293B] focus:border-[#2563EB] focus:outline-none"
        >
          {jurados.length === 0 ? (
            <option value={0}>Nenhum jurado cadastrado</option>
          ) : (
            jurados.map((j) => (
              <option key={j.Id} value={j.Id}>
                {j.Nome}
              </option>
            ))
          )}
        </select>
      </div>

      {/* Grid editável */}
      <div className="flex-1 overflow-auto bg-white">
        {participantes.length === 0 || criterios.length === 0 ? (
          <div className="p-8 text-center text-sm text-[#64748B]">
            Cadastre participantes, jurados e critérios para iniciar a apuração de notas.
          </div>
        ) : (
          <table className="w-full border-collapse text-sm">
            <thead>
              <tr className="bg-[#1E293B] text-white">
                <th className="min-w-[220px] border-r border-[#334155] px-3 py-2.5 text-left font-bold">
                  Participante
                </th>
                {criterios.map((c) => (
                  <th
                    key={c.Id}
                    className="min-w-[115px] border-r border-[#334155] px-3 py-2.5 text-center font-bold last:border-r-0"
                  >
                    {c.Id} - {c.Nome}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {participantes.map((p) => (
                <tr key={p.Id} className="border-b border-[#E2E8F0] hover:bg-[#F8FAFC]">
                  <td className="border-r border-[#E2E8F0] px-3 py-2 font-medium text-[#1E293B]">
                    {p.Nome}
                  </td>
                  {criterios.map((c) => (
                    <td
                      key={c.Id}
                      className="border-r border-[#E2E8F0] p-1 text-center last:border-r-0"
                    >
                      <input
                        type="text"
                        inputMode="numeric"
                        value={notas[`${p.Id}_${c.Id}`] ?? '0'}
                        onChange={(e) => handleChangeNota(p.Id, c.Id, e.target.value)}
                        onFocus={(e) => e.target.select()}
                        className="h-8 w-20 rounded border border-transparent bg-transparent text-center text-sm text-[#1E293B] hover:border-[#CBD5E1] focus:border-[#2563EB] focus:bg-[#EFF6FF] focus:outline-none"
                      />
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* Rodapé de ações */}
      <div className="flex items-center justify-end gap-2.5 border-t border-[#E2E8F0] bg-white px-5 py-3">
        <button
          type="button"
          onClick={handleZerar}
          className="h-8 cursor-pointer rounded bg-[#94A3B8] px-4 text-xs font-bold text-white hover:bg-[#64748B]"
        >
          Zerar notas
        </button>
        <button
          type="button"
          onClick={handleSalvar}
          className="h-8 cursor-pointer rounded bg-[#16A34A] px-5 text-xs font-bold text-white hover:bg-[#15803D]"
        >
          Salvar notas
        </button>
      </div>
    </div>
  );
};
