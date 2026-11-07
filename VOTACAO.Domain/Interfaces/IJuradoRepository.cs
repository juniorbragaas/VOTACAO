using VOTACAO.Domain.Entities;

namespace VOTACAO.Domain.Interfaces;

public interface IJuradoRepository
{
    Task<IEnumerable<Jurado>> ObterTodosAsync();
    Task<Jurado?> ObterPorIdAsync(int id);
    Task<Jurado?> ObterPorNomeAsync(string nome);
    Task<IEnumerable<Jurado>> PesquisarAsync(string termo);
    Task<int> InserirAsync(Jurado jurado);
    Task<bool> AtualizarAsync(Jurado jurado);
    Task<bool> ExcluirAsync(int id);
    Task<int> ObterTotalAsync();
}
