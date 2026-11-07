using VOTACAO.BO.Validators;
using VOTACAO.Domain.DTOs;
using VOTACAO.Domain.Entities;
using VOTACAO.Domain.Exceptions;
using VOTACAO.Domain.Interfaces;
using VOTACAO.Infrastructure.Logging;

namespace VOTACAO.BO.Services;

public class ParticipanteService : IParticipanteService
{
    private readonly IParticipanteRepository _repository;
    private readonly IAuditoriaRepository _auditoria;
    private readonly IAppLogger _logger;

    public ParticipanteService(IParticipanteRepository repository, IAuditoriaRepository auditoria, IAppLogger logger)
    {
        _repository = repository;
        _auditoria = auditoria;
        _logger = logger;
    }

    public async Task<IEnumerable<Participante>> ObterTodosAsync()
    {
        return await _repository.ObterTodosAsync();
    }

    public async Task<IEnumerable<Participante>> PesquisarAsync(string termo)
    {
        if (string.IsNullOrWhiteSpace(termo))
            return await _repository.ObterTodosAsync();

        return await _repository.PesquisarAsync(termo.Trim());
    }

    public async Task<Participante?> ObterPorIdAsync(int id)
    {
        return await _repository.ObterPorIdAsync(id);
    }

    public async Task<int> SalvarAsync(ParticipanteDto dto)
    {
        ParticipanteValidator.Validar(dto);

        if (dto.Id == 0)
        {
            var novo = new Participante
            {
                Nome = dto.Nome.Trim(),
                DataNascimento = dto.DataNascimento,
                Categoria = dto.Categoria
            };

            int novoId = await _repository.InserirAsync(novo);
            await _auditoria.InserirLogAsync("INCLUSAO_PARTICIPANTE", $"Participante {novo.Nome} (ID: {novoId}) cadastrado.");
            _logger.LogInfo($"Participante cadastrado: {novo.Nome} (ID: {novoId})");
            return novoId;
        }
        else
        {
            var existente = await _repository.ObterPorIdAsync(dto.Id);
            if (existente == null)
                throw new DomainException("Participante não encontrado para atualização.");

            existente.Nome = dto.Nome.Trim();
            existente.DataNascimento = dto.DataNascimento;
            existente.Categoria = dto.Categoria;

            await _repository.AtualizarAsync(existente);
            await _auditoria.InserirLogAsync("ALTERACAO_PARTICIPANTE", $"Participante ID: {dto.Id} alterado.");
            _logger.LogInfo($"Participante atualizado: {existente.Nome} (ID: {existente.Id})");
            return dto.Id;
        }
    }

    public async Task<bool> ExcluirAsync(int id)
    {
        var existente = await _repository.ObterPorIdAsync(id);
        if (existente == null)
            throw new DomainException("Participante não encontrado para exclusão.");

        bool excluido = await _repository.ExcluirAsync(id);
        if (excluido)
        {
            await _auditoria.InserirLogAsync("EXCLUSAO_PARTICIPANTE", $"Participante '{existente.Nome}' (ID: {id}) excluído.");
            _logger.LogInfo($"Participante excluído: {existente.Nome} (ID: {id})");
        }
        return excluido;
    }

    public async Task<int> ObterTotalAsync()
    {
        return await _repository.ObterTotalCadastradosAsync();
    }
}
