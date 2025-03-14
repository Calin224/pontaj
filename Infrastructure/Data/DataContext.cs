using System;
using Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class DataContext(DbContextOptions options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<ZiDeLucru> ZileDeLucru { get; set; }
    public DbSet<Pontaj> Pontaje { get; set; }
    public DbSet<Proiect> Proiecte { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    }
}
