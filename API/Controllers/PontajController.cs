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
            Proiect = p.Proiect != null ? new { p.Proiect.Id, DenumireaActivitatii = p.Proiect.DenumireaActivitatii } : null
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
        if (!pontaje.Any()) return NotFound("Nu există pontaje pentru această lună!");

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Fișă de pontaj");

        var project = await projectRepo.GetEntityWithSpec(new ProiectSpecification(userId));
        if(project == null) return NotFound();

        // Title Section
        worksheet.Cells["A1:E1"].Merge = true;
        worksheet.Cells["A1"].Value = "Fișă de pontaj";
        worksheet.Cells["A1"].Style.Font.Size = 14;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        // Header Section
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
        // worksheet.Cells["E7"].Style.WrapText = true;
        worksheet.Cells["D7:E7"].Merge = true;
        worksheet.Cells["D7:E7"].Value = project.TitluProiect != null ? $"{project.TitluProiect}, " : "";
        worksheet.Cells["D7:E7"].Value += project.CodProiect != null ? $"{project.CodProiect}" : "";

        // Table Headers
        worksheet.Cells["A9"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["A9"].Style.Fill.BackgroundColor.SetColor(Color.Gray);
        worksheet.Cells["A9"].Value = "Ziua";
        
        worksheet.Cells["B9"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["B9"].Style.Fill.BackgroundColor.SetColor(Color.Gray);
        worksheet.Cells["B9"].Value = "Etape/Denumirea activității";
        
        worksheet.Cells["C9"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["C9"].Style.Fill.BackgroundColor.SetColor(Color.Gray);
        worksheet.Cells["C9"].Value = "Descrierea activității prestate";
        
        worksheet.Cells["D9"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["D9"].Style.Fill.BackgroundColor.SetColor(Color.Gray);
        worksheet.Cells["D9"].Value = "Nr. ore lucrate și interval orar proiect";
        
        worksheet.Cells["E9"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["E9"].Style.Fill.BackgroundColor.SetColor(Color.Gray);
        worksheet.Cells["E9"].Value = "Nr. ore și interval orar alocate altor activități";
        
        worksheet.Cells["A9:E9"].Style.Font.Bold = true;
        worksheet.Cells["A9:E9"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

        int row = 10;
        // int totalOreProiect = 0;
        // int totalOreAlteActivitati = 0;
        int totalZileLucrate = 0;
        
        double totalOreProiect = 0;
        double totalOreAlteActivitati = 0;

        for (int day = 1; day <= DateTime.DaysInMonth(year, month); day++)
        {
            var pontajeZi = pontaje.Where(p => p.ZiDeLucru.Data.Day == day).ToList();
            if (pontajeZi.Any())
            {
                totalZileLucrate++;
                bool firstEntry = true;
                var normeDeBazaTotal = 0.0;
                var proiectePontaje = new List<Pontaj>();

                foreach (Pontaj pontaj in pontajeZi)
                {
                    if (pontaj.TipMunca == "Norma de baza")
                    {
                        normeDeBazaTotal += pontaj.DurataMuncita.TotalHours;
                    }
                    else
                    {
                        proiectePontaje.Add(pontaj);
                    }
                }

                int startRow = row;
                worksheet.Cells[row, 1].Value = day;
                worksheet.Cells[row, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                if (normeDeBazaTotal > 0)
                {
                    worksheet.Cells[row, 2].Value = "";
                    worksheet.Cells[row, 5].Value = $"{normeDeBazaTotal} h";
                    totalOreAlteActivitati += normeDeBazaTotal;
                    row++;
                }

                foreach (var pontaj in proiectePontaje)
                {
                    worksheet.Cells[row, 2].Value = pontaj.Proiect?.DenumireaActivitatii;
                    worksheet.Cells[row, 3].Value = pontaj.Proiect?.DescriereaActivitatii;
                    worksheet.Cells[row, 4].Value =
                        $"{pontaj.DurataMuncita.TotalHours} h ({pontaj.OraInceput:hh\\:mm}-{pontaj.OraSfarsit:hh\\:mm})";
                    totalOreProiect += pontaj.DurataMuncita.TotalHours;
                    row++;
                }

                if (row > startRow + 1)
                {
                    worksheet.Cells[startRow, 1, row - 1, 1].Merge = true;
                    worksheet.Cells[startRow, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                }
            }
            else
            {
                worksheet.Cells[row, 1].Value = day;
                row++;
            }

            // Set row height for each row
            worksheet.Row(row).Height = 30; // Adjust the value as needed
        }

        // Total Section
        worksheet.Cells[row, 1].Value = "Total:";
        worksheet.Cells[row, 1].Style.Font.Bold = true;
        worksheet.Cells[row, 4].Value = totalOreProiect.ToString("0.00") + " h";
        worksheet.Cells[row, 5].Value = totalOreAlteActivitati.ToString("0.00") + " h";
        worksheet.Cells[9, 1, row, 5].Style.Border.BorderAround(ExcelBorderStyle.Thin);
        

        // Adjust Row Height and Column Width
        worksheet.Row(1).Height = 20;
        worksheet.Column(1).Width = 8;
        worksheet.Column(2).Width = 30;
        worksheet.Column(3).Width = 30;
        worksheet.Column(4).Width = 30;
        worksheet.Column(5).Width = 30;

        // Center Text Alignment
        worksheet.Cells["A1:E" + row].Style.WrapText = true;
        worksheet.Cells["A1:E" + row].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Cells["A1:E" + row].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

        for (int i = 10; i < row; i++)
        {
            worksheet.Cells[i, 1, i, 5].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        }
        
        // worksheet.Cells.AutoFitColumns();

        var stream = new MemoryStream(package.GetAsByteArray());
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Pontaj_{month}_{year}.xlsx");
    }
}