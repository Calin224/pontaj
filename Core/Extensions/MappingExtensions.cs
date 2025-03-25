using System;
using API.DTOs;
using Core.DTOs;
using Core.Entities;

namespace Core.Extensions;

public static class MappingExtensions
{
    public static PontajDto ToDto(this Pontaj pontaj)
    {
        return new PontajDto
        {
            Id = pontaj.Id,
            Data = pontaj.Data,
            OraStart = pontaj.OraStart,
            OraFinal = pontaj.OraFinal,
            NumeProiect = pontaj.NumeProiect,
            NormaBaza = pontaj.NormaBaza
        };
    }

    public static IReadOnlyList<PontajDto> ToDto(this IReadOnlyList<Pontaj> pontaje)
    {
        return pontaje.Select(p => p.ToDto()).ToList();
    }

    public static Pontaj ToPontaj(this GenerarePontajDto dto, string userId)
    {
        return new Pontaj
        {
            UserId = userId,
            NumeProiect = dto.NumeProiect
        };
    }
}
