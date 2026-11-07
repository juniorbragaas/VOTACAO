using VOTACAO.Domain.Entities;

namespace VOTACAO.BO.Services;

public interface ICriterioService
{
    Task<IEnumerable<Criterio>> ObterTodosAsync();
    Task<IEnumerable<Criterio>> PesquisarAsync(string termo);
    Task<Criterio?> ObterPorIdAsync(int id);
    Task<int> SalvarAsync(Criterio criterio);
    Task<bool> ExcluirAsync(int id);
    Task<int> ObterTotalAsync();
}


