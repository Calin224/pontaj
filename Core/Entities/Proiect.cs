using System;

namespace Core.Entities;

public class Proiect : BaseEntity
{
    public string Nume { get; set; }

    public ICollection<AppUser> Useri { get; set; } = [];
    public ICollection<Pontaj> Pontaje { get; set; } = [];
}
