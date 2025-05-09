using System;
using API.DTOs;
using API.Extensions;
using Core.DTOs;
using Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

public class AccountController(SignInManager<AppUser> signInManager) : BaseApiController
{
    [HttpPost("register")]
    public async Task<ActionResult> Register(RegisterDto registerDto)
    {
        var user = new AppUser()
        {
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            Email = registerDto.Email,
            UserName = registerDto.Email,
        };

        var res = await signInManager.UserManager.CreateAsync(user, registerDto.Password);
        
        if (!res.Succeeded)
        {
            return ValidationProblem();
        }

        await signInManager.SignInAsync(user, false);
        return Ok();
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }

    [HttpGet("user-info")]
    public async Task<ActionResult> GetUserInfo()
    {
        if (User.Identity?.IsAuthenticated == false) return NoContent();
        var user = await signInManager.UserManager.GetUserByEmail(User);

        return Ok(new
        {
            user.FirstName,
            user.LastName,
            user.Email,
            user.Id
        });
    }

    [HttpGet("auth-status")]
    public ActionResult AuthStatus()
    {
        return Ok(new {
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false
        });
    }

    [HttpGet("all-users")]
    public async Task<ActionResult<IReadOnlyList<AppUser>>> GetAllUsers()
    {
        var users = await signInManager.UserManager.Users.ToListAsync();
        
        // var user = await signInManager.UserManager.GetUserByEmail(User);

        return Ok(users.Select(x => new
        {
            x.FirstName,
            x.LastName
        }).ToList());
    }
}
