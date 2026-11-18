import React from 'react';

export const SobreView: React.FC = () => {
  return (
    <div className="h-full w-full overflow-y-auto bg-[#F8FAFC] p-7">
      <div className="min-h-full rounded border border-[#CBD5E1] bg-white p-7 text-[15px] leading-relaxed text-[#475569]">
        <h1 className="mb-5 text-2xl font-bold text-[#1E293B]">Sobre o Sistema</h1>

        <p className="mb-4">
          O <strong className="text-[#1E293B]">Sistema de Votação para Concursos</strong> foi
          desenvolvido para facilitar, organizar e automatizar todo o processo de avaliação e
          classificação de participantes em concursos.
        </p>

        <p className="mb-4">
          A plataforma permite o cadastro e gerenciamento de participantes, jurados e critérios de
          avaliação, proporcionando um ambiente centralizado, prático e seguro para a realização das
          votações.
        </p>

        <p className="mb-4">
          Por meio do sistema, cada jurado pode registrar individualmente suas notas para todas as
          participantes, considerando os critérios previamente definidos pela organização do
          concurso.
        </p>

        <p className="mb-5">
          Após o lançamento das avaliações, o sistema realiza automaticamente o processamento das
          notas, permitindo gerar consultas, apurar resultados e emitir relatórios de classificação
          conforme as regras e os critérios estabelecidos para cada concurso.
        </p>

        <p className="mb-2 font-bold text-[#1E293B]">Principais funcionalidades:</p>
        <ul className="mb-5 list-disc space-y-1 pl-5">
          <li>Cadastro e gerenciamento de participantes.</li>
          <li>Cadastro e gerenciamento de jurados.</li>
          <li>Configuração dos critérios de avaliação.</li>
          <li>Lançamento individual de notas por jurado.</li>
          <li>Consulta e acompanhamento das avaliações.</li>
          <li>Processamento automático das pontuações.</li>
          <li>Geração de relatórios de classificação e resultados.</li>
        </ul>

        <p className="mb-4">
          O objetivo é proporcionar{' '}
          <strong className="text-[#1E293B]">
            transparência, agilidade, organização e confiabilidade
          </strong>{' '}
          em todas as etapas do processo de votação, contribuindo para uma gestão eficiente e uma
          apuração precisa dos resultados.
        </p>

        <p className="font-bold text-[#1E293B]">Todos os direitos reservados.</p>
      </div>
    </div>
  );
};
