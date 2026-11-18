import React, { useEffect, useState } from 'react';
import { DialogConfig } from '../components/DialogModal';
import { ConsultaSalva, SqlExecutionResult } from '../domain/types';
import { consultasService } from '../services/database';
import { exportarTabelaPdf } from '../services/pdfExporter';
import { executarSql } from '../services/sqlEngine';

interface DiculgacaoViewProps {
  showDialog: (config: DialogConfig) => void;
}

export const DiculgacaoView: React.FC<DiculgacaoViewProps> = ({ showDialog }) => {
  const [consultas, setConsultas] = useState<ConsultaSalva[]>([]);
  const [selectedId, setSelectedId] = useState<number>(0);
  const [tituloConsulta, setTituloConsulta] = useState('Resultados Votação');
  const [mensagem, setMensagem] = useState('');
  const [resultado, setResultado] = useState<SqlExecutionResult | null>(null);
  const [executando, setExecutando] = useState(false);

  useEffect(() => {
    const lista = consultasService.obterTodas();
    setConsultas(lista);
    if (lista.length > 0) {
      setSelectedId(lista[0].Id);
      setTituloConsulta(lista[0].Titulo);
      setMensagem('Clique em Consultar para executar o SQL.');
    } else {
      setSelectedId(0);
      setTituloConsulta('Resultados Votação');
      setMensagem(
        'Nenhuma consulta salva. Salve uma consulta na tela Consultas para exibir os resultados aqui.'
      );
    }
    setResultado(null);
  }, []);

  const consultaAtual = consultas.find((c) => c.Id === selectedId);

  const handleSelecionar = (id: number) => {
    setSelectedId(id);
    const encontrada = consultas.find((c) => c.Id === id);
    if (encontrada) {
      setTituloConsulta(encontrada.Titulo);
      setMensagem('Clique em Consultar para executar o SQL.');
    }
    setResultado(null);
  };

  const handleConsultar = async () => {
    if (!consultaAtual) {
      setMensagem('Selecione uma consulta salva.');
      return;
    }
    if (!consultaAtual.SQL.trim()) {
      setResultado(null);
      setMensagem('A consulta selecionada não contém um comando SQL.');
      return;
    }

    setExecutando(true);
    setMensagem('Consultando resultado...');
    try {
      const res = await executarSql(consultaAtual.SQL);
      setTituloConsulta(consultaAtual.Titulo);
      setResultado(res);
      setMensagem(`Resultado da consulta: ${res.rows.length} registro(s)`);
    } catch (err) {
      setResultado(null);
      setMensagem('Erro ao executar a consulta salva.');
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
        message: 'Não há dados para exportar em PDF.',
        type: 'info',
      });
      return;
    }

    const tituloFinal = tituloConsulta.trim() || 'Consulta';
    try {
      exportarTabelaPdf(resultado.columns, resultado.rows, tituloFinal);
      showDialog({
        title: 'Sucesso',
        message: `Arquivo '${tituloFinal}.pdf' gerado com sucesso.`,
        type: 'info',
      });
    } catch (err) {
      showDialog({
        title: 'Erro',
        message: `Erro ao gerar o PDF: ${err instanceof Error ? err.message : String(err)}`,
        type: 'error',
      });
    }
  };

  return (
    <div className="flex h-full flex-col bg-[#F8FAFC]">
      {/* Cabeçalho */}
      <div className="bg-[#1E293B] px-5 py-5">
        <h2 className="text-lg font-bold text-white">Resultados Votação</h2>
      </div>

      {/* Seleção de consulta */}
      <div className="border-b border-[#E2E8F0] bg-white px-5 py-4">
        <div className="flex flex-wrap items-center gap-3">
          <label htmlFor="consulta-salva-select" className="text-sm font-bold text-[#1E293B]">
            Consulta salva:
          </label>
          <select
            id="consulta-salva-select"
            value={selectedId}
            onChange={(e) => handleSelecionar(Number(e.target.value))}
            className="h-9 w-full max-w-lg rounded border border-[#CBD5E1] bg-white px-3 text-sm text-[#1E293B] focus:border-[#2563EB] focus:outline-none"
          >
            {consultas.length === 0 ? (
              <option value={0}>Nenhuma consulta salva</option>
            ) : (
              consultas.map((c) => (
                <option key={c.Id} value={c.Id}>
                  {c.Titulo}
                </option>
              ))
            )}
          </select>

          <button
            type="button"
            disabled={executando}
            onClick={handleConsultar}
            className="h-9 cursor-pointer rounded bg-[#2563EB] px-6 text-xs font-bold text-white hover:bg-[#1D4ED8] disabled:cursor-not-allowed disabled:opacity-50"
          >
            {executando ? 'Consultando...' : 'Consultar'}
          </button>

          <button
            type="button"
            disabled={!resultado}
            onClick={handleSalvarPdf}
            className="h-9 cursor-pointer rounded bg-[#DC267F] px-5 text-xs font-bold text-white hover:bg-[#BE185D] disabled:cursor-not-allowed disabled:opacity-50"
          >
            Salvar PDF
          </button>
        </div>

        <p className="mt-3 text-sm font-bold text-[#1E293B]">{tituloConsulta}</p>
        {mensagem && <p className="mt-1 text-xs italic text-[#374151]">{mensagem}</p>}
      </div>

      {/* Grid de resultados */}
      <div className="flex-1 overflow-hidden p-5">
        <div className="h-full overflow-auto rounded border border-[#E2E8F0] bg-white">
          {!resultado ? (
            <div className="p-8 text-center text-sm text-[#64748B]">
              Selecione uma consulta salva e clique em &quot;Consultar&quot; para exibir os
              resultados da votação.
            </div>
          ) : (
            <table className="w-full border-collapse text-left text-sm">
              <thead>
                <tr className="bg-[#1E293B] text-white">
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
                      Nenhum registro retornado.
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
    </div>
  );
};
