using VOTACAO.Domain.Entities;

namespace VOTACAO.Domain.Interfaces;

public interface IParticipanteRepository
{
    Task<IEnumerable<Participante>> ObterTodosAsync();
    Task<Participante?> ObterPorIdAsync(int id);
    Task<IEnumerable<Participante>> PesquisarAsync(string termo);
    Task<int> InserirAsync(Participante participante);
    Task<bool> AtualizarAsync(Participante participante);
    Task<bool> ExcluirAsync(int id);
    Task<int> ObterTotalCadastradosAsync();
}
