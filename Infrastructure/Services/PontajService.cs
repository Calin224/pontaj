using Core.DTOs;
using Core.Entities;
using Core.Interfaces;
using Core.Specification;
using PublicHoliday;

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
            var spec = new PontajByUserSpecification(userId);
            var pontaje = await _pontajRepository.ListAsync(spec);

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
            
            var romanianHolidays = new RomanianPublicHoliday();
            
            for (var data = primaZiLuna; data <= ultimaZiLuna; data = data.AddDays(1))
            {
                if (data.DayOfWeek != DayOfWeek.Saturday && data.DayOfWeek != DayOfWeek.Sunday && !romanianHolidays.IsPublicHoliday(data))
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
            
            var romanianHolidays = new RomanianPublicHoliday();
            
            var spec = new PontajByUserAndPeriodSpecification(userId, dataInceput, dataSfarsit);
            var pontajeExistente = await _pontajRepository.ListAsync(spec);
            
            var pontajePeZile = pontajeExistente
                .GroupBy(p => p.Data.Date)
                .ToDictionary(g => g.Key, g => g.ToList());
            
            for (var data = dataInceput; data <= dataSfarsit && oreRamase > 0; data = data.AddDays(1))
            {
                // if (data.DayOfWeek != DayOfWeek.Saturday && data.DayOfWeek != DayOfWeek.Sunday)
                if (!romanianHolidays.IsPublicHoliday(data))
                {
                    var pontajeZiCurenta = pontajePeZile.ContainsKey(data.Date)
                        ? pontajePeZile[data.Date]
                        : new List<Pontaj>();

                    var intervaleLibere = GasesteIntervaleLibere(pontajeZiCurenta);

                    foreach (var interval in intervaleLibere)
                    {
                        if (oreRamase <= 0) break;

                        var durataInterval = (interval.Item2 - interval.Item1).TotalHours;

                        var oreDeAlocat = Math.Min(durataInterval, oreRamase);

                        if (oreDeAlocat >= 1)
                        {
                            oreDeAlocat = Math.Floor(oreDeAlocat);

                            var oraStart = interval.Item1;
                            var oraFinal = oraStart.Add(TimeSpan.FromHours(oreDeAlocat));

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

                            if (!pontajePeZile.ContainsKey(data.Date))
                            {
                                pontajePeZile[data.Date] = new List<Pontaj>();
                            }
                            pontajePeZile[data.Date].Add(pontajNou);

                            oreRamase -= (int)oreDeAlocat;
                        }
                    }
                }
            }

            await _pontajRepository.SaveChangesAsync();

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

        public async Task<IEnumerable<PontajSumarDto>> GetPontajeSumarizateAsync(string userId, DateTime start,
            DateTime end)
        {
            var spec = new PontajByUserAndPeriodSpecification(userId, start, end);
            var pontaje = await _pontajRepository.ListAsync(spec);

            var res = pontaje
                .GroupBy(p => p.NumeProiect)
                .Select(group => new PontajSumarDto()
                {
                    NumeProiect = group.Key,
                    TotalOre = group.Sum(p => (p.OraFinal - p.OraStart).TotalHours),
                    NrPontaje = group.Count()
                })
                .OrderByDescending(p => p.TotalOre)
                .ToList();

            return res;
        } 

        private List<Tuple<TimeSpan, TimeSpan>> GasesteIntervaleLibere(List<Pontaj> pontajeZi)
        {
            var intervaleLibere = new List<Tuple<TimeSpan, TimeSpan>>();

            var oraInceputZi = new TimeSpan(8, 0, 0);
            var oraSfarsitZi = new TimeSpan(20, 0, 0);

            var pontajeOrdonate = pontajeZi.OrderBy(p => p.OraStart).ToList();

            var oraCurenta = oraInceputZi;

            foreach (var pontaj in pontajeOrdonate)
            {
                if (pontaj.OraStart > oraCurenta)
                {
                    var durataInterval = (pontaj.OraStart - oraCurenta).TotalHours;
                    if (durataInterval >= 1)
                    {
                        intervaleLibere.Add(new Tuple<TimeSpan, TimeSpan>(oraCurenta, pontaj.OraStart));
                    }
                }

                if (pontaj.OraFinal > oraCurenta)
                {
                    oraCurenta = pontaj.OraFinal;
                }
            }

            if (oraCurenta < oraSfarsitZi)
            {
                var durataInterval = (oraSfarsitZi - oraCurenta).TotalHours;
                if (durataInterval >= 1)
                {
                    intervaleLibere.Add(new Tuple<TimeSpan, TimeSpan>(oraCurenta, oraSfarsitZi));
                }
            }

            return intervaleLibere
                .OrderByDescending(i => (i.Item2 - i.Item1).TotalHours)
                .ToList();
        }

        public async Task<IEnumerable<PontajDto>> SimuleazaPontajeProiectAsync(string userId, DateTime dataInceput, DateTime dataSfarsit, string numeProiect, int oreAlocate)
        {
            dataInceput = dataInceput.Date;
            dataSfarsit = dataSfarsit.Date.AddDays(1).AddTicks(-1);

            Console.WriteLine($"Simulare pontaje pentru perioada: {dataInceput} - {dataSfarsit}");

            var pontajeSimulate = new List<Pontaj>();
            var oreRamase = oreAlocate;
            
            var romanianHolidays = new RomanianPublicHoliday();

            var spec = new PontajByUserAndPeriodSpecification(userId, dataInceput, dataSfarsit);
            var pontajeExistente = await _pontajRepository.ListAsync(spec);

            var pontajePeZile = pontajeExistente
                .GroupBy(p => p.Data.Date)
                .ToDictionary(g => g.Key, g => g.ToList());

            for (var data = dataInceput.Date; data <= dataSfarsit.Date && oreRamase > 0; data = data.AddDays(1))
            {
                if (!romanianHolidays.IsPublicHoliday(data))
                {
                    var pontajeZiCurenta = pontajePeZile.ContainsKey(data.Date)
                        ? pontajePeZile[data.Date]
                        : new List<Pontaj>();

                    Console.WriteLine($"Verificare zi: {data.ToShortDateString()} - {pontajeZiCurenta.Count} pontaje existente");

                    var intervaleLibere = GasesteIntervaleLibere(pontajeZiCurenta);

                    Console.WriteLine($"Intervale libere găsite: {intervaleLibere.Count}");

                    foreach (var interval in intervaleLibere)
                    {
                        if (oreRamase <= 0) break;

                        var durataInterval = (interval.Item2 - interval.Item1).TotalHours;

                        var oreDeAlocat = Math.Min(durataInterval, oreRamase);

                        if (oreDeAlocat >= 1)
                        {
                            oreDeAlocat = Math.Floor(oreDeAlocat);

                            var oraStart = interval.Item1;
                            var oraFinal = oraStart.Add(TimeSpan.FromHours(oreDeAlocat));

                            var pontajNou = new Pontaj
                            {
                                Id = -1,
                                UserId = userId,
                                Data = data,
                                OraStart = oraStart,
                                OraFinal = oraFinal,
                                NumeProiect = numeProiect,
                                NormaBaza = false
                            };

                            pontajeSimulate.Add(pontajNou);

                            if (!pontajePeZile.ContainsKey(data.Date))
                            {
                                pontajePeZile[data.Date] = new List<Pontaj>();
                            }
                            pontajePeZile[data.Date].Add(pontajNou);

                            oreRamase -= (int)oreDeAlocat;

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
                UserId = p.UserId,
                InlocuiesteNorma = false
            }).OrderBy(p => p.Data).ThenBy(p => p.OraStart).ToList();
        }

        public async Task<PontajSimulareResponse> SimuleazaPontajeProiectCuAjustareNormaAsync(
            string userId, DateTime dataInceput, DateTime dataSfarsit, string numeProiect, int oreAlocate)
        {
            dataInceput = dataInceput.Date;
            dataSfarsit = dataSfarsit.Date.AddDays(1).AddTicks(-1);

            Console.WriteLine($"Simulare pontaje cu ajustare normă pentru perioada: {dataInceput} - {dataSfarsit}");

            var pontajeSimulate = new List<Pontaj>();
            var oreRamase = oreAlocate;
            var pontajeNormaSterse = new List<Pontaj>();
            
            var romanianHolidays = new RomanianPublicHoliday();

            var spec = new PontajByUserAndPeriodSpecification(userId, dataInceput, dataSfarsit);
            var pontajeExistente = await _pontajRepository.ListAsync(spec);

            var pontajePeZile = pontajeExistente
                .GroupBy(p => p.Data.Date)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Prima etapă: Încercăm să alocăm ore în intervale libere, ca în metoda originală
            for (var data = dataInceput.Date; data <= dataSfarsit.Date && oreRamase > 0; data = data.AddDays(1))
            {
                if (!romanianHolidays.IsPublicHoliday(data))
                {
                    var pontajeZiCurenta = pontajePeZile.ContainsKey(data.Date)
                        ? pontajePeZile[data.Date]
                        : new List<Pontaj>();

                    Console.WriteLine($"Verificare zi: {data.ToShortDateString()} - {pontajeZiCurenta.Count} pontaje existente");

                    var intervaleLibere = GasesteIntervaleLibere(pontajeZiCurenta);

                    Console.WriteLine($"Intervale libere găsite: {intervaleLibere.Count}");

                    foreach (var interval in intervaleLibere)
                    {
                        if (oreRamase <= 0) break;

                        var durataInterval = (interval.Item2 - interval.Item1).TotalHours;

                        var oreDeAlocat = Math.Min(durataInterval, oreRamase);

                        if (oreDeAlocat >= 1)
                        {
                            oreDeAlocat = Math.Floor(oreDeAlocat);

                            var oraStart = interval.Item1;
                            var oraFinal = oraStart.Add(TimeSpan.FromHours(oreDeAlocat));

                            var pontajNou = new Pontaj
                            {
                                Id = -1, // Id negativ pentru pontajele simulate
                                UserId = userId,
                                Data = data,
                                OraStart = oraStart,
                                OraFinal = oraFinal,
                                NumeProiect = numeProiect,
                                NormaBaza = false
                            };

                            pontajeSimulate.Add(pontajNou);

                            if (!pontajePeZile.ContainsKey(data.Date))
                            {
                                pontajePeZile[data.Date] = new List<Pontaj>();
                            }
                            pontajePeZile[data.Date].Add(pontajNou);

                            oreRamase -= (int)oreDeAlocat;

                            Console.WriteLine($"Pontaj adăugat: {data.ToShortDateString()} {oraStart} - {oraFinal}, ore: {oreDeAlocat}");
                        }
                    }
                }
            }

            // A doua etapă: Dacă mai sunt ore rămase, încercăm să înlocuim pontaje de normă bază
            if (oreRamase > 0)
            {
                Console.WriteLine($"Au rămas {oreRamase} ore neacoperite. Încercăm înlocuirea normei de bază.");

                // Ordonăm zilele cronologic
                var zileOrdonate = pontajePeZile.Keys.OrderBy(d => d).ToList();

                foreach (var dataZi in zileOrdonate)
                {
                    if (oreRamase <= 0) break;
                    
                    var pontajeZi = pontajePeZile[dataZi];
                    var pontajeNormaBaza = pontajeZi.Where(p => p.NormaBaza).ToList();
                    
                    if (!pontajeNormaBaza.Any()) continue;
                    
                    foreach (var pontajNorma in pontajeNormaBaza)
                    {
                        if (oreRamase <= 0) break;
                        
                        var durataNorma = (pontajNorma.OraFinal - pontajNorma.OraStart).TotalHours;
                        var oreDeInlocuit = Math.Min(durataNorma, oreRamase);
                        
                        if (oreDeInlocuit >= 1)
                        {
                            oreDeInlocuit = Math.Floor(oreDeInlocuit);
                            
                            var oraStart = pontajNorma.OraStart;
                            var oraFinal = oraStart.Add(TimeSpan.FromHours(oreDeInlocuit));
                            
                            // Marcăm pontajul de normă bază ca fiind "șters" în simulare
                            pontajeNormaSterse.Add(pontajNorma);
                            
                            // Cream pontajul nou pentru proiect
                            var pontajNou = new Pontaj
                            {
                                Id = -1, // Id negativ pentru pontajele simulate
                                UserId = userId,
                                Data = pontajNorma.Data,
                                OraStart = oraStart,
                                OraFinal = oraFinal,
                                NumeProiect = numeProiect,
                                NormaBaza = false
                            };
                            
                            pontajeSimulate.Add(pontajNou);
                            pontajePeZile[dataZi].Add(pontajNou);
                            
                            // Dacă înlocuim doar parțial norma de bază, creăm un nou pontaj pentru restul orelor
                            if (oreDeInlocuit < durataNorma)
                            {
                                var pontajNormaRamas = new Pontaj
                                {
                                    Id = -2, // Id special pentru pontajele de normă rămase după ajustare
                                    UserId = userId,
                                    Data = pontajNorma.Data,
                                    OraStart = oraFinal,
                                    OraFinal = pontajNorma.OraFinal,
                                    NumeProiect = pontajNorma.NumeProiect,
                                    NormaBaza = true
                                };
                                
                                pontajeSimulate.Add(pontajNormaRamas);
                                pontajePeZile[dataZi].Add(pontajNormaRamas);
                            }
                            
                            oreRamase -= (int)oreDeInlocuit;
                            
                            Console.WriteLine($"Normă bază înlocuită: {pontajNorma.Data.ToShortDateString()} {oraStart} - {oraFinal}, ore: {oreDeInlocuit}");
                        }
                    }
                }
            }

            var oreAcoperite = oreAlocate - oreRamase;
            var zileNecesareExtra = Math.Ceiling(oreRamase / 12.0); // presupun zile de 12 ore

            var rezultat = pontajeSimulate
                .Where(p => p.Id != -2) // Excludem pontajele marcate ca fiind de normă rămasă după ajustare
                .Select(p => new PontajDto
                {
                    Id = p.Id,
                    Data = p.Data,
                    OraStart = p.OraStart,
                    OraFinal = p.OraFinal,
                    NumeProiect = p.NumeProiect,
                    NormaBaza = p.NormaBaza,
                    UserId = p.UserId,
                    InlocuiesteNorma = pontajeNormaSterse.Any(pns => 
                        pns.Data.Date == p.Data.Date && 
                        ((pns.OraStart <= p.OraStart && p.OraFinal <= pns.OraFinal) || 
                         (p.OraStart <= pns.OraStart && pns.OraFinal <= p.OraFinal) || 
                         (pns.OraStart <= p.OraStart && p.OraStart < pns.OraFinal) || 
                         (pns.OraStart < p.OraFinal && p.OraFinal <= pns.OraFinal)))  
                })
                .OrderBy(p => p.Data)
                .ThenBy(p => p.OraStart)
                .ToList();

            return new PontajSimulareResponse
            {
                Pontaje = rezultat,
                OreRamase = oreRamase,
                OreAcoperite = oreAcoperite,
                ZileNecesareExtra = zileNecesareExtra
            };
        }

        public async Task<IEnumerable<PontajDto>> GenerarePontajeProiectCuAjustareNormaAsync(
            string userId, DateTime dataInceput, DateTime dataSfarsit, string numeProiect, int oreAlocate)
        {
            dataInceput = dataInceput.Date;
            dataSfarsit = dataSfarsit.Date.AddDays(1).AddTicks(-1);

            var pontajeCreate = new List<Pontaj>();
            var oreRamase = oreAlocate;
            
            var romanianHolidays = new RomanianPublicHoliday();
            
            var spec = new PontajByUserAndPeriodSpecification(userId, dataInceput, dataSfarsit);
            var pontajeExistente = await _pontajRepository.ListAsync(spec);
            
            var pontajePeZile = pontajeExistente
                .GroupBy(p => p.Data.Date)
                .ToDictionary(g => g.Key, g => g.ToList());
            
            // Prima etapă: Încercăm să alocăm ore în intervale libere
            for (var data = dataInceput; data <= dataSfarsit && oreRamase > 0; data = data.AddDays(1))
            {
                if (!romanianHolidays.IsPublicHoliday(data))
                {
                    var pontajeZiCurenta = pontajePeZile.ContainsKey(data.Date)
                        ? pontajePeZile[data.Date]
                        : new List<Pontaj>();

                    var intervaleLibere = GasesteIntervaleLibere(pontajeZiCurenta);

                    foreach (var interval in intervaleLibere)
                    {
                        if (oreRamase <= 0) break;

                        var durataInterval = (interval.Item2 - interval.Item1).TotalHours;
                        var oreDeAlocat = Math.Min(durataInterval, oreRamase);

                        if (oreDeAlocat >= 1)
                        {
                            oreDeAlocat = Math.Floor(oreDeAlocat);

                            var oraStart = interval.Item1;
                            var oraFinal = oraStart.Add(TimeSpan.FromHours(oreDeAlocat));

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

                            if (!pontajePeZile.ContainsKey(data.Date))
                            {
                                pontajePeZile[data.Date] = new List<Pontaj>();
                            }
                            pontajePeZile[data.Date].Add(pontajNou);

                            oreRamase -= (int)oreDeAlocat;
                        }
                    }
                }
            }

            // A doua etapă: Dacă mai sunt ore rămase, înlocuim pontaje de normă bază
            if (oreRamase > 0)
            {
                // Ordonăm zilele cronologic pentru predictibilitate
                var zileOrdonate = pontajePeZile.Keys.OrderBy(d => d).ToList();

                foreach (var dataZi in zileOrdonate)
                {
                    if (oreRamase <= 0) break;
                    
                    var pontajeZi = pontajePeZile[dataZi];
                    var pontajeNormaBaza = pontajeZi.Where(p => p.NormaBaza).ToList();
                    
                    if (!pontajeNormaBaza.Any()) continue;
                    
                    foreach (var pontajNorma in pontajeNormaBaza)
                    {
                        if (oreRamase <= 0) break;
                        
                        var durataNorma = (pontajNorma.OraFinal - pontajNorma.OraStart).TotalHours;
                        var oreDeInlocuit = Math.Min(durataNorma, oreRamase);
                        
                        if (oreDeInlocuit >= 1)
                        {
                            oreDeInlocuit = Math.Floor(oreDeInlocuit);
                            
                            var oraStart = pontajNorma.OraStart;
                            var oraFinal = oraStart.Add(TimeSpan.FromHours(oreDeInlocuit));
                            
                            // Ștergem pontajul de normă bază existent
                            _pontajRepository.Delete(pontajNorma);
                            
                            // Cream pontajul nou pentru proiect
                            var pontajNou = new Pontaj
                            {
                                UserId = userId,
                                Data = pontajNorma.Data,
                                OraStart = oraStart,
                                OraFinal = oraFinal,
                                NumeProiect = numeProiect,
                                NormaBaza = false
                            };
                            
                            _pontajRepository.Add(pontajNou);
                            pontajeCreate.Add(pontajNou);
                            
                            // Dacă înlocuim doar parțial norma de bază, creăm un nou pontaj pentru restul orelor
                            if (oreDeInlocuit < durataNorma)
                            {
                                var pontajNormaRamas = new Pontaj
                                {
                                    UserId = userId,
                                    Data = pontajNorma.Data,
                                    OraStart = oraFinal,
                                    OraFinal = pontajNorma.OraFinal,
                                    NumeProiect = pontajNorma.NumeProiect,
                                    NormaBaza = true
                                };
                                
                                _pontajRepository.Add(pontajNormaRamas);
                            }
                            
                            oreRamase -= (int)oreDeInlocuit;
                        }
                    }
                }
            }

            await _pontajRepository.SaveChangesAsync();

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
    }
}