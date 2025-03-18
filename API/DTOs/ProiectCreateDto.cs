 using System;
using Microsoft.Build.Framework;
using Microsoft.Extensions.Localization;

namespace API.DTOs;

public class ProiectCreateDto
{
    [Required] public string DenumireaActivitatii { get; set; } = string.Empty;
    [Required] public string DescriereaActivitatii { get; set; } = string.Empty;
    [Required] public string PozitiaInProiect { get; set; } = string.Empty;
    [Required] public string CategoriaExpertului { get; set; } = string.Empty;
    [Required] public string DenumireBeneficiar { get; set; } = string.Empty;
    public string? TitluProiect { get; set; }
    public string? CodProiect { get; set; }
}
