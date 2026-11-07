using VOTACAO.Domain.Entities;

namespace VOTACAO.BO.Services;

public interface IJuradoService
{
    Task<IEnumerable<Jurado>> ObterTodosAsync();
    Task<IEnumerable<Jurado>> PesquisarAsync(string termo);
    Task<Jurado?> ObterPorIdAsync(int id);
    Task<int> SalvarAsync(Jurado jurado);
    Task<bool> ExcluirAsync(int id);
    Task<int> ObterTotalAsync();
}
