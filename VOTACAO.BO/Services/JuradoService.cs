using VOTACAO.Domain.Entities;
using VOTACAO.Domain.Exceptions;
using VOTACAO.Domain.Interfaces;
using VOTACAO.Infrastructure.Logging;

namespace VOTACAO.BO.Services;

public class JuradoService : IJuradoService
{
    private readonly IJuradoRepository _repository;
    private readonly IAuditoriaRepository _auditoria;
    private readonly IAppLogger _logger;

    public JuradoService(IJuradoRepository repository, IAuditoriaRepository auditoria, IAppLogger logger)
    {
        _repository = repository;
        _auditoria = auditoria;
        _logger = logger;
    }

    public async Task<IEnumerable<Jurado>> ObterTodosAsync()
    {
        return await _repository.ObterTodosAsync();
    }

    public async Task<IEnumerable<Jurado>> PesquisarAsync(string termo)
    {
        if (string.IsNullOrWhiteSpace(termo))
            return await _repository.ObterTodosAsync();

        return await _repository.PesquisarAsync(termo.Trim());
    }

    public async Task<Jurado?> ObterPorIdAsync(int id)
    {
        return await _repository.ObterPorIdAsync(id);
    }

    public async Task<int> SalvarAsync(Jurado jurado)
    {
        if (string.IsNullOrWhiteSpace(jurado.Nome))
            throw new DomainException("O nome do jurado é obrigatório.");

        if (jurado.Nome.Trim().Length < 3)
            throw new DomainException("O nome do jurado deve conter pelo menos 3 caracteres.");

        if (string.IsNullOrWhiteSpace(jurado.Descricao))
            throw new DomainException("A descrição do jurado é obrigatória.");

        var existente = await _repository.ObterPorNomeAsync(jurado.Nome.Trim());
        if (existente != null && existente.Id != jurado.Id)
            throw new DomainException($"Já existe um jurado cadastrado com o nome '{jurado.Nome}'.");

        if (jurado.Id == 0)
        {
            var novoId = await _repository.InserirAsync(new Jurado
            {
                Nome = jurado.Nome.Trim(),
                Descricao = jurado.Descricao.Trim()
            });

            await _auditoria.InserirLogAsync("INCLUSAO_JURADO", $"Jurado {jurado.Nome} (ID: {novoId}) cadastrado.");
            _logger.LogInfo($"Jurado cadastrado: {jurado.Nome} (ID: {novoId})");
            return novoId;
        }

        var atual = await _repository.ObterPorIdAsync(jurado.Id);
        if (atual == null)
            throw new DomainException("Jurado não encontrado para atualização.");

        atual.Nome = jurado.Nome.Trim();
        atual.Descricao = jurado.Descricao.Trim();

        await _repository.AtualizarAsync(atual);
        await _auditoria.InserirLogAsync("ALTERACAO_JURADO", $"Jurado ID: {jurado.Id} alterado.");
        _logger.LogInfo($"Jurado atualizado: {atual.Nome} (ID: {atual.Id})");
        return jurado.Id;
    }

    public async Task<bool> ExcluirAsync(int id)
    {
        var existente = await _repository.ObterPorIdAsync(id);
        if (existente == null)
            throw new DomainException("Jurado não encontrado para exclusão.");

        bool excluido = await _repository.ExcluirAsync(id);
        if (excluido)
        {
            await _auditoria.InserirLogAsync("EXCLUSAO_JURADO", $"Jurado '{existente.Nome}' (ID: {id}) excluído.");
            _logger.LogInfo($"Jurado excluído: {existente.Nome} (ID: {id})");
        }
        return excluido;
    }

    public async Task<int> ObterTotalAsync()
    {
        return await _repository.ObterTotalAsync();
    }
}
