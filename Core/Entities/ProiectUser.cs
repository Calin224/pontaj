using System;

namespace Core.Entities;

public class ProiectUser
{
    public string UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public int ProiectId { get; set; }
    public Proiect Proiect { get; set; } = null!;
}
