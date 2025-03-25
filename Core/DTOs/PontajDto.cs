using System;

namespace Core.DTOs;

public class PontajDto
{
    public int Id { get; set; }
    public DateTime Data { get; set; }
    public TimeSpan OraStart { get; set; }
    public TimeSpan OraFinal { get; set; }
    public string NumeProiect { get; set; }
    public bool NormaBaza { get; set; }
    public string UserId { get; set; }
}
