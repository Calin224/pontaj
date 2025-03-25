using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using Core.DTOs;
using Core.Entities;
using Core.Interfaces;
using Core.Specification;

namespace Infrastructure.Services
{
    public class PontajService : IPontajService
    {
        private readonly IGenericRepository<Pontaj> _pontajRepository;

        public PontajService(IGenericRepository<Pontaj> pontajRepository)
        {
            _pontajRepository = pontajRepository;
        }

        public async Task<PontajDto> GetPontajByIdAsync(int id)
        {
            var pontaj = await _pontajRepository.GetByIdAsync(id);
            if (pontaj == null) return null;

            return new PontajDto
            {
                Id = pontaj.Id,
                Data = pontaj.Data,
                OraStart = pontaj.OraStart,
                OraFinal = pontaj.OraFinal,
                NumeProiect = pontaj.NumeProiect,
                NormaBaza = pontaj.NormaBaza,
                UserId = pontaj.UserId
            };
        }

        public async Task DeletePontajAsync(int id)
        {
            var pontaj = await _pontajRepository.GetByIdAsync(id);
            if (pontaj != null)
            {
                _pontajRepository.Delete(pontaj);
                await _pontajRepository.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<PontajDto>> GetPontajeByUserAndPeriodAsync(string userId, DateTime start, DateTime end)
        {
            var spec = new PontajByUserAndPeriodSpecification(userId, start, end);
            var pontaje = await _pontajRepository.ListAsync(spec);

            return pontaje.Select(p => new PontajDto
            {
                Id = p.Id,
                Data = p.Data,
                OraStart = p.OraStart,
                OraFinal = p.OraFinal,
                NumeProiect = p.NumeProiect,
                NormaBaza = p.NormaBaza,
                UserId = p.UserId
            }).ToList();
        }

        public async Task<IEnumerable<string>> GetProiecteListByUserIdAsync(string userId)
        {
            // Obținem toate pontajele utilizatorului
            var spec = new PontajByUserSpecification(userId);
            var pontaje = await _pontajRepository.ListAsync(spec);

            // Extragem numele unice de proiecte
            return pontaje
                .Select(p => p.NumeProiect)
                .Distinct()
                .OrderBy(name => name)
                .ToList();
        }

        public async Task GenerareNormaBazaAsync(string userId, DateTime luna)
        {
            var primaZiLuna = new DateTime(luna.Year, luna.Month, 1);
            var ultimaZiLuna = primaZiLuna.AddMonths(1).AddDays(-1);

            for (var data = primaZiLuna; data <= ultimaZiLuna; data = data.AddDays(1))
            {
                if (data.DayOfWeek != DayOfWeek.Saturday && data.DayOfWeek != DayOfWeek.Sunday)
                {
                    var specZi = new PontajByUserAndDateSpecification(userId, data);
                    var pontajeZi = await _pontajRepository.ListAsync(specZi);
                    var existaPontajNormaBaza = pontajeZi.Any(p => p.NormaBaza);

                    if (!existaPontajNormaBaza)
                    {
                        var pontajNormaBaza = new Pontaj
                        {
                            UserId = userId,
                            Data = data,
                            OraStart = new TimeSpan(9, 0, 0),
                            OraFinal = new TimeSpan(17, 0, 0),
                            NumeProiect = "Normă de bază",
                            NormaBaza = true
                        };

                        _pontajRepository.Add(pontajNormaBaza);
                    }
                }
            }

            await _pontajRepository.SaveChangesAsync();
        }

        public async Task<IEnumerable<PontajDto>> GenerarePontajeProiectAsync(string userId, DateTime dataInceput, DateTime dataSfarsit, string numeProiect, int oreAlocate)
        {
            var pontajeCreate = new List<Pontaj>();
            var oreRamase = oreAlocate;

            // Obținem toate pontajele existente în perioada specificată
            var spec = new PontajByUserAndPeriodSpecification(userId, dataInceput, dataSfarsit);
            var pontajeExistente = await _pontajRepository.ListAsync(spec);

            // Grupăm pontajele existente pe zile
            var pontajePeZile = pontajeExistente
                .GroupBy(p => p.Data.Date)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Iterăm prin fiecare zi din perioada specificată
            for (var data = dataInceput; data <= dataSfarsit && oreRamase > 0; data = data.AddDays(1))
            {
                // Verificăm dacă este zi lucrătoare (Luni-Vineri)
                if (data.DayOfWeek != DayOfWeek.Saturday && data.DayOfWeek != DayOfWeek.Sunday)
                {
                    // Obținem pontajele pentru ziua curentă sau o listă goală dacă nu există
                    var pontajeZiCurenta = pontajePeZile.ContainsKey(data.Date)
                        ? pontajePeZile[data.Date]
                        : new List<Pontaj>();

                    // Identificăm intervale libere (între 8:00 și 20:00)
                    var intervaleLibere = GasesteIntervaleLibere(pontajeZiCurenta);

                    // Adăugăm pontaje în intervalele libere până epuizăm orele alocate
                    foreach (var interval in intervaleLibere)
                    {
                        if (oreRamase <= 0) break;

                        // Calculăm durata intervalului în ore
                        var durataInterval = (interval.Item2 - interval.Item1).TotalHours;

                        // Determinăm câte ore putem aloca în acest interval
                        var oreDeAlocat = Math.Min(durataInterval, oreRamase);

                        if (oreDeAlocat >= 1) // Alocăm doar intervale de minim 1 oră
                        {
                            // Prioritizăm alocarea de ore întregi
                            oreDeAlocat = Math.Floor(oreDeAlocat);

                            // Calculăm ora de sfârșit
                            var oraStart = interval.Item1;
                            var oraFinal = oraStart.Add(TimeSpan.FromHours(oreDeAlocat));

                            // Creăm un nou pontaj
                            var pontajNou = new Pontaj
                            {
                                UserId = userId,
                                Data = data,
                                OraStart = oraStart,
                                OraFinal = oraFinal,
                                NumeProiect = numeProiect,
                                NormaBaza = false
                            };

                            _pontajRepository.Add(pontajNou);
                            pontajeCreate.Add(pontajNou);

                            // Actualizăm pontajele existente pentru ziua curentă
                            if (!pontajePeZile.ContainsKey(data.Date))
                            {
                                pontajePeZile[data.Date] = new List<Pontaj>();
                            }
                            pontajePeZile[data.Date].Add(pontajNou);

                            // Decrementăm orele rămase
                            oreRamase -= (int)oreDeAlocat;
                        }
                    }
                }
            }

            await _pontajRepository.SaveChangesAsync();

            // Returnăm pontajele create ca DTOs
            return pontajeCreate.Select(p => new PontajDto
            {
                Id = p.Id,
                Data = p.Data,
                OraStart = p.OraStart,
                OraFinal = p.OraFinal,
                NumeProiect = p.NumeProiect,
                NormaBaza = p.NormaBaza,
                UserId = p.UserId
            }).ToList();
        }

        private List<Tuple<TimeSpan, TimeSpan>> GasesteIntervaleLibere(List<Pontaj> pontajeZi)
        {
            var intervaleLibere = new List<Tuple<TimeSpan, TimeSpan>>();

            // Ora de început și sfârșit a zilei de lucru
            var oraInceputZi = new TimeSpan(8, 0, 0); // 8:00
            var oraSfarsitZi = new TimeSpan(20, 0, 0); // 20:00

            // Sortăm pontajele după ora de început
            var pontajeOrdonate = pontajeZi.OrderBy(p => p.OraStart).ToList();

            // Ora curentă începe de la ora de început a zilei
            var oraCurenta = oraInceputZi;

            // Parcurgem toate pontajele și identificăm intervale libere
            foreach (var pontaj in pontajeOrdonate)
            {
                // Dacă există un interval liber între ora curentă și începutul pontajului
                if (pontaj.OraStart > oraCurenta)
                {
                    // Verificăm dacă intervalul este suficient de mare (minim 1 oră)
                    var durataInterval = (pontaj.OraStart - oraCurenta).TotalHours;
                    if (durataInterval >= 1)
                    {
                        intervaleLibere.Add(new Tuple<TimeSpan, TimeSpan>(oraCurenta, pontaj.OraStart));
                    }
                }

                // Actualizăm ora curentă la sfârșitul pontajului curent
                if (pontaj.OraFinal > oraCurenta)
                {
                    oraCurenta = pontaj.OraFinal;
                }
            }

            // Verificăm dacă mai există un interval liber până la sfârșitul zilei
            if (oraCurenta < oraSfarsitZi)
            {
                // Verificăm dacă intervalul rămas este suficient de mare
                var durataInterval = (oraSfarsitZi - oraCurenta).TotalHours;
                if (durataInterval >= 1)
                {
                    intervaleLibere.Add(new Tuple<TimeSpan, TimeSpan>(oraCurenta, oraSfarsitZi));
                }
            }

            // Sortăm intervalele după dimensiune (preferăm mai întâi intervalele mari)
            return intervaleLibere
                .OrderByDescending(i => (i.Item2 - i.Item1).TotalHours)
                .ToList();
        }

        public async Task<IEnumerable<PontajDto>> SimuleazaPontajeProiectAsync(string userId, DateTime dataInceput, DateTime dataSfarsit, string numeProiect, int oreAlocate)
        {
            // Asigurăm-ne că datele sunt setate corect pentru a include prima zi
            dataInceput = dataInceput.Date; // Setăm ora la 00:00:00
            dataSfarsit = dataSfarsit.Date.AddDays(1).AddTicks(-1); // Setăm ora la 23:59:59.9999999

            Console.WriteLine($"Simulare pontaje pentru perioada: {dataInceput} - {dataSfarsit}");

            var pontajeSimulate = new List<Pontaj>();
            var oreRamase = oreAlocate;

            // Obținem toate pontajele existente în perioada specificată
            var spec = new PontajByUserAndPeriodSpecification(userId, dataInceput, dataSfarsit);
            var pontajeExistente = await _pontajRepository.ListAsync(spec);

            // Grupăm pontajele existente pe zile
            var pontajePeZile = pontajeExistente
                .GroupBy(p => p.Data.Date)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Iterăm prin fiecare zi din perioada specificată, inclusiv prima zi
            for (var data = dataInceput.Date; data <= dataSfarsit.Date && oreRamase > 0; data = data.AddDays(1))
            {
                // Verificăm dacă este zi lucrătoare (Luni-Vineri)
                if (data.DayOfWeek != DayOfWeek.Saturday && data.DayOfWeek != DayOfWeek.Sunday)
                {
                    // Obținem pontajele pentru ziua curentă sau o listă goală dacă nu există
                    var pontajeZiCurenta = pontajePeZile.ContainsKey(data.Date)
                        ? pontajePeZile[data.Date]
                        : new List<Pontaj>();

                    // Logăm pentru debug
                    Console.WriteLine($"Verificare zi: {data.ToShortDateString()} - {pontajeZiCurenta.Count} pontaje existente");

                    // Identificăm intervale libere (între 8:00 și 20:00)
                    var intervaleLibere = GasesteIntervaleLibere(pontajeZiCurenta);

                    // Logăm pentru debug
                    Console.WriteLine($"Intervale libere găsite: {intervaleLibere.Count}");

                    // Adăugăm pontaje în intervalele libere până epuizăm orele alocate
                    foreach (var interval in intervaleLibere)
                    {
                        if (oreRamase <= 0) break;

                        // Calculăm durata intervalului în ore
                        var durataInterval = (interval.Item2 - interval.Item1).TotalHours;

                        // Determinăm câte ore putem aloca în acest interval
                        var oreDeAlocat = Math.Min(durataInterval, oreRamase);

                        if (oreDeAlocat >= 1) // Alocăm doar intervale de minim 1 oră
                        {
                            // Prioritizăm alocarea de ore întregi
                            oreDeAlocat = Math.Floor(oreDeAlocat);

                            // Calculăm ora de sfârșit
                            var oraStart = interval.Item1;
                            var oraFinal = oraStart.Add(TimeSpan.FromHours(oreDeAlocat));

                            // Creăm un nou pontaj simulat
                            var pontajNou = new Pontaj
                            {
                                Id = -1, // ID temporar pentru simulare
                                UserId = userId,
                                Data = data,
                                OraStart = oraStart,
                                OraFinal = oraFinal,
                                NumeProiect = numeProiect,
                                NormaBaza = false
                            };

                            pontajeSimulate.Add(pontajNou);

                            // Actualizăm pontajele existente pentru ziua curentă (pentru simulare)
                            if (!pontajePeZile.ContainsKey(data.Date))
                            {
                                pontajePeZile[data.Date] = new List<Pontaj>();
                            }
                            pontajePeZile[data.Date].Add(pontajNou);

                            // Decrementăm orele rămase
                            oreRamase -= (int)oreDeAlocat;

                            // Logăm pentru debug
                            Console.WriteLine($"Pontaj adăugat: {data.ToShortDateString()} {oraStart} - {oraFinal}, ore: {oreDeAlocat}");
                        }
                    }
                }
            }

            return pontajeSimulate.Select(p => new PontajDto
            {
                Id = p.Id,
                Data = p.Data,
                OraStart = p.OraStart,
                OraFinal = p.OraFinal,
                NumeProiect = p.NumeProiect,
                NormaBaza = p.NormaBaza,
                UserId = p.UserId
            }).OrderBy(p => p.Data).ThenBy(p => p.OraStart).ToList();
        }
    }
}