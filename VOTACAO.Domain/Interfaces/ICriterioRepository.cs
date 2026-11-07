using VOTACAO.Domain.Entities;

namespace VOTACAO.Domain.Interfaces;

public interface ICriterioRepository
{
    Task<IEnumerable<Criterio>> ObterTodosAsync();
    Task<Criterio?> ObterPorIdAsync(int id);
    Task<Criterio?> ObterPorNomeAsync(string nome);
    Task<IEnumerable<Criterio>> PesquisarAsync(string termo);
    Task<int> InserirAsync(Criterio criterio);
    Task<bool> AtualizarAsync(Criterio criterio);
    Task<bool> ExcluirAsync(int id);
    Task<int> ObterTotalAsync();
}


