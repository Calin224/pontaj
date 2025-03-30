using Core.DTOs;

namespace Core.Interfaces;

public interface ITimpDisponibilService
{
    Task<TimpDisponibilDto> GetTimpDisponibilAsync(string userId, DateTime luna);
}