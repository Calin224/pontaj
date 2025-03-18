using System.Security.Claims;
using Core.Entities;
using Core.Interfaces;
using Core.Specification;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IO;

namespace API.Controllers;

public class ZiController(IGenericRepository<ZiDeLucru> repo) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ZiDeLucru>>> GetZileDeLucru([FromQuery] int year, [FromQuery] int month)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var spec = new ZiDeLucruSpecification(userId, year, month);
        var zileDeLucru = await repo.ListAsync(spec);
        
        return Ok(zileDeLucru);
    }
}