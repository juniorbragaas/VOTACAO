using VOTACAO.Domain.DTOs;
using VOTACAO.Domain.Enums;
using VOTACAO.Domain.Exceptions;

namespace VOTACAO.BO.Validators;

public static class ParticipanteValidator
{
    public static void Validar(ParticipanteDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new DomainException("O nome do participante é obrigatório.");

        if (dto.Nome.Trim().Length < 3)
            throw new DomainException("O nome deve conter pelo menos 3 caracteres.");

        if (dto.DataNascimento == default)
            throw new DomainException("A data de nascimento é obrigatória.");

        if (dto.DataNascimento > DateTime.Today)
            throw new DomainException("A data de nascimento não pode ser uma data futura.");

        if (!Enum.IsDefined(typeof(CategoriaParticipante), dto.Categoria))
            throw new DomainException("A categoria informada é inválida. Use ADULTO ou TEEN.");
    }
}
