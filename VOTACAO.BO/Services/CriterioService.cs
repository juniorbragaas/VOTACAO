using VOTACAO.Domain.Entities;
using VOTACAO.Domain.Exceptions;
using VOTACAO.Domain.Interfaces;
using VOTACAO.Infrastructure.Logging;

namespace VOTACAO.BO.Services;

public class CriterioService : ICriterioService
{
    private readonly ICriterioRepository _repository;
    private readonly IAuditoriaRepository _auditoria;
    private readonly IAppLogger _logger;

    public CriterioService(ICriterioRepository repository, IAuditoriaRepository auditoria, IAppLogger logger)
    {
        _repository = repository;
        _auditoria = auditoria;
        _logger = logger;
    }

    public async Task<IEnumerable<Criterio>> ObterTodosAsync()
    {
        return await _repository.ObterTodosAsync();
    }

    public async Task<IEnumerable<Criterio>> PesquisarAsync(string termo)
    {
        if (string.IsNullOrWhiteSpace(termo))
            return await _repository.ObterTodosAsync();

        return await _repository.PesquisarAsync(termo.Trim());
    }

    public async Task<Criterio?> ObterPorIdAsync(int id)
    {
        return await _repository.ObterPorIdAsync(id);
    }

    public async Task<int> SalvarAsync(Criterio criterio)
    {
        if (string.IsNullOrWhiteSpace(criterio.Nome))
            throw new DomainException("O nome do critério é obrigatório.");

        if (criterio.Nome.Trim().Length < 2)
            throw new DomainException("O nome do critério deve conter pelo menos 2 caracteres.");

        var existente = await _repository.ObterPorNomeAsync(criterio.Nome.Trim());
        if (existente != null && existente.Id != criterio.Id)
            throw new DomainException($"Já existe um critério cadastrado com o nome '{criterio.Nome}'.");

        if (criterio.Id == 0)
        {
            var novoId = await _repository.InserirAsync(new Criterio
            {
                Nome = criterio.Nome.Trim()
            });

            await _auditoria.InserirLogAsync("INCLUSAO_CRITERIO", $"Critério {criterio.Nome} (ID: {novoId}) cadastrado.");
            _logger.LogInfo($"Critério cadastrado: {criterio.Nome} (ID: {novoId})");
            return novoId;
        }

        var atual = await _repository.ObterPorIdAsync(criterio.Id);
        if (atual == null)
            throw new DomainException("Critério não encontrado para atualização.");

        atual.Nome = criterio.Nome.Trim();

        await _repository.AtualizarAsync(atual);
        await _auditoria.InserirLogAsync("ALTERACAO_CRITERIO", $"Critério ID: {criterio.Id} alterado.");
        _logger.LogInfo($"Critério atualizado: {atual.Nome} (ID: {atual.Id})");
        return criterio.Id;
    }

    public async Task<bool> ExcluirAsync(int id)
    {
        var existente = await _repository.ObterPorIdAsync(id);
        if (existente == null)
            throw new DomainException("Critério não encontrado para exclusão.");

        bool excluido = await _repository.ExcluirAsync(id);
        if (excluido)
        {
            await _auditoria.InserirLogAsync("EXCLUSAO_CRITERIO", $"Critério '{existente.Nome}' (ID: {id}) excluído.");
            _logger.LogInfo($"Critério excluído: {existente.Nome} (ID: {id})");
        }
        return excluido;
    }

    public async Task<int> ObterTotalAsync()
    {
        return await _repository.ObterTotalAsync();
    }
}
