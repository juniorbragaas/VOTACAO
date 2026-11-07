using VOTACAO.Domain.Entities;
using VOTACAO.Domain.Exceptions;
using VOTACAO.Domain.Interfaces;
using VOTACAO.Infrastructure.Logging;

namespace VOTACAO.BO.Services;

public class ApuracaoNotasService : IApuracaoNotasService
{
    private readonly IJuradoRepository _juradoRepository;
    private readonly IParticipanteRepository _participanteRepository;
    private readonly ICriterioRepository _criterioRepository;
    private readonly IAvaliacaoNotaRepository _avaliacaoNotaRepository;
    private readonly IAuditoriaRepository _auditoriaRepository;
    private readonly IAppLogger _logger;

    public ApuracaoNotasService(
        IJuradoRepository juradoRepository,
        IParticipanteRepository participanteRepository,
        ICriterioRepository criterioRepository,
        IAvaliacaoNotaRepository avaliacaoNotaRepository,
        IAuditoriaRepository auditoriaRepository,
        IAppLogger logger)
    {
        _juradoRepository = juradoRepository;
        _participanteRepository = participanteRepository;
        _criterioRepository = criterioRepository;
        _avaliacaoNotaRepository = avaliacaoNotaRepository;
        _auditoriaRepository = auditoriaRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<Jurado>> ObterJuradosAsync()
    {
        return await _juradoRepository.ObterTodosAsync();
    }

    public async Task<IEnumerable<Participante>> ObterParticipantesOrdenadosAsync()
    {
        var participantes = await _participanteRepository.ObterTodosAsync();
        return participantes.OrderBy(p => p.Nome, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<IEnumerable<Criterio>> ObterCriteriosAsync()
    {
        var criterios = await _criterioRepository.ObterTodosAsync();
        return criterios.OrderBy(c => c.Nome, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<IEnumerable<AvaliacaoNota>> ObterNotasPorJuradoAsync(int idJurado)
    {
        return await _avaliacaoNotaRepository.ObterPorJuradoAsync(idJurado);
    }

    public async Task<int> SalvarNotasAsync(int idJurado, IReadOnlyCollection<(int IdParticipante, int IdCriterio, int Nota)> notas)
    {
        if (idJurado <= 0)
            throw new DomainException("Jurado inválido.");

        if (notas == null || notas.Count == 0)
            return 0;

        foreach (var nota in notas)
        {
            if (nota.IdParticipante <= 0)
                throw new DomainException("Participante inválido.");

            if (nota.IdCriterio <= 0)
                throw new DomainException("Critério inválido.");

            var participante = await _participanteRepository.ObterPorIdAsync(nota.IdParticipante);
            if (participante == null)
                throw new DomainException($"Participante {nota.IdParticipante} não encontrado.");

            var criterio = await _criterioRepository.ObterPorIdAsync(nota.IdCriterio);
            if (criterio == null)
                throw new DomainException($"Critério {nota.IdCriterio} não encontrado.");
        }

        var salvo = await _avaliacaoNotaRepository.SalvarNotasAsync(idJurado, notas);
        if (salvo <= 0)
            throw new DomainException("Não foi possível salvar as notas do jurado.");

        return salvo;
    }

    public async Task<IEnumerable<AvaliacaoNota>> ObterNotasDoJuradoParaParticipanteAsync(int idJurado, int idParticipante)
    {
        return await _avaliacaoNotaRepository.ObterPorJuradoEParticipanteAsync(idJurado, idParticipante);
    }

    public async Task GarantirNotasEmBrancoAsync()
    {
        var jurados = (await _juradoRepository.ObterTodosAsync()).ToList();
        var participantes = (await _participanteRepository.ObterTodosAsync()).ToList();
        var criterios = (await _criterioRepository.ObterTodosAsync()).ToList();

        if (!jurados.Any() || !participantes.Any() || !criterios.Any())
            return;

        foreach (var jurado in jurados)
        {
            foreach (var participante in participantes)
            {
                foreach (var criterio in criterios)
                {
                    var existente = await _avaliacaoNotaRepository.ObterPorJuradoParticipanteCriterioAsync(jurado.Id, participante.Id, criterio.Id);
                    if (existente == null)
                    {
                        await _avaliacaoNotaRepository.InserirAsync(new AvaliacaoNota
                        {
                            IdJurado = jurado.Id,
                            IdParticipante = participante.Id,
                            IdCriterio = criterio.Id,
                            Nota = 5
                        });
                    }
                }
            }
        }
    }

    public async Task ZerarTodasNotasAsync()
    {
        await _avaliacaoNotaRepository.ZerarTodasNotasAsync();
    }

    public async Task SalvarNotaAsync(int idJurado, int idParticipante, int idCriterio, int nota)
    {
        if (idJurado <= 0)
            throw new DomainException("Jurado inválido.");

        if (idParticipante <= 0)
            throw new DomainException("Participante inválido.");

        if (idCriterio <= 0)
            throw new DomainException("Critério inválido.");

        var jurado = await _juradoRepository.ObterPorIdAsync(idJurado);
        if (jurado == null)
            throw new DomainException("Jurado não encontrado.");

        var participante = await _participanteRepository.ObterPorIdAsync(idParticipante);
        if (participante == null)
            throw new DomainException("Participante não encontrado.");

        var criterio = await _criterioRepository.ObterPorIdAsync(idCriterio);
        if (criterio == null)
            throw new DomainException("Critério não encontrado.");

        var registro = new AvaliacaoNota
        {
            IdJurado = idJurado,
            IdParticipante = idParticipante,
            IdCriterio = idCriterio,
            Nota = nota
        };

        var salvo = await _avaliacaoNotaRepository.SalvarOuAtualizarAsync(registro);
        if (!salvo)
            throw new DomainException("Não foi possível salvar a nota do critério.");

        await _auditoriaRepository.InserirLogAsync("APURACAO_NOTA", $"Jurado {jurado.Nome} avaliou participante {participante.Nome} no critério {criterio.Nome}: nota {nota}.");
        _logger.LogInfo($"Nota salva: Jurado {jurado.Nome}, Participante {participante.Nome}, Critério {criterio.Nome} => {nota}");
    }
}
