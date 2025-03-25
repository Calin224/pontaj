using System;
using API.DTOs;
using API.Extensions;
using AutoMapper;
using Core.DTOs;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class PontajeController(IPontajService pontajService, SignInManager<AppUser> signInManager) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PontajDto>>> GetPontaje([FromQuery] DateTime start, [FromQuery] DateTime end)
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

        var pontajeCreate = await pontajService.GenerarePontajeProiectAsync(
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
    public async Task<ActionResult<IEnumerable<PontajDto>>> SimularePontajeProiect([FromBody] GenerarePontajDto dto)
    {
        var user = await signInManager.UserManager.GetUserByEmail(User);
        if (user == null) return Unauthorized();

        var pontajeSimulate = await pontajService.SimuleazaPontajeProiectAsync(
            user.Id, dto.DataInceput, dto.DataSfarsit, dto.NumeProiect, dto.OreAlocate);
            
        return Ok(pontajeSimulate);
    }
}
