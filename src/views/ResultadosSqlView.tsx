import React, { useState } from 'react';
import { DialogConfig } from '../components/DialogModal';
import { SqlExecutionResult } from '../domain/types';
import { consultasService } from '../services/database';
import { exportarTabelaPdf } from '../services/pdfExporter';
import { executarSql } from '../services/sqlEngine';

interface ResultadosSqlViewProps {
  showDialog: (config: DialogConfig) => void;
}

const DEFAULT_TITULO = 'Resultados do Banco';
const DEFAULT_SQL = 'SELECT * FROM Participantes;';

export const ResultadosSqlView: React.FC<ResultadosSqlViewProps> = ({ showDialog }) => {
  const [titulo, setTitulo] = useState(DEFAULT_TITULO);
  const [sql, setSql] = useState(DEFAULT_SQL);
  const [mensagem, setMensagem] = useState('');
  const [tituloResultado, setTituloResultado] = useState('Resultado');
  const [resultado, setResultado] = useState<SqlExecutionResult | null>(null);
  const [executando, setExecutando] = useState(false);

  const handleExecutar = async () => {
    const query = sql.trim();
    if (!query) {
      showDialog({ title: 'Aviso', message: 'Informe um SQL para executar.', type: 'warning' });
      return;
    }

    setExecutando(true);
    setMensagem('Executando SQL...');

    try {
      const res = await executarSql(query);
      if (res.isQuery) {
        setResultado(res);
        setMensagem(`Total de registros encontrados: ${res.rows.length.toLocaleString('pt-BR')}.`);
      } else {
        setResultado(null);
        setMensagem(
          `Comando executado com sucesso. ${res.affectedRows} linha(s) afetada(s).`
        );
      }
      if (titulo.trim()) {
        setTituloResultado(titulo.trim());
      }
    } catch (err) {
      setResultado(null);
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

  const handleSalvarConsulta = () => {
    try {
      const linhas = sql.trim().replace(/\r\n/g, '\n').split('\n').length;
      consultasService.salvar(titulo, sql);
      setMensagem(`Consulta salva (${linhas.toLocaleString('pt-BR')} linhas).`);
      showDialog({
        title: 'Sucesso',
        message: 'Consulta salva com sucesso no banco de dados.',
        type: 'info',
      });
    } catch (err) {
      setMensagem('Erro ao salvar a consulta.');
      showDialog({
        title: 'Aviso',
        message: err instanceof Error ? err.message : String(err),
        type: 'warning',
      });
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

    const tituloFinal = titulo.trim() || DEFAULT_TITULO;
    if (!titulo.trim()) setTitulo(tituloFinal);

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
      {/* Cabeçalho com título */}
      <div className="bg-[#1E293B] px-5 py-4">
        <h2 className="mb-2.5 text-lg font-bold text-white">RESULTADOS SQL</h2>
        <input
          type="text"
          aria-label="Título da consulta"
          value={titulo}
          onChange={(e) => setTitulo(e.target.value)}
          placeholder="Título da consulta"
          className="h-9 w-full max-w-xl rounded border border-[#475569] bg-white px-3 text-sm text-[#1E293B] focus:border-[#2563EB] focus:outline-none"
        />
      </div>

      {/* Editor SQL */}
      <div className="border-b border-[#E2E8F0] bg-white px-5 py-4">
        <label htmlFor="sql-editor" className="mb-1.5 block text-sm font-bold text-[#1E293B]">
          SQL:
        </label>
        <textarea
          id="sql-editor"
          value={sql}
          onChange={(e) => setSql(e.target.value)}
          rows={5}
          spellCheck={false}
          className="w-full rounded border border-[#CBD5E1] bg-[#F8FAFC] p-2.5 font-mono text-xs leading-relaxed text-[#1E293B] focus:border-[#2563EB] focus:bg-white focus:outline-none"
        />

        <div className="mt-3 flex flex-wrap items-center gap-2.5">
          <button
            type="button"
            disabled={executando}
            onClick={handleExecutar}
            className="h-9 cursor-pointer rounded bg-[#2563EB] px-5 text-xs font-bold text-white hover:bg-[#1D4ED8] disabled:cursor-not-allowed disabled:opacity-50"
          >
            {executando ? 'Executando...' : 'Executar SQL'}
          </button>
          <button
            type="button"
            onClick={handleSalvarConsulta}
            className="h-9 cursor-pointer rounded bg-[#10B981] px-5 text-xs font-bold text-white hover:bg-[#059669]"
          >
            Salvar
          </button>
          <button
            type="button"
            disabled={!resultado}
            onClick={handleSalvarPdf}
            className="h-9 cursor-pointer rounded bg-[#DC267F] px-5 text-xs font-bold text-white hover:bg-[#BE185D] disabled:cursor-not-allowed disabled:opacity-50"
          >
            Salvar PDF
          </button>
          <button
            type="button"
            onClick={handleLimparBanco}
            className="h-9 cursor-pointer rounded border border-[#991B1B] bg-[#B91C1C] px-5 text-xs font-bold text-white hover:bg-[#991B1B]"
          >
            Limpar banco
          </button>
        </div>

        {mensagem && <p className="mt-2 text-xs italic text-[#374151]">{mensagem}</p>}
      </div>

      {/* Resultado */}
      <div className="flex flex-1 flex-col overflow-hidden px-5 py-3">
        <h3 className="mb-2 text-sm font-bold text-[#1E293B]">{tituloResultado}</h3>
        <div className="flex-1 overflow-auto rounded border border-[#E2E8F0] bg-white">
          {!resultado ? (
            <div className="p-8 text-center text-sm text-[#64748B]">
              Execute uma consulta SQL para visualizar os resultados.
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
    </div>
  );
};
