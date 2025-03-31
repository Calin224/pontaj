using System;
using System.Security.Claims;
using API.DTOs;
using API.Extensions;
using Core.DTOs;
using Core.Entities;
using Core.Interfaces;
using Core.Specification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace API.Controllers;

[Authorize]
public class PontajeController(
    IPontajService pontajService,
    SignInManager<AppUser> signInManager,
    ITimpDisponibilService timpDisponibilService,
    IGenericRepository<Pontaj> repo) : BaseApiController
{
    [HttpGet("timp-disponibil")]
    public async Task<ActionResult<TimpDisponibilDto>> GetTimpDisponibil([FromQuery] DateTime? luna = null)
    {
        var user = await signInManager.UserManager.GetUserByEmail(User);
        if (user == null) return Unauthorized();

        var lunaSpecificata = luna ?? DateTime.UtcNow;

        var timpDisponibil = await timpDisponibilService.GetTimpDisponibilAsync(user.Id, lunaSpecificata);
        return Ok(timpDisponibil);
    }

    [HttpGet("sumar")]
    public async Task<ActionResult<IEnumerable<PontajSumarDto>>> GetPontajeSumarizate([FromQuery] DateTime start,
        [FromQuery] DateTime end)
    {
        var user = await signInManager.UserManager.GetUserByEmail(User);
        if (user == null) return Unauthorized();

        var pontajeSumar = await pontajService.GetPontajeSumarizateAsync(user.Id, start, end);
        return Ok(pontajeSumar);
    }

    [HttpGet("by-project")]
    public async Task<ActionResult<IEnumerable<PontajDto>>> GetPontajeByProject([FromQuery] DateTime start,
        [FromQuery] DateTime end, [FromQuery] string numeProiect)
    {
        var user = await signInManager.UserManager.GetUserByEmail(User);
        if (user == null) return Unauthorized();

        var spec = new PontajByUserProjectAndPeriodSpecification(user.Id, numeProiect, start, end);
        var pontaje = await repo.ListAsync(spec);

        var pontajeDto = pontaje.Select(p => new PontajDto()
        {
            Id = p.Id,
            Data = p.Data,
            OraStart = p.OraStart,
            OraFinal = p.OraFinal,
            NumeProiect = p.NumeProiect,
            NormaBaza = p.NormaBaza,
            UserId = p.UserId
        }).ToList();

        return Ok(pontajeDto);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PontajDto>>> GetPontaje([FromQuery] DateTime start,
        [FromQuery] DateTime end)
    {
        var user = await signInManager.UserManager.GetUserByEmail(User);
        if (user == null) return Unauthorized();

        var pontaje = await pontajService.GetPontajeByUserAndPeriodAsync(user.Id, start, end);
        return Ok(pontaje);
    }

    [HttpGet("proiecte")]
    public async Task<ActionResult<IEnumerable<string>>> GetProiecte()
    {
        var user = await signInManager.UserManager.GetUserByEmail(User);
        if (user == null) return Unauthorized();

        var proiecte = await pontajService.GetProiecteListByUserIdAsync(user.Id);
        return Ok(proiecte);
    }

    [HttpPost("generare-norma")]
    public async Task<ActionResult> GenerareNormaBaza([FromBody] GenerareNormaDto dto)
    {
        var user = await signInManager.UserManager.GetUserByEmail(User);
        if (user == null) return Unauthorized();

        await pontajService.GenerareNormaBazaAsync(user.Id, dto.Luna);
        return Ok(new { message = "Norma de bază a fost generată cu succes." });
    }

    [HttpPost("generare-pontaj")]
    public async Task<ActionResult<IEnumerable<PontajDto>>> GenerarePontajeProiect([FromBody] GenerarePontajDto dto)
    {
        var user = await signInManager.UserManager.GetUserByEmail(User);
        if (user == null) return Unauthorized();

        var pontajeCreate = dto.PermiteAjustareaNorma
            ? await pontajService.GenerarePontajeProiectCuAjustareNormaAsync(
                user.Id, dto.DataInceput, dto.DataSfarsit, dto.NumeProiect, dto.OreAlocate)
            : await pontajService.GenerarePontajeProiectAsync(
                user.Id, dto.DataInceput, dto.DataSfarsit, dto.NumeProiect, dto.OreAlocate);

        return Ok(pontajeCreate);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeletePontaj(int id)
    {
        var user = await signInManager.UserManager.GetUserByEmail(User);
        if (user == null) return Unauthorized();

        var pontaj = await pontajService.GetPontajByIdAsync(id);
        if (pontaj == null) return NotFound();

        if (pontaj.UserId != user.Id) return Unauthorized();

        await pontajService.DeletePontajAsync(id);
        return Ok();
    }

    [HttpPost("simulare-pontaj")]
    public async Task<ActionResult<PontajSimulareResponse>> SimularePontajeProiect([FromBody] GenerarePontajDto dto)
    {
        var user = await signInManager.UserManager.GetUserByEmail(User);
        if (user == null) return Unauthorized();

        Console.WriteLine($"Simulare pontaje: PermiteAjustareaNorma = {dto.PermiteAjustareaNorma}");

        try
        {
            if (dto.PermiteAjustareaNorma)
            {
                var rezultatCuAjustare = await pontajService.SimuleazaPontajeProiectCuAjustareNormaAsync(
                    user.Id, dto.DataInceput, dto.DataSfarsit, dto.NumeProiect, dto.OreAlocate);

                var countInlocuiri = rezultatCuAjustare.Pontaje.Count(p => p.InlocuiesteNorma);
                Console.WriteLine(
                    $"Pontaje care înlocuiesc normă: {countInlocuiri} din {rezultatCuAjustare.Pontaje.Count()}");
                Console.WriteLine(
                    $"Ore rămase: {rezultatCuAjustare.OreRamase}, Zile necesare: {rezultatCuAjustare.ZileNecesareExtra}");

                return Ok(rezultatCuAjustare);
            }
            else
            {
                var rezultatSimulare = await pontajService.SimuleazaPontajeProiectAsync(
                    user.Id, dto.DataInceput, dto.DataSfarsit, dto.NumeProiect, dto.OreAlocate);

                return Ok(rezultatSimulare);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Excepție în timpul simulării: {ex.Message}");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("export-excel")]
    public async Task<IActionResult> ExportPontajToExcel([FromQuery] string numeProiect, [FromQuery] DateTime luna)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await signInManager.UserManager.GetUserByEmail(User);
        if (user == null) return Unauthorized();

        var year = luna.Year;
        var month = luna.Month;

        // Obținem pontajele utilizatorului pentru luna specificată
        var spec = new PontajByUserAndPeriodSpecification(
            userId,
            new DateTime(year, month, 1),
            new DateTime(year, month, DateTime.DaysInMonth(year, month))
        );
        var pontaje = await repo.ListAsync(spec);

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Fișă de pontaj");

        worksheet.Cells["A1:E1"].Merge = true;
        worksheet.Cells["A1"].Value = "Fișă de pontaj";
        worksheet.Cells["A1"].Style.Font.Size = 14;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        worksheet.Cells["A3"].Value = "Luna / anul:";
        worksheet.Cells["C3:E3"].Merge = true;
        worksheet.Cells["C3:E3"].Value =
            $"{System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month)} / {year}";
        worksheet.Cells["A4"].Value = "Numele și prenumele expertului";
        worksheet.Cells["C4:E4"].Merge = true;
        worksheet.Cells["C4"].Value = $"{user.FirstName} {user.LastName}";
        worksheet.Cells["A5"].Value = "Poziția în proiect";
        worksheet.Cells["C5:E5"].Merge = true;
        worksheet.Cells["C5:E5"].Value = "Expert"; // Valoare predefinită sau dintr-o proprietate a utilizatorului
        worksheet.Cells["A6"].Value = "Denumire beneficiar";
        worksheet.Cells["C6:E6"].Merge = true;
        worksheet.Cells["C6:E6"].Value = "Universitatea din Craiova"; // Valoare predefinită
        worksheet.Cells["A7"].Value = "Cod / titlu proiect";
        worksheet.Cells["C7:E7"].Style.WrapText = true;
        worksheet.Cells["C7:E7"].Merge = true;
        worksheet.Cells["C7:E7"].Value = numeProiect;

        worksheet.Cells["A9"].Style.WrapText = true;
        worksheet.Cells["A9"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["A9"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Gray);
        worksheet.Cells["A9"].Value = "Ziua";

        worksheet.Cells["B9"].Style.WrapText = true;
        worksheet.Cells["B9"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["B9"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Gray);
        worksheet.Cells["B9"].Value = "Etape/Denumirea activității";

        worksheet.Cells["C9"].Style.WrapText = true;
        worksheet.Cells["C9"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["C9"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Gray);
        worksheet.Cells["C9"].Value = "Descrierea activității prestate";

        worksheet.Cells["D9"].Style.WrapText = true;
        worksheet.Cells["D9"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["D9"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Gray);
        worksheet.Cells["D9"].Value = "Nr. ore lucrate și interval orar proiect";

        worksheet.Cells["E9"].Style.WrapText = true;
        worksheet.Cells["E9"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["E9"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Gray);
        worksheet.Cells["E9"].Value = "Nr. ore și interval orar alocate altor activități";
        worksheet.Cells["A9:E9"].Style.Font.Bold = true;

        int row = 10;
        double totalOreProiect = 0;
        double totalOreAlteActivitati = 0;

        for (int day = 1; day <= DateTime.DaysInMonth(year, month); day++)
        {
            var pontajeZi = pontaje.Where(p => p.Data.Day == day).ToList();
            bool firstEntry = true;
            int startRow = row;

            if (pontajeZi.Any())
            {
                var pontajeNormaBaza = pontajeZi.Where(p => p.NormaBaza).ToList();
                var oreAlteActivitati = pontajeNormaBaza.Sum(p => (p.OraFinal - p.OraStart).TotalHours);
                totalOreAlteActivitati += oreAlteActivitati;

                var pontajeProiect = pontajeZi.Where(p => !p.NormaBaza && p.NumeProiect == numeProiect).ToList();
                var pontajeAlteProiecte = pontajeZi.Where(p => !p.NormaBaza && p.NumeProiect != numeProiect).ToList();

                // Adăugăm și orele altor proiecte la totalul de alte activități
                oreAlteActivitati += pontajeAlteProiecte.Sum(p => (p.OraFinal - p.OraStart).TotalHours);

                if (oreAlteActivitati > 0)
                {
                    worksheet.Cells[row, 1].Value = day;
                    worksheet.Cells[row, 5].Value = $"{oreAlteActivitati} h";
                    row++;
                }

                foreach (var pontaj in pontajeProiect)
                {
                    worksheet.Cells[row, 1].Value = firstEntry ? day : (object)DBNull.Value;
                    worksheet.Cells[row, 2].Value = numeProiect; // Cod activitate predefinit

                    var oreLucrate = (pontaj.OraFinal - pontaj.OraStart).TotalHours;
                    worksheet.Cells[row, 4].Value =
                        $"{oreLucrate} h ({pontaj.OraStart.Hours:D2}:{pontaj.OraStart.Minutes:D2}-{pontaj.OraFinal.Hours:D2}:{pontaj.OraFinal.Minutes:D2})";
                    totalOreProiect += oreLucrate;

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

        worksheet.Cells[row, 1].Value = "Total:";
        worksheet.Cells[row, 4].Value = $"{totalOreProiect} h";
        worksheet.Cells[row, 5].Value = $"{totalOreAlteActivitati} h";
        worksheet.Cells[row, 1, row, 5].Style.Font.Bold = true;

        worksheet.Column(1).Width = 10;
        worksheet.Column(2).Width = 30;
        worksheet.Column(3).Width = 40;
        worksheet.Column(4).Width = 25;
        worksheet.Column(5).Width = 25;

        worksheet.Cells[10, 1, row, 5].Style.Border.BorderAround(ExcelBorderStyle.Thin);
        worksheet.Cells[10, 1, row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Cells[10, 1, row, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        worksheet.Cells[10, 1, row, 5].Style.WrapText = true;

        var stream = new MemoryStream(package.GetAsByteArray());
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Pontaj.xlsx");
    }
}