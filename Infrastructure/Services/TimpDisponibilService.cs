using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.DTOs;
using Core.Entities;
using Core.Interfaces;
using Core.Specification;
using PublicHoliday;

namespace Infrastructure.Services
{
    public class TimpDisponibilService : ITimpDisponibilService
    {
        private readonly IGenericRepository<Pontaj> _pontajRepository;
        private const int ORE_MAXIME_PE_ZI = 12;

        public TimpDisponibilService(IGenericRepository<Pontaj> pontajRepository)
        {
            _pontajRepository = pontajRepository;
        }

        public async Task<TimpDisponibilDto> GetTimpDisponibilAsync(string userId, DateTime luna)
        {
            var primaZiLuna = new DateTime(luna.Year, luna.Month, 1);
            var ultimaZiLuna = primaZiLuna.AddMonths(1).AddDays(-1);

            var spec = new PontajByUserAndPeriodSpecification(userId, primaZiLuna, ultimaZiLuna);
            var pontajeLuna = await _pontajRepository.ListAsync(spec);

            var romanianHolidays = new RomanianPublicHoliday();
            var zileLucratoareLuna = 0;
            var zileLucratoareRamase = 0;
            var dataCurenta = DateTime.Now.Date;

            for (var data = primaZiLuna; data <= ultimaZiLuna; data = data.AddDays(1))
            {
                if (data.DayOfWeek != DayOfWeek.Saturday &&
                    data.DayOfWeek != DayOfWeek.Sunday &&
                    !romanianHolidays.IsPublicHoliday(data))
                {
                    zileLucratoareLuna++;

                    if (data >= dataCurenta)
                    {
                        zileLucratoareRamase++;
                    }
                }
            }

            var oreDisponibileLuna = zileLucratoareLuna * ORE_MAXIME_PE_ZI;

            var orePontateLuna = CalculeazaOrePontate(pontajeLuna);

            var oreRamaseLuna = Math.Max(0, oreDisponibileLuna - orePontateLuna);

            var procentUtilizare = oreDisponibileLuna > 0
                ? (float)(orePontateLuna / oreDisponibileLuna * 100)
                : 0;

            return new TimpDisponibilDto
            {
                OreDisponibileLuna = oreDisponibileLuna,
                OrePontateLuna = orePontateLuna,
                OreRamaseLuna = oreRamaseLuna,
                ZileLucratoareLuna = zileLucratoareLuna,
                ZileLucratoareRamase = zileLucratoareRamase,
                ProcentUtilizare = procentUtilizare
            };
        }

        private double CalculeazaOrePontate(IReadOnlyList<Pontaj> pontaje)
        {
            double oreTotale = 0;

            foreach (var pontaj in pontaje)
            {
                var durataOre = (pontaj.OraFinal - pontaj.OraStart).TotalHours;
                oreTotale += durataOre;
            }

            return oreTotale;
        }
    }
}