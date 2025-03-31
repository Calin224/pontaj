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
        private const int ORE_NORMA_BAZA_PE_ZI = 8;
        private const int ORE_PROIECTE_PE_ZI = 4;
        private const int ORE_MAXIME_PE_ZI = 12; // Total: 8 ore normă + 4 ore proiecte

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

            // Calculează limita maximă de ore permisă pentru lună (12 ore pe zi lucrătoare)
            var limitaMaximaLuna = zileLucratoareLuna * 12;

            // Calculează orele deja pontate
            var oreTotalPontate = pontajeLuna.Sum(p => (p.OraFinal - p.OraStart).TotalHours);

            // Calculează orele disponibile pe categorii
            var oreNormaBazaLuna = zileLucratoareLuna * ORE_NORMA_BAZA_PE_ZI;
            var oreProiecteLuna = zileLucratoareLuna * ORE_PROIECTE_PE_ZI;
            var oreDisponibileLuna = oreNormaBazaLuna + oreProiecteLuna;

            // Verifică dacă s-a depășit limita maximă
            if (oreTotalPontate > limitaMaximaLuna)
            {
                Console.WriteLine(
                    $"ATENȚIE: S-a depășit limita maximă de ore pentru lună ({oreTotalPontate} > {limitaMaximaLuna})");
                // În acest caz, nu mai sunt ore disponibile
                oreTotalPontate = limitaMaximaLuna;
            }

            // Calculează orele pontate, separând pe categorii
            var rezultatPontaje = CalculeazaOrePontate(pontajeLuna);
            var orePontateNormaBaza = rezultatPontaje.OrePontateNormaBaza;
            var orePontateProiecte = rezultatPontaje.OrePontateProiecte;
            var orePontateLuna = orePontateNormaBaza + orePontateProiecte;

            // Calculează orele rămase disponibile (folosind limita maximă)
            var oreRamaseLuna = Math.Max(0, limitaMaximaLuna - oreTotalPontate);

            var procentUtilizare = limitaMaximaLuna > 0
                ? (float)(oreTotalPontate / limitaMaximaLuna * 100)
                : 0;

            return new TimpDisponibilDto
            {
                OreDisponibileLuna = oreDisponibileLuna,
                OreNormaBazaLuna = oreNormaBazaLuna,
                OreProiecteLuna = oreProiecteLuna,
                OrePontateLuna = orePontateLuna,
                OrePontateNormaBaza = orePontateNormaBaza,
                OrePontateProiecte = orePontateProiecte,
                OreRamaseLuna = oreRamaseLuna, // Această valoare va fi acum limitată corect
                ZileLucratoareLuna = zileLucratoareLuna,
                ZileLucratoareRamase = zileLucratoareRamase,
                ProcentUtilizare = procentUtilizare,
                // LimitaMaximaLuna = limitaMaximaLuna // Adăugat pentru referință
            };
        }

        private (double OrePontateNormaBaza, double OrePontateProiecte) CalculeazaOrePontate(
            IReadOnlyList<Pontaj> pontaje)
        {
            double oreTotaleNormaBaza = 0;
            double oreTotaleProiecte = 0;

            foreach (var pontaj in pontaje)
            {
                var durataOre = (pontaj.OraFinal - pontaj.OraStart).TotalHours;

                if (pontaj.NormaBaza)
                {
                    oreTotaleNormaBaza += durataOre;
                }
                else
                {
                    oreTotaleProiecte += durataOre;
                }
            }

            return (oreTotaleNormaBaza, oreTotaleProiecte);
        }
    }
}