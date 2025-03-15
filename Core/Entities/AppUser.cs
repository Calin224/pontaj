using System;
using Microsoft.AspNetCore.Identity;

namespace Core.Entities;

public class AppUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public ICollection<Proiect> Proiecte { get; set; } = [];
    public ICollection<ZiDeLucru> ZileMuncite { get; set; } = [];
}
