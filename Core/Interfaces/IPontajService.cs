using System;
using API.DTOs;
using Core.DTOs;
using Core.Entities;

namespace Core.Interfaces;

public interface IPontajService
{
        Task<IEnumerable<PontajDto>> GetPontajeByUserAndPeriodAsync(string userId, DateTime start, DateTime end);
        Task GenerareNormaBazaAsync(string userId, DateTime luna);
        Task<IEnumerable<PontajDto>> GenerarePontajeProiectAsync(string userId, DateTime dataInceput, DateTime dataSfarsit, string numeProiect, int oreAlocate);
        Task<IEnumerable<string>> GetProiecteListByUserIdAsync(string userId);
        Task<IEnumerable<PontajDto>> SimuleazaPontajeProiectAsync(string userId, DateTime dataInceput, DateTime dataSfarsit, string numeProiect, int oreAlocate);
        
        // Metode pentru ștergerea unui pontaj
        Task<PontajDto> GetPontajByIdAsync(int id);
        Task DeletePontajAsync(int id);
}
