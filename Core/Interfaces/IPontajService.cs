using Core.DTOs;
using Core.Entities;

namespace Core.Interfaces
{
    public interface IPontajService
    {
        Task<PontajDto> GetPontajByIdAsync(int id);
        Task DeletePontajAsync(int id);
        Task<IEnumerable<PontajDto>> GetPontajeByUserAndPeriodAsync(string userId, DateTime start, DateTime end);
        Task<IEnumerable<string>> GetProiecteListByUserIdAsync(string userId);
        Task GenerareNormaBazaAsync(string userId, DateTime luna);
        Task<IEnumerable<PontajDto>> GenerarePontajeProiectAsync(string userId, DateTime dataInceput, DateTime dataSfarsit, string numeProiect, int oreAlocate);
        Task<IEnumerable<PontajSumarDto>> GetPontajeSumarizateAsync(string userId, DateTime start, DateTime end);
        Task<IEnumerable<PontajDto>> SimuleazaPontajeProiectAsync(string userId, DateTime dataInceput, DateTime dataSfarsit, string numeProiect, int oreAlocate);
        
        // Noile metode adăugate
        Task<PontajSimulareResponse> SimuleazaPontajeProiectCuAjustareNormaAsync(string userId, DateTime dataInceput, DateTime dataSfarsit, string numeProiect, int oreAlocate);
        Task<IEnumerable<PontajDto>> GenerarePontajeProiectCuAjustareNormaAsync(string userId, DateTime dataInceput, DateTime dataSfarsit, string numeProiect, int oreAlocate);
    }
}