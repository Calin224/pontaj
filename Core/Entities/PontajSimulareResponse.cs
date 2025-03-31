using Core.DTOs;

namespace Core.Entities;

public class PontajSimulareResponse
{
    public IEnumerable<PontajDto> Pontaje { get; set; } = [];
    public double OreRamase { get; set; }
    public int OreAcoperite { get; set; }
    public double ZileNecesareExtra { get; set; }
}