using System;

namespace Core.Entities;

public class Proiect : BaseEntity
{
    public string Nume { get; set; }
    public ICollection<Pontaj> Pontaje { get; set; } = [];
}
