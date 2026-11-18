import React, { useEffect, useRef, useState } from 'react';
import { DialogConfig, DialogModal } from './components/DialogModal';
import { ApuracaoNotasView } from './views/ApuracaoNotasView';
import { CadastradosView } from './views/CadastradosView';
import { CriteriosView } from './views/CriteriosView';
import { DiculgacaoView } from './views/DiculgacaoView';
import { JuradosView } from './views/JuradosView';
import { ResultadosSqlView } from './views/ResultadosSqlView';
import { SobreView } from './views/SobreView';

type ActiveView =
  | 'sobre'
  | 'participantes'
  | 'jurados'
  | 'criterios'
  | 'apuracao'
  | 'consultas'
  | 'divulgacao';

export function App() {
  const [activeView, setActiveView] = useState<ActiveView>('sobre');
  const [cadastrosOpen, setCadastrosOpen] = useState(false);
  const [dialog, setDialog] = useState<DialogConfig | null>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setCadastrosOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const isCadastrosActive =
    activeView === 'participantes' || activeView === 'jurados' || activeView === 'criterios';

  const navBtnClass = (active: boolean) =>
    `h-9 min-w-[155px] cursor-pointer rounded px-4 text-sm font-bold transition-colors ${
      active
        ? 'bg-[#EEF2FF] text-[#2563EB]'
        : 'bg-white text-[#475569] hover:bg-[#F8FAFC] hover:text-[#1E293B]'
    }`;

  return (
    <div className="flex h-screen w-screen flex-col overflow-hidden bg-[#F8FAFC]">
      {/* 1. CABEÇALHO */}
      <header className="flex h-[78px] shrink-0 flex-col items-center justify-center bg-[#1B365D] px-4 text-center">
        <div className="text-xl font-bold tracking-wide text-white">AVALIAÇÃO DE MODELOS</div>
        <div className="mt-0.5 text-xs tracking-wider text-[#CBD5E1]">
          SISTEMA INTEGRADO DE GESTÃO E AVALIAÇÃO
        </div>
      </header>

      {/* 2. MENU HORIZONTAL */}
      <nav className="relative z-30 flex min-h-[52px] shrink-0 flex-wrap items-center gap-2 border-b border-[#E2E8F0] bg-white px-4 py-1.5">
        <button
          type="button"
          onClick={() => {
            setActiveView('sobre');
            setCadastrosOpen(false);
          }}
          className={navBtnClass(activeView === 'sobre')}
        >
          Sobre o Sistema
        </button>

        <div className="relative" ref={dropdownRef}>
          <button
            type="button"
            onClick={() => setCadastrosOpen((o) => !o)}
            className={navBtnClass(isCadastrosActive)}
          >
            Cadastros ▾
          </button>
          {cadastrosOpen && (
            <div className="absolute left-0 top-full mt-1 w-48 rounded border border-[#E2E8F0] bg-white py-1 shadow-lg">
              <button
                type="button"
                onClick={() => {
                  setActiveView('participantes');
                  setCadastrosOpen(false);
                }}
                className={`block w-full cursor-pointer px-4 py-2 text-left text-sm ${
                  activeView === 'participantes'
                    ? 'bg-[#EEF2FF] font-bold text-[#2563EB]'
                    : 'text-[#334155] hover:bg-[#F1F5F9]'
                }`}
              >
                Participantes
              </button>
              <button
                type="button"
                onClick={() => {
                  setActiveView('jurados');
                  setCadastrosOpen(false);
                }}
                className={`block w-full cursor-pointer px-4 py-2 text-left text-sm ${
                  activeView === 'jurados'
                    ? 'bg-[#EEF2FF] font-bold text-[#2563EB]'
                    : 'text-[#334155] hover:bg-[#F1F5F9]'
                }`}
              >
                Jurados
              </button>
              <button
                type="button"
                onClick={() => {
                  setActiveView('criterios');
                  setCadastrosOpen(false);
                }}
                className={`block w-full cursor-pointer px-4 py-2 text-left text-sm ${
                  activeView === 'criterios'
                    ? 'bg-[#EEF2FF] font-bold text-[#2563EB]'
                    : 'text-[#334155] hover:bg-[#F1F5F9]'
                }`}
              >
                Critérios
              </button>
            </div>
          )}
        </div>

        <button
          type="button"
          onClick={() => {
            setActiveView('apuracao');
            setCadastrosOpen(false);
          }}
          className={navBtnClass(activeView === 'apuracao')}
        >
          Apuração de Notas
        </button>

        <button
          type="button"
          onClick={() => {
            setActiveView('consultas');
            setCadastrosOpen(false);
          }}
          className={navBtnClass(activeView === 'consultas')}
        >
          Consultas
        </button>

        <button
          type="button"
          onClick={() => {
            setActiveView('divulgacao');
            setCadastrosOpen(false);
          }}
          className={navBtnClass(activeView === 'divulgacao')}
        >
          Resultados Votação
        </button>
      </nav>

      {/* 3. ÁREA CENTRAL */}
      <main className="flex-1 overflow-hidden bg-[#F8FAFC]">
        {activeView === 'sobre' && <SobreView />}
        {activeView === 'participantes' && <CadastradosView showDialog={setDialog} />}
        {activeView === 'jurados' && <JuradosView showDialog={setDialog} />}
        {activeView === 'criterios' && <CriteriosView showDialog={setDialog} />}
        {activeView === 'apuracao' && <ApuracaoNotasView showDialog={setDialog} />}
        {activeView === 'consultas' && <ResultadosSqlView showDialog={setDialog} />}
        {activeView === 'divulgacao' && <DiculgacaoView showDialog={setDialog} />}
      </main>

      {/* 4. RODAPÉ */}
      <footer className="flex h-[35px] shrink-0 items-center justify-center bg-[#1E293B] px-3 text-center text-xs text-[#94A3B8]">
        © 2026 AVALIAÇÃO DE MODELOS – Todos os direitos reservados.&nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;Banco de Dados: Armazenamento Local Conectado
      </footer>

      <DialogModal dialog={dialog} onClose={() => setDialog(null)} />
    </div>
  );
}

export default App;
