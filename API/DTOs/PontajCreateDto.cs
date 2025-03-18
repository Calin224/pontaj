using System;

namespace API.DTOs;

public class PontajCreateDto
{
    public DateTime Data { get; set; }
    public TimeSpan OraInceput { get; set; }
    public TimeSpan OraSfarsit { get; set; }
    public string TipMunca { get; set; } = "Norma de baza";
    public int? ProiectId { get; set; }
}
