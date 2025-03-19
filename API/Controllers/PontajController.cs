using System.Drawing;
using System.Globalization;
using System.Security.Claims;
using API.DTOs;
using API.Extensions;
using Core.Entities;
using Core.Interfaces;
using Core.Specification;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace API.Controllers;

public class PontajController(
    IGenericRepository<Pontaj> repo,
    IGenericRepository<Proiect> projectRepo,
    IGenericRepository<ZiDeLucru> dayRepo,
    UserManager<AppUser> userManager) : BaseApiController
{
    [HttpPost]
    public async Task<IActionResult> AddPontaj(PontajCreateDto pontajCreateDto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var ziDeLucruSpec = new WorkDaySpecification(userId, pontajCreateDto.Data);
        var ziDeLucru = await dayRepo.GetEntityWithSpec(ziDeLucruSpec);

        if (ziDeLucru == null)
        {
            ziDeLucru = new ZiDeLucru
            {
                Data = pontajCreateDto.Data,
                UserId = userId
            };
            dayRepo.Add(ziDeLucru);

            if (!await dayRepo.SaveAllAsync()) return BadRequest("Eroare la adaugarea zilei de lucru");
        }

        // calcul ore pe zi -- start

        var totalWorkedHours = ziDeLucru.Pontaje.Sum(p => (p.OraSfarsit - p.OraInceput).TotalHours);

        var oreNoi = (pontajCreateDto.OraSfarsit - pontajCreateDto.OraInceput).TotalHours;

        if (totalWorkedHours + oreNoi > 12)
        {
            return BadRequest("Nu poti lucra mai mult de 12 ore intr-o zi!");
        }

        // calcul ore pe zi -- end

        // calcul ore pe luna -- start

        var startOfWeek = pontajCreateDto.Data.AddDays(-(int)pontajCreateDto.Data.DayOfWeek + (int)DayOfWeek.Monday);
        var endOfWeek = startOfWeek.AddDays(6);

        var pontajSapt = await repo.ListAsync(new PontajByMonthSpecification(userId, startOfWeek, endOfWeek));
        var totalOreSapt = pontajSapt.Sum(p => (p.OraSfarsit - p.OraInceput).TotalHours);

        if (totalOreSapt + oreNoi > 60)
        {
            return BadRequest("Nu poți lucra mai mult de 60 de ore într-o săptămână!");
        }

        // calcul ore pe luna -- end

        Proiect? proiect = null;
        if (pontajCreateDto.ProiectId != null)
        {
            proiect = await projectRepo.GetByIdAsync(pontajCreateDto.ProiectId.Value);
            if (proiect == null) return BadRequest("Proiectul nu exista");
        }

        if (pontajCreateDto.OraInceput > pontajCreateDto.OraSfarsit)
            return BadRequest("Ora de inceput nu poate fi mai mare decat ora de sfarsit.");

        var pontajeExistente = await repo.ListAsync(new PontajSpecification(ziDeLucru.Id));

        var suprapunere = pontajeExistente.Any(p =>
            (pontajCreateDto.OraInceput < p.OraSfarsit && pontajCreateDto.OraSfarsit > p.OraInceput));

        if (suprapunere)
        {
            return BadRequest("Exista un pontaj care se suprapune cu acesta.");
        }

        var pontaj = new Pontaj
        {
            ZiDeLucru = ziDeLucru,
            ZiDeLucruId = ziDeLucru.Id,
            OraInceput = pontajCreateDto.OraInceput,
            OraSfarsit = pontajCreateDto.OraSfarsit,
            TipMunca = pontajCreateDto.TipMunca,
            Proiect = proiect,
            UserId = userId
        };

        repo.Add(pontaj);
        if (!await dayRepo.SaveAllAsync()) return BadRequest("Eroare la adaugarea pontajului");

        return Ok(pontaj);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePontaj(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var pontaj = await repo.GetByIdAsync(id);
        if (pontaj == null) return NotFound();

        if (pontaj.UserId != userId) return Unauthorized();

        repo.Delete(pontaj);
        if (!await repo.SaveAllAsync()) return BadRequest("Eroare la stergerea pontajului");

        return Ok();
    }

    [HttpGet("zile-pontaje")]
    public async Task<IActionResult> GetZileCuPontaje([FromQuery] int year, [FromQuery] int month)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var pontaje = await repo.ListAsync(new PontajByMonthSpecification(userId, year, month));

        var zileCuPontaje = pontaje
            .Select(p => p.ZiDeLucru.Data.Day)
            .Distinct()
            .OrderBy(day => day)
            .ToList();

        return Ok(zileCuPontaje);
    }

    [HttpGet("zi/{data}")]
    public async Task<IActionResult> GetPontajByDate([FromRoute] DateTime data)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var spec = new WorkDaySpecification(userId, data);
        var ziDeLucru = await dayRepo.GetEntityWithSpec(spec);
        if (ziDeLucru == null) return NotFound();

        var pontaje = await repo.ListAsync(new PontajSpecification(ziDeLucru.Id));

        var res = pontaje.Select(p => new
        {
            p.Id,
            p.OraInceput,
            p.OraSfarsit,
            p.TipMunca,
            Durata = p.DurataMuncita.ToString(@"hh\:mm"),
            Proiect = p.Proiect != null
                ? new { p.Proiect.Id, DenumireaActivitatii = p.Proiect.DenumireaActivitatii }
                : null
        });

        return Ok(res);
    }

    [HttpGet("luna/{year}/{month}")]
    public async Task<IActionResult> GetPontajeByMonth(int year, int month)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var spec = new PontajByMonthSpecification(userId, startDate, endDate);
        var pontaje = await repo.ListAsync(spec);

        var groupedPontaje = pontaje
            .GroupBy(p => p.ZiDeLucru.Data)
            .Select(g => new
            {
                Data = g.Key,
                Intrari = g.OrderBy(p => p.OraInceput).ToList()
            })
            .OrderBy(g => g.Data)
            .ToList();

        return Ok(groupedPontaje);
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportPontajToExcel([FromQuery] int year, [FromQuery] int month)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await userManager.GetUserByEmail(User);
        var pontaje = await repo.ListAsync(new PontajByMonthSpecification(userId, year, month));

        var project = await projectRepo.GetEntityWithSpec(new ProiectSpecification(userId));
        if (project == null) return NotFound("Nu există proiecte asociate!");

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Fișă de pontaj");

        // Titlul fișei
        worksheet.Cells["A1:E1"].Merge = true;
        worksheet.Cells["A1"].Value = "Fișă de pontaj";
        worksheet.Cells["A1"].Style.Font.Size = 14;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        // Informații despre utilizator și proiect
        worksheet.Cells["A3"].Value = "Luna / anul:";
        worksheet.Cells["C3:E3"].Merge = true;
        worksheet.Cells["C3:E3"].Value = $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month)} / {year}";
        worksheet.Cells["A4"].Value = "Numele și prenumele expertului";
        worksheet.Cells["C4:E4"].Merge = true;
        worksheet.Cells["C4"].Value = $"{user.FirstName} {user.LastName}";
        worksheet.Cells["A5"].Value = "Poziția în proiect";
        worksheet.Cells["C5:E5"].Merge = true;
        worksheet.Cells["C5:E5"].Value = project.PozitiaInProiect;
        worksheet.Cells["A6"].Value = "Denumire beneficiar";
        worksheet.Cells["C6:E6"].Merge = true;
        worksheet.Cells["C6:E6"].Value = project.DenumireBeneficiar;
        worksheet.Cells["A7"].Value = "Cod / titlu proiect";
        worksheet.Cells["C7:E7"].Style.WrapText = true;
        worksheet.Cells["C7:E7"].Merge = true;
        worksheet.Cells["C7:E7"].Value = $"{project.TitluProiect}, {project.CodProiect}";

        // Titluri coloane
        worksheet.Cells["A9"].Style.WrapText = true;
        worksheet.Cells["A9"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["A9"].Style.Fill.BackgroundColor.SetColor(Color.Gray);
        worksheet.Cells["A9"].Value = "Ziua";

        worksheet.Cells["B9"].Style.WrapText = true;
        worksheet.Cells["B9"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["B9"].Style.Fill.BackgroundColor.SetColor(Color.Gray);
        worksheet.Cells["B9"].Value = "Etape/Denumirea activității";

        worksheet.Cells["C9"].Style.WrapText = true;
        worksheet.Cells["C9"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["C9"].Style.Fill.BackgroundColor.SetColor(Color.Gray);
        worksheet.Cells["C9"].Value = "Descrierea activității prestate";

        worksheet.Cells["D9"].Style.WrapText = true;
        worksheet.Cells["D9"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["D9"].Style.Fill.BackgroundColor.SetColor(Color.Gray);
        worksheet.Cells["D9"].Value = "Nr. ore lucrate și interval orar proiect";

        worksheet.Cells["E9"].Style.WrapText = true;
        worksheet.Cells["E9"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["E9"].Style.Fill.BackgroundColor.SetColor(Color.Gray);
        worksheet.Cells["E9"].Value = "Nr. ore și interval orar alocate altor activități";
        worksheet.Cells["A9:E9"].Style.Font.Bold = true;


        int row = 10;
        double totalOreProiect = 0;
        double totalOreAlteActivitati = 0;

        for (int day = 1; day <= DateTime.DaysInMonth(year, month); day++)
        {
            var pontajeZi = pontaje.Where(p => p.ZiDeLucru.Data.Day == day).ToList();
            bool firstEntry = true;
            int startRow = row;

            if (pontajeZi.Any())
            {
                var oreAlteActivitati = pontajeZi
                    .Where(p => p.TipMunca == "Norma de baza")
                    .Sum(p => p.DurataMuncita.TotalHours);
                totalOreAlteActivitati += oreAlteActivitati;

                var pontajeProiect = pontajeZi.Where(p => p.TipMunca != "Norma de baza").ToList();

                if (oreAlteActivitati > 0)
                {
                    worksheet.Cells[row, 1].Value = day;
                    worksheet.Cells[row, 5].Value = $"{oreAlteActivitati} h";
                    row++;
                }

                foreach (var pontaj in pontajeProiect)
                {
                    worksheet.Cells[row, 1].Value = firstEntry ? day : (object)DBNull.Value;
                    worksheet.Cells[row, 2].Value = pontaj.Proiect?.DenumireaActivitatii;
                    worksheet.Cells[row, 3].Value = pontaj.Proiect?.DescriereaActivitatii;
                    worksheet.Cells[row, 4].Value =
                        $"{pontaj.DurataMuncita.TotalHours} h ({pontaj.OraInceput:hh\\:mm}-{pontaj.OraSfarsit:hh\\:mm})";
                    totalOreProiect += pontaj.DurataMuncita.TotalHours;
                    row++;
                    firstEntry = false;
                }

                if (row - startRow > 1)
                {
                    worksheet.Cells[startRow, 1, row - 1, 1].Merge = true;
                }
            }
            else
            {
                worksheet.Cells[row, 1].Value = day;
                worksheet.Cells[row, 2].Value = "-";
                worksheet.Cells[row, 3].Value = "-";
                worksheet.Cells[row, 4].Value = "-";
                worksheet.Cells[row, 5].Value = "-";
                row++;
            }
        }

        // Total general
        worksheet.Cells[row, 1].Value = "Total:";
        worksheet.Cells[row, 4].Value = $"{totalOreProiect} h";
        worksheet.Cells[row, 5].Value = $"{totalOreAlteActivitati} h";
        worksheet.Cells[row, 1, row, 5].Style.Font.Bold = true;

        // Ajustare dimensiuni coloane
        worksheet.Column(1).Width = 10;
        worksheet.Column(2).Width = 30;
        worksheet.Column(3).Width = 40;
        worksheet.Column(4).Width = 25;
        worksheet.Column(5).Width = 25;

        // Aplicare borduri și aliniere
        worksheet.Cells[10, 1, row, 5].Style.Border.BorderAround(ExcelBorderStyle.Thin);
        worksheet.Cells[10, 1, row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Cells[10, 1, row, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        worksheet.Cells[10, 1, row, 5].Style.WrapText = true;

        var stream = new MemoryStream(package.GetAsByteArray());
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Pontaj_{month}_{year}.xlsx");
    }


    [HttpGet("export-with-project")]
    public async Task<IActionResult> ExportTimesheetByProject([FromQuery] int year, [FromQuery] int month,
        [FromQuery] int projectId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await userManager.GetUserByEmail(User);
        var project = await projectRepo.GetByIdAsync(projectId);
        if (project == null) return NotFound("Proiectul nu există!");

        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var spec = new PontajByMonthSpecification(userId, startDate, endDate, projectId);
        var pontaje = await repo.ListAsync(spec);

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Fisa de pontaj");

        // Setăm antetul
        worksheet.Cells["A3:C3"].Merge = true;
        worksheet.Cells["A3"].Value = "Luna / anul:";
        worksheet.Cells["D3:E3"].Merge = true;
        worksheet.Cells["D3:E3"].Value = $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month)} / {year}";

        worksheet.Cells["A4:C4"].Merge = true;
        worksheet.Cells["A4"].Value = "Numele si prenumele expertului";
        worksheet.Cells["D4:E4"].Merge = true;
        worksheet.Cells["D4:E4"].Value = user.FirstName + " " + user.LastName;

        worksheet.Cells["A5:C5"].Merge = true;
        worksheet.Cells["A5"].Value = "Poziția in proiect";
        worksheet.Cells["D5:E5"].Merge = true;
        worksheet.Cells["D5:E5"].Value = project.PozitiaInProiect;

        worksheet.Cells["A6:C6"].Merge = true;
        worksheet.Cells["A6"].Value = "Denumire beneficiar";
        worksheet.Cells["D6:E6"].Merge = true;
        worksheet.Cells["D6:E6"].Value = project.DenumireBeneficiar;

        worksheet.Cells["A7:C7"].Merge = true;
        worksheet.Cells["A7"].Value = "Cod / titlu proiect";
        worksheet.Cells["D7:E7"].Merge = true;
        worksheet.Cells["D7:E7"].Value = $"{project.TitluProiect}, {project.CodProiect}";

        // Stilizare antet tabel
        worksheet.Cells["A9:E9"].Style.Font.Bold = true;
        worksheet.Cells["A9:E9"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["A9:E9"].Style.Fill.BackgroundColor.SetColor(Color.Gray);
        worksheet.Cells["A9:E9"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Cells["A9:E9"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        worksheet.Cells["A9:E9"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

        // Titlurile coloanelor
        worksheet.Cells["A9"].Value = "Ziua";
        worksheet.Cells["B9"].Value = "Etape/Denumirea activității";
        worksheet.Cells["C9"].Value = "Descrierea activității prestate";
        worksheet.Cells["D9:E9"].Merge = true;
        worksheet.Cells["D9:E9"].Value = "Nr. ore lucrate și interval orar proiect";

        int row = 10;
        double totalOre = 0;

        for (int day = 1; day <= DateTime.DaysInMonth(year, month); day++)
        {
            var pontajeZi = pontaje.Where(p => p.ZiDeLucru.Data.Day == day).ToList();
            if (pontajeZi.Any())
            {
                int startRow = row;
                foreach (var pontaj in pontajeZi)
                {
                    worksheet.Cells[row, 2].Value = pontaj.Proiect?.DenumireaActivitatii;
                    worksheet.Cells[row, 3].Value = pontaj.Proiect?.DescriereaActivitatii;
                    worksheet.Cells[row, 4, row, 5].Merge = true;
                    worksheet.Cells[row, 4].Value =
                        $"{pontaj.DurataMuncita.TotalHours} h ({pontaj.OraInceput:hh\\:mm}-{pontaj.OraSfarsit:hh\\:mm})";
                    totalOre += pontaj.DurataMuncita.TotalHours;
                    row++;
                }

                // Aplicăm Merge doar dacă sunt mai multe rânduri pentru aceeași zi
                if (row - startRow > 1)
                {
                    worksheet.Cells[startRow, 1, row - 1, 1].Merge = true;
                    worksheet.Cells[startRow, 1].Value = day;
                    worksheet.Cells[startRow, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                }
                else
                {
                    worksheet.Cells[startRow, 1].Value = day;
                }
            }
            else
            {
                // Zi fără proiecte
                worksheet.Cells[row, 1].Value = day;
                worksheet.Cells[row, 2].Value = "-";
                worksheet.Cells[row, 3].Value = "-";
                worksheet.Cells[row, 4, row, 5].Merge = true;
                worksheet.Cells[row, 4].Value = "-";
                row++;
            }
        }

        // Total general
        worksheet.Cells[row, 1].Value = "Total:";
        worksheet.Cells[row, 4, row, 5].Merge = true;
        worksheet.Cells[row, 4].Value = $"{totalOre} h";
        worksheet.Cells[row, 1, row, 5].Style.Font.Bold = true;

        // Ajustăm dimensiunile coloanelor
        worksheet.Column(1).Width = 10; // Ziua
        worksheet.Column(2).Width = 30; // Etape/Denumirea activității
        worksheet.Column(3).Width = 40; // Descrierea activității prestate
        worksheet.Column(4).Width = 15; // Nr. ore lucrate
        worksheet.Column(5).Width = 20; // Interval orar proiect

        // Aplicăm borduri și aliniere
        worksheet.Cells[10, 1, row, 5].Style.Border.BorderAround(ExcelBorderStyle.Thin);
        worksheet.Cells[10, 1, row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Cells[10, 1, row, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        worksheet.Cells[10, 1, row, 5].Style.WrapText = true;

        var stream = new MemoryStream(package.GetAsByteArray());
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Pontaj_{project.DenumireaActivitatii}_{month}_{year}.xlsx");
    }
}