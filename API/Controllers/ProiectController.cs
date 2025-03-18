using System;
using System.Security.Claims;
using API.DTOs;
using Core.Entities;
using Core.Interfaces;
using Core.Specification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;

namespace API.Controllers;

public class ProiectController(IGenericRepository<Proiect> repo) : BaseApiController
{
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateProject(ProiectCreateDto proiectCreateDto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var proiect = new Proiect
        {
            DenumireaActivitatii = proiectCreateDto.DenumireaActivitatii,
            DescriereaActivitatii = proiectCreateDto.DescriereaActivitatii,
            PozitiaInProiect = proiectCreateDto.PozitiaInProiect,
            CategoriaExpertului = proiectCreateDto.CategoriaExpertului,
            DenumireBeneficiar = proiectCreateDto.DenumireBeneficiar,
            TitluProiect = proiectCreateDto.TitluProiect,
            CodProiect = proiectCreateDto.CodProiect,
            UserId = userId
        };

        repo.Add(proiect);
        if(await repo.SaveAllAsync()) return Ok();

        return BadRequest("Failed to create project");
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Proiect>>> GetProjects()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var spec = new ProiectSpecification(userId);
        var projects = await repo.ListAsync(spec);

        return Ok(projects);
    }
    
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var spec = new ProiectSpecification(userId, id);
        var project = await repo.GetEntityWithSpec(spec);

        if (project == null) return NotFound();

        repo.Delete(project);
        if (await repo.SaveAllAsync()) return Ok();

        return BadRequest("Failed to delete project");
    }
}
