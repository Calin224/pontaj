using System;
using System.Text.Json.Serialization;

namespace Core.Entities;

public class Pontaj : BaseEntity
{
    [JsonIgnore]
    public int ZiDeLucruId { get; set; }
    public ZiDeLucru ZiDeLucru { get; set; }

    public TimeSpan OraInceput { get; set; }
    public TimeSpan OraSfarsit { get; set; }
    public string TipMunca { get; set; } = "Norma de baza";

    public int? ProiectId { get; set; }
    public Proiect? Proiect { get; set; }

    public string UserId { get; set; }
    [JsonIgnore]
    public AppUser AppUser { get; set; }

    public TimeSpan DurataMuncita => OraSfarsit - OraInceput;
}
